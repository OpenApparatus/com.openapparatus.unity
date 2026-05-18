using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    public static class PrefabSubstitutionApplicator
    {
        public static void Apply(GameObject environmentRoot,
                                 PrefabSubstitutionTable table,
                                 ObjectSlotDefinition[] objectSlots)
        {
            if (environmentRoot == null) return;
            if (table == null || table.Entries == null || table.Entries.Length == 0) return;
            if (objectSlots == null || objectSlots.Length == 0) return;

            var warnedTypes = new HashSet<string>();
            var instances = environmentRoot
                .GetComponentsInChildren<RoomObjectInstance>(includeInactive: true);

            foreach (var instance in instances)
            {
                int slotIndex = instance.Slot - 1;
                if (slotIndex < 0 || slotIndex >= objectSlots.Length) continue;

                var slot = objectSlots[slotIndex];
                var objectType = !string.IsNullOrEmpty(slot.ObjectType)
                    ? slot.ObjectType
                    : slot.DisplayName;
                if (string.IsNullOrEmpty(objectType)) continue;

                if (!table.TryFind(objectType, out var entry)) continue;

                if (entry.Prefab == null)
                {
                    if (warnedTypes.Add(objectType))
                        Debug.LogWarning(
                            $"[OpenApparatus] PrefabSubstitutionTable has an entry for " +
                            $"'{objectType}' but Prefab is null; leaving placeholder.");
                    continue;
                }

                SwapStandIn(instance, entry);
            }
        }

        // Replaces the slot's "StandIn" visual child with the entry's prefab.
        // The slot node itself -- transform, RoomObjectInstance identity -- is
        // left intact; only the visual is swapped.
        static void SwapStandIn(RoomObjectInstance slot, SubstitutionEntry entry)
        {
            var standIn = slot.transform.Find("StandIn");

            GameObject instance = null;
            if (PrefabUtility.IsPartOfPrefabAsset(entry.Prefab))
                instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab);
            if (instance == null)
                instance = Object.Instantiate(entry.Prefab);
            if (instance == null) return;
            instance.name = entry.Prefab.name;

            instance.transform.SetParent(slot.transform, worldPositionStays: false);
            instance.transform.localPosition = entry.PositionOffset;
            instance.transform.localRotation = Quaternion.Euler(0f, entry.RotationOffsetYDegrees, 0f);

            var scale = entry.ScaleMultiplier;
            if (scale == Vector3.zero) scale = Vector3.one;
            var prefabScale = instance.transform.localScale;
            instance.transform.localScale = new Vector3(
                prefabScale.x * scale.x,
                prefabScale.y * scale.y,
                prefabScale.z * scale.z);

            if (standIn != null) Object.DestroyImmediate(standIn.gameObject);
        }
    }
}
