using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using MeltEngine.Utils;
using Raylib_cs;

namespace MeltEngine.Systems
{
    public class RenderSystem(PhysicsManager physicsSystem, ChunkCullerSystem culler) : ISystem
    {
        // One mesh per block type (each with unique atlas UVs)
        private readonly Dictionary<BlockType, Mesh> _blockMeshes = new();
        private Mesh? _cubeMesh = null; // Fallback mesh for physics/bullets
        private Shader _instancingShader;

        // Single shared material with the texture atlas
        private Material _blockMaterial;
        private Texture2D _atlasTexture;
        private bool _atlasLoaded;

        // Fallback materials for non-terrain entities (physics debug, bullets)
        private Material _greenMaterial;
        private Material _blueMaterial;
        private Material _yellowMaterial;

        private readonly BlockRenderBuffers _buffers = new();

        private readonly Process _process = Process.GetCurrentProcess();
        private long _lastWorkingSet = 0;
        private int _memUpdateCounter = 0;
        private const int MEM_UPDATE_INTERVAL = 60;

        private const string VertexShaderCode = @"#version 330
            in vec3 vertexPosition;
            in vec2 vertexTexCoord;
            in vec3 vertexNormal;
            in vec4 vertexColor;
            in mat4 instanceTransform;

            out vec2 fragTexCoord;
            out vec4 fragColor;
            out vec3 fragNormal;

            uniform mat4 matView;
            uniform mat4 matProjection;

            void main()
            {
                fragTexCoord = vertexTexCoord;
                fragColor = vertexColor;
                
                mat3 normalMatrix = mat3(instanceTransform);
                fragNormal = normalize(normalMatrix * vertexNormal);

                mat4 mvpi = matProjection * matView * instanceTransform;
                gl_Position = mvpi * vec4(vertexPosition, 1.0);
            }";

        private const string FragmentShaderCode = @"#version 330
            in vec2 fragTexCoord;
            in vec4 fragColor;
            in vec3 fragNormal;

            out vec4 finalColor;

            uniform sampler2D texture0;
            uniform vec4 colDiffuse;

            void main()
            {
                vec3 lightDir = normalize(vec3(0.8, 1.0, 0.5));
                float NdotL = max(dot(normalize(fragNormal), lightDir), 0.3);

                vec4 texelColor = texture(texture0, fragTexCoord);
                vec3 litColor = texelColor.rgb * colDiffuse.rgb * fragColor.rgb * NdotL;
                
                finalColor = vec4(litColor, texelColor.a * colDiffuse.a * fragColor.a);
            }";

        public unsafe void Update(ECSOperator entityOperator, float alpha)
        {
            var renderSw = Stopwatch.StartNew();

            if (_cubeMesh == null)
            {
                InitializeRenderResources();
            }

            var cameraComponents = entityOperator.GetComponentArray<GameCameraComponent>();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.ColorAlpha(new Color(0, 0, 0), 1f));

            if (cameraComponents.Components.Count > 0)
            {
                var cameraEntity = cameraComponents.DenseEntities[0];
                var cameraComponent = cameraComponents.Components[cameraEntity];
                var mainCamera = cameraComponent.Camera;

                Raylib.BeginMode3D(mainCamera);

                var surfaceBlockComponents = entityOperator.GetComponentArray<SurfaceBlockComponent>();
                var physicsComponents = entityOperator.GetComponentArray<PhysicsBodyComponent>();
                var bulletRenderers = entityOperator.GetComponentArray<BulletRendererComponent>();
                var coordComponents = entityOperator.GetComponentArray<CoordComponent>();
                var enabledComponents = entityOperator.GetComponentArray<EnabledComponent>();
                var blockTypeComponents = entityOperator.GetComponentArray<BlockTypeComponent>();
                var bodyInterface = physicsSystem.BodyInterface;

                float drawDist = cameraComponent.DrawDistance > 0 ? cameraComponent.DrawDistance : 150.0f;
                float maxDrawDistSq = drawDist * drawDist;
                var camPos = mainCamera.Position;
                var camDir = Vector3.Normalize(mainCamera.Target - mainCamera.Position);

                var surfaceCoords = coordComponents.Components;
                var physicsBodies = physicsComponents.Components;
                var blockTypes = blockTypeComponents.Components;
                var visibleChunks = culler.GetVisibleChunks();

                for (int i = 0; i < surfaceBlockComponents.Count; i++)
                {
                    var entity = surfaceBlockComponents.DenseEntities[i];
                    if (!enabledComponents.Components.ContainsKey(entity)) continue;
                    if (!surfaceCoords.TryGetValue(entity, out var cubeCoord)) continue;

                    int blockChunkX = (int)(cubeCoord.Position.X >= 0
                        ? cubeCoord.Position.X / TerrainData.ChunkSizeX
                        : (cubeCoord.Position.X - TerrainData.ChunkSizeX + 1) / TerrainData.ChunkSizeX);
                    int blockChunkZ = (int)(cubeCoord.Position.Z >= 0
                        ? cubeCoord.Position.Z / TerrainData.ChunkSizeZ
                        : (cubeCoord.Position.Z - TerrainData.ChunkSizeZ + 1) / TerrainData.ChunkSizeZ);

                    if (!visibleChunks.Contains((blockChunkX, blockChunkZ))) continue;

                    float distSq = Vector3.DistanceSquared(camPos, cubeCoord.Position);
                    if (distSq > maxDrawDistSq) continue;
                    if (distSq > 100.0f)
                    {
                        Vector3 toCube = Vector3.Normalize(cubeCoord.Position - camPos);
                        float dot = Vector3.Dot(camDir, toCube);
                        if (dot < 0.3f) continue;
                    }

                    Matrix4x4 modelMatrix;
                    if (cubeCoord.IsDirty)
                    {
                        var transform = Matrix4x4.CreateScale(cubeCoord.Scale) *
                                        Matrix4x4.CreateFromQuaternion(cubeCoord.Rotation) *
                                        Matrix4x4.CreateTranslation(cubeCoord.Position);
                        modelMatrix = Matrix4x4.Transpose(transform);
                        cubeCoord.ModelMatrix = modelMatrix;
                        cubeCoord.IsDirty = false;
                        coordComponents.Components[entity] = cubeCoord;
                    }
                    else
                    {
                        modelMatrix = cubeCoord.ModelMatrix;
                    }

                    if (blockTypes.TryGetValue(entity, out var blockType))
                    {
                        switch (blockType.Type)
                        {
                            case BlockType.Grass:
                                _buffers.Grass.Add(modelMatrix);
                                break;
                            case BlockType.Dirt:
                                _buffers.Dirt.Add(modelMatrix);
                                break;
                            case BlockType.Stone:
                                _buffers.Stone.Add(modelMatrix);
                                break;
                            case BlockType.Sand:
                                _buffers.Sand.Add(modelMatrix);
                                break;
                            case BlockType.Water:
                                _buffers.Water.Add(modelMatrix);
                                break;
                            case BlockType.Wood:
                                _buffers.Wood.Add(modelMatrix);
                                break;
                            case BlockType.Leaves:
                                _buffers.Leaves.Add(modelMatrix);
                                break;
                            case BlockType.Brick:
                                _buffers.Brick.Add(modelMatrix);
                                break;
                            case BlockType.Bedrock:
                                _buffers.Bedrock.Add(modelMatrix);
                                break;
                        }
                    }
                }

                for (int i = 0; i < physicsComponents.Count; i++)
                {
                    var entity = physicsComponents.DenseEntities[i];
                    if (!enabledComponents.Components.ContainsKey(entity)) continue;
                    if (!surfaceCoords.TryGetValue(entity, out var cubeCoord)) continue;
                    if (!physicsBodies.TryGetValue(entity, out var physBody)) continue;
                    if (physBody.IsBuried || physBody.IsLodDisabled) continue;

                    float distSq = Vector3.DistanceSquared(camPos, cubeCoord.Position);
                    if (distSq > maxDrawDistSq) continue;

                    Vector3 interpPos = Vector3.Lerp(cubeCoord.PreviousPosition, cubeCoord.Position, Time.Alpha);
                    var transform = Matrix4x4.CreateScale(cubeCoord.Scale) *
                                    Matrix4x4.CreateFromQuaternion(cubeCoord.Rotation) *
                                    Matrix4x4.CreateTranslation(interpPos);
                    Matrix4x4 modelMatrix = Matrix4x4.Transpose(transform);

                    // Not update IsDirty because we are calculating it continuously for interpolation
                    cubeCoord.ModelMatrix = modelMatrix;
                    coordComponents.Components[entity] = cubeCoord;

                    if (!physBody.BodyId.IsInvalid && bodyInterface.IsActive(physBody.BodyId))
                    {
                        _buffers.PhysicsAwake.Add(modelMatrix);
                    }
                    else
                    {
                        _buffers.PhysicsSleep.Add(modelMatrix);
                    }
                }

                for (int i = 0; i < bulletRenderers.Count; i++)
                {
                    var entity = bulletRenderers.DenseEntities[i];

                    if (!enabledComponents.Components.ContainsKey(entity)) continue;
                    if (!coordComponents.Components.TryGetValue(entity, out var bulletCoord)) continue;

                    float distSq = Vector3.DistanceSquared(camPos, bulletCoord.Position);
                    if (distSq > maxDrawDistSq) continue;

                    Vector3 interpPos = Vector3.Lerp(bulletCoord.PreviousPosition, bulletCoord.Position, Time.Alpha);
                    var transform = Matrix4x4.CreateScale(bulletCoord.Scale) *
                                    Matrix4x4.CreateFromQuaternion(bulletCoord.Rotation) *
                                    Matrix4x4.CreateTranslation(interpPos);

                    bulletCoord.ModelMatrix = Matrix4x4.Transpose(transform);
                    coordComponents.Components[entity] = bulletCoord;

                    _buffers.Bullets.Add(bulletCoord.ModelMatrix);
                }

                renderSw.Stop();
                EngineStats.RenderCullingTimeMs = renderSw.Elapsed.TotalMilliseconds;

                // Capture counts BEFORE reset for HUD display
                var counts = _buffers.GetCounts();

                // Draw terrain blocks using textured meshes (one mesh per block type, shared atlas material)
                DrawBlockTypeInstanced(BlockType.Water, _buffers.Water);
                DrawBlockTypeInstanced(BlockType.Grass, _buffers.Grass);
                DrawBlockTypeInstanced(BlockType.Dirt, _buffers.Dirt);
                DrawBlockTypeInstanced(BlockType.Stone, _buffers.Stone);
                DrawBlockTypeInstanced(BlockType.Sand, _buffers.Sand);
                DrawBlockTypeInstanced(BlockType.Wood, _buffers.Wood);
                DrawBlockTypeInstanced(BlockType.Leaves, _buffers.Leaves);
                DrawBlockTypeInstanced(BlockType.Brick, _buffers.Brick);
                DrawBlockTypeInstanced(BlockType.Bedrock, _buffers.Bedrock);

                // Physics and bullet entities use the generic cube mesh
                if (_buffers.PhysicsSleep.Count > 0)
                    Raylib.DrawMeshInstanced(_cubeMesh!.Value, _blueMaterial, _buffers.PhysicsSleep.GetArray(),
                        _buffers.PhysicsSleep.Count);
                if (_buffers.PhysicsAwake.Count > 0)
                    Raylib.DrawMeshInstanced(_cubeMesh!.Value, _greenMaterial, _buffers.PhysicsAwake.GetArray(),
                        _buffers.PhysicsAwake.Count);
                if (_buffers.Bullets.Count > 0)
                    Raylib.DrawMeshInstanced(_cubeMesh!.Value, _yellowMaterial, _buffers.Bullets.GetArray(),
                        _buffers.Bullets.Count);

                _buffers.ResetAll();

                if (BlockInteractionSystem.HasHit && BlockInteractionSystem.HitPosition.HasValue)
                {
                    Raylib.DrawCubeWires(BlockInteractionSystem.HitPosition.Value, 1.01f, 1.01f, 1.01f, Color.Red);
                }
                
                if (BlockInteractionSystem.HasHit && BlockInteractionSystem.PlacePosition.HasValue)
                {
                    Raylib.DrawCubeWires(BlockInteractionSystem.PlacePosition.Value, 1.0f, 1.0f, 1.0f, new Color(0, 255, 0, 255));
                }

                Raylib.DrawGrid(50, 1.0f);
                Raylib.EndMode3D();

                _memUpdateCounter++;
                long ramTotalMb = _lastWorkingSet / (1024 * 1024);
                long gcTotalMb = GC.GetTotalMemory(false) / (1024 * 1024);

                if (_memUpdateCounter >= MEM_UPDATE_INTERVAL)
                {
                    _memUpdateCounter = 0;
                    try
                    {
                        _lastWorkingSet = _process.WorkingSet64;
                    }
                    catch
                    {
                    }
                }

                float frameTimeMs = Raylib.GetFrameTime() * 1000f;
                int totalTerrain = counts.grass + counts.dirt + counts.stone + counts.sand + counts.water
                                 + counts.wood + counts.leaves + counts.brick + counts.bedrock;

                Raylib.DrawRectangle(5, 35, 580, 310, Raylib.ColorAlpha(Color.Black, 0.7f));
                Raylib.DrawText("MELT ENGINE - DEBUG MODE:", 10, 40, 20, Color.Green);
                Raylib.DrawText($"Memory: {ramTotalMb} MB | GC: {gcTotalMb} MB", 10, 65, 18, Color.White);
                Raylib.DrawText($"[Terrain] G={counts.grass} D={counts.dirt} S={counts.stone}",
                    10, 90, 16, new Color(144, 238, 144));
                Raylib.DrawText($"[Terrain] Sand={counts.sand} Water={counts.water}", 10, 108, 16,
                    new Color(173, 216, 230));
                Raylib.DrawText($"[Terrain] Wood={counts.wood} Leaves={counts.leaves} Brick={counts.brick}", 10, 126, 16,
                    new Color(200, 180, 140));
                Raylib.DrawText($"[Total] Visible={totalTerrain} All={coordComponents.Components.Count}", 10, 144, 16,
                    Color.Yellow);
                Raylib.DrawText($"[Physics] Awake={counts.physics} Sleep={counts.physics}",
                    10, 162, 16, Color.Orange);
                Raylib.DrawText($"[FPS] {1.0f / Raylib.GetFrameTime():F0} | Frame: {frameTimeMs:F1}ms", 10, 180, 16,
                    new Color(0, 255, 255));
                Raylib.DrawText(
                    $"[Physics] {EngineStats.PhysicsTimeMs:F2}ms | [Render] {EngineStats.RenderCullingTimeMs:F2}ms", 10,
                    198, 16, Color.LightGray);
                Raylib.DrawText($"[Chunks] Visible={culler.GetVisibleCount()} Cached={culler.GetCachedCount()}", 10,
                    216, 16, Color.SkyBlue);
                Raylib.DrawText($"Cam: {mainCamera.Position:F0}", 10, 234, 16, Color.SkyBlue);
                string atlasStatus = _atlasLoaded ? "ATLAS" : "FLAT";
                Raylib.DrawText($"WASD=Move | Mouse=Look | LClick=Break | RClick=Place | Scroll=Cycle | [{atlasStatus}]", 10, 252, 16, Color.Gray);
                Raylib.DrawText("Typed Buffers | Frustum Culling | Chunk LOD | Texture Atlas", 10, 270, 14, Color.DarkGray);
                Raylib.DrawFPS(10, 10);

                // Minecraft-style UI elements
                DrawCrosshair();
                DrawHotbar();
            }

            Raylib.DrawFPS(10, 10);
            Raylib.EndDrawing();
        }

        /// <summary>
        /// Initializes all render resources: shaders, meshes, materials, and texture atlas.
        /// </summary>
        private unsafe void InitializeRenderResources()
        {
            // Generic fallback cube mesh (used for physics/bullets)
            _cubeMesh = Raylib.GenMeshCube(1.0f, 1.0f, 1.0f);
            _instancingShader = Raylib.LoadShaderFromMemory(VertexShaderCode, FragmentShaderCode);

            int locInstanceTransform = Raylib.GetShaderLocationAttrib(_instancingShader, "instanceTransform");
            _instancingShader.Locs[(int)ShaderLocationIndex.MatrixModel] = locInstanceTransform;

            // Load texture atlas
            string atlasPath = Path.Combine(AppContext.BaseDirectory, "Resources", "blockTex.png");
            _atlasTexture = Raylib.LoadTexture(atlasPath);
            _atlasLoaded = _atlasTexture.Id > 0;

            if (_atlasLoaded)
            {
                Console.WriteLine($"[RenderSystem] Texture atlas loaded: {_atlasTexture.Width}x{_atlasTexture.Height}");

                // Create single shared material with the texture atlas
                _blockMaterial = Raylib.LoadMaterialDefault();
                _blockMaterial.Shader = _instancingShader;
                _blockMaterial.Maps[(int)MaterialMapIndex.Albedo].Texture = _atlasTexture;

                // Generate block meshes with correct UVs for each registered block type
                foreach (byte id in BlockRegistry.GetRegisteredIds())
                {
                    var type = (BlockType)id;
                    _blockMeshes[type] = BlockMeshGenerator.GenerateBlockMesh(type);
                }

                Console.WriteLine($"[RenderSystem] Generated {_blockMeshes.Count} textured block meshes.");
            }
            else
            {
                Console.WriteLine("[RenderSystem] WARNING: Atlas texture not found, using fallback flat colors.");
                InitializeFallbackMaterials();
            }

            // Non-terrain materials (physics debug, bullets)
            _greenMaterial = Raylib.LoadMaterialDefault();
            _blueMaterial = Raylib.LoadMaterialDefault();
            _yellowMaterial = Raylib.LoadMaterialDefault();

            _greenMaterial.Shader = _instancingShader;
            _blueMaterial.Shader = _instancingShader;
            _yellowMaterial.Shader = _instancingShader;

            _greenMaterial.Maps[(int)MaterialMapIndex.Albedo].Color = Color.Green;
            _blueMaterial.Maps[(int)MaterialMapIndex.Albedo].Color = Color.Blue;
            _yellowMaterial.Maps[(int)MaterialMapIndex.Albedo].Color = Color.Gold;
        }

        /// <summary>
        /// Fallback: create flat-color materials if the texture atlas is not available.
        /// </summary>
        private unsafe void InitializeFallbackMaterials()
        {
            _blockMaterial = Raylib.LoadMaterialDefault();
            _blockMaterial.Shader = _instancingShader;

            // Create a fallback material per type using the generic mesh
            var fallbackColors = new Dictionary<BlockType, Color>
            {
                { BlockType.Grass, new Color(76, 178, 51) },
                { BlockType.Dirt, new Color(140, 89, 51) },
                { BlockType.Stone, new Color(128, 128, 128) },
                { BlockType.Sand, new Color(217, 191, 128) },
                { BlockType.Water, new Color(51, 102, 204) },
                { BlockType.Wood, new Color(102, 76, 38) },
                { BlockType.Leaves, new Color(51, 153, 38) },
                { BlockType.Brick, new Color(178, 76, 64) },
                { BlockType.Bedrock, new Color(51, 51, 51) },
            };

            foreach (var (type, _) in fallbackColors)
            {
                // Reuse the generic cube for all types when atlas is missing
                _blockMeshes[type] = _cubeMesh!.Value;
            }

            _fallbackMaterials = new Dictionary<BlockType, Material>();
            foreach (var (type, color) in fallbackColors)
            {
                var mat = Raylib.LoadMaterialDefault();
                mat.Shader = _instancingShader;
                mat.Maps[(int)MaterialMapIndex.Albedo].Color = color;
                _fallbackMaterials[type] = mat;
            }
        }

        private Dictionary<BlockType, Material>? _fallbackMaterials;

        /// <summary>
        /// Draws instanced blocks for a specific block type using the correct textured mesh.
        /// </summary>
        private unsafe void DrawBlockTypeInstanced(BlockType type, TypedRenderBuffer<Matrix4x4> buffer)
        {
            if (buffer.Count == 0) return;

            if (_blockMeshes.TryGetValue(type, out var mesh))
            {
                // Choose material: atlas material if loaded, fallback material otherwise
                var material = _atlasLoaded ? _blockMaterial :
                    (_fallbackMaterials != null && _fallbackMaterials.TryGetValue(type, out var fb) ? fb : _blockMaterial);

                Raylib.DrawMeshInstanced(mesh, material, buffer.GetArray(), buffer.Count);
            }
            else
            {
                // Type not in registry: use generic cube with atlas material (top-left tile)
                Raylib.DrawMeshInstanced(_cubeMesh!.Value, _blockMaterial, buffer.GetArray(), buffer.Count);
            }
        }

        /// <summary>
        /// Draws a simple crosshair at the center of the screen (Minecraft-style).
        /// </summary>
        private void DrawCrosshair()
        {
            int cx = Raylib.GetScreenWidth() / 2;
            int cy = Raylib.GetScreenHeight() / 2;
            int size = 12;
            int thickness = 2;

            // White cross with dark outline for visibility
            Raylib.DrawRectangle(cx - size, cy - thickness / 2, size * 2, thickness, Raylib.ColorAlpha(Color.Black, 0.6f));
            Raylib.DrawRectangle(cx - thickness / 2, cy - size, thickness, size * 2, Raylib.ColorAlpha(Color.Black, 0.6f));
            Raylib.DrawRectangle(cx - size + 1, cy - thickness / 2 + 1, size * 2 - 2, thickness - 1, Color.White);
            Raylib.DrawRectangle(cx - thickness / 2 + 1, cy - size + 1, thickness - 1, size * 2 - 2, Color.White);
        }

        /// <summary>
        /// Draws a Minecraft-style hotbar at the bottom center of the screen.
        /// Shows atlas texture tile previews for each selectable block type.
        /// </summary>
        private void DrawHotbar()
        {
            var blocks = BlockInteractionSystem.SelectableBlocks;
            int selectedSlot = BlockInteractionSystem.Instance?.SelectedSlot ?? 0;
            int slotCount = blocks.Length;

            // Hotbar layout constants
            const int slotSize = 48;
            const int slotGap = 4;
            const int hotbarPadding = 6;

            int hotbarWidth = slotCount * (slotSize + slotGap) - slotGap + hotbarPadding * 2;
            int hotbarHeight = slotSize + hotbarPadding * 2;

            int screenW = Raylib.GetScreenWidth();
            int screenH = Raylib.GetScreenHeight();

            int hotbarX = (screenW - hotbarWidth) / 2;
            int hotbarY = screenH - hotbarHeight - 12;

            // Hotbar background
            Raylib.DrawRectangleRounded(
                new Rectangle(hotbarX - 2, hotbarY - 2, hotbarWidth + 4, hotbarHeight + 4),
                0.15f, 4, Raylib.ColorAlpha(Color.Black, 0.75f));
            Raylib.DrawRectangleRoundedLines(
                new Rectangle(hotbarX - 2, hotbarY - 2, hotbarWidth + 4, hotbarHeight + 4),
                0.15f, 4, Raylib.ColorAlpha(Color.White, 0.2f));

            // Atlas tile dimensions
            float tileW = _atlasLoaded ? (float)_atlasTexture.Width / BlockMeshGenerator.AtlasCols : 0;
            float tileH = _atlasLoaded ? (float)_atlasTexture.Height / BlockMeshGenerator.AtlasRows : 0;

            for (int i = 0; i < slotCount; i++)
            {
                int slotX = hotbarX + hotbarPadding + i * (slotSize + slotGap);
                int slotY = hotbarY + hotbarPadding;
                bool isSelected = i == selectedSlot;

                // Slot background
                Color slotBg = isSelected
                    ? Raylib.ColorAlpha(Color.White, 0.25f)
                    : Raylib.ColorAlpha(Color.Gray, 0.15f);
                Raylib.DrawRectangle(slotX, slotY, slotSize, slotSize, slotBg);

                // Slot border
                Color borderColor = isSelected
                    ? Color.White
                    : Raylib.ColorAlpha(Color.Gray, 0.4f);
                int borderW = isSelected ? 3 : 1;
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(slotX, slotY, slotSize, slotSize),
                    borderW, borderColor);

                // Draw block preview (atlas tile or fallback color)
                int previewPadding = 6;
                int previewSize = slotSize - previewPadding * 2;

                if (_atlasLoaded && BlockRegistry.HasDefinition(blocks[i].type))
                {
                    // Use the top face (index 2) as the preview tile
                    int[] faceIndices = BlockRegistry.GetFaceIndices(blocks[i].type);
                    int topFaceIndex = faceIndices[2]; // Top face

                    int col = topFaceIndex % BlockMeshGenerator.AtlasCols;
                    int row = topFaceIndex / BlockMeshGenerator.AtlasCols;

                    Rectangle srcRect = new Rectangle(col * tileW, row * tileH, tileW, tileH);
                    Rectangle dstRect = new Rectangle(
                        slotX + previewPadding, slotY + previewPadding,
                        previewSize, previewSize);

                    Raylib.DrawTexturePro(_atlasTexture, srcRect, dstRect,
                        new Vector2(0, 0), 0f, Color.White);
                }
                else
                {
                    // Fallback: colored rectangle
                    Color preview = GetBlockFallbackColor(blocks[i].type);
                    Raylib.DrawRectangle(slotX + previewPadding, slotY + previewPadding,
                        previewSize, previewSize, preview);
                }

                // Slot number (1-9)
                string numText = (i + 1).ToString();
                Raylib.DrawText(numText, slotX + 3, slotY + 2, 12, 
                    isSelected ? Color.White : Raylib.ColorAlpha(Color.White, 0.5f));
            }

            // Selected block name below hotbar
            string blockName = BlockInteractionSystem.CurrentBlockName;
            int nameWidth = Raylib.MeasureText(blockName, 18);
            int nameX = (screenW - nameWidth) / 2;
            int nameY = hotbarY + hotbarHeight + 6;
            Raylib.DrawText(blockName, nameX + 1, nameY + 1, 18, Raylib.ColorAlpha(Color.Black, 0.6f)); // Shadow
            Raylib.DrawText(blockName, nameX, nameY, 18, Color.White);
        }

        /// <summary>
        /// Returns a fallback color for block types when atlas is not available.
        /// </summary>
        private static Color GetBlockFallbackColor(BlockType type) => type switch
        {
            BlockType.Grass => new Color(76, 178, 51, 255),
            BlockType.Dirt => new Color(140, 89, 51, 255),
            BlockType.Stone => new Color(128, 128, 128, 255),
            BlockType.Sand => new Color(217, 191, 128, 255),
            BlockType.Water => new Color(51, 102, 204, 200),
            BlockType.Wood => new Color(102, 76, 38, 255),
            BlockType.Leaves => new Color(51, 153, 38, 200),
            BlockType.Brick => new Color(178, 76, 64, 255),
            BlockType.Bedrock => new Color(51, 51, 51, 255),
            _ => new Color(200, 200, 200, 255),
        };
    }
}
