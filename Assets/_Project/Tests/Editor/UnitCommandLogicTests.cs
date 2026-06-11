using NUnit.Framework;
using DiplomaGame.Runtime.Units;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для UnitCommandLogic.
    /// Не требуют сцены — вся логика статическая.
    /// </summary>
    [TestFixture]
    public class UnitCommandLogicTests
    {
        // ----------------------------------------------------------------
        // GetStateForCommand
        // ----------------------------------------------------------------

        [Test]
        public void GetStateForCommand_Move_ReturnsMoving()
        {
            Assert.AreEqual(UnitState.Moving, UnitCommandLogic.GetStateForCommand(UnitCommandType.Move));
        }

        [Test]
        public void GetStateForCommand_AttackMove_ReturnsMoving()
        {
            // До M4 AttackMove ведёт себя как Move
            Assert.AreEqual(UnitState.Moving, UnitCommandLogic.GetStateForCommand(UnitCommandType.AttackMove));
        }

        [Test]
        public void GetStateForCommand_Hold_ReturnsHolding()
        {
            Assert.AreEqual(UnitState.Holding, UnitCommandLogic.GetStateForCommand(UnitCommandType.Hold));
        }

        [Test]
        public void GetStateForCommand_Patrol_ReturnsPatrolling()
        {
            Assert.AreEqual(UnitState.Patrolling, UnitCommandLogic.GetStateForCommand(UnitCommandType.Patrol));
        }

        // ----------------------------------------------------------------
        // GetNextPatrolPoint
        // ----------------------------------------------------------------

        [Test]
        public void GetNextPatrolPoint_NearA_ReturnsB()
        {
            var a       = new Vector3(0f, 0f, 0f);
            var b       = new Vector3(10f, 0f, 0f);
            var current = new Vector3(0.1f, 0f, 0f); // почти у A

            var next = UnitCommandLogic.GetNextPatrolPoint(current, a, b);

            Assert.AreEqual(b, next, "Если рядом с A — следующая точка должна быть B.");
        }

        [Test]
        public void GetNextPatrolPoint_NearB_ReturnsA()
        {
            var a       = new Vector3(0f,  0f, 0f);
            var b       = new Vector3(10f, 0f, 0f);
            var current = new Vector3(9.9f, 0f, 0f); // почти у B

            var next = UnitCommandLogic.GetNextPatrolPoint(current, a, b);

            Assert.AreEqual(a, next, "Если рядом с B — следующая точка должна быть A.");
        }

        [Test]
        public void GetNextPatrolPoint_AtMidpoint_ReturnsEitherAorB()
        {
            // Посередине — алгоритм выбирает один из двух вариантов;
            // главное, что результат строго равен A или B (не промежуточному).
            var a       = new Vector3(0f, 0f, 0f);
            var b       = new Vector3(10f, 0f, 0f);
            var current = new Vector3(5f, 0f, 0f);

            var next = UnitCommandLogic.GetNextPatrolPoint(current, a, b);

            Assert.IsTrue(next == a || next == b,
                "Из средней точки следующей должна быть A или B.");
        }

        // ----------------------------------------------------------------
        // HasArrived
        // ----------------------------------------------------------------

        [Test]
        public void HasArrived_PathPending_ReturnsFalse()
        {
            // Пока путь вычисляется — не считаем прибывшим
            Assert.IsFalse(UnitCommandLogic.HasArrived(0f, 0.1f, pathPending: true),
                "pathPending=true → HasArrived должен вернуть false.");
        }

        [Test]
        public void HasArrived_PathPendingFalse_CloseEnough_ReturnsTrue()
        {
            // remainingDistance <= stoppingDistance + epsilon
            Assert.IsTrue(UnitCommandLogic.HasArrived(0.05f, 0.1f, pathPending: false),
                "remainingDistance < stoppingDistance → прибыли.");
        }

        [Test]
        public void HasArrived_PathPendingFalse_TooFar_ReturnsFalse()
        {
            Assert.IsFalse(UnitCommandLogic.HasArrived(5f, 0.1f, pathPending: false),
                "remainingDistance >> stoppingDistance → ещё не прибыли.");
        }

        [Test]
        public void HasArrived_ExactlyAtStoppingPlusEpsilon_ReturnsTrue()
        {
            // На границе: remaining = stopping + 0.01f → должны быть прибывшими
            Assert.IsTrue(UnitCommandLogic.HasArrived(0.11f, 0.1f, pathPending: false),
                "remaining = stopping + epsilon → прибыли.");
        }

        [Test]
        public void HasArrived_JustOverBoundary_ReturnsFalse()
        {
            // remaining = stopping + 0.02f → чуть дальше границы
            Assert.IsFalse(UnitCommandLogic.HasArrived(0.12f, 0.1f, pathPending: false),
                "remaining = stopping + 0.02f → ещё не прибыли.");
        }

        // ----------------------------------------------------------------
        // GetFormationOffset
        // ----------------------------------------------------------------

        [Test]
        public void GetFormationOffset_Index0_ReturnsZero()
        {
            var offset = UnitCommandLogic.GetFormationOffset(0);
            Assert.AreEqual(Vector3.zero, offset,
                "Индекс 0 должен давать нулевое смещение (центр формации).");
        }

        [Test]
        public void GetFormationOffset_DifferentIndices_ProduceDifferentOffsets()
        {
            var o1 = UnitCommandLogic.GetFormationOffset(1);
            var o2 = UnitCommandLogic.GetFormationOffset(2);
            var o3 = UnitCommandLogic.GetFormationOffset(3);

            Assert.AreNotEqual(o1, o2, "Индексы 1 и 2 должны давать разные смещения.");
            Assert.AreNotEqual(o2, o3, "Индексы 2 и 3 должны давать разные смещения.");
            Assert.AreNotEqual(o1, o3, "Индексы 1 и 3 должны давать разные смещения.");
        }

        [Test]
        public void GetFormationOffset_NonZeroIndices_YComponentIsZero()
        {
            // Формация — плоская (XZ), Y должен быть 0.
            for (int i = 1; i <= 8; i++)
            {
                var offset = UnitCommandLogic.GetFormationOffset(i);
                Assert.AreEqual(0f, offset.y, 0.001f,
                    $"Индекс {i}: Y-компонента смещения должна быть 0.");
            }
        }

        [Test]
        public void GetFormationOffset_Ring1_HasCorrectRadius()
        {
            // Кольцо 1 — радиус равен spacing = 1.5f (явно передан для обратной совместимости теста)
            const float spacing  = 1.5f;
            const float expected = 1 * spacing; // ring=1

            for (int i = 1; i <= 8; i++)
            {
                var offset = UnitCommandLogic.GetFormationOffset(i, spacing);
                float radius = new Vector2(offset.x, offset.z).magnitude;
                Assert.AreEqual(expected, radius, 0.001f,
                    $"Индекс {i}: радиус первого кольца должен быть {expected}.");
            }
        }

        // ----------------------------------------------------------------
        // v3 crowd avoidance: проверка нового дефолтного spacing = 2.0
        // ----------------------------------------------------------------

        [Test]
        public void GetFormationOffset_DefaultSpacing_IsTwo()
        {
            // Дефолтный spacing изменён на 2.0 в v3 (crowd avoidance ADR-018).
            // Индекс 1 — первое кольцо, радиус должен равняться 2.0f.
            var offset = UnitCommandLogic.GetFormationOffset(1);
            float radius = new Vector2(offset.x, offset.z).magnitude;
            Assert.AreEqual(2.0f, radius, 0.001f,
                "Дефолтный spacing = 2.0: радиус первого кольца должен быть 2.0.");
        }

        [Test]
        public void GetFormationOffset_Ring1_DefaultSpacing_AllUnitsHaveRadius2()
        {
            // Все 8 юнитов первого кольца при spacing = 2.0 (дефолт) имеют радиус 2.0.
            const float expected = 2.0f;
            for (int i = 1; i <= 8; i++)
            {
                var offset = UnitCommandLogic.GetFormationOffset(i);
                float radius = new Vector2(offset.x, offset.z).magnitude;
                Assert.AreEqual(expected, radius, 0.001f,
                    $"Индекс {i}: радиус первого кольца при spacing=2.0 должен быть {expected}.");
            }
        }
    }
}
