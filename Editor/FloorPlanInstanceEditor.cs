using UnityEngine;
using UnityEditor;
using OpenApparatus.Unity;

namespace OpenApparatus.Unity.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="FloorPlanInstance"/>. Adds a Generate /
    /// Reseed / Clear button row above the default field listing.
    /// </summary>
    [CustomEditor(typeof(FloorPlanInstance))]
    public class FloorPlanInstanceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var inst = (FloorPlanInstance)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(inst.gameObject, "Generate Floor Plan");
                    inst.Regenerate();
                }
                if (GUILayout.Button("Reseed"))
                {
                    Undo.RecordObject(inst, "Reseed Floor Plan");
                    inst.seed = UnityEngine.Random.Range(0, int.MaxValue);
                    EditorUtility.SetDirty(inst);
                    inst.Regenerate();
                }
                if (GUILayout.Button("Clear"))
                {
                    inst.ClearGenerated();
                }
            }

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
