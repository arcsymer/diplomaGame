using System.Collections;
using UnityEngine;

namespace DiplomaGame.Runtime.Buildings
{
    /// <summary>
    /// Воспроизводит scale-in анимацию при появлении здания:
    /// 0 → 1.15 → 1.0 за 0.35 секунды корутиной в OnEnable.
    /// Без аллокаций в кадре (WaitForEndOfFrame не используется — только yield return null).
    /// </summary>
    public sealed class BuildingSpawnEffect : MonoBehaviour
    {
        private const float Duration    = 0.35f;
        private const float OvershootAt = 0.75f; // доля времени до пика (0.75 * 0.35 = 0.2625 сек)
        private const float PeakScale   = 1.15f;
        private const float FinalScale  = 1.0f;

        private Coroutine _scaleCoroutine;

        private void OnEnable()
        {
            // Останавливаем предыдущую анимацию если GO переиспользуется (пул)
            if (_scaleCoroutine != null)
                StopCoroutine(_scaleCoroutine);

            _scaleCoroutine = StartCoroutine(ScaleInRoutine());
        }

        private IEnumerator ScaleInRoutine()
        {
            transform.localScale = Vector3.zero;

            float elapsed = 0f;

            // Фаза 1: 0 → PeakScale за OvershootAt * Duration
            float phase1Duration = OvershootAt * Duration;
            while (elapsed < phase1Duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phase1Duration);
                float s = Mathf.Lerp(0f, PeakScale, t);
                transform.localScale = new Vector3(s, s, s);
                yield return null;
            }

            // Фаза 2: PeakScale → FinalScale за оставшееся время
            float phase2Duration = Duration - phase1Duration;
            float phase2Elapsed  = 0f;
            while (phase2Elapsed < phase2Duration)
            {
                phase2Elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(phase2Elapsed / phase2Duration);
                float s = Mathf.Lerp(PeakScale, FinalScale, t);
                transform.localScale = new Vector3(s, s, s);
                yield return null;
            }

            transform.localScale = new Vector3(FinalScale, FinalScale, FinalScale);
            _scaleCoroutine = null;
        }
    }
}
