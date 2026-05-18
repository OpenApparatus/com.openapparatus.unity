using UnityEditor;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Inspectors
{
    // Bottom "Imported Object" section of the .oae inspector.
    // Unity locks this entire section read-only to prevent edits that would
    // be lost on reimport. Spawn options + Spawn button therefore live on
    // JsonEnvironmentImporterEditor (the top "Import Settings" section), not
    // here. This editor stays as a passive summary.
    [CustomEditor(typeof(ApparatusAsset))]
    public sealed class ApparatusAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var asset = (ApparatusAsset)target;

            EditorGUILayout.LabelField("Schema version", asset.SchemaVersion.ToString());
            EditorGUILayout.LabelField("Rooms", (asset.Rooms?.Length ?? 0).ToString());
            int objectCount = 0;
            if (asset.Rooms != null)
                foreach (var r in asset.Rooms) objectCount += r.Objects?.Length ?? 0;
            EditorGUILayout.LabelField("Objects", objectCount.ToString());
            EditorGUILayout.LabelField("Object slots", (asset.ObjectSlots?.Length ?? 0).ToString());
            EditorGUILayout.LabelField("Collider mode", asset.ColliderMode.ToString());
            EditorGUILayout.LabelField("Substitution table",
                asset.Substitution != null ? asset.Substitution.name : "(none)");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Edit spawn options and trigger spawn via the Import Settings " +
                "section above (top of inspector).",
                MessageType.Info);
        }
    }
}
