using UnityEngine;

namespace DiplomaGame.Runtime.Audio
{
    /// <summary>
    /// Чистая статика аудио-логики. Без зависимостей на MonoBehaviour — тестируется в EditMode.
    /// </summary>
    public static class AudioLogic
    {
        /// <summary>
        /// Конвертирует нормализованную громкость [0..1] в децибелы.
        /// 0   → -80 dB (практическая тишина)
        /// 0.5 → -6 dB (−6 дБ)
        /// 1   →   0 dB
        /// </summary>
        public static float VolumeToDb(float v01)
        {
            float safe = Mathf.Max(v01, 0.0001f);
            return Mathf.Log10(safe) * 20f;
        }

        /// <summary>
        /// Возвращает случайный индекс клипа, не совпадающий с предыдущим.
        /// При count == 1 всегда возвращает 0.
        /// </summary>
        public static int PickRandomIndex(int count, int previous)
        {
            if (count <= 1) return 0;

            int idx = Random.Range(0, count - 1);
            if (idx >= previous)
                idx++;
            return idx;
        }
    }
}
