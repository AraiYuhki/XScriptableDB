using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Xeon.XScriptableDB.LazyLoad
{
    /// <summary>
    /// テーブルの遅延読み込み参照。
    /// Addressablesを使用して必要に応じてテーブルをロードします。
    /// </summary>
    /// <typeparam name="T">テーブルの型</typeparam>
    [Serializable]
    public class LazyTableReference<T> where T : ScriptableObject, ITableAsset
    {
        [SerializeField]
        private AssetReference assetReference;

        private T loadedAsset;
        private AsyncOperationHandle<T> handle;
        private int referenceCount;
        private bool isLoading;

        /// <summary>アセット参照</summary>
        public AssetReference AssetReference => assetReference;

        /// <summary>アセットがロードされているかどうか</summary>
        public bool IsLoaded => loadedAsset != null;

        /// <summary>アセットがロード中かどうか</summary>
        public bool IsLoading => isLoading;

        /// <summary>参照カウント</summary>
        public int ReferenceCount => referenceCount;

        /// <summary>
        /// ロードされたアセットを返します。
        /// まだロードされていない場合はnullを返します。
        /// </summary>
        public T Asset => loadedAsset;

        public LazyTableReference()
        {
        }

        public LazyTableReference(AssetReference reference)
        {
            assetReference = reference;
        }

        /// <summary>
        /// テーブルを同期的に取得します。
        /// まだロードされていない場合は同期ロードを実行します。
        /// </summary>
        /// <returns>テーブルアセット</returns>
        public T GetOrLoad()
        {
            if (loadedAsset != null)
            {
                referenceCount++;
                return loadedAsset;
            }

            if (assetReference == null || !assetReference.RuntimeKeyIsValid())
            {
                Debug.LogError("LazyTableReference: 無効なアセット参照です");
                return null;
            }

            // Synchronous load
            handle = assetReference.LoadAssetAsync<T>();
            loadedAsset = handle.WaitForCompletion();
            referenceCount = 1;

            return loadedAsset;
        }

        /// <summary>
        /// テーブルを非同期的にロードします。
        /// </summary>
        /// <returns>テーブルアセット</returns>
        public async Task<T> LoadAsync()
        {
            if (loadedAsset != null)
            {
                referenceCount++;
                return loadedAsset;
            }

            if (isLoading)
            {
                // Already loading — wait for completion
                await handle.Task;
                referenceCount++;
                return loadedAsset;
            }

            if (assetReference == null || !assetReference.RuntimeKeyIsValid())
            {
                Debug.LogError("LazyTableReference: 無効なアセット参照です");
                return null;
            }

            isLoading = true;

            try
            {
                handle = assetReference.LoadAssetAsync<T>();
                loadedAsset = await handle.Task;
                referenceCount = 1;
                return loadedAsset;
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>
        /// 参照を解放します。
        /// 参照カウントがゼロになるとアセットがアンロードされます。
        /// </summary>
        public void Release()
        {
            if (loadedAsset == null) return;

            referenceCount--;

            if (referenceCount <= 0)
            {
                Unload();
            }
        }

        /// <summary>
        /// アセットを強制的にアンロードします。
        /// </summary>
        public void Unload()
        {
            if (loadedAsset == null) return;

            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            loadedAsset = null;
            referenceCount = 0;
        }
    }
}
