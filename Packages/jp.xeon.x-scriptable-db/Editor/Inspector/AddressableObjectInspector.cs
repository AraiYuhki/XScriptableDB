using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    [CustomPropertyDrawer(typeof(AddressableObjectAttribute))]
    public class AddressableObjectInspector : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var originalWidth = position.width;
            position.width = originalWidth * 0.7f;
            EditorGUI.PropertyField(position, property);

            position.x += position.width + 5f;
            position.width = originalWidth - position.width - 5f;
            if (property.objectReferenceValue == null)
            {
                EditorGUI.LabelField(position, "Null");
                return;
            }
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 50;
#if UNITY_6000_0_OR_NEWER
            var assetPath = AssetDatabase.GetAssetPath(property.objectReferenceValue);
#else
            var assetPath = AssetDatabase.GetAssetPath(property.objectReferenceInstanceIDValue);
#endif
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
            if (!TryGetAddressProperty(property, out var addressProperty))
            {
                Debug.LogError($"No Address property found corresponding to {property.propertyPath}");
                return;
            }
            addressProperty.stringValue = entry?.address;
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, addressProperty, new GUIContent("address"));
            EditorGUI.EndDisabledGroup();
            EditorGUIUtility.labelWidth = labelWidth;
        }

        private bool IsArrayProperty(string propertyPath)
            => Regex.IsMatch(propertyPath, @"\.Array\.data\[[0-9]\]$");

        private bool TryGetAddressPropertyPath(SerializedProperty property, out string result)
        {
            if (!IsArrayProperty(property.propertyPath))
            {
                result = property.propertyPath + "Address";
                return true;
            }
            result = string.Empty;
            var regex = new Regex(@"\.(?<propertyName>\w+)\.Array\.");
            if (!regex.IsMatch(property.propertyPath))
                return false;
            var propertyName = regex.Match(property.propertyPath).Groups["propertyName"].Value;
            result = property.propertyPath.Replace(propertyName, $"{propertyName}Address");
            return true;
        }

        private bool TryGetAddressProperty(SerializedProperty targetProperty, out SerializedProperty result)
        {
            result = null;
            if (!TryGetAddressPropertyPath(targetProperty, out var addressPath))
            {
                Debug.LogError("Failed to generate address path");
                return false;
            }

            result = targetProperty.serializedObject.FindProperty(addressPath);
            if (result != null)
                return true;

            var replaceRegex = new Regex(@"\.Array\.data\[(?<index>[0-9]+)\]$");
            if (!replaceRegex.IsMatch(targetProperty.propertyPath))
            {
                Debug.LogError($"Could not detect address path from {targetProperty.propertyPath}");
                return false;
            }

            var arrayAddressPath = replaceRegex.Replace(targetProperty.propertyPath, "Address");
            var arrayProperty = targetProperty.serializedObject.FindProperty(arrayAddressPath);
            if (arrayProperty == null)
            {
                Debug.LogError($"{arrayAddressPath} not found from {targetProperty.propertyPath}");
                return false;
            }

            var match = replaceRegex.Match(targetProperty.propertyPath);
            if (!int.TryParse(match.Groups["index"].Value, out var index))
            {
                Debug.LogError($"Could not parse {match.Groups["index"].Value}");
                return false;
            }

            arrayProperty.InsertArrayElementAtIndex(index);
            result = targetProperty.serializedObject.FindProperty(addressPath);
            return true;
        }
    }
}
