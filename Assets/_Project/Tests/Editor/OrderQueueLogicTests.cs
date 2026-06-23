using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using DiplomaGame.Runtime.Units;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для OrderQueueLogic и логики очереди приказов в Unit (M15).
    /// Не требуют сцены — OrderQueueLogic чисто статическая, Unit тестируется через InitForTest.
    /// </summary>
    [TestFixture]
    public class OrderQueueLogicTests
    {
        // ----------------------------------------------------------------
        // SetUp / TearDown (NavMesh-сообщения игнорируются в EditMode-тестах)
        // ----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            // NavMeshAgent.SetDestination/ResetPath выводят Error, если нет NavMesh-поверхности.
            // В EditMode это ожидаемо — тестируем только логику очереди, не навигацию.
            LogAssert.ignoreFailingMessages = true;
        }

        // ----------------------------------------------------------------
        // CanEnqueue
        // ----------------------------------------------------------------

        [Test]
        public void CanEnqueue_Move_ReturnsTrue()
        {
            Assert.IsTrue(OrderQueueLogic.CanEnqueue(UnitCommandType.Move),
                "Move должен допускать постановку в очередь.");
        }

        [Test]
        public void CanEnqueue_AttackMove_ReturnsTrue()
        {
            Assert.IsTrue(OrderQueueLogic.CanEnqueue(UnitCommandType.AttackMove),
                "AttackMove должен допускать постановку в очередь.");
        }

        [Test]
        public void CanEnqueue_Hold_ReturnsFalse()
        {
            Assert.IsFalse(OrderQueueLogic.CanEnqueue(UnitCommandType.Hold),
                "Hold не допускает очередь — немедленный приказ.");
        }

        [Test]
        public void CanEnqueue_Patrol_ReturnsFalse()
        {
            Assert.IsFalse(OrderQueueLogic.CanEnqueue(UnitCommandType.Patrol),
                "Patrol не допускает очередь — бесконечный цикл, не завершается.");
        }

        // ----------------------------------------------------------------
        // Enqueue / Count
        // ----------------------------------------------------------------

        [Test]
        public void Enqueue_AddsToQueue_CountIncreases()
        {
            var queue = new Queue<UnitCommand>(8);
            var cmd   = UnitCommand.Move(new Vector3(1f, 0f, 0f));

            OrderQueueLogic.Enqueue(queue, cmd);

            Assert.AreEqual(1, OrderQueueLogic.Count(queue),
                "После Enqueue Count должен стать 1.");
        }

        [Test]
        public void Enqueue_TwoCommands_CountIsTwo()
        {
            var queue = new Queue<UnitCommand>(8);

            OrderQueueLogic.Enqueue(queue, UnitCommand.Move(new Vector3(1f, 0f, 0f)));
            OrderQueueLogic.Enqueue(queue, UnitCommand.Move(new Vector3(2f, 0f, 0f)));

            Assert.AreEqual(2, OrderQueueLogic.Count(queue),
                "После двух Enqueue Count должен быть 2.");
        }

        // ----------------------------------------------------------------
        // TryDequeueNext — FIFO-порядок
        // ----------------------------------------------------------------

        [Test]
        public void TryDequeueNext_FIFO_Order()
        {
            var queue = new Queue<UnitCommand>(8);
            var pt1   = new Vector3(1f, 0f, 0f);
            var pt2   = new Vector3(2f, 0f, 0f);

            OrderQueueLogic.Enqueue(queue, UnitCommand.Move(pt1));
            OrderQueueLogic.Enqueue(queue, UnitCommand.Move(pt2));

            bool got1 = OrderQueueLogic.TryDequeueNext(queue, out UnitCommand first);
            bool got2 = OrderQueueLogic.TryDequeueNext(queue, out UnitCommand second);

            Assert.IsTrue(got1, "Первый Dequeue должен вернуть true.");
            Assert.IsTrue(got2, "Второй Dequeue должен вернуть true.");
            Assert.AreEqual(pt1, first.TargetPoint,  "Первый должен быть pt1 (FIFO).");
            Assert.AreEqual(pt2, second.TargetPoint, "Второй должен быть pt2 (FIFO).");
        }

        [Test]
        public void TryDequeueNext_EmptyQueue_ReturnsFalse()
        {
            var queue = new Queue<UnitCommand>(8);

            bool result = OrderQueueLogic.TryDequeueNext(queue, out _);

            Assert.IsFalse(result, "Пустая очередь должна вернуть false.");
        }

        [Test]
        public void TryDequeueNext_AfterLastItem_CountIsZero()
        {
            var queue = new Queue<UnitCommand>(8);
            OrderQueueLogic.Enqueue(queue, UnitCommand.Move(Vector3.one));

            OrderQueueLogic.TryDequeueNext(queue, out _);

            Assert.AreEqual(0, OrderQueueLogic.Count(queue),
                "После извлечения последнего Count должен стать 0.");
        }

        // ----------------------------------------------------------------
        // Clear
        // ----------------------------------------------------------------

        [Test]
        public void Clear_RemovesAllItems()
        {
            var queue = new Queue<UnitCommand>(8);
            OrderQueueLogic.Enqueue(queue, UnitCommand.Move(Vector3.one));
            OrderQueueLogic.Enqueue(queue, UnitCommand.Move(Vector3.forward));

            OrderQueueLogic.Clear(queue);

            Assert.AreEqual(0, OrderQueueLogic.Count(queue),
                "После Clear очередь должна быть пустой.");
        }

        // ----------------------------------------------------------------
        // Unit.EnqueueCommand — тесты через InitForTest
        // ----------------------------------------------------------------

        // ВАЖНО: Unit — MonoBehaviour. В EditMode мы создаём GO через new GameObject()
        // и AddComponent, но NavMeshAgent нужен сцене. Мы тестируем очередь через
        // internal-методы: InitForTest (выделяет очередь), SetMovingForTest (эмулирует
        // состояние Moving), SimulateArrivalForTest (вызывает AdvanceOrderQueue).
        // NavMeshAgent.SetDestination/ResetPath выводят [Error] без NavMesh-поверхности.
        // Используем LogAssert.Expect, чтобы зарегистрировать ожидаемое сообщение.

        // Вспомогательные методы: регистрация ожидаемых NavMesh-ошибок
        private static void ExpectNavMeshSetDestination()
            => LogAssert.Expect(LogType.Error,
                "\"SetDestination\" can only be called on an active agent that has been placed on a NavMesh.");

        private static void ExpectNavMeshResetPath()
            => LogAssert.Expect(LogType.Error,
                "\"ResetPath\" can only be called on an active agent that has been placed on a NavMesh.");

        private static Unit CreateTestUnit()
        {
            // ignoreFailingMessages, выставленный в [SetUp], не действует: UTF 1.6 создаёт
            // LogScope теста ПОСЛЕ SetUp и сбрасывает флаг. Ставим в теле теста (CreateTestUnit —
            // первая строка каждого Unit-теста), чтобы NavMesh-ошибки SetDestination/ResetPath
            // (агент не на NavMesh в EditMode) не валили тест. Логику ловят Assert'ы ниже.
            LogAssert.ignoreFailingMessages = true;

            var go = new GameObject("TestUnit");
            // NavMeshAgent должен быть добавлен до Unit, чтобы GetComponent в Awake нашёл его.
            go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            var unit = go.AddComponent<Unit>();
            // В EditMode Awake() не вызывается автоматически при AddComponent.
            // Вызываем вручную через рефлексию, чтобы _agent был инициализирован.
            var awake = typeof(Unit).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            awake?.Invoke(unit, null);
            unit.InitForTest();
            return unit;
        }

        [Test]
        public void EnqueueCommand_WhenIdle_ExecutesImmediately_QueueRemainsEmpty()
        {
            // Юнит в Idle: первый EnqueueCommand должен стартовать движение немедленно,
            // не добавляя ничего в очередь.
            var unit = CreateTestUnit();
            // Не вызываем SetMovingForTest → юнит в Idle (дефолт).

            // EnqueueCommand вызовет ExecuteCommand напрямую (не добавит в очередь).
            // NavMeshAgent.SetDestination кинет предупреждение (нет NavMesh), но не упадёт.
            unit.EnqueueCommand(UnitCommand.Move(new Vector3(5f, 0f, 0f)));

            Assert.AreEqual(0, unit.OrderQueueCount,
                "Первый EnqueueCommand при Idle не должен добавлять в очередь — выполняется немедленно.");
        }

        [Test]
        public void EnqueueCommand_WhenMoving_AddsToQueue_DoesNotClearCurrentCommand()
        {
            // Юнит в Moving: EnqueueCommand добавляет в очередь.
            var unit = CreateTestUnit();
            unit.SetMovingForTest(); // эмулируем: юнит уже движется

            unit.EnqueueCommand(UnitCommand.Move(new Vector3(10f, 0f, 0f)));

            Assert.AreEqual(1, unit.OrderQueueCount,
                "EnqueueCommand при Moving должен добавить приказ в очередь.");
        }

        [Test]
        public void EnqueueCommand_WhenMoving_TwoCommands_QueueCountIsTwo()
        {
            var unit = CreateTestUnit();
            unit.SetMovingForTest();

            unit.EnqueueCommand(UnitCommand.Move(new Vector3(10f, 0f, 0f)));
            unit.EnqueueCommand(UnitCommand.Move(new Vector3(20f, 0f, 0f)));

            Assert.AreEqual(2, unit.OrderQueueCount,
                "Два EnqueueCommand должны дать Count=2 в очереди.");
        }

        [Test]
        public void IssueCommand_ClearsQueue()
        {
            // IssueCommand (без Shift) должен очищать очередь и выполняться немедленно.
            var unit = CreateTestUnit();
            unit.SetMovingForTest();

            unit.EnqueueCommand(UnitCommand.Move(new Vector3(10f, 0f, 0f)));
            unit.EnqueueCommand(UnitCommand.Move(new Vector3(20f, 0f, 0f)));
            Assert.AreEqual(2, unit.OrderQueueCount, "Предусловие: два приказа в очереди.");

            unit.IssueCommand(UnitCommand.Move(new Vector3(0f, 0f, 5f)));

            Assert.AreEqual(0, unit.OrderQueueCount,
                "IssueCommand должен очистить очередь (обычный приказ без Shift).");
        }

        [Test]
        public void SimulateArrival_WithQueuedCommand_AdvancesToNext()
        {
            // Эмулируем прибытие: очередь с одним элементом → должен выполниться.
            var unit = CreateTestUnit();
            unit.SetMovingForTest();

            var nextPoint = new Vector3(99f, 0f, 0f);
            unit.EnqueueCommand(UnitCommand.Move(nextPoint));
            Assert.AreEqual(1, unit.OrderQueueCount, "Предусловие: один приказ в очереди.");

            unit.SimulateArrivalForTest();

            Assert.AreEqual(0, unit.OrderQueueCount,
                "После прибытия очередь должна стать пустой (следующий приказ запущен).");
            Assert.AreEqual(UnitCommandType.Move, unit.CurrentCommandType,
                "CurrentCommandType должен отражать выполняемый приказ из очереди.");
        }

        [Test]
        public void SimulateArrival_EmptyQueue_TransitionsToIdle()
        {
            // Прибытие при пустой очереди → Idle.
            var unit = CreateTestUnit();
            unit.SetMovingForTest();
            // Очередь пустая.

            unit.SimulateArrivalForTest();

            Assert.AreEqual(0, unit.OrderQueueCount, "Очередь должна оставаться пустой.");
            Assert.AreEqual(UnitState.Idle, unit.CurrentState,
                "После прибытия с пустой очередью юнит должен перейти в Idle.");
        }

        [Test]
        public void SimulateArrival_TwoQueuedCommands_FIFO()
        {
            // Два приказа в очереди → прибытие дважды → FIFO-порядок.
            var unit = CreateTestUnit();
            unit.SetMovingForTest();

            var pt1 = new Vector3(10f, 0f, 0f);
            var pt2 = new Vector3(20f, 0f, 0f);
            unit.EnqueueCommand(UnitCommand.Move(pt1));
            unit.EnqueueCommand(UnitCommand.Move(pt2));

            // Первое прибытие — извлекаем pt1
            unit.SimulateArrivalForTest();
            Assert.AreEqual(1, unit.OrderQueueCount, "После первого прибытия: 1 приказ остался.");
            Assert.AreEqual(UnitCommandType.Move, unit.CurrentCommandType);

            // Второе прибытие — извлекаем pt2
            unit.SimulateArrivalForTest();
            Assert.AreEqual(0, unit.OrderQueueCount, "После второго прибытия: очередь пуста.");
        }

        [Test]
        public void EnqueueCommand_Hold_ClearsQueueAndExecutesImmediately()
        {
            // Hold — не поддерживает очередь.
            // EnqueueCommand(Hold) должен вызвать IssueCommand внутри → очередь очистится.
            var unit = CreateTestUnit();
            unit.SetMovingForTest();

            unit.EnqueueCommand(UnitCommand.Move(new Vector3(10f, 0f, 0f)));
            unit.EnqueueCommand(UnitCommand.Move(new Vector3(20f, 0f, 0f)));
            Assert.AreEqual(2, unit.OrderQueueCount, "Предусловие: два приказа в очереди.");

            unit.EnqueueCommand(UnitCommand.Hold());

            Assert.AreEqual(0, unit.OrderQueueCount,
                "EnqueueCommand(Hold) должен сбросить очередь (Hold — немедленный приказ).");
            Assert.AreEqual(UnitCommandType.Hold, unit.CurrentCommandType,
                "CurrentCommandType должен стать Hold после немедленного выполнения.");
        }

        [Test]
        public void EnqueueCommand_Patrol_ClearsQueueAndExecutesImmediately()
        {
            // Patrol — не поддерживает очередь (бесконечный цикл, никогда не завершится).
            // Shift+P должен вести себя как обычный Patrol (очередь сбрасывается).
            var unit = CreateTestUnit();
            unit.SetMovingForTest();

            unit.EnqueueCommand(UnitCommand.Move(new Vector3(10f, 0f, 0f)));
            Assert.AreEqual(1, unit.OrderQueueCount, "Предусловие: один приказ в очереди.");

            unit.EnqueueCommand(UnitCommand.Patrol(new Vector3(30f, 0f, 0f)));

            Assert.AreEqual(0, unit.OrderQueueCount,
                "EnqueueCommand(Patrol) должен сбросить очередь — Patrol не встаёт в очередь.");
            Assert.AreEqual(UnitCommandType.Patrol, unit.CurrentCommandType,
                "CurrentCommandType должен стать Patrol после немедленного выполнения.");
        }

        [Test]
        public void EnqueueCommand_AttackMove_WhenMoving_AddsToQueue()
        {
            // AttackMove должен поддерживать очередь наравне с Move.
            var unit = CreateTestUnit();
            unit.SetMovingForTest();

            unit.EnqueueCommand(UnitCommand.AttackMove(new Vector3(15f, 0f, 0f)));

            Assert.AreEqual(1, unit.OrderQueueCount,
                "AttackMove должен добавляться в очередь при Moving.");
        }

        // ----------------------------------------------------------------
        // Дополнительный: очередь не ломает CommandIssued-событие
        // ----------------------------------------------------------------

        [Test]
        public void SimulateArrival_WithQueuedCommand_RaisesCommandIssuedEvent()
        {
            var unit = CreateTestUnit();
            unit.SetMovingForTest();

            bool eventFired = false;
            unit.CommandIssued += _ => eventFired = true;

            unit.EnqueueCommand(UnitCommand.Move(new Vector3(5f, 0f, 0f)));
            unit.SimulateArrivalForTest();

            Assert.IsTrue(eventFired,
                "CommandIssued должно сработать при продвижении очереди (UnitCombat подписан).");
        }

        // ----------------------------------------------------------------
        // Cleanup
        // ----------------------------------------------------------------

        [TearDown]
        public void TearDown()
        {
            // Сбрасываем флаг игнорирования сообщений, чтобы не влиять на другие тесты.
            LogAssert.ignoreFailingMessages = false;

            // Уничтожаем все GO созданные в тесте
            var gos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in gos)
            {
                if (go.name == "TestUnit")
                    Object.DestroyImmediate(go);
            }
        }
    }
}
