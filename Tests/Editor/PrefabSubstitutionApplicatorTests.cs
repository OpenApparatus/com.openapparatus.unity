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
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
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
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);

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

                // Substitution destroys the placeholder GameObject but carries
                // the RoomObjectInstance marker onto the new prefab, so exactly
                // one marker remains — on the substituted instance.
                var markers = root.GetComponentsInChildren<RoomObjectInstance>();
                Assert.AreEqual(1, markers.Length,
                    "Substituted instance should carry the RoomObjectInstance marker.");
                Assert.AreEqual("TestCupPrefab", markers[0].gameObject.name,
                    "Marker should be on the substituted prefab, not the destroyed placeholder.");

                bool foundCylinder = false;
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null && mf.sharedMesh.name.Contains("Cylinder"))
                        foundCylinder = true;
                Assert.IsTrue(foundCylinder, "Substituted prefab should be present.");
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
            var asset = AssetDatabase.LoadAssetAtPath<MultiRoomEnvironmentAsset>(FixturePath);
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
