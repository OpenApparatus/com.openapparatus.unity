using System;

namespace OpenApparatus.Unity.Editor.Internal
{
    /// <summary>
    /// User-facing choices for how an imported environment is turned into a
    /// GameObject tree. Importers expose these so researchers decide what gets
    /// generated rather than having it prescribed.
    /// </summary>
    [Serializable]
    public sealed class EnvironmentBuildOptions
    {
        public bool GenerateFloors = true;
        public bool GenerateCeilings = true;
        public bool GenerateWalls = true;

        /// <summary>Optional per-part / per-name material assignments. Null falls
        /// back to <see cref="MaterialResolver"/>'s authored or synthesized defaults.</summary>
        public MaterialOverrides MaterialOverrides;
    }
}
