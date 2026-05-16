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

                SwapPlaceholder(instance, entry);
            }
        }

        static void SwapPlaceholder(RoomObjectInstance placeholder, SubstitutionEntry entry)
        {
            var parent = placeholder.transform.parent;
            var basePos = placeholder.transform.localPosition;
            var baseRot = placeholder.transform.localRotation;

            // Capture the placeholder's slot metadata so we can carry it onto
            // the substituted instance. Downstream tools (ColliderBuilder,
            // researcher code) identify research objects via RoomObjectInstance.
            int slot         = placeholder.Slot;
            int owningRoomId = placeholder.OwningRoomId;
            float rotationY  = placeholder.LocalRotationY;

            GameObject instance = null;
            if (PrefabUtility.IsPartOfPrefabAsset(entry.Prefab))
                instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab);
            if (instance == null)
                instance = Object.Instantiate(entry.Prefab);
            if (instance == null) return;
            instance.name = entry.Prefab.name;

            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.localPosition = basePos + entry.PositionOffset;
            instance.transform.localRotation =
                baseRot * Quaternion.Euler(0f, entry.RotationOffsetYDegrees, 0f);

            var scale = entry.ScaleMultiplier;
            if (scale == Vector3.zero) scale = Vector3.one;
            var prefabScale = instance.transform.localScale;
            instance.transform.localScale = new Vector3(
                prefabScale.x * scale.x,
                prefabScale.y * scale.y,
                prefabScale.z * scale.z);

            // Marker survives the swap. The scene instance is mutable; the
            // source prefab asset is untouched.
            var marker = instance.GetComponent<RoomObjectInstance>()
                         ?? instance.AddComponent<RoomObjectInstance>();
            marker.Slot          = slot;
            marker.OwningRoomId  = owningRoomId;
            marker.LocalRotationY = rotationY;

            Object.DestroyImmediate(placeholder.gameObject);
        }
    }
}
