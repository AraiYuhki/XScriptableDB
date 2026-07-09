using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SerializableNullable&lt;T&gt;型用のカスタムPropertyDrawer。
    /// 値の有無をチェックボックスで切り替え、値ありのときだけ中身のフィールドを編集可能にします。
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableNullable<>))]
    public class SerializableNullablePropertyDrawer : PropertyDrawer
    {
        private const float ToggleWidth = 18f;
        private const float Spacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var hasValueProperty = property.FindPropertyRelative("hasValue");
            var valueProperty = property.FindPropertyRelative("value");
            if (hasValueProperty == null || valueProperty == null)
            {
                EditorGUI.LabelField(position, "SerializableNullable (fields not found)");
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
                return;
            }

            var toggleRect = new Rect(position.x, position.y, ToggleWidth, position.height);
            var valueRect = new Rect(
                position.x + ToggleWidth + Spacing,
                position.y,
                position.width - ToggleWidth - Spacing,
                position.height);

            hasValueProperty.boolValue = EditorGUI.Toggle(toggleRect, hasValueProperty.boolValue);

            using (new EditorGUI.DisabledScope(!hasValueProperty.boolValue))
            {
                if (hasValueProperty.boolValue)
                {
                    EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);
                }
                else
                {
                    EditorGUI.LabelField(valueRect, "null");
                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
