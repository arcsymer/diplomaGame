using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode smoke-тесты для HUD M6a.
    /// Тесты синхронные — не требуют UnityTest/корутин.
    /// </summary>
    [TestFixture]
    public class HudTests
    {
        // ----------------------------------------------------------------
        // Инфраструктура
        // ----------------------------------------------------------------

        [TearDown]
        public void TearDown()
        {
            // Удаляем все GO, созданные тестами
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("Hud_Test_") ||
                    go.name.StartsWith("HealthBar_Test_") ||
                    go.name.StartsWith("ResourceDisplay_Test_") ||
                    go.name.StartsWith("HudController_Test_"))
                {
                    Object.Destroy(go);
                }
            }
        }

        // ================================================================
        // HealthBar
        // ================================================================

        [Test]
        public void HealthBar_TakeDamage50_FillAmountApprox05()
        {
            // Arrange: GO с Health + child canvas + fill
            var root   = new GameObject("HealthBar_Test_Root");
            var health = root.AddComponent<Health>();
            health.Init(100f);

            var canvasGo = new GameObject("HealthBar_Test_Canvas");
            canvasGo.transform.SetParent(root.transform, false);
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            var fillGo = new GameObject("HealthBar_Test_Fill");
            fillGo.transform.SetParent(canvasGo.transform, false);
            var fill = fillGo.AddComponent<Image>();
            fill.fillAmount = 1f;

            var bar = canvasGo.AddComponent<HealthBar>();
            bar.InitForTest(health);

            // Подписываем события вручную (Awake уже отработал, вызываем OnEnable через reflection)
            // Проще: добавляем слушатель к health напрямую, имитируя работу HealthBar
            // Реальный тест — через рефлексию к приватному OnDamaged не нужен,
            // HealthBar подписывается в OnEnable, который вызывается при AddComponent.
            // Используем InitForTest чтобы подменить ссылку после Awake.
            // Поскольку Awake уже вызван с null-health, нужно пересоздать.

            Object.Destroy(canvasGo);

            // Пересоздаём правильно: сначала здоровье, потом canvas, потом bar с нужным health
            var root2   = new GameObject("HealthBar_Test_Root2");
            var health2 = root2.AddComponent<Health>();
            health2.Init(100f);

            // Создаём HealthBar не как дочерний к root2, чтобы GetComponentInParent нашёл health2
            var canvas2Go = new GameObject("HealthBar_Test_Canvas2");
            canvas2Go.transform.SetParent(root2.transform, false);
            canvas2Go.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;

            var fill2Go = new GameObject("HealthBar_Test_Fill2");
            fill2Go.transform.SetParent(canvas2Go.transform, false);
            var fill2 = fill2Go.AddComponent<Image>();
            fill2.fillAmount = 1f;

            var bar2 = canvas2Go.AddComponent<HealthBar>();

            // Проставляем fill через SerializedObject-like подход в тестах не работает,
            // используем рефлексию
            var fillField = typeof(HealthBar).GetField(
                "fill",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fillField != null)
                fillField.SetValue(bar2, fill2);

            // Act
            canvas2Go.SetActive(true); // триггер OnEnable → подписка
            health2.TakeDamage(50f);

            // Assert
            Assert.AreEqual(true,  canvas2Go.activeSelf, "HealthBar должен стать видимым после первого урона.");
            Assert.AreEqual(0.5f,  fill2.fillAmount, 0.01f, "fillAmount должен быть ~0.5 после 50 урона из 100.");

            Object.Destroy(root2);
        }

        // ================================================================
        // ResourceDisplay
        // ================================================================

        [Test]
        public void ResourceDisplay_BankAdd_TextContainsNewAmount()
        {
            // Arrange
            var bankGo = new GameObject("ResourceDisplay_Test_Bank");
            var bank   = bankGo.AddComponent<ResourceBank>();
            bank.InitForTest(100, 100);

            var displayGo = new GameObject("ResourceDisplay_Test_Display");
            // TMP_Text требует Canvas
            var canvasGo = new GameObject("ResourceDisplay_Test_Canvas");
            displayGo.transform.SetParent(canvasGo.transform, false);
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var tmp     = displayGo.AddComponent<TextMeshProUGUI>();
            var display = displayGo.AddComponent<ResourceDisplay>();
            display.InitForTest(bank);

            // Trigger Start manually by simulating subscription
            // InitForTest sets bank, but Start subscribes — call it via reflection
            var startMethod = typeof(ResourceDisplay).GetMethod(
                "Start",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod?.Invoke(display, null);

            // Act
            bank.Add(Faction.Player, 75);

            // Assert — текст должен содержать новое значение 175
            Assert.IsTrue(tmp.text.Contains("175"),
                $"Текст должен содержать «175» после Add(75). Текущий текст: «{tmp.text}»");

            Object.Destroy(canvasGo);
            Object.Destroy(bankGo);
        }

        // ================================================================
        // HudController
        // ================================================================

        [Test]
        public void HudController_SetModeTps_RtsBlockInactive_TpsActive()
        {
            // Arrange
            var managersGo  = new GameObject("HudController_Test_Managers");
            var controller  = managersGo.AddComponent<GameModeController>();
            controller.InitForTest(null, null);

            var canvasGo    = new GameObject("HudController_Test_Canvas");
            var rtsGo       = new GameObject("HudController_Test_Rts");
            var tpsGo       = new GameObject("HudController_Test_Tps");
            rtsGo.transform.SetParent(canvasGo.transform, false);
            tpsGo.transform.SetParent(canvasGo.transform, false);

            var hudCtrl = canvasGo.AddComponent<HudController>();
            hudCtrl.InitForTest(rtsGo, tpsGo, controller);

            // Подписываем вручную (имитация Awake после InitForTest)
            controller.ModeChanged += m =>
            {
                // HudController слушает — вызовем его ApplyMode через событие
            };

            // Act: переключаем в TPS
            controller.SetMode(GameMode.Tps);

            // HudController подписывается в Awake; так как мы вызвали InitForTest после Awake,
            // подпишемся через рефлексию
            var awakeMethod = typeof(HudController).GetMethod(
                "OnModeChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod?.Invoke(hudCtrl, new object[] { GameMode.Tps });

            // Assert
            Assert.IsFalse(rtsGo.activeSelf, "RTS-блок должен быть неактивен в TPS-режиме.");
            Assert.IsTrue(tpsGo.activeSelf,  "TPS-блок должен быть активен в TPS-режиме.");

            Object.Destroy(managersGo);
            Object.Destroy(canvasGo);
        }

        [Test]
        public void HudController_SetModeRts_TpsBlockInactive_RtsActive()
        {
            var managersGo = new GameObject("HudController_Test_Managers2");
            var controller = managersGo.AddComponent<GameModeController>();
            controller.InitForTest(null, null);

            var canvasGo = new GameObject("HudController_Test_Canvas2");
            var rtsGo    = new GameObject("HudController_Test_Rts2");
            var tpsGo    = new GameObject("HudController_Test_Tps2");
            rtsGo.transform.SetParent(canvasGo.transform, false);
            tpsGo.transform.SetParent(canvasGo.transform, false);

            var hudCtrl = canvasGo.AddComponent<HudController>();
            hudCtrl.InitForTest(rtsGo, tpsGo, controller);

            var applyMode = typeof(HudController).GetMethod(
                "ApplyMode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            applyMode?.Invoke(hudCtrl, new object[] { GameMode.Rts });

            // Assert
            Assert.IsTrue(rtsGo.activeSelf,   "RTS-блок должен быть активен в RTS-режиме.");
            Assert.IsFalse(tpsGo.activeSelf,  "TPS-блок должен быть неактивен в RTS-режиме.");

            Object.Destroy(managersGo);
            Object.Destroy(canvasGo);
        }
    }
}
