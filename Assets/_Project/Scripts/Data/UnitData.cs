using UnityEngine;

namespace DiplomaGame.Runtime.Data
{
    /// <summary>
    /// Определяет, кого юнит атакует в первую очередь при сканировании целей.
    /// </summary>
    public enum TargetPriority
    {
        /// <summary>Сначала проверяются вражеские юниты, затем здания.</summary>
        Units,
        /// <summary>Сначала проверяются вражеские здания, затем юниты.</summary>
        Buildings,
    }

    /// <summary>
    /// ScriptableObject с боевыми и ходовыми характеристиками юнита.
    /// Один ассет — один тип юнита. Нет хардкода.
    /// </summary>
    [CreateAssetMenu(menuName = "DiplomaGame/Unit Data", fileName = "NewUnitData")]
    public sealed class UnitData : ScriptableObject
    {
        [SerializeField] private string _displayName      = "Unit";
        [TextArea(2, 4)]
        [SerializeField] private string _description      = "";
        [Tooltip("Стоимость в Supply. 0 — строка Supply в тултипе скрыта.")]
        [SerializeField] private int    _supplyCost       = 0;
        [SerializeField] private float  _maxHp            = 100f;
        [SerializeField] private float  _damage           = 10f;
        [SerializeField] private float  _attackRange      = 8f;
        [SerializeField] private float  _attackCooldown   = 1f;
        [SerializeField] private float  _aggroRadius      = 12f;
        [SerializeField] private float  _moveSpeed        = 5f;

        [Tooltip("Доля HP (0..1), ниже которой юнит начинает отступление.")]
        [SerializeField] private float  _retreatHpFraction = 0.25f;

        [Tooltip("Если true — юнит никогда не уходит в отступление (для будущих типов).")]
        [SerializeField] private bool   _retreatDisabled  = false;

        [Tooltip("Кулдаун (сек) между повторными отступлениями. 0 — отступление одноразовое.")]
        [SerializeField] private float  _retreatCooldown  = 0f;

        [Tooltip("Радиус AoE-атаки. 0 — одиночная атака (поведение по умолчанию).")]
        [SerializeField] private float  _aoeRadius        = 0f;

        [Tooltip("Приоритет выбора цели при сканировании.")]
        [SerializeField] private TargetPriority _targetPriority = TargetPriority.Units;

        // ----------------------------------------------------------------
        // Публичный API (read-only)
        // ----------------------------------------------------------------

        public string         DisplayName       => _displayName;
        public string         Description       => _description;
        public int            SupplyCost        => _supplyCost;
        public float          MaxHp             => _maxHp;
        public float          Damage            => _damage;
        public float          AttackRange       => _attackRange;
        public float          AttackCooldown    => _attackCooldown;
        public float          AggroRadius       => _aggroRadius;
        public float          MoveSpeed         => _moveSpeed;
        public float          RetreatHpFraction => _retreatHpFraction;
        public bool           RetreatDisabled   => _retreatDisabled;
        public TargetPriority TargetPriority    => _targetPriority;

        /// <summary>
        /// Кулдаун повторного отступления в секундах.
        /// 0 — отступление одноразово за бой (старое поведение).
        /// &gt; 0 — юнит может отступать повторно, но не чаще чем раз в N секунд.
        /// </summary>
        public float  RetreatCooldown   => _retreatCooldown;

        /// <summary>
        /// Радиус AoE-атаки. 0 — одиночная атака (поведение по умолчанию).
        /// При AoE все враги в радиусе получают урон за один тик атаки.
        /// </summary>
        public float  AoeRadius         => _aoeRadius;

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов (без SerializedObject)
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт UnitData с произвольными значениями — без SerializedObject (доступно в PlayMode).
        /// Параметры aoeRadius и targetPriority опциональны для обратной совместимости.
        /// </summary>
        internal static UnitData CreateForTest(
            string         displayName       = "TestUnit",
            float          maxHp             = 100f,
            float          damage            = 10f,
            float          attackRange       = 8f,
            float          attackCooldown    = 1f,
            float          aggroRadius       = 12f,
            float          moveSpeed         = 5f,
            float          retreatHpFraction = 0.25f,
            bool           retreatDisabled   = false,
            float          retreatCooldown   = 0f,
            string         description       = "",
            int            supplyCost        = 0,
            float          aoeRadius         = 0f,
            TargetPriority targetPriority    = TargetPriority.Units)
        {
            var data                 = CreateInstance<UnitData>();
            data._displayName        = displayName;
            data._description        = description;
            data._supplyCost         = supplyCost;
            data._maxHp              = maxHp;
            data._damage             = damage;
            data._attackRange        = attackRange;
            data._attackCooldown     = attackCooldown;
            data._aggroRadius        = aggroRadius;
            data._moveSpeed          = moveSpeed;
            data._retreatHpFraction  = retreatHpFraction;
            data._retreatDisabled    = retreatDisabled;
            data._retreatCooldown    = retreatCooldown;
            data._aoeRadius          = aoeRadius;
            data._targetPriority     = targetPriority;
            return data;
        }
    }
}
