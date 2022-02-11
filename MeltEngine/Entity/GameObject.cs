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
        
        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (value)
                {
                    OnShow?.Invoke();
                } else
                {
                    OnHide?.Invoke();
                }

                _enabled = value;
            }
        }

        public readonly List<string> Tags = new();
        private readonly List<Behaviour> _behaviours = new();

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
            _behaviours.Add(behaviour);
        }

        public Behaviour GetBehaviour(Behaviour behaviour) => _behaviours?.Find(b => b == behaviour);
    }
}