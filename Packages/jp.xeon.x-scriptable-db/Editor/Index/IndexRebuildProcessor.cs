using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Processor that automatically rebuilds SecondaryKey indices when a table asset is saved.
    /// </summary>
    public class IndexRebuildProcessor : AssetModificationProcessor
    {
        /// <summary>
        /// Called when assets are about to be saved.
        /// </summary>
        private static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                    continue;

                if (asset is ITableAsset tableAsset)
                    RebuildIndicesIfNeeded(asset, tableAsset);
            }
            return paths;
        }

        private static void RebuildIndicesIfNeeded(ScriptableObject asset, ITableAsset tableAsset)
        {
            var recordType = tableAsset.RecordType;
            if (!IndexBuilder.HasSecondaryKeys(recordType))
                return;

            // Call the RebuildSecondaryIndices method
            var method = asset.GetType().GetMethod("RebuildSecondaryIndices", BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(asset, null);
                Debug.Log($"Rebuilt secondary indices for {asset.name}");
            }
        }
    }
}
