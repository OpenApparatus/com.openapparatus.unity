using UnityEditor;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Editor.Inspectors
{
    [CustomEditor(typeof(MultiRoomEnvironmentAsset))]
    public sealed class MultiRoomEnvironmentAssetEditor : UnityEditor.Editor
    {
        SerializedProperty _colliderMode;
        SerializedProperty _substitution;

        void OnEnable()
        {
            _colliderMode = serializedObject.FindProperty(nameof(MultiRoomEnvironmentAsset.ColliderMode));
            _substitution = serializedObject.FindProperty(nameof(MultiRoomEnvironmentAsset.Substitution));
        }

        public override void OnInspectorGUI()
        {
            var asset = (MultiRoomEnvironmentAsset)target;

            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
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
                EditorGUILayout.LabelField($"Wall: {asset.Parameters.WallThickness:0.##} x {asset.Parameters.WallHeight:0.##}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn options", EditorStyles.boldLabel);
            serializedObject.Update();
            EditorGUILayout.PropertyField(_colliderMode);
            EditorGUILayout.PropertyField(_substitution);
            EditorGUILayout.HelpBox(
                "Collider mode and substitution table take effect on next spawn.",
                MessageType.Info);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            if (GUILayout.Button("Spawn into scene", GUILayout.Height(28)))
            {
                var root = EnvironmentSpawner.Spawn(asset);
                if (root != null) Selection.activeGameObject = root;
            }
        }
    }
}
