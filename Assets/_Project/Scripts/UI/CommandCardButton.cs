using System;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Одна кнопка в командной карте здания (v6).
    /// Bind() привязывает к ProductionEntry; Hide() скрывает слот.
    /// Реализует ITooltipProvider — данные тултипа строятся один раз при Bind.
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

        // Индекс слота — нужен только для выбора цвета заглушки.
        // Проставляется Forge-кнопкой через SerializedObject.
        [SerializeField] private int slotIndex;

        private ProductionEntry   _entry;
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

            // Иконка
            if (iconImage != null)
            {
                if (entry.icon != null)
                {
                    iconImage.sprite = entry.icon;
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
            string displayName = entry.unitData != null ? entry.unitData.DisplayName : "???";
            string hotkey      = string.IsNullOrEmpty(entry.hotkeyLabel) ? "" : entry.hotkeyLabel;

            if (unitNameText != null) unitNameText.text = displayName;
            if (costText     != null) costText.text     = entry.cost.ToString();
            if (hotkeyText   != null) hotkeyText.text   = hotkey;

            // Строим тултип один раз при Bind (Invariant — без culture-зависимых разделителей)
            string title = "Производство: " + displayName;

            string desc = entry.unitData != null && !string.IsNullOrEmpty(entry.unitData.Description)
                ? entry.unitData.Description
                : "";

            string stats = FormattableString.Invariant(
                $"Стоимость: {entry.cost}   Время: {entry.productionTime:F0}с");

            if (entry.unitData != null && entry.unitData.SupplyCost > 0)
                stats += FormattableString.Invariant($"   Supply: {entry.unitData.SupplyCost}");

            if (!string.IsNullOrEmpty(hotkey))
                stats += "   [" + hotkey + "]";

            _tooltipData = new TooltipData(title, desc, stats);

            // Подписка на клик (один раз)
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClick);
            }
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

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        internal void InitForTest(int index)
        {
            slotIndex = index;
        }
    }
}
