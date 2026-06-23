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

        // ================================================================
        // HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees (Circle-23)
        // ================================================================
        // Система координат: refForwardXZ = Vector3.forward (герой смотрит +Z).
        // heroPos = (0, 0, 0). Угол 0° = атака спереди (вверх кольца HUD).
        // +90° = атака справа. 180°/−180° = атака сзади. −90° = атака слева.

        private static readonly Vector3 HeroPos = Vector3.zero;
        private static readonly Vector3 Forward  = Vector3.forward; // (0,0,1)
        private const float AngleTolerance = 0.5f; // градусов

        [Test]
        public void ComputeAngle_AttackerInFront_ReturnsZero()
        {
            // Атакующий прямо перед героем (+Z при forward=+Z)
            Vector3 sourcePos = new Vector3(0f, 0f, 5f);
            float angle = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward, HeroPos, sourcePos);
            Assert.AreEqual(0f, angle, AngleTolerance,
                "Атакующий спереди → угол 0°.");
        }

        [Test]
        public void ComputeAngle_AttackerBehind_Returns180()
        {
            // Атакующий прямо сзади (−Z при forward=+Z)
            Vector3 sourcePos = new Vector3(0f, 0f, -5f);
            float angle = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward, HeroPos, sourcePos);
            // SignedAngle возвращает значение в (−180, +180]; −Z даёт 180° или −180°
            Assert.AreEqual(180f, Mathf.Abs(angle), AngleTolerance,
                "Атакующий сзади → |угол| = 180°.");
        }

        [Test]
        public void ComputeAngle_AttackerToRight_ReturnsPositive90()
        {
            // Атакующий справа (+X при forward=+Z)
            Vector3 sourcePos = new Vector3(5f, 0f, 0f);
            float angle = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward, HeroPos, sourcePos);
            Assert.AreEqual(90f, angle, AngleTolerance,
                "Атакующий справа → угол +90°.");
        }

        [Test]
        public void ComputeAngle_AttackerToLeft_ReturnsNegative90()
        {
            // Атакующий слева (−X при forward=+Z)
            Vector3 sourcePos = new Vector3(-5f, 0f, 0f);
            float angle = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward, HeroPos, sourcePos);
            Assert.AreEqual(-90f, angle, AngleTolerance,
                "Атакующий слева → угол −90°.");
        }

        [Test]
        public void ComputeAngle_AttackerDiagonalFrontRight_ReturnsPositive45()
        {
            // Атакующий по диагонали спереди-справа (+X+Z)
            Vector3 sourcePos = new Vector3(5f, 0f, 5f);
            float angle = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward, HeroPos, sourcePos);
            Assert.AreEqual(45f, angle, AngleTolerance,
                "Атакующий по диагонали спереди-справа → угол ≈ +45°.");
        }

        [Test]
        public void ComputeAngle_AttackerDiagonalBehindLeft_ReturnsNegative135()
        {
            // Атакующий по диагонали сзади-слева (−X−Z)
            Vector3 sourcePos = new Vector3(-5f, 0f, -5f);
            float angle = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward, HeroPos, sourcePos);
            Assert.AreEqual(-135f, angle, AngleTolerance,
                "Атакующий по диагонали сзади-слева → угол ≈ −135°.");
        }

        [Test]
        public void ComputeAngle_YOffsetIgnored_SameAsXZResult()
        {
            // Y-координата атакующего не должна влиять на результат
            Vector3 sourcePosGround = new Vector3(5f, 0f, 0f);
            Vector3 sourcePosElevated = new Vector3(5f, 100f, 0f);
            float angleGround   = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward, HeroPos, sourcePosGround);
            float angleElevated = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward, HeroPos, sourcePosElevated);
            Assert.AreEqual(angleGround, angleElevated, AngleTolerance,
                "Y-позиция атакующего не должна влиять на угол (работаем в плоскости XZ).");
        }

        [Test]
        public void ComputeAngle_SamePosition_ReturnsZero()
        {
            // Источник урона совпадает с героем — возвращаем 0
            float angle = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward, HeroPos, HeroPos);
            Assert.AreEqual(0f, angle, AngleTolerance,
                "Источник в той же точке, что герой → безопасный fallback 0°.");
        }

        [Test]
        public void ComputeAngle_ZeroForward_ReturnsZero()
        {
            // Нулевой вектор forward — защита от деления на ноль
            float angle = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Vector3.zero, HeroPos, new Vector3(5f, 0f, 0f));
            Assert.AreEqual(0f, angle, AngleTolerance,
                "Нулевой refForward → безопасный fallback 0°.");
        }

        [Test]
        public void ComputeAngle_ForwardRotated90CW_ShiftsAllAnglesCorrectly()
        {
            // Если герой смотрит вправо (+X), то атака сзади (−X) должна дать 180°
            // а атака «сзади по мировым координатам» должна быть 0° (спереди по refForward=+X)
            Vector3 refRight = Vector3.right; // герой смотрит на +X
            Vector3 attackFromLeft = new Vector3(-5f, 0f, 0f); // мировой "left" = сзади героя
            float angle = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                refRight, HeroPos, attackFromLeft);
            Assert.AreEqual(180f, Mathf.Abs(angle), AngleTolerance,
                "При forward=+X, атака с −X = атака сзади → |угол| = 180°.");
        }

        [Test]
        public void ComputeAngle_NonUnitForward_SameAsNormalized()
        {
            // Метод должен нормализовать forward сам — длина не должна влиять на результат
            Vector3 sourcePos = new Vector3(5f, 0f, 0f);
            float angleUnit   = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward, HeroPos, sourcePos);
            float angleLong   = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                Forward * 10f, HeroPos, sourcePos);
            Assert.AreEqual(angleUnit, angleLong, AngleTolerance,
                "Длина refForwardXZ не должна влиять на результат — метод нормализует.");
        }

        // ================================================================
        // GameFeelSettings — дефолты Circle-23
        // ================================================================

        [Test]
        public void GameFeelSettings_DamageArrowPeakAlpha_IsPositiveAndLeOne()
        {
            var settings = ScriptableObject.CreateInstance<GameFeelSettings>();
            Assert.Greater(settings.damageArrowPeakAlpha, 0f,
                "damageArrowPeakAlpha должна быть > 0.");
            Assert.LessOrEqual(settings.damageArrowPeakAlpha, 1f,
                "damageArrowPeakAlpha не может превышать 1.");
            Object.DestroyImmediate(settings);
        }
    }
}
