using UnityEditor;
using UnityEngine;

namespace OpenApparatus.Unity.Editor.Internal
{
    public static class OApparatusMaterialResolver
    {
        const string PackagePath = "Packages/com.openapparatus.unity/Materials";
        static bool _warnedFallback;

        public static Material Resolve(string studioName, OApparatusMaterialOverrides overrides = null)
        {
            if (overrides != null)
            {
                if (overrides.ByStudioName != null &&
                    overrides.ByStudioName.TryGetValue(studioName, out var mapped) &&
                    mapped != null)
                {
                    return mapped;
                }

                var partDefault =
                    studioName.StartsWith("OpenApparatus_Floor_")    ? overrides.FloorDefault   :
                    studioName.StartsWith("OpenApparatus_Ceiling_")  ? overrides.CeilingDefault :
                    studioName.StartsWith("OpenApparatus_Walls_")    ? overrides.WallDefault    :
                                                                       null;
                if (partDefault != null) return partDefault;
            }

            var asset = LoadAuthoredDefault(studioName);
            if (asset != null) return asset;

            return SynthesizeDefault(studioName);
        }

        static Material LoadAuthoredDefault(string studioName)
        {
            string part = PartFromStudioName(studioName);
            if (part == null) return null;
            var path = $"{PackagePath}/{PipelineFolder()}/{part}.mat";
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        static Material SynthesizeDefault(string studioName)
        {
            string part = PartFromStudioName(studioName);
            var shader = FindPipelineShader();
            if (shader == null)
            {
                if (!_warnedFallback)
                {
                    Debug.LogWarning("[OpenApparatus] No pipeline shader found; using magenta fallback.");
                    _warnedFallback = true;
                }
                return CreateMagentaFallback();
            }

            var mat = new Material(shader) { name = $"OpenApparatus_{part ?? "Default"}_Synth" };
            mat.color = part switch
            {
                "Floor"   => new Color(0.55f, 0.55f, 0.55f),
                "Ceiling" => new Color(0.85f, 0.85f, 0.85f),
                "Wall"    => new Color(0.75f, 0.75f, 0.70f),
                _         => Color.white,
            };
            return mat;
        }

        static string PartFromStudioName(string studioName)
        {
            if (string.IsNullOrEmpty(studioName)) return null;
            if (studioName.StartsWith("OpenApparatus_Floor_"))   return "Floor";
            if (studioName.StartsWith("OpenApparatus_Ceiling_")) return "Ceiling";
            if (studioName.StartsWith("OpenApparatus_Walls_"))   return "Wall";
            return null;
        }

        static string PipelineFolder()
        {
#if OPENAPPARATUS_HDRP
            return "HDRP";
#elif OPENAPPARATUS_URP
            return "URP";
#else
            return "Builtin";
#endif
        }

        static Shader FindPipelineShader()
        {
#if OPENAPPARATUS_HDRP
            return Shader.Find("HDRP/Lit");
#elif OPENAPPARATUS_URP
            return Shader.Find("Universal Render Pipeline/Lit");
#else
            return Shader.Find("Standard");
#endif
        }

        static Material CreateMagentaFallback()
        {
            var shader = Shader.Find("Hidden/InternalErrorShader") ?? Shader.Find("Unlit/Color");
            var mat = new Material(shader) { name = "OpenApparatus_Fallback" };
            mat.color = Color.magenta;
            return mat;
        }
    }
}
