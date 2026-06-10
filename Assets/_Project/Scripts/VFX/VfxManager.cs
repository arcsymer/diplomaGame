using System.Collections;
using System.Collections.Generic;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.Units;
using UnityEngine;

namespace DiplomaGame.Runtime.VFX
{
    /// <summary>
    /// Управляет пулами VFX-эффектов. Подписывается на игровые события и воспроизводит
    /// соответствующие эффекты.
    ///
    /// Используйте InitForTest() для тестового сценария (фейковые ParticleSystem).
    /// </summary>
    public sealed class VfxManager : MonoBehaviour
    {
        private const int PoolSize = 6;

        // ----------------------------------------------------------------
        // Инспекторные ссылки на VFX-префабы
        // ----------------------------------------------------------------

        [SerializeField] private GameObject _muzzleFlashPrefab;
        [SerializeField] private GameObject _hitImpactPrefab;
        [SerializeField] private GameObject _explosionPrefab;
        [SerializeField] private GameObject _buildEffectPrefab;

        // ----------------------------------------------------------------
        // Пулы
        // ----------------------------------------------------------------

        private readonly List<ParticleSystem> _muzzlePool    = new List<ParticleSystem>(PoolSize);
        private readonly List<ParticleSystem> _hitPool       = new List<ParticleSystem>(PoolSize);
        private readonly List<ParticleSystem> _explosionPool = new List<ParticleSystem>(PoolSize);
        private readonly List<ParticleSystem> _buildPool     = new List<ParticleSystem>(PoolSize);

        // Текущие индексы пулов
        private int _muzzleIndex    = -1;
        private int _hitIndex       = -1;
        private int _explosionIndex = -1;
        private int _buildIndex     = -1;

        // Флаг: тестовый режим (пул уже инициализирован снаружи)
        private bool _testMode;

        // Кэш WaitForSeconds по длительности (избегаем new на каждой корутине)
        private readonly Dictionary<float, WaitForSeconds> _waitCache =
            new Dictionary<float, WaitForSeconds>(8);

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            if (!_testMode)
                InitPools();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        // ----------------------------------------------------------------
        // Подписки на события
        // ----------------------------------------------------------------

        private void SubscribeToEvents()
        {
            // HeroShooter.ShotFired — экземплярное событие; подписываемся через поиск в сцене
            var heroShooter = UnityEngine.Object.FindFirstObjectByType<HeroShooter>();
            if (heroShooter != null)
                heroShooter.ShotFired += OnHeroShotFired;

            // Статические события шины
            UnitCombat.AnyAttacked   += OnUnitAttacked;
            Health.AnyDied           += OnAnyDied;

            // BuildingPlacer — экземплярное событие
            var placer = UnityEngine.Object.FindFirstObjectByType<BuildingPlacer>();
            if (placer != null)
                placer.BuildingPlaced += OnBuildingPlaced;
        }

        private void UnsubscribeFromEvents()
        {
            var heroShooter = UnityEngine.Object.FindFirstObjectByType<HeroShooter>();
            if (heroShooter != null)
                heroShooter.ShotFired -= OnHeroShotFired;

            UnitCombat.AnyAttacked   -= OnUnitAttacked;
            Health.AnyDied           -= OnAnyDied;

            var placer = UnityEngine.Object.FindFirstObjectByType<BuildingPlacer>();
            if (placer != null)
                placer.BuildingPlaced -= OnBuildingPlaced;
        }

        // ----------------------------------------------------------------
        // Обработчики событий
        // ----------------------------------------------------------------

        private void OnHeroShotFired(Vector3 origin, Vector3 end, bool hit)
        {
            // Эффект вспышки только если есть попадание (end = точка удара)
            if (hit)
                Play(_hitPool, ref _hitIndex, end);
        }

        private void OnUnitAttacked(Vector3 position)
        {
            Play(_muzzlePool, ref _muzzleIndex, position);
        }

        private void OnAnyDied(Health health)
        {
            if (health != null)
                Play(_explosionPool, ref _explosionIndex, health.transform.position);
        }

        private void OnBuildingPlaced(Vector3 position)
        {
            Play(_buildPool, ref _buildIndex, position);
        }

        // ----------------------------------------------------------------
        // Публичный API воспроизведения
        // ----------------------------------------------------------------

        /// <summary>
        /// Воспроизводит следующий по кругу эффект из пула в заданной позиции.
        /// </summary>
        public void Play(List<ParticleSystem> pool, ref int currentIndex, Vector3 position)
        {
            if (pool == null || pool.Count == 0)
                return;

            currentIndex = VfxLogic.NextPoolIndex(currentIndex, pool.Count);
            var ps = pool[currentIndex];
            if (ps == null)
                return;

            ps.transform.position = position;
            ps.gameObject.SetActive(true);
            ps.Play();

            StartCoroutine(DeactivateAfterPlay(ps));
        }

        // ----------------------------------------------------------------
        // Инициализация для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Заменяет пулы заранее созданными объектами. Вызывать до Awake (в SetUp теста).
        /// </summary>
        internal void InitForTest(
            List<ParticleSystem> muzzle,
            List<ParticleSystem> hit,
            List<ParticleSystem> explosion,
            List<ParticleSystem> build)
        {
            _testMode = true;

            _muzzlePool.Clear();
            _muzzlePool.AddRange(muzzle);

            _hitPool.Clear();
            _hitPool.AddRange(hit);

            _explosionPool.Clear();
            _explosionPool.AddRange(explosion);

            _buildPool.Clear();
            _buildPool.AddRange(build);
        }

        /// <summary>
        /// Вспомогательный метод для тестов: получить текущий индекс muzzle-пула.
        /// </summary>
        internal int MuzzleIndex => _muzzleIndex;

        // ----------------------------------------------------------------
        // Инициализация пулов
        // ----------------------------------------------------------------

        private void InitPools()
        {
            FillPool(_muzzleFlashPrefab,  _muzzlePool,    PoolSize);
            FillPool(_hitImpactPrefab,    _hitPool,       PoolSize);
            FillPool(_explosionPrefab,    _explosionPool, PoolSize);
            FillPool(_buildEffectPrefab,  _buildPool,     PoolSize);
        }

        private void FillPool(GameObject prefab, List<ParticleSystem> pool, int count)
        {
            if (prefab == null)
                return;

            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(prefab, transform);
                go.SetActive(false);
                var ps = go.GetComponent<ParticleSystem>();
                if (ps == null)
                    ps = go.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                    pool.Add(ps);
            }
        }

        // ----------------------------------------------------------------
        // Корутина деактивации
        // ----------------------------------------------------------------

        private IEnumerator DeactivateAfterPlay(ParticleSystem ps)
        {
            if (ps == null) yield break;

            var main = ps.main;
            float duration = main.duration + main.startLifetime.constantMax;

            // Переиспользуем WaitForSeconds с одинаковой длительностью — без аллокаций
            if (!_waitCache.TryGetValue(duration, out WaitForSeconds wait))
            {
                wait = new WaitForSeconds(duration);
                _waitCache[duration] = wait;
            }

            yield return wait;

            if (ps != null && ps.gameObject != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                ps.gameObject.SetActive(false);
            }
        }
    }
}
