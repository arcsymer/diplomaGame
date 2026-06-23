using DiplomaGame.Runtime.UI;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для <see cref="StaminaBarLogic"/> (Circle-24).
    /// Не требуют MonoBehaviour или сцены — чистая математика.
    /// </summary>
    [TestFixture]
    public class StaminaBarLogicTests
    {
        private const float Epsilon = 0.0001f;

        // ================================================================
        // GetFillAmount
        // ================================================================

        [Test]
        public void GetFillAmount_FullStamina_ReturnsOne()
        {
            Assert.AreEqual(1f, StaminaBarLogic.GetFillAmount(1f), Epsilon,
                "Полная стамина → fillAmount = 1.");
        }

        [Test]
        public void GetFillAmount_EmptyStamina_ReturnsZero()
        {
            Assert.AreEqual(0f, StaminaBarLogic.GetFillAmount(0f), Epsilon,
                "Нулевая стамина → fillAmount = 0.");
        }

        [Test]
        public void GetFillAmount_Half_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, StaminaBarLogic.GetFillAmount(0.5f), Epsilon,
                "Стамина 0.5 → fillAmount = 0.5.");
        }

        [Test]
        public void GetFillAmount_OverOne_ClampsToOne()
        {
            Assert.AreEqual(1f, StaminaBarLogic.GetFillAmount(1.5f), Epsilon,
                "Стамина > 1 должна зажиматься в 1.");
        }

        [Test]
        public void GetFillAmount_BelowZero_ClampsToZero()
        {
            Assert.AreEqual(0f, StaminaBarLogic.GetFillAmount(-0.1f), Epsilon,
                "Стамина < 0 должна зажиматься в 0.");
        }

        // ================================================================
        // GetBarColor
        // ================================================================

        [Test]
        public void GetBarColor_FullStamina_IsYellow()
        {
            Color c = StaminaBarLogic.GetBarColor(1f);
            // Yellow = (1,1,0,1)
            Assert.AreEqual(Color.yellow, c,
                "Полная стамина → цвет жёлтый.");
        }

        [Test]
        public void GetBarColor_AboveRedThreshold_IsYellow()
        {
            Color c = StaminaBarLogic.GetBarColor(StaminaBarLogic.RedThreshold + 0.1f);
            Assert.AreEqual(Color.yellow, c,
                "Стамина выше RedThreshold → цвет жёлтый.");
        }

        [Test]
        public void GetBarColor_ZeroStamina_IsRed()
        {
            Color c = StaminaBarLogic.GetBarColor(0f);
            Assert.AreEqual(Color.red, c,
                "Нулевая стамина → цвет красный.");
        }

        [Test]
        public void GetBarColor_HalfRedThreshold_IsBetweenYellowAndRed()
        {
            float mid = StaminaBarLogic.RedThreshold * 0.5f;
            Color c   = StaminaBarLogic.GetBarColor(mid);

            // Цвет должен быть между жёлтым и красным:
            // r всегда 1, g убывает, b ниже жёлтого (lerp к красному, у которого b=0; Color.yellow.b = 4/255)
            Assert.AreEqual(1f, c.r, Epsilon, "R-канал должен быть 1.");
            Assert.Greater(c.g, 0f,   "G-канал должен быть > 0 (не чисто красный).");
            Assert.Less(c.g,    1f,   "G-канал должен быть < 1 (не чисто жёлтый).");
            Assert.Less(c.b, Color.yellow.b, "B-канал должен убывать к красному (ниже жёлтого).");
        }

        [Test]
        public void GetBarColor_AtRedThreshold_IsYellow()
        {
            Color c = StaminaBarLogic.GetBarColor(StaminaBarLogic.RedThreshold);
            Assert.AreEqual(Color.yellow, c,
                "На границе RedThreshold цвет должен быть жёлтым.");
        }

        [Test]
        public void GetBarColor_NegativeInput_ClampsToRed()
        {
            Color c = StaminaBarLogic.GetBarColor(-1f);
            Assert.AreEqual(Color.red, c,
                "Отрицательный вход зажимается в 0 → красный.");
        }

        [Test]
        public void GetBarColor_OverOneInput_ClampsToYellow()
        {
            Color c = StaminaBarLogic.GetBarColor(2f);
            Assert.AreEqual(Color.yellow, c,
                "Вход > 1 зажимается в 1 → жёлтый.");
        }

        // ================================================================
        // ShouldShowBar
        // ================================================================

        [Test]
        public void ShouldShowBar_SprintingFullStamina_ReturnsTrue()
        {
            Assert.IsTrue(StaminaBarLogic.ShouldShowBar(isSprinting: true, staminaNormalized: 1f),
                "Спринт с полной стаминой — показывать бар (чтобы игрок видел расход).");
        }

        [Test]
        public void ShouldShowBar_NotSprintingFullStamina_ReturnsFalse()
        {
            Assert.IsFalse(StaminaBarLogic.ShouldShowBar(isSprinting: false, staminaNormalized: 1f),
                "Полная стамина без спринта — прятать бар.");
        }

        [Test]
        public void ShouldShowBar_NotSprintingReducedStamina_ReturnsTrue()
        {
            Assert.IsTrue(StaminaBarLogic.ShouldShowBar(isSprinting: false, staminaNormalized: 0.9f),
                "Стамина не полная — показывать бар даже без спринта.");
        }

        [Test]
        public void ShouldShowBar_NotSprintingZeroStamina_ReturnsTrue()
        {
            Assert.IsTrue(StaminaBarLogic.ShouldShowBar(isSprinting: false, staminaNormalized: 0f),
                "Нулевая стамина — бар должен быть виден.");
        }

        [Test]
        public void ShouldShowBar_SprintingZeroStamina_ReturnsTrue()
        {
            Assert.IsTrue(StaminaBarLogic.ShouldShowBar(isSprinting: true, staminaNormalized: 0f),
                "Спринт + нулевая стамина — бар виден.");
        }

        [Test]
        public void ShouldShowBar_StaminaVeryCloseToFull_ReturnsFalse()
        {
            // 1f - 1e-5 должно трактоваться как «полная» (в пределах эпсилона 1e-4)
            Assert.IsFalse(StaminaBarLogic.ShouldShowBar(isSprinting: false, staminaNormalized: 1f - 1e-5f),
                "Стамина в пределах 1e-4 от максимума — считается полной, бар прячем.");
        }

        [Test]
        public void ShouldShowBar_StaminaJustBelowFull_ReturnsTrue()
        {
            // 1f - 2e-4 гарантированно ниже порога (1e-4)
            Assert.IsTrue(StaminaBarLogic.ShouldShowBar(isSprinting: false, staminaNormalized: 1f - 2e-4f),
                "Стамина явно ниже максимума — бар должен быть виден.");
        }

        // ================================================================
        // RedThreshold константа
        // ================================================================

        [Test]
        public void RedThreshold_IsInValidRange()
        {
            Assert.Greater(StaminaBarLogic.RedThreshold, 0f,
                "RedThreshold должен быть > 0.");
            Assert.Less(StaminaBarLogic.RedThreshold, 1f,
                "RedThreshold должен быть < 1.");
        }
    }
}
