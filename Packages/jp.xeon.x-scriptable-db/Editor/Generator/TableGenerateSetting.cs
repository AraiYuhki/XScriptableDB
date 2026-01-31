using System.IO;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    [CreateAssetMenu(fileName = "TableGenerateSetting", menuName = "Xeon/XScriptableDB/Setting/TableGenerateSetting")]
    public class TableGenerateSetting : ScriptableObject
    {
        [SerializeField]
        private string namespaceName = "Xeon.XScriptableDB";
        [SerializeField]
        private string savePath = "XScriptableDB/Generated";

        public string NamespaceName => namespaceName;
        public string SavePath => savePath;

        private static TableGenerateSetting instance;
        public static TableGenerateSetting Instance
        {
            get
            {
                if (instance == null)
                {
                    var guids = AssetDatabase.FindAssets($"t:{nameof(TableGenerateSetting)}");
                    if (guids == null || guids.Length <= 0)
                    {
                        instance = CreateInstance<TableGenerateSetting>();
                        var directoryPath = "Assets/Editor/XScriptableDB/Settings";
                        if (!Directory.Exists(directoryPath))
                            Directory.CreateDirectory(directoryPath);
                        var path = $"{directoryPath}/TableGenerateSetting.asset";
                        AssetDatabase.CreateAsset(instance, path);
                        AssetDatabase.ImportAsset(path);
                    }
                    else
                    {
                        // 設定ファイルはプロジェクト内に一つだけに限定する
                        var path = AssetDatabase.GUIDToAssetPath(new GUID(guids[0]));
                        instance = AssetDatabase.LoadAssetAtPath<TableGenerateSetting>(path);
                    }
                }
                return instance;
            }
        }
    }
}
