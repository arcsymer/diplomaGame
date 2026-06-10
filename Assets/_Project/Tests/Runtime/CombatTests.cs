using System.Collections;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode-тесты для системы боя M4.
    /// Создают NavMesh-плоскость и проверяют Health, UnitCombat и CombatState.
    /// </summary>
    [TestFixture]
    public class CombatTests
    {
        // ----------------------------------------------------------------
        // Общая инфраструктура
        // ----------------------------------------------------------------

        private GameObject     _groundGo;
        private NavMeshSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "CombatTestGround";
            _groundGo.transform.position   = Vector3.zero;
            _groundGo.transform.localScale = new Vector3(5f, 1f, 5f); // 50x50

            _surface = _groundGo.AddComponent<NavMeshSurface>();
            _surface.BuildNavMesh();

            // Маркеры баз — как в продакшен-сцене (Forge их всегда создаёт).
            // Без них rally-точка отступления = Vector3.zero = позиция юнита,
            // и отступление вырождается в мгновенное "прибытие".
            var playerBase = new GameObject("PlayerBaseSpawn");
            playerBase.transform.position = new Vector3(-15f, 0f, -15f);
            var enemyBase = new GameObject("EnemyBaseSpawn");
            enemyBase.transform.position = new Vector3(15f, 0f, 15f);
        }

        [TearDown]
        public void TearDown()
        {
            // Чистим всё что могло остаться
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("CombatTest") ||
                    go.name.StartsWith("PlayerUnit") ||
                    go.name.StartsWith("EnemyUnit")  ||
                    go.name.StartsWith("HoldUnit")   ||
                    go.name.StartsWith("RetreatUnit") ||
                    go.name.EndsWith("BaseSpawn"))
                {
                    Object.Destroy(go);
                }
            }
            Object.Destroy(_groundGo);
            NavMesh.RemoveAllNavMeshData();
        }

        // ----------------------------------------------------------------
        // Вспомогательные фабрики
        // ----------------------------------------------------------------

        private static GameObject CreateUnit(
            string   name,
            Faction  faction,
            Vector3  position,
            UnitData data)
        {
            var go = new GameObject(name);
            go.transform.position = position;

            go.AddComponent<NavMeshAgent>();
            var unit = go.AddComponent<Unit>();

            // Устанавливаем фракцию через Reflection после добавления компонента.
            // Unit.OnEnable уже зарегистрировал юнита с дефолтной фракцией Player —
            // принудительно пере-регистрируем с правильной фракцией.
            var factionField = typeof(Unit).GetField(
                "_faction",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (factionField != null)
                factionField.SetValue(unit, faction);

            // Перерегистрируем с правильной фракцией
            UnitRegistry.Unregister(unit);
            UnitRegistry.Register(unit);

            go.AddComponent<Health>();

            var combat = go.AddComponent<UnitCombat>();
            combat.InitForTest(data);

            return go;
        }

        // ================================================================
        // Health: базовые тесты
        // ================================================================

        [TestFixture]
        public class HealthTests
        {
            private GameObject _go;
            private Health     _health;

            [SetUp]
            public void SetUp()
            {
                _go     = new GameObject("HealthTest");
                _health = _go.AddComponent<Health>();
                _health.Init(100f);
            }

            [TearDown]
            public void TearDown()
            {
                Object.Destroy(_go);
            }

            [Test]
            public void TakeDamage_ReducesHp()
            {
                _health.TakeDamage(30f);

                Assert.AreEqual(70f, _health.CurrentHp, 0.001f,
                    "TakeDamage(30) должен уменьшить HP с 100 до 70.");
            }

            [Test]
            public void TakeDamage_FiresDamagedEvent()
            {
                float capturedAmount = 0f;
                float capturedCurrent = 0f;
                _health.Damaged += (amount, current) =>
                {
                    capturedAmount  = amount;
                    capturedCurrent = current;
                };

                _health.TakeDamage(25f);

                Assert.AreEqual(25f, capturedAmount,  0.001f, "Событие Damaged должно передать amount=25.");
                Assert.AreEqual(75f, capturedCurrent, 0.001f, "Событие Damaged должно передать currentHp=75.");
            }

            [Test]
            public void TakeDamage_KillsUnit_DiedFiredOnce()
            {
                int diedCount = 0;
                _health.Died += () => diedCount++;

                _health.TakeDamage(100f);

                Assert.AreEqual(1, diedCount, "Событие Died должно сработать ровно один раз.");
                Assert.IsTrue(_health.IsDead,  "После смертельного урона IsDead должен быть true.");
            }

            [Test]
            public void TakeDamage_AfterDeath_Ignored()
            {
                int diedCount = 0;
                _health.Died += () => diedCount++;

                _health.TakeDamage(100f); // смерть
                _health.TakeDamage(50f);  // повторный урон — должен игнорироваться

                Assert.AreEqual(1, diedCount, "Died не должен срабатывать повторно.");
                Assert.AreEqual(0f, _health.CurrentHp, 0.001f,
                    "HP после смерти не должно уходить ниже 0.");
            }

            [Test]
            public void TakeDamage_HpNeverBelowZero()
            {
                _health.TakeDamage(999f);

                Assert.AreEqual(0f, _health.CurrentHp, 0.001f,
                    "HP не должно уходить ниже нуля.");
            }
        }

        // ================================================================
        // Бой: юнит Player атакует Enemy и тот умирает
        // ================================================================

        [UnityTest]
        public IEnumerator UnitCombat_PlayerAttacksEnemy_EnemyDies()
        {
            // Data: большой урон, маленький cooldown, маленький range чтобы юниты были рядом
            var playerData = UnitData.CreateForTest(
                displayName:     "Player",
                maxHp:           100f,
                damage:          100f,    // убивает за один удар
                attackRange:     5f,
                attackCooldown:  0.1f,    // быстрая атака
                aggroRadius:     12f,
                moveSpeed:       5f,
                retreatHpFraction: 0f,    // отступление отключено для чистоты теста
                retreatDisabled: true);

            var enemyData = UnitData.CreateForTest(
                displayName:     "Enemy",
                maxHp:           50f,
                damage:          1f,
                attackRange:     5f,
                attackCooldown:  10f,
                aggroRadius:     12f,
                moveSpeed:       5f,
                retreatHpFraction: 0f,
                retreatDisabled: true);

            // Размещаем рядом — в attackRange
            var playerGo = CreateUnit("PlayerUnit_Attack", Faction.Player, new Vector3(0f, 0f, 0f), playerData);
            var enemyGo  = CreateUnit("EnemyUnit_Attack",  Faction.Enemy,  new Vector3(3f, 0f, 0f), enemyData);

            var enemyHealth = enemyGo.GetComponent<Health>();

            // Ждём ~1 секунду боевых тиков
            yield return new WaitForSeconds(1f);

            Assert.IsTrue(
                enemyHealth.IsDead || enemyHealth.CurrentHp < 50f,
                "Через 1 секунду у Enemy должно упасть HP или он должен умереть.");
        }

        // ================================================================
        // Отступление: юнит с низким HP переходит в Retreating
        // ================================================================

        [UnityTest]
        public IEnumerator UnitCombat_LowHp_TransitionsToRetreating()
        {
            var data = UnitData.CreateForTest(
                maxHp:             100f,
                damage:            1f,
                attackRange:       5f,
                attackCooldown:    10f,
                aggroRadius:       12f,
                moveSpeed:         5f,
                retreatHpFraction: 0.25f,
                retreatDisabled:   false);

            var unitGo = CreateUnit("RetreatUnit_Test", Faction.Player, new Vector3(0f, 0f, 0f), data);
            var health = unitGo.GetComponent<Health>();
            var combat = unitGo.GetComponent<UnitCombat>();

            // Создаём врага в aggro-радиусе (чтобы был кандидат)
            var enemyData = UnitData.CreateForTest(
                maxHp: 200f, damage: 0f, attackRange: 0f,
                attackCooldown: 10f, aggroRadius: 12f, moveSpeed: 1f,
                retreatDisabled: true);
            CreateUnit("EnemyUnit_Retreat", Faction.Enemy, new Vector3(5f, 0f, 0f), enemyData);

            // Опускаем HP ниже порога (< 25%)
            health.TakeDamage(80f); // останется 20 из 100 = 20%

            // Ждём больше одного тика сканирования (0.25с + запас)
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(CombatState.Retreating, combat.CurrentCombatState,
                "При HP < retreatHpFraction юнит должен перейти в CombatState.Retreating.");
        }

        // ================================================================
        // Hold: юнит в Holding не преследует, но атакует в радиусе
        // ================================================================

        [UnityTest]
        public IEnumerator UnitCombat_Holding_DoesNotMove()
        {
            var data = UnitData.CreateForTest(
                maxHp:             100f,
                damage:            1f,
                attackRange:       3f,      // атака ближнего боя
                attackCooldown:    10f,
                aggroRadius:       12f,
                moveSpeed:         5f,
                retreatHpFraction: 0f,
                retreatDisabled:   true);

            var unitGo = CreateUnit("HoldUnit_Test", Faction.Player, new Vector3(0f, 0f, 0f), data);
            var unit   = unitGo.GetComponent<Unit>();

            // Даём приказ Hold
            unit.IssueCommand(UnitCommand.Hold());

            Vector3 startPos = unitGo.transform.position;

            // Ставим врага в aggro, но за attackRange
            var enemyData = UnitData.CreateForTest(
                maxHp: 200f, damage: 0f, attackRange: 0f,
                attackCooldown: 10f, aggroRadius: 0f, moveSpeed: 1f,
                retreatDisabled: true);
            CreateUnit("EnemyUnit_Hold", Faction.Enemy, new Vector3(8f, 0f, 0f), enemyData);

            // Ждём пару тактов сканирования
            yield return new WaitForSeconds(0.6f);

            float displacement = Vector3.Distance(startPos, unitGo.transform.position);
            Assert.Less(displacement, 0.5f,
                "Юнит в Holding не должен двигаться при враге за attackRange.");
        }
    }
}
