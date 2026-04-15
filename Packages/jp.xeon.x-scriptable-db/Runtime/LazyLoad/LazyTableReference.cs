using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Xeon.XScriptableDB.LazyLoad
{
    /// <summary>
    /// Lazy-load reference for a table.
    /// Loads the table on demand using Addressables.
    /// </summary>
    /// <typeparam name="T">Type of the table</typeparam>
    [Serializable]
    public class LazyTableReference<T> where T : ScriptableObject, ITableAsset
    {
        [SerializeField]
        private AssetReference assetReference;

        private T loadedAsset;
        private AsyncOperationHandle<T> handle;
        private int referenceCount;
        private bool isLoading;

        /// <summary>Asset reference</summary>
        public AssetReference AssetReference => assetReference;

        /// <summary>Whether the asset is loaded</summary>
        public bool IsLoaded => loadedAsset != null;

        /// <summary>Whether the asset is loading</summary>
        public bool IsLoading => isLoading;

        /// <summary>Reference count</summary>
        public int ReferenceCount => referenceCount;

        /// <summary>
        /// Returns the loaded asset.
        /// Returns null if not yet loaded.
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
        /// Synchronously retrieves the table.
        /// Performs a synchronous load if not yet loaded.
        /// </summary>
        /// <returns>Table asset</returns>
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

            // Synchronous load
            handle = assetReference.LoadAssetAsync<T>();
            loadedAsset = handle.WaitForCompletion();
            referenceCount = 1;

            return loadedAsset;
        }

        /// <summary>
        /// Asynchronously loads the table.
        /// </summary>
        /// <returns>Table asset</returns>
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
        /// Releases the reference.
        /// The asset is unloaded when the reference count reaches zero.
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
        /// Force-unloads the asset.
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
