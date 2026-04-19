using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Xeon.XScriptableDB.LazyLoad
{
    /// <summary>
    /// テーブルローダー。
    /// 複数のテーブルの遅延読み込みを管理します。
    /// </summary>
    public class TableLoader : IDisposable
    {
        private readonly Dictionary<Type, LoadedTable> loadedTables = new();
        private readonly Dictionary<string, Type> addressToType = new();
        private bool isDisposed;

        /// <summary>ロード済みテーブル数</summary>
        public int LoadedCount => loadedTables.Count;

        /// <summary>
        /// テーブルを登録します。
        /// </summary>
        /// <typeparam name="T">テーブルの型</typeparam>
        /// <param name="address">Addressablesのアドレス</param>
        public void Register<T>(string address) where T : ScriptableObject, ITableAsset
        {
            addressToType[address] = typeof(T);
        }

        /// <summary>
        /// テーブルを同期的に取得します。
        /// </summary>
        /// <typeparam name="T">テーブルの型</typeparam>
        /// <returns>テーブルアセット</returns>
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
                Debug.LogError($"TableLoader: 型 {type.Name} のアドレスが登録されていません");
                return null;
            }

            return LoadSync<T>(address);
        }

        /// <summary>
        /// テーブルを非同期的に取得します。
        /// </summary>
        /// <typeparam name="T">テーブルの型</typeparam>
        /// <returns>テーブルアセット</returns>
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
                Debug.LogError($"TableLoader: 型 {type.Name} のアドレスが登録されていません");
                return null;
            }

            return await LoadAsync<T>(address);
        }

        /// <summary>
        /// 指定されたアドレスからテーブルを同期的にロードします。
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
        /// 指定されたアドレスからテーブルを非同期的にロードします。
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
        /// 複数のテーブルを一度に非同期的にロードします。
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
        /// テーブルへの参照を解放します。
        /// </summary>
        /// <typeparam name="T">テーブルの型</typeparam>
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
        /// テーブルを強制的にアンロードします。
        /// </summary>
        /// <typeparam name="T">テーブルの型</typeparam>
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
        /// すべてのテーブルをアンロードします。
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
        /// テーブルがすでにロードされているかどうかを確認します。
        /// </summary>
        public bool IsLoaded<T>() where T : ScriptableObject, ITableAsset
        {
            return loadedTables.ContainsKey(typeof(T));
        }

        /// <summary>
        /// ロードされたテーブルに関する情報を返します。
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
