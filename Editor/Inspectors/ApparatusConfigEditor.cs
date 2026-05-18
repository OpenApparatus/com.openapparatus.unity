using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Editor.Inspectors
{
    [CustomEditor(typeof(ApparatusConfig))]
    public sealed class ApparatusConfigEditor : UnityEditor.Editor
    {
        static readonly string[] Modes = { "Rooms", "Object Types" };

        ReorderableList _roomList;
        ReorderableList _objectList;
        int _mode;

        void OnEnable()
        {
            _roomList = MakeList("Rooms", "Room");
            _objectList = MakeList("ObjectTypes", "Object Type");
        }

        ReorderableList MakeList(string propertyName, string itemLabel)
        {
            var prop = serializedObject.FindProperty(propertyName);
            var list = new ReorderableList(serializedObject, prop,
                draggable: false, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);

            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"{itemLabel}s");
            list.elementHeightCallback = index =>
                EditorGUI.GetPropertyHeight(prop.GetArrayElementAtIndex(index)) + 4f;
            list.drawElementCallback = (rect, index, active, focused) =>
            {
                var element = prop.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.x += 12f;
                rect.width -= 12f;
                EditorGUI.PropertyField(rect, element,
                    new GUIContent(ElementLabel(element, itemLabel, index)), true);
            };
            return list;
        }

        static string ElementLabel(SerializedProperty element, string itemLabel, int index)
        {
            var roomId = element.FindPropertyRelative("RoomId");
            if (roomId != null)
            {
                var name = element.FindPropertyRelative("Name");
                string suffix = name != null && !string.IsNullOrEmpty(name.stringValue)
                    ? $" - {name.stringValue}" : "";
                return $"Room {roomId.intValue}{suffix}";
            }
            var type = element.FindPropertyRelative("ObjectType");
            if (type != null && !string.IsNullOrEmpty(type.stringValue))
                return type.stringValue;
            return $"{itemLabel} {index}";
        }

        public override void OnInspectorGUI()
        {
            var config = (ApparatusConfig)target;

            // Generate the room / object-type lists from the source the first
            // time one is assigned.
            if (config.Source != null && (config.Rooms == null || config.Rooms.Length == 0))
                SyncFromSource(config);

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            DrawPropertiesExcluding(serializedObject, "m_Script", "Rooms", "ObjectTypes");

            EditorGUILayout.Space();
            _mode = GUILayout.Toolbar(_mode, Modes);
            (_mode == 0 ? _roomList : _objectList).DoLayoutList();

            bool changed = EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(config.Source == null))
            {
                if (GUILayout.Button("Sync Rooms / Object Types from Source"))
                {
                    SyncFromSource(config);
                    serializedObject.Update();
                }
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

            // Debounced regenerate: rebuild once the inspector settles.
            if (changed && config.Source != null)
                ScheduleRegenerate(config);
        }

        // Ensures one RoomConfig per source room and one ObjectTypeConfig per
        // source object slot, preserving any entries already configured.
        static void SyncFromSource(ApparatusConfig config)
        {
            if (config.Source == null) return;

            var existingRooms = new Dictionary<int, RoomConfig>();
            if (config.Rooms != null)
                foreach (var rc in config.Rooms)
                    if (rc != null) existingRooms[rc.RoomId] = rc;
            var rooms = new List<RoomConfig>();
            if (config.Source.Rooms != null)
                foreach (var rd in config.Source.Rooms)
                    rooms.Add(existingRooms.TryGetValue(rd.Id, out var e)
                        ? e : new RoomConfig { RoomId = rd.Id });
            config.Rooms = rooms.ToArray();

            var existingTypes = new Dictionary<string, ObjectTypeConfig>();
            if (config.ObjectTypes != null)
                foreach (var oc in config.ObjectTypes)
                    if (oc != null && !string.IsNullOrEmpty(oc.ObjectType))
                        existingTypes[oc.ObjectType] = oc;
            var types = new List<ObjectTypeConfig>();
            if (config.Source.ObjectSlots != null)
                foreach (var slot in config.Source.ObjectSlots)
                {
                    var type = !string.IsNullOrEmpty(slot.ObjectType)
                        ? slot.ObjectType : slot.DisplayName;
                    if (string.IsNullOrEmpty(type)) continue;
                    types.Add(existingTypes.TryGetValue(type, out var e)
                        ? e : new ObjectTypeConfig { ObjectType = type });
                }
            config.ObjectTypes = types.ToArray();

            EditorUtility.SetDirty(config);
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
