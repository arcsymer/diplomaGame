using UnityEngine;

namespace DiplomaGame.Runtime.Data
{
    /// <summary>
    /// ScriptableObject с боевыми и ходовыми характеристиками юнита.
    /// Один ассет — один тип юнита. Нет хардкода.
    /// </summary>
    [CreateAssetMenu(menuName = "DiplomaGame/Unit Data", fileName = "NewUnitData")]
    public sealed class UnitData : ScriptableObject
    {
        [SerializeField] private string _displayName      = "Unit";
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

        // ----------------------------------------------------------------
        // Публичный API (read-only)
        // ----------------------------------------------------------------

        public string DisplayName       => _displayName;
        public float  MaxHp             => _maxHp;
        public float  Damage            => _damage;
        public float  AttackRange       => _attackRange;
        public float  AttackCooldown    => _attackCooldown;
        public float  AggroRadius       => _aggroRadius;
        public float  MoveSpeed         => _moveSpeed;
        public float  RetreatHpFraction => _retreatHpFraction;
        public bool   RetreatDisabled   => _retreatDisabled;

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов (без SerializedObject)
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт UnitData с произвольными значениями — без SerializedObject (доступно в PlayMode).
        /// </summary>
        internal static UnitData CreateForTest(
            string displayName      = "TestUnit",
            float  maxHp            = 100f,
            float  damage           = 10f,
            float  attackRange      = 8f,
            float  attackCooldown   = 1f,
            float  aggroRadius      = 12f,
            float  moveSpeed        = 5f,
            float  retreatHpFraction = 0.25f,
            bool   retreatDisabled  = false)
        {
            var data                 = CreateInstance<UnitData>();
            data._displayName        = displayName;
            data._maxHp              = maxHp;
            data._damage             = damage;
            data._attackRange        = attackRange;
            data._attackCooldown     = attackCooldown;
            data._aggroRadius        = aggroRadius;
            data._moveSpeed          = moveSpeed;
            data._retreatHpFraction  = retreatHpFraction;
            data._retreatDisabled    = retreatDisabled;
            return data;
        }
    }
}
