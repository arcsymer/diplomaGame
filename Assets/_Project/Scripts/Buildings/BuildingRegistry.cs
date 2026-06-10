using System;
using System.Collections.Generic;
using DiplomaGame.Runtime.Units;

namespace DiplomaGame.Runtime.Buildings
{
    /// <summary>
    /// Статический реестр всех активных зданий сцены.
    /// Building регистрируется в OnEnable и снимается с учёта в OnDisable.
    /// По образцу UnitRegistry — без аллокаций в горячих путях.
    /// </summary>
    public static class BuildingRegistry
    {
        private static readonly List<Building> _all = new List<Building>(32);

        /// <summary>Все зарегистрированные здания.</summary>
        public static IReadOnlyList<Building> AllBuildings => _all;

        /// <summary>Вызывается при регистрации нового здания (M7 Audio шина).</summary>
        public static event Action<Building> BuildingRegistered;

        /// <summary>Регистрирует здание (вызывается из Building.OnEnable).</summary>
        public static void Register(Building building)
        {
            if (!_all.Contains(building))
            {
                _all.Add(building);
                BuildingRegistered?.Invoke(building);
            }
        }

        /// <summary>Снимает здание с учёта (вызывается из Building.OnDisable).</summary>
        public static void Unregister(Building building)
        {
            _all.Remove(building);
        }

        /// <summary>
        /// Заполняет переданный буфер зданиями указанной фракции.
        /// Без аллокаций — список очищается и заполняется заново.
        /// </summary>
        public static void GetBuildings(Faction faction, List<Building> buffer)
        {
            buffer.Clear();
            for (int i = 0; i < _all.Count; i++)
            {
                if (_all[i] != null && _all[i].Faction == faction)
                    buffer.Add(_all[i]);
            }
        }
    }
}
