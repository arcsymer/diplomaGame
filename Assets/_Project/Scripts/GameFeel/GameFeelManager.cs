using System.Collections;
using System.Collections.Generic;
using DiplomaGame.Runtime.Audio;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.Units;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Runtime.GameFeel
{
    /// <summary>
    /// Оркестратор game-feel: подписывается на игровые события и запускает
    /// визуальные/звуковые отклики (рекойл камеры, HitFlash, нокбэк, hitstop, dash-трейл).
    /// Живёт на GameManagers.
    /// </summary>
    public sealed class GameFeelManager : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные ссылки
        // ----------------------------------------------------------------

        [SerializeField] private GameFeelSettings _settings;
        [SerializeField] private CinemachineCamera _tpsCamera;
        [SerializeField] private HeroShooter       _heroShooter;
        [SerializeField] private AbilitySystem     _abilitySystem;
        [SerializeField] private PauseController   _pauseController;
        [SerializeField] private DashTrailHandler  _dashTrail;

        // ----------------------------------------------------------------
        // Внутреннее состояние рекойла камеры
        // ----------------------------------------------------------------

        private CinemachineFollow _tpsFollow;
        private Vector3           _baseFollowOffset;       // кэшированная базовая позиция
        private bool              _baseOffsetCached;

        private float   _recoilCurrentY;    // текущий офсет по Y
        private bool    _recoilActive;

        // ----------------------------------------------------------------
        // Кэш: Health → данные цели (флэш + нокбэк)
        // ----------------------------------------------------------------

        private struct TargetCache
        {
            public HitFlashHandler Handler;
            public NavMeshAgent    Agent;
            public bool            IsPawn;   // имеет Unit-компонент (пехота для нокбэка)
        }

        private readonly Dictionary<Health, TargetCache> _targetCache
            = new Dictionary<Health, TargetCache>(64);

        // ----------------------------------------------------------------
        // Hitstop-состояние
        // ----------------------------------------------------------------

        private bool      _hitstopRunning;
        private Coroutine _hitstopCoroutine;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        // Защита от двойной подписки: InitForTest подписывается сразу
        // (тесты стреляют до первого кадра, когда выполнился бы Start),
        // а Start — обычный путь продакшена.
        private bool _subscribed;

        private void Start()
        {
            // Кэшируем CinemachineFollow TPS-камеры
            if (_tpsCamera != null)
            {
                _tpsFollow = _tpsCamera.GetComponent<CinemachineFollow>();
                if (_tpsFollow != null)
                {
                    _baseFollowOffset  = _tpsFollow.FollowOffset;
                    _baseOffsetCached  = true;
                }
            }

            // Строим начальный кэш целей из UnitRegistry
            BuildTargetCache();

            Subscribe();
        }

        private void Subscribe()
        {
            if (_subscribed) return;
            _subscribed = true;

            if (_heroShooter != null)
                _heroShooter.ShotFired += OnShotFired;

            if (_abilitySystem != null)
                _abilitySystem.AbilityCast += OnAbilityCast;

            Health.AnyDamaged += OnAnyDamaged;

            if (_pauseController != null)
                _pauseController.PauseChanged += OnPauseChanged;
        }

        private void Update()
        {
            if (_recoilActive)
                TickRecoil();
        }

        private void OnDestroy()
        {
            if (_heroShooter != null)
                _heroShooter.ShotFired -= OnShotFired;

            if (_abilitySystem != null)
                _abilitySystem.AbilityCast -= OnAbilityCast;

            Health.AnyDamaged -= OnAnyDamaged;

            if (_pauseController != null)
                _pauseController.PauseChanged -= OnPauseChanged;
        }

        // ----------------------------------------------------------------
        // Internal (для тестов)
        // ----------------------------------------------------------------

        /// <summary>Принудительно задаёт ссылку на HeroShooter (для PlayMode-тестов).</summary>
        internal void InitForTest(
            GameFeelSettings settings,
            HeroShooter shooter,
            CinemachineCamera tpsCam = null,
            AbilitySystem ability = null)
        {
            _settings      = settings;
            _heroShooter   = shooter;
            _tpsCamera     = tpsCam;
            _abilitySystem = ability;

            // Тесты стреляют синхронно после SetUp — Start ещё не выполнился,
            // подписываемся немедленно (Start защищён флагом от дубля).
            Subscribe();
        }

        /// <summary>Текущий Y-офсет рекойла (для тестов).</summary>
        internal float RecoilCurrentY => _recoilCurrentY;

        // ----------------------------------------------------------------
        // Обработчики событий
        // ----------------------------------------------------------------

        private void OnShotFired(Vector3 origin, Vector3 end, bool hit)
        {
            if (_settings == null) return;

            bool overcharged = _heroShooter != null && _heroShooter.IsOvercharged;
            float amplitude = _settings.shotImpulseAmplitude
                              * (overcharged ? _settings.overchargeRecoilMult : 1f);

            _recoilCurrentY += amplitude;
            _recoilActive   = true;

            // Звук с питч-вариацией
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayHeroShotFeel(origin, _settings.shotPitchVariance);
        }

        private void OnAbilityCast(int slot, AbilityData data)
        {
            if (_settings == null || data == null) return;

            switch (data.AbilityType)
            {
                case AbilityType.Shockwave:
                    // Шейк камеры
                    _recoilCurrentY += _settings.shockwaveImpulseAmplitude;
                    _recoilActive    = true;

                    // Hitstop
                    TryStartHitstop();
                    break;

                case AbilityType.RepairField:
                    // Зелёный флэш союзников в радиусе
                    if (_abilitySystem != null)
                    {
                        ApplyRepairFlash(data, _abilitySystem.transform.position);
                    }
                    break;

                case AbilityType.Dash:
                    // Трейл дэша
                    if (_dashTrail != null)
                        _dashTrail.TriggerDash(_settings.dashTrailDuration);
                    break;
            }
        }

        private void OnAnyDamaged(Health health, float amount)
        {
            if (_settings == null || health == null) return;

            // Lazy-добавление новой цели в кэш
            if (!_targetCache.TryGetValue(health, out var cache))
            {
                cache = BuildCacheForHealth(health);
                _targetCache[health] = cache;
            }

            // HitFlash — белый
            if (cache.Handler != null)
                cache.Handler.TriggerFlash(Color.white, _settings.hitFlashDuration);

            // Нокбэк пехоты
            if (_settings.knockbackEnabled && cache.IsPawn && cache.Agent != null)
            {
                ApplyKnockback(health.transform, cache.Agent, _settings.knockbackDistance);
            }
        }

        private void OnPauseChanged(bool isPaused)
        {
            // При паузе прерываем активный hitstop (timeScale уже 0)
            if (isPaused && _hitstopRunning && _hitstopCoroutine != null)
            {
                StopCoroutine(_hitstopCoroutine);
                _hitstopRunning = false;
                Time.timeScale  = 0f; // пауза устанавливает 0 — уже выставлено PauseController
            }
        }

        // ----------------------------------------------------------------
        // Рекойл камеры
        // ----------------------------------------------------------------

        private void TickRecoil()
        {
            if (_settings == null) return;

            float decay = _recoilActive && _recoilCurrentY != 0f
                ? _settings.shotImpulseDecay
                : _settings.shockwaveImpulseDecay;

            // Экспоненциальный спад: x_new = Lerp(x, 0, 1 - exp(-decay*dt)).
            // Математика спада живёт независимо от камеры: применение к FollowOffset —
            // опциональный шаг (камеры может не быть в тестах или при смене режима).
            float dt     = Time.deltaTime;
            float factor = 1f - Mathf.Exp(-decay * dt);
            _recoilCurrentY = Mathf.Lerp(_recoilCurrentY, 0f, factor);

            bool applyToCamera = _baseOffsetCached && _tpsFollow != null;
            if (applyToCamera)
                _tpsFollow.FollowOffset = _baseFollowOffset + new Vector3(0f, _recoilCurrentY, 0f);

            if (Mathf.Abs(_recoilCurrentY) < 0.001f)
            {
                _recoilCurrentY = 0f;
                _recoilActive   = false;
                if (applyToCamera)
                    _tpsFollow.FollowOffset = _baseFollowOffset;
            }
        }

        // ----------------------------------------------------------------
        // Hitstop
        // ----------------------------------------------------------------

        private void TryStartHitstop()
        {
            if (_settings == null || !_settings.hitstopEnabled) return;

            // Guard: пауза
            if (Time.timeScale < 0.1f) return;

            // Guard: batch-mode (интеграционные тесты)
            if (Application.isBatchMode) return;

            if (_hitstopRunning && _hitstopCoroutine != null)
                StopCoroutine(_hitstopCoroutine);

            _hitstopCoroutine = StartCoroutine(HitstopCoroutine());
        }

        private IEnumerator HitstopCoroutine()
        {
            _hitstopRunning = true;
            Time.timeScale  = _settings.hitstopTargetScale;

            // Ждём реального времени (не игрового)
            yield return new WaitForSecondsRealtime(_settings.hitstopDuration);

            // Восстанавливаем только если не на паузе
            if (Time.timeScale < 0.1f && _pauseController != null && _pauseController.IsPaused)
            {
                // Пауза успела включиться — не трогаем timeScale
            }
            else
            {
                Time.timeScale = 1f;
            }

            _hitstopRunning = false;
        }

        // ----------------------------------------------------------------
        // RepairFlash союзников
        // ----------------------------------------------------------------

        private static readonly List<Unit> _repairBuffer = new List<Unit>(64);

        private void ApplyRepairFlash(AbilityData data, Vector3 center)
        {
            if (_settings == null) return;

            UnitRegistry.GetUnits(Faction.Player, _repairBuffer);
            float radiusSqr = data.EffectRadius * data.EffectRadius;

            for (int i = 0; i < _repairBuffer.Count; i++)
            {
                var unit = _repairBuffer[i];
                if (unit == null) continue;

                if ((unit.transform.position - center).sqrMagnitude > radiusSqr) continue;

                var health = unit.CachedHealth;
                if (health == null) continue;

                if (!_targetCache.TryGetValue(health, out var cache))
                {
                    cache = BuildCacheForHealth(health);
                    _targetCache[health] = cache;
                }

                if (cache.Handler != null)
                    cache.Handler.TriggerFlash(
                        _settings.repairFlashColor,
                        _settings.repairFlashDuration);
            }
        }

        // ----------------------------------------------------------------
        // Нокбэк
        // ----------------------------------------------------------------

        private static void ApplyKnockback(Transform target, NavMeshAgent agent, float distance)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            // Направление от центра (0,0,0) к цели, или случайное
            Vector3 dir = target.forward * -1f; // отталкиваем назад
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.back;
            dir.y = 0f;
            dir.Normalize();

            Vector3 candidate = target.position + dir * distance;

            // SamplePosition-страховка
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, distance + 0.5f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        // ----------------------------------------------------------------
        // Построение кэша
        // ----------------------------------------------------------------

        private void BuildTargetCache()
        {
            var allUnits = UnitRegistry.AllUnits;
            for (int i = 0; i < allUnits.Count; i++)
            {
                var unit = allUnits[i];
                if (unit == null) continue;
                var h = unit.CachedHealth;
                if (h == null) continue;
                if (!_targetCache.ContainsKey(h))
                    _targetCache[h] = BuildCacheForHealth(h);
            }
        }

        private static TargetCache BuildCacheForHealth(Health health)
        {
            var cache = new TargetCache();
            cache.Handler = health.GetComponent<HitFlashHandler>();
            cache.Agent   = health.GetComponent<NavMeshAgent>();
            cache.IsPawn  = health.GetComponent<Unit>() != null;
            return cache;
        }
    }
}
