using System.Collections.Generic;

namespace DiplomaGame.Runtime.Units
{
    /// <summary>
    /// Статический реестр всех живых юнитов сцены.
    /// Unit регистрируется в OnEnable и снимается с учёта в OnDisable — без аллокаций.
    /// </summary>
    public static class UnitRegistry
    {
        private static readonly List<Unit> _all = new List<Unit>(64);

        /// <summary>Все зарегистрированные юниты.</summary>
        public static IReadOnlyList<Unit> AllUnits => _all;

        /// <summary>Регистрирует юнита (вызывается из Unit.OnEnable).</summary>
        public static void Register(Unit unit)
        {
            if (!_all.Contains(unit))
                _all.Add(unit);
        }

        /// <summary>Снимает юнита с учёта (вызывается из Unit.OnDisable).</summary>
        public static void Unregister(Unit unit)
        {
            _all.Remove(unit);
        }

        /// <summary>
        /// Заполняет переданный буфер юнитами игрока. Без аллокаций — список очищается
        /// и заполняется заново.
        /// </summary>
        public static void GetPlayerUnits(List<Unit> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i] != null && _all[i].Faction == Faction.Player)
                    buffer.Add(_all[i]);
            }
        }
    }
}
