using System;
using System.Reflection;
using MeltEngine.Core;

namespace MeltEngine.Entity.Component
{
    public abstract class Behaviour
    {
        private readonly MethodInfo MethodStart;
        private readonly MethodInfo MethodUpdate;
        private readonly MethodInfo MethodRender;
        private readonly MethodInfo MethodShow;
        private readonly MethodInfo MethodHide;
        public GameObject gameObject { get; private set; }

        public Behaviour()
        {
            MethodStart = GetType().GetMethod("Start", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodUpdate = GetType().GetMethod("Update", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodRender = GetType().GetMethod("Render", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodShow = GetType().GetMethod("Show", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodHide = GetType().GetMethod("Hide", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }
        
        // ? HOTFIX: Avoid lambda subscriptions
        private void START_BINDING() => MethodStart.Invoke(this, null);
        private void UPDATE_BINDING() => MethodUpdate.Invoke(this, null);
        private void RENDER_BINDING() => MethodRender.Invoke(this, null);
        private void SHOW_BINDING() => MethodShow.Invoke(this, null);
        private void HIDE_BINDING() => MethodHide.Invoke(this, null);

        private void SubscribeWorkflow()
        {
            if (MethodStart is not null) Workflow.OnInit += START_BINDING;
            if (MethodUpdate is not null) Workflow.OnUpdate += UPDATE_BINDING;
            if (MethodRender is not null) Workflow.OnRender += RENDER_BINDING;
        }

        private void UnsubscribeWorkflow()
        {
            if (MethodStart is not null) Workflow.OnInit -= START_BINDING;
            if (MethodUpdate is not null) Workflow.OnUpdate -= UPDATE_BINDING;
            if (MethodRender is not null) Workflow.OnRender -= RENDER_BINDING;
        }

        public void SetGameObject(GameObject go)
        {
            gameObject = go;

            if (MethodShow is not null) gameObject.OnShow += SHOW_BINDING;
            if (MethodHide is not null) gameObject.OnHide += HIDE_BINDING;

            gameObject.OnShow += SubscribeWorkflow;
            gameObject.OnHide += UnsubscribeWorkflow;
        }

        public void DestroyGameObject(GameObject go)
        {
            gameObject.Enabled = false;

            if(MethodShow is not null) gameObject.OnShow -= SHOW_BINDING;
            if(MethodHide is not null) gameObject.OnHide -= HIDE_BINDING;

            gameObject.OnShow -= SubscribeWorkflow;
            gameObject.OnHide -= UnsubscribeWorkflow;

            gameObject = null;
        }
    }
}