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
    /// 複数のテーブルの遅延ロードを管理する。
    /// </summary>
    public class TableLoader : IDisposable
    {
        private readonly Dictionary<Type, LoadedTable> loadedTables = new();
        private readonly Dictionary<string, Type> addressToType = new();
        private bool isDisposed;

        /// <summary>ロード済みテーブル数</summary>
        public int LoadedCount => loadedTables.Count;

        /// <summary>
        /// テーブルを登録する。
        /// </summary>
        /// <typeparam name="T">テーブルの型</typeparam>
        /// <param name="address">Addressablesのアドレス</param>
        public void Register<T>(string address) where T : ScriptableObject, ITableAsset
        {
            addressToType[address] = typeof(T);
        }

        /// <summary>
        /// テーブルを同期的に取得する。
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

            // アドレスを探す
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
        /// テーブルを非同期で取得する。
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

            // アドレスを探す
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
        /// アドレスを指定してテーブルを同期ロードする。
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
        /// アドレスを指定してテーブルを非同期ロードする。
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
        /// 複数のテーブルを一括で非同期ロードする。
        /// </summary>
        public async Task LoadMultipleAsync(params string[] addresses)
        {
            var tasks = new List<Task>();

            foreach (var address in addresses)
            {
                if (addressToType.TryGetValue(address, out var type))
                {
                    // リフレクションで適切な型のLoadAsyncを呼び出す
                    var method = typeof(TableLoader)
                        .GetMethod(nameof(LoadAsync))
                        .MakeGenericMethod(type);

                    tasks.Add((Task)method.Invoke(this, new object[] { address }));
                }
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// テーブルの参照を解放する。
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
        /// テーブルを強制アンロードする。
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
        /// 全てのテーブルをアンロードする。
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
        /// テーブルがロード済みかチェックする。
        /// </summary>
        public bool IsLoaded<T>() where T : ScriptableObject, ITableAsset
        {
            return loadedTables.ContainsKey(typeof(T));
        }

        /// <summary>
        /// ロード済みテーブルの情報を取得する。
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
