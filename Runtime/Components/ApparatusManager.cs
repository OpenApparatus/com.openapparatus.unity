using UnityEngine;

namespace OpenApparatus.Unity
{
    /// <summary>
    /// Scene handle for an apparatus. References an <see cref="ApparatusConfig"/>
    /// and instantiates its generated prefab as a child, so a scene depends only
    /// on the config — re-generating the config updates every placement.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ApparatusManager : MonoBehaviour
    {
        public ApparatusConfig Config;

        const string InstanceName = "Apparatus";

        /// <summary>
        /// Instantiates the config's generated prefab as a child, replacing any
        /// previous instance. Returns the instance, or null if the config or its
        /// prefab is missing.
        /// </summary>
        public GameObject Spawn()
        {
            var existing = transform.Find(InstanceName);
            if (existing != null) DestroyImmediate(existing.gameObject);

            if (Config == null || Config.GeneratedPrefab == null) return null;

            var instance = Instantiate(Config.GeneratedPrefab, transform);
            instance.name = InstanceName;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            return instance;
        }
    }
}
