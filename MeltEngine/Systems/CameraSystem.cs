using System;
using System.Linq;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;

namespace MeltEngine.Systems
{
    public class CameraSystem : ISystem
    {
        public void Update(ECSOperator entityOperator, float deltaTime)
        {
            var cameraComponents = entityOperator.GetComponentArray<GameCameraComponent>();
            var coordComponents = entityOperator.GetComponentArray<CoordComponent>();

            if (cameraComponents.Components.Count == 0)
            {
                return;
            }
            
            foreach (var (cameraEntity, cameraComponent) in cameraComponents.Components.ToList())
            {
                if (cameraComponent.TargetEntity.Id == 0) 
                {
                    Console.WriteLine($"ADVERTENCIA: Cámara Entity {cameraEntity.Id} no tiene target válido");
                    continue;
                }
                
                if (!coordComponents.Components.TryGetValue(cameraComponent.TargetEntity, out var targetCoord))
                {
                    Console.WriteLine($"ADVERTENCIA: No se encontró CoordComponent para target Entity {cameraComponent.TargetEntity.Id}");
                    continue;
                }
                
                var updatedCamera = cameraComponent;

                var cameraPosition = targetCoord.Position + updatedCamera.Offset;
                var targetPosition = targetCoord.Position;
                
                updatedCamera.Camera.position = cameraPosition;
                updatedCamera.Camera.target = targetPosition;
                
                cameraComponents.Components[cameraEntity] = updatedCamera;
            }
        }
    }
}