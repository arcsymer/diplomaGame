using System.Collections;
using System.Collections.Generic;
using DiplomaGame.Runtime.Selection;
using DiplomaGame.Runtime.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// HUD-бейдж «бездействующая армия» (Circle-17).
    /// Отображает счётчик юнитов игрока, бездействующих >= 5 секунд.
    /// Виден только когда счётчик > 0. Клик — выделить все такие юниты.
    ///
    /// Поллинг через WaitForSeconds (~0.75 с) — без Update-аллокаций.
    /// Пульс масштаба через UiPulse при изменении счётчика.
    /// Ссылки проставляются через Forge (SerializedObject).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class IdleArmyIndicator : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные поля (проставляются через Forge)
        // ----------------------------------------------------------------

        [Header("Зависимости")]
        [Tooltip("SelectionSystem в сцене (GameManagers).")]
        [SerializeField] private SelectionSystem _selectionSystem;

        [Header("UI")]
        [Tooltip("Текстовый лейбл со счётчиком («3 idle»).")]
        [SerializeField] private TMP_Text _countLabel;

        [Tooltip("(Опционально) UiPulse на корне бейджа — вздрагивает при изменении счётчика.")]
        [SerializeField] private UiPulse _pulse;

        [Header("Тайминги")]
        [Tooltip("Интервал поллинга юнитов (секунды). По умолчанию 0.75.")]
        [SerializeField] private float _pollInterval = 0.75f;

        // ----------------------------------------------------------------
        // Приватные поля — буферы (предаллоцированы в Awake)
        // ----------------------------------------------------------------

        // Текущий список юнитов игрока (переиспользуемый буфер — нет аллокаций при поллинге).
        private readonly List<Unit> _unitBuffer   = new List<Unit>(64);

        // Словарь unit → время последней НЕ-Idle активности.
        private readonly Dictionary<Unit, float> _lastNonIdleTimes = new Dictionary<Unit, float>(64);

        // Буфер результата GetIdleUnits (переиспользуется между поллами).
        private readonly List<Unit> _idleBuffer   = new List<Unit>(32);

        // Буфер для PruneDeadUnits (нет аллокаций List в горячем пути).
        private readonly List<Unit> _deadBuffer   = new List<Unit>(16);

        // ----------------------------------------------------------------
        // Внутреннее состояние
        // ----------------------------------------------------------------

        private Button    _button;
        private int       _lastDisplayedCount = -1;
        private Coroutine _pollRoutine;

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов
        // ----------------------------------------------------------------

        /// <summary>Текущий счётчик застоявшихся юнитов (для тестов).</summary>
        internal int IdleCount => _idleBuffer.Count;

        /// <summary>Позволяет тестам инжектировать SelectionSystem без Forge.</summary>
        internal void InitForTest(SelectionSystem sys)
        {
            _selectionSystem = sys;
        }

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnBadgeClicked);
            _pollRoutine = StartCoroutine(PollRoutine());
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnBadgeClicked);

            if (_pollRoutine != null)
            {
                StopCoroutine(_pollRoutine);
                _pollRoutine = null;
            }
        }

        // ----------------------------------------------------------------
        // Поллинг (корутина — не Update)
        // ----------------------------------------------------------------

        private IEnumerator PollRoutine()
        {
            var wait = new WaitForSeconds(_pollInterval);

            while (true)
            {
                Poll();
                yield return wait;
            }
        }

        private void Poll()
        {
            float now = Time.time;

            // 1. Получаем актуальный список юнитов игрока (без аллокаций).
            UnitRegistry.GetPlayerUnits(_unitBuffer);

            // 2. Убираем из словаря мёртвые/деспавнившиеся юниты.
            IdleArmyLogic.PruneDeadUnits(_unitBuffer, _lastNonIdleTimes, _deadBuffer);

            // 3. Обновляем временны́е метки.
            IdleArmyLogic.UpdateTimestamps(_unitBuffer, _lastNonIdleTimes, now);

            // 4. Вычисляем застоявших.
            IdleArmyLogic.GetIdleUnits(_unitBuffer, _lastNonIdleTimes, now, _idleBuffer);

            // 5. Обновляем UI.
            UpdateBadge(_idleBuffer.Count);
        }

        // ----------------------------------------------------------------
        // Обновление UI
        // ----------------------------------------------------------------

        private void UpdateBadge(int count)
        {
            // Скрываем если нет бездействующих.
            bool shouldShow = count > 0;
            if (gameObject.activeSelf != shouldShow)
                gameObject.SetActive(shouldShow);

            if (!shouldShow) return;

            // Обновляем текст только при изменении (нет SetText-аллокации каждый poll).
            if (count != _lastDisplayedCount)
            {
                _lastDisplayedCount = count;

                if (_countLabel != null)
                    _countLabel.SetText("{0:0} idle", (float)count);

                // Пульс-анимация при изменении числа — привлекает взгляд.
                if (_pulse != null)
                    _pulse.TriggerPulse();
            }
        }

        // ----------------------------------------------------------------
        // Обработка клика
        // ----------------------------------------------------------------

        private void OnBadgeClicked()
        {
            if (_selectionSystem == null) return;
            if (_idleBuffer.Count == 0) return;

            _selectionSystem.SelectUnits(_idleBuffer);
        }
    }
}
