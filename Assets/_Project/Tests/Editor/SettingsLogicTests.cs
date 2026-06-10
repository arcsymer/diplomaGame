using DiplomaGame.Runtime.Core;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для SettingsLogic — чистая статика, без MonoBehaviour.
    /// </summary>
    [TestFixture]
    public class SettingsLogicTests
    {
        // ================================================================
        // ClampQuality
        // ================================================================

        [Test]
        public void ClampQuality_ZeroCount_ReturnsZero()
        {
            Assert.AreEqual(0, SettingsLogic.ClampQuality(3, 0));
        }

        [Test]
        public void ClampQuality_NegativeCount_ReturnsZero()
        {
            Assert.AreEqual(0, SettingsLogic.ClampQuality(1, -1));
        }

        [Test]
        public void ClampQuality_NegativeLevel_ReturnsZero()
        {
            Assert.AreEqual(0, SettingsLogic.ClampQuality(-1, 5));
        }

        [Test]
        public void ClampQuality_LevelEqualsCount_ReturnsCountMinusOne()
        {
            // level == count → вне диапазона → count-1
            Assert.AreEqual(4, SettingsLogic.ClampQuality(5, 5));
        }

        [Test]
        public void ClampQuality_LevelAboveCount_ClampsToMax()
        {
            Assert.AreEqual(4, SettingsLogic.ClampQuality(99, 5));
        }

        [Test]
        public void ClampQuality_ValidLevel_ReturnsUnchanged()
        {
            Assert.AreEqual(2, SettingsLogic.ClampQuality(2, 5));
        }

        [Test]
        public void ClampQuality_ZeroLevelValidCount_ReturnsZero()
        {
            Assert.AreEqual(0, SettingsLogic.ClampQuality(0, 5));
        }

        [Test]
        public void ClampQuality_MaxValidLevel_ReturnsIt()
        {
            Assert.AreEqual(4, SettingsLogic.ClampQuality(4, 5));
        }

        // ================================================================
        // ClampVolume01
        // ================================================================

        [Test]
        public void ClampVolume01_NegativeValue_ReturnsZero()
        {
            Assert.AreEqual(0f, SettingsLogic.ClampVolume01(-0.5f), 0.001f);
        }

        [Test]
        public void ClampVolume01_AboveOne_ReturnsOne()
        {
            Assert.AreEqual(1f, SettingsLogic.ClampVolume01(1.5f), 0.001f);
        }

        [Test]
        public void ClampVolume01_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, SettingsLogic.ClampVolume01(0f), 0.001f);
        }

        [Test]
        public void ClampVolume01_One_ReturnsOne()
        {
            Assert.AreEqual(1f, SettingsLogic.ClampVolume01(1f), 0.001f);
        }

        [Test]
        public void ClampVolume01_MidValue_ReturnsUnchanged()
        {
            Assert.AreEqual(0.5f, SettingsLogic.ClampVolume01(0.5f), 0.001f);
        }

        // ================================================================
        // ClampSensitivity
        // ================================================================

        [Test]
        public void ClampSensitivity_BelowMin_ReturnMin()
        {
            Assert.AreEqual(0.01f, SettingsLogic.ClampSensitivity(0f), 0.0001f);
        }

        [Test]
        public void ClampSensitivity_NegativeValue_ReturnsMin()
        {
            Assert.AreEqual(0.01f, SettingsLogic.ClampSensitivity(-1f), 0.0001f);
        }

        [Test]
        public void ClampSensitivity_ExactMin_ReturnsMin()
        {
            Assert.AreEqual(0.01f, SettingsLogic.ClampSensitivity(0.01f), 0.0001f);
        }

        [Test]
        public void ClampSensitivity_AboveOne_ReturnsOne()
        {
            Assert.AreEqual(1f, SettingsLogic.ClampSensitivity(2f), 0.0001f);
        }

        [Test]
        public void ClampSensitivity_ExactOne_ReturnsOne()
        {
            Assert.AreEqual(1f, SettingsLogic.ClampSensitivity(1f), 0.0001f);
        }

        [Test]
        public void ClampSensitivity_MidValue_ReturnsUnchanged()
        {
            Assert.AreEqual(0.5f, SettingsLogic.ClampSensitivity(0.5f), 0.0001f);
        }

        [Test]
        public void ClampSensitivity_Default_IsInRange()
        {
            float def = SettingsLogic.ClampSensitivity(0.15f);
            Assert.GreaterOrEqual(def, 0.01f);
            Assert.LessOrEqual(def, 1f);
        }
    }
}
