using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using DiplomaGame.Runtime.Core;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode-тесты для GameModeController.
    /// Используют internal-метод InitForTest (InternalsVisibleTo в AssemblyInfo.cs).
    /// Тесты синхронные — UnityTest-корутина не нужна, так как вся логика
    /// выполняется в одном кадре без yield.
    /// Запуск: Window → General → Test Runner → PlayMode.
    /// </summary>
    [TestFixture]
    public class GameModeControllerTests
    {
        private GameObject       _controllerGo;
        private GameObject       _rtsCamGo;
        private GameObject       _tpsCamGo;
        private GameModeController _controller;
        private CinemachineCamera  _rtsCam;
        private CinemachineCamera  _tpsCam;

        [SetUp]
        public void SetUp()
        {
            _rtsCamGo = new GameObject("TestRtsCam");
            _tpsCamGo = new GameObject("TestTpsCam");
            _rtsCam   = _rtsCamGo.AddComponent<CinemachineCamera>();
            _tpsCam   = _tpsCamGo.AddComponent<CinemachineCamera>();

            _controllerGo = new GameObject("TestGameManagers");
            _controller   = _controllerGo.AddComponent<GameModeController>();

            // Инициализируем без InputActionAsset — безопасно для теста
            _controller.InitForTest(_rtsCam, _tpsCam);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_controllerGo);
            Object.Destroy(_rtsCamGo);
            Object.Destroy(_tpsCamGo);
        }

        // ----------------------------------------------------------------
        // SetMode(Tps)
        // ----------------------------------------------------------------

        [Test]
        public void SetMode_Tps_TpsCameraHasHigherPriority()
        {
            _controller.SetMode(GameMode.Tps);

            // Priority в Cinemachine 3.x — структура PrioritySettings; сравниваем через Value
            Assert.AreEqual(20, _tpsCam.Priority.Value,
                "После SetMode(Tps) TPS-камера должна иметь приоритет 20.");
            Assert.AreEqual(10, _rtsCam.Priority.Value,
                "После SetMode(Tps) RTS-камера должна иметь приоритет 10.");
        }

        [Test]
        public void SetMode_Rts_RtsCameraHasHigherPriority()
        {
            // Сначала переключаем в Tps, потом обратно в Rts
            _controller.SetMode(GameMode.Tps);
            _controller.SetMode(GameMode.Rts);

            Assert.AreEqual(20, _rtsCam.Priority.Value,
                "После SetMode(Rts) RTS-камера должна иметь приоритет 20.");
            Assert.AreEqual(10, _tpsCam.Priority.Value,
                "После SetMode(Rts) TPS-камера должна иметь приоритет 10.");
        }

        // ----------------------------------------------------------------
        // ModeChanged событие
        // ----------------------------------------------------------------

        [Test]
        public void SetMode_Tps_EventFired()
        {
            GameMode? receivedMode = null;
            _controller.ModeChanged += m => receivedMode = m;

            _controller.SetMode(GameMode.Tps);

            Assert.IsTrue(receivedMode.HasValue,
                "ModeChanged должно сработать при SetMode(Tps).");
            Assert.AreEqual(GameMode.Tps, receivedMode.Value,
                "ModeChanged должно передать GameMode.Tps.");
        }

        [Test]
        public void SetMode_SameMode_EventNotFiredTwice()
        {
            // Первый вызов — инициализирует (ModeChanged == null → проходит).
            _controller.SetMode(GameMode.Tps);

            int callCount = 0;
            _controller.ModeChanged += _ => callCount++;

            // Повторный вызов с тем же режимом должен быть проигнорирован.
            _controller.SetMode(GameMode.Tps);

            Assert.AreEqual(0, callCount,
                "Повторный SetMode с тем же режимом не должен вызывать ModeChanged.");
        }

        // ----------------------------------------------------------------
        // CurrentMode
        // ----------------------------------------------------------------

        [Test]
        public void SetMode_Tps_CurrentModeIsTps()
        {
            _controller.SetMode(GameMode.Tps);

            Assert.AreEqual(GameMode.Tps, _controller.CurrentMode,
                "CurrentMode должен отражать последний установленный режим.");
        }

        [Test]
        public void SetMode_Rts_CurrentModeIsRts()
        {
            _controller.SetMode(GameMode.Tps);
            _controller.SetMode(GameMode.Rts);

            Assert.AreEqual(GameMode.Rts, _controller.CurrentMode,
                "CurrentMode должен отражать последний установленный режим.");
        }

        // ----------------------------------------------------------------
        // SwitchMode (Toggle)
        // ----------------------------------------------------------------

        [Test]
        public void SwitchMode_FromRts_SwitchesToTps()
        {
            _controller.SetMode(GameMode.Rts);
            _controller.SwitchMode();

            Assert.AreEqual(GameMode.Tps, _controller.CurrentMode,
                "SwitchMode из Rts должен переключить в Tps.");
        }

        [Test]
        public void SwitchMode_FromTps_SwitchesToRts()
        {
            _controller.SetMode(GameMode.Tps);
            _controller.SwitchMode();

            Assert.AreEqual(GameMode.Rts, _controller.CurrentMode,
                "SwitchMode из Tps должен переключить в Rts.");
        }
    }
}
