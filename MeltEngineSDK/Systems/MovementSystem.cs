using System;
using System.Linq;
using System.Numerics;
using MagicPhysX;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using Raylib_cs;

namespace MeltEngine.Systems;

public class MovementSystem : ISystem
{
    public void Update(ECSOperator entityOperator, float deltaTime)
    {
        var controllableArray = entityOperator.GetComponentArray<PlayerControllableComponent>();
        var physicsArray = entityOperator.GetComponentArray<PhysicsBodyComponent>();
        var enabledArray = entityOperator.GetComponentArray<EnabledComponent>();
        var cameraArray = entityOperator.GetComponentArray<GameCameraComponent>();

        // Obtener la cámara activa para movimiento relativo
        GameCameraComponent? activeCamera = cameraArray.Components.Values.Cast<GameCameraComponent?>().FirstOrDefault();

        foreach (var (entity, controllable) in controllableArray.Components)
        {
            var currentControllable = controllable;

            // Alternar modo dios con la tecla G
            if (Raylib.IsKeyPressed(KeyboardKey.G))
            {
                currentControllable.IsGodMode = !currentControllable.IsGodMode;
                controllableArray.Components[entity] = currentControllable;
                Console.WriteLine($"Modo Dios: {currentControllable.IsGodMode}");
            }

            if (!enabledArray.Components.ContainsKey(entity) ||
                !physicsArray.Components.TryGetValue(entity, out var physicsBody))
            {
                continue;
            }

            Vector3 moveDir = Vector3.Zero;
            float currentSpeed = currentControllable.Speed;

            if (currentControllable.IsGodMode)
            {
                currentSpeed *= 5.0f; // Más rápido en modo dios

                if (activeCamera.HasValue)
                {
                    var cam = activeCamera.Value.Camera;
                    Vector3 forward = Vector3.Normalize(cam.Target - cam.Position);
                    Vector3 right = Vector3.Normalize(Vector3.Cross(forward, cam.Up));

                    if (Raylib.IsKeyDown(KeyboardKey.W)) moveDir += forward;
                    if (Raylib.IsKeyDown(KeyboardKey.S)) moveDir -= forward;
                    if (Raylib.IsKeyDown(KeyboardKey.D)) moveDir += right;
                    if (Raylib.IsKeyDown(KeyboardKey.A)) moveDir -= right;

                    if (Raylib.IsKeyDown(KeyboardKey.Space)) moveDir += cam.Up;
                    if (Raylib.IsKeyDown(KeyboardKey.LeftShift)) moveDir -= cam.Up;
                }
            }
            else
            {
                // Movimiento normal relativo a la cámara (sobre el plano horizontal)
                if (activeCamera.HasValue)
                {
                    var cam = activeCamera.Value.Camera;
                    Vector3 forward = Vector3.Normalize(cam.Target - cam.Position);

                    // Proyectar sobre el plano horizontal para que no vuele al mirar hacia arriba/abajo
                    Vector3 flatForward = Vector3.Normalize(new Vector3(forward.X, 0, forward.Z));
                    Vector3 flatRight = Vector3.Normalize(Vector3.Cross(flatForward, Vector3.UnitY));

                    if (Raylib.IsKeyDown(KeyboardKey.W)) moveDir += flatForward;
                    if (Raylib.IsKeyDown(KeyboardKey.S)) moveDir -= flatForward;
                    if (Raylib.IsKeyDown(KeyboardKey.D)) moveDir += flatRight;
                    if (Raylib.IsKeyDown(KeyboardKey.A)) moveDir -= flatRight;

                    if (Raylib.IsKeyDown(KeyboardKey.Space)) moveDir.Y += 2;
                }
                else
                {
                    // Fallback si no hay cámara
                    if (Raylib.IsKeyDown(KeyboardKey.W)) moveDir.Z += 1;
                    if (Raylib.IsKeyDown(KeyboardKey.S)) moveDir.Z -= 1;
                    if (Raylib.IsKeyDown(KeyboardKey.A)) moveDir.X += 1;
                    if (Raylib.IsKeyDown(KeyboardKey.D)) moveDir.X -= 1;
                    if (Raylib.IsKeyDown(KeyboardKey.Space)) moveDir.Y += 2;
                }
            }

            if (moveDir != Vector3.Zero)
            {
                moveDir = Vector3.Normalize(moveDir) * currentSpeed;
                unsafe
                {
                    var pxForce = new PxVec3 { x = moveDir.X, y = moveDir.Y, z = moveDir.Z };
                    NativeMethods.PxRigidDynamic_setLinearVelocity_mut(physicsBody.Actor, &pxForce, true);
                }
            }
            else if (controllable.IsGodMode)
            {
                // En modo dios, si no hay input, frenar en seco para vuelo preciso
                unsafe
                {
                    var zero = new PxVec3 { x = 0, y = 0, z = 0 };
                    NativeMethods.PxRigidDynamic_setLinearVelocity_mut(physicsBody.Actor, &zero, true);
                }
            }
        }
    }
}