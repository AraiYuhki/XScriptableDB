using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// テーブルアセットの保存時にSecondaryKeyインデックスを自動的に再構築するプロセッサ。
    /// </summary>
    public class IndexRebuildProcessor : AssetModificationProcessor
    {
        /// <summary>
        /// アセットが保存される際に呼び出されます。
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

            // RebuildSecondaryIndicesメソッドを呼び出します
            var method = asset.GetType().GetMethod("RebuildSecondaryIndices", BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(asset, null);
                Debug.Log($"Rebuilt secondary indices for {asset.name}");
            }
        }
    }
}
