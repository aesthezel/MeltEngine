using System;
using MeltEngine.Core;

namespace MeltEngine.Entity.Components
{
    public abstract class Behaviour : IComponent
    {
        public GameObject GameObject { get; private set; }
        
        // Delegados directos
        public Action OnStart { get; protected set; }
        public Action OnUpdate { get; protected set; }
        public Action OnRender { get; protected set; }
        public Action OnEndRender { get; protected set; }
        public Action OnShow { get; protected set; }
        public Action OnHide { get; protected set; }

        public Behaviour()
        {
            // Vincula los métodos a delegados
            OnStart = Start;
            OnUpdate = Update;
            OnRender = Render;
            OnEndRender = EndRender;
            OnShow = Show;
            OnHide = Hide;
        }

        // Métodos virtuales
        public virtual void Start() { }
        public virtual void Update() { }
        public virtual void Render() { }
        public virtual void EndRender() { }
        public virtual void Show() { }
        public virtual void Hide() { }

        private void SubscribeWorkflow()
        {
            Workflow.OnInit += OnStart;
            Workflow.OnUpdate += ConditionalUpdate;
            Workflow.OnRender += OnRender;
            Workflow.OnEndRender += OnEndRender;
        }

        private void UnsubscribeWorkflow()
        {
            Workflow.OnInit -= OnStart;
            Workflow.OnUpdate -= ConditionalUpdate;
            Workflow.OnRender -= OnRender;
            Workflow.OnEndRender -= OnEndRender;
        }

        public void SetGameObject(GameObject go)
        {
            GameObject = go;

            GameObject.OnShow += OnShow;
            GameObject.OnHide += OnHide;
            GameObject.OnShow += SubscribeWorkflow;
            GameObject.OnHide += UnsubscribeWorkflow;

            Workflow.OnUpdate += ConditionalUpdate; // <- Se suscribe un método condicional.
            
            SubscribeWorkflow();
        }
        
        private void ConditionalUpdate()
        {
            if (GameObject is { Enabled: true })
            {
                OnUpdate?.Invoke();
            }
        }

        public virtual void DestroyGameObject(GameObject go)
        {
            GameObject.Enabled = false;

            GameObject.OnShow -= OnShow;
            GameObject.OnHide -= OnHide;
            GameObject.OnShow -= SubscribeWorkflow;
            GameObject.OnHide -= UnsubscribeWorkflow;

            GameObject = null;
        }
    }
}