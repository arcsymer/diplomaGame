using System.Text;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Selection;
using DiplomaGame.Runtime.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Нижняя панель RTS: отображает выделенных юнитов или выделенное здание.
    /// Пустое выделение — панель скрыта.
    /// </summary>
    public sealed class SelectionPanel : MonoBehaviour
    {
        [SerializeField] private SelectionSystem selectionSystem;

        [Header("Общий текст")]
        [SerializeField] private TMP_Text infoText;

        [Header("Прогресс производства")]
        [SerializeField] private GameObject progressRoot;   // скрываем целиком, если не здание
        [SerializeField] private Image      progressFill;
        [SerializeField] private TMP_Text   queueText;
        [SerializeField] private TMP_Text   hintText;

        private ProductionBuilding _trackedProduction;
        private int                _lastQueueCount = -1;

        // Переиспользуемый StringBuilder для составной строки выделения (без GC)
        private readonly StringBuilder _selectionSb = new StringBuilder(64);

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            if (selectionSystem != null)
            {
                selectionSystem.SelectionChanged  += OnSelectionChanged;
                selectionSystem.BuildingSelected  += OnBuildingSelected;
            }
        }

        private void OnDisable()
        {
            if (selectionSystem != null)
            {
                selectionSystem.SelectionChanged  -= OnSelectionChanged;
                selectionSystem.BuildingSelected  -= OnBuildingSelected;
            }
        }

        private void Update()
        {
            // Прогресс обновляем каждый кадр только если отслеживаем здание с производством
            if (_trackedProduction == null) return;
            if (progressFill == null)       return;

            // Fill-анимация — плавная, обновляем каждый кадр
            progressFill.fillAmount = _trackedProduction.CurrentProgress01;

            // Текст очереди — обновляем только при изменении (избегаем лишней строки каждый кадр)
            if (queueText != null)
            {
                int currentCount = _trackedProduction.QueueCount;
                if (currentCount != _lastQueueCount)
                {
                    _lastQueueCount = currentCount;
                    // SetText(string, float) использует внутренний форматтер TMP — без boxing/GC
                    queueText.SetText("В очереди: {0}", currentCount);
                }
            }
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        internal void InitForTest(SelectionSystem system)
        {
            selectionSystem = system;
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void OnSelectionChanged()
        {
            _trackedProduction = null;
            _lastQueueCount    = -1;

            if (selectionSystem == null) return;

            var selected = selectionSystem.Selected;

            if (selected.Count == 0 && selectionSystem.SelectedBuilding == null)
            {
                SetVisible(false);
                return;
            }

            if (selected.Count > 0)
            {
                // Показываем юниты
                SetVisible(true);
                ShowProgressBlock(false);

                int totalHp = 0;
                int maxHp   = 0;

                for (int i = 0; i < selected.Count; i++)
                {
                    var unit = selected[i];
                    if (unit == null) continue;

                    var health = unit.GetComponent<Health>();
                    if (health != null)
                    {
                        totalHp += Mathf.RoundToInt(health.CurrentHp);
                        maxHp   += Mathf.RoundToInt(health.MaxHp);
                    }
                }

                if (infoText != null)
                {
                    // Используем кэшированный StringBuilder — без строковых аллокаций
                    _selectionSb.Clear();
                    _selectionSb.Append("Выбрано: ");
                    _selectionSb.Append(selected.Count);
                    _selectionSb.Append(" юнитов");
                    if (maxHp > 0)
                    {
                        _selectionSb.Append("  HP: ");
                        _selectionSb.Append(totalHp);
                        _selectionSb.Append('/');
                        _selectionSb.Append(maxHp);
                    }
                    infoText.SetText(_selectionSb);
                }
            }
        }

        private void OnBuildingSelected(Building building)
        {
            _trackedProduction = null;
            _lastQueueCount    = -1;

            if (building == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            // Имя здания
            string buildingName = building.Data != null ? building.Data.name : building.name;
            if (infoText != null)
                infoText.SetText(buildingName);

            // Производство (если есть)
            _trackedProduction = building.GetComponent<ProductionBuilding>();
            ShowProgressBlock(_trackedProduction != null);

            if (_trackedProduction != null && hintText != null)
                hintText.SetText("[T] — обучить    ПКМ — точка сбора");
        }

        private void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        private void ShowProgressBlock(bool show)
        {
            if (progressRoot != null)
                progressRoot.SetActive(show);
        }
    }
}
