using DiplomaGame.Runtime.UI;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для чистой статики HudLogic.
    /// Нет MonoBehaviour — запускаются мгновенно.
    /// </summary>
    [TestFixture]
    public class HudLogicTests
    {
        // ----------------------------------------------------------------
        // FormatCrystals
        // ----------------------------------------------------------------

        [Test]
        public void FormatCrystals_Zero_ReturnsCorrectString()
        {
            Assert.AreEqual("Crystals: 0", HudLogic.FormatCrystals(0));
        }

        [Test]
        public void FormatCrystals_Positive_ReturnsCorrectString()
        {
            Assert.AreEqual("Crystals: 150", HudLogic.FormatCrystals(150));
        }

        [Test]
        public void FormatCrystals_LargeNumber_ContainsNumber()
        {
            var result = HudLogic.FormatCrystals(9999);
            Assert.IsTrue(result.Contains("9999"), $"Результат должен содержать «9999»: «{result}»");
        }

        // ----------------------------------------------------------------
        // CooldownFill
        // ----------------------------------------------------------------

        [Test]
        public void CooldownFill_ZeroTotal_ReturnsZero()
        {
            Assert.AreEqual(0f, HudLogic.CooldownFill(5f, 0f), 0.001f,
                "При total=0 fill должен быть 0.");
        }

        [Test]
        public void CooldownFill_NegativeTotal_ReturnsZero()
        {
            Assert.AreEqual(0f, HudLogic.CooldownFill(3f, -1f), 0.001f,
                "При total<0 fill должен быть 0.");
        }

        [Test]
        public void CooldownFill_ZeroRemaining_ReturnsZero()
        {
            Assert.AreEqual(0f, HudLogic.CooldownFill(0f, 8f), 0.001f,
                "При remaining=0 способность готова — fill=0.");
        }

        [Test]
        public void CooldownFill_FullCooldown_ReturnsOne()
        {
            Assert.AreEqual(1f, HudLogic.CooldownFill(8f, 8f), 0.001f,
                "При remaining==total fill должен быть 1.");
        }

        [Test]
        public void CooldownFill_Half_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, HudLogic.CooldownFill(4f, 8f), 0.001f,
                "При remaining=4, total=8 fill должен быть 0.5.");
        }

        [Test]
        public void CooldownFill_OverMax_ClampedToOne()
        {
            Assert.AreEqual(1f, HudLogic.CooldownFill(10f, 8f), 0.001f,
                "Значение > total должно быть зажато до 1.");
        }
    }
}
