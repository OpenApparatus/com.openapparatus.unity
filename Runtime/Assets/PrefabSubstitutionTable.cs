using UnityEngine;

namespace OpenApparatus.Unity
{
    [CreateAssetMenu(menuName = "OpenApparatus/Prefab Substitution Table",
                     fileName = "PrefabSubstitutionTable")]
    public sealed class PrefabSubstitutionTable : ScriptableObject
    {
        public SubstitutionEntry[] Entries;

        public bool TryFind(string objectType, out SubstitutionEntry entry)
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
