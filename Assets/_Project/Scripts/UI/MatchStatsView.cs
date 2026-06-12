using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Core.Localization;
using DiplomaGame.Runtime.Units;
using TMPro;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Отображает экран статистики матча.
    /// Show(stats, playerWon) заполняет TMP-поля без строковых аллокаций.
    /// Размещается как компонент на StatsPanel внутри VictoryPanel / DefeatPanel.
    /// </summary>
    public sealed class MatchStatsView : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Поля игрока (зелёные)
        // ----------------------------------------------------------------

        [SerializeField] private TMP_Text _playerKills;
        [SerializeField] private TMP_Text _playerLosses;
        [SerializeField] private TMP_Text _playerDamageDealt;
        [SerializeField] private TMP_Text _playerDamageTaken;
        [SerializeField] private TMP_Text _playerCrystals;
        [SerializeField] private TMP_Text _playerProduced;
        [SerializeField] private TMP_Text _playerArmyPeak;

        // ----------------------------------------------------------------
        // Поля врага (красные)
        // ----------------------------------------------------------------

        [SerializeField] private TMP_Text _enemyKills;
        [SerializeField] private TMP_Text _enemyLosses;
        [SerializeField] private TMP_Text _enemyDamageDealt;
        [SerializeField] private TMP_Text _enemyDamageTaken;
        [SerializeField] private TMP_Text _enemyProduced;
        [SerializeField] private TMP_Text _enemyCrystals;
        [SerializeField] private TMP_Text _enemyArmyPeak;

        // ----------------------------------------------------------------
        // Длительность
        // ----------------------------------------------------------------

        [SerializeField] private TMP_Text _durationText;

        // Кэш: -1 означает «не задана» (экран закрыт / stats == null)
        private float _lastDurationSeconds = -1f;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void OnEnable()
        {
            LocService.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            LocService.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            // Перерисовываем строку длительности в новом языке.
            if (_lastDurationSeconds >= 0f)
                SetDuration(_lastDurationSeconds);
            else if (_durationText != null)
                _durationText.SetText(LocService.Get("stats.duration_empty"));
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>
        /// Заполняет все поля статистики и активирует панель.
        /// stats может быть null — тогда сбрасывает поля в «—».
        /// </summary>
        public void Show(MatchStats stats, bool playerWon)
        {
            gameObject.SetActive(true);

            if (stats == null)
            {
                ClearAll();
                return;
            }

            // Без аллокаций — используем числовые перегрузки SetText
            SetInt(_playerKills,      stats.UnitsKilled  (Faction.Player));
            SetInt(_playerLosses,     stats.UnitsLost    (Faction.Player));
            SetFloat(_playerDamageDealt, stats.DamageDealt(Faction.Player));
            SetFloat(_playerDamageTaken, stats.DamageTaken(Faction.Player));
            SetInt(_playerCrystals,   stats.CrystalsMined(Faction.Player));
            SetInt(_playerProduced,   stats.UnitsProduced(Faction.Player));
            SetInt(_playerArmyPeak,   stats.ArmyPeak     (Faction.Player));

            SetInt(_enemyKills,       stats.UnitsKilled  (Faction.Enemy));
            SetInt(_enemyLosses,      stats.UnitsLost    (Faction.Enemy));
            SetFloat(_enemyDamageDealt, stats.DamageDealt(Faction.Enemy));
            SetFloat(_enemyDamageTaken, stats.DamageTaken(Faction.Enemy));
            SetInt(_enemyCrystals,    stats.CrystalsMined(Faction.Enemy));
            SetInt(_enemyProduced,    stats.UnitsProduced(Faction.Enemy));
            SetInt(_enemyArmyPeak,    stats.ArmyPeak     (Faction.Enemy));

            _lastDurationSeconds = stats.MatchDurationSeconds;
            SetDuration(_lastDurationSeconds);
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы — без аллокаций
        // ----------------------------------------------------------------

        private static void SetInt(TMP_Text label, int value)
        {
            if (label == null) return;
            label.SetText("{0}", value);
        }

        private static void SetFloat(TMP_Text label, float value)
        {
            if (label == null) return;
            // F0 — целое число без дробей, инвариантная культура
            label.SetText("{0:F0}", value);
        }

        private void SetDuration(float seconds)
        {
            if (_durationText == null) return;

            int totalSecs = (int)seconds;
            int mm        = totalSecs / 60;
            int ss        = totalSecs % 60;

            // Получаем локализованный шаблон "Длительность: {0:00}:{1:00}" / "Duration: {0:00}:{1:00}".
            // TMP SetText(string, float, float) — передаём минуты и секунды как float.
            // {0:00} — числовой формат с ведущим нулём, поддерживаемый TMP-форматтером.
            _durationText.SetText(LocService.Get("stats.duration_format"), mm, ss);
        }

        private void ClearAll()
        {
            SetDash(_playerKills);
            SetDash(_playerLosses);
            SetDash(_playerDamageDealt);
            SetDash(_playerDamageTaken);
            SetDash(_playerCrystals);
            SetDash(_playerProduced);
            SetDash(_playerArmyPeak);

            SetDash(_enemyKills);
            SetDash(_enemyLosses);
            SetDash(_enemyDamageDealt);
            SetDash(_enemyDamageTaken);
            SetDash(_enemyCrystals);
            SetDash(_enemyProduced);
            SetDash(_enemyArmyPeak);

            _lastDurationSeconds = -1f;
            if (_durationText != null)
                _durationText.SetText(LocService.Get("stats.duration_empty"));
        }

        private static void SetDash(TMP_Text label)
        {
            if (label != null) label.SetText("--");
        }
    }
}
