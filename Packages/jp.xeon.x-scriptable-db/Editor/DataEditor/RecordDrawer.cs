using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// IXRecordを実装するレコード用のカスタムPropertyDrawer。
    /// PrimaryKeyフィールドをハイライトし、より使いやすいUIを提供します。
    /// </summary>
    public static class RecordDrawerUtility
    {
        private static GUIStyle primaryKeyStyle;
        private static GUIStyle primaryKeyLabelStyle;

        /// <summary>
        /// SerializedPropertyからレコードを描画します。
        /// </summary>
        /// <param name="position">描画位置</param>
        /// <param name="property">プロパティ</param>
        /// <param name="label">ラベル</param>
        /// <param name="recordType">レコードの型</param>
        public static void DrawRecord(Rect position, SerializedProperty property, GUIContent label, Type recordType)
        {
            InitializeStyles();

            var primaryKeyInfo = FindPrimaryKeyMember(recordType);

            EditorGUI.BeginProperty(position, label, property);

            var currentPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            property.isExpanded = EditorGUI.Foldout(currentPosition, property.isExpanded, label, true);
            currentPosition.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var iterator = property.Copy();
                var enterChildren = true;
                var depth = iterator.depth;

                while (iterator.NextVisible(enterChildren) && iterator.depth > depth)
                {
                    enterChildren = false;
                    currentPosition.height = EditorGUI.GetPropertyHeight(iterator, true);

                    var isPrimaryKey = primaryKeyInfo != null && iterator.name == primaryKeyInfo.Name;
                    if (isPrimaryKey)
                        DrawPrimaryKeyField(currentPosition, iterator);
                    else
                        EditorGUI.PropertyField(currentPosition, iterator, true);

                    currentPosition.y += currentPosition.height + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// レコードの高さを計算します。
        /// </summary>
        /// <param name="property">プロパティ</param>
        /// <param name="label">ラベル</param>
        /// <returns>高さ</returns>
        public static float GetRecordHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (!property.isExpanded)
                return height;

            var iterator = property.Copy();
            var enterChildren = true;
            var depth = iterator.depth;

            while (iterator.NextVisible(enterChildren) && iterator.depth > depth)
            {
                enterChildren = false;
                height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        /// <summary>
        /// レコードのサマリー文字列を取得します。
        /// </summary>
        /// <param name="property">プロパティ</param>
        /// <param name="recordType">レコードの型</param>
        /// <returns>サマリー文字列</returns>
        public static string GetRecordSummary(SerializedProperty property, Type recordType)
        {
            var primaryKeyInfo = FindPrimaryKeyMember(recordType);

            if (primaryKeyInfo == null)
                return GetFirstFieldSummary(property);

            var pkProperty = property.FindPropertyRelative(primaryKeyInfo.Name);
            if (pkProperty == null)
                return GetFirstFieldSummary(property);

            var keyValue = GetPropertyValueString(pkProperty);
            return $"[{primaryKeyInfo.Name}: {keyValue}]";
        }

        private static void InitializeStyles()
        {
            if (primaryKeyStyle != null)
                return;

            primaryKeyStyle = new GUIStyle(EditorStyles.textField)
            {
                fontStyle = FontStyle.Bold
            };

            primaryKeyLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.7f, 0.2f) }
            };
        }

        private static void DrawPrimaryKeyField(Rect position, SerializedProperty property)
        {
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            var fieldRect = new Rect(
                position.x + EditorGUIUtility.labelWidth,
                position.y,
                position.width - EditorGUIUtility.labelWidth,
                position.height);

            EditorGUI.LabelField(labelRect, new GUIContent($"★ {property.displayName}", "PrimaryKey"), primaryKeyLabelStyle);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
            EditorGUI.EndChangeCheck();
        }

        private static MemberInfo FindPrimaryKeyMember(Type type)
        {
            if (type == null)
                return null;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (var field in type.GetFields(flags))
            {
                if (field.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                    return field;
            }

            foreach (var property in type.GetProperties(flags))
            {
                if (property.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                    return property;
            }

            return null;
        }

        private static string GetFirstFieldSummary(SerializedProperty property)
        {
            var iterator = property.Copy();
            if (iterator.NextVisible(true))
                return $"{iterator.name}: {GetPropertyValueString(iterator)}";
            return "(empty)";
        }

        private static string GetPropertyValueString(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Generic)
            {
                if (DateTimeEditorUtility.IsDateTimeProperty(property))
                {
                    var dateTime = DateTimeEditorUtility.GetDateTime(property);
                    return DateTimeEditorUtility.FormatDateTime(dateTime);
                }
            }

            return property.propertyType switch
            {
                SerializedPropertyType.Integer => property.intValue.ToString(),
                SerializedPropertyType.Float => property.floatValue.ToString("F2"),
                SerializedPropertyType.String => string.IsNullOrEmpty(property.stringValue)
                    ? "(empty)"
                    : property.stringValue.Length > 20
                        ? property.stringValue.Substring(0, 20) + "..."
                        : property.stringValue,
                SerializedPropertyType.Boolean => property.boolValue.ToString(),
                SerializedPropertyType.Enum => property.enumDisplayNames.Length > property.enumValueIndex
                    ? property.enumDisplayNames[property.enumValueIndex]
                    : property.enumValueIndex.ToString(),
                SerializedPropertyType.ObjectReference => property.objectReferenceValue != null
                    ? property.objectReferenceValue.name
                    : "(null)",
                SerializedPropertyType.Vector2 => property.vector2Value.ToString(),
                SerializedPropertyType.Vector3 => property.vector3Value.ToString(),
                _ => "..."
            };
        }
    }
}
