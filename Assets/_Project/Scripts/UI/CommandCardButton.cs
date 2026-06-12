using System;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Tech;
using DiplomaGame.Runtime.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Одна кнопка в командной карте здания (v6/v7).
    /// Bind() привязывает к ProductionEntry; BindTech() — к TechEntry.
    /// Hide() скрывает слот. Реализует ITooltipProvider.
    /// </summary>
    public sealed class CommandCardButton : MonoBehaviour, ITooltipProvider
    {
        // Placeholder-цвета для слотов без иконки (индексы 0-2)
        private static readonly Color[] PlaceholderColors =
        {
            new Color(0.25f, 0.45f, 0.75f, 1f),
            new Color(0.55f, 0.30f, 0.20f, 1f),
            new Color(0.25f, 0.55f, 0.30f, 1f),
        };

        [Header("Визуал кнопки")]
        [SerializeField] private Image    iconImage;
        [SerializeField] private TMP_Text unitNameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text hotkeyText;
        [SerializeField] private Button   button;

        [Header("Tech overlay (v7)")]
        /// <summary>
        /// Полупрозрачный зелёный оверлей — показывается когда технология уже исследована.
        /// Содержит Image (0.2, 0.8, 0.3, 0.6) и TMP "✓".
        /// Проставляется Forge через SerializedObject.
        /// </summary>
        [SerializeField] private GameObject researchedOverlay;

        // Индекс слота — нужен только для выбора цвета заглушки.
        // Проставляется Forge-кнопкой через SerializedObject.
        [SerializeField] private int slotIndex;

        private ProductionEntry    _entry;
        private ProductionBuilding _building;

        // Кэш тултипа — строится при Bind (холодный путь)
        private TooltipData _tooltipData;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>
        /// Привязывает кнопку к записи производства. Холодный путь — допустима конкатенация строк.
        /// </summary>
        public void Bind(ProductionEntry entry, ProductionBuilding building)
        {
            _entry    = entry;
            _building = building;

            gameObject.SetActive(true);

            // Иконка — сначала entry.icon, потом techData.Icon как fallback
            if (iconImage != null)
            {
                Sprite iconSprite = entry.icon;
                if (iconSprite == null && entry.techData != null)
                    iconSprite = entry.techData.Icon;

                if (iconSprite != null)
                {
                    iconImage.sprite = iconSprite;
                    iconImage.color  = Color.white;
                }
                else
                {
                    // Цветной placeholder по индексу слота
                    iconImage.sprite = null;
                    iconImage.color  = PlaceholderColors[slotIndex % PlaceholderColors.Length];
                }
            }

            // Тексты (холодный путь — string allocation OK)
            // techData fallback: для записей исследований в production-очереди
            string displayName;
            if (entry.unitData != null)
                displayName = entry.unitData.DisplayName;
            else if (entry.techData != null)
                displayName = entry.techData.DisplayName;
            else
                displayName = "???";
            string hotkey      = string.IsNullOrEmpty(entry.hotkeyLabel) ? "" : entry.hotkeyLabel;

            if (unitNameText != null) unitNameText.text = displayName;
            if (costText     != null) costText.text     = entry.cost.ToString();
            if (hotkeyText   != null) hotkeyText.text   = hotkey;

            // Строим тултип один раз при Bind (Invariant — без culture-зависимых разделителей)
            string title = "Производство: " + displayName;

            string desc;
            if (entry.unitData != null && !string.IsNullOrEmpty(entry.unitData.Description))
                desc = entry.unitData.Description;
            else if (entry.techData != null && !string.IsNullOrEmpty(entry.techData.Description))
                desc = entry.techData.Description;
            else
                desc = "";

            string stats = FormattableString.Invariant(
                $"Стоимость: {entry.cost}   Время: {entry.productionTime:F0}с");

            if (entry.unitData != null && entry.unitData.SupplyCost > 0)
                stats += FormattableString.Invariant($"   Supply: {entry.unitData.SupplyCost}");

            if (!string.IsNullOrEmpty(hotkey))
                stats += "   [" + hotkey + "]";

            _tooltipData = new TooltipData(title, desc, stats);

            // Скрываем tech-overlay для production-кнопок
            if (researchedOverlay != null)
                researchedOverlay.SetActive(false);

            // Кнопка всегда интерактивна для production-слота
            if (button != null)
                button.interactable = true;

            // Подписка на клик (один раз)
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }
        }

        /// <summary>
        /// Привязывает кнопку к технологии (v7).
        /// Состояние: исследована / prerequisites не выполнены / доступна.
        /// faction нужна для проверки TechRegistry.
        /// </summary>
        public void BindTech(TechEntry techEntry, ProductionBuilding building, Faction faction)
        {
            if (techEntry == null || techEntry.techData == null)
            {
                Hide();
                return;
            }

            var tech = techEntry.techData;

            _building = building;
            gameObject.SetActive(true);

            // Иконка
            if (iconImage != null)
            {
                if (tech.Icon != null)
                {
                    iconImage.sprite = tech.Icon;
                    iconImage.color  = Color.white;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.color  = PlaceholderColors[slotIndex % PlaceholderColors.Length];
                }
            }

            // Тексты
            if (unitNameText != null) unitNameText.text = tech.DisplayName;
            if (costText     != null) costText.text     = tech.Cost.ToString();
            if (hotkeyText   != null) hotkeyText.text   = string.IsNullOrEmpty(tech.HotkeyLabel) ? "" : tech.HotkeyLabel;

            bool isResearched   = TechRegistry.Instance.IsResearched(faction, tech);
            bool canResearch    = TechRegistry.Instance.CanResearch(faction, tech);

            // Тултип
            string title = "Исследование: " + tech.DisplayName;
            string desc  = string.IsNullOrEmpty(tech.Description) ? "" : tech.Description;

            string stats = FormattableString.Invariant(
                $"Стоимость: {tech.Cost}   Время: {tech.ResearchTime:F0}с");

            if (!string.IsNullOrEmpty(tech.HotkeyLabel))
                stats += "   [" + tech.HotkeyLabel + "]";

            // Если prerequisites не выполнены — перечислить их в тултипе
            if (!isResearched && !canResearch && tech.Prerequisites != null && tech.Prerequisites.Length > 0)
            {
                string prereqNames = "";
                for (int i = 0; i < tech.Prerequisites.Length; i++)
                {
                    if (tech.Prerequisites[i] == null) continue;
                    if (!TechRegistry.Instance.IsResearched(faction, tech.Prerequisites[i]))
                    {
                        if (prereqNames.Length > 0) prereqNames += ", ";
                        prereqNames += tech.Prerequisites[i].DisplayName;
                    }
                }
                if (prereqNames.Length > 0)
                    desc += (desc.Length > 0 ? "\n" : "") + "Требует: " + prereqNames;
            }

            _tooltipData = new TooltipData(title, desc, stats);

            // Overlay исследована
            SetResearched(isResearched);

            // Интерактивность
            if (button != null)
            {
                button.interactable = !isResearched && canResearch;
                button.onClick.RemoveAllListeners();

                if (!isResearched && canResearch)
                {
                    // Синтетическая ProductionEntry (по образцу EnemyCommander.DecideResearch)
                    var techRef = tech; // захват в замыкании
                    button.onClick.AddListener(() => EnqueueResearch(techRef));
                }
            }

            // Серая иконка когда prerequisites не выполнены
            if (iconImage != null && !isResearched && !canResearch)
                iconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        }

        /// <summary>Включает/выключает оверлей "исследовано".</summary>
        public void SetResearched(bool researched)
        {
            if (researchedOverlay != null)
                researchedOverlay.SetActive(researched);

            if (button != null && researched)
                button.interactable = false;
        }

        /// <summary>Скрывает слот (нет entry для этого индекса).</summary>
        public void Hide()
        {
            _entry    = null;
            _building = null;
            gameObject.SetActive(false);
        }

        // ----------------------------------------------------------------
        // ITooltipProvider
        // ----------------------------------------------------------------

        public TooltipData GetTooltipData() => _tooltipData;

        // ----------------------------------------------------------------
        // Внутреннее
        // ----------------------------------------------------------------

        private void OnClick()
        {
            if (_building == null || _entry == null) return;
            _building.TryEnqueue(_entry);
        }

        private void EnqueueResearch(TechData tech)
        {
            if (_building == null || tech == null) return;

            var entry = new ProductionEntry
            {
                techData       = tech,
                cost           = tech.Cost,
                productionTime = tech.ResearchTime,
                icon           = tech.Icon,
                hotkeyLabel    = tech.HotkeyLabel,
                unitData       = null,
            };

            _building.TryEnqueue(entry);
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        internal void InitForTest(int index)
        {
            slotIndex = index;
        }
    }
}
