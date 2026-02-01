using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// DateTime型のカスタムPropertyDrawer。
    /// Inspector上でDateTime型のフィールドを編集可能にする。
    /// </summary>
    [CustomPropertyDrawer(typeof(DateTime))]
    public class DateTimePropertyDrawer : PropertyDrawer
    {
        private const float DateFieldWidth = 80f;
        private const float TimeFieldWidth = 60f;
        private const float LabelWidth = 30f;
        private const float Spacing = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, label);

            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var ticksProperty = property.FindPropertyRelative("ticks");
            var dateKindProperty = property.FindPropertyRelative("dateData");

            if (ticksProperty == null && dateKindProperty == null)
            {
                DrawFallbackField(position, property);
                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
                return;
            }

            var currentDateTime = GetDateTime(property);

            EditorGUI.BeginChangeCheck();

            var x = position.x;

            var yearRect = new Rect(x, position.y, DateFieldWidth, position.height);
            x += DateFieldWidth + Spacing;

            var monthLabelRect = new Rect(x, position.y, LabelWidth, position.height);
            x += LabelWidth;
            var monthRect = new Rect(x, position.y, 30f, position.height);
            x += 30f + Spacing;

            var dayLabelRect = new Rect(x, position.y, LabelWidth, position.height);
            x += LabelWidth;
            var dayRect = new Rect(x, position.y, 30f, position.height);
            x += 30f + Spacing;

            var hourRect = new Rect(x, position.y, 30f, position.height);
            x += 30f;
            var colonRect1 = new Rect(x, position.y, 10f, position.height);
            x += 10f;
            var minuteRect = new Rect(x, position.y, 30f, position.height);
            x += 30f;
            var colonRect2 = new Rect(x, position.y, 10f, position.height);
            x += 10f;
            var secondRect = new Rect(x, position.y, 30f, position.height);

            var year = EditorGUI.IntField(yearRect, currentDateTime.Year);
            EditorGUI.LabelField(monthLabelRect, "/");
            var month = EditorGUI.IntField(monthRect, currentDateTime.Month);
            EditorGUI.LabelField(dayLabelRect, "/");
            var day = EditorGUI.IntField(dayRect, currentDateTime.Day);

            var hour = EditorGUI.IntField(hourRect, currentDateTime.Hour);
            EditorGUI.LabelField(colonRect1, ":");
            var minute = EditorGUI.IntField(minuteRect, currentDateTime.Minute);
            EditorGUI.LabelField(colonRect2, ":");
            var second = EditorGUI.IntField(secondRect, currentDateTime.Second);

            if (EditorGUI.EndChangeCheck())
            {
                year = Mathf.Clamp(year, 1, 9999);
                month = Mathf.Clamp(month, 1, 12);
                var maxDay = DateTime.DaysInMonth(year, month);
                day = Mathf.Clamp(day, 1, maxDay);
                hour = Mathf.Clamp(hour, 0, 23);
                minute = Mathf.Clamp(minute, 0, 59);
                second = Mathf.Clamp(second, 0, 59);

                try
                {
                    var newDateTime = new DateTime(year, month, day, hour, minute, second, currentDateTime.Kind);
                    SetDateTime(property, newDateTime);
                }
                catch (ArgumentOutOfRangeException)
                {
                    Debug.LogWarning("無効な日時が入力されました。");
                }
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private DateTime GetDateTime(SerializedProperty property)
        {
            var dateDataProperty = property.FindPropertyRelative("dateData");
            if (dateDataProperty != null)
            {
                var dateData = (ulong)dateDataProperty.longValue;
                var ticks = (long)(dateData & 0x3FFFFFFFFFFFFFFF);
                return new DateTime(ticks);
            }

            var ticksProperty = property.FindPropertyRelative("ticks");
            if (ticksProperty != null)
                return new DateTime(ticksProperty.longValue);

            return DateTime.MinValue;
        }

        private void SetDateTime(SerializedProperty property, DateTime dateTime)
        {
            var dateDataProperty = property.FindPropertyRelative("dateData");
            if (dateDataProperty != null)
            {
                var ticks = dateTime.Ticks;
                var kind = (ulong)dateTime.Kind << 62;
                dateDataProperty.longValue = (long)(((ulong)ticks) | kind);
                return;
            }

            var ticksProperty = property.FindPropertyRelative("ticks");
            if (ticksProperty != null)
                ticksProperty.longValue = dateTime.Ticks;
        }

        private void DrawFallbackField(Rect position, SerializedProperty property)
        {
            EditorGUI.LabelField(position, "DateTime (unsupported serialization)");
        }
    }

    /// <summary>
    /// DateTime型に関するユーティリティ。
    /// </summary>
    public static class DateTimeEditorUtility
    {
        /// <summary>
        /// SerializedPropertyからDateTimeを取得する。
        /// </summary>
        public static DateTime GetDateTime(SerializedProperty property)
        {
            if (property == null)
                return DateTime.MinValue;

            var dateDataProperty = property.FindPropertyRelative("dateData");
            if (dateDataProperty != null)
            {
                var dateData = (ulong)dateDataProperty.longValue;
                var ticks = (long)(dateData & 0x3FFFFFFFFFFFFFFF);
                if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                    return DateTime.MinValue;
                return new DateTime(ticks);
            }

            var ticksProperty = property.FindPropertyRelative("ticks");
            if (ticksProperty != null)
            {
                var ticks = ticksProperty.longValue;
                if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
                    return DateTime.MinValue;
                return new DateTime(ticks);
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// DateTimeをSerializedPropertyに設定する。
        /// </summary>
        public static void SetDateTime(SerializedProperty property, DateTime dateTime)
        {
            if (property == null)
                return;

            var dateDataProperty = property.FindPropertyRelative("dateData");
            if (dateDataProperty != null)
            {
                var ticks = dateTime.Ticks;
                var kind = (ulong)dateTime.Kind << 62;
                dateDataProperty.longValue = (long)(((ulong)ticks) | kind);
                return;
            }

            var ticksProperty = property.FindPropertyRelative("ticks");
            if (ticksProperty != null)
                ticksProperty.longValue = dateTime.Ticks;
        }

        /// <summary>
        /// DateTimeがサポートされているかどうかを判定する。
        /// </summary>
        public static bool IsDateTimeProperty(SerializedProperty property)
        {
            if (property == null)
                return false;

            return property.FindPropertyRelative("dateData") != null ||
                   property.FindPropertyRelative("ticks") != null;
        }

        /// <summary>
        /// DateTimeを表示用の文字列に変換する。
        /// </summary>
        public static string FormatDateTime(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
                return "(未設定)";
            return dateTime.ToString("yyyy/MM/dd HH:mm:ss");
        }

        /// <summary>
        /// 文字列からDateTimeをパースする。
        /// </summary>
        public static bool TryParse(string value, out DateTime result)
        {
            var formats = new[]
            {
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm:ss",
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "MM/dd/yyyy HH:mm:ss",
                "MM/dd/yyyy",
                "dd/MM/yyyy HH:mm:ss",
                "dd/MM/yyyy",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ssZ",
                "o"
            };

            return DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result) ||
                DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
        }
    }
}
