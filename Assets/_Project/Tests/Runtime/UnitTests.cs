using System.Collections;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode-тесты для Unit и NavMeshAgent.
    /// Создают плоскость с NavMeshSurface, запекают NavMesh в рантайме
    /// и проверяют реакцию Unit на приказы.
    /// </summary>
    [TestFixture]
    public class UnitTests
    {
        private GameObject       _groundGo;
        private NavMeshSurface   _surface;
        private GameObject       _unitGo;
        private NavMeshAgent     _agent;
        private Unit             _unit;

        [SetUp]
        public void SetUp()
        {
            // --- Плоскость-земля ---
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "TestGround";
            _groundGo.transform.position = Vector3.zero;
            _groundGo.transform.localScale = new Vector3(5f, 1f, 5f); // 50x50 units

            _surface = _groundGo.AddComponent<NavMeshSurface>();
            _surface.BuildNavMesh();

            // --- Юнит ---
            _unitGo = new GameObject("TestUnit");
            _unitGo.transform.position = new Vector3(0f, 0f, 0f);

            _agent = _unitGo.AddComponent<NavMeshAgent>();
            _agent.speed           = 5f;
            _agent.stoppingDistance = 0.1f;

            _unit  = _unitGo.AddComponent<Unit>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_unitGo);
            Object.Destroy(_groundGo);

            // Очищаем NavMesh после теста
            NavMesh.RemoveAllNavMeshData();
        }

        // ----------------------------------------------------------------
        // Move — юнит переходит в Moving и получает путь
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator IssueCommand_Move_StateIsMovingAndHasPath()
        {
            Vector3 target = new Vector3(5f, 0f, 0f);
            _unit.IssueCommand(UnitCommand.Move(target));

            // Ждём 2-3 кадра, чтобы NavMesh обработал запрос
            yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(UnitState.Moving, _unit.CurrentState,
                "После IssueCommand(Move) состояние должно быть Moving.");
            Assert.IsTrue(_agent.hasPath || _agent.pathPending,
                "После IssueCommand(Move) агент должен иметь путь или вычислять его.");
        }

        // ----------------------------------------------------------------
        // Hold — юнит останавливается
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator IssueCommand_Hold_StateIsHoldingAndNoPath()
        {
            // Сначала даём приказ Move, потом Hold
            _unit.IssueCommand(UnitCommand.Move(new Vector3(5f, 0f, 0f)));
            yield return null;

            _unit.IssueCommand(UnitCommand.Hold());
            yield return null;

            Assert.AreEqual(UnitState.Holding, _unit.CurrentState,
                "После IssueCommand(Hold) состояние должно быть Holding.");
            Assert.IsFalse(_agent.hasPath,
                "После Hold у агента не должно быть активного пути.");
        }

        // ----------------------------------------------------------------
        // SetSelected без SelectionRing — не кидает исключений
        // ----------------------------------------------------------------

        [Test]
        public void SetSelected_WithoutSelectionRing_DoesNotThrow()
        {
            // _unitGo не имеет дочернего SelectionRing — должно быть null-безопасно
            Assert.DoesNotThrow(() => _unit.SetSelected(true),
                "SetSelected(true) без дочернего SelectionRing не должен бросать исключение.");
            Assert.DoesNotThrow(() => _unit.SetSelected(false),
                "SetSelected(false) без дочернего SelectionRing не должен бросать исключение.");
        }

        // ----------------------------------------------------------------
        // UnitRegistry — OnEnable/OnDisable регистрация
        // ----------------------------------------------------------------

        [Test]
        public void UnitRegistry_OnEnableRegisters_OnDisableUnregisters()
        {
            // _unit уже включён в SetUp, должен быть в реестре
            Assert.IsTrue(ContainsUnit(_unit),
                "Unit должен быть зарегистрирован в UnitRegistry после OnEnable.");

            _unitGo.SetActive(false);

            Assert.IsFalse(ContainsUnit(_unit),
                "Unit должен быть снят с учёта в UnitRegistry после OnDisable.");
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private static bool ContainsUnit(Unit unit)
        {
            var all = UnitRegistry.AllUnits;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == unit) return true;
            }
            return false;
        }
    }
}
