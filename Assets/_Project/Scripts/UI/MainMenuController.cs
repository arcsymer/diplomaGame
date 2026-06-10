using DiplomaGame.Runtime.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Контроллер главного меню.
    /// Кнопки: Play → загружает сцену Sandbox; Settings → показывает SettingsPanel;
    /// Quit → закрывает приложение.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private SettingsPanel settingsPanel;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Start()
        {
            // Убеждаемся, что панель настроек скрыта при старте
            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(false);

            // Применяем сохранённые настройки при заходе в главное меню
            SettingsService.ApplyAll();

            // Подписываемся на закрытие панели настроек
            if (settingsPanel != null)
                settingsPanel.Closed += OnSettingsClosed;
        }

        private void OnDestroy()
        {
            if (settingsPanel != null)
                settingsPanel.Closed -= OnSettingsClosed;
        }

        // ----------------------------------------------------------------
        // Кнопки UI (вызываются через UnityEvent из Inspector)
        // ----------------------------------------------------------------

        /// <summary>Кнопка «Играть».</summary>
        public void OnPlayClicked()
        {
            SceneManager.LoadScene("Sandbox");
        }

        /// <summary>Кнопка «Настройки».</summary>
        public void OnSettingsClicked()
        {
            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(true);
        }

        /// <summary>Кнопка «Выйти».</summary>
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

        private void OnSettingsClosed()
        {
            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(false);
        }
    }
}
