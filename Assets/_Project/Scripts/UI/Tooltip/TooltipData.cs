namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Данные для отображения одного тултипа.
    /// Stats == null — блок статистики скрыт.
    /// </summary>
    public readonly struct TooltipData
    {
        public readonly string Title;
        public readonly string Description;
        public readonly string Stats;        // null — не показывать

        public TooltipData(string title, string description, string stats = null)
        {
            Title       = title;
            Description = description;
            Stats       = stats;
        }
    }
}
