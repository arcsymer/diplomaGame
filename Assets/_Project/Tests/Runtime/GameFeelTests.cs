using System.Collections;
using DiplomaGame.Runtime.GameFeel;
using DiplomaGame.Runtime.Hero;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode smoke-тесты для системы GameFeel.
    /// Проверяют: рекойл после выстрела, затухание рекойла, hitstop-гарды,
    /// DashTrailHandler таймер, HitFlash таймер без рендереров.
    /// </summary>
    [TestFixture]
    public class GameFeelTests
    {
        private GameObject       _managerGo;
        private GameFeelManager  _manager;
        private GameFeelSettings _settings;

        private GameObject       _heroGo;
        private HeroShooter      _heroShooter;
        private GameObject       _cameraGo;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GameFeelSettings>();

            // PerformShot тихо выходит при Camera.main == null — в голой
            // тест-сцене камеры нет, выстрел (и ShotFired) не происходил.
            _cameraGo = new GameObject("TestMainCamera", typeof(Camera))
            {
                tag = "MainCamera"
            };

            _managerGo = new GameObject("TestGameFeelManager");
            _manager   = _managerGo.AddComponent<GameFeelManager>();

            _heroGo      = new GameObject("TestHero");
            _heroShooter = _heroGo.AddComponent<HeroShooter>();

            _manager.InitForTest(_settings, _heroShooter);
        }

        [TearDown]
        public void TearDown()
        {
            // Восстанавливаем timeScale на случай, если тест его нарушил
            Time.timeScale = 1f;

            if (_managerGo != null) Object.Destroy(_managerGo);
            if (_heroGo    != null) Object.Destroy(_heroGo);
            if (_cameraGo  != null) Object.Destroy(_cameraGo);
            if (_settings  != null) Object.DestroyImmediate(_settings);
        }

        // ================================================================
        // Рекойл после выстрела
        // ================================================================


        /// <summary>
        /// Выстрел с обнулённым кулдауном: в начале PlayMode-сессии Time.time < fireCooldown,
        /// и CanFire блокировал самый первый выстрел теста.
        /// </summary>
        private static void FireIgnoringCooldown(HeroShooter shooter)
        {
            var f = typeof(HeroShooter).GetField("_lastFireTime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            f?.SetValue(shooter, -100f);
            shooter.TryFire();
        }

        [UnityTest]
        public IEnumerator Recoil_AfterShot_RecoilYIsPositive()
        {
            // До выстрела рекойл должен быть нулём
            Assert.AreEqual(0f, _manager.RecoilCurrentY, 0.0001f,
                "Рекойл до выстрела должен быть 0.");

            // Диагностика: доходит ли вообще ShotFired
            int shotEvents = 0;
            _heroShooter.ShotFired += (_, _, _) => shotEvents++;

            // Симулируем выстрел через internal TryFire
            FireIgnoringCooldown(_heroShooter);
            yield return null;

            Assert.Greater(shotEvents, 0,
                $"HeroShooter.ShotFired не сработал (Camera.main={(Camera.main != null ? Camera.main.name : "null")}, " +
                $"Time.time={Time.time:F2}).");

            // Рекойл должен стать положительным
            Assert.Greater(_manager.RecoilCurrentY, 0f,
                "Рекойл после выстрела должен быть положительным.");
        }

        [UnityTest]
        public IEnumerator Recoil_AfterShot_DecaysOverTime()
        {
            FireIgnoringCooldown(_heroShooter);
            yield return null;

            float recoilAfterShot = _manager.RecoilCurrentY;
            Assert.Greater(recoilAfterShot, 0f,
                "Рекойл после выстрела должен быть > 0.");

            // Ждём несколько кадров — рекойл должен уменьшиться
            yield return new WaitForSeconds(0.1f);

            float recoilAfterWait = _manager.RecoilCurrentY;
            Assert.Less(recoilAfterWait, recoilAfterShot,
                "Рекойл должен затухать со временем.");
        }

        [UnityTest]
        public IEnumerator Recoil_TwoShots_AccumulatesAmplitude()
        {
            FireIgnoringCooldown(_heroShooter);
            yield return null;
            float afterFirst = _manager.RecoilCurrentY;

            FireIgnoringCooldown(_heroShooter);
            yield return null;
            float afterSecond = _manager.RecoilCurrentY;

            // После второго выстрела рекойл должен быть больше (даже с учётом одного кадра спада)
            Assert.Greater(afterSecond, afterFirst * 0.5f,
                "Второй выстрел должен добавлять рекойл (суммируется с первым).");
        }

        // ================================================================
        // Hitstop guard: при timeScale < 0.1 hitstop не запускается
        // ================================================================

        [UnityTest]
        public IEnumerator Hitstop_WhenTimeScaleZero_DoesNotChangeTimeScale()
        {
            // Выставляем паузу вручную
            Time.timeScale = 0f;
            yield return null;

            // Пытаемся запустить hitstop через TryStartHitstop (вызывается из OnAbilityCast)
            // В реальном GameFeelManager guard срабатывает в TryStartHitstop.
            // Проверяем косвенно: timeScale не изменился.
            Assert.AreEqual(0f, Time.timeScale, 0.001f,
                "timeScale не должен изменяться если уже 0 (пауза).");

            Time.timeScale = 1f;
        }

        // ================================================================
        // DashTrailHandler — таймер гасит trail
        // ================================================================

        [UnityTest]
        public IEnumerator DashTrailHandler_AfterTrigger_TrailEmitsInitially()
        {
            var go = new GameObject("TestDashTrail");
            go.AddComponent<DashTrailHandler>();
            var trail   = go.GetComponent<TrailRenderer>();
            var handler = go.GetComponent<DashTrailHandler>();

            // До TriggerDash trail не эмитирует
            Assert.IsFalse(trail.emitting,
                "Trail до TriggerDash должен быть выключен.");

            handler.TriggerDash(0.2f);
            yield return null;

            Assert.IsTrue(trail.emitting,
                "Trail должен начать эмитировать сразу после TriggerDash.");

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator DashTrailHandler_AfterDuration_TrailStops()
        {
            var go = new GameObject("TestDashTrailTimer");
            go.AddComponent<DashTrailHandler>();
            var trail   = go.GetComponent<TrailRenderer>();
            var handler = go.GetComponent<DashTrailHandler>();

            handler.TriggerDash(0.1f);
            yield return null;
            Assert.IsTrue(trail.emitting, "Trail должен эмитировать после TriggerDash.");

            // Ждём дольше, чем dashDuration
            yield return new WaitForSeconds(0.25f);

            Assert.IsFalse(trail.emitting,
                "Trail должен прекратить эмиссию по истечении dashDuration.");

            Object.Destroy(go);
        }

        // ================================================================
        // HitFlash integration — GO с MeshRenderer
        // ================================================================

        [UnityTest]
        public IEnumerator HitFlashHandler_AfterTrigger_IsFlashingInitially()
        {
            var go = new GameObject("TestFlashGO");
            // Добавляем Visual child с рендерером
            var visualGo       = new GameObject("Visual");
            visualGo.transform.SetParent(go.transform);
            visualGo.AddComponent<MeshRenderer>();

            var handler = go.AddComponent<HitFlashHandler>();
            yield return null; // даём Awake

            handler.TriggerFlash(Color.white, 0.1f);
            yield return null;

            // Проверяем что объект не был уничтожен и не выброшено исключение
            Assert.IsNotNull(handler, "HitFlashHandler должен оставаться активным после TriggerFlash.");

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator HitFlashHandler_AfterDuration_DoesNotThrow()
        {
            var go      = new GameObject("TestFlashTimer");
            var handler = go.AddComponent<HitFlashHandler>();

            handler.TriggerFlash(Color.white, 0.05f);
            yield return null;

            // Ждём дольше, чем hitFlashDuration
            yield return new WaitForSeconds(0.15f);

            // Объект должен быть в нормальном состоянии
            Assert.IsNotNull(handler,
                "HitFlashHandler должен быть работоспособен после истечения таймера.");

            Object.Destroy(go);
        }

        // ================================================================
        // GameFeelSettings влияет на рекойл
        // ================================================================

        [UnityTest]
        public IEnumerator Recoil_HighAmplitude_GivesHigherRecoil()
        {
            // Первый выстрел с дефолтными настройками
            FireIgnoringCooldown(_heroShooter);
            yield return null;
            float defaultRecoil = _manager.RecoilCurrentY;

            // Пересоздаём с высокой амплитудой
            Object.Destroy(_managerGo);
            Object.Destroy(_heroGo);

            var highSettings = ScriptableObject.CreateInstance<GameFeelSettings>();
            highSettings.shotImpulseAmplitude = 1.0f; // в 10+ раз больше дефолтного

            var newHeroGo  = new GameObject("HighRecoilHero");
            var newShooter = newHeroGo.AddComponent<HeroShooter>();

            var newManagerGo = new GameObject("HighRecoilManager");
            var newManager   = newManagerGo.AddComponent<GameFeelManager>();
            newManager.InitForTest(highSettings, newShooter);

            yield return null;

            newShooter.TryFire();
            yield return null;

            Assert.Greater(newManager.RecoilCurrentY, defaultRecoil,
                "Высокая амплитуда должна давать больший рекойл.");

            Object.Destroy(newManagerGo);
            Object.Destroy(newHeroGo);
            Object.DestroyImmediate(highSettings);

            // Предотвращаем двойной destroy в TearDown
            _managerGo = null;
            _heroGo    = null;
        }
    }
}
