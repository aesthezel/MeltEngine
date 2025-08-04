using System.Linq;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using Raylib_cs;

namespace MeltEngine.Systems
{
    public class RenderSystem : ISystem
    {
        public void Update(ECSOperator entityOperator, float alpha)
        {
            var cameraComponents = entityOperator.GetComponentArray<GameCameraComponent>();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.ColorAlpha(new Color(0, 0, 0), 1f));

            if (cameraComponents.Components.Count > 0)
            {
                var cameraEntity = cameraComponents.Components.First().Key;
                var cameraComponent = cameraComponents.Components[cameraEntity];
                var mainCamera = cameraComponent.Camera;
                
                Raylib.DrawText($"Cam Pos: {mainCamera.Position:F1}", 10, 50, 20, Raylib.ColorAlpha(new Color(255, 255, 255), 1f));
                Raylib.DrawText($"Cam Target: {mainCamera.Target:F1}", 10, 75, 20, Raylib.ColorAlpha(new Color(255, 255, 255), 1f));
                Raylib.DrawText($"Target Entity: {cameraComponent.TargetEntity.Id}", 10, 100, 20, Raylib.ColorAlpha(new Color(255, 255, 255), 1f));
            
                Raylib.BeginMode3D(mainCamera);

                var cubeRenderers = entityOperator.GetComponentArray<CubeRendererComponent>();
                var coordComponents = entityOperator.GetComponentArray<CoordComponent>();
                var enabledComponents = entityOperator.GetComponentArray<EnabledComponent>();
                var physicsComponents = entityOperator.GetComponentArray<PhysicsBodyComponent>();

                foreach (var (entity, _) in cubeRenderers.Components)
                {
                    if (!enabledComponents.Components.ContainsKey(entity) ||
                        !coordComponents.Components.TryGetValue(entity, out var cubeCoord)) continue;

                    var renderPosition = physicsComponents.Components.ContainsKey(entity) ? Vector3.Lerp(cubeCoord.PreviousPosition, cubeCoord.Position, alpha) : cubeCoord.Position;
                    Raylib.DrawText($"Player Pos: {renderPosition:F1}", 10, 200, 20, Raylib.ColorAlpha(new Color(255, 255, 255), 1f));
                    
                    var color = physicsComponents.Components.ContainsKey(entity) ? Raylib.ColorAlpha(new Color(255, 0, 0), 1f) : Raylib.ColorAlpha(new Color(0, 0, 255), 1f);
                    Raylib.DrawCube(renderPosition, cubeCoord.Scale.X, cubeCoord.Scale.Y, cubeCoord.Scale.Z, color);
                }
            
                Raylib.DrawGrid(50, 1.0f);
                Raylib.EndMode3D();
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