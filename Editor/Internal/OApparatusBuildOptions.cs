using System;

namespace OpenApparatus.Unity.Editor.Internal
{
    /// <summary>
    /// User-facing choices for how an imported environment is turned into a
    /// GameObject tree. Importers expose these so researchers decide what gets
    /// generated rather than having it prescribed.
    /// </summary>
    [Serializable]
    public sealed class OApparatusBuildOptions
    {
        public bool GenerateFloors = true;
        public bool GenerateCeilings = true;
        public bool GenerateWalls = true;

        /// <summary>Render the outward-facing skin of outer walls. Off matches a
        /// Studio .glb (each room renders only the faces it sees from inside, so
        /// the building is see-through from outside); on closes those exterior
        /// faces for an external camera view.</summary>
        public bool GenerateExteriorWalls = true;

        /// <summary>Optional per-part / per-name material assignments. Null falls
        /// back to <see cref="OApparatusMaterialResolver"/>'s authored or synthesized defaults.</summary>
        public OApparatusMaterialOverrides OApparatusMaterialOverrides;
    }
}
