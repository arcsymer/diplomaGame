using System.Text;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
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
    /// v6: добавлена CommandCard (3 кнопки) и QueueSlots (5 слотов) для multi-production.
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

        [Header("Command Card (v6)")]
        [SerializeField] private GameObject       commandCardRoot;
        [SerializeField] private CommandCardButton[] commandCardSlots = new CommandCardButton[3];
        [SerializeField] private QueueSlotUI[]       queueSlots       = new QueueSlotUI[5];

        private ProductionBuilding _trackedProduction;
        private int                _lastQueueCount = -1;

        // True если активен multi-production режим (командная карта)
        private bool _multiProductionMode;

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

            float progress = _trackedProduction.CurrentProgress01;

            // --- Legacy режим: обновляем progressFill ---
            if (!_multiProductionMode && progressFill != null)
                progressFill.fillAmount = progress;

            // --- Multi режим: обновляем слот 0 progressOverlay (без GC) ---
            if (_multiProductionMode && queueSlots != null && queueSlots.Length > 0 && queueSlots[0] != null)
                queueSlots[0].SetProgress(progress);

            // Текст очереди — обновляем только при изменении (избегаем лишней строки каждый кадр)
            int currentCount = _trackedProduction.QueueCount;
            if (currentCount != _lastQueueCount)
            {
                _lastQueueCount = currentCount;

                // Обновляем legacy queueText
                if (queueText != null)
                    queueText.SetText("В очереди: {0}", currentCount);

                // Обновляем очередь-слоты в multi-режиме
                if (_multiProductionMode)
                    RefreshQueueSlots(currentCount);
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
            _trackedProduction    = null;
            _lastQueueCount       = -1;
            _multiProductionMode  = false;
            HideCommandCard();

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
            _trackedProduction   = null;
            _lastQueueCount      = -1;
            _multiProductionMode = false;
            HideCommandCard();

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

            if (_trackedProduction != null && building.Data != null && building.Data.HasMultiProduction)
            {
                // --- Multi-production mode: командная карта ---
                _multiProductionMode = true;
                ShowProgressBlock(false);

                // Скрываем legacy hint и ProductionTooltipHitArea
                if (hintText != null) hintText.gameObject.SetActive(false);

                BindCommandCard(building.Data.ProductionEntries, _trackedProduction);
            }
            else if (_trackedProduction != null)
            {
                // --- Legacy mode ---
                _multiProductionMode = false;
                ShowProgressBlock(true);

                // В legacy режиме показываем синтетическую карту с одной кнопкой
                if (building.Data != null)
                {
                    var syntheticEntry = new ProductionEntry
                    {
                        unitData       = building.Data.Produces,
                        cost           = building.Data.ProductionCost,
                        productionTime = building.Data.ProductionTime,
                        icon           = null,
                        hotkeyLabel    = "T"
                    };
                    BindCommandCard(new[] { syntheticEntry }, _trackedProduction);
                }

                if (hintText != null)
                {
                    hintText.gameObject.SetActive(true);
                    hintText.SetText("[T] — обучить    ПКМ — точка сбора");
                }
            }
            else
            {
                // Здание без производства
                ShowProgressBlock(false);
                if (hintText != null) hintText.gameObject.SetActive(false);
            }
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

        // ----------------------------------------------------------------
        // Command Card (v6)
        // ----------------------------------------------------------------

        private void BindCommandCard(ProductionEntry[] entries, ProductionBuilding prod)
        {
            if (commandCardRoot != null)
                commandCardRoot.SetActive(true);

            if (commandCardSlots == null) return;

            for (int i = 0; i < commandCardSlots.Length; i++)
            {
                var slot = commandCardSlots[i];
                if (slot == null) continue;

                if (entries != null && i < entries.Length)
                    slot.Bind(entries[i], prod);
                else
                    slot.Hide();
            }

            // Сбрасываем очередь-слоты
            HideAllQueueSlots();
        }

        private void HideCommandCard()
        {
            if (commandCardRoot != null)
                commandCardRoot.SetActive(false);

            HideAllCommandSlots();
            HideAllQueueSlots();
        }

        private void HideAllCommandSlots()
        {
            if (commandCardSlots == null) return;
            for (int i = 0; i < commandCardSlots.Length; i++)
                if (commandCardSlots[i] != null) commandCardSlots[i].Hide();
        }

        private void HideAllQueueSlots()
        {
            if (queueSlots == null) return;
            for (int i = 0; i < queueSlots.Length; i++)
                if (queueSlots[i] != null) queueSlots[i].Hide();
        }

        private void RefreshQueueSlots(int count)
        {
            if (queueSlots == null || _trackedProduction == null) return;

            for (int i = 0; i < queueSlots.Length; i++)
            {
                var slot = queueSlots[i];
                if (slot == null) continue;

                if (i < count)
                {
                    var entry    = _trackedProduction.PeekEntryAt(i);
                    // Прогресс overlay — только у слота 0 (через SetProgress в Update)
                    float prog   = (i == 0) ? _trackedProduction.CurrentProgress01 : 0f;
                    slot.ShowEntry(entry, prog);
                }
                else
                {
                    slot.Hide();
                }
            }
        }
    }
}
