namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Контракт для любого компонента, способного предоставить данные тултипа.
    /// </summary>
    public interface ITooltipProvider
    {
        TooltipData GetTooltipData();
    }
}
