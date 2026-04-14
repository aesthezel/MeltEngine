namespace MeltEngine.Entities.Components.Interfaces;

public interface IComponentArray
{
    void RemoveComponent(Entity entity);
    public void OnEntityDestroyed(Entity entity);
}