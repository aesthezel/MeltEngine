using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using Raylib_cs;
using MagicPhysX;
using static MagicPhysX.NativeMethods;

namespace MeltEngine.Systems
{
    public class RenderSystem : ISystem
    {
        private Mesh? _cubeMesh = null;
        private Shader _instancingShader;
        private Material _greenMaterial;
        private Material _blueMaterial;
        private Material _redMaterial; // Usado para estáticos
        private Matrix4x4[] _awakeTransforms = new Matrix4x4[1024];
        private Matrix4x4[] _sleepTransforms = new Matrix4x4[1024];
        private Matrix4x4[] _statTransforms = new Matrix4x4[1024];

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
                
                // Simple normal matrix (works well mostly with uniform scales)
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
                vec3 lightDir = normalize(vec3(0.8, 1.0, 0.5)); // Directional Light
                float NdotL = max(dot(normalize(fragNormal), lightDir), 0.3); // Ambient base 0.3

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

                // Set custom shader locs for instancing
                int locInstanceTransform = Raylib.GetShaderLocationAttrib(_instancingShader, "instanceTransform");
                // Because matrix is 4 vectors, set locations for matrix columns
                _instancingShader.Locs[(int)ShaderLocationIndex.MatrixModel] = locInstanceTransform;

                _greenMaterial = Raylib.LoadMaterialDefault();
                _blueMaterial = Raylib.LoadMaterialDefault();
                _redMaterial = Raylib.LoadMaterialDefault();

                _greenMaterial.Shader = _instancingShader;
                _blueMaterial.Shader = _instancingShader;
                _redMaterial.Shader = _instancingShader;

                // Color mapping: Verde=Despierto, Azul=Dormido, Rojo=Suelo/Estatico
                _greenMaterial.Maps[(int)MaterialMapIndex.Albedo].Color = Color.Green;
                _blueMaterial.Maps[(int)MaterialMapIndex.Albedo].Color = Color.Blue;
                _redMaterial.Maps[(int)MaterialMapIndex.Albedo].Color = Color.Red;
            }

            var cameraComponents = entityOperator.GetComponentArray<GameCameraComponent>();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.ColorAlpha(new Color(0, 0, 0), 1f));

            if (cameraComponents.Components.Count > 0)
            {
                var cameraEntity = cameraComponents.Components.First().Key;
                var cameraComponent = cameraComponents.Components[cameraEntity];
                var mainCamera = cameraComponent.Camera;

                Raylib.BeginMode3D(mainCamera);

                var cubeRenderers = entityOperator.GetComponentArray<CubeRendererComponent>();
                var coordComponents = entityOperator.GetComponentArray<CoordComponent>();
                var enabledComponents = entityOperator.GetComponentArray<EnabledComponent>();
                var physicsComponents = entityOperator.GetComponentArray<PhysicsBodyComponent>();

                int awakeCount = 0;
                int sleepCount = 0;
                int statCount = 0;

                // Distancia maxima para dibujar - Render Culling brutal
                float maxDrawDistSq = 120.0f * 120.0f;
                // Cachear pos de camara
                var camPos = mainCamera.Position;

                // Vector Direccional de la Camara para Frustum Culling
                var camDir = Vector3.Normalize(mainCamera.Target - mainCamera.Position);

                // Nativo for loop sobre matriz densa rompe la ineficiencia de punteros en memoria random
                for (int i = 0; i < cubeRenderers.Count; i++)
                {
                    var entity = cubeRenderers.DenseEntities[i];

                    if (!enabledComponents.Components.ContainsKey(entity)) continue;
                    if (!coordComponents.Components.TryGetValue(entity, out var cubeCoord)) continue;

                    float distSq = Vector3.DistanceSquared(camPos, cubeCoord.Position);

                    // Excluir suelo asumiendo que el entity suelo no es 1x1
                    if (cubeCoord.Scale.X < 50.0f)
                    {
                        // 1. Distance Culling
                        if (distSq > maxDrawDistSq)
                            continue;

                        // 2. Frustum Culling Suave
                        // Excluimos objetos que están detrás de nosotros.
                        // Alivio brutal para la GPU (hasta 70% menos overdraw)
                        if (distSq > 25.0f) // Solo aplicar culling a cubos que no tenemos pegados a la cara
                        {
                            Vector3 toCube = Vector3.Normalize(cubeCoord.Position - camPos);
                            float dot = Vector3.Dot(camDir, toCube);

                            // Un dot de < 0.2 es seguro para FOV estándar, descartará lo que está a los lados pesados o detrás.
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
                        coordComponents.Components[entity] = cubeCoord; // Save cached state
                    }

                    if (physicsComponents.Components.TryGetValue(entity, out var physBody))
                    {
                        if (physBody.Actor != null && !PxRigidDynamic_isSleeping(physBody.Actor))
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

                renderSw.Stop();
                EngineStats.RenderCullingTimeMs = renderSw.Elapsed.TotalMilliseconds;

                awakeCount = Math.Min(awakeCount, _awakeTransforms.Length);
                sleepCount = Math.Min(sleepCount, _sleepTransforms.Length);
                statCount = Math.Min(statCount, _statTransforms.Length);

                // Push to GPU in batched Instanced calls!
                if (awakeCount > 0)
                    Raylib.DrawMeshInstanced(_cubeMesh.Value, _greenMaterial, _awakeTransforms, awakeCount);

                if (sleepCount > 0)
                    Raylib.DrawMeshInstanced(_cubeMesh.Value, _blueMaterial, _sleepTransforms, sleepCount);

                if (statCount > 0)
                    Raylib.DrawMeshInstanced(_cubeMesh.Value, _redMaterial, _statTransforms, statCount);

                Raylib.DrawGrid(50, 1.0f);
                Raylib.EndMode3D();

                var process = Process.GetCurrentProcess();
                long ramTotalMb = process.WorkingSet64 / (1024 * 1024);
                long gcTotalMb = GC.GetTotalMemory(false) / (1024 * 1024);
                float frameTimeMs = Raylib.GetFrameTime() * 1000f;

                Raylib.DrawRectangle(5, 35, 390, 230, Raylib.ColorAlpha(Color.Black, 0.6f));
                Raylib.DrawText("MELT ENGINE DEBUG MODE:", 10, 40, 20, Color.Green);
                Raylib.DrawText($"Memory: {ramTotalMb} MB Process | {gcTotalMb} MB GC", 10, 65, 20, Color.White);

                Raylib.DrawText(
                    $"[Entities] Rendered: {awakeCount + sleepCount + statCount} / Instanced: {coordComponents.Components.Count}",
                    10, 95, 20, Color.Yellow);

                Raylib.DrawText($"[CPU CPU] Frame Time: {frameTimeMs:F2} ms", 10, 125, 20, Color.LightGray);
                Raylib.DrawText($"[CPU PHY] Physics Loop: {EngineStats.PhysicsTimeMs:F2} ms", 10, 150, 20,
                    Color.LightGray);
                Raylib.DrawText($"[CPU RND] Render Culling: {EngineStats.RenderCullingTimeMs:F2} ms", 10, 175, 20,
                    Color.LightGray);

                Raylib.DrawText($"Cam: {mainCamera.Position:F1} -> Tgt {cameraComponent.TargetEntity.Id}", 10, 210, 20,
                    Color.SkyBlue);
                Raylib.DrawText(
                    $"Stats: Green={awakeCount} (Physics Awake) | Blue={sleepCount} (Sleeping) | Red={statCount} (Static)",
                    10, 235, 20, Color.Gray);
            }
            else
            {
                Raylib.DrawText("No active camera", 10, 30, 20, Raylib.ColorAlpha(new Color(255, 255, 255), 1f));
            }

            Raylib.DrawFPS(10, 10);
            Raylib.EndDrawing();
        }
    }
}