using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Numerics;
using JoltPhysicsSharp;
using MeltEngine.Core;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using MeltEngine.Utils;

namespace MeltEngine.Systems;

public struct SurfaceBlockComponent
{
}

public class WorldGeneratorSystem : ISystem
{
    public static WorldGeneratorSystem? Instance { get; private set; }
    
    private readonly TerrainData _terrainData;
    private readonly int _worldSizeX;
    private readonly int _worldSizeZ;
    private readonly int _renderRadius;
    private PhysicsManager? _physicsManager;

    private Vector3 _spawnPosition;
    private bool _worldInitialized;
    private readonly HashSet<(int cx, int cz)> _loadedChunks = new();
    private readonly HashSet<(int cx, int cz)> _chunksToLoad = new();
    private readonly ConcurrentQueue<(int cx, int cz, BlockType[,,] blocks)> _pendingChunks = new();
    private readonly ConcurrentQueue<Entity> _entitiesToDestroy = new();
    private readonly List<Entity> _surfaceEntities = new();
    private readonly Dictionary<Entity, (int worldX, int worldY, int worldZ)> _blockPositions = new();
    private readonly Dictionary<(int worldX, int worldY, int worldZ), Entity> _entityByPosition = new();
    private int _blocksAppliedThisFrame;
    private const int MaxBlocksPerFrame = 2000;
    private const int MaxChunksPerFrame = 4;
    private bool _pendingBroadPhaseOptimize;

    public WorldGeneratorSystem(int seed, int worldSizeX = 256, int worldSizeZ = 256, int renderRadius = 6, PhysicsManager? physicsManager = null)
    {
        Instance = this;
        _terrainData = new TerrainData(seed);
        _worldSizeX = worldSizeX;
        _worldSizeZ = worldSizeZ;
        _renderRadius = renderRadius;
        _physicsManager = physicsManager;
        Console.WriteLine($"[WorldGenerator] Seed={seed}, Size={worldSizeX}x{worldSizeZ}, RenderRadius={renderRadius}");
    }

    /// <summary>
    /// Sets the PhysicsManager reference (can be called after construction).
    /// Required for proper Jolt body cleanup on block destruction.
    /// </summary>
    public void SetPhysicsManager(PhysicsManager physicsManager)
    {
        _physicsManager = physicsManager;
    }

    public void Update(ECSOperator entityOperator, float frameTime)
    {
        if (!_worldInitialized)
        {
            InitializeWorld();
            _worldInitialized = true;
        }

        ApplyPendingChunks(entityOperator);
        DestroyQueuedEntities(entityOperator);
        UpdateChunkLoading();

        // Batch broadphase optimization: run once after all block changes this frame
        if (_pendingBroadPhaseOptimize && _physicsManager != null)
        {
            _physicsManager.OptimizeBroadPhase();
            _pendingBroadPhaseOptimize = false;
        }
    }

    private void InitializeWorld()
    {
        int spawnX = _worldSizeX / 2;
        int spawnZ = _worldSizeZ / 2;
        int spawnY = _terrainData.GetHeight(spawnX, spawnZ) + 3;
        _spawnPosition = new Vector3(spawnX, spawnY, spawnZ);

        int playerChunkX = spawnX / TerrainData.ChunkSizeX;
        int playerChunkZ = spawnZ / TerrainData.ChunkSizeZ;

        for (int dx = -_renderRadius; dx <= _renderRadius; dx++)
        {
            for (int dz = -_renderRadius; dz <= _renderRadius; dz++)
            {
                int dist = Math.Max(Math.Abs(dx), Math.Abs(dz));
                if (dist <= _renderRadius)
                {
                    _chunksToLoad.Add((playerChunkX + dx, playerChunkZ + dz));
                }
            }
        }

        int chunksToGenerate = _chunksToLoad.Count;
        int generated = 0;
        foreach (var chunk in _chunksToLoad.ToList())
        {
            if (generated >= 16) break;
            Task.Run(() => GenerateChunkAsync(chunk.cx, chunk.cz));
            generated++;
        }

        Console.WriteLine($"[WorldGenerator] Spawn: ({spawnX}, {spawnY}, {spawnZ})");
        Console.WriteLine($"[WorldGenerator] Initial chunks queued: {chunksToGenerate}");
    }

    private void GenerateChunkAsync(int chunkX, int chunkZ)
    {
        var blocks = _terrainData.GenerateChunk(chunkX, chunkZ);
        _pendingChunks.Enqueue((chunkX, chunkZ, blocks));
    }

    private void ApplyPendingChunks(ECSOperator entityOperator)
    {
        _blocksAppliedThisFrame = 0;
        int chunksApplied = 0;

        while (_pendingChunks.TryDequeue(out var pending) && chunksApplied < MaxChunksPerFrame)
        {
            var (chunkX, chunkZ, blocks) = pending;

            if (_loadedChunks.Contains((chunkX, chunkZ))) continue;

            int count = GenerateSurfaceBlocks(entityOperator, chunkX, chunkZ, blocks);
            _blocksAppliedThisFrame += count;
            _loadedChunks.Add((chunkX, chunkZ));
            chunksApplied++;

            if (_blocksAppliedThisFrame >= MaxBlocksPerFrame) break;
        }
    }

    private void DestroyQueuedEntities(ECSOperator entityOperator)
    {
        int destroyed = 0;
        var staticBodies = entityOperator.GetComponentArray<StaticPhysicsBodyComponent>();
        bool hasPhysics = _physicsManager != null;

        while (_entitiesToDestroy.TryDequeue(out var entity) && destroyed < 100)
        {
            // Clean up Jolt physics body BEFORE destroying the entity
            if (hasPhysics && staticBodies.Components.TryGetValue(entity, out var staticBody))
            {
                if (!staticBody.BodyId.IsInvalid)
                {
                    _physicsManager!.BodyInterface.RemoveBody(staticBody.BodyId);
                    _physicsManager.BodyInterface.DestroyBody(staticBody.BodyId);
                }
            }

            if (_surfaceEntities.Remove(entity))
            {
                if (_blockPositions.TryGetValue(entity, out var pos))
                {
                    _entityByPosition.Remove(pos);
                }
                _blockPositions.Remove(entity);
            }

            entityOperator.DestroyEntity(entity);
            destroyed++;
        }
    }

    private void UpdateChunkLoading()
    {
        int pendingGeneration = _pendingChunks.Count;
        if (pendingGeneration < 20)
        {
            int toGenerate = Math.Min(8, _chunksToLoad.Count - _loadedChunks.Count);
            for (int i = 0; i < toGenerate; i++)
            {
                var next = _chunksToLoad.Except(_loadedChunks).FirstOrDefault();
                if (next != default)
                {
                    _chunksToLoad.Remove(next);
                    Task.Run(() => GenerateChunkAsync(next.cx, next.cz));
                }
            }
        }
    }

    private int GenerateSurfaceBlocks(ECSOperator entityOperator, int chunkX, int chunkZ, BlockType[,,] blocks)
    {
        int count = 0;

        for (int lx = 0; lx < TerrainData.ChunkSizeX && _blocksAppliedThisFrame + count < MaxBlocksPerFrame; lx++)
        {
            for (int lz = 0; lz < TerrainData.ChunkSizeZ && _blocksAppliedThisFrame + count < MaxBlocksPerFrame; lz++)
            {
                int worldX = chunkX * TerrainData.ChunkSizeX + lx;
                int worldZ = chunkZ * TerrainData.ChunkSizeZ + lz;
                int surfaceHeight = _terrainData.GetHeight(worldX, worldZ);

                for (int y = Math.Max(0, surfaceHeight - 2); y <= surfaceHeight + 1 && y < TerrainData.ChunkSizeY; y++)
                {
                    if (_blocksAppliedThisFrame + count >= MaxBlocksPerFrame) break;

                    var block = blocks[lx, y, lz];
                    if (block == BlockType.Air || block == BlockType.Bedrock) continue;

                    if (!IsBlockVisible(blocks, lx, y, lz, block)) continue;

                    var entity = entityOperator.CreateEntity();
                    Vector3 pos = new Vector3(worldX, y, worldZ);

                    entityOperator.AddComponent(entity, new CoordComponent
                    {
                        Position = pos,
                        Scale = Vector3.One
                    });

                    entityOperator.AddComponent(entity, new CubeRendererComponent());
                    entityOperator.AddComponent(entity, new EnabledComponent());
                    entityOperator.AddComponent(entity, new BlockTypeComponent(block));
                    entityOperator.AddComponent(entity, new SurfaceBlockComponent());
                    entityOperator.AddComponent(entity, new SceneMemberComponent("Terrain"));

                    // Añadir física estática con LOD para optimización.
                    // Solo estará activa la simulación en los bloques cercanos al jugador.
                    entityOperator.AddComponent(entity, new StaticPhysicsBodyComponent
                    {
                        UseLod = true,
                        LodDisableDistance = 60.0f
                    });

                    _surfaceEntities.Add(entity);
                    var worldPos = (worldX, y, worldZ);
                    _blockPositions[entity] = worldPos;
                    _entityByPosition[worldPos] = entity;
                    count++;
                }
            }
        }

        return count;
    }

    private bool IsBlockVisible(BlockType[,,] chunk, int lx, int ly, int lz, BlockType blockType)
    {
        if (blockType == BlockType.Water) return true;

        int[] dx = { 1, -1, 0, 0, 0, 0 };
        int[] dy = { 0, 0, 1, -1, 0, 0 };
        int[] dz = { 0, 0, 0, 0, 1, -1 };

        for (int i = 0; i < 6; i++)
        {
            int nx = lx + dx[i];
            int ny = ly + dy[i];
            int nz = lz + dz[i];

            BlockType neighbor;
            if (nx >= 0 && nx < TerrainData.ChunkSizeX &&
                ny >= 0 && ny < TerrainData.ChunkSizeY &&
                nz >= 0 && nz < TerrainData.ChunkSizeZ)
            {
                neighbor = chunk[nx, ny, nz];
            }
            else
            {
                int worldX = _loadedChunks.FirstOrDefault().cx * TerrainData.ChunkSizeX + nx;
                int worldZ = _loadedChunks.FirstOrDefault().cz * TerrainData.ChunkSizeZ + nz;
                neighbor = _terrainData.GetBlock(worldX, ny, worldZ);
            }

            if (neighbor == BlockType.Air || neighbor == BlockType.Water)
            {
                return true;
            }
        }

        return false;
    }

    public Vector3 GetSpawnPosition()
    {
        if (_spawnPosition == default)
        {
            int spawnX = _worldSizeX / 2;
            int spawnZ = _worldSizeZ / 2;
            int spawnY = _terrainData.GetHeight(spawnX, spawnZ) + 3;
            _spawnPosition = new Vector3(spawnX, spawnY, spawnZ);
        }

        return _spawnPosition;
    }

    public int GetVisibleBlockCount() => _surfaceEntities.Count;
    public int GetLoadedChunks() => _loadedChunks.Count;
    public int GetPendingChunks() => _pendingChunks.Count;
    public TerrainData GetTerrainData() => _terrainData;

    public void ModifyBlock(ECSOperator entityOperator, int worldX, int worldY, int worldZ, BlockType newType)
    {
        if (worldY < 0 || worldY >= TerrainData.ChunkSizeY) return;

        BlockType oldType = _terrainData.GetBlock(worldX, worldY, worldZ);
        if (oldType == newType) return;

        _terrainData.SetBlock(worldX, worldY, worldZ, newType);

        if (newType == BlockType.Air || newType == BlockType.Water)
        {
            if (_entityByPosition.TryGetValue((worldX, worldY, worldZ), out var entity))
            {
                _entitiesToDestroy.Enqueue(entity);
            }
        }
        else
        {
            if (!_entityByPosition.ContainsKey((worldX, worldY, worldZ)))
            {
                CreateBlockEntity(entityOperator, worldX, worldY, worldZ, newType);
                _pendingBroadPhaseOptimize = true;
            }
        }

        // Refresh neighbors
        RefreshBlockVisibility(entityOperator, worldX + 1, worldY, worldZ);
        RefreshBlockVisibility(entityOperator, worldX - 1, worldY, worldZ);
        RefreshBlockVisibility(entityOperator, worldX, worldY + 1, worldZ);
        RefreshBlockVisibility(entityOperator, worldX, worldY - 1, worldZ);
        RefreshBlockVisibility(entityOperator, worldX, worldY, worldZ + 1);
        RefreshBlockVisibility(entityOperator, worldX, worldY, worldZ - 1);
    }

    private void RefreshBlockVisibility(ECSOperator entityOperator, int worldX, int worldY, int worldZ)
    {
        if (worldY < 0 || worldY >= TerrainData.ChunkSizeY) return;

        BlockType type = _terrainData.GetBlock(worldX, worldY, worldZ);
        if (type == BlockType.Air || type == BlockType.Bedrock) return;

        int cx = worldX >= 0 ? worldX / TerrainData.ChunkSizeX : (worldX - TerrainData.ChunkSizeX + 1) / TerrainData.ChunkSizeX;
        int cz = worldZ >= 0 ? worldZ / TerrainData.ChunkSizeZ : (worldZ - TerrainData.ChunkSizeZ + 1) / TerrainData.ChunkSizeZ;
        int lx = worldX - cx * TerrainData.ChunkSizeX;
        int lz = worldZ - cz * TerrainData.ChunkSizeZ;

        bool isVisible = false;
        
        // Simple visibility check manually without blocks array
        int[] dx = { 1, -1, 0, 0, 0, 0 };
        int[] dy = { 0, 0, 1, -1, 0, 0 };
        int[] dz = { 0, 0, 0, 0, 1, -1 };

        for (int i = 0; i < 6; i++)
        {
            BlockType neighbor = _terrainData.GetBlock(worldX + dx[i], worldY + dy[i], worldZ + dz[i]);
            if (neighbor == BlockType.Air || neighbor == BlockType.Water)
            {
                isVisible = true;
                break;
            }
        }

        bool hasEntity = _entityByPosition.TryGetValue((worldX, worldY, worldZ), out var entity);

        if (isVisible && !hasEntity)
        {
            CreateBlockEntity(entityOperator, worldX, worldY, worldZ, type);
            _pendingBroadPhaseOptimize = true;
        }
        else if (!isVisible && hasEntity)
        {
            _entitiesToDestroy.Enqueue(entity);
        }
    }

    private void CreateBlockEntity(ECSOperator entityOperator, int worldX, int worldY, int worldZ, BlockType block)
    {
        var entity = entityOperator.CreateEntity();
        Vector3 pos = new Vector3(worldX, worldY, worldZ);

        entityOperator.AddComponent(entity, new CoordComponent
        {
            Position = pos,
            Scale = Vector3.One
        });

        entityOperator.AddComponent(entity, new CubeRendererComponent());
        entityOperator.AddComponent(entity, new EnabledComponent());
        entityOperator.AddComponent(entity, new BlockTypeComponent(block));
        entityOperator.AddComponent(entity, new SurfaceBlockComponent());
        entityOperator.AddComponent(entity, new SceneMemberComponent("Terrain"));

        entityOperator.AddComponent(entity, new StaticPhysicsBodyComponent
        {
            UseLod = true,
            LodDisableDistance = 60.0f
        });

        _surfaceEntities.Add(entity);
        var worldPos = (worldX, worldY, worldZ);
        _blockPositions[entity] = worldPos;
        _entityByPosition[worldPos] = entity;
    }
}
