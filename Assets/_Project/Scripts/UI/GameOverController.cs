using DiplomaGame.Runtime.Audio;
using DiplomaGame.Runtime.Core;
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
        [SerializeField] private GameObject    victoryPanel;
        [SerializeField] private GameObject    defeatPanel;
        [SerializeField] private MatchStatsView _statsView;

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
            ShowVictory(null);
        }

        /// <summary>
        /// Показывает экран победы со статистикой матча.
        /// stats может быть null — тогда MatchStatsView очищается.
        /// </summary>
        public void ShowVictory(MatchStats stats)
        {
            if (defeatPanel  != null) defeatPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(true);
            _statsView?.Show(stats, playerWon: true);
            Time.timeScale = 0f;
            AudioManager.Instance?.PlayVictory();
        }

        /// <summary>
        /// Показывает экран поражения и останавливает время.
        /// Вызывается из M9 (DefeatCondition / ScenarioController).
        /// </summary>
        public void ShowDefeat()
        {
            ShowDefeat(null);
        }

        /// <summary>
        /// Показывает экран поражения со статистикой матча.
        /// stats может быть null — тогда MatchStatsView очищается.
        /// </summary>
        public void ShowDefeat(MatchStats stats)
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel  != null) defeatPanel.SetActive(true);
            _statsView?.Show(stats, playerWon: false);
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
