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

        /// <summary>Наносит урон. Повторный урон после смерти игнорируется.</summary>
        public void TakeDamage(float amount)
        {
            if (_isDead) return;
            if (amount <= 0f) return;

            _currentHp -= amount;
            if (_currentHp < 0f)
                _currentHp = 0f;

            Damaged?.Invoke(amount, _currentHp);

            if (_currentHp <= 0f)
            {
                _isDead = true;
                Died?.Invoke();
            }
        }
    }
}
