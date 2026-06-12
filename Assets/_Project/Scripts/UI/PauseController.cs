using System;
using DiplomaGame.Runtime.Buildings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Управляет паузой в игровой сцене.
    /// Escape → toggle пауза, но если BuildingPlacer.IsPlacing — Escape
    /// обрабатывается плейсером в том же кадре, пауза не открывается.
    /// При паузе: Time.timeScale = 0, панель видима.
    /// При продолжении: Time.timeScale = 1, панель скрыта.
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] private GameObject   pausePanel;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private BuildingPlacer buildingPlacer;

        // ----------------------------------------------------------------
        // Событие (GameFeel — инвок ДО установки timeScale)
        // ----------------------------------------------------------------

        /// <summary>
        /// Вызывается при изменении состояния паузы до установки Time.timeScale.
        /// Параметр true → переходим в паузу; false → возобновляем.
        /// </summary>
        public event Action<bool> PauseChanged;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>Возвращает true, если игра сейчас на паузе.</summary>
        public bool IsPaused { get; private set; }

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Start()
        {
            SetPanelVisible(false);

            // Подписываемся на кнопку «Назад» панели настроек
            if (settingsPanel != null)
                settingsPanel.Closed += OnSettingsClosed;
        }

        private void OnDestroy()
        {
            if (settingsPanel != null)
                settingsPanel.Closed -= OnSettingsClosed;
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

            // Если плейсер активен — Escape обрабатывает отмену размещения,
            // паузу в том же кадре не открываем.
            if (buildingPlacer != null && buildingPlacer.IsPlacing) return;

            // Если открыта панель настроек — Escape закрывает её, а не всю паузу
            if (settingsPanel != null && settingsPanel.gameObject.activeSelf)
            {
                settingsPanel.gameObject.SetActive(false);
                return;
            }

            TogglePause();
        }

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов
        // ----------------------------------------------------------------

        /// <summary>Переключает паузу — используется в PlayMode-тестах.</summary>
        internal void TogglePause()
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }

        // ----------------------------------------------------------------
        // Кнопки UI (вызываются через UnityEvent из Inspector)
        // ----------------------------------------------------------------

        /// <summary>Кнопка «Продолжить».</summary>
        public void OnContinueClicked()
        {
            Resume();
        }

        /// <summary>Кнопка «Настройки» — открывает SettingsPanel поверх паузы.</summary>
        public void OnSettingsClicked()
        {
            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(true);
        }

        /// <summary>Кнопка «Выйти в меню».</summary>
        public void OnExitToMenuClicked()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            SceneManager.LoadScene("MainMenu");
        }

        /// <summary>Кнопка «Выйти из игры».</summary>
        public void OnQuitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void Pause()
        {
            IsPaused = true;

            // Инвок ДО установки timeScale — GameFeelManager успевает прервать hitstop
            PauseChanged?.Invoke(true);

            Time.timeScale = 0f;
            SetPanelVisible(true);

            // Скрываем SettingsPanel при открытии паузы (может быть видима с прошлого раза)
            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(false);
        }

        private void Resume()
        {
            IsPaused = false;

            // Инвок ДО установки timeScale
            PauseChanged?.Invoke(false);

            Time.timeScale = 1f;
            SetPanelVisible(false);
        }

        private void SetPanelVisible(bool visible)
        {
            if (pausePanel != null)
                pausePanel.SetActive(visible);
        }

        private void OnSettingsClosed()
        {
            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(false);
        }
    }
}
