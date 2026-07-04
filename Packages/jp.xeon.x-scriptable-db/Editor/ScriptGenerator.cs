using System.IO;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

#if UNITY_6000_5_OR_NEWER
using ProjectWindowAssetId = UnityEngine.EntityId;
using ProjectWindowEndAction = UnityEditor.ProjectWindowCallback.AssetCreationEndAction;
#else
using ProjectWindowAssetId = System.Int32;
using ProjectWindowEndAction = UnityEditor.ProjectWindowCallback.EndNameEditAction;
#endif

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

        private static void CreateFile<T>(string fileName, string templateName) where T : ProjectWindowEndAction
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
                GetProjectWindowAssetId(asset),
                ScriptableObject.CreateInstance<T>(),
                newFilePath,
                AssetPreview.GetMiniThumbnail(asset),
                newFilePath);
            Selection.activeObject = asset;
        }

#if UNITY_6000_5_OR_NEWER
        private static ProjectWindowAssetId GetProjectWindowAssetId(Object asset) => asset.GetEntityId();
#else
        private static ProjectWindowAssetId GetProjectWindowAssetId(Object asset) => asset.GetInstanceID();
#endif

        private abstract class EndScriptNameEditActionBase : ProjectWindowEndAction
        {
            protected abstract string TemplateFileName { get; }
            public override void Action(ProjectWindowAssetId entityId, string pathName, string resourceFile)
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
