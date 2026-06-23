using DiplomaGame.Runtime.UI;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для чистой статики UnderAttackAlertLogic.
    /// Нет MonoBehaviour — запускаются мгновенно без сцены.
    /// </summary>
    [TestFixture]
    public class UnderAttackAlertLogicTests
    {
        // ----------------------------------------------------------------
        // Константы (дублируем для читаемости assert-ов)
        // ----------------------------------------------------------------

        private const float Throttle = UnderAttackAlertLogic.ThrottleWindow;     // 3f
        private const float Clear    = UnderAttackAlertLogic.ClearAfterSeconds;  // 3f

        // ----------------------------------------------------------------
        // ShouldTrigger — дросселирование
        // ----------------------------------------------------------------

        [Test]
        public void ShouldTrigger_FirstEver_ReturnsTrue()
        {
            // При инициализации _lastTriggerTime = float.NegativeInfinity —
            // алерт ни разу не срабатывал, должен пройти немедленно.
            Assert.IsTrue(
                UnderAttackAlertLogic.ShouldTrigger(float.NegativeInfinity, 0f),
                "Первый алерт (lastTriggerTime = -∞) должен возвращать true.");
        }

        [Test]
        public void ShouldTrigger_AfterFullWindow_ReturnsTrue()
        {
            // Ровно через ThrottleWindow секунд после последнего тригера — разрешаем.
            float last = 10f;
            float now  = last + Throttle;
            Assert.IsTrue(
                UnderAttackAlertLogic.ShouldTrigger(last, now),
                $"Спустя ровно {Throttle}s алерт должен разрешаться.");
        }

        [Test]
        public void ShouldTrigger_AfterMoreThanWindow_ReturnsTrue()
        {
            float last = 5f;
            float now  = last + Throttle + 1f;
            Assert.IsTrue(
                UnderAttackAlertLogic.ShouldTrigger(last, now),
                "Спустя больше ThrottleWindow алерт должен разрешаться.");
        }

        [Test]
        public void ShouldTrigger_TooSoon_ReturnsFalse()
        {
            // Менее чем через ThrottleWindow — дросселируем.
            float last = 10f;
            float now  = last + Throttle - 0.01f;
            Assert.IsFalse(
                UnderAttackAlertLogic.ShouldTrigger(last, now),
                $"Спустя менее {Throttle}s алерт должен быть заблокирован.");
        }

        [Test]
        public void ShouldTrigger_SameTime_ReturnsFalse()
        {
            // last == now → разница 0, меньше ThrottleWindow → дросселируем.
            float last = 7f;
            Assert.IsFalse(
                UnderAttackAlertLogic.ShouldTrigger(last, last),
                "При lastTriggerTime == now алерт должен быть заблокирован.");
        }

        [Test]
        public void ShouldTrigger_NegativeLastTime_ReturnsTrue()
        {
            // lastTriggerTime может быть любым отрицательным числом (не только -∞).
            float last = -100f;
            float now  = 0f;
            Assert.IsTrue(
                UnderAttackAlertLogic.ShouldTrigger(last, now),
                "При очень старом lastTriggerTime алерт должен разрешаться.");
        }

        // ----------------------------------------------------------------
        // ShouldClear — авто-очистка
        // ----------------------------------------------------------------

        [Test]
        public void ShouldClear_AfterFullClearWindow_ReturnsTrue()
        {
            float last = 20f;
            float now  = last + Clear;
            Assert.IsTrue(
                UnderAttackAlertLogic.ShouldClear(last, now),
                $"Спустя ровно {Clear}s алерт должен быть снят.");
        }

        [Test]
        public void ShouldClear_AfterMoreThanClearWindow_ReturnsTrue()
        {
            float last = 20f;
            float now  = last + Clear + 5f;
            Assert.IsTrue(
                UnderAttackAlertLogic.ShouldClear(last, now),
                "Спустя больше ClearAfterSeconds алерт должен быть снят.");
        }

        [Test]
        public void ShouldClear_TooSoon_ReturnsFalse()
        {
            float last = 20f;
            float now  = last + Clear - 0.01f;
            Assert.IsFalse(
                UnderAttackAlertLogic.ShouldClear(last, now),
                $"До истечения {Clear}s алерт не должен сниматься.");
        }

        [Test]
        public void ShouldClear_Immediately_ReturnsFalse()
        {
            // Только что сработал алерт — до очистки ещё 3 секунды.
            float last = 50f;
            float now  = 50f;
            Assert.IsFalse(
                UnderAttackAlertLogic.ShouldClear(last, now),
                "Сразу после триггера алерт не должен сниматься.");
        }

        [Test]
        public void ShouldClear_NegativeInfinityLast_ReturnsTrue()
        {
            // Если алерт никогда не срабатывал — ShouldClear должен возвращать true,
            // чтобы Update не оставил alertActive=false неопределённым.
            Assert.IsTrue(
                UnderAttackAlertLogic.ShouldClear(float.NegativeInfinity, 0f),
                "При lastTriggerTime=-∞ ShouldClear должен возвращать true.");
        }

        // ----------------------------------------------------------------
        // Согласованность констант
        // ----------------------------------------------------------------

        [Test]
        public void Constants_ThrottleAndClear_AreEqual()
        {
            // По ТЗ: дросселирование и авто-очистка — одно окно 3 секунды.
            Assert.AreEqual(
                UnderAttackAlertLogic.ThrottleWindow,
                UnderAttackAlertLogic.ClearAfterSeconds,
                0.001f,
                "ThrottleWindow и ClearAfterSeconds должны совпадать (3s по ТЗ).");
        }

        [Test]
        public void Constants_ThrottleWindow_IsThreeSeconds()
        {
            Assert.AreEqual(3f, UnderAttackAlertLogic.ThrottleWindow, 0.001f,
                "ThrottleWindow должен быть 3 секунды согласно ТЗ.");
        }
    }
}
