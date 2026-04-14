using System;
using System.Collections.Generic;
using System.Linq;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;
using MeltEngine.Entities.Components.Interfaces;

namespace MeltEngine.Core
{
    public class ECSOperator
    {
        private int _nextEntityId;
        private readonly Queue<Entity> _availableEntities = new();
        private readonly Dictionary<Type, IComponentArray> _componentArrays = new();
        public int ActiveEntityCount => _nextEntityId - _availableEntities.Count;

        public Entity CreateEntity()
        {
            return _availableEntities.Count != 0 ? _availableEntities.Dequeue() : new Entity { Id = (uint)_nextEntityId++ };
        }

        public virtual void DestroyEntity(Entity entity)
        {
            foreach (var componentArray in _componentArrays.Values)
            {
                componentArray.OnEntityDestroyed(entity);
            }

            _availableEntities.Enqueue(entity);
        }
        
        public virtual ComponentArray<T> GetComponentArray<T>() where T : struct
        {
            var type = typeof(T);

            if (_componentArrays.TryGetValue(type, out var componentArray)) return (ComponentArray<T>)componentArray;
            componentArray = new ComponentArray<T>();
            _componentArrays[type] = componentArray;

            return (ComponentArray<T>)componentArray;
        }

        public virtual void AddComponent<T>(Entity entity, T component) where T : struct
        {
            GetComponentArray<T>().AddComponent(entity, component);
        }

        public virtual void RemoveComponent<T>(Entity entity) where T : struct
        {
            GetComponentArray<T>().RemoveComponent(entity);
        }

        public virtual T GetComponent<T>(Entity entity) where T : struct
        {
            return GetComponentArray<T>().GetComponent(entity);
        }
    }
}