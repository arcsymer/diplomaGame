using DiplomaGame.Runtime.Audio;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Управляет экранами победы и поражения.
    /// Панели Victory и Defeat скрыты по умолчанию.
    /// ShowVictory / ShowDefeat — публичный API, вызывается из M9 (сценарий).
    /// Restart — перезагружает активную сцену; Exit to Menu — уходит в главное меню.
    /// </summary>
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Start()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel  != null) defeatPanel.SetActive(false);
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает true, если один из экранов GameOver уже отображается.
        /// Используется GameWatcher для идемпотентности.
        /// </summary>
        public bool IsShown =>
            (victoryPanel != null && victoryPanel.activeSelf) ||
            (defeatPanel  != null && defeatPanel.activeSelf);

        /// <summary>
        /// Показывает экран победы и останавливает время.
        /// Вызывается из M9 (WinCondition / ScenarioController).
        /// </summary>
        public void ShowVictory()
        {
            if (defeatPanel  != null) defeatPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(true);
            Time.timeScale = 0f;
            AudioManager.Instance?.PlayVictory();
        }

        /// <summary>
        /// Показывает экран поражения и останавливает время.
        /// Вызывается из M9 (DefeatCondition / ScenarioController).
        /// </summary>
        public void ShowDefeat()
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel  != null) defeatPanel.SetActive(true);
            Time.timeScale = 0f;
            AudioManager.Instance?.PlayDefeat();
        }

        // ----------------------------------------------------------------
        // Кнопки UI (вызываются через UnityEvent из Inspector)
        // ----------------------------------------------------------------

        /// <summary>Кнопка «Заново» — перезагружает текущую сцену.</summary>
        public void OnRestartClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Кнопка «Выйти в меню».</summary>
        public void OnExitToMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
