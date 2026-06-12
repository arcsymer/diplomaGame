using System;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Core.Localization;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Selection;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Провайдер тултипа для hit-area производства юнитов.
    /// Динамически читает текущее выделенное здание из SelectionSystem.
    /// Если здание без производства выбрано — тултип не показывать
    /// (возвращает пустые данные; TooltipTrigger не вызовет Show если данные пустые).
    /// </summary>
    public sealed class UnitProductionTooltipProvider : MonoBehaviour, ITooltipProvider
    {
        [SerializeField] private SelectionSystem selectionSystem;

        // Кэш: последний использованный BuildingData и соответствующий TooltipData.
        // Пересчёт — только при смене здания (сравнение по ссылке на BuildingData).
        private BuildingData _cachedData;
        private TooltipData  _cachedTooltip;

        private void OnEnable()
        {
            LocService.LanguageChanged += InvalidateCache;
        }

        private void OnDisable()
        {
            LocService.LanguageChanged -= InvalidateCache;
        }

        private void InvalidateCache()
        {
            _cachedData = null;
        }

        public TooltipData GetTooltipData()
        {
            if (selectionSystem == null) return default;

            var building = selectionSystem.SelectedBuilding;
            if (building == null) return default;

            var production = building.GetComponent<ProductionBuilding>();
            if (production == null) return default;

            var data = building.Data;
            if (data == null) return default;

            var produces = data.Produces;
            if (produces == null) return default;

            // Пересчитываем строки при смене здания ИЛИ смене языка (_cachedData == null после InvalidateCache)
            if (!ReferenceEquals(data, _cachedData))
            {
                _cachedData = data;

                string title = LocService.Get("tooltip.production_prefix") + produces.DisplayName;
                string desc  = string.IsNullOrEmpty(produces.Description)
                    ? data.Description
                    : produces.Description;

                string stats = LocService.Get("tooltip.cost_label") + data.ProductionCost
                    + "   " + LocService.Get("tooltip.time_label")
                    + FormattableString.Invariant($"{data.ProductionTime:F0}с");
                if (produces.SupplyCost > 0)
                    stats += "   " + LocService.Get("tooltip.supply_label") + produces.SupplyCost;

                _cachedTooltip = new TooltipData(title, desc, stats);
            }

            return _cachedTooltip;
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        internal void InitForTest(SelectionSystem system)
        {
            selectionSystem = system;
        }
    }
}
