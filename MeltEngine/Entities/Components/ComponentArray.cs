using System.Collections.Generic;
using MeltEngine.Entities.Components.Interfaces;

namespace MeltEngine.Entities.Components;

public class ComponentArray<T> : IComponentArray
{
    public Dictionary<Entity, T> Components { get; } = new();

    public void AddComponent(Entity entity, T component) => Components[entity] = component;
    public void RemoveComponent(Entity entity) => Components.Remove(entity);
    public T GetComponent(Entity entity) => Components[entity];
    
    public void OnEntityDestroyed(Entity entity)
    {
        if (Components.ContainsKey(entity))
        {
            RemoveComponent(entity);
        }
    }
}