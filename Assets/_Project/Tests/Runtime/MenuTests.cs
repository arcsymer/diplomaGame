using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.UI;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode smoke-тесты для M6b: PauseController, GameOverController, SettingsService.
    /// </summary>
    [TestFixture]
    public class MenuTests
    {
        // ----------------------------------------------------------------
        // TearDown — обязательное восстановление timeScale
        // ----------------------------------------------------------------

        [TearDown]
        public void TearDown()
        {
            // ВСЕГДА восстанавливаем timeScale, иначе остальные тесты зависнут
            Time.timeScale = 1f;

            // Уничтожаем тестовые объекты
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("MenuTest_"))
                    Object.Destroy(go);
            }
        }

        // ================================================================
        // PauseController
        // ================================================================

        [Test]
        public void PauseController_TogglePause_TimeScaleZero_PanelActive()
        {
            // Arrange
            var canvasGo = new GameObject("MenuTest_PauseCanvas");
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var panelGo = new GameObject("MenuTest_PausePanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            panelGo.AddComponent<RectTransform>();

            var pauseCtrl = canvasGo.AddComponent<PauseController>();

            // Проставляем ссылки через рефлексию (как SerializedObject в Editor)
            var pausePanelField = typeof(PauseController).GetField(
                "pausePanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            pausePanelField?.SetValue(pauseCtrl, panelGo);

            // Скрываем панель (Start ещё не вызван в тесте, так что ставим вручную)
            panelGo.SetActive(false);

            // Act
            pauseCtrl.TogglePause();

            // Assert
            Assert.AreEqual(0f,  Time.timeScale,   0.001f, "После паузы timeScale должен быть 0.");
            Assert.IsTrue(pauseCtrl.IsPaused,               "IsPaused должен быть true.");
            Assert.IsTrue(panelGo.activeSelf,               "Панель паузы должна быть активна.");
        }

        [Test]
        public void PauseController_TogglePauseTwice_TimeScaleOne_PanelInactive()
        {
            // Arrange
            var canvasGo = new GameObject("MenuTest_PauseCanvas2");
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var panelGo = new GameObject("MenuTest_PausePanel2");
            panelGo.transform.SetParent(canvasGo.transform, false);
            panelGo.AddComponent<RectTransform>();

            var pauseCtrl = canvasGo.AddComponent<PauseController>();

            var pausePanelField = typeof(PauseController).GetField(
                "pausePanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            pausePanelField?.SetValue(pauseCtrl, panelGo);
            panelGo.SetActive(false);

            // Act — пауза, потом снять
            pauseCtrl.TogglePause();
            pauseCtrl.TogglePause();

            // Assert
            Assert.AreEqual(1f,   Time.timeScale,  0.001f, "После снятия паузы timeScale должен быть 1.");
            Assert.IsFalse(pauseCtrl.IsPaused,              "IsPaused должен быть false.");
            Assert.IsFalse(panelGo.activeSelf,              "Панель паузы должна быть скрыта.");
        }

        // ================================================================
        // GameOverController
        // ================================================================

        [Test]
        public void GameOverController_ShowVictory_VictoryPanelActive_TimeScaleZero()
        {
            // Arrange
            var canvasGo = new GameObject("MenuTest_GameOverCanvas");
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var victoryGo = new GameObject("MenuTest_VictoryPanel");
            victoryGo.transform.SetParent(canvasGo.transform, false);
            victoryGo.AddComponent<RectTransform>();
            victoryGo.SetActive(false);

            var defeatGo = new GameObject("MenuTest_DefeatPanel");
            defeatGo.transform.SetParent(canvasGo.transform, false);
            defeatGo.AddComponent<RectTransform>();
            defeatGo.SetActive(false);

            var ctrl = canvasGo.AddComponent<GameOverController>();

            var victoryField = typeof(GameOverController).GetField(
                "victoryPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var defeatField = typeof(GameOverController).GetField(
                "defeatPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            victoryField?.SetValue(ctrl, victoryGo);
            defeatField?.SetValue(ctrl, defeatGo);

            // Act
            ctrl.ShowVictory();

            // Assert
            Assert.IsTrue(victoryGo.activeSelf,  "Панель победы должна быть активна.");
            Assert.IsFalse(defeatGo.activeSelf,  "Панель поражения должна быть скрыта.");
            Assert.AreEqual(0f, Time.timeScale, 0.001f, "timeScale должен быть 0 после ShowVictory.");
        }

        [Test]
        public void GameOverController_ShowDefeat_DefeatPanelActive_TimeScaleZero()
        {
            // Arrange
            var canvasGo = new GameObject("MenuTest_GameOverCanvas2");
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var victoryGo = new GameObject("MenuTest_VictoryPanel2");
            victoryGo.transform.SetParent(canvasGo.transform, false);
            victoryGo.AddComponent<RectTransform>();
            victoryGo.SetActive(false);

            var defeatGo = new GameObject("MenuTest_DefeatPanel2");
            defeatGo.transform.SetParent(canvasGo.transform, false);
            defeatGo.AddComponent<RectTransform>();
            defeatGo.SetActive(false);

            var ctrl = canvasGo.AddComponent<GameOverController>();

            var victoryField = typeof(GameOverController).GetField(
                "victoryPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var defeatField = typeof(GameOverController).GetField(
                "defeatPanel",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            victoryField?.SetValue(ctrl, victoryGo);
            defeatField?.SetValue(ctrl, defeatGo);

            // Act
            ctrl.ShowDefeat();

            // Assert
            Assert.IsTrue(defeatGo.activeSelf,   "Панель поражения должна быть активна.");
            Assert.IsFalse(victoryGo.activeSelf, "Панель победы должна быть скрыта.");
            Assert.AreEqual(0f, Time.timeScale, 0.001f, "timeScale должен быть 0 после ShowDefeat.");
        }

        // ================================================================
        // SettingsService
        // ================================================================

        [Test]
        public void SettingsService_SaveAndLoadMouseSensitivity_ReturnsCorrectValue()
        {
            // Arrange & Act
            SettingsService.SaveMouseSensitivity(0.3f);
            float loaded = SettingsService.LoadMouseSensitivity();

            // Assert
            Assert.AreEqual(0.3f, loaded, 0.0001f,
                "LoadMouseSensitivity должен вернуть сохранённое значение 0.3.");
        }

        [Test]
        public void SettingsService_SaveMouseSensitivity_ClampsBelowMin()
        {
            SettingsService.SaveMouseSensitivity(0f);
            float loaded = SettingsService.LoadMouseSensitivity();
            Assert.GreaterOrEqual(loaded, 0.01f,
                "Сохранённое значение должно быть зажато до минимума 0.01.");
        }

        [Test]
        public void SettingsService_SaveMouseSensitivity_ClampsAboveMax()
        {
            SettingsService.SaveMouseSensitivity(2f);
            float loaded = SettingsService.LoadMouseSensitivity();
            Assert.LessOrEqual(loaded, 1f,
                "Сохранённое значение должно быть зажато до максимума 1.");
        }

        // ----------------------------------------------------------------
        // TearDown чистит PlayerPrefs-ключ, чтобы не влиять на другие тесты
        // ----------------------------------------------------------------

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            UnityEngine.PlayerPrefs.DeleteKey("Settings.MouseSensitivity");
            UnityEngine.PlayerPrefs.Save();
        }
    }
}
