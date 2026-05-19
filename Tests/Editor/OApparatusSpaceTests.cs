using NUnit.Framework;
using UnityEngine;
using OpenApparatus.Unity.Internal;

namespace OpenApparatus.Unity.Tests.Editor
{
    public sealed class OApparatusSpaceTests
    {
        [Test]
        public void ToUnity_NegatesX()
        {
            var unity = OApparatusSpace.ToUnity(new Vector3(1, 2, 3));
            Assert.AreEqual(new Vector3(-1, 2, 3), unity);
        }

        [Test]
        public void ToUnityXZ_NegatesX()
        {
            var xz = OApparatusSpace.ToUnityXZ(new Vector2(5, 7));
            Assert.AreEqual(new Vector2(-5, 7), xz);
        }

        [Test]
        public void YawToUnity_NegatesAngle()
        {
            Assert.AreEqual(-1.5f, OApparatusSpace.YawToUnity(1.5f), 1e-6f);
        }

        [Test]
        public void ToUnity_IsInvolution()
        {
            var original = new Vector3(2.5f, 1.0f, -3.5f);
            var back = OApparatusSpace.ToUnity(OApparatusSpace.ToUnity(original));
            Assert.AreEqual(original, back);
        }
    }
}
