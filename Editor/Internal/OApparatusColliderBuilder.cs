using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    /// <summary>
    /// Adds colliders to a spawned environment. Each generated part
    /// (Floor / Walls / Ceiling) under a room receives a MeshCollider matching
    /// its render mesh when the corresponding <see cref="OApparatusColliderMode"/> flag is
    /// set — so the collision surface always matches what is drawn, including
    /// doorway cut-outs.
    ///
    /// Object placeholder colliders are owned by the spawner via the Objects
    /// flag (it keeps or drops the CreatePrimitive collider); this pass does
    /// not touch them, and substituted prefabs keep whatever collider their
    /// author shipped.
    /// </summary>
    public static class OApparatusColliderBuilder
    {
        public static void Apply(GameObject environmentRoot,
                                 OApparatusColliderMode mode,
                                 OApparatusParameters parameters)
        {
            if (mode == OApparatusColliderMode.None || environmentRoot == null) return;

            foreach (var room in environmentRoot.GetComponentsInChildren<OApparatusRoomManager>(includeInactive: true))
            {
                if ((mode & OApparatusColliderMode.Floors) != 0)   AddPartCollider(room.transform, "Floor");
                if ((mode & OApparatusColliderMode.Walls) != 0)    AddPartCollider(room.transform, "Walls");
                if ((mode & OApparatusColliderMode.Ceilings) != 0) AddPartCollider(room.transform, "Ceiling");
            }
        }

        static void AddPartCollider(Transform room, string partName)
        {
            var part = room.Find(partName);
            if (part == null) return;
            var mf = part.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            if (part.GetComponent<MeshCollider>() != null) return;
            part.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }
    }
}
