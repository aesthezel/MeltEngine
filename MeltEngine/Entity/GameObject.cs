using System;
using System.Collections.Generic;
using MeltEngine.Entity.Component;

namespace MeltEngine.Entity
{
    public class GameObject
    {
        public string Name { get; set; }
        
        public Action OnShow;
        public Action OnHide;
        
        private bool enabled;
        public bool Enabled
        {
            get => enabled;
            set
            {
                if(value) OnShow?.Invoke();
                else OnHide?.Invoke();
                
                enabled = value;
            }
        }

        public readonly List<string> Tags = new();
        private readonly List<Behaviour> Behaviours = new();

        public GameObject(string name, bool enabled, string[] tags = null)
        {
            Name = name;
            Enabled = enabled;
            
            if(tags is not null) Tags.AddRange(tags);

            var coord = new Coord();
            AddBehaviour(coord);
        }

        public void AddBehaviour(Behaviour behaviour)
        {
            behaviour.SetGameObject(this);
            Behaviours.Add(behaviour);
        }

        public Behaviour GetBehaviour(Behaviour behaviour) => Behaviours?.Find(b => b == behaviour);
    }
}