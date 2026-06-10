using UnityEngine;

namespace DiplomaGame.Runtime.Units
{
    /// <summary>
    /// Тип приказа юниту.
    /// </summary>
    public enum UnitCommandType
    {
        Move,
        AttackMove,
        Hold,
        Patrol,
    }

    /// <summary>
    /// Неизменяемая структура-приказ. Создаётся через статические фабрики.
    /// Нет аллокаций — передаётся по значению.
    /// </summary>
    public readonly struct UnitCommand
    {
        public readonly UnitCommandType Type;
        public readonly Vector3         TargetPoint;

        private UnitCommand(UnitCommandType type, Vector3 targetPoint)
        {
            Type        = type;
            TargetPoint = targetPoint;
        }

        /// <summary>Приказ двигаться к точке.</summary>
        public static UnitCommand Move(Vector3 point)
            => new UnitCommand(UnitCommandType.Move, point);

        /// <summary>Приказ двигаться к точке с атакой по пути (до M4 ведёт себя как Move).</summary>
        public static UnitCommand AttackMove(Vector3 point)
            => new UnitCommand(UnitCommandType.AttackMove, point);

        /// <summary>Приказ остановиться и держать позицию.</summary>
        public static UnitCommand Hold()
            => new UnitCommand(UnitCommandType.Hold, Vector3.zero);

        /// <summary>Приказ патрулировать между текущей позицией и точкой.</summary>
        public static UnitCommand Patrol(Vector3 point)
            => new UnitCommand(UnitCommandType.Patrol, point);
    }
}
