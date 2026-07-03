using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    [InitializeOnLoad]
    public class ScriptGenerator : MonoBehaviour
    {
        private const string TemplateBasePath = "./Packages/SODatabase/Template";

        [MenuItem("Assets/Create/Scripting/Database/Database")]
        public static void CreateDatabaseCode()
        {
            CreateFile<EndDBScriptNameEditAction>("NewDatabaseScript.cs", "DB.template");
        }

        private static void CreateFile<T>(string fileName, string templateName) where T : AssetCreationEndAction
        {
            var directoryPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(Path.GetExtension(directoryPath)))
                directoryPath = Path.GetDirectoryName(directoryPath);

            var newFilePath = Path.Combine(directoryPath, fileName);
            var templateFilePath = Path.Join(TemplateBasePath, templateName);

            var fileText = File.ReadAllText(templateFilePath);

            File.WriteAllText(newFilePath, fileText);
            AssetDatabase.ImportAsset(newFilePath);
            AssetDatabase.Refresh();
            var asset = AssetDatabase.LoadAssetAtPath(newFilePath, typeof(TextAsset));
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                asset.GetEntityId(),
                ScriptableObject.CreateInstance<T>(),
                newFilePath,
                AssetPreview.GetMiniThumbnail(asset),
                newFilePath);
            Selection.activeObject = asset;
        }

        private abstract class EndScriptNameEditActionBase : AssetCreationEndAction
        {
            protected abstract string TemplateFileName { get; }
            public override void Action(EntityId entityId, string pathName, string resourceFile)
            {
                var templateFilePath = Path.Join(TemplateBasePath, TemplateFileName);
                var fileText = File.ReadAllText(templateFilePath);
                Debug.Log(templateFilePath);
                File.Move(resourceFile, pathName);
                File.Move(resourceFile + ".meta", pathName + ".meta");
                File.WriteAllText(pathName,
                    fileText.Replace("#CLASSNAME", Path.GetFileNameWithoutExtension(pathName).Replace(" ", ""))
                    );
                AssetDatabase.Refresh();
            }
        }
        private class EndDBScriptNameEditAction : EndScriptNameEditActionBase
        {
            protected override string TemplateFileName => "DB.template";
        }
    }
}
