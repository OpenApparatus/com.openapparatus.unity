using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Editor.Inspectors
{
    [CustomEditor(typeof(OApparatusConfig))]
    public sealed class OApparatusConfigEditor : UnityEditor.Editor
    {
        static readonly string[] Tabs = { "Apparatus", "Rooms", "Object Types" };

        ReorderableList _roomList;
        ReorderableList _objectList;
        int _tab;

        void OnEnable()
        {
            _roomList = MakeList("Rooms", "OApparatusRoomManager");
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
                return $"OApparatusRoomManager {roomId.intValue}{suffix}";
            }
            var type = element.FindPropertyRelative("ObjectType");
            if (type != null && !string.IsNullOrEmpty(type.stringValue))
                return type.stringValue;
            return $"{itemLabel} {index}";
        }

        public override void OnInspectorGUI()
        {
            var config = (OApparatusConfig)target;

            if (config.Source != null && (config.Rooms == null || config.Rooms.Length == 0))
                SyncFromSource(config);

            serializedObject.Update();

            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
            EditorGUILayout.LabelField(config.name, titleStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Source Apparatus", EditorStyles.miniLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("Source"), GUIContent.none);
                }
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("Output Prefab", EditorStyles.miniLabel);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("GeneratedPrefab"), GUIContent.none);
                }
            }
            EditorGUILayout.Space(6);

            EditorGUI.BeginChangeCheck();

            _tab = GUILayout.Toolbar(_tab, Tabs);
            EditorGUILayout.Space(4);
            switch (_tab)
            {
                case 0:
                    DrawPropertiesExcluding(serializedObject,
                        "m_Script", "Source", "GeneratedPrefab", "Rooms", "ObjectTypes");
                    break;
                case 1: _roomList.DoLayoutList(); break;
                case 2: _objectList.DoLayoutList(); break;
            }

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
                    OApparatusPrefabBuilder.Regenerate(config);
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

            if (changed && config.Source != null)
                ScheduleRegenerate(config);
        }

        // Ensures one OApparatusRoomConfig per source room and one OApparatusObjectTypeConfig per
        // source object slot, preserving any entries already configured.
        static void SyncFromSource(OApparatusConfig config)
        {
            if (config.Source == null) return;

            var existingRooms = new Dictionary<int, OApparatusRoomConfig>();
            if (config.Rooms != null)
                foreach (var rc in config.Rooms)
                    if (rc != null) existingRooms[rc.RoomId] = rc;
            var rooms = new List<OApparatusRoomConfig>();
            if (config.Source.Rooms != null)
                foreach (var rd in config.Source.Rooms)
                    rooms.Add(existingRooms.TryGetValue(rd.Id, out var e)
                        ? e : new OApparatusRoomConfig { RoomId = rd.Id });
            config.Rooms = rooms.ToArray();

            var existingTypes = new Dictionary<string, OApparatusObjectTypeConfig>();
            if (config.ObjectTypes != null)
                foreach (var oc in config.ObjectTypes)
                    if (oc != null && !string.IsNullOrEmpty(oc.ObjectType))
                        existingTypes[oc.ObjectType] = oc;
            var types = new List<OApparatusObjectTypeConfig>();
            if (config.Source.ObjectSlots != null)
                foreach (var slot in config.Source.ObjectSlots)
                {
                    var type = !string.IsNullOrEmpty(slot.ObjectType)
                        ? slot.ObjectType : slot.DisplayName;
                    if (string.IsNullOrEmpty(type)) continue;
                    types.Add(existingTypes.TryGetValue(type, out var e)
                        ? e : new OApparatusObjectTypeConfig { ObjectType = type });
                }
            config.ObjectTypes = types.ToArray();

            EditorUtility.SetDirty(config);
        }

        static OApparatusConfig s_pending;

        static void ScheduleRegenerate(OApparatusConfig config)
        {
            s_pending = config;
            EditorApplication.delayCall -= DeferredRegenerate;
            EditorApplication.delayCall += DeferredRegenerate;
        }

        static void DeferredRegenerate()
        {
            if (s_pending != null) OApparatusPrefabBuilder.Regenerate(s_pending);
            s_pending = null;
        }

        static void SpawnIntoScene(OApparatusConfig config)
        {
            if (config.GeneratedPrefab == null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(config.GeneratedPrefab);
            if (instance == null) return;
            Undo.RegisterCreatedObjectUndo(instance, "Spawn Apparatus");
            Selection.activeGameObject = instance;
        }
    }
}
