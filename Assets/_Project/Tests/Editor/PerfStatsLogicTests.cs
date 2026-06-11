using System.Collections.Generic;
using DiplomaGame.Runtime.Diagnostics;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты агрегации перф-семплов (харнесс фазы улучшения, ADR-014).
    /// </summary>
    [TestFixture]
    public class PerfStatsLogicTests
    {
        [Test]
        public void Average_TypicalSamples_ReturnsMean()
        {
            var samples = new List<float> { 10f, 20f, 30f };
            Assert.AreEqual(20f, PerfStatsLogic.Average(samples), 1e-5f);
        }

        [Test]
        public void Average_EmptyOrNull_ReturnsZero()
        {
            Assert.AreEqual(0f, PerfStatsLogic.Average(null));
            Assert.AreEqual(0f, PerfStatsLogic.Average(new List<float>()));
        }

        [Test]
        public void Worst_ReturnsMaximum()
        {
            var samples = new List<float> { 16.7f, 33.4f, 8.3f };
            Assert.AreEqual(33.4f, PerfStatsLogic.Worst(samples), 1e-5f);
        }

        [Test]
        public void Percentile_P95Of100UniformSamples_ReturnsRank95()
        {
            // 1..100 мс — p95 методом ближайшего ранга = 95-й элемент
            var samples = new List<float>(100);
            for (int i = 1; i <= 100; i++)
                samples.Add(i);

            Assert.AreEqual(95f, PerfStatsLogic.Percentile(samples, 0.95f), 1e-5f);
        }

        [Test]
        public void Percentile_BoundsClampToMinAndMax()
        {
            var samples = new List<float> { 5f, 1f, 9f };

            Assert.AreEqual(1f, PerfStatsLogic.Percentile(samples, 0f));
            Assert.AreEqual(9f, PerfStatsLogic.Percentile(samples, 1f));
        }

        [Test]
        public void Percentile_Empty_ReturnsZero()
        {
            Assert.AreEqual(0f, PerfStatsLogic.Percentile(new List<float>(), 0.95f));
        }

        [Test]
        public void ToFps_ConvertsFrameTime()
        {
            Assert.AreEqual(60f, PerfStatsLogic.ToFps(1000f / 60f), 1e-3f);
            Assert.AreEqual(0f,  PerfStatsLogic.ToFps(0f));
            Assert.AreEqual(0f,  PerfStatsLogic.ToFps(-5f));
        }
    }
}
