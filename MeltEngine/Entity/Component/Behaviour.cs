using System.Reflection;
using MeltEngine.Core;

namespace MeltEngine.Entity.Component
{
    public abstract class Behaviour
    {
        private readonly MethodInfo MethodStart;
        private readonly MethodInfo MethodUpdate;
        private readonly MethodInfo MethodRender;
        private readonly MethodInfo MethodHide;
        private readonly MethodInfo MethodShow;
        public GameObject gameObject { get; private set; }

        public Behaviour()
        {
            MethodStart = GetType().GetMethod("Start", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodUpdate = GetType().GetMethod("Update", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodRender = GetType().GetMethod("Render", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodShow = GetType().GetMethod("Show", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            MethodHide = GetType().GetMethod("Hide", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            if(MethodStart is not null) Workflow.OnInit += () => MethodStart.Invoke(this, null);
            if(MethodUpdate is not null) Workflow.OnUpdate += () => MethodUpdate.Invoke(this, null);
            if(MethodRender is not null) Workflow.OnRender += () => MethodRender.Invoke(this, null);
        }

        public void SetGameObject(GameObject go)
        {
            gameObject = go;
            if(MethodHide is not null) gameObject.OnShow += () => MethodShow.Invoke(this, null);
            if(MethodShow is not null) gameObject.OnHide += () => MethodHide.Invoke(this, null);
        }
    }
}