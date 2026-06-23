using System;
using UnityEngine;

namespace DiplomaGame.Runtime.Combat
{
    /// <summary>
    /// Компонент здоровья. Реализует IDamageable.
    /// Смерть не уничтожает GO — это делает слушатель (UnitCombat).
    /// </summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        // ----------------------------------------------------------------
        // Статическое событие (M7 Audio шина)
        // ----------------------------------------------------------------

        /// <summary>Вызывается при гибели любого Health в сцене. Параметр — экземпляр.</summary>
        public static event Action<Health> AnyDied;

        /// <summary>Вызывается при получении урона любым Health в сцене. Параметры: экземпляр, amount.</summary>
        public static event Action<Health, float> AnyDamaged;

        /// <summary>
        /// Вызывается при получении урона любым Health в сцене — только когда известна позиция источника.
        /// Параметры: экземпляр, amount, мировая позиция источника урона.
        /// Не вызывается, если урон нанесён через TakeDamage(float) без источника.
        /// </summary>
        public static event Action<Health, float, Vector3> AnyDamagedFrom;
        [SerializeField] private float _maxHp = 100f;

        // ----------------------------------------------------------------
        // Состояние
        // ----------------------------------------------------------------

        private float _currentHp;
        private bool  _isDead;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        public float CurrentHp => _currentHp;
        public float MaxHp     => _maxHp;

        /// <summary>Доля текущего HP от максимального (0..1).</summary>
        public float Fraction  => _maxHp > 0f ? _currentHp / _maxHp : 0f;

        public bool  IsDead    => _isDead;

        // ----------------------------------------------------------------
        // События
        // ----------------------------------------------------------------

        /// <summary>Вызывается при получении урона. Параметры: amount, currentHp.</summary>
        public event Action<float, float> Damaged;

        /// <summary>Вызывается ровно один раз при гибели.</summary>
        public event Action Died;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _currentHp = _maxHp;
        }

        // ----------------------------------------------------------------
        // Internal — инициализация из Unit/UnitCombat и тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Задаёт максимальное HP и восстанавливает текущее до нового максимума.
        /// Вызывать до Awake (если конфигурация из UnitCombat.Awake) не требуется —
        /// Init отдельно сбрасывает состояние.
        /// </summary>
        internal void Init(float maxHp)
        {
            _maxHp     = maxHp;
            _currentHp = maxHp;
            _isDead    = false;
        }

        // ----------------------------------------------------------------
        // IDamageable
        // ----------------------------------------------------------------

        /// <summary>
        /// Восстанавливает HP (не выше максимума). Мёртвых не лечит.
        /// Возвращает фактически восстановленное количество.
        /// </summary>
        public float Heal(float amount)
        {
            float applied = CombatLogic.ClampHeal(amount, _currentHp, _maxHp, _isDead);
            if (applied <= 0f)
                return 0f;

            _currentHp += applied;
            return applied;
        }

        /// <summary>Наносит урон. Повторный урон после смерти игнорируется.</summary>
        public void TakeDamage(float amount)
        {
            ApplyDamage(amount, hasSource: false, sourcePos: default);
        }

        /// <summary>
        /// Наносит урон с указанием мировой позиции источника (атакующего).
        /// Поднимает <see cref="AnyDamaged"/> (как и без источника) и дополнительно
        /// <see cref="AnyDamagedFrom"/> с позицией источника.
        /// </summary>
        public void TakeDamage(float amount, Vector3 sourcePos)
        {
            ApplyDamage(amount, hasSource: true, sourcePos: sourcePos);
        }

        // Единственное место, где меняется состояние HP и поднимаются события.
        // hasSource=false → AnyDamagedFrom не вызывается (нет позиции источника).
        private void ApplyDamage(float amount, bool hasSource, Vector3 sourcePos)
        {
            if (_isDead) return;
            if (amount <= 0f) return;

            _currentHp -= amount;
            if (_currentHp < 0f)
                _currentHp = 0f;

            Damaged?.Invoke(amount, _currentHp);
            AnyDamaged?.Invoke(this, amount);

            if (hasSource)
                AnyDamagedFrom?.Invoke(this, amount, sourcePos);

            if (_currentHp <= 0f)
            {
                _isDead = true;
                Died?.Invoke();
                AnyDied?.Invoke(this);
            }
        }
    }
}
