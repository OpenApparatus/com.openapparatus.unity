using UnityEditor;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Editor.Inspectors
{
    [CustomEditor(typeof(ApparatusConfig))]
    public sealed class ApparatusConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (ApparatusConfig)target;

            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(config.Source == null))
            {
                if (GUILayout.Button("Regenerate Prefab", GUILayout.Height(28)))
                    ApparatusPrefabBuilder.Regenerate(config);

                using (new EditorGUI.DisabledScope(config.GeneratedPrefab == null))
                {
                    if (GUILayout.Button("Spawn into Scene"))
                        SpawnIntoScene(config);
                }
            }

            if (config.Source == null)
                EditorGUILayout.HelpBox(
                    "Assign a Source apparatus (.oae / .oapp) to generate a prefab.",
                    MessageType.Info);

            // Debounced regenerate: rebuild once the inspector settles rather
            // than on every keystroke.
            if (changed && config.Source != null)
                ScheduleRegenerate(config);
        }

        static ApparatusConfig s_pending;

        static void ScheduleRegenerate(ApparatusConfig config)
        {
            s_pending = config;
            EditorApplication.delayCall -= DeferredRegenerate;
            EditorApplication.delayCall += DeferredRegenerate;
        }

        static void DeferredRegenerate()
        {
            if (s_pending != null) ApparatusPrefabBuilder.Regenerate(s_pending);
            s_pending = null;
        }

        static void SpawnIntoScene(ApparatusConfig config)
        {
            var go = new GameObject(config.name);
            var manager = go.AddComponent<ApparatusManager>();
            manager.Config = config;
            manager.Spawn();
            Undo.RegisterCreatedObjectUndo(go, "Spawn Apparatus");
            Selection.activeGameObject = go;
        }
    }
}
