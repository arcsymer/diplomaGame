using System.Collections.Generic;

namespace DiplomaGame.Runtime.Diagnostics
{
    /// <summary>
    /// Чистая статика: агрегация семплов времени кадра в перф-метрики.
    /// Полностью тестируется в EditMode. Семплы — в миллисекундах.
    /// </summary>
    public static class PerfStatsLogic
    {
        /// <summary>Среднее время кадра (мс). 0 — если семплов нет.</summary>
        public static float Average(IReadOnlyList<float> samplesMs)
        {
            if (samplesMs == null || samplesMs.Count == 0)
                return 0f;

            float sum = 0f;
            for (int i = 0; i < samplesMs.Count; i++)
                sum += samplesMs[i];

            return sum / samplesMs.Count;
        }

        /// <summary>Максимальное время кадра (мс) — худший кадр. 0 — если семплов нет.</summary>
        public static float Worst(IReadOnlyList<float> samplesMs)
        {
            if (samplesMs == null || samplesMs.Count == 0)
                return 0f;

            float max = 0f;
            for (int i = 0; i < samplesMs.Count; i++)
            {
                if (samplesMs[i] > max)
                    max = samplesMs[i];
            }

            return max;
        }

        /// <summary>
        /// Перцентиль времени кадра (мс) методом ближайшего ранга.
        /// <paramref name="percentile"/> в долях (0.95 → p95). Сортирует копию — не для горячего пути.
        /// </summary>
        public static float Percentile(IReadOnlyList<float> samplesMs, float percentile)
        {
            if (samplesMs == null || samplesMs.Count == 0)
                return 0f;

            var sorted = new List<float>(samplesMs);
            sorted.Sort();

            if (percentile <= 0f) return sorted[0];
            if (percentile >= 1f) return sorted[sorted.Count - 1];

            int rank = (int)System.Math.Ceiling(percentile * sorted.Count) - 1;
            if (rank < 0) rank = 0;
            if (rank >= sorted.Count) rank = sorted.Count - 1;

            return sorted[rank];
        }

        /// <summary>Перевод времени кадра (мс) в FPS. 0 — при неположительном времени.</summary>
        public static float ToFps(float frameTimeMs)
        {
            return frameTimeMs > 0f ? 1000f / frameTimeMs : 0f;
        }
    }
}
