using System.Collections.Generic;
using MeltEngine.Entities.Components.Interfaces;

namespace MeltEngine.Entities.Components;

public class ComponentArray<T> : IComponentArray
{
    public Dictionary<Entity, T> Components { get; } = new();

    // Matriz Densa para Iteraciones en Paralelo O(1)
    public Entity[] DenseEntities = new Entity[256];
    public int Count = 0;

    // Índice inverso: Entity → posición en DenseEntities para RemoveComponent O(1)
    private readonly Dictionary<Entity, int> _denseIndex = new();

    public void AddComponent(Entity entity, T component)
    {
        if (!Components.ContainsKey(entity))
        {
            if (Count >= DenseEntities.Length)
            {
                // Estrategia de crecimiento dinámico x2 (igual que List<T>)
                int newSize = DenseEntities.Length == 0 ? 256 : DenseEntities.Length * 2;
                System.Array.Resize(ref DenseEntities, newSize);
            }

            _denseIndex[entity] = Count;
            DenseEntities[Count++] = entity;
        }
        Components[entity] = component;
    }

    public void RemoveComponent(Entity entity)
    {
        if (Components.Remove(entity) && _denseIndex.TryGetValue(entity, out int idx))
        {
            _denseIndex.Remove(entity);
            int last = Count - 1;
            if (idx != last)
            {
                // Swap con el último y actualizar su índice
                Entity movedEntity = DenseEntities[last];
                DenseEntities[idx] = movedEntity;
                _denseIndex[movedEntity] = idx;
            }
            Count--;
        }
    }

    public T GetComponent(Entity entity) => Components[entity];
    
    public void OnEntityDestroyed(Entity entity)
    {
        if (Components.ContainsKey(entity))
        {
            RemoveComponent(entity);
        }
    }
}