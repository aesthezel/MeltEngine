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

                if (moveDir != Vector3.Zero)
                {
                    moveDir = Vector3.Normalize(moveDir) * currentSpeed;
                    physicsSystem.BodyInterface.SetLinearVelocity(physicsBody.BodyId, moveDir);
                }
                else
                {
                    physicsSystem.BodyInterface.SetLinearVelocity(physicsBody.BodyId, Vector3.Zero);
                }
            }
            else
            {
                bool jumpPressed = false;
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

                    if (Raylib.IsKeyPressed(KeyboardKey.Space)) jumpPressed = true;
                }
                else
                {
                    if (Raylib.IsKeyDown(KeyboardKey.W)) moveDir.Z += 1;
                    if (Raylib.IsKeyDown(KeyboardKey.S)) moveDir.Z -= 1;
                    if (Raylib.IsKeyDown(KeyboardKey.A)) moveDir.X += 1;
                    if (Raylib.IsKeyDown(KeyboardKey.D)) moveDir.X -= 1;
                    
                    if (Raylib.IsKeyPressed(KeyboardKey.Space)) jumpPressed = true;
                }

                Vector3 currentVel = physicsSystem.BodyInterface.GetLinearVelocity(physicsBody.BodyId);
                Vector3 targetVel = new Vector3(currentVel.X, currentVel.Y, currentVel.Z);

                if (moveDir != Vector3.Zero)
                {
                    moveDir = Vector3.Normalize(moveDir) * currentSpeed;
                    targetVel.X = moveDir.X;
                    targetVel.Z = moveDir.Z;
                }
                else
                {
                    // Fricción horizontal si no se presionan teclas
                    targetVel.X = 0;
                    targetVel.Z = 0;
                }

                if (jumpPressed)
                {
                    targetVel.Y = 8.0f; // Impulso de salto
                }

                physicsSystem.BodyInterface.SetLinearVelocity(physicsBody.BodyId, targetVel);
            }
        }
    }
}
