using System;
using System.Linq;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using Raylib_cs;

namespace MeltEngine.Systems
{
    public class CameraSystem : ISystem
    {
        private const float MouseSensitivity = 0.05f;
        private const float MaxMouseDeltaPerFrame = 50.0f;
        private bool _skipNextMouseDelta = false;

        public void Update(ECSOperator entityOperator, float deltaTime)
        {
            var cameraComponents = entityOperator.GetComponentArray<GameCameraComponent>();
            var coordComponents = entityOperator.GetComponentArray<CoordComponent>();

            if (cameraComponents.Components.Count == 0) return;

            foreach (var kvp in cameraComponents.Components)
            {
                var cameraEntity = kvp.Key;
                var cameraComponent = kvp.Value;

                if (cameraComponent.TargetEntity.Id == 0) continue;

                if (!coordComponents.Components.TryGetValue(cameraComponent.TargetEntity, out var targetCoord))
                {
                    continue;
                }

                var updatedCamera = cameraComponent;
                var targetPosition = targetCoord.Position;

                if (updatedCamera.IsOrbitMode)
                {
                    // Capturar ratón si no está ya deshabilitado; saltar delta del primer frame
                    if (!Raylib.IsCursorHidden())
                    {
                        Raylib.DisableCursor();
                        _skipNextMouseDelta = true;
                    }

                    // Leer entrada de ratón (ignorar primer frame para evitar salto brusco)
                    var mouseDelta = Raylib.GetMouseDelta();
                    if (_skipNextMouseDelta)
                    {
                        mouseDelta = System.Numerics.Vector2.Zero;
                        _skipNextMouseDelta = false;
                    }

                    // Limitar delta máximo por frame para evitar saltos bruscos
                    mouseDelta.X = Math.Clamp(mouseDelta.X, -MaxMouseDeltaPerFrame, MaxMouseDeltaPerFrame);
                    mouseDelta.Y = Math.Clamp(mouseDelta.Y, -MaxMouseDeltaPerFrame, MaxMouseDeltaPerFrame);

                    updatedCamera.Yaw -= mouseDelta.X * MouseSensitivity;
                    updatedCamera.Pitch -= mouseDelta.Y * MouseSensitivity;

                    // Limitar Pitch para evitar dar la vuelta completa
                    updatedCamera.Pitch = Math.Clamp(updatedCamera.Pitch, -89.0f, 89.0f);

                    // Zoom con la rueda del ratón
                    float wheel = Raylib.GetMouseWheelMove();
                    updatedCamera.Distance -= wheel * 1.5f;
                    updatedCamera.Distance = Math.Max(updatedCamera.Distance, 2.0f);

                    // Calcular nueva posición Orbitada
                    float yawRad = updatedCamera.Yaw * (MathF.PI / 180.0f);
                    float pitchRad = updatedCamera.Pitch * (MathF.PI / 180.0f);

                    Vector3 posOffset;
                    posOffset.X = updatedCamera.Distance * MathF.Cos(pitchRad) * MathF.Sin(yawRad);
                    posOffset.Y = updatedCamera.Distance * MathF.Sin(pitchRad);
                    posOffset.Z = updatedCamera.Distance * MathF.Cos(pitchRad) * MathF.Cos(yawRad);

                    updatedCamera.Camera.Position = targetPosition + posOffset;
                    updatedCamera.Camera.Target = targetPosition;
                }
                else
                {
                    // Modo Offset Fijo (Original)
                    if (Raylib.IsCursorHidden()) Raylib.EnableCursor();

                    updatedCamera.Camera.Position = targetPosition + updatedCamera.Offset;
                    updatedCamera.Camera.Target = targetPosition;
                }

                cameraComponents.Components[cameraEntity] = updatedCamera;
            }
        }
    }
}