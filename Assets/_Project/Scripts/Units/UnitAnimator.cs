using DiplomaGame.Runtime.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Runtime.Units
{
    /// <summary>
    /// Мост между FSM юнита и компонентом Animator.
    /// Считывает NavMeshAgent.velocity для перехода Idle ↔ Run без аллокаций в Update.
    /// Подписывается на инстанс-событие UnitCombat.Attacked и Health.Died своего юнита.
    /// Null-safe: если Animator не задан — молчит, не бросает исключений.
    /// </summary>
    [RequireComponent(typeof(Unit))]
    public sealed class UnitAnimator : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Константы параметров аниматора
        // ----------------------------------------------------------------

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackHash   = Animator.StringToHash("Attack");
        private static readonly int DieHash      = Animator.StringToHash("Die");

        // Порог sqrMagnitude скорости агента для перехода Idle → Run.
        // Выбран 0.01 (скорость ~0.1 м/с) — согласован с Unit.StuckVelocitySqr.
        private const float MovingVelocitySqrThreshold = 0.01f;

        // ----------------------------------------------------------------
        // Кэшированные ссылки
        // ----------------------------------------------------------------

        private Animator     _animator;
        private NavMeshAgent _agent;
        private UnitCombat   _combat;
        private Health       _health;

        // ----------------------------------------------------------------
        // Состояние
        // ----------------------------------------------------------------

        private bool _wasMoving;
        private bool _isDead;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _agent  = GetComponent<NavMeshAgent>();
            _combat = GetComponent<UnitCombat>();
            _health = GetComponent<Health>();

            // Animator — обязателен на child Visual; ищем в дочерних тоже
            _animator = GetComponentInChildren<Animator>(includeInactive: true);
        }

        private void OnEnable()
        {
            if (_combat != null)
                _combat.Attacked += OnAttacked;

            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_combat != null)
                _combat.Attacked -= OnAttacked;

            if (_health != null)
                _health.Died -= OnDied;
        }

        private void Update()
        {
            if (_animator == null || _isDead) return;
            if (_agent == null) return;

            bool isMoving = _agent.velocity.sqrMagnitude > MovingVelocitySqrThreshold;
            if (isMoving == _wasMoving) return;

            _wasMoving = isMoving;
            _animator.SetBool(IsMovingHash, isMoving);
        }

        // ----------------------------------------------------------------
        // Обработчики событий
        // ----------------------------------------------------------------

        private void OnAttacked()
        {
            if (_animator == null || _isDead) return;
            _animator.SetTrigger(AttackHash);
        }

        private void OnDied()
        {
            _isDead = true;
            if (_animator == null) return;
            _animator.SetTrigger(DieHash);
        }
    }
}
