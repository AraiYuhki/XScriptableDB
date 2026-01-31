using System.IO;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    public class TableGenerator : EditorWindow
    {
        [MenuItem("Assets/Create/Scripting/Database/Table")]
        public static void OpenWindow()
        {
            var window = GetWindow<TableGenerator>();
            window.Initialize();
        }

        private const string TemplateBasePath = "./Packages/SODatabase/Template";

        private string tableCreatePath = string.Empty;
        private string infoCreatePath = string.Empty;
        private string tableName = "NewTable";
        private string infoName = "NewInfo";

        private void Initialize()
        {
            tableCreatePath = infoCreatePath = AssetDatabase.GetAssetPath(Selection.activeObject);
        }


        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                tableCreatePath = EditorGUILayout.TextField("テーブル作成先", tableCreatePath);
                if (GUILayout.Button("...", GUILayout.Width(50f)))
                    tableCreatePath = SelectCreatePath(tableCreatePath);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                infoCreatePath = EditorGUILayout.TextField("Info作成先", infoCreatePath);
                if (GUILayout.Button("...", GUILayout.Width(50f)))
                    infoCreatePath = SelectCreatePath(infoCreatePath);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                tableName = EditorGUILayout.TextField("テーブルクラス名", tableName);
                infoName = EditorGUILayout.TextField("Infoクラス名", infoName);
            }

            if (GUILayout.Button("生成"))
            {
                GenerateTableClass();
                GenerateInfoClass();
                AssetDatabase.Refresh();
                Close();
            }
        }


        private string SelectCreatePath(string currentPath)
        {
            var directoryName = currentPath;
            if (!string.IsNullOrEmpty(Path.GetExtension(directoryName)))
                directoryName = Path.GetDirectoryName(directoryName);
            directoryName = EditorUtility.SaveFolderPanel("保存先を選択", directoryName, directoryName);
            if (!string.IsNullOrEmpty(directoryName))
                return directoryName;
            return currentPath;
        }

        private void GenerateTableClass()
        {
            var templateFilePath = Path.Combine(TemplateBasePath, "Table.template");
            var fileText = File.ReadAllText(templateFilePath);
            fileText = fileText.Replace("#CLASSNAME", tableName).Replace("#INFO_CLASS_NAME", infoName);
            var newFileName = Path.Combine(tableCreatePath, $"{tableName}.cs");
            File.WriteAllText(newFileName, fileText);
            AssetDatabase.ImportAsset(newFileName);
        }

        private void GenerateInfoClass()
        {
            var templateFilePath = Path.Combine(TemplateBasePath, "Info.template");
            var fileText = File.ReadAllText(templateFilePath);
            fileText = fileText.Replace("#CLASSNAME", infoName);
            var newFileName = Path.Combine(infoCreatePath, $"{infoName}.cs");
            File.WriteAllText(newFileName, fileText);
            AssetDatabase.ImportAsset(newFileName);
        }
    }
}
