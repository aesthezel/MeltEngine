using System;
using System.Diagnostics;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using Raylib_cs;

namespace MeltEngine.Systems
{
    public class RenderSystem(PhysicsManager physicsSystem) : ISystem
    {
        private Mesh? _cubeMesh = null;
        private Shader _instancingShader;
        private Material _greenMaterial;
        private Material _blueMaterial;
        private Material _redMaterial;
        private Material _yellowMaterial;
        private Matrix4x4[] _awakeTransforms = new Matrix4x4[1024];
        private Matrix4x4[] _sleepTransforms = new Matrix4x4[1024];
private Matrix4x4[] _statTransforms = new Matrix4x4[1024];
        private Matrix4x4[] _bulletTransforms = new Matrix4x4[256];

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
                _cubeMesh = Raylib.GenMeshCube(1.0f, 1.0f, 1.0f);
                _instancingShader = Raylib.LoadShaderFromMemory(VertexShaderCode, FragmentShaderCode);

                int locInstanceTransform = Raylib.GetShaderLocationAttrib(_instancingShader, "instanceTransform");
                _instancingShader.Locs[(int)ShaderLocationIndex.MatrixModel] = locInstanceTransform;

                _greenMaterial = Raylib.LoadMaterialDefault();
                _blueMaterial = Raylib.LoadMaterialDefault();
                _redMaterial = Raylib.LoadMaterialDefault();
                _yellowMaterial = Raylib.LoadMaterialDefault();

                _greenMaterial.Shader = _instancingShader;
                _blueMaterial.Shader = _instancingShader;
                _redMaterial.Shader = _instancingShader;
                _yellowMaterial.Shader = _instancingShader;

                _greenMaterial.Maps[(int)MaterialMapIndex.Albedo].Color = Color.Green;
                _blueMaterial.Maps[(int)MaterialMapIndex.Albedo].Color = Color.Blue;
                _redMaterial.Maps[(int)MaterialMapIndex.Albedo].Color = Color.Red;
                _yellowMaterial.Maps[(int)MaterialMapIndex.Albedo].Color = Color.Gold;
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

                var cubeRenderers = entityOperator.GetComponentArray<CubeRendererComponent>();
                var bulletRenderers = entityOperator.GetComponentArray<BulletRendererComponent>();
                var coordComponents = entityOperator.GetComponentArray<CoordComponent>();
                var enabledComponents = entityOperator.GetComponentArray<EnabledComponent>();
                var physicsComponents = entityOperator.GetComponentArray<PhysicsBodyComponent>();
                var bodyInterface = physicsSystem.BodyInterface;

                int awakeCount = 0;
                int sleepCount = 0;
                int statCount = 0;

                float drawDist = cameraComponent.DrawDistance > 0 ? cameraComponent.DrawDistance : 120.0f;
                float maxDrawDistSq = drawDist * drawDist;
                var camPos = mainCamera.Position;
                var camDir = Vector3.Normalize(mainCamera.Target - mainCamera.Position);

                for (int i = 0; i < cubeRenderers.Count; i++)
                {
                    var entity = cubeRenderers.DenseEntities[i];

                    if (!enabledComponents.Components.ContainsKey(entity)) continue;
                    if (!coordComponents.Components.TryGetValue(entity, out var cubeCoord)) continue;

                    float distSq = Vector3.DistanceSquared(camPos, cubeCoord.Position);

                    if (cubeCoord.Scale.X < 50.0f)
                    {
                        if (distSq > maxDrawDistSq)
                            continue;

                        if (distSq > 25.0f)
                        {
                            Vector3 toCube = Vector3.Normalize(cubeCoord.Position - camPos);
                            float dot = Vector3.Dot(camDir, toCube);
                            if (dot < 0.2f) continue;
                        }
                    }

                    if (cubeCoord.IsDirty)
                    {
                        var transform = Matrix4x4.CreateScale(cubeCoord.Scale) *
                                        Matrix4x4.CreateFromQuaternion(cubeCoord.Rotation) *
                                        Matrix4x4.CreateTranslation(cubeCoord.Position);

                        cubeCoord.ModelMatrix = Matrix4x4.Transpose(transform);
                        cubeCoord.IsDirty = false;
                        coordComponents.Components[entity] = cubeCoord;
                    }

                    if (physicsComponents.Components.TryGetValue(entity, out var physBody))
                    {
                        if (physBody.IsBuried || physBody.IsLodDisabled) continue;

                        if (!physBody.BodyId.IsInvalid && bodyInterface.IsActive(physBody.BodyId))
                        {
                            if (awakeCount >= _awakeTransforms.Length)
                                Array.Resize(ref _awakeTransforms, _awakeTransforms.Length * 2);
                            _awakeTransforms[awakeCount++] = cubeCoord.ModelMatrix;
                        }
                        else
                        {
                            if (sleepCount >= _sleepTransforms.Length)
                                Array.Resize(ref _sleepTransforms, _sleepTransforms.Length * 2);
                            _sleepTransforms[sleepCount++] = cubeCoord.ModelMatrix;
                        }
                    }
                    else
                    {
                        if (statCount >= _statTransforms.Length)
                            Array.Resize(ref _statTransforms, _statTransforms.Length * 2);
                        _statTransforms[statCount++] = cubeCoord.ModelMatrix;
                    }
                }

                int bulletCount = 0;
                for (int i = 0; i < bulletRenderers.Count; i++)
                {
                    var entity = bulletRenderers.DenseEntities[i];

                    if (!enabledComponents.Components.ContainsKey(entity)) continue;
                    if (!coordComponents.Components.TryGetValue(entity, out var bulletCoord)) continue;

                    float distSq = Vector3.DistanceSquared(camPos, bulletCoord.Position);
                    if (distSq > maxDrawDistSq) continue;

                    if (bulletCoord.IsDirty)
                    {
                        var transform = Matrix4x4.CreateScale(bulletCoord.Scale) *
                                        Matrix4x4.CreateFromQuaternion(bulletCoord.Rotation) *
                                        Matrix4x4.CreateTranslation(bulletCoord.Position);

                        bulletCoord.ModelMatrix = Matrix4x4.Transpose(transform);
                        bulletCoord.IsDirty = false;
                        coordComponents.Components[entity] = bulletCoord;
                    }

                    if (bulletCount >= _bulletTransforms.Length)
                        Array.Resize(ref _bulletTransforms, _bulletTransforms.Length * 2);
                    _bulletTransforms[bulletCount++] = bulletCoord.ModelMatrix;
                }

                renderSw.Stop();
                EngineStats.RenderCullingTimeMs = renderSw.Elapsed.TotalMilliseconds;

                if (awakeCount > 0)
                    Raylib.DrawMeshInstanced(_cubeMesh.Value, _greenMaterial, _awakeTransforms, awakeCount);

                if (sleepCount > 0)
                    Raylib.DrawMeshInstanced(_cubeMesh.Value, _blueMaterial, _sleepTransforms, sleepCount);

                if (statCount > 0)
                    Raylib.DrawMeshInstanced(_cubeMesh.Value, _redMaterial, _statTransforms, statCount);

                if (bulletCount > 0)
                    Raylib.DrawMeshInstanced(_cubeMesh.Value, _yellowMaterial, _bulletTransforms, bulletCount);

                Raylib.DrawGrid(50, 1.0f);
                Raylib.EndMode3D();

                _memUpdateCounter++;
                long ramTotalMb = _lastWorkingSet / (1024 * 1024);
                long gcTotalMb = GC.GetTotalMemory(false) / (1024 * 1024);

                if (_memUpdateCounter >= MEM_UPDATE_INTERVAL)
                {
                    _memUpdateCounter = 0;
                    try { _lastWorkingSet = _process.WorkingSet64; } catch { }
                }

                float frameTimeMs = Raylib.GetFrameTime() * 1000f;

                Raylib.DrawRectangle(5, 35, 520, 230, Raylib.ColorAlpha(Color.Black, 0.6f));
                Raylib.DrawText("MELT ENGINE DEBUG MODE (JOLT):", 10, 40, 20, Color.Green);
                Raylib.DrawText($"Memory: {ramTotalMb} MB Process | {gcTotalMb} MB GC", 10, 65, 20, Color.White);
                Raylib.DrawText($"[Entities] Rendered: {awakeCount + sleepCount + statCount} / Instanced: {coordComponents.Components.Count}", 10, 95, 20, Color.Yellow);
                Raylib.DrawText($"[CPU CPU] Frame Time: {frameTimeMs:F2} ms", 10, 125, 20, Color.LightGray);
                Raylib.DrawText($"[CPU PHY] Physics Loop: {EngineStats.PhysicsTimeMs:F2} ms", 10, 150, 20, Color.LightGray);
                Raylib.DrawText($"[CPU RND] Render Culling: {EngineStats.RenderCullingTimeMs:F2} ms", 10, 175, 20, Color.LightGray);
                Raylib.DrawText($"Cam: {mainCamera.Position:F1} -> Tgt {cameraComponent.TargetEntity.Id}", 10, 200, 20, Color.SkyBlue);
                Raylib.DrawText($"Green={awakeCount} (Awake) | Blue={sleepCount} (Sleep) | Red={statCount} (Static)", 10, 225, 20, Color.Gray);
            }

            Raylib.DrawFPS(10, 10);
            Raylib.EndDrawing();
        }
    }
}
