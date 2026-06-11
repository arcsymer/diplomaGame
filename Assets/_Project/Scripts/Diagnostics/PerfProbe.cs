using System.Collections.Generic;
using UnityEngine;

namespace DiplomaGame.Runtime.Diagnostics
{
    /// <summary>
    /// Накопитель перф-семплов: время каждого кадра (мс) и дельта managed-памяти.
    /// Без аллокаций в Update — буфер преаллоцирован, при переполнении пишет по кругу.
    /// Используется Balance/Stress-тестами и Forge-харнессом для baseline-метрик.
    /// </summary>
    public sealed class PerfProbe : MonoBehaviour
    {
        private const int DefaultCapacity = 4096;

        private readonly List<float> _frameTimesMs = new List<float>(DefaultCapacity);

        private int  _writeIndex;
        private long _gcStartBytes;
        private bool _recording;

        /// <summary>Семплы времени кадра (мс), накопленные с момента StartRecording.</summary>
        public IReadOnlyList<float> FrameTimesMs => _frameTimesMs;

        /// <summary>Прирост managed-памяти с момента StartRecording (байты, может быть отрицательным после GC).</summary>
        public long ManagedDeltaBytes { get; private set; }

        /// <summary>Начинает запись. Сбрасывает прежние семплы.</summary>
        public void StartRecording()
        {
            _frameTimesMs.Clear();
            _writeIndex       = 0;
            _gcStartBytes     = System.GC.GetTotalMemory(false);
            ManagedDeltaBytes = 0;
            _recording        = true;
        }

        /// <summary>Останавливает запись и фиксирует дельту памяти.</summary>
        public void StopRecording()
        {
            if (!_recording) return;

            _recording        = false;
            ManagedDeltaBytes = System.GC.GetTotalMemory(false) - _gcStartBytes;
        }

        /// <summary>Среднее время кадра (мс) за записанный интервал.</summary>
        public float AverageMs => PerfStatsLogic.Average(_frameTimesMs);

        /// <summary>p95 времени кадра (мс) за записанный интервал.</summary>
        public float P95Ms => PerfStatsLogic.Percentile(_frameTimesMs, 0.95f);

        /// <summary>Худший кадр (мс) за записанный интервал.</summary>
        public float WorstMs => PerfStatsLogic.Worst(_frameTimesMs);

        private void Update()
        {
            if (!_recording) return;

            // unscaledDeltaTime — метрики не зависят от timeScale (тесты гоняют ×10)
            float ms = Time.unscaledDeltaTime * 1000f;

            if (_frameTimesMs.Count < DefaultCapacity)
            {
                _frameTimesMs.Add(ms);
            }
            else
            {
                // Кольцевая перезапись без роста списка
                _frameTimesMs[_writeIndex] = ms;
                _writeIndex = (_writeIndex + 1) % DefaultCapacity;
            }
        }
    }
}
