using UnityEngine;

namespace OpenApparatus.Unity
{
    [CreateAssetMenu(menuName = "OpenApparatus/Prefab Substitution Table",
                     fileName = "OApparatusSubstitutionTable")]
    public sealed class OApparatusSubstitutionTable : ScriptableObject
    {
        public OApparatusSubstitutionEntry[] Entries;

        public bool TryFind(string objectType, out OApparatusSubstitutionEntry entry)
        {
            if (Entries != null && !string.IsNullOrEmpty(objectType))
            {
                for (int i = 0; i < Entries.Length; i++)
                {
                    if (Entries[i].ObjectType == objectType)
                    {
                        entry = Entries[i];
                        return true;
                    }
                }
            }
            entry = default;
            return false;
        }
    }
}
