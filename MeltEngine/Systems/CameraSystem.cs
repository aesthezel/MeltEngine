using System.Linq;
using System.Numerics;
using MeltEngine.Core;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;

namespace MeltEngine.Systems
{
    /// <summary>
    /// Este sistema es el ÚNICO responsable de actualizar el estado de la cámara.
    /// Se ejecuta una vez por fotograma, después de la física, para asegurar
    /// que la cámara siga la posición más actualizada de su objetivo.
    /// </summary>
    public class CameraSystem : ISystem
    {
        public void Update(ECSOperator entityOperator, float deltaTime)
        {
            var cameraComponents = entityOperator.GetComponentArray<GameCameraComponent>();
            var coordComponents = entityOperator.GetComponentArray<CoordComponent>();

            // Si no hay ninguna entidad de cámara en la escena, no hacemos nada.
            if (!cameraComponents.Components.Any())
            {
                return;
            }

            // --- Patrón "Leer -> Modificar -> Escribir de Vuelta" ---
            // Este patrón es esencial cuando se modifican componentes que son 'structs'.

            // 1. PASO DE LECTURA: 
            // Obtenemos el ID de la entidad y una COPIA del componente 'GameCameraComponent'.
            // Como 'GameCameraComponent' es un struct, 'cameraComponent' es una copia, no una referencia.
            var cameraEntity = cameraComponents.Components.First().Key;
            var cameraComponent = cameraComponents.Components.First().Value;

            // Buscamos la posición actual del objetivo.
            if (coordComponents.Components.TryGetValue(cameraComponent.TargetEntity, out var targetCoord))
            {
                // 2. PASO DE MODIFICACIÓN: 
                // Actualizamos la estructura 'Camera3D' DENTRO de nuestra copia local del componente.
                cameraComponent.Camera.position = targetCoord.Position + cameraComponent.Offset;
                cameraComponent.Camera.target = targetCoord.Position;

                // 3. PASO DE ESCRITURA DE VUELTA:
                // Sobrescribimos el componente en el ECS con nuestra copia actualizada.
                // Este es el paso más importante: "guarda" el estado de la cámara para el
                // siguiente fotograma y para que el RenderSystem lo pueda usar.
                cameraComponents.AddComponent(cameraEntity, cameraComponent);
            }
        }
    }
}