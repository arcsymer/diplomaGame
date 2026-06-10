namespace DiplomaGame.Runtime.Units
{
    /// <summary>
    /// Состояние боевого ИИ юнита (UnitCombat).
    /// </summary>
    public enum CombatState
    {
        /// <summary>Нет активного боевого действия.</summary>
        None,

        /// <summary>Юнит движется к цели в агро-радиусе.</summary>
        Engaging,

        /// <summary>Юнит в дальности атаки — стоит и атакует.</summary>
        Attacking,

        /// <summary>HP ниже порога — юнит отступает к базе.</summary>
        Retreating,
    }
}
