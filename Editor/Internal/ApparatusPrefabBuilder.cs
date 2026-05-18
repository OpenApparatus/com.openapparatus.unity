using System.IO;
using UnityEditor;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    /// <summary>
    /// Generates an environment prefab from an <see cref="ApparatusConfig"/>:
    /// builds the GameObject tree through the shared spawn pipeline with the
    /// config's options, then saves it as a prefab asset next to the config.
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
            return EnvironmentSpawner.Spawn(config.Source, options);
        }

        /// <summary>
        /// Rebuilds the config's prefab asset and points
        /// <see cref="ApparatusConfig.GeneratedPrefab"/> at it. Returns the prefab.
        /// </summary>
        public static GameObject Regenerate(ApparatusConfig config)
        {
            if (config == null || config.Source == null) return null;

            var instance = BuildInstance(config);
            if (instance == null) return null;

            string configPath = AssetDatabase.GetAssetPath(config);
            string directory = string.IsNullOrEmpty(configPath)
                ? "Assets"
                : Path.GetDirectoryName(configPath);
            string prefabPath = $"{directory}/{config.name}_Apparatus.prefab";

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            config.GeneratedPrefab = prefab;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
            return prefab;
        }
    }
}
