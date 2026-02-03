using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// インデックス関連のメニューコマンド。
    /// </summary>
    public static class IndexMenuCommands
    {
        /// <summary>
        /// 選択したテーブルアセットのインデックスを再構築する。
        /// </summary>
        [MenuItem("Assets/XScriptableDB/Rebuild Secondary Indices")]
        private static void RebuildSelectedIndices()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is not ScriptableObject so)
                    continue;

                if (so is not ITableAsset tableAsset)
                    continue;

                var recordType = tableAsset.RecordType;
                if (!IndexBuilder.HasSecondaryKeys(recordType))
                {
                    Debug.Log($"{so.name} has no secondary keys defined");
                    continue;
                }

                var method = so.GetType().GetMethod("RebuildSecondaryIndices", BindingFlags.Public | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(so, null);
                    EditorUtility.SetDirty(so);
                    Debug.Log($"Rebuilt secondary indices for {so.name}");
                }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Assets/XScriptableDB/Rebuild Secondary Indices", true)]
        private static bool RebuildSelectedIndicesValidation()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is ScriptableObject so && so is ITableAsset)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// プロジェクト内の全テーブルアセットのインデックスを再構築する。
        /// </summary>
        [MenuItem("Tools/XScriptableDB/Rebuild All Secondary Indices")]
        private static void RebuildAllIndices()
        {
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            var count = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (asset is not ITableAsset tableAsset)
                    continue;

                var recordType = tableAsset.RecordType;
                if (!IndexBuilder.HasSecondaryKeys(recordType))
                    continue;

                var method = asset.GetType().GetMethod("RebuildSecondaryIndices", BindingFlags.Public | BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(asset, null);
                    EditorUtility.SetDirty(asset);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Rebuilt secondary indices for {count} table(s)");
        }
    }
}