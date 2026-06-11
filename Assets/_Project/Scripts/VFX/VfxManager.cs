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
    /// Деактивация эффектов — через массивы таймеров float[] в Update() без корутин и GC.
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

        // ----------------------------------------------------------------
        // Таймеры деактивации — массивы float по размеру пула.
        // Значение > 0: оставшееся время до SetActive(false).
        // Инициализируются после финального заполнения пулов.
        // ----------------------------------------------------------------

        private float[] _muzzleTimers;
        private float[] _hitTimers;
        private float[] _explosionTimers;
        private float[] _buildTimers;

        // Флаг: тестовый режим (пул уже инициализирован снаружи)
        private bool _testMode;

        // Кэш AbilitySystem (находим один раз в Start)
        private AbilitySystem _abilitySystem;

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

        private void Update()
        {
            TickTimers(_muzzlePool,    _muzzleTimers);
            TickTimers(_hitPool,       _hitTimers);
            TickTimers(_explosionPool, _explosionTimers);
            TickTimers(_buildPool,     _buildTimers);
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

            // AbilitySystem.AbilityCast — визуальный отклик на способности героя
            _abilitySystem = UnityEngine.Object.FindFirstObjectByType<AbilitySystem>();
            if (_abilitySystem != null)
                _abilitySystem.AbilityCast += OnAbilityCast;

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

            if (_abilitySystem != null)
                _abilitySystem.AbilityCast -= OnAbilityCast;

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

        private void OnAbilityCast(int slot, DiplomaGame.Runtime.Data.AbilityData data)
        {
            if (_abilitySystem == null || data == null)
                return;

            Vector3 heroPos = _abilitySystem.transform.position;

            switch (data.AbilityType)
            {
                case DiplomaGame.Runtime.Data.AbilityType.Shockwave:
                    Play(_explosionPool, ref _explosionIndex, heroPos);
                    break;

                case DiplomaGame.Runtime.Data.AbilityType.RepairField:
                    Play(_buildPool, ref _buildIndex, heroPos);
                    break;

                case DiplomaGame.Runtime.Data.AbilityType.Overcharge:
                    Play(_muzzlePool, ref _muzzleIndex, heroPos);
                    break;
            }
        }

        // ----------------------------------------------------------------
        // Публичный API воспроизведения
        // ----------------------------------------------------------------

        /// <summary>
        /// Воспроизводит следующий по кругу эффект из пула в заданной позиции.
        /// Деактивация выполняется через массив таймеров в Update() — без корутин и GC-аллокаций.
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

            // Записываем время жизни в таймер-массив нужного пула
            float[] timers = GetTimersForPool(pool);
            if (timers != null && currentIndex < timers.Length)
            {
                var main     = ps.main;
                timers[currentIndex] = main.duration + main.startLifetime.constantMax;
            }
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

            // Инициализируем массивы таймеров под фактический размер тестовых пулов
            InitTimerArrays();
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

            // Массивы таймеров — после финального заполнения пулов
            InitTimerArrays();
        }

        private void InitTimerArrays()
        {
            _muzzleTimers    = new float[_muzzlePool.Count];
            _hitTimers       = new float[_hitPool.Count];
            _explosionTimers = new float[_explosionPool.Count];
            _buildTimers     = new float[_buildPool.Count];
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
        // Тик таймеров деактивации (вызывается из Update)
        // ----------------------------------------------------------------

        private void TickTimers(List<ParticleSystem> pool, float[] timers)
        {
            if (timers == null) return;
            float dt = Time.deltaTime;
            int count = pool.Count < timers.Length ? pool.Count : timers.Length;
            for (int i = 0; i < count; i++)
            {
                if (timers[i] <= 0f) continue;
                timers[i] -= dt;
                if (timers[i] <= 0f)
                {
                    timers[i] = 0f;
                    var ps = pool[i];
                    if (ps != null && ps.gameObject != null)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                        ps.gameObject.SetActive(false);
                    }
                }
            }
        }

        // ----------------------------------------------------------------
        // Получение массива таймеров по ссылке на пул
        // ----------------------------------------------------------------

        private float[] GetTimersForPool(List<ParticleSystem> pool)
        {
            if (pool == _muzzlePool)    return _muzzleTimers;
            if (pool == _hitPool)       return _hitTimers;
            if (pool == _explosionPool) return _explosionTimers;
            if (pool == _buildPool)     return _buildTimers;
            return null;
        }
    }
}
