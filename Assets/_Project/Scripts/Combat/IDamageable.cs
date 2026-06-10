namespace DiplomaGame.Runtime.Combat
{
    /// <summary>
    /// Интерфейс для всего, что может получить урон.
    /// Реализация логики HP/смерти — M4.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}
