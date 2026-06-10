using System.Collections;
using System.Collections.Generic;
using DiplomaGame.Runtime.VFX;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode smoke-тесты для VfxManager.
    /// Проверяем observable-поведение: активация объектов пула, циклический обход индексов.
    /// ParticleSystem.Play() требует реальный кадр, поэтому используем UnityTest+IEnumerator.
    /// </summary>
    [TestFixture]
    public class VfxTests
    {
        private GameObject   _managerGo;
        private VfxManager   _manager;

        // Фейковые пулы (создаём GO + ParticleSystem прямо в тесте)
        private List<ParticleSystem> _muzzlePool;
        private List<ParticleSystem> _hitPool;
        private List<ParticleSystem> _explosionPool;
        private List<ParticleSystem> _buildPool;

        // Все созданные GO для уборки в TearDown
        private readonly List<GameObject> _created = new List<GameObject>();

        // ----------------------------------------------------------------
        // SetUp / TearDown
        // ----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _managerGo = new GameObject("VfxTest_Manager");
            _created.Add(_managerGo);

            _manager = _managerGo.AddComponent<VfxManager>();

            _muzzlePool    = BuildFakePool(3, "Muzzle");
            _hitPool       = BuildFakePool(3, "Hit");
            _explosionPool = BuildFakePool(3, "Explosion");
            _buildPool     = BuildFakePool(3, "Build");

            // InitForTest должен вызываться ДО Awake/Start — в нашем случае Awake уже
            // произошёл при AddComponent, но _testMode=false при этом.
            // Поскольку InitForTest заполняет пулы и выставляет _testMode,
            // повторный вызов InitPools() при этом не произойдёт (Awake уже прошёл).
            _manager.InitForTest(_muzzlePool, _hitPool, _explosionPool, _buildPool);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
            {
                if (go != null)
                    Object.Destroy(go);
            }

            _created.Clear();
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private List<ParticleSystem> BuildFakePool(int size, string prefix)
        {
            var pool = new List<ParticleSystem>(size);
            for (int i = 0; i < size; i++)
            {
                var go = new GameObject($"VfxTest_{prefix}_{i}");
                _created.Add(go);
                var ps = go.AddComponent<ParticleSystem>();
                go.SetActive(false);
                pool.Add(ps);
            }

            return pool;
        }

        // ----------------------------------------------------------------
        // Play активирует объект пула
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Play_MuzzlePool_ActivatesParticleObject()
        {
            // Состояние до: все объекты неактивны
            foreach (var ps in _muzzlePool)
                Assert.IsFalse(ps.gameObject.activeSelf,
                    "Объект пула должен быть неактивен перед Play.");

            int idx = -1;
            _manager.Play(_muzzlePool, ref idx, Vector3.zero);

            // Даём один кадр, чтобы Play() обработался
            yield return null;

            Assert.AreEqual(0, idx, "После первого Play индекс должен быть 0.");
            Assert.IsTrue(_muzzlePool[0].gameObject.activeSelf,
                "Первый объект пула должен стать активным после Play.");
        }

        [UnityTest]
        public IEnumerator Play_HitPool_ActivatesObjectAtPosition()
        {
            var pos = new Vector3(3f, 1f, -2f);
            int idx = -1;

            _manager.Play(_hitPool, ref idx, pos);
            yield return null;

            Assert.AreEqual(pos, _hitPool[0].transform.position,
                "ParticleSystem должен переместиться в указанную позицию.");
            Assert.IsTrue(_hitPool[0].gameObject.activeSelf,
                "Объект пула должен быть активен.");
        }

        // ----------------------------------------------------------------
        // Циклический обход пула
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Play_ThreeTimes_CyclesPool()
        {
            // Пул размером 3: три вызова должны дать индексы 0, 1, 2
            int idx = -1;

            _manager.Play(_muzzlePool, ref idx, Vector3.zero);
            yield return null;
            Assert.AreEqual(0, idx, "Первый Play → индекс 0");

            _manager.Play(_muzzlePool, ref idx, Vector3.zero);
            yield return null;
            Assert.AreEqual(1, idx, "Второй Play → индекс 1");

            _manager.Play(_muzzlePool, ref idx, Vector3.zero);
            yield return null;
            Assert.AreEqual(2, idx, "Третий Play → индекс 2");
        }

        [UnityTest]
        public IEnumerator Play_FourthCall_WrapsToZero()
        {
            // Четвёртый вызов на пуле размером 3 должен вернуться к 0
            int idx = -1;

            for (int i = 0; i < 3; i++)
            {
                _manager.Play(_muzzlePool, ref idx, Vector3.zero);
                yield return null;
            }

            _manager.Play(_muzzlePool, ref idx, Vector3.zero);
            yield return null;

            Assert.AreEqual(0, idx, "Четвёртый Play на пуле из 3 должен обернуться к индексу 0.");
        }

        // ----------------------------------------------------------------
        // Play на пустом пуле — не должно бросать исключение
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Play_EmptyPool_DoesNotThrow()
        {
            var emptyPool = new List<ParticleSystem>();
            int idx = -1;

            Assert.DoesNotThrow(() => _manager.Play(emptyPool, ref idx, Vector3.zero),
                "Play на пустом пуле не должен бросать исключение.");

            yield return null;

            Assert.AreEqual(-1, idx, "Индекс должен остаться -1 при пустом пуле.");
        }

        [UnityTest]
        public IEnumerator Play_NullPool_DoesNotThrow()
        {
            int idx = -1;

            Assert.DoesNotThrow(() => _manager.Play(null, ref idx, Vector3.zero),
                "Play с null-пулом не должен бросать исключение.");

            yield return null;
        }

        // ----------------------------------------------------------------
        // MuzzleIndex через internal свойство
        // ----------------------------------------------------------------

        [UnityTest]
        public IEnumerator MuzzleIndex_InitiallyMinusOne()
        {
            // После InitForTest индекс должен оставаться -1
            Assert.AreEqual(-1, _manager.MuzzleIndex,
                "Начальный MuzzleIndex должен быть -1 (ещё ничего не воспроизводилось).");

            yield return null;
        }

        [UnityTest]
        public IEnumerator MuzzleIndex_AfterTwoPlays_IsOne()
        {
            int idx = _manager.MuzzleIndex; // -1

            _manager.Play(_muzzlePool, ref idx, Vector3.zero);
            yield return null;

            _manager.Play(_muzzlePool, ref idx, Vector3.zero);
            yield return null;

            Assert.AreEqual(1, idx, "После двух Play индекс должен быть 1.");
        }
    }
}
