using System;
using UnityEngine;

namespace OpenApparatus.Unity
{
    /// <summary>
    /// Authoring asset for one apparatus: references an imported
    /// <see cref="OApparatusAsset"/> and layers build options plus per-room and
    /// per-object-type configuration on top, then generates an environment
    /// prefab from it. Several configs may target the same source asset to
    /// produce experimental-condition variations.
    /// </summary>
    [CreateAssetMenu(fileName = "OApparatusConfig", menuName = "OpenApparatus/Apparatus Config")]
    public sealed class OApparatusConfig : ScriptableObject
    {
        [Tooltip("Imported .oae / .oapp asset this configuration builds from.")]
        public OApparatusAsset Source;

        [Header("Geometry")]
        public bool GenerateFloors = true;
        public bool GenerateCeilings = true;
        public bool GenerateWalls = true;
        public bool GenerateExteriorWalls = true;

        [Tooltip("OApparatusRoomManager id to centre the apparatus on; -1 leaves the source origin.")]
        public int OriginRoomId = -1;

        [Header("Objects")]
        [Tooltip("Snap object positions to this grid size, in metres. 0 = no snapping.")]
        public float ObjectSnapGridSize = 0f;
        public OApparatusObjectTypeConfig[] ObjectTypes;

        [Header("Rooms")]
        public GameObject DefaultDoorPrefab;
        public GameObject DefaultWindowPrefab;
        public OApparatusRoomConfig[] Rooms;

        [Tooltip("Prefab generated from this config; regenerated when settings change.")]
        public GameObject GeneratedPrefab;
    }

    /// <summary>Per-room overrides layered on the source apparatus.</summary>
    [Serializable]
    public sealed class OApparatusRoomConfig
    {
        public int RoomId;
        public string Name;

        public bool OverrideColors;
        public Color FloorColor = Color.white;
        public Color WallColor = Color.white;
        public Color CeilingColor = Color.white;

        public Material FloorMaterial;
        public Material WallMaterial;
        public Material CeilingMaterial;

        [Tooltip("Door / window prefabs for this room; null falls back to the config defaults.")]
        public GameObject DoorPrefab;
        public GameObject WindowPrefab;
    }

    /// <summary>Per-object-type configuration: prefab substitution and placement.</summary>
    [Serializable]
    public sealed class OApparatusObjectTypeConfig
    {
        public string ObjectType;
        public GameObject Prefab;
        public Vector3 ScaleMultiplier = Vector3.one;

        [Tooltip("Override the Y of every object of this type instead of the source value.")]
        public bool UseYOverride;
        public float YOverride;
    }
}
