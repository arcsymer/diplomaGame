using NUnit.Framework;
using DiplomaGame.Runtime.Core;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для ModeSwitchLogic.
    /// Не требуют сцены — вся логика статическая.
    /// Запуск: Window → General → Test Runner → EditMode.
    /// </summary>
    [TestFixture]
    public class ModeSwitchLogicTests
    {
        // ----------------------------------------------------------------
        // Toggle
        // ----------------------------------------------------------------

        [Test]
        public void Toggle_Rts_ReturnsTps()
        {
            var result = ModeSwitchLogic.Toggle(GameMode.Rts);
            Assert.AreEqual(GameMode.Tps, result,
                "Toggle(Rts) должен вернуть Tps.");
        }

        [Test]
        public void Toggle_Tps_ReturnsRts()
        {
            var result = ModeSwitchLogic.Toggle(GameMode.Tps);
            Assert.AreEqual(GameMode.Rts, result,
                "Toggle(Tps) должен вернуть Rts.");
        }

        [Test]
        public void Toggle_DoubleToggle_ReturnsOriginal()
        {
            var result = ModeSwitchLogic.Toggle(ModeSwitchLogic.Toggle(GameMode.Rts));
            Assert.AreEqual(GameMode.Rts, result,
                "Двойной Toggle должен вернуть исходный режим.");
        }

        // ----------------------------------------------------------------
        // GetPriorities
        // ----------------------------------------------------------------

        [Test]
        public void GetPriorities_RtsMode_RtsIsActive()
        {
            var (rtsPriority, tpsPriority) = ModeSwitchLogic.GetPriorities(GameMode.Rts);

            Assert.Greater(rtsPriority, tpsPriority,
                "В режиме Rts приоритет RTS-камеры должен быть выше TPS.");
            Assert.AreEqual(20, rtsPriority,
                "Активная камера должна иметь приоритет 20.");
            Assert.AreEqual(10, tpsPriority,
                "Неактивная камера должна иметь приоритет 10.");
        }

        [Test]
        public void GetPriorities_TpsMode_TpsIsActive()
        {
            var (rtsPriority, tpsPriority) = ModeSwitchLogic.GetPriorities(GameMode.Tps);

            Assert.Greater(tpsPriority, rtsPriority,
                "В режиме Tps приоритет TPS-камеры должен быть выше RTS.");
            Assert.AreEqual(20, tpsPriority,
                "Активная камера должна иметь приоритет 20.");
            Assert.AreEqual(10, rtsPriority,
                "Неактивная камера должна иметь приоритет 10.");
        }

        [Test]
        public void GetPriorities_PrioritiesAreDifferent()
        {
            var (r1, t1) = ModeSwitchLogic.GetPriorities(GameMode.Rts);
            var (r2, t2) = ModeSwitchLogic.GetPriorities(GameMode.Tps);

            Assert.AreNotEqual(r1, t1, "Приоритеты в Rts-режиме должны различаться.");
            Assert.AreNotEqual(r2, t2, "Приоритеты в Tps-режиме должны различаться.");
        }
    }
}
