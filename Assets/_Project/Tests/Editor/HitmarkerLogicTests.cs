using DiplomaGame.Runtime.UI;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для CrosshairHitmarkerLogic и AbilityReadyLogic.
    /// Не требуют MonoBehaviour или сцены.
    /// </summary>
    [TestFixture]
    public class HitmarkerLogicTests
    {
        // ================================================================
        // CrosshairHitmarkerLogic.Resolve
        // ================================================================

        private static readonly Color BaseWhite = Color.white;
        private static readonly Color HitOrange = new Color(1f, 0.55f, 0f, 1f);
        private const float ExpandScale = 1.15f;
        private const float MissScale   = 1.05f;

        [Test]
        public void Resolve_Hit_ReturnsHitColorAndExpandScale()
        {
            CrosshairHitmarkerLogic.Resolve(
                hit: true,
                baseColor: BaseWhite,
                hitColor: HitOrange,
                expandScale: ExpandScale,
                missScale: MissScale,
                out Color outColor,
                out float outScale);

            Assert.AreEqual(HitOrange, outColor,  "Цвет при попадании должен быть hitColor.");
            Assert.AreEqual(ExpandScale, outScale, "Масштаб при попадании должен быть expandScale.");
        }

        [Test]
        public void Resolve_Miss_ReturnsBaseColorAndMissScale()
        {
            CrosshairHitmarkerLogic.Resolve(
                hit: false,
                baseColor: BaseWhite,
                hitColor: HitOrange,
                expandScale: ExpandScale,
                missScale: MissScale,
                out Color outColor,
                out float outScale);

            Assert.AreEqual(BaseWhite, outColor, "Цвет при промахе должен остаться baseColor.");
            Assert.AreEqual(MissScale, outScale, "Масштаб при промахе должен быть missScale.");
        }

        // ================================================================
        // CrosshairHitmarkerLogic.PingPongScale
        // ================================================================

        [Test]
        public void PingPongScale_AtZero_ReturnsOne()
        {
            float result = CrosshairHitmarkerLogic.PingPongScale(0f, ExpandScale);
            Assert.AreEqual(1f, result, 0.0001f, "В начале (t=0) масштаб должен быть 1.");
        }

        [Test]
        public void PingPongScale_AtHalf_ReturnsPeak()
        {
            float result = CrosshairHitmarkerLogic.PingPongScale(0.5f, ExpandScale);
            Assert.AreEqual(ExpandScale, result, 0.0001f, "В середине (t=0.5) масштаб должен быть peakScale.");
        }

        [Test]
        public void PingPongScale_AtOne_ReturnsOne()
        {
            float result = CrosshairHitmarkerLogic.PingPongScale(1f, ExpandScale);
            Assert.AreEqual(1f, result, 0.0001f, "В конце (t=1) масштаб должен вернуться к 1.");
        }

        [Test]
        public void PingPongScale_AtQuarter_IsBetweenOneAndPeak()
        {
            float result = CrosshairHitmarkerLogic.PingPongScale(0.25f, ExpandScale);
            Assert.Greater(result, 1f,          "При t=0.25 масштаб должен быть выше 1.");
            Assert.Less(result, ExpandScale,    "При t=0.25 масштаб ещё не достиг пика.");
        }

        [Test]
        public void PingPongScale_MissScale_SmallerThanHitScale()
        {
            float hitPeak  = CrosshairHitmarkerLogic.PingPongScale(0.5f, ExpandScale);
            float missPeak = CrosshairHitmarkerLogic.PingPongScale(0.5f, MissScale);

            Assert.Greater(hitPeak, missPeak,
                "Пик масштаба попадания должен быть больше, чем пик промаха.");
        }

        // ================================================================
        // CrosshairHitmarkerLogic.PingPongColor
        // ================================================================

        [Test]
        public void PingPongColor_AtZero_ReturnsBaseColor()
        {
            Color result = CrosshairHitmarkerLogic.PingPongColor(0f, BaseWhite, HitOrange);
            Assert.AreEqual(BaseWhite, result, "При t=0 цвет должен быть базовым.");
        }

        [Test]
        public void PingPongColor_AtHalf_ReturnsTargetColor()
        {
            Color result = CrosshairHitmarkerLogic.PingPongColor(0.5f, BaseWhite, HitOrange);
            Assert.AreEqual(HitOrange, result, "При t=0.5 цвет должен быть targetColor.");
        }

        [Test]
        public void PingPongColor_AtOne_ReturnsBaseColor()
        {
            Color result = CrosshairHitmarkerLogic.PingPongColor(1f, BaseWhite, HitOrange);
            Assert.AreEqual(BaseWhite, result, "При t=1 цвет должен вернуться к базовому.");
        }

        [Test]
        public void PingPongColor_Miss_ColorNeverChanges()
        {
            // При промахе targetColor == baseColor, поэтому интерполяция не меняет цвет
            Color result = CrosshairHitmarkerLogic.PingPongColor(0.5f, BaseWhite, BaseWhite);
            Assert.AreEqual(BaseWhite, result,
                "При совпадающих цветах (промах) цвет не должен меняться.");
        }

        // ================================================================
        // AbilityReadyLogic.IsCoolingDown
        // ================================================================

        [Test]
        public void IsCoolingDown_ZeroRemaining_ReturnsFalse()
        {
            Assert.IsFalse(AbilityReadyLogic.IsCoolingDown(0f),
                "Remaining=0 → способность готова, не на КД.");
        }

        [Test]
        public void IsCoolingDown_PositiveRemaining_ReturnsTrue()
        {
            Assert.IsTrue(AbilityReadyLogic.IsCoolingDown(3f),
                "Remaining>0 → способность ещё на КД.");
        }

        [Test]
        public void IsCoolingDown_NegativeRemaining_ReturnsFalse()
        {
            Assert.IsFalse(AbilityReadyLogic.IsCoolingDown(-0.1f),
                "Отрицательный остаток → тоже считается готовой.");
        }

        // ================================================================
        // AbilityReadyLogic.DetectReadyEdge
        // ================================================================

        [Test]
        public void DetectReadyEdge_WasCoolingWasZero_ReturnsTrue()
        {
            // Переход: прошлый кадр wasCooling=true, текущий remaining=0 → EDGE
            Assert.IsTrue(
                AbilityReadyLogic.DetectReadyEdge(wasCoolingDown: true, currentRemaining: 0f),
                "Переход из КД в готовность должен давать edge=true.");
        }

        [Test]
        public void DetectReadyEdge_WasNotCoolingAndZero_ReturnsFalse()
        {
            // Способность уже была готова, remaining=0 → НЕТ edge (не новый)
            Assert.IsFalse(
                AbilityReadyLogic.DetectReadyEdge(wasCoolingDown: false, currentRemaining: 0f),
                "Если предыдущий кадр уже был готов — edge не должен срабатывать.");
        }

        [Test]
        public void DetectReadyEdge_StillCooling_ReturnsFalse()
        {
            // КД не закончился — никакого edge
            Assert.IsFalse(
                AbilityReadyLogic.DetectReadyEdge(wasCoolingDown: true, currentRemaining: 1.5f),
                "Пока КД > 0 edge не должен срабатывать.");
        }

        [Test]
        public void DetectReadyEdge_WasNotCoolingAndPositive_ReturnsFalse()
        {
            // Способность была готова, но вдруг остаток > 0 (например, повторное использование)
            // В этом кадре — не edge перехода «стало готово».
            Assert.IsFalse(
                AbilityReadyLogic.DetectReadyEdge(wasCoolingDown: false, currentRemaining: 2f),
                "Если не было КД — edge не должен срабатывать независимо от remaining.");
        }

        // ================================================================
        // GameFeelSettings — дефолты хитмаркера
        // ================================================================

        [Test]
        public void GameFeelSettings_HitmarkerDuration_IsPositive()
        {
            var settings = ScriptableObject.CreateInstance<DiplomaGame.Runtime.GameFeel.GameFeelSettings>();
            Assert.Greater(settings.hitmarkerDuration, 0f,
                "hitmarkerDuration должна быть положительной.");
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void GameFeelSettings_HitmarkerExpandScale_IsAboveOne()
        {
            var settings = ScriptableObject.CreateInstance<DiplomaGame.Runtime.GameFeel.GameFeelSettings>();
            Assert.Greater(settings.hitmarkerExpandScale, 1f,
                "hitmarkerExpandScale должен быть больше 1 (раскрытие).");
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void GameFeelSettings_HitmarkerMissScale_IsBetweenOneAndExpandScale()
        {
            var settings = ScriptableObject.CreateInstance<DiplomaGame.Runtime.GameFeel.GameFeelSettings>();
            Assert.Greater(settings.hitmarkerMissScale, 1f,
                "hitmarkerMissScale должен быть больше 1.");
            Assert.Less(settings.hitmarkerMissScale, settings.hitmarkerExpandScale,
                "hitmarkerMissScale должен быть меньше hitmarkerExpandScale (промах — слабее).");
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void GameFeelSettings_HitmarkerColorHit_HasWarmOrangeTone()
        {
            var settings = ScriptableObject.CreateInstance<DiplomaGame.Runtime.GameFeel.GameFeelSettings>();
            // Тёплый оранжевый: R=1, G~0.55, B~0
            Assert.AreEqual(1f, settings.hitmarkerColorHit.r, 0.01f,
                "Красный канал hitmarkerColorHit должен быть максимальным (тёплый).");
            Assert.Greater(settings.hitmarkerColorHit.r, settings.hitmarkerColorHit.b,
                "Красный должен быть больше синего (тёплый оттенок).");
            Object.DestroyImmediate(settings);
        }
    }
}
