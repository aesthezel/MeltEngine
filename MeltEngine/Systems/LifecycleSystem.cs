using System.Collections.Generic;
using MeltEngine.Core;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;
using MeltEngine.Systems.Interfaces;

namespace MeltEngine.Systems;

public class LifecycleSystem : ISystem
{
    public void Update(ECSOperator entityOperator, float deltaTime)
    {
        var destroyedArray = entityOperator.GetComponentArray<DestroyedComponent>();
        
        var entitiesToDestroy = new List<Entity>(destroyedArray.Components.Keys);

        foreach (var entity in entitiesToDestroy)
        {
            entityOperator.DestroyEntity(entity);
        }
    }
}