using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Xeon.XScriptableDB.LazyLoad
{
    /// <summary>
    /// テーブルの遅延ロード参照。
    /// Addressablesを使用してオンデマンドでテーブルを読み込む。
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

        /// <summary>ロード済みかどうか</summary>
        public bool IsLoaded => loadedAsset != null;

        /// <summary>ロード中かどうか</summary>
        public bool IsLoading => isLoading;

        /// <summary>参照カウント</summary>
        public int ReferenceCount => referenceCount;

        /// <summary>
        /// ロード済みのアセットを取得する。
        /// ロードされていない場合はnullを返す。
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
        /// 同期的にテーブルを取得する。
        /// まだロードされていない場合は同期ロードを行う。
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
                Debug.LogError("LazyTableReference: Invalid asset reference");
                return null;
            }

            // 同期ロード
            handle = assetReference.LoadAssetAsync<T>();
            loadedAsset = handle.WaitForCompletion();
            referenceCount = 1;

            return loadedAsset;
        }

        /// <summary>
        /// 非同期でテーブルを読み込む。
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
                // 既にロード中の場合は完了を待つ
                await handle.Task;
                referenceCount++;
                return loadedAsset;
            }

            if (assetReference == null || !assetReference.RuntimeKeyIsValid())
            {
                Debug.LogError("LazyTableReference: Invalid asset reference");
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
        /// 参照を解放する。
        /// 参照カウントが0になるとアンロードされる。
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
        /// 強制的にアンロードする。
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
