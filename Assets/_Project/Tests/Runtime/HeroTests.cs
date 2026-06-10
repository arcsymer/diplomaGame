using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Hero;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode-тесты для системы TPS-героя (M3).
    /// </summary>
    [TestFixture]
    public class HeroTests
    {
        // ================================================================
        // AbilitySystem — кулдаун и событие AbilityCast
        // ================================================================

        [TestFixture]
        public class AbilitySystemTests
        {
            private GameObject    _go;
            private AbilitySystem _abilitySystem;
            private GameObject    _controllerGo;
            private GameModeController _modeController;

            [SetUp]
            public void SetUp()
            {
                // Создаём GameModeController без камер и InputActionAsset
                _controllerGo   = new GameObject("TestGameManagers");
                _modeController = _controllerGo.AddComponent<GameModeController>();
                _modeController.InitForTest(null, null);
                _modeController.SetMode(GameMode.Tps);

                // AbilitySystem на отдельном GO (не нужен CharacterController для этого теста)
                _go           = new GameObject("TestHero_AbilitySystem");
                _abilitySystem = _go.AddComponent<AbilitySystem>();

                // Подготавливаем AbilityData через internal-метод (без UnityEditor)
                var dashData = AbilityData.CreateForTest("Dash", 4f, AbilityType.Dash, dashDistance: 6f);

                _abilitySystem.InitForTest(_modeController, null, new[] { dashData, null, null, null });
            }

            [TearDown]
            public void TearDown()
            {
                UnityEngine.Object.Destroy(_go);
                UnityEngine.Object.Destroy(_controllerGo);
            }

            [Test]
            public void TryCast_FirstCast_ReturnsTrue()
            {
                bool result = _abilitySystem.TryCast(0);

                Assert.IsTrue(result, "Первое применение способности должно быть успешным.");
            }

            [Test]
            public void TryCast_AfterFirstCast_ReturnsFalse()
            {
                _abilitySystem.TryCast(0);
                bool second = _abilitySystem.TryCast(0);

                Assert.IsFalse(second, "Повторное применение сразу после должно быть заблокировано кулдауном.");
            }

            [Test]
            public void TryCast_FiresAbilityCastEvent()
            {
                int          fireCount   = 0;
                int          firedIndex  = -1;
                AbilityData  firedData   = null;

                _abilitySystem.AbilityCast += (idx, data) =>
                {
                    fireCount++;
                    firedIndex = idx;
                    firedData  = data;
                };

                _abilitySystem.TryCast(0);

                Assert.AreEqual(1, fireCount,  "Событие AbilityCast должно сработать ровно один раз.");
                Assert.AreEqual(0, firedIndex, "Событие должно передать правильный индекс слота.");
                Assert.IsNotNull(firedData,    "Событие должно передать ненулевой AbilityData.");
            }

            [Test]
            public void TryCast_EmptySlot_ReturnsFalse()
            {
                // Слот 1..3 не заполнен
                bool result = _abilitySystem.TryCast(1);

                Assert.IsFalse(result, "Пустой слот не должен применяться.");
            }

            [Test]
            public void TryCast_InRtsMode_ReturnsFalse()
            {
                _modeController.SetMode(GameMode.Rts);
                bool result = _abilitySystem.TryCast(0);

                Assert.IsFalse(result, "В RTS-режиме способность не должна применяться.");
            }
        }

        // ================================================================
        // HeroShooter — выстрел, IDamageable, событие ShotFired
        // ================================================================

        [TestFixture]
        public class HeroShooterTests
        {
            private GameObject         _heroGo;
            private HeroShooter        _shooter;
            private GameObject         _targetGo;
            private TestDamageable     _damageable;
            private Camera             _testCamera;
            private GameObject         _controllerGo;
            private GameModeController _modeController;

            [SetUp]
            public void SetUp()
            {
                // GameModeController в TPS-режиме
                _controllerGo   = new GameObject("TestGameManagers");
                _modeController = _controllerGo.AddComponent<GameModeController>();
                _modeController.InitForTest(null, null);
                _modeController.SetMode(GameMode.Tps);

                // Создаём куб-цель прямо перед камерой
                _targetGo   = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _targetGo.name = "ShootTarget";
                _targetGo.transform.position = new Vector3(0f, 0f, 10f);
                _damageable = _targetGo.AddComponent<TestDamageable>();

                // Камера — направлена прямо на куб (+Z)
                var camGo   = new GameObject("TestCamera");
                _testCamera = camGo.AddComponent<Camera>();
                camGo.transform.position = new Vector3(0f, 0f, 0f);
                camGo.transform.rotation = Quaternion.identity;

                // HeroShooter
                _heroGo  = new GameObject("TestHero_Shooter");
                _shooter = _heroGo.AddComponent<HeroShooter>();
                _shooter.InitForTest(_modeController, _testCamera);

                // Коллайдер куба создан в этом же кадре — синхронизируем физический мир,
                // иначе Raycast его не увидит
                Physics.SyncTransforms();
            }

            [TearDown]
            public void TearDown()
            {
                // DestroyImmediate: Destroy отложен до конца кадра, а синхронные тесты
                // выполняются в одном кадре — куб предыдущего теста перехватывал бы луч
                UnityEngine.Object.DestroyImmediate(_heroGo);
                UnityEngine.Object.DestroyImmediate(_targetGo);
                UnityEngine.Object.DestroyImmediate(_testCamera.gameObject);
                UnityEngine.Object.DestroyImmediate(_controllerGo);
                Physics.SyncTransforms();
            }

            [Test]
            public void TryFire_HitsIDamageable_TakeDamageCalled()
            {
                _shooter.TryFire();

                Assert.IsTrue(_damageable.WasHit, "TakeDamage должен быть вызван на IDamageable.");
                Assert.Greater(_damageable.TotalDamage, 0f, "Урон должен быть больше нуля.");
            }

            [Test]
            public void TryFire_FiresShotFiredEvent()
            {
                bool eventFired = false;
                bool hitResult  = false;

                _shooter.ShotFired += (origin, end, hit) =>
                {
                    eventFired = true;
                    hitResult  = hit;
                };

                _shooter.TryFire();

                Assert.IsTrue(eventFired, "Событие ShotFired должно сработать.");
                Assert.IsTrue(hitResult,  "ShotFired должно передать hit=true при попадании.");
            }

            [Test]
            public void TryFire_Miss_ShotFiredHitFalse()
            {
                // Перемещаем цель так, чтобы луч не попал
                _targetGo.transform.position = new Vector3(100f, 100f, 10f);
                Physics.SyncTransforms();

                bool hitResult = true;
                _shooter.ShotFired += (origin, end, hit) => hitResult = hit;

                _shooter.TryFire();

                Assert.IsFalse(hitResult, "При промахе ShotFired должно передать hit=false.");
            }
        }

        // ================================================================
        // HeroController — переключение NavMeshAgent / CharacterController
        // ================================================================

        [TestFixture]
        public class HeroControllerModeTests
        {
            private GameObject         _groundGo;
            private NavMeshSurface     _surface;
            private GameObject         _heroGo;
            private HeroController     _heroCtrl;
            private CharacterController _cc;
            private NavMeshAgent       _agent;
            private GameObject         _controllerGo;
            private GameModeController _modeController;

            [SetUp]
            public void SetUp()
            {
                // Создаём NavMesh для NavMeshAgent
                _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
                _groundGo.name = "TestGround";
                _groundGo.transform.localScale = new Vector3(5f, 1f, 5f);
                _surface  = _groundGo.AddComponent<NavMeshSurface>();
                _surface.BuildNavMesh();

                // GameModeController
                _controllerGo   = new GameObject("TestGameManagers");
                _modeController = _controllerGo.AddComponent<GameModeController>();
                _modeController.InitForTest(null, null);

                // Hero GO: CharacterController + NavMeshAgent + HeroController
                _heroGo   = new GameObject("TestHero");
                _heroGo.transform.position = Vector3.zero;
                _cc       = _heroGo.AddComponent<CharacterController>();
                _agent    = _heroGo.AddComponent<NavMeshAgent>();
                _heroCtrl = _heroGo.AddComponent<HeroController>();
                _heroCtrl.InitForTest(_modeController);
            }

            [TearDown]
            public void TearDown()
            {
                UnityEngine.Object.Destroy(_heroGo);
                UnityEngine.Object.Destroy(_groundGo);
                UnityEngine.Object.Destroy(_controllerGo);
                NavMesh.RemoveAllNavMeshData();
            }

            [Test]
            public void SetMode_Tps_CharacterControllerEnabled_AgentDisabled()
            {
                _modeController.SetMode(GameMode.Tps);

                Assert.IsTrue(_cc.enabled,      "В TPS: CharacterController должен быть включён.");
                Assert.IsFalse(_agent.enabled,  "В TPS: NavMeshAgent должен быть выключен.");
            }

            [Test]
            public void SetMode_Rts_CharacterControllerDisabled_AgentEnabled()
            {
                // Сначала TPS, потом RTS
                _modeController.SetMode(GameMode.Tps);
                _modeController.SetMode(GameMode.Rts);

                Assert.IsFalse(_cc.enabled,    "В RTS: CharacterController должен быть выключен.");
                Assert.IsTrue(_agent.enabled,  "В RTS: NavMeshAgent должен быть включён.");
            }

            [Test]
            public void SetMode_Tps_HeroControllerEnabled()
            {
                _modeController.SetMode(GameMode.Tps);

                Assert.IsTrue(_heroCtrl.enabled, "В TPS: HeroController должен быть включён.");
            }

            [Test]
            public void SetMode_Rts_HeroControllerDisabled()
            {
                _modeController.SetMode(GameMode.Tps);
                _modeController.SetMode(GameMode.Rts);

                Assert.IsFalse(_heroCtrl.enabled, "В RTS: HeroController должен быть выключен.");
            }
        }
    }

    // ----------------------------------------------------------------
    // Вспомогательный компонент для тестов
    // ----------------------------------------------------------------

    /// <summary>Простейшая реализация IDamageable для тестов стрельбы.</summary>
    internal sealed class TestDamageable : MonoBehaviour, IDamageable
    {
        public bool  WasHit      { get; private set; }
        public float TotalDamage { get; private set; }

        public void TakeDamage(float amount)
        {
            WasHit       = true;
            TotalDamage += amount;
        }
    }
}
