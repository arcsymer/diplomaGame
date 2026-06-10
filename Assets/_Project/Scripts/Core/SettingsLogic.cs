namespace DiplomaGame.Runtime.Core
{
    /// <summary>
    /// Чистая статика для вычислений настроек.
    /// Не зависит от MonoBehaviour — тестируется через EditMode.
    /// </summary>
    public static class SettingsLogic
    {
        /// <summary>
        /// Зажимает индекс уровня качества в диапазон [0, count-1].
        /// При count &lt;= 0 возвращает 0.
        /// </summary>
        public static int ClampQuality(int level, int count)
        {
            if (count <= 0) return 0;
            if (level < 0)       return 0;
            if (level >= count)  return count - 1;
            return level;
        }

        /// <summary>Зажимает громкость в диапазон [0, 1].</summary>
        public static float ClampVolume01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        /// <summary>Зажимает чувствительность мыши в диапазон [0.01, 1].</summary>
        public static float ClampSensitivity(float s)
        {
            if (s < 0.01f) return 0.01f;
            if (s > 1f)    return 1f;
            return s;
        }
    }
}
