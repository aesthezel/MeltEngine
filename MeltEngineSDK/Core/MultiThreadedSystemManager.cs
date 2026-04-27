using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MeltEngine.Systems.Interfaces;

namespace MeltEngine.Core
{
    public class MultiThreadedSystemManager
    {
        private readonly List<ISystem> _mainThreadSystems = new();
        private readonly List<ISystem> _backgroundThreadSystems = new();
        private readonly Lock _physicsLock = new Lock();
        
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private Task _backgroundTask;
        
        public void AddMainThreadSystem(ISystem system)
        {
            lock (_physicsLock)
            {
                _mainThreadSystems.Add(system);
            }
        }
        
        public void AddBackgroundThreadSystem(ISystem system)
        {
            _backgroundThreadSystems.Add(system);
        }
        
        public void Start()
        {
            _backgroundTask = Task.Run(BackgroundThreadLoop);
        }
        
        public void UpdateMainThread(ECSOperator entityOperator, float deltaTime)
        {
            lock (_physicsLock)
            {
                foreach (var system in _mainThreadSystems)
                {
                    system.Update(entityOperator, deltaTime);
                }
            }
        }
        
        private void BackgroundThreadLoop()
        {
            var lastTime = DateTime.UtcNow;
            
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var currentTime = DateTime.UtcNow;
                var deltaTime = (float)(currentTime - lastTime).TotalSeconds;
                lastTime = currentTime;
                
                foreach (var system in _backgroundThreadSystems)
                {
                    // system.Update(entityOperator, deltaTime);
                }
                
                Thread.Sleep(16); // ~60 FPS
            }
        }
        
        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _backgroundTask?.Wait(5000);
        }
    }
}