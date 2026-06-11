using DiplomaGame.Runtime.Buildings;
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

            string title = $"Производство: {produces.DisplayName}";
            string desc  = string.IsNullOrEmpty(produces.Description)
                ? data.Description
                : produces.Description;

            string stats = $"Стоимость: {data.ProductionCost}   Время: {data.ProductionTime:F0}с";

            if (produces.SupplyCost > 0)
                stats += $"   Supply: {produces.SupplyCost}";

            return new TooltipData(title, desc, stats);
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
