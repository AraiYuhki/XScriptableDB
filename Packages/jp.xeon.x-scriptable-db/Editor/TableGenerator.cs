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
                tableCreatePath = EditorGUILayout.TextField("�e�[�u���쐬��", tableCreatePath);
                if (GUILayout.Button("...", GUILayout.Width(50f)))
                    tableCreatePath = SelectCreatePath(tableCreatePath);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                infoCreatePath = EditorGUILayout.TextField("Info�N���X�쐬��", infoCreatePath);
                if (GUILayout.Button("...", GUILayout.Width(50f)))
                    infoCreatePath = SelectCreatePath(infoCreatePath);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                tableName = EditorGUILayout.TextField("�e�[�u���N���X��", tableName);
                infoName = EditorGUILayout.TextField("Info�N���X��", infoName);
            }

            if (GUILayout.Button("����"))
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
            directoryName = EditorUtility.SaveFolderPanel("�������I��", directoryName, directoryName);
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
