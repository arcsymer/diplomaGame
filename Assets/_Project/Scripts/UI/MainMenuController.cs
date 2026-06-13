using System.Collections.Generic;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Core.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Контроллер главного меню.
    /// Кнопки: Play → загружает сцену Sandbox; Settings → показывает SettingsPanel;
    /// Quit → закрывает приложение.
    /// Dropdown сложности: опции заполняются через LocService (ru/en),
    /// значение читается из SettingsService.LoadDifficulty() и сохраняется при изменении.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private SettingsPanel  settingsPanel;
        [SerializeField] private TMP_Dropdown   difficultyDropdown;

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

            // Заполняем и инициализируем dropdown сложности
            PopulateDifficultyDropdown();
        }

        private void OnEnable()
        {
            LocService.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            LocService.LanguageChanged -= OnLanguageChanged;

            if (difficultyDropdown != null)
                difficultyDropdown.onValueChanged.RemoveListener(OnDifficultyChanged);
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
        // Сложность
        // ----------------------------------------------------------------

        private void PopulateDifficultyDropdown()
        {
            if (difficultyDropdown == null) return;

            // Снимаем слушатель перед перезаполнением, восстанавливаем после
            difficultyDropdown.onValueChanged.RemoveListener(OnDifficultyChanged);

            int savedValue = SettingsService.LoadDifficulty();

            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new List<string>
            {
                LocService.Get("menu.difficulty_easy"),
                LocService.Get("menu.difficulty_normal"),
                LocService.Get("menu.difficulty_hard"),
            });

            // Восстанавливаем сохранённое значение
            difficultyDropdown.SetValueWithoutNotify(Mathf.Clamp(savedValue, 0, 2));

            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        }

        private void OnDifficultyChanged(int index)
        {
            SettingsService.SaveDifficulty(index);
        }

        private void OnLanguageChanged()
        {
            PopulateDifficultyDropdown();
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
