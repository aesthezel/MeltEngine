using System;
using System.Linq;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;
using Raylib_cs;

namespace MeltEngine.Systems;

public class MovementSystem(PhysicsManager physicsSystem) : ISystem
{
    public void Update(ECSOperator entityOperator, float deltaTime)
    {
        var controllableArray = entityOperator.GetComponentArray<PlayerControllableComponent>();
        var physicsArray = entityOperator.GetComponentArray<PhysicsBodyComponent>();
        var enabledArray = entityOperator.GetComponentArray<EnabledComponent>();
        var cameraArray = entityOperator.GetComponentArray<GameCameraComponent>();

        GameCameraComponent? activeCamera = cameraArray.Components.Values.Cast<GameCameraComponent?>().FirstOrDefault();

        foreach (var (entity, controllable) in controllableArray.Components)
        {
            var currentControllable = controllable;

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

            if (physicsBody.BodyId.IsInvalid) continue;

            Vector3 moveDir = Vector3.Zero;
            float currentSpeed = currentControllable.Speed;

            if (currentControllable.IsGodMode)
            {
                currentSpeed *= 5.0f;

                if (activeCamera.HasValue)
                {
                    var cam = activeCamera.Value.Camera;
                    Vector3 forward = Vector3.Normalize(cam.Target - cam.Position);
                    // Proyectar en plano horizontal para WASD; movimiento vertical con Space/Shift
                    Vector3 flatForward = Vector3.Normalize(new Vector3(forward.X, 0, forward.Z));
                    Vector3 flatRight = Vector3.Normalize(Vector3.Cross(flatForward, Vector3.UnitY));

                    if (Raylib.IsKeyDown(KeyboardKey.W)) moveDir += flatForward;
                    if (Raylib.IsKeyDown(KeyboardKey.S)) moveDir -= flatForward;
                    if (Raylib.IsKeyDown(KeyboardKey.D)) moveDir += flatRight;
                    if (Raylib.IsKeyDown(KeyboardKey.A)) moveDir -= flatRight;

                    if (Raylib.IsKeyDown(KeyboardKey.Space)) moveDir += Vector3.UnitY;
                    if (Raylib.IsKeyDown(KeyboardKey.LeftShift)) moveDir -= Vector3.UnitY;
                }
            }
            else
            {
                if (activeCamera.HasValue)
                {
                    var cam = activeCamera.Value.Camera;
                    Vector3 forward = Vector3.Normalize(cam.Target - cam.Position);
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
                physicsSystem.BodyInterface.SetLinearVelocity(physicsBody.BodyId, moveDir);
            }
            else if (controllable.IsGodMode)
            {
                physicsSystem.BodyInterface.SetLinearVelocity(physicsBody.BodyId, Vector3.Zero);
            }
        }
    }
}
