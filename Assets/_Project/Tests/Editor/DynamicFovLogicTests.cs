using DiplomaGame.Runtime.GameFeel;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для <see cref="DynamicFovLogic"/>.
    /// Не требуют MonoBehaviour или сцены — чистая математика.
    /// </summary>
    [TestFixture]
    public class DynamicFovLogicTests
    {
        private const float BaseFov      = 60f;
        private const float KickAmount   = 9f;
        private const float KickDuration = 0.08f;
        private const float ReturnSpeed  = 12f;
        private const float Dt60         = 1f / 60f;   // ~0.0167 с, 60 fps

        // ================================================================
        // TriggerKick
        // ================================================================

        [Test]
        public void TriggerKick_PositiveDuration_ReturnsSameDuration()
        {
            float result = DynamicFovLogic.TriggerKick(KickDuration);
            Assert.AreEqual(KickDuration, result, 0.0001f,
                "TriggerKick должен вернуть kickDuration как начальный таймер.");
        }

        [Test]
        public void TriggerKick_NegativeDuration_ReturnsZero()
        {
            float result = DynamicFovLogic.TriggerKick(-1f);
            Assert.AreEqual(0f, result, 0.0001f,
                "TriggerKick с отрицательной длительностью должен вернуть 0.");
        }

        [Test]
        public void TriggerKick_Zero_ReturnsZero()
        {
            float result = DynamicFovLogic.TriggerKick(0f);
            Assert.AreEqual(0f, result, 0.0001f,
                "TriggerKick(0) должен вернуть 0.");
        }

        // ================================================================
        // GetTargetFov
        // ================================================================

        [Test]
        public void GetTargetFov_KickActive_ReturnsWidenedFov()
        {
            float target = DynamicFovLogic.GetTargetFov(BaseFov, KickAmount, kickRemaining: 0.05f);
            Assert.AreEqual(BaseFov + KickAmount, target, 0.0001f,
                "При активном kick целевой FOV = baseFov + kickAmount.");
        }

        [Test]
        public void GetTargetFov_NoKick_ReturnsBaseFov()
        {
            float target = DynamicFovLogic.GetTargetFov(BaseFov, KickAmount, kickRemaining: 0f);
            Assert.AreEqual(BaseFov, target, 0.0001f,
                "Без kick целевой FOV = baseFov.");
        }

        [Test]
        public void GetTargetFov_KickAmountZero_EqualToBase()
        {
            float target = DynamicFovLogic.GetTargetFov(BaseFov, kickAmount: 0f, kickRemaining: 1f);
            Assert.AreEqual(BaseFov, target, 0.0001f,
                "При kickAmount=0 целевой FOV = baseFov даже с активным таймером.");
        }

        // ================================================================
        // StepFov
        // ================================================================

        [Test]
        public void StepFov_CurrentEqualTarget_ReturnsTarget()
        {
            float result = DynamicFovLogic.StepFov(BaseFov, BaseFov, ReturnSpeed, Dt60);
            Assert.AreEqual(BaseFov, result, 0.0001f,
                "Если current == target, StepFov должен вернуть target.");
        }

        [Test]
        public void StepFov_MovesCloserToTarget()
        {
            float current = BaseFov + KickAmount;    // 69°
            float target  = BaseFov;                 // 60°
            float result  = DynamicFovLogic.StepFov(current, target, ReturnSpeed, Dt60);
            Assert.Less(result, current,
                "StepFov должен приближаться к target.");
            Assert.Greater(result, target,
                "StepFov за один кадр не должен перескочить target.");
        }

        [Test]
        public void StepFov_HighReturnSpeed_ConvergesFast()
        {
            float current    = BaseFov + KickAmount;
            float resultFast = DynamicFovLogic.StepFov(current, BaseFov, returnSpeed: 100f,  dt: Dt60);
            float resultSlow = DynamicFovLogic.StepFov(current, BaseFov, returnSpeed: 2f,    dt: Dt60);
            Assert.Less(Mathf.Abs(resultFast - BaseFov), Mathf.Abs(resultSlow - BaseFov),
                "Более высокий returnSpeed должен давать более быстрое сближение.");
        }

        [Test]
        public void StepFov_AfterManyFrames_ReachesBaseFov()
        {
            float current = BaseFov + KickAmount;
            for (int i = 0; i < 120; i++)
                current = DynamicFovLogic.StepFov(current, BaseFov, ReturnSpeed, Dt60);

            Assert.AreEqual(BaseFov, current, 0.01f,
                "После 120 кадров FOV должен вернуться к baseFov (< 0.01° погрешность).");
        }

        [Test]
        public void StepFov_ZeroDt_ReturnsCurrent()
        {
            float current = BaseFov + KickAmount;
            float result  = DynamicFovLogic.StepFov(current, BaseFov, ReturnSpeed, dt: 0f);
            Assert.AreEqual(current, result, 0.0001f,
                "При dt=0 StepFov не должен двигаться (Lerp(x, y, 0) = x).");
        }

        // ================================================================
        // TickKick
        // ================================================================

        [Test]
        public void TickKick_ReducesTimer()
        {
            float result = DynamicFovLogic.TickKick(KickDuration, Dt60);
            Assert.Less(result, KickDuration,
                "TickKick должен уменьшать kickRemaining.");
        }

        [Test]
        public void TickKick_DoesNotGoBelowZero()
        {
            float result = DynamicFovLogic.TickKick(0.001f, dt: 1f);
            Assert.AreEqual(0f, result, 0.0001f,
                "TickKick не должен возвращать отрицательное значение.");
        }

        [Test]
        public void TickKick_AlreadyZero_StaysZero()
        {
            float result = DynamicFovLogic.TickKick(0f, Dt60);
            Assert.AreEqual(0f, result, 0.0001f,
                "TickKick(0) должен оставаться 0.");
        }

        [Test]
        public void TickKick_ExpiresAfterDuration()
        {
            float timer = KickDuration;
            // Убываем шагами по Dt60 до исчерпания
            int steps = Mathf.CeilToInt(KickDuration / Dt60) + 5;
            for (int i = 0; i < steps; i++)
                timer = DynamicFovLogic.TickKick(timer, Dt60);

            Assert.AreEqual(0f, timer, 0.0001f,
                "Kick-таймер должен достичь 0 после истечения kickDuration.");
        }

        // ================================================================
        // Tick (all-in-one)
        // ================================================================

        [Test]
        public void Tick_WithActiveKick_FovIsWidened()
        {
            var (nextFov, nextKick) = DynamicFovLogic.Tick(
                currentFov:    BaseFov,
                baseFov:       BaseFov,
                kickAmount:    KickAmount,
                kickRemaining: KickDuration,
                returnSpeed:   ReturnSpeed,
                dt:            Dt60);

            Assert.Greater(nextFov, BaseFov,
                "При активном kick nextFov должен быть больше baseFov.");
            Assert.Less(nextKick, KickDuration,
                "Kick-таймер должен убыть.");
        }

        [Test]
        public void Tick_NoKick_FovStaysAtBase()
        {
            var (nextFov, nextKick) = DynamicFovLogic.Tick(
                currentFov:    BaseFov,
                baseFov:       BaseFov,
                kickAmount:    KickAmount,
                kickRemaining: 0f,
                returnSpeed:   ReturnSpeed,
                dt:            Dt60);

            Assert.AreEqual(BaseFov, nextFov, 0.0001f,
                "Без kick FOV должен оставаться на baseFov.");
            Assert.AreEqual(0f, nextKick, 0.0001f,
                "Без kick таймер остаётся 0.");
        }

        [Test]
        public void Tick_KickTriggered_ThenReturnsToBase()
        {
            // Симулируем: 1 кадр kick, затем 120 кадров возврата
            float currentFov    = BaseFov;
            float kickRemaining = DynamicFovLogic.TriggerKick(KickDuration);

            // Первый кадр — FOV уходит к widened
            (currentFov, kickRemaining) = DynamicFovLogic.Tick(
                currentFov, BaseFov, KickAmount, kickRemaining, ReturnSpeed, Dt60);

            Assert.Greater(currentFov, BaseFov,
                "Сразу после kick FOV должен быть > baseFov.");

            // Продолжаем тикать до возврата
            for (int i = 0; i < 120; i++)
                (currentFov, kickRemaining) = DynamicFovLogic.Tick(
                    currentFov, BaseFov, KickAmount, kickRemaining, ReturnSpeed, Dt60);

            Assert.AreEqual(BaseFov, currentFov, 0.1f,
                "После возврата FOV должен быть близок к baseFov.");
        }

        // ================================================================
        // GameFeelSettings — дефолты Circle-22
        // ================================================================

        [Test]
        public void GameFeelSettings_FovKickAmount_DefaultIsPositive()
        {
            var s = ScriptableObject.CreateInstance<GameFeelSettings>();
            Assert.Greater(s.fovKickAmount, 0f,
                "fovKickAmount по умолчанию должен быть положительным.");
            Object.DestroyImmediate(s);
        }

        [Test]
        public void GameFeelSettings_FovKickDuration_DefaultIsPositive()
        {
            var s = ScriptableObject.CreateInstance<GameFeelSettings>();
            Assert.Greater(s.fovKickDuration, 0f,
                "fovKickDuration по умолчанию должен быть положительным.");
            Object.DestroyImmediate(s);
        }

        [Test]
        public void GameFeelSettings_FovReturnSpeed_DefaultIsPositive()
        {
            var s = ScriptableObject.CreateInstance<GameFeelSettings>();
            Assert.Greater(s.fovReturnSpeed, 0f,
                "fovReturnSpeed по умолчанию должен быть положительным.");
            Object.DestroyImmediate(s);
        }

        [Test]
        public void GameFeelSettings_FovKickAmount_DefaultInExpectedRange()
        {
            var s = ScriptableObject.CreateInstance<GameFeelSettings>();
            Assert.GreaterOrEqual(s.fovKickAmount, 6f,
                "fovKickAmount должен быть >= 6° (ощутимый kick).");
            Assert.LessOrEqual(s.fovKickAmount, 15f,
                "fovKickAmount должен быть <= 15° (не слишком агрессивно).");
            Object.DestroyImmediate(s);
        }

        [Test]
        public void GameFeelSettings_FovReturnSpeed_DefaultEnablesFastReturn()
        {
            // При returnSpeed=12 и dt=1/60, kick ~9° должен вернуться за ~0.5 с (30 кадров)
            var s       = ScriptableObject.CreateInstance<GameFeelSettings>();
            float fov   = BaseFov + s.fovKickAmount;
            for (int i = 0; i < 30; i++)
                fov = DynamicFovLogic.StepFov(fov, BaseFov, s.fovReturnSpeed, Dt60);

            Assert.AreEqual(BaseFov, fov, 0.5f,
                "С дефолтным returnSpeed возврат за 30 кадров должен быть почти полным (< 0.5°).");
            Object.DestroyImmediate(s);
        }

        // ================================================================
        // GameFeelSettings — дефолты Circle-24 (sprint-widen)
        // ================================================================

        [Test]
        public void GameFeelSettings_FovSprintWiden_DefaultIsPositive()
        {
            var s = ScriptableObject.CreateInstance<GameFeelSettings>();
            Assert.Greater(s.fovSprintWiden, 0f,
                "fovSprintWiden по умолчанию должен быть положительным.");
            Object.DestroyImmediate(s);
        }

        [Test]
        public void GameFeelSettings_FovSprintWiden_DefaultInExpectedRange()
        {
            var s = ScriptableObject.CreateInstance<GameFeelSettings>();
            Assert.GreaterOrEqual(s.fovSprintWiden, 2f,
                "fovSprintWiden должен быть >= 2° (заметное расширение).");
            Assert.LessOrEqual(s.fovSprintWiden, 8f,
                "fovSprintWiden должен быть <= 8° (без укачивания).");
            Object.DestroyImmediate(s);
        }

        // ================================================================
        // GetTargetFov (sprint-widen overload) — Circle-24
        // ================================================================

        [Test]
        public void GetTargetFov_SprintingNoKick_ReturnsBaseWithSprintWiden()
        {
            const float SprintWiden = 4f;
            float target = DynamicFovLogic.GetTargetFov(
                BaseFov, KickAmount, kickRemaining: 0f,
                isSprinting: true, sprintWiden: SprintWiden);

            Assert.AreEqual(BaseFov + SprintWiden, target, 0.0001f,
                "При спринте (без kick) target = baseFov + sprintWiden.");
        }

        [Test]
        public void GetTargetFov_NotSprintingNoKick_ReturnsBase()
        {
            const float SprintWiden = 4f;
            float target = DynamicFovLogic.GetTargetFov(
                BaseFov, KickAmount, kickRemaining: 0f,
                isSprinting: false, sprintWiden: SprintWiden);

            Assert.AreEqual(BaseFov, target, 0.0001f,
                "Без спринта и без kick target = baseFov.");
        }

        [Test]
        public void GetTargetFov_SprintingWithKick_ReturnsCombined()
        {
            const float SprintWiden = 4f;
            float target = DynamicFovLogic.GetTargetFov(
                BaseFov, KickAmount, kickRemaining: 0.05f,
                isSprinting: true, sprintWiden: SprintWiden);

            Assert.AreEqual(BaseFov + SprintWiden + KickAmount, target, 0.0001f,
                "Спринт + активный kick: target = baseFov + sprintWiden + kickAmount.");
        }

        [Test]
        public void GetTargetFov_KickOnly_ReturnsBaseWithKick()
        {
            const float SprintWiden = 4f;
            float target = DynamicFovLogic.GetTargetFov(
                BaseFov, KickAmount, kickRemaining: 0.05f,
                isSprinting: false, sprintWiden: SprintWiden);

            Assert.AreEqual(BaseFov + KickAmount, target, 0.0001f,
                "Только kick (без спринта): target = baseFov + kickAmount.");
        }

        [Test]
        public void GetTargetFov_SprintWidenZero_BehavesLikeNoWiden()
        {
            float target = DynamicFovLogic.GetTargetFov(
                BaseFov, KickAmount, kickRemaining: 0f,
                isSprinting: true, sprintWiden: 0f);

            Assert.AreEqual(BaseFov, target, 0.0001f,
                "При sprintWiden=0 sprint-флаг не влияет на target.");
        }

        // ================================================================
        // Tick (sprint overload) — Circle-24
        // ================================================================

        [Test]
        public void Tick_Sprint_FovWidens()
        {
            const float SprintWiden = 4f;
            var (nextFov, _) = DynamicFovLogic.Tick(
                currentFov:    BaseFov,
                baseFov:       BaseFov,
                kickAmount:    KickAmount,
                kickRemaining: 0f,
                returnSpeed:   ReturnSpeed,
                dt:            Dt60,
                isSprinting:   true,
                sprintWiden:   SprintWiden);

            Assert.Greater(nextFov, BaseFov,
                "При спринте FOV должен быть выше базового.");
        }

        [Test]
        public void Tick_SprintStops_FovReturnsToBase()
        {
            const float SprintWiden = 4f;

            // Разгоняем FOV спринтом до насыщения
            float fov  = BaseFov;
            float kick = 0f;
            for (int i = 0; i < 60; i++)
                (fov, kick) = DynamicFovLogic.Tick(fov, BaseFov, KickAmount, kick, ReturnSpeed, Dt60, true, SprintWiden);

            Assert.Greater(fov, BaseFov, "После 60 кадров спринта FOV должен быть выше базового.");

            // Прекращаем спринт — FOV должен вернуться к baseFov
            for (int i = 0; i < 120; i++)
                (fov, kick) = DynamicFovLogic.Tick(fov, BaseFov, KickAmount, kick, ReturnSpeed, Dt60, false, SprintWiden);

            Assert.AreEqual(BaseFov, fov, 0.05f,
                "После остановки спринта FOV должен вернуться к baseFov.");
        }

        [Test]
        public void Tick_SprintPlusKick_FovIsHigherThanEitherAlone()
        {
            const float SprintWiden = 4f;

            var (fovBoth, _) = DynamicFovLogic.Tick(
                BaseFov, BaseFov, KickAmount, kickRemaining: KickDuration,
                ReturnSpeed, Dt60, isSprinting: true, sprintWiden: SprintWiden);

            var (fovKickOnly, _) = DynamicFovLogic.Tick(
                BaseFov, BaseFov, KickAmount, kickRemaining: KickDuration,
                ReturnSpeed, Dt60, isSprinting: false, sprintWiden: SprintWiden);

            var (fovSprintOnly, _) = DynamicFovLogic.Tick(
                BaseFov, BaseFov, KickAmount, kickRemaining: 0f,
                ReturnSpeed, Dt60, isSprinting: true, sprintWiden: SprintWiden);

            Assert.Greater(fovBoth, fovKickOnly,
                "Спринт + kick должен давать больший FOV, чем только kick.");
            Assert.Greater(fovBoth, fovSprintOnly,
                "Спринт + kick должен давать больший FOV, чем только спринт.");
        }

        [Test]
        public void Tick_SprintOverload_BackwardsCompatible_NoSprintSameasOldTick()
        {
            // Старый Tick(6 арг) и новый Tick(8 арг, isSprinting=false, sprintWiden=0)
            // должны давать одинаковый результат.
            var (fovOld, kickOld) = DynamicFovLogic.Tick(
                BaseFov, BaseFov, KickAmount, KickDuration, ReturnSpeed, Dt60);

            var (fovNew, kickNew) = DynamicFovLogic.Tick(
                BaseFov, BaseFov, KickAmount, KickDuration, ReturnSpeed, Dt60,
                isSprinting: false, sprintWiden: 0f);

            Assert.AreEqual(fovOld, fovNew, 0.0001f,
                "Новый Tick с isSprinting=false/sprintWiden=0 должен совпадать со старым.");
            Assert.AreEqual(kickOld, kickNew, 0.0001f,
                "Kick-таймер не должен изменяться при совместимом вызове.");
        }
    }
}
