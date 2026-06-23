using DiplomaGame.Runtime.Hero;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для SprintStaminaLogic.
    /// Детерминированы — не требуют сцены.
    /// </summary>
    [TestFixture]
    public class SprintStaminaLogicTests
    {
        private const float Epsilon = 0.0001f;

        // Дефолтные параметры, соответствующие проектным значениям
        private const float DrainRate   = 25f;
        private const float RegenRate   = 15f;
        private const float Max         = 100f;
        private const float MinToStart  = 10f;

        // ================================================================
        // Drain: спринт + движение расходует стамину
        // ================================================================

        [Test]
        public void Tick_SprintingAndMoving_DrainsStamina()
        {
            // 100 стамины, спринт зажат, есть движение, dt=1 → 100 - 25 = 75
            var (stamina, isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: 100f,
                sprintHeld:     true,
                isMoving:       true,
                dt:             1f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   false);

            Assert.AreEqual(75f,  stamina,    Epsilon, "После 1с спринта стамина должна упасть на drainRate.");
            Assert.IsTrue(isSprinting, "Должны спринтовать при достаточной стамине.");
        }

        // ================================================================
        // Regen: не спринтуем → стамина восстанавливается
        // ================================================================

        [Test]
        public void Tick_NotSprinting_RegensStamina()
        {
            // 50 стамины, кнопка не зажата, dt=1 → 50 + 15 = 65
            var (stamina, isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: 50f,
                sprintHeld:     false,
                isMoving:       true,
                dt:             1f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   false);

            Assert.AreEqual(65f,   stamina,    Epsilon, "Без спринта стамина регенерирует.");
            Assert.IsFalse(isSprinting, "Не должны спринтовать без зажатой кнопки.");
        }

        [Test]
        public void Tick_Regen_DoesNotExceedMax()
        {
            // 98 стамины, dt=1 → 98+15=113 → зажать в 100
            var (stamina, _) = SprintStaminaLogic.Tick(
                currentStamina: 98f,
                sprintHeld:     false,
                isMoving:       false,
                dt:             1f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   false);

            Assert.AreEqual(Max, stamina, Epsilon, "Стамина не может превысить максимум.");
        }

        // ================================================================
        // Нельзя начать спринт ниже минимума
        // ================================================================

        [Test]
        public void Tick_BelowMinToStart_CannotStartSprint()
        {
            // Стамина 5 (< 10), кнопка зажата, движение есть, wasSprinting=false → не спринтуем
            var (_, isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: 5f,
                sprintHeld:     true,
                isMoving:       true,
                dt:             0.016f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   false);

            Assert.IsFalse(isSprinting, "Нельзя начать спринт при стамине ниже minToStart.");
        }

        [Test]
        public void Tick_ExactlyAtMinToStart_CanStartSprint()
        {
            // Стамина ровно 10 = minToStart → можно начать
            var (_, isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: 10f,
                sprintHeld:     true,
                isMoving:       true,
                dt:             0.016f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   false);

            Assert.IsTrue(isSprinting, "При стамине == minToStart спринт должен начинаться.");
        }

        // ================================================================
        // Auto-stop: стамина кончилась → спринт прерывается
        // ================================================================

        [Test]
        public void Tick_StaminaHitsZero_SprintAutoStops()
        {
            // 5 стамины, dt=1, drain=25 → было бы -20, зажимается в 0, спринт = false
            var (stamina, isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: 5f,
                sprintHeld:     true,
                isMoving:       true,
                dt:             1f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   true);   // уже спринтовали — ниже минимума порога нет

            Assert.AreEqual(0f,   stamina,    Epsilon, "Стамина не может уйти ниже нуля.");
            Assert.IsFalse(isSprinting, "Спринт должен прерваться при достижении нулевой стамины.");
        }

        [Test]
        public void Tick_StaminaZero_WasSprinting_KeyHeld_StillStops()
        {
            // Стамина уже 0, wasSprinting=true, кнопка зажата → isSprinting = false (нет стамины)
            var (stamina, isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: 0f,
                sprintHeld:     true,
                isMoving:       true,
                dt:             0.016f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   true);

            Assert.IsFalse(isSprinting, "При нулевой стамине спринт не активен даже с зажатой кнопкой.");
            // При isSprinting=false — регенерация
            Assert.Greater(stamina, 0f, "Пока спринт не активен, стамина начинает восстанавливаться.");
        }

        // ================================================================
        // Стоим на месте: держим кнопку, но нет движения → нет дрейна
        // ================================================================

        [Test]
        public void Tick_SprintHeld_ButNotMoving_NoDrain()
        {
            // Кнопка зажата, но isMoving = false → спринт неактивен, стамина регенерирует
            var (stamina, isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: 80f,
                sprintHeld:     true,
                isMoving:       false,
                dt:             1f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   false);

            Assert.IsFalse(isSprinting, "Без движения спринт не активен.");
            Assert.AreEqual(95f, stamina, Epsilon, "Без движения стамина регенерирует, а не тратится.");
        }

        [Test]
        public void Tick_SprintHeld_WasSprinting_ButStoppedMoving_SprintCancels()
        {
            // Бежали, потом остановились → спринт прерывается и стамина регенерирует
            var (stamina, isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: 60f,
                sprintHeld:     true,
                isMoving:       false,
                dt:             1f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   true);

            Assert.IsFalse(isSprinting, "Остановка отменяет спринт.");
            Assert.AreEqual(75f, stamina, Epsilon, "После отмены спринта стамина регенерирует.");
        }

        // ================================================================
        // Продолжение спринта ниже minToStart (wasSprinting=true)
        // ================================================================

        [Test]
        public void Tick_WasSprinting_BelowMinToStart_ContinuesSprint()
        {
            // Спринтовали, стамина упала до 8 (< 10), но wasSprinting=true → продолжаем
            var (_, isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: 8f,
                sprintHeld:     true,
                isMoving:       true,
                dt:             0.016f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   true);

            Assert.IsTrue(isSprinting, "Если уже спринтовали, то ниже minToStart спринт продолжается до 0.");
        }

        // ================================================================
        // Drain не уходит ниже 0
        // ================================================================

        [Test]
        public void Tick_Drain_DoesNotGoBelowZero()
        {
            var (stamina, _) = SprintStaminaLogic.Tick(
                currentStamina: 1f,
                sprintHeld:     true,
                isMoving:       true,
                dt:             1f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   true);

            Assert.AreEqual(0f, stamina, Epsilon, "Стамина не может уйти ниже нуля.");
        }

        // ================================================================
        // SprintHeld = false → никогда не спринтуем
        // ================================================================

        [Test]
        public void Tick_SprintNotHeld_NeverSprints()
        {
            var (_, isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: 100f,
                sprintHeld:     false,
                isMoving:       true,
                dt:             0.016f,
                drainRate:      DrainRate,
                regenRate:      RegenRate,
                max:            Max,
                minToStart:     MinToStart,
                wasSprinting:   false);

            Assert.IsFalse(isSprinting, "Без нажатой кнопки спринт не активен.");
        }
    }
}
