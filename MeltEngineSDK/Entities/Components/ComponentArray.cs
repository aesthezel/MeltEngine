using System.Collections.Generic;
using MeltEngine.Entities.Components.Interfaces;

namespace MeltEngine.Entities.Components;

public class ComponentArray<T> : IComponentArray
{
    public Dictionary<Entity, T> Components { get; } = new();
    
    // Matriz Densa para Iteraciones en Paralelo O(1)
    public Entity[] DenseEntities = new Entity[256];
    public int Count = 0;

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
                
            DenseEntities[Count++] = entity;
        }
        Components[entity] = component;
    }

    public void RemoveComponent(Entity entity)
    {
        if (Components.Remove(entity))
        {
            for (int i = 0; i < Count; i++)
            {
                if (DenseEntities[i].Id == entity.Id)
                {
                    // Swap con el último para mantener la matriz densa compacta O(1)
                    DenseEntities[i] = DenseEntities[Count - 1];
                    Count--;
                    break;
                }
            }
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