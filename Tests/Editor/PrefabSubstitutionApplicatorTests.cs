using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using OpenApparatus.Unity.Editor.Internal;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class PrefabSubstitutionApplicatorTests
    {
        const string FixturePath = "Packages/com.openapparatus.unity/Tests/Fixtures/single_room.oae";

        [Test]
        public void Apply_NullTable_LeavesPlaceholdersUntouched()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ApparatusAsset>(FixturePath);
            asset.Substitution = null;
            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);
                var placeholders = root.GetComponentsInChildren<RoomObjectInstance>();
                Assert.AreEqual(1, placeholders.Length);
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Apply_MatchingEntry_ReplacesPlaceholderWithPrefab()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ApparatusAsset>(FixturePath);

            var prefabSource = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            prefabSource.name = "TestCupPrefab";

            var table = ScriptableObject.CreateInstance<PrefabSubstitutionTable>();
            table.Entries = new[]
            {
                new SubstitutionEntry
                {
                    ObjectType = "Cup",
                    Prefab = prefabSource,
                    ScaleMultiplier = Vector3.one,
                }
            };
            asset.Substitution = table;

            GameObject root = null;
            try
            {
                root = EnvironmentSpawner.Spawn(asset);

                // The slot node keeps its RoomObjectInstance; substitution
                // replaces the StandIn child with the prefab.
                var markers = root.GetComponentsInChildren<RoomObjectInstance>();
                Assert.AreEqual(1, markers.Length);
                var slotNode = markers[0].transform;
                Assert.IsNull(slotNode.Find("StandIn"), "StandIn placeholder should be removed.");
                Assert.IsNotNull(slotNode.Find("TestCupPrefab"),
                    "Substituted prefab should be a child of the slot node.");
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                Object.DestroyImmediate(prefabSource);
                asset.Substitution = null;
                Object.DestroyImmediate(table);
            }
        }

        [Test]
        public void Apply_NullPrefabEntry_LeavesPlaceholder()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ApparatusAsset>(FixturePath);
            var table = ScriptableObject.CreateInstance<PrefabSubstitutionTable>();
            table.Entries = new[]
            {
                new SubstitutionEntry { ObjectType = "Cup", Prefab = null, ScaleMultiplier = Vector3.one }
            };
            asset.Substitution = table;

            GameObject root = null;
            try
            {
                LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(
                    "PrefabSubstitutionTable has an entry for 'Cup' but Prefab is null"));
                root = EnvironmentSpawner.Spawn(asset);
                Assert.AreEqual(1, root.GetComponentsInChildren<RoomObjectInstance>().Length);
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                asset.Substitution = null;
                Object.DestroyImmediate(table);
            }
        }
    }
}
