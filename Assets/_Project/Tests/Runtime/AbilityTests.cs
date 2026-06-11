using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode-тесты реальных способностей героя 2–4 (ADR-013):
    /// Ударная волна (Shockwave), Ремонтное поле (RepairField), Перегрузка (Overcharge).
    /// </summary>
    [TestFixture]
    public class AbilityTests
    {
        private GameObject         _groundGo;
        private GameObject         _controllerGo;
        private GameModeController _modeController;
        private GameObject         _heroGo;
        private AbilitySystem      _abilitySystem;

        [SetUp]
        public void SetUp()
        {
            // NavMesh-плоскость: Unit требует NavMeshAgent
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "AbilityTestGround";
            _groundGo.transform.localScale = new Vector3(5f, 1f, 5f);
            _groundGo.AddComponent<NavMeshSurface>().BuildNavMesh();
            Physics.SyncTransforms();

            _controllerGo   = new GameObject("AbilityTestManagers");
            _modeController = _controllerGo.AddComponent<GameModeController>();
            _modeController.InitForTest(null, null);
            _modeController.SetMode(GameMode.Tps);

            _heroGo        = new GameObject("AbilityTestHero");
            _heroGo.transform.position = Vector3.zero;
            _abilitySystem = _heroGo.AddComponent<AbilitySystem>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.name.StartsWith("AbilityTest"))
                    Object.DestroyImmediate(go);
            }
            NavMesh.RemoveAllNavMeshData();
        }

        // ----------------------------------------------------------------
        // Фабрика юнитов (паттерн CombatTests)
        // ----------------------------------------------------------------

        private static Health CreateUnitWithHealth(string name, Faction faction, Vector3 position, float maxHp = 100f)
        {
            var go = new GameObject(name);
            go.transform.position = position;

            go.AddComponent<NavMeshAgent>();
            var unit = go.AddComponent<Unit>();

            var factionField = typeof(Unit).GetField(
                "_faction",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            factionField?.SetValue(unit, faction);

            UnitRegistry.Unregister(unit);
            UnitRegistry.Register(unit);

            var health = go.AddComponent<Health>();
            health.Init(maxHp);
            return health;
        }

        // ----------------------------------------------------------------
        // Shockwave
        // ----------------------------------------------------------------

        [Test]
        public void Shockwave_DamagesEnemiesInRadius_IgnoresOutsideAndAllies()
        {
            var shockwave = AbilityData.CreateForTest(
                "Ударная волна", cooldown: 10f, AbilityType.Shockwave,
                effectRadius: 6f, effectAmount: 40f);
            _abilitySystem.InitForTest(_modeController, null, new[] { shockwave, null, null, null });

            var enemyNear = CreateUnitWithHealth("AbilityTestEnemyNear", Faction.Enemy,  new Vector3(3f, 0f, 0f));
            var enemyFar  = CreateUnitWithHealth("AbilityTestEnemyFar",  Faction.Enemy,  new Vector3(20f, 0f, 0f));
            var allyNear  = CreateUnitWithHealth("AbilityTestAllyNear",  Faction.Player, new Vector3(-3f, 0f, 0f));

            bool cast = _abilitySystem.TryCast(0);

            Assert.IsTrue(cast, "Каст Ударной волны должен пройти.");
            Assert.AreEqual(60f,  enemyNear.CurrentHp, 1e-3f, "Враг в радиусе должен получить 40 урона.");
            Assert.AreEqual(100f, enemyFar.CurrentHp,  1e-3f, "Враг вне радиуса не должен пострадать.");
            Assert.AreEqual(100f, allyNear.CurrentHp,  1e-3f, "Союзник не должен получать урон от Ударной волны.");
        }

        // ----------------------------------------------------------------
        // RepairField
        // ----------------------------------------------------------------

        [Test]
        public void RepairField_HealsAlliesInRadius_NotEnemiesAndNotAboveMax()
        {
            var repair = AbilityData.CreateForTest(
                "Ремонтное поле", cooldown: 12f, AbilityType.RepairField,
                effectRadius: 8f, effectAmount: 30f);
            _abilitySystem.InitForTest(_modeController, null, new[] { repair, null, null, null });

            var allyWounded = CreateUnitWithHealth("AbilityTestAllyWounded", Faction.Player, new Vector3(4f, 0f, 0f));
            allyWounded.TakeDamage(50f); // 50/100

            var allyAlmostFull = CreateUnitWithHealth("AbilityTestAllyAlmostFull", Faction.Player, new Vector3(-4f, 0f, 0f));
            allyAlmostFull.TakeDamage(10f); // 90/100

            var allyFar = CreateUnitWithHealth("AbilityTestAllyFar", Faction.Player, new Vector3(20f, 0f, 0f));
            allyFar.TakeDamage(50f); // вне радиуса

            var enemyWounded = CreateUnitWithHealth("AbilityTestEnemyWounded", Faction.Enemy, new Vector3(2f, 0f, 0f));
            enemyWounded.TakeDamage(50f);

            bool cast = _abilitySystem.TryCast(0);

            Assert.IsTrue(cast, "Каст Ремонтного поля должен пройти.");
            Assert.AreEqual(80f,  allyWounded.CurrentHp,    1e-3f, "Раненый союзник в радиусе должен вылечиться на 30.");
            Assert.AreEqual(100f, allyAlmostFull.CurrentHp, 1e-3f, "Лечение не должно превышать максимум HP.");
            Assert.AreEqual(50f,  allyFar.CurrentHp,        1e-3f, "Союзник вне радиуса не лечится.");
            Assert.AreEqual(50f,  enemyWounded.CurrentHp,   1e-3f, "Враг не должен лечиться Ремонтным полем.");
        }

        // ----------------------------------------------------------------
        // Overcharge
        // ----------------------------------------------------------------

        [Test]
        public void Overcharge_CastActivatesShooterBuff()
        {
            var shooter = _heroGo.AddComponent<HeroShooter>();
            shooter.InitForTest(_modeController, cam: null);

            var overcharge = AbilityData.CreateForTest(
                "Перегрузка", cooldown: 15f, AbilityType.Overcharge,
                buffDuration: 5f, fireRateMultiplier: 2f, damageMultiplier: 1.5f);
            _abilitySystem.InitForTest(_modeController, null, new[] { overcharge, null, null, null }, shooter);

            Assert.IsFalse(shooter.IsOvercharged, "До каста перегрузки быть не должно.");

            bool cast = _abilitySystem.TryCast(0);

            Assert.IsTrue(cast, "Каст Перегрузки должен пройти.");
            Assert.IsTrue(shooter.IsOvercharged, "После каста бафф должен быть активен.");
        }

        [Test]
        public void Overcharge_ExpiresAfterDuration()
        {
            var shooter = _heroGo.AddComponent<HeroShooter>();
            shooter.InitForTest(_modeController, cam: null);

            // Нулевая длительность: бафф истекает в этом же кадре (now > until на следующем запросе)
            shooter.ApplyOvercharge(duration: -0.01f, fireRateMultiplier: 2f, damageMultiplier: 1.5f);

            Assert.IsFalse(shooter.IsOvercharged, "Бафф с истёкшей длительностью не должен быть активен.");
        }
    }
}
