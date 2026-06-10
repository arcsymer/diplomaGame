using DiplomaGame.Runtime.Hero;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для FireRateLogic и AbilityCooldownLogic.
    /// Не требуют сцены — вся логика статическая.
    /// </summary>
    [TestFixture]
    public class CooldownLogicTests
    {
        private const float Epsilon = 0.0001f;

        // ================================================================
        // FireRateLogic.CanFire
        // ================================================================

        [Test]
        public void CanFire_FirstShot_ReturnsTrue()
        {
            // lastFireTime = 0, cooldown = 0.15, now = 0 → 0 - 0 = 0 >= 0.15? No
            // Но now=0, lastFireTime=0, cooldown=0 → 0>=0 true
            // Для первого выстрела (lastFireTime=0, now=0): CanFire(0, 0.15, 0) → 0>=0.15 false
            // Чтобы первый выстрел прошёл сразу, lastFireTime должен быть достаточно маленьким
            // В реальном коде lastFireTime=0, now>=cooldown при старте игры (Time.time > 0.15)
            // Проверяем: now = 1.0, lastFireTime = 0, cooldown = 0.15 → 1.0 >= 0.15 → true
            Assert.IsTrue(FireRateLogic.CanFire(0f, 0.15f, 1.0f),
                "Первый выстрел через 1с после запуска должен быть разрешён.");
        }

        [Test]
        public void CanFire_ExactlyAtCooldown_ReturnsTrue()
        {
            // Граничное условие: now - lastFireTime == cooldown → разрешено
            Assert.IsTrue(FireRateLogic.CanFire(1.0f, 0.15f, 1.15f),
                "На границе кулдауна (now - last == cooldown) выстрел должен быть разрешён.");
        }

        [Test]
        public void CanFire_JustUnderCooldown_ReturnsFalse()
        {
            // now - lastFireTime < cooldown → запрещено
            Assert.IsFalse(FireRateLogic.CanFire(1.0f, 0.15f, 1.14f),
                "До истечения кулдауна выстрел должен быть запрещён.");
        }

        [Test]
        public void CanFire_AfterCooldown_ReturnsTrue()
        {
            // now - lastFireTime > cooldown → разрешено
            Assert.IsTrue(FireRateLogic.CanFire(1.0f, 0.15f, 1.5f),
                "После истечения кулдауна выстрел должен быть разрешён.");
        }

        [Test]
        public void CanFire_ZeroCooldown_AlwaysTrue()
        {
            // cooldown = 0 → всегда разрешено (теоретический минимум)
            Assert.IsTrue(FireRateLogic.CanFire(5.0f, 0f, 5.0f),
                "При нулевом кулдауне выстрел всегда разрешён.");
        }

        [Test]
        public void CanFire_SameTime_ZeroCooldown_ReturnsTrue()
        {
            // now == lastFireTime, cooldown == 0 → 0 >= 0 → true
            Assert.IsTrue(FireRateLogic.CanFire(2.0f, 0f, 2.0f));
        }

        // ================================================================
        // AbilityCooldownLogic.IsReady
        // ================================================================

        [Test]
        public void IsReady_ZeroRemaining_ReturnsTrue()
        {
            Assert.IsTrue(AbilityCooldownLogic.IsReady(0f),
                "При нулевом оставшемся кулдауне способность готова.");
        }

        [Test]
        public void IsReady_NegativeRemaining_ReturnsTrue()
        {
            // Отрицательный кулдаун (перешли через 0) — тоже готова
            Assert.IsTrue(AbilityCooldownLogic.IsReady(-1f),
                "При отрицательном остатке способность готова.");
        }

        [Test]
        public void IsReady_PositiveRemaining_ReturnsFalse()
        {
            Assert.IsFalse(AbilityCooldownLogic.IsReady(1f),
                "При положительном остатке способность не готова.");
        }

        [Test]
        public void IsReady_SmallPositiveRemaining_ReturnsFalse()
        {
            Assert.IsFalse(AbilityCooldownLogic.IsReady(0.001f),
                "Малый положительный остаток → способность не готова.");
        }

        // ================================================================
        // AbilityCooldownLogic.Tick
        // ================================================================

        [Test]
        public void Tick_ReducesRemainingCooldown()
        {
            float result = AbilityCooldownLogic.Tick(4f, 0.5f);

            Assert.AreEqual(3.5f, result, Epsilon, "Tick(4, 0.5) должен вернуть 3.5.");
        }

        [Test]
        public void Tick_DoesNotGoBelowZero()
        {
            float result = AbilityCooldownLogic.Tick(0.1f, 1.0f);

            Assert.AreEqual(0f, result, Epsilon, "Tick не может вернуть отрицательное значение.");
        }

        [Test]
        public void Tick_ZeroRemaining_StaysZero()
        {
            float result = AbilityCooldownLogic.Tick(0f, 0.016f);

            Assert.AreEqual(0f, result, Epsilon, "Tick(0, dt) должен возвращать 0.");
        }

        [Test]
        public void Tick_ExactlyDepleted_ReturnsZero()
        {
            // remaining = 0.016, dt = 0.016 → 0
            float result = AbilityCooldownLogic.Tick(0.016f, 0.016f);

            Assert.AreEqual(0f, result, Epsilon, "Когда оставшееся равно dt, результат 0.");
        }

        // ================================================================
        // AbilityCooldownLogic.StartCooldown
        // ================================================================

        [Test]
        public void StartCooldown_ReturnsDuration()
        {
            float result = AbilityCooldownLogic.StartCooldown(8f);

            Assert.AreEqual(8f, result, Epsilon, "StartCooldown(8) должен вернуть 8.");
        }

        [Test]
        public void StartCooldown_ZeroDuration_ReturnsZero()
        {
            float result = AbilityCooldownLogic.StartCooldown(0f);

            Assert.AreEqual(0f, result, Epsilon, "StartCooldown(0) должен вернуть 0.");
        }
    }
}
