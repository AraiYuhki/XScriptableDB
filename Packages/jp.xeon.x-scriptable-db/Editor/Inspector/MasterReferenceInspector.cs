using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Editor
{
    public class MasterReferenceInspector : PropertyDrawer
    {
        protected void Draw<T>(Rect position, SerializedProperty property, GUIContent label, List<T> data) where T : IIdentifiable
        {
            if (property.propertyType != SerializedPropertyType.Integer)
                throw new Exception("This attribute can't attach for this type isn't int type property or field.");
            var selectedId = property.intValue;
            position.width *= 0.333f;
            EditorGUI.LabelField(position, label);
            position.x += position.width;
            EditorGUI.BeginChangeCheck();
            var indexies = new int[] { -1 }.Concat(data.Select(row => row.Id)).ToArray();
            var elements = new string[] { "None" }.Concat(data.Select(row => $"ID{row.Id}:{row.Name}")).ToArray();
            selectedId = EditorGUI.IntPopup(position, selectedId, elements, indexies);
            position.x += position.width;
            using (new EditorGUI.DisabledGroupScope(true))
                EditorGUI.LabelField(position, selectedId.ToString());
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = selectedId;
            }
        }
    }
}
