using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using OpenApparatus.Unity.Editor.Inspectors;

namespace OpenApparatus.Unity.Editor.Importers
{
    [CustomEditor(typeof(JsonEnvironmentImporter))]
    public sealed class JsonEnvironmentImporterEditor : ScriptedImporterEditor
    {
        SerializedProperty _colliderMode;
        SerializedProperty _substitution;

        public override void OnEnable()
        {
            base.OnEnable();
            _colliderMode = serializedObject.FindProperty(nameof(JsonEnvironmentImporter.ColliderMode));
            _substitution = serializedObject.FindProperty(nameof(JsonEnvironmentImporter.Substitution));
        }

        public override void OnInspectorGUI()
        {
            var importer = (JsonEnvironmentImporter)target;
            var asset = AssetDatabase.LoadAssetAtPath<ApparatusAsset>(importer.assetPath);

            DrawSummary(asset);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn options", EditorStyles.boldLabel);

            serializedObject.Update();
            EditorGUILayout.PropertyField(_colliderMode);
            EditorGUILayout.PropertyField(_substitution);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.HelpBox(
                "Collider mode and substitution apply on reimport (Apply below). " +
                "Create Apparatus is the recommended way to build a configurable, " +
                "re-skinnable apparatus.",
                MessageType.Info);

            EditorGUILayout.Space();
            ApplyRevertGUI();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(asset == null))
            {
                if (GUILayout.Button("Create Apparatus", GUILayout.Height(28)))
                    ApparatusWizardOverlay.CreateFor(importer.assetPath, asset);
            }
        }

        static void DrawSummary(ApparatusAsset asset)
        {
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            if (asset == null)
            {
                EditorGUILayout.HelpBox("No imported asset (file may have failed to parse).",
                    MessageType.Warning);
                return;
            }
            EditorGUILayout.LabelField("Schema version", asset.SchemaVersion.ToString());
            EditorGUILayout.LabelField("Rooms", (asset.Rooms?.Length ?? 0).ToString());
            int objectCount = 0;
            if (asset.Rooms != null)
                foreach (var r in asset.Rooms) objectCount += r.Objects?.Length ?? 0;
            EditorGUILayout.LabelField("Objects", objectCount.ToString());
            EditorGUILayout.LabelField("Object slots", (asset.ObjectSlots?.Length ?? 0).ToString());
            if (asset.Parameters != null)
            {
                EditorGUILayout.LabelField($"Tile size: {asset.Parameters.TileSize:0.##}");
                EditorGUILayout.LabelField(
                    $"Wall: {asset.Parameters.WallThickness:0.##} x {asset.Parameters.WallHeight:0.##}");
            }

            if (asset.ObjectSlots != null && asset.ObjectSlots.Length > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Object types in this environment",
                    EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Use the bold type name on the left as the 'Object Type' " +
                    "value in a PrefabSubstitutionTable entry.",
                    MessageType.None);
                foreach (var slot in asset.ObjectSlots)
                {
                    var label = !string.IsNullOrEmpty(slot.ObjectType)
                        ? slot.ObjectType
                        : (slot.DisplayName ?? $"slot {slot.Id}");
                    var meta = $"slot {slot.Id} · {slot.Shape ?? "?"} · size {slot.Size:0.##}";
                    EditorGUILayout.LabelField(label, meta);
                }
            }
        }
    }
}
