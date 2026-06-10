using DiplomaGame.Runtime.VFX;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для VfxLogic — чистая статика, без MonoBehaviour.
    /// </summary>
    [TestFixture]
    public class VfxLogicTests
    {
        // ================================================================
        // NextPoolIndex — базовые случаи
        // ================================================================

        [Test]
        public void NextPoolIndex_ZeroToSize_ReturnsOne()
        {
            // current=-1 (ещё не запускался) → должен вернуть 0
            int result = VfxLogic.NextPoolIndex(-1, 6);
            Assert.AreEqual(0, result, "(-1 + 1) % 6 == 0");
        }

        [Test]
        public void NextPoolIndex_Middle_ReturnsNext()
        {
            int result = VfxLogic.NextPoolIndex(2, 6);
            Assert.AreEqual(3, result, "(2 + 1) % 6 == 3");
        }

        [Test]
        public void NextPoolIndex_LastIndex_WrapsToZero()
        {
            // Последний индекс пула: должен обернуться на 0
            int result = VfxLogic.NextPoolIndex(5, 6);
            Assert.AreEqual(0, result, "(5 + 1) % 6 == 0");
        }

        [Test]
        public void NextPoolIndex_SizeOne_AlwaysReturnsZero()
        {
            int result = VfxLogic.NextPoolIndex(0, 1);
            Assert.AreEqual(0, result, "(0 + 1) % 1 == 0");
        }

        [Test]
        public void NextPoolIndex_SizeZero_ReturnsZero()
        {
            // Защита: size<=0 → всегда 0
            int result = VfxLogic.NextPoolIndex(3, 0);
            Assert.AreEqual(0, result, "size=0 → защитный возврат 0");
        }

        [Test]
        public void NextPoolIndex_NegativeSize_ReturnsZero()
        {
            int result = VfxLogic.NextPoolIndex(2, -5);
            Assert.AreEqual(0, result, "size<0 → защитный возврат 0");
        }

        // ================================================================
        // NextPoolIndex — полный цикл пула
        // ================================================================

        [Test]
        public void NextPoolIndex_FullCycle_ReturnsToStart()
        {
            const int size = 6;
            int idx = -1;

            // size шагов от -1 покрывают индексы 0..size-1
            for (int i = 0; i < size; i++)
                idx = VfxLogic.NextPoolIndex(idx, size);
            Assert.AreEqual(size - 1, idx, "После size шагов индекс — последний слот");

            // следующий шаг замыкает круг
            idx = VfxLogic.NextPoolIndex(idx, size);
            Assert.AreEqual(0, idx, "Шаг size+1 должен вернуть индекс к 0");
        }

        [Test]
        public void NextPoolIndex_TwoCycles_Consistent()
        {
            // Два полных цикла — последовательность должна быть одинаковой
            const int size = 4;
            int idx = -1;

            var firstCycle  = new int[size];
            var secondCycle = new int[size];

            for (int i = 0; i < size; i++)
            {
                idx = VfxLogic.NextPoolIndex(idx, size);
                firstCycle[i] = idx;
            }

            for (int i = 0; i < size; i++)
            {
                idx = VfxLogic.NextPoolIndex(idx, size);
                secondCycle[i] = idx;
            }

            Assert.AreEqual(firstCycle, secondCycle,
                "Два полных цикла должны давать одинаковую последовательность индексов");
        }

        // ================================================================
        // NextPoolIndex — граничные значения current
        // ================================================================

        [Test]
        public void NextPoolIndex_CurrentEqualsSize_WrapsCorrectly()
        {
            // current == size (выход за диапазон) — модуль должен отработать корректно
            int result = VfxLogic.NextPoolIndex(6, 6);
            Assert.AreEqual(1, result, "(6 + 1) % 6 == 1");
        }

        [Test]
        public void NextPoolIndex_SizeTwo_AlternatesZeroOne()
        {
            int idx = -1;

            idx = VfxLogic.NextPoolIndex(idx, 2);
            Assert.AreEqual(0, idx, "Первый шаг: 0");

            idx = VfxLogic.NextPoolIndex(idx, 2);
            Assert.AreEqual(1, idx, "Второй шаг: 1");

            idx = VfxLogic.NextPoolIndex(idx, 2);
            Assert.AreEqual(0, idx, "Третий шаг: снова 0");
        }
    }
}
