using UnityEngine;

namespace DiplomaGame.Runtime.GameFeel
{
    /// <summary>
    /// Управляет TrailRenderer для эффекта дэша героя.
    /// При TriggerDash: активирует трейл, сбрасывает его историю, через duration — скрывает.
    /// Таймер ведётся в Update без корутин и аллокаций.
    /// </summary>
    [RequireComponent(typeof(TrailRenderer))]
    public sealed class DashTrailHandler : MonoBehaviour
    {
        private TrailRenderer _trail;
        private float         _timer;
        private float         _duration;
        private bool          _active;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
            if (_trail != null)
                _trail.emitting = false;
        }

        private void Update()
        {
            if (!_active) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer  = 0f;
                _active = false;

                if (_trail != null)
                    _trail.emitting = false;
            }
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>Запускает трейл на duration секунд.</summary>
        public void TriggerDash(float duration)
        {
            if (_trail == null) return;

            _duration = duration;
            _timer    = duration;
            _active   = true;

            _trail.Clear();
            _trail.emitting = true;
        }
    }
}
