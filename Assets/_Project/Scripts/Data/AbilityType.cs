namespace DiplomaGame.Runtime.Data
{
    /// <summary>
    /// Тип способности героя. Каждый вариант определяет логику применения в AbilitySystem.
    /// Анти-микро-дизайн: способности дают герою массовое влияние на бой
    /// без увеличения APM (ADR-013).
    /// </summary>
    public enum AbilityType
    {
        /// <summary>Рывок вперёд — мобильность героя.</summary>
        Dash,

        /// <summary>Ударная волна — AoE-урон по врагам вокруг героя.</summary>
        Shockwave,

        /// <summary>Ремонтное поле — лечение союзных юнитов вокруг героя.</summary>
        RepairField,

        /// <summary>Перегрузка — временный бафф скорострельности и урона героя.</summary>
        Overcharge,
    }
}
