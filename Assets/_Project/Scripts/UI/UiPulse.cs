using System.Collections;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Утилита «пульс» для juice-анимации: scale 1 → 1.1 → 1.
    /// Вешается на элемент, который нужно подёргать при изменении значения.
    /// </summary>
    public sealed class UiPulse : MonoBehaviour
    {
        [SerializeField] private float duration   = 0.25f;
        [SerializeField] private float peakScale  = 1.1f;

        private Vector3    _originalScale;
        private Coroutine  _current;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        public void TriggerPulse()
        {
            if (_current != null)
                StopCoroutine(_current);

            _current = StartCoroutine(PulseRoutine());
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private IEnumerator PulseRoutine()
        {
            float half    = duration * 0.5f;
            float elapsed = 0f;

            // Вверх: 1 → peakScale
            while (elapsed < half)
            {
                float t = elapsed / half;
                transform.localScale = Vector3.LerpUnclamped(_originalScale, _originalScale * peakScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;

            // Вниз: peakScale → 1
            while (elapsed < half)
            {
                float t = elapsed / half;
                transform.localScale = Vector3.LerpUnclamped(_originalScale * peakScale, _originalScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = _originalScale;
            _current = null;
        }
    }
}
