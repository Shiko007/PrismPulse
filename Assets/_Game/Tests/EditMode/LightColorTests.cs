using NUnit.Framework;
using PrismPulse.Core.Colors;

namespace PrismPulse.Tests
{
    [TestFixture]
    public class LightColorTests
    {
        [Test]
        public void RedPlusBlue_EqualsPurple()
        {
            Assert.AreEqual(LightColor.Purple, LightColorMath.Mix(LightColor.Red, LightColor.Blue));
        }

        [Test]
        public void RedPlusGreen_EqualsYellow()
        {
            Assert.AreEqual(LightColor.Yellow, LightColorMath.Mix(LightColor.Red, LightColor.Green));
        }

        [Test]
        public void GreenPlusBlue_EqualsCyan()
        {
            Assert.AreEqual(LightColor.Cyan, LightColorMath.Mix(LightColor.Green, LightColor.Blue));
        }

        [Test]
        public void AllThree_EqualsWhite()
        {
            var mixed = LightColorMath.Mix(LightColor.Red, LightColor.Green);
            mixed = LightColorMath.Mix(mixed, LightColor.Blue);
            Assert.AreEqual(LightColor.White, mixed);
        }

        [Test]
        public void MixWithSelf_ReturnsSame()
        {
            Assert.AreEqual(LightColor.Red, LightColorMath.Mix(LightColor.Red, LightColor.Red));
        }

        [Test]
        public void White_ContainsAll()
        {
            Assert.IsTrue(LightColor.White.Contains(LightColor.Red));
            Assert.IsTrue(LightColor.White.Contains(LightColor.Green));
            Assert.IsTrue(LightColor.White.Contains(LightColor.Blue));
            Assert.IsTrue(LightColor.White.Contains(LightColor.Purple));
        }

        [Test]
        public void Red_DoesNotContainBlue()
        {
            Assert.IsFalse(LightColor.Red.Contains(LightColor.Blue));
        }

        [Test]
        public void IsPrimary_TrueForPrimaries()
        {
            Assert.IsTrue(LightColor.Red.IsPrimary());
            Assert.IsTrue(LightColor.Green.IsPrimary());
            Assert.IsTrue(LightColor.Blue.IsPrimary());
        }

        [Test]
        public void IsPrimary_FalseForMixed()
        {
            Assert.IsFalse(LightColor.Purple.IsPrimary());
            Assert.IsFalse(LightColor.Yellow.IsPrimary());
            Assert.IsFalse(LightColor.White.IsPrimary());
        }

        [Test]
        public void ComponentCount_Correct()
        {
            Assert.AreEqual(1, LightColor.Red.ComponentCount());
            Assert.AreEqual(2, LightColor.Purple.ComponentCount());
            Assert.AreEqual(3, LightColor.White.ComponentCount());
            Assert.AreEqual(0, LightColor.None.ComponentCount());
        }
    }
}
