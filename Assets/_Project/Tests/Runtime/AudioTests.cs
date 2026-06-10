using System.Collections;
using DiplomaGame.Runtime.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode smoke-тесты для M7: AudioManager.
    /// Тестируем только observable-поведение без AudioSource (нет аудиодрайвера в CI).
    /// </summary>
    [TestFixture]
    public class AudioTests
    {
        private GameObject _managerGo;
        private AudioManager _manager;

        // ----------------------------------------------------------------
        // SetUp / TearDown
        // ----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _managerGo = new GameObject("AudioTest_AudioManager");
            _manager   = _managerGo.AddComponent<AudioManager>();

            // Фейковые клипы: без них TryPlayUnitAck честно отвечает false
            // (пустой массив = нечего играть), а нам нужен rate-limit
            var fake = AudioClip.Create("fake", 441, 1, 44100, false);
            _manager.InitForTest(fake, fake, fake, new[] { fake, fake });
        }

        [TearDown]
        public void TearDown()
        {
            if (_managerGo != null)
                Object.Destroy(_managerGo);
        }

        // ================================================================
        // SetMusicState — проверка идемпотентности
        // ================================================================

        [Test]
        public void SetMusicState_Combat_SetsState()
        {
            _manager.SetMusicState(MusicState.Combat);
            Assert.AreEqual(MusicState.Combat, _manager.CurrentMusicState,
                "После SetMusicState(Combat) CurrentMusicState должен быть Combat.");
        }

        [Test]
        public void SetMusicState_Ambient_SetsState()
        {
            _manager.SetMusicState(MusicState.Ambient);
            Assert.AreEqual(MusicState.Ambient, _manager.CurrentMusicState,
                "После SetMusicState(Ambient) CurrentMusicState должен быть Ambient.");
        }

        [Test]
        public void SetMusicState_Menu_SetsState()
        {
            _manager.SetMusicState(MusicState.Menu);
            Assert.AreEqual(MusicState.Menu, _manager.CurrentMusicState,
                "После SetMusicState(Menu) CurrentMusicState должен быть Menu.");
        }

        [Test]
        public void SetMusicState_SameStateTwice_Idempotent()
        {
            _manager.SetMusicState(MusicState.Combat);
            _manager.SetMusicState(MusicState.Combat);
            Assert.AreEqual(MusicState.Combat, _manager.CurrentMusicState,
                "Повторный вызов с тем же состоянием не должен менять CurrentMusicState.");
        }

        // ================================================================
        // Rate-limit для голосовых подтверждений
        // ================================================================

        [Test]
        public void TryPlayUnitAck_FirstCall_ReturnsTrue()
        {
            // Устанавливаем LastAckTime в прошлое, чтобы rate-limit не блокировал
            _manager.LastAckTime = -999f;

            bool result = _manager.TryPlayUnitAck();
            Assert.IsTrue(result,
                "Первый вызов TryPlayUnitAck должен вернуть true (нет rate-limit).");
        }

        [Test]
        public void TryPlayUnitAck_SecondCallImmediately_ReturnsFalse()
        {
            // Симулируем недавний вызов: LastAckTime = текущее время
            _manager.LastAckTime = Time.time;

            bool result = _manager.TryPlayUnitAck();
            Assert.IsFalse(result,
                "Вызов TryPlayUnitAck сразу после предыдущего должен вернуть false (rate-limit).");
        }

        [Test]
        public void TryPlayUnitAck_AfterCooldown_ReturnsTrue()
        {
            // Симулируем, что последний ack был больше 1.5 сек назад
            _manager.LastAckTime = Time.time - (AudioManager.AckRateLimit + 0.1f);

            bool result = _manager.TryPlayUnitAck();
            Assert.IsTrue(result,
                "После истечения кулдауна TryPlayUnitAck должен вернуть true.");
        }

        [UnityTest]
        public IEnumerator TryPlayUnitAck_WaitForCooldown_ReturnsTrue()
        {
            // Первый вызов
            _manager.LastAckTime = Time.time;
            bool immediate = _manager.TryPlayUnitAck();
            Assert.IsFalse(immediate, "Сразу — false.");

            // Ждём истечения кулдауна
            yield return new WaitForSeconds(AudioManager.AckRateLimit + 0.1f);

            bool afterWait = _manager.TryPlayUnitAck();
            Assert.IsTrue(afterWait,
                $"Через {AudioManager.AckRateLimit + 0.1f}s TryPlayUnitAck должен вернуть true.");
        }

        // ================================================================
        // SetCategoryVolume — проверка fallback-режима (без mixer)
        // ================================================================

        [Test]
        public void SetCategoryVolume_Music_NoMixer_DoesNotThrow()
        {
            // Без миксера — fallback через поля
            Assert.DoesNotThrow(() =>
                _manager.SetCategoryVolume(AudioManager.VolumeCategory.Music, 0.5f),
                "SetCategoryVolume не должен бросать без AudioMixer.");
        }

        [Test]
        public void SetCategoryVolume_Sfx_NoMixer_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                _manager.SetCategoryVolume(AudioManager.VolumeCategory.Sfx, 0.7f));
        }

        [Test]
        public void SetCategoryVolume_Ui_NoMixer_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                _manager.SetCategoryVolume(AudioManager.VolumeCategory.Ui, 0.3f));
        }

        [Test]
        public void SetCategoryVolume_Voice_NoMixer_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                _manager.SetCategoryVolume(AudioManager.VolumeCategory.Voice, 0.8f));
        }

        [Test]
        public void SetCategoryVolume_Master_NoMixer_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                _manager.SetCategoryVolume(AudioManager.VolumeCategory.Master, 1.0f));
        }
    }
}
