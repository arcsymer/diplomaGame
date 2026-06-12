using DiplomaGame.Runtime.GameFeel;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для логики GameFeel: значения SO, математика рекойла, тест HitFlash без рендереров.
    /// Не требуют MonoBehaviour или игровой сцены.
    /// </summary>
    [TestFixture]
    public class GameFeelLogicTests
    {
        // ================================================================
        // GameFeelSettings — дефолтные значения
        // ================================================================

        private GameFeelSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GameFeelSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void GameFeelSettings_DefaultShotImpulseAmplitude_IsPositive()
        {
            Assert.Greater(_settings.shotImpulseAmplitude, 0f,
                "shotImpulseAmplitude должна быть положительной.");
        }

        [Test]
        public void GameFeelSettings_DefaultShotImpulseDecay_IsPositive()
        {
            Assert.Greater(_settings.shotImpulseDecay, 0f,
                "shotImpulseDecay должна быть положительной.");
        }

        [Test]
        public void GameFeelSettings_DefaultShotPitchVariance_IsInRange()
        {
            Assert.GreaterOrEqual(_settings.shotPitchVariance, 0f,
                "shotPitchVariance не может быть отрицательной.");
            Assert.LessOrEqual(_settings.shotPitchVariance, 1f,
                "shotPitchVariance не должна превышать 1 (100%).");
        }

        [Test]
        public void GameFeelSettings_DefaultHitFlashDuration_IsPositive()
        {
            Assert.Greater(_settings.hitFlashDuration, 0f,
                "hitFlashDuration должна быть положительной.");
        }

        [Test]
        public void GameFeelSettings_DefaultRepairFlashDuration_IsPositive()
        {
            Assert.Greater(_settings.repairFlashDuration, 0f,
                "repairFlashDuration должна быть положительной.");
        }

        [Test]
        public void GameFeelSettings_DefaultRepairFlashColor_HasGreenChannel()
        {
            Assert.Greater(_settings.repairFlashColor.g, _settings.repairFlashColor.r,
                "Зелёный канал цвета лечения должен быть больше красного.");
        }

        [Test]
        public void GameFeelSettings_DefaultOverchargeRecoilMult_IsAboveOne()
        {
            Assert.Greater(_settings.overchargeRecoilMult, 1f,
                "overchargeRecoilMult должен усиливать рекойл (> 1).");
        }

        [Test]
        public void GameFeelSettings_DefaultHitstopEnabled_IsTrue()
        {
            Assert.IsTrue(_settings.hitstopEnabled,
                "hitstopEnabled должен быть включён по умолчанию.");
        }

        [Test]
        public void GameFeelSettings_DefaultHitstopDuration_IsPositive()
        {
            Assert.Greater(_settings.hitstopDuration, 0f,
                "hitstopDuration должна быть положительной.");
        }

        [Test]
        public void GameFeelSettings_DefaultHitstopTargetScale_IsNearZero()
        {
            Assert.GreaterOrEqual(_settings.hitstopTargetScale, 0f,
                "hitstopTargetScale не может быть отрицательной.");
            Assert.Less(_settings.hitstopTargetScale, 0.5f,
                "hitstopTargetScale должна быть меньше 0.5 (иначе это не hitstop).");
        }

        [Test]
        public void GameFeelSettings_DefaultKnockbackEnabled_IsTrue()
        {
            Assert.IsTrue(_settings.knockbackEnabled,
                "knockbackEnabled должен быть включён по умолчанию.");
        }

        [Test]
        public void GameFeelSettings_DefaultKnockbackDistance_IsPositive()
        {
            Assert.Greater(_settings.knockbackDistance, 0f,
                "knockbackDistance должна быть положительной.");
        }

        [Test]
        public void GameFeelSettings_DefaultDashTrailDuration_IsPositive()
        {
            Assert.Greater(_settings.dashTrailDuration, 0f,
                "dashTrailDuration должна быть положительной.");
        }

        // ================================================================
        // Математика экспоненциального спада рекойла
        // ================================================================

        [Test]
        public void RecoilDecay_ExponentialFormula_ReducesOverTime()
        {
            // factor = 1 - exp(-decay * dt)
            // x_new = Lerp(x, 0, factor) = x * (1 - factor) = x * exp(-decay * dt)
            const float amplitude = 0.08f;
            const float decay     = 8f;
            const float dt        = 0.016f; // ~60 fps

            float current = amplitude;
            float factor  = 1f - Mathf.Exp(-decay * dt);
            float newVal  = Mathf.Lerp(current, 0f, factor);

            Assert.Less(newVal, current,
                "Значение рекойла должно уменьшаться после спада.");
            Assert.Greater(newVal, 0f,
                "За один кадр рекойл не должен полностью пропасть.");
        }

        [Test]
        public void RecoilDecay_After100Frames_PracticallyZero()
        {
            // После 100 кадров по 16 мс рекойл должен стать практически нулём
            const float amplitude = 0.08f;
            const float decay     = 8f;
            const float dt        = 0.016f;

            float current = amplitude;
            for (int i = 0; i < 100; i++)
            {
                float factor = 1f - Mathf.Exp(-decay * dt);
                current = Mathf.Lerp(current, 0f, factor);
            }

            Assert.Less(Mathf.Abs(current), 0.001f,
                "После 100 кадров рекойл должен стать < 0.001.");
        }

        [Test]
        public void RecoilDecay_HigherDecay_FastersConvergence()
        {
            const float amplitude = 0.08f;
            const float dt        = 0.016f;

            float currentSlow = amplitude;
            float currentFast = amplitude;

            for (int i = 0; i < 20; i++)
            {
                float factorSlow = 1f - Mathf.Exp(-4f  * dt);
                float factorFast = 1f - Mathf.Exp(-16f * dt);
                currentSlow = Mathf.Lerp(currentSlow, 0f, factorSlow);
                currentFast = Mathf.Lerp(currentFast, 0f, factorFast);
            }

            Assert.Less(currentFast, currentSlow,
                "Более высокий decay должен давать более быстрое затухание.");
        }

        [Test]
        public void RecoilDecay_SumOfShots_AccumulatesCorrectly()
        {
            // Два выстрела должны дать сумму amplitude * 2 (до спада)
            const float amplitude = 0.08f;

            float current = 0f;
            current += amplitude;
            current += amplitude;

            Assert.AreEqual(amplitude * 2f, current, 0.0001f,
                "Два выстрела должны суммировать амплитуды рекойла.");
        }

        // ================================================================
        // HitFlashHandler — без рендереров (безопасность)
        // ================================================================

        [Test]
        public void HitFlashHandler_WithoutRenderers_TriggerDoesNotThrow()
        {
            // Создаём GO без MeshRenderer/SkinnedMeshRenderer
            var go = new GameObject("TestFlashNoRenderer");
            var handler = go.AddComponent<HitFlashHandler>();

            Assert.DoesNotThrow(
                () => handler.TriggerFlash(Color.white, 0.1f),
                "TriggerFlash не должен бросать исключение даже при отсутствии рендереров.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void HitFlashHandler_TriggerFlash_NegativeDuration_DoesNotThrow()
        {
            var go = new GameObject("TestFlashNegDur");
            var handler = go.AddComponent<HitFlashHandler>();

            Assert.DoesNotThrow(
                () => handler.TriggerFlash(Color.red, -1f),
                "TriggerFlash с отрицательной длительностью не должен бросать исключение.");

            Object.DestroyImmediate(go);
        }

        // ================================================================
        // DashTrailHandler — создание без TrailRenderer
        // ================================================================

        [Test]
        public void DashTrailHandler_RequireComponent_HasTrailRenderer()
        {
            // DashTrailHandler имеет [RequireComponent(typeof(TrailRenderer))]
            // Unity автоматически добавляет TrailRenderer при AddComponent
            var go = new GameObject("TestDashTrail");
            var handler = go.AddComponent<DashTrailHandler>();

            Assert.IsNotNull(go.GetComponent<TrailRenderer>(),
                "TrailRenderer должен автоматически добавляться вместе с DashTrailHandler.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void DashTrailHandler_TriggerDash_DoesNotThrow()
        {
            var go = new GameObject("TestDashTrailTrigger");
            go.AddComponent<DashTrailHandler>();

            var handler = go.GetComponent<DashTrailHandler>();
            Assert.DoesNotThrow(
                () => handler.TriggerDash(0.25f),
                "TriggerDash не должен бросать исключение.");

            Object.DestroyImmediate(go);
        }

        // ================================================================
        // Overcharge recoil multiplier
        // ================================================================

        [Test]
        public void OverchargeRecoil_Amplitude_IsMultipliedByMult()
        {
            float normal = _settings.shotImpulseAmplitude;
            float overcharged = _settings.shotImpulseAmplitude * _settings.overchargeRecoilMult;

            Assert.Greater(overcharged, normal,
                "Overcharge рекойл должен быть больше обычного.");
        }
    }
}
