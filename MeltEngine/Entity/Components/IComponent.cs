using System;

namespace MeltEngine.Entity.Components;

public interface IComponent
{
    GameObject GameObject { get; }
    
    Action OnStart { get; }
    Action OnUpdate { get; }
    Action OnRender { get; }
    Action OnEndRender { get; }
    Action OnShow { get; }
    Action OnHide { get; }

    void SetGameObject(GameObject go);
    void DestroyGameObject(GameObject go);
    
    void Start();
    void Update();
    void Render();
    void EndRender();
    void Show();
    void Hide();
}