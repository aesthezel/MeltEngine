using System;
using System.Collections.Concurrent;
using System.Threading;
using MeltEngine.Entities;
using MeltEngine.Entities.Components;

namespace MeltEngine.Core
{
    public class ThreadSafeECSOperator : ECSOperator
    {
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly ConcurrentQueue<Action> _pendingOperations = new();
        
        public override void DestroyEntity(Entity entity)
        {
            _lock.EnterWriteLock();
            try
            {
                base.DestroyEntity(entity);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        
        public override void AddComponent<T>(Entity entity, T component) where T : struct
        {
            _lock.EnterWriteLock();
            try
            {
                base.AddComponent(entity, component);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public override void RemoveComponent<T>(Entity entity) where T : struct
        {
            _lock.EnterWriteLock();
            try
            {
                base.RemoveComponent<T>(entity);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public override T GetComponent<T>(Entity entity) where T : struct
        {
            _lock.EnterReadLock();
            try
            {
                return base.GetComponent<T>(entity);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        
        public ComponentArray<T> GetComponentArraySafe<T>() where T : struct
        {
            _lock.EnterReadLock();
            try
            {
                return base.GetComponentArray<T>();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        
        public void QueueOperation(Action operation)
        {
            _pendingOperations.Enqueue(operation);
        }
        
        public void ProcessPendingOperations()
        {
            while (_pendingOperations.TryDequeue(out var operation))
            {
                operation.Invoke();
            }
        }
        
        public void QueueAddComponent<T>(Entity entity, T component) where T : struct
        {
            _pendingOperations.Enqueue(() => AddComponent(entity, component));
        }
        
        public void QueueRemoveComponent<T>(Entity entity) where T : struct
        {
            _pendingOperations.Enqueue(() => RemoveComponent<T>(entity));
        }
        
        public void QueueDestroyEntity(Entity entity)
        {
            _pendingOperations.Enqueue(() => DestroyEntity(entity));
        }
        
        public bool TryGetComponent<T>(Entity entity, out T component) where T : struct
        {
            _lock.EnterReadLock();
            try
            {
                try
                {
                    component = base.GetComponent<T>(entity);
                    return true;
                }
                catch
                {
                    component = default(T);
                    return false;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        
        public bool HasComponent<T>(Entity entity) where T : struct
        {
            _lock.EnterReadLock();
            try
            {
                try
                {
                    base.GetComponent<T>(entity);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Dispose()
        {
            _lock?.Dispose();
        }
    }
}