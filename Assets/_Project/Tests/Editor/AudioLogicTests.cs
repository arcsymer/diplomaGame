using DiplomaGame.Runtime.Audio;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для AudioLogic — чистая статика, без MonoBehaviour.
    /// </summary>
    [TestFixture]
    public class AudioLogicTests
    {
        // ================================================================
        // VolumeToDb
        // ================================================================

        [Test]
        public void VolumeToDb_One_ReturnsZero()
        {
            float db = AudioLogic.VolumeToDb(1f);
            Assert.AreEqual(0f, db, 0.001f, "1.0 → 0 dB");
        }

        [Test]
        public void VolumeToDb_Zero_ReturnsApproxMinus80()
        {
            // VolumeToDb зажимает v в 0.0001 → log10(0.0001)*20 = -80 dB
            float db = AudioLogic.VolumeToDb(0f);
            Assert.AreEqual(-80f, db, 0.1f, "0.0 → ~-80 dB");
        }

        [Test]
        public void VolumeToDb_Half_ReturnsApproxMinus6()
        {
            // log10(0.5) * 20 ≈ -6.02 dB
            float db = AudioLogic.VolumeToDb(0.5f);
            Assert.AreEqual(-6.02f, db, 0.05f, "0.5 → ~-6 dB");
        }

        [Test]
        public void VolumeToDb_NegativeInput_TreatedAsZero()
        {
            // Отрицательные значения зажимаются как 0 → -80 dB
            float db = AudioLogic.VolumeToDb(-1f);
            Assert.AreEqual(-80f, db, 0.1f, "Negative input clamped to 0.0001 → -80 dB");
        }

        [Test]
        public void VolumeToDb_Tenth_ReturnsApproxMinus20()
        {
            // log10(0.1) * 20 = -20 dB
            float db = AudioLogic.VolumeToDb(0.1f);
            Assert.AreEqual(-20f, db, 0.01f, "0.1 → -20 dB");
        }

        [Test]
        public void VolumeToDb_Hundredth_ReturnsApproxMinus40()
        {
            // log10(0.01) * 20 = -40 dB
            float db = AudioLogic.VolumeToDb(0.01f);
            Assert.AreEqual(-40f, db, 0.01f, "0.01 → -40 dB");
        }

        // ================================================================
        // PickRandomIndex
        // ================================================================

        [Test]
        public void PickRandomIndex_CountOne_ReturnsZero()
        {
            int result = AudioLogic.PickRandomIndex(1, 0);
            Assert.AreEqual(0, result, "Единственный клип всегда 0");
        }

        [Test]
        public void PickRandomIndex_CountZero_ReturnsZero()
        {
            // Защита от count<=0: возвращает 0 без Random
            int result = AudioLogic.PickRandomIndex(0, 0);
            Assert.AreEqual(0, result, "count=0 → 0 (защита от исключений)");
        }

        [Test]
        public void PickRandomIndex_NeverReturnsPrevious_ManyTrials()
        {
            const int count   = 5;
            const int trials  = 200;
            const int prev    = 2;

            for (int i = 0; i < trials; i++)
            {
                int idx = AudioLogic.PickRandomIndex(count, prev);
                Assert.AreNotEqual(prev, idx,
                    $"Итерация {i}: PickRandomIndex не должен вернуть prev={prev}");
            }
        }

        [Test]
        public void PickRandomIndex_ReturnsInRange()
        {
            const int count  = 4;
            const int trials = 100;

            for (int i = 0; i < trials; i++)
            {
                int idx = AudioLogic.PickRandomIndex(count, 0);
                Assert.GreaterOrEqual(idx, 0,    $"idx >= 0 (trial {i})");
                Assert.Less(idx,         count,  $"idx < count (trial {i})");
            }
        }

        [Test]
        public void PickRandomIndex_CountTwo_AlwaysReturnsOther()
        {
            // Если count=2 и prev=0 — всегда должно вернуться 1, и наоборот
            for (int i = 0; i < 50; i++)
            {
                Assert.AreEqual(1, AudioLogic.PickRandomIndex(2, 0), "prev=0 → 1");
                Assert.AreEqual(0, AudioLogic.PickRandomIndex(2, 1), "prev=1 → 0");
            }
        }

        [Test]
        public void PickRandomIndex_PreviousOutOfRange_ReturnsInRange()
        {
            // prev вне диапазона [0, count) — не должно бросать исключение
            Assert.DoesNotThrow(() =>
            {
                int idx = AudioLogic.PickRandomIndex(3, 99);
                Assert.GreaterOrEqual(idx, 0);
                Assert.Less(idx, 3);
            });
        }
    }
}
