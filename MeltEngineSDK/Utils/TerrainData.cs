using System;
using System.Collections.Generic;
using System.Numerics;
using MeltEngine.Entities.Components;

namespace MeltEngine.Utils;

public class TerrainData
{
    public const int ChunkSizeX = 16;
    public const int ChunkSizeZ = 16;
    public const int ChunkSizeY = 64;
    public const float BlockSize = 1.0f;
    
    private readonly Dictionary<(int cx, int cz), BlockType[,,]> _chunks = new();
    private readonly NoiseGenerator _heightNoise;
    private readonly NoiseGenerator _caveNoise;
    private readonly NoiseGenerator _detailNoise;
    private readonly NoiseGenerator _waterNoise;
    private readonly int _seed;
    
    private const float HeightScale = 0.02f;
    private const float CaveScale = 0.08f;
    private const float DetailScale = 0.1f;
    private const float WaterScale = 0.015f;
    private const int SeaLevel = 10;
    private const float CaveThreshold = 0.55f;
    private const int BedrockOffset = 2;
    
    private int _minHeight = int.MaxValue;
    private int _maxHeight = int.MinValue;

    public TerrainData(int seed)
    {
        _seed = seed;
        _heightNoise = new NoiseGenerator(seed);
        _caveNoise = new NoiseGenerator(seed + 1);
        _detailNoise = new NoiseGenerator(seed + 2);
        _waterNoise = new NoiseGenerator(seed + 3);
    }

    public int GetHeight(int worldX, int worldZ)
    {
        float height = _heightNoise.OctaveNoise2D(worldX * HeightScale, worldZ * HeightScale, 4, 0.5f);
        height = MathF.Pow(height, 1.2f);
        int h = (int)(height * 30) + SeaLevel + 5;
        _minHeight = Math.Min(_minHeight, h);
        _maxHeight = Math.Max(_maxHeight, h);
        return h;
    }

    public float GetWaterLevel(int worldX, int worldZ)
    {
        return _waterNoise.OctaveNoise2D(worldX * WaterScale, worldZ * WaterScale, 2) * 5 + SeaLevel;
    }

    private BlockType GetBlockType(int localY, int surfaceY, int worldX, int worldZ)
    {
        float heightNoise = _heightNoise.Noise2D(worldX * HeightScale * 2, worldZ * HeightScale * 2);
        bool isBeach = heightNoise > 0.45f && heightNoise < 0.55f && localY < SeaLevel + 3;
        
        if (isBeach) return BlockType.Sand;
        if (localY == 0 || localY < BedrockOffset) return BlockType.Bedrock;
        if (localY < 3) return _detailNoise.Noise3D(worldX * 0.1f, localY * 0.1f, worldZ * 0.1f) > 0.6f 
            ? BlockType.Stone : BlockType.Dirt;

        if (localY < surfaceY - 4) return BlockType.Dirt;
        if (localY < surfaceY) return BlockType.Stone;
        if (localY == surfaceY) return isBeach ? BlockType.Sand : BlockType.Grass;
        
        return BlockType.Air;
    }

    public BlockType GetBlock(int worldX, int worldY, int worldZ)
    {
        int cx = worldX >= 0 ? worldX / ChunkSizeX : (worldX - ChunkSizeX + 1) / ChunkSizeX;
        int cz = worldZ >= 0 ? worldZ / ChunkSizeZ : (worldZ - ChunkSizeZ + 1) / ChunkSizeZ;
        
        if (!_chunks.TryGetValue((cx, cz), out var chunk))
        {
            chunk = GenerateChunk(cx, cz);
        }
        
        int lx = worldX - cx * ChunkSizeX;
        int lz = worldZ - cz * ChunkSizeZ;
        
        if (lx < 0 || lx >= ChunkSizeX || lz < 0 || lz >= ChunkSizeZ || worldY < 0 || worldY >= ChunkSizeY)
            return BlockType.Air;
            
        return chunk[lx, worldY, lz];
    }

    public BlockType[,,] GenerateChunk(int chunkX, int chunkZ)
    {
        var key = (chunkX, chunkZ);
        if (_chunks.TryGetValue(key, out var cached)) return cached;

        var blocks = new BlockType[ChunkSizeX, ChunkSizeY, ChunkSizeZ];
        
        for (int lx = 0; lx < ChunkSizeX; lx++)
        {
            for (int lz = 0; lz < ChunkSizeZ; lz++)
            {
                int worldX = chunkX * ChunkSizeX + lx;
                int worldZ = chunkZ * ChunkSizeZ + lz;
                int surfaceHeight = GetHeight(worldX, worldZ);
                float waterLevel = GetWaterLevel(worldX, worldZ);

                for (int y = 0; y < ChunkSizeY; y++)
                {
                    float cave = _caveNoise.Noise3D(worldX * CaveScale, y * CaveScale, worldZ * CaveScale);
                    bool isCave = y > 5 && y < surfaceHeight - 2 && cave > CaveThreshold;
                    
                    if (isCave)
                    {
                        blocks[lx, y, lz] = BlockType.Air;
                        continue;
                    }

                    if (y > surfaceHeight)
                    {
                        if (y <= waterLevel) blocks[lx, y, lz] = BlockType.Water;
                        else blocks[lx, y, lz] = BlockType.Air;
                    }
                    else
                    {
                        blocks[lx, y, lz] = GetBlockType(y, surfaceHeight, worldX, worldZ);
                    }
                }
            }
        }

        _chunks[key] = blocks;
        return blocks;
    }

    public void EnsureChunkLoaded(int chunkX, int chunkZ)
    {
        GenerateChunk(chunkX, chunkZ);
    }

    public void SetBlock(int worldX, int worldY, int worldZ, BlockType type)
    {
        int cx = worldX >= 0 ? worldX / ChunkSizeX : (worldX - ChunkSizeX + 1) / ChunkSizeX;
        int cz = worldZ >= 0 ? worldZ / ChunkSizeZ : (worldZ - ChunkSizeZ + 1) / ChunkSizeZ;
        
        // Ensure chunk exists before modifying — generate it if needed
        if (!_chunks.TryGetValue((cx, cz), out var chunk))
        {
            chunk = GenerateChunk(cx, cz);
        }

        int lx = worldX - cx * ChunkSizeX;
        int lz = worldZ - cz * ChunkSizeZ;
        
        if (lx < 0 || lx >= ChunkSizeX || lz < 0 || lz >= ChunkSizeZ || worldY < 0 || worldY >= ChunkSizeY) return;

        chunk[lx, worldY, lz] = type;
    }

    public static (int cx, int cz) WorldToChunk(int worldX, int worldZ)
    {
        int cx = worldX >= 0 ? worldX / ChunkSizeX : (worldX - ChunkSizeX + 1) / ChunkSizeX;
        int cz = worldZ >= 0 ? worldZ / ChunkSizeZ : (worldZ - ChunkSizeZ + 1) / ChunkSizeZ;
        return (cx, cz);
    }

    public int GetMinHeight() => _minHeight;
    public int GetMaxHeight() => _maxHeight;
    
    public int EstimatedBlockCount(int worldX, int worldZ, int radius)
    {
        int count = 0;
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dz = -radius; dz <= radius; dz++)
            {
                int h = GetHeight(worldX + dx, worldZ + dz);
                count += h;
            }
        }
        return count;
    }
}
