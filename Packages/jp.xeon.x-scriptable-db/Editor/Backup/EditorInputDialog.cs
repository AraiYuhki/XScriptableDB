using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// 簡易入力ダイアログ。
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        private string inputText = "";
        private string message = "";
        private System.Action<string> onConfirm;

        public static string Show(string title, string message, string defaultValue = "")
        {
            var result = defaultValue;

            var window = CreateInstance<EditorInputDialog>();
            window.titleContent = new GUIContent(title);
            window.message = message;
            window.inputText = defaultValue;
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(300, 100);

            window.onConfirm = (value) => result = value;
            window.ShowModal();

            return result;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(message);
            inputText = EditorGUILayout.TextField(inputText);

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("OK", GUILayout.Width(80)))
            {
                onConfirm?.Invoke(inputText);
                Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                onConfirm?.Invoke(null);
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}