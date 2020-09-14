using ConcurrentCollections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CachingMechanism
{
    public class EntityCache
    {
        public EntityCache(int checkFrequencySeconds)
        {
            this._checkFrequencySeconds = checkFrequencySeconds;
            this._cancellationTokenSource = new CancellationTokenSource();
            this._cache = new ConcurrentDictionary<Type, ConcurrentHashSet<CachedEntity>>();
            this._ignoreRequests = new ConcurrentDictionary<Type, ConcurrentHashSet<IgnoredEntity>>();
            this.BeginRemovalLoop();
        }

        private readonly CancellationTokenSource _cancellationTokenSource;

        private readonly ConcurrentDictionary<Type, ConcurrentHashSet<CachedEntity>> _cache;

        private readonly ConcurrentDictionary<Type, ConcurrentHashSet<IgnoredEntity>> _ignoreRequests;

        private readonly int _checkFrequencySeconds;

        public T Remove<T>(Func<T, bool> keyRetrievalFunc) where T : class
        {
            var container = _cache.FirstOrDefault(x => x.Key == typeof(T));

            if (container.Value == null)
            {
                return null;
            }

            CachedEntity val = null;
            T valObj = null;
            foreach (var o in container.Value)
            {
                if (o.CachedObject is T tmp)
                {
                    if (keyRetrievalFunc(tmp))
                    {
                        valObj = tmp;
                        val = o;
                        break;
                    }
                }
            }

            if (val != null)
            {
                container.Value.TryRemove(val);
            }

            return valObj;
        }

        public T Retrieve<T>(Func<T, bool> keyRetrievalFunc) where T : class
        {
            var container = _cache.FirstOrDefault(x => x.Key == typeof(T));

            if (container.Value == null)
            {
                return null;
            }

            foreach (var o in container.Value)
            {
                if (o.CachedObject is T val)
                {
                    if (keyRetrievalFunc(val))
                    {
                        return val;
                    }
                }
            }

            return null;
        }

        public void Put<T>(T item, TimeSpan? expiresAfter = null)
        {
            var container = _cache.FirstOrDefault(x => x.Key == typeof(T));

            var set = container.Value;
            if (set == null)
            {
                set = new ConcurrentHashSet<CachedEntity>();
                _cache.TryAdd(typeof(T), set);
            }

            set.Add(new CachedEntity(item, expiresAfter));
        }

        public void PutIgnore<T>(TimeSpan? expiresAfter, params object[] key)
        {
            var container = _ignoreRequests.FirstOrDefault(x => x.Key == typeof(T));

            var set = container.Value;
            if (set == null)
            {
                set = new ConcurrentHashSet<IgnoredEntity>();
                _ignoreRequests.TryAdd(typeof(T), set);
            }

            set.Add(new IgnoredEntity(key, expiresAfter));
        }

        public void RemoveIgnore<T>(params object[] key)
        {
            var container = _ignoreRequests.FirstOrDefault(x => x.Key == typeof(T));

            if (container.Value == null)
            {
                return;
            }

            IgnoredEntity val = null;
            foreach (var o in container.Value)
            {
                if (o.CachedObject.SequenceEqual(key))
                {
                    val = o;
                    break;
                }
            }

            if (val != null)
            {
                container.Value.TryRemove(val);
            }
        }

        public bool ExistsIgnore<T>(params object[] key)
        {
            var container = _ignoreRequests.FirstOrDefault(x => x.Key == typeof(T));

            if (container.Value == null)
            {
                return false;
            }

            foreach (var o in container.Value)
            {
                if (key.SequenceEqual(o.CachedObject))
                {
                    return true;
                }
            }

            return false;
        }

        private void BeginRemovalLoop()
        {
            var checkDatesTask = new Task(
                () =>
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        // Iterate all sub-collections
                        foreach (var (key, value) in _cache)
                        {
                            // Check for entities that have expired
                            List<CachedEntity> removableEntities = null;
                            foreach (var item in value)
                            {
                                if (item.ExpiresAfter != null &&
                                    item.CacheEntryTime + item.ExpiresAfter < DateTime.UtcNow)
                                {
                                    removableEntities ??= new List<CachedEntity>();
                                    removableEntities.Add(item);
                                }
                            }

                            if (removableEntities != null)
                                foreach (var removableEntity in removableEntities)
                                {
                                    value.TryRemove(removableEntity);
                                }
                        }

                        foreach (var (key, value) in _ignoreRequests)
                        {
                            // Check for entities that have expired
                            List<IgnoredEntity> removableEntities = null;
                            foreach (var item in value)
                            {
                                if (item.ExpiresAfter != null &&
                                    item.CacheEntryTime + item.ExpiresAfter < DateTime.UtcNow)
                                {
                                    removableEntities ??= new List<IgnoredEntity>();
                                    removableEntities.Add(item);
                                }
                            }

                            if (removableEntities != null)
                                foreach (var removableEntity in removableEntities)
                                {
                                    value.TryRemove(removableEntity);
                                }
                        }

                        _cancellationTokenSource.Token.WaitHandle.WaitOne(
                            TimeSpan.FromSeconds(
                                _checkFrequencySeconds));
                    }
                },
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning);
            checkDatesTask.Start();
        }

        private class IgnoredEntity
        {
            public readonly object[] CachedObject;

            public readonly DateTime CacheEntryTime;

            public readonly TimeSpan? ExpiresAfter;

            public IgnoredEntity(object[] item, TimeSpan? expiresAfter = null)
            {
                this.CachedObject = item;
                this.ExpiresAfter = expiresAfter;
                this.CacheEntryTime = DateTime.UtcNow;
            }
        }

        private class CachedEntity
        {
            public readonly object CachedObject;

            public readonly DateTime CacheEntryTime;

            public readonly TimeSpan? ExpiresAfter;

            public CachedEntity(object item, TimeSpan? expiresAfter = null)
            {
                this.CachedObject = item;
                this.ExpiresAfter = expiresAfter;
                this.CacheEntryTime = DateTime.UtcNow;
            }
        }
    }
}