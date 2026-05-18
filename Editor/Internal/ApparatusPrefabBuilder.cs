using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    /// <summary>
    /// Generates an environment prefab from an <see cref="ApparatusConfig"/>:
    /// builds the GameObject tree through the shared spawn pipeline, layers the
    /// config's per-room and per-object settings on top, fills door/window
    /// openings with prefabs, optionally recentres on a chosen room, then saves
    /// the result as a prefab asset next to the config.
    /// </summary>
    public static class ApparatusPrefabBuilder
    {
        /// <summary>Builds a scene instance from the config (caller owns it).</summary>
        public static GameObject BuildInstance(ApparatusConfig config)
        {
            if (config == null || config.Source == null) return null;

            var options = new EnvironmentBuildOptions
            {
                GenerateFloors = config.GenerateFloors,
                GenerateCeilings = config.GenerateCeilings,
                GenerateWalls = config.GenerateWalls,
                GenerateExteriorWalls = config.GenerateExteriorWalls,
            };
            var root = EnvironmentSpawner.Spawn(config.Source, options);
            if (root == null) return null;

            ApplyRoomConfigs(root, config);
            FillOpenings(root, config);
            ApplyObjectConfigs(root, config);
            RecentreOnOriginRoom(root, config);
            return root;
        }

        /// <summary>
        /// Rebuilds the config's prefab asset and points
        /// <see cref="ApparatusConfig.GeneratedPrefab"/> at it. Returns the prefab.
        /// </summary>
        public static GameObject Regenerate(ApparatusConfig config)
        {
            if (config == null || config.Source == null) return null;

            var apparatus = BuildInstance(config);
            if (apparatus == null) return null;

            // The prefab is a self-contained, scene-droppable handle: a root
            // carrying ApparatusManager with the apparatus geometry as a child.
            var prefabRoot = new GameObject(config.name);
            prefabRoot.AddComponent<ApparatusManager>().Config = config;
            apparatus.name = "Apparatus";
            apparatus.transform.SetParent(prefabRoot.transform, worldPositionStays: false);

            // Saved alongside the source apparatus asset.
            string sourcePath = AssetDatabase.GetAssetPath(config.Source);
            string directory = string.IsNullOrEmpty(sourcePath)
                ? "Assets"
                : Path.GetDirectoryName(sourcePath);
            string prefabPath = $"{directory}/{config.name}_Apparatus.prefab";

            // The geometry meshes and synthesized/tinted materials are created at
            // build time (new Mesh / new Material) and are not assets. Persist
            // them as sub-assets of the config *before* the prefab is saved —
            // SaveAsPrefabAsset drops references to transient objects, so
            // embedding after the save would be too late (the prefab's
            // MeshFilter/Renderer references are already null by then).
            PersistGeneratedAssets(prefabRoot, config);

            AssetDatabase.DeleteAsset(prefabPath);
            var prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Object.DestroyImmediate(prefabRoot);
            if (prefab == null) return null;

            config.GeneratedPrefab = prefab;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);

            // If the prefab is open in Prefab Mode, reopen the stage so the
            // Scene view reflects the regenerated content.
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == prefabPath)
                PrefabStageUtility.OpenPrefab(prefabPath);

            return prefab;
        }

        // Persists the build-time meshes and materials referenced by the tree as
        // sub-assets of the config, so the prefab saved next serialises real
        // asset references. Old sub-assets from a previous regenerate are purged
        // first — otherwise they accumulate across rebuilds.
        static void PersistGeneratedAssets(GameObject prefabRoot, ApparatusConfig config)
        {
            string configPath = AssetDatabase.GetAssetPath(config);
            if (string.IsNullOrEmpty(configPath)) return;

            foreach (var existing in AssetDatabase.LoadAllAssetsAtPath(configPath))
                if (existing != config && (existing is Mesh || existing is Material))
                    Object.DestroyImmediate(existing, allowDestroyingAssets: true);

            var seen = new HashSet<Object>();
            foreach (var filter in prefabRoot.GetComponentsInChildren<MeshFilter>(true))
                Persist(filter.sharedMesh, config, seen);
            foreach (var renderer in prefabRoot.GetComponentsInChildren<Renderer>(true))
                foreach (var material in renderer.sharedMaterials)
                    Persist(material, config, seen);

            AssetDatabase.SaveAssets();
        }

        // Adds a build-time object (empty asset path) as a sub-asset of the
        // config. Built-in resources (StandIn primitive meshes) and already-saved
        // assets (authored materials, substituted prefab meshes) report a
        // non-empty asset path and are left to keep their own references.
        static void Persist(Object obj, ApparatusConfig config, HashSet<Object> seen)
        {
            if (obj == null || !seen.Add(obj)) return;
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj))) return;
            AssetDatabase.AddObjectToAsset(obj, config);
        }

        // ---- Per-room configuration ----

        static void ApplyRoomConfigs(GameObject root, ApparatusConfig config)
        {
            if (config.Rooms == null) return;
            var envRoot = root.GetComponent<EnvironmentRoot>();
            if (envRoot == null) return;

            foreach (var rc in config.Rooms)
            {
                if (rc == null) continue;
                var room = envRoot.GetRoom(rc.RoomId);
                if (room == null) continue;

                if (rc.OverrideColors)
                {
                    room.SetFloorColor(rc.FloorColor);
                    room.SetWallColor(rc.WallColor);
                    room.SetCeilingColor(rc.CeilingColor);
                }
                if (rc.FloorMaterial != null) room.SetFloorMaterial(rc.FloorMaterial);
                if (rc.WallMaterial != null) room.SetWallMaterial(rc.WallMaterial);
                if (rc.CeilingMaterial != null) room.SetCeilingMaterial(rc.CeilingMaterial);

                if (!string.IsNullOrEmpty(rc.Name))
                    room.gameObject.name = $"Room_{rc.RoomId}_{rc.Name.Trim()}";
            }
        }

        // ---- Door / window opening fill ----

        static void FillOpenings(GameObject root, ApparatusConfig config)
        {
            var envRoot = root.GetComponent<EnvironmentRoot>();
            if (envRoot == null) return;

            foreach (var room in envRoot.Rooms)
            {
                if (room.Walls == null) continue;
                var rc = FindRoomConfig(config, room.RoomId);
                var doorPrefab = rc != null && rc.DoorPrefab != null ? rc.DoorPrefab : config.DefaultDoorPrefab;
                var windowPrefab = rc != null && rc.WindowPrefab != null ? rc.WindowPrefab : config.DefaultWindowPrefab;
                if (doorPrefab == null && windowPrefab == null) continue;

                var centroid = RoomCentroid(room);
                foreach (var wall in room.Walls)
                {
                    if (wall?.Openings == null) continue;
                    foreach (var opening in wall.Openings)
                    {
                        bool isWindow = opening.SillHeight > 1e-4f;
                        var prefab = isWindow ? windowPrefab : doorPrefab;
                        if (prefab != null)
                            PlaceOpening(room.transform, wall, opening, prefab, centroid);
                    }
                }
            }
        }

        // Places an opening prefab centred in the opening, facing the room
        // interior, scaled to the opening's width x height. The prefab is
        // assumed authored as a unit (1x1) panel in its local XY plane.
        static void PlaceOpening(Transform roomT, WallData wall, OpeningSpec opening,
                                 GameObject prefab, Vector3 centroid)
        {
            var delta = wall.EndLocal - wall.StartLocal;
            float length = delta.magnitude;
            if (length < 1e-4f) return;
            var direction = delta / length;

            float along = opening.OffsetAlongEdge + opening.Width * 0.5f;
            float openingHeight = Mathf.Max(0.01f, opening.Height - opening.SillHeight);
            float centreY = opening.SillHeight + openingHeight * 0.5f;
            var position = wall.StartLocal + direction * along + Vector3.up * centreY;

            var inward = new Vector3(-direction.z, 0f, direction.x);
            var toCentroid = centroid - position;
            toCentroid.y = 0f;
            if (Vector3.Dot(toCentroid, inward) < 0f) inward = -inward;

            var instance = Object.Instantiate(prefab, roomT);
            instance.transform.localPosition = position;
            instance.transform.localRotation = inward.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(inward, Vector3.up)
                : Quaternion.identity;
            var scale = instance.transform.localScale;
            instance.transform.localScale =
                new Vector3(opening.Width * scale.x, openingHeight * scale.y, scale.z);
        }

        // ---- Per-object-type configuration ----

        static void ApplyObjectConfigs(GameObject root, ApparatusConfig config)
        {
            // Prefab substitution by type, reusing the shared applicator.
            if (config.ObjectTypes != null && config.ObjectTypes.Length > 0
                && config.Source.ObjectSlots != null)
            {
                var entries = new List<SubstitutionEntry>();
                foreach (var oc in config.ObjectTypes)
                    if (oc != null && !string.IsNullOrEmpty(oc.ObjectType) && oc.Prefab != null)
                        entries.Add(new SubstitutionEntry
                        {
                            ObjectType = oc.ObjectType,
                            Prefab = oc.Prefab,
                            ScaleMultiplier = oc.ScaleMultiplier,
                        });
                if (entries.Count > 0)
                {
                    var table = ScriptableObject.CreateInstance<PrefabSubstitutionTable>();
                    table.Entries = entries.ToArray();
                    PrefabSubstitutionApplicator.Apply(root, table, config.Source.ObjectSlots);
                    Object.DestroyImmediate(table);
                }
            }

            // Grid snap + per-type Y override.
            float grid = config.ObjectSnapGridSize;
            foreach (var instance in root.GetComponentsInChildren<RoomObjectInstance>(true))
            {
                var local = instance.transform.localPosition;
                if (grid > 1e-4f)
                {
                    local.x = Mathf.Round(local.x / grid) * grid;
                    local.z = Mathf.Round(local.z / grid) * grid;
                }
                var oc = FindObjectConfig(config, instance.ObjectType);
                if (oc != null && oc.UseYOverride)
                    local.y = oc.YOverride;
                instance.transform.localPosition = local;
            }
        }

        // ---- Origin room ----

        static void RecentreOnOriginRoom(GameObject root, ApparatusConfig config)
        {
            if (config.OriginRoomId < 0) return;
            var envRoot = root.GetComponent<EnvironmentRoot>();
            var room = envRoot != null ? envRoot.GetRoom(config.OriginRoomId) : null;
            if (room == null) return;

            var offset = RoomCentroid(room);
            foreach (Transform child in root.transform)
                child.localPosition -= offset;
        }

        // ---- Helpers ----

        static Vector3 RoomCentroid(Room room)
        {
            if (room.Walls == null || room.Walls.Length == 0) return Vector3.zero;
            var sum = Vector3.zero;
            foreach (var w in room.Walls)
                sum += (w.StartLocal + w.EndLocal) * 0.5f;
            return sum / room.Walls.Length;
        }

        static RoomConfig FindRoomConfig(ApparatusConfig config, int roomId)
        {
            if (config.Rooms == null) return null;
            foreach (var rc in config.Rooms)
                if (rc != null && rc.RoomId == roomId) return rc;
            return null;
        }

        static ObjectTypeConfig FindObjectConfig(ApparatusConfig config, string objectType)
        {
            if (config.ObjectTypes == null || string.IsNullOrEmpty(objectType)) return null;
            foreach (var oc in config.ObjectTypes)
                if (oc != null && oc.ObjectType == objectType) return oc;
            return null;
        }
    }
}
