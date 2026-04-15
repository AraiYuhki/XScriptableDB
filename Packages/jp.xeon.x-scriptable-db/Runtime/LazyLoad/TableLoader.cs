using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Xeon.XScriptableDB.LazyLoad
{
    /// <summary>
    /// Table loader.
    /// Manages lazy loading of multiple tables.
    /// </summary>
    public class TableLoader : IDisposable
    {
        private readonly Dictionary<Type, LoadedTable> loadedTables = new();
        private readonly Dictionary<string, Type> addressToType = new();
        private bool isDisposed;

        /// <summary>Number of loaded tables</summary>
        public int LoadedCount => loadedTables.Count;

        /// <summary>
        /// Registers a table.
        /// </summary>
        /// <typeparam name="T">Type of the table</typeparam>
        /// <param name="address">Addressables address</param>
        public void Register<T>(string address) where T : ScriptableObject, ITableAsset
        {
            addressToType[address] = typeof(T);
        }

        /// <summary>
        /// Synchronously retrieves a table.
        /// </summary>
        /// <typeparam name="T">Type of the table</typeparam>
        /// <returns>Table asset</returns>
        public T Get<T>() where T : ScriptableObject, ITableAsset
        {
            var type = typeof(T);

            if (loadedTables.TryGetValue(type, out var loaded))
            {
                loaded.ReferenceCount++;
                return (T)loaded.Asset;
            }

            // Find the address
            string address = null;
            foreach (var kvp in addressToType)
            {
                if (kvp.Value == type)
                {
                    address = kvp.Key;
                    break;
                }
            }

            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"TableLoader: No address registered for type {type.Name}");
                return null;
            }

            return LoadSync<T>(address);
        }

        /// <summary>
        /// Asynchronously retrieves a table.
        /// </summary>
        /// <typeparam name="T">Type of the table</typeparam>
        /// <returns>Table asset</returns>
        public async Task<T> GetAsync<T>() where T : ScriptableObject, ITableAsset
        {
            var type = typeof(T);

            if (loadedTables.TryGetValue(type, out var loaded))
            {
                loaded.ReferenceCount++;
                return (T)loaded.Asset;
            }

            // Find the address
            string address = null;
            foreach (var kvp in addressToType)
            {
                if (kvp.Value == type)
                {
                    address = kvp.Key;
                    break;
                }
            }

            if (string.IsNullOrEmpty(address))
            {
                Debug.LogError($"TableLoader: No address registered for type {type.Name}");
                return null;
            }

            return await LoadAsync<T>(address);
        }

        /// <summary>
        /// Synchronously loads a table from the specified address.
        /// </summary>
        public T LoadSync<T>(string address) where T : ScriptableObject, ITableAsset
        {
            var type = typeof(T);

            if (loadedTables.TryGetValue(type, out var existing))
            {
                existing.ReferenceCount++;
                return (T)existing.Asset;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            var asset = handle.WaitForCompletion();

            loadedTables[type] = new LoadedTable
            {
                Asset = asset,
                Handle = handle,
                ReferenceCount = 1
            };

            return asset;
        }

        /// <summary>
        /// Asynchronously loads a table from the specified address.
        /// </summary>
        public async Task<T> LoadAsync<T>(string address) where T : ScriptableObject, ITableAsset
        {
            var type = typeof(T);

            if (loadedTables.TryGetValue(type, out var existing))
            {
                existing.ReferenceCount++;
                return (T)existing.Asset;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            var asset = await handle.Task;

            loadedTables[type] = new LoadedTable
            {
                Asset = asset,
                Handle = handle,
                ReferenceCount = 1
            };

            return asset;
        }

        /// <summary>
        /// Asynchronously loads multiple tables at once.
        /// </summary>
        public async Task LoadMultipleAsync(params string[] addresses)
        {
            var tasks = new List<Task>();

            foreach (var address in addresses)
            {
                if (addressToType.TryGetValue(address, out var type))
                {
                    // Call LoadAsync with the appropriate type via reflection
                    var method = typeof(TableLoader)
                        .GetMethod(nameof(LoadAsync))
                        .MakeGenericMethod(type);

                    tasks.Add((Task)method.Invoke(this, new object[] { address }));
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Releases the reference to the table.
        /// </summary>
        /// <typeparam name="T">Type of the table</typeparam>
        public void Release<T>() where T : ScriptableObject, ITableAsset
        {
            var type = typeof(T);

            if (!loadedTables.TryGetValue(type, out var loaded))
                return;

            loaded.ReferenceCount--;

            if (loaded.ReferenceCount <= 0)
            {
                Unload<T>();
            }
        }

        /// <summary>
        /// Force-unloads the table.
        /// </summary>
        /// <typeparam name="T">Type of the table</typeparam>
        public void Unload<T>() where T : ScriptableObject, ITableAsset
        {
            var type = typeof(T);

            if (!loadedTables.TryGetValue(type, out var loaded))
                return;

            if (loaded.Handle.IsValid())
            {
                Addressables.Release(loaded.Handle);
            }

            loadedTables.Remove(type);
        }

        /// <summary>
        /// Unloads all tables.
        /// </summary>
        public void UnloadAll()
        {
            foreach (var loaded in loadedTables.Values)
            {
                if (loaded.Handle.IsValid())
                {
                    Addressables.Release(loaded.Handle);
                }
            }

            loadedTables.Clear();
        }

        /// <summary>
        /// Checks whether the table is already loaded.
        /// </summary>
        public bool IsLoaded<T>() where T : ScriptableObject, ITableAsset
        {
            return loadedTables.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Returns information about the loaded tables.
        /// </summary>
        public IReadOnlyCollection<LoadedTableInfo> GetLoadedTableInfo()
        {
            var result = new List<LoadedTableInfo>();

            foreach (var kvp in loadedTables)
            {
                result.Add(new LoadedTableInfo
                {
                    TableType = kvp.Key,
                    ReferenceCount = kvp.Value.ReferenceCount,
                    RecordCount = kvp.Value.Asset?.Count ?? 0
                });
            }

            return result;
        }

        public void Dispose()
        {
            if (isDisposed) return;

            UnloadAll();
            isDisposed = true;
        }

        private class LoadedTable
        {
            public ITableAsset Asset;
            public AsyncOperationHandle Handle;
            public int ReferenceCount;
        }
    }
}
