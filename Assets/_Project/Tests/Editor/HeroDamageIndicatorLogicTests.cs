using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.GameFeel;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для <see cref="HeroDamageIndicatorLogic"/>.
    /// Не требуют MonoBehaviour или сцены.
    /// </summary>
    [TestFixture]
    public class HeroDamageIndicatorLogicTests
    {
        // ================================================================
        // HeroDamageIndicatorLogic.ComputeFadeAlpha
        // ================================================================

        private const float PeakAlpha = 0.6f;

        [Test]
        public void ComputeFadeAlpha_AtZero_ReturnsPeakAlpha()
        {
            float result = HeroDamageIndicatorLogic.ComputeFadeAlpha(0f, PeakAlpha);
            Assert.AreEqual(PeakAlpha, result, 0.0001f,
                "При t=0 (только что ударили) альфа должна быть пиковой.");
        }

        [Test]
        public void ComputeFadeAlpha_AtOne_ReturnsZero()
        {
            float result = HeroDamageIndicatorLogic.ComputeFadeAlpha(1f, PeakAlpha);
            Assert.AreEqual(0f, result, 0.0001f,
                "При t=1 (конец анимации) альфа должна быть 0.");
        }

        [Test]
        public void ComputeFadeAlpha_AtHalf_IsHalfPeak()
        {
            float result = HeroDamageIndicatorLogic.ComputeFadeAlpha(0.5f, PeakAlpha);
            Assert.AreEqual(PeakAlpha * 0.5f, result, 0.0001f,
                "При t=0.5 альфа должна быть половиной пика (линейный спад).");
        }

        [Test]
        public void ComputeFadeAlpha_IsMonotonicallyDecreasing()
        {
            float prev = HeroDamageIndicatorLogic.ComputeFadeAlpha(0f, PeakAlpha);
            for (int i = 1; i <= 10; i++)
            {
                float t    = i / 10f;
                float curr = HeroDamageIndicatorLogic.ComputeFadeAlpha(t, PeakAlpha);
                Assert.LessOrEqual(curr, prev,
                    $"Альфа при t={t} должна быть <= альфе при t={(i - 1) / 10f} (монотонный спад).");
                prev = curr;
            }
        }

        [Test]
        public void ComputeFadeAlpha_BeyondOne_ClampsToZero()
        {
            float result = HeroDamageIndicatorLogic.ComputeFadeAlpha(2f, PeakAlpha);
            Assert.AreEqual(0f, result, 0.0001f,
                "Для t > 1 альфа должна быть 0 (clamped).");
        }

        [Test]
        public void ComputeFadeAlpha_NegativeT_ClampsToPeak()
        {
            float result = HeroDamageIndicatorLogic.ComputeFadeAlpha(-1f, PeakAlpha);
            Assert.AreEqual(PeakAlpha, result, 0.0001f,
                "Для t < 0 альфа должна быть пиковой (clamped).");
        }

        // ================================================================
        // HeroDamageIndicatorLogic.IsFadeDone
        // ================================================================

        [Test]
        public void IsFadeDone_ElapsedEqualsDuration_ReturnsTrue()
        {
            Assert.IsTrue(
                HeroDamageIndicatorLogic.IsFadeDone(1f, 1f),
                "elapsed == duration → анимация завершена.");
        }

        [Test]
        public void IsFadeDone_ElapsedBeyondDuration_ReturnsTrue()
        {
            Assert.IsTrue(
                HeroDamageIndicatorLogic.IsFadeDone(1.5f, 1f),
                "elapsed > duration → анимация завершена.");
        }

        [Test]
        public void IsFadeDone_ElapsedLessThanDuration_ReturnsFalse()
        {
            Assert.IsFalse(
                HeroDamageIndicatorLogic.IsFadeDone(0.5f, 1f),
                "elapsed < duration → анимация ещё идёт.");
        }

        [Test]
        public void IsFadeDone_ZeroElapsed_ReturnsFalse()
        {
            Assert.IsFalse(
                HeroDamageIndicatorLogic.IsFadeDone(0f, 1f),
                "elapsed=0 → только что запустили, не завершена.");
        }

        // ================================================================
        // GameFeelSettings — дефолты Circle-21
        // ================================================================

        [Test]
        public void GameFeelSettings_DamageIndicatorDuration_IsOneSecond()
        {
            var settings = ScriptableObject.CreateInstance<GameFeelSettings>();
            Assert.AreEqual(1.0f, settings.damageIndicatorDuration, 0.001f,
                "damageIndicatorDuration по умолчанию должна быть 1.0 с (по ТЗ Circle-21).");
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void GameFeelSettings_DamageIndicatorPeakAlpha_IsBetweenZeroAndOne()
        {
            var settings = ScriptableObject.CreateInstance<GameFeelSettings>();
            Assert.Greater(settings.damageIndicatorPeakAlpha, 0f,
                "damageIndicatorPeakAlpha должна быть > 0.");
            Assert.LessOrEqual(settings.damageIndicatorPeakAlpha, 1f,
                "damageIndicatorPeakAlpha должна быть <= 1.");
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void GameFeelSettings_DamageIndicatorDuration_IsPositive()
        {
            var settings = ScriptableObject.CreateInstance<GameFeelSettings>();
            Assert.Greater(settings.damageIndicatorDuration, 0f,
                "damageIndicatorDuration должна быть положительной.");
            Object.DestroyImmediate(settings);
        }
    }
}
