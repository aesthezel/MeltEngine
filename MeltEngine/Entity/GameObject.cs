﻿using System;
using System.Collections.Generic;
using System.Linq;
using MeltEngine.Entity.Components;

namespace MeltEngine.Entity
{
    public class GameObject
    {
        public string Name { get; set; }
        
        public event Action OnShow;
        public event Action OnHide;
        
        private bool _enabled;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (value)
                {
                    OnShow?.Invoke();
                }
                else
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
            
            if (tags is not null) Tags.AddRange(tags);
        }

        public void AddBehaviour(Behaviour behaviour)
        {
            _behaviours.Add(behaviour);
            behaviour.SetGameObject(this);
        }

        public T GetBehaviour<T>() where T : Behaviour
        {
            foreach (var behaviour in _behaviours)
            {
                Console.WriteLine($"{behaviour.GetType().Name} - Found!");
            }
            
            return _behaviours.OfType<T>().FirstOrDefault();
        }

        public void Destroy()
        {
            foreach(var behaviour in _behaviours) behaviour.DestroyGameObject(this);
        }
    }
}