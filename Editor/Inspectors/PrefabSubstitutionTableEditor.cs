using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Inspectors
{
    [CustomEditor(typeof(PrefabSubstitutionTable))]
    public sealed class PrefabSubstitutionTableEditor : UnityEditor.Editor
    {
        ReorderableList _list;
        SerializedProperty _entriesProp;

        void OnEnable()
        {
            _entriesProp = serializedObject.FindProperty(nameof(PrefabSubstitutionTable.Entries));
            _list = new ReorderableList(serializedObject, _entriesProp,
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            _list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "Substitutions");

            _list.elementHeightCallback = index =>
                EditorGUIUtility.singleLineHeight * 5 + 12;

            _list.drawElementCallback = (rect, index, _isActive, _isFocused) =>
            {
                var element = _entriesProp.GetArrayElementAtIndex(index);
                float h = EditorGUIUtility.singleLineHeight;
                float pad = 2f;
                rect.y += pad;

                DrawField(ref rect, element, "ObjectType", h, pad);
                DrawField(ref rect, element, "Prefab", h, pad);
                DrawField(ref rect, element, "PositionOffset", h, pad);
                DrawField(ref rect, element, "RotationOffsetYDegrees", h, pad);
                DrawField(ref rect, element, "ScaleMultiplier", h, pad);
            };

            _list.onAddCallback = list =>
            {
                int newIndex = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = newIndex;
                var element = list.serializedProperty.GetArrayElementAtIndex(newIndex);
                element.FindPropertyRelative("ScaleMultiplier").vector3Value = Vector3.one;
            };
        }

        static void DrawField(ref Rect rect, SerializedProperty element,
                              string fieldName, float h, float pad)
        {
            var r = new Rect(rect.x, rect.y, rect.width, h);
            EditorGUI.PropertyField(r, element.FindPropertyRelative(fieldName));
            rect.y += h + pad;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
