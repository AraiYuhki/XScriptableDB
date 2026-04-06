using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Xeon.XScriptableDB;

namespace Xeon.XScriptableDB.Editor
{
    public class GroupReferenceInspector : PropertyDrawer
    {
        protected void Draw<T>(Rect position, SerializedProperty property, GUIContent label, List<T> data) where T : IGroupIdentifiable
        {
            if (property.propertyType != SerializedPropertyType.Integer)
                throw new Exception("This attribute can't attach for this type isn't int type property or field.");

            var selectedId = property.intValue;
            position.width *= 0.333f;

            EditorGUI.LabelField(position, label);

            position.x += position.width;

            EditorGUI.BeginChangeCheck();
            var indexies = new int[] { -1 }.Concat(data.Select(row => row.GroupId).Distinct()).ToArray();
            var elements = indexies.Select(id =>
            {
                if (id <= 0) return "None";
                var row = data.FirstOrDefault(row => row.GroupId == id);
                return $"GroupId{id}:{row.Name}";
            }).ToArray();

            selectedId = EditorGUI.IntPopup(position, selectedId, elements, indexies);
            position.x += position.width;
            using (new EditorGUI.DisabledGroupScope(true))
                EditorGUI.LabelField(position, selectedId.ToString());
            if (EditorGUI.EndChangeCheck())
                property.intValue = selectedId;
        }
    }
}
