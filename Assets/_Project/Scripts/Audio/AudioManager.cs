using System.Collections;
using System.Collections.Generic;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Commands;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.Selection;
using DiplomaGame.Runtime.Units;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace DiplomaGame.Runtime.Audio
{
    /// <summary>
    /// Центральный аудио-менеджер проекта (M7).
    /// Отвечает за: музыкальные состояния с кроссфейдом, SFX-пул, UI-звуки, голоса.
    /// Синглтон без DontDestroyOnLoad — в каждой сцене свой инстанс на GameManagers (или AudioSystem).
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Синглтон
        // ----------------------------------------------------------------

        public static AudioManager Instance { get; private set; }

        // ----------------------------------------------------------------
        // Сериализованные поля — проставляются через Forge или Inspector
        // ----------------------------------------------------------------

        [Header("Mixer (опционально — если null, используется fallback-режим)")]
        [SerializeField] private AudioMixer _mixer;

        [Header("Mixer Groups")]
        [SerializeField] private AudioMixerGroup _musicGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;
        [SerializeField] private AudioMixerGroup _voiceGroup;

        [Header("Музыка")]
        [SerializeField] private AudioClip _menuMusic;
        [SerializeField] private AudioClip _ambientMusic;
        [SerializeField] private AudioClip _combatMusic;

        [Header("SFX — выстрелы героя")]
        [SerializeField] private AudioClip[] _heroShot;

        [Header("SFX — выстрелы юнитов")]
        [SerializeField] private AudioClip[] _unitShot;

        [Header("SFX — взрывы/смерти")]
        [SerializeField] private AudioClip[] _explosion;

        [Header("SFX — постройка здания")]
        [SerializeField] private AudioClip[] _buildPlace;

        [Header("SFX — попадания (тяжёлые)")]
        [SerializeField] private AudioClip[] _hitHeavy;

        [Header("SFX — попадания (лёгкие)")]
        [SerializeField] private AudioClip[] _hitLight;

        [Header("UI-звуки")]
        [SerializeField] private AudioClip[] _uiClick;
        [SerializeField] private AudioClip[] _uiBack;
        [SerializeField] private AudioClip[] _uiConfirm;
        [SerializeField] private AudioClip[] _uiError;

        [Header("Голоса — подтверждения юнитов")]
        [SerializeField] private AudioClip[] _unitAck;

        [Header("Голоса — GameOver")]
        [SerializeField] private AudioClip _victoryClip;
        [SerializeField] private AudioClip _defeatClip;

        [Header("Голоса — Match Start (M9)")]
        [SerializeField] private AudioClip _matchStartClip;

        // ----------------------------------------------------------------
        // Fallback-громкости (когда mixer == null)
        // ----------------------------------------------------------------

        private float _masterVol   = 1f;
        private float _musicVol    = 1f;
        private float _sfxVol      = 1f;
        private float _uiVol       = 1f;
        private float _voiceVol    = 1f;

        // ----------------------------------------------------------------
        // Музыкальные источники (кроссфейд)
        // ----------------------------------------------------------------

        private AudioSource _musicA;
        private AudioSource _musicB;
        private bool        _musicAActive = true;   // какой из двух сейчас активен

        private MusicState  _currentMusicState = (MusicState)(-1); // невалидное начальное
        private Coroutine   _crossfadeCoroutine;

        // ----------------------------------------------------------------
        // SFX-пул
        // ----------------------------------------------------------------

        private const int SfxPoolSize = 8;
        private AudioSource[] _sfxPool;
        private int           _sfxPoolIndex;

        // ----------------------------------------------------------------
        // Выделенные источники
        // ----------------------------------------------------------------

        private AudioSource _uiSource;
        private AudioSource _voiceSource;

        // ----------------------------------------------------------------
        // Состояние боя (для детекции Music Combat)
        // ----------------------------------------------------------------

        private float _lastHeroShotTime = -999f;
        private const float HeroShotCombatWindow = 4f;
        private const float CombatCheckInterval  = 0.5f;
        private float _combatCheckTimer;

        // ----------------------------------------------------------------
        // Rate-limit для голосовых подтверждений
        // ----------------------------------------------------------------

        /// <summary>Минимальный интервал между голосовыми подтверждениями (секунды).</summary>
        internal const float AckRateLimit = 1.5f;
        private float _lastAckTime = -999f;

        // ----------------------------------------------------------------
        // Предыдущие индексы (для PickRandomIndex)
        // ----------------------------------------------------------------

        private int _prevHeroShot    = -1;
        private int _prevUnitShot    = -1;
        private int _prevExplosion   = -1;
        private int _prevBuildPlace  = -1;
        private int _prevUiClick     = -1;
        private int _prevUiConfirm   = -1;
        private int _prevUiError     = -1;
        private int _prevUnitAck     = -1;

        // ----------------------------------------------------------------
        // Буфер зданий (без аллокаций)
        // ----------------------------------------------------------------

        private readonly List<Building> _buildingBuffer = new List<Building>(32);

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BuildSources();
            SubscribeToEvents();

            // Определяем начальное состояние по активной сцене
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "MainMenu")
                SetMusicState(MusicState.Menu);
            else
                SetMusicState(MusicState.Ambient);
        }

        private void Update()
        {
            // Определяем состояние боя каждые 0.5 сек
            _combatCheckTimer += Time.deltaTime;
            if (_combatCheckTimer >= CombatCheckInterval)
            {
                _combatCheckTimer = 0f;
                UpdateCombatState();
            }

            // Обновляем AudioListener.volume в fallback-режиме
            if (_mixer == null)
                AudioListener.volume = _masterVol;
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();

            if (Instance == this)
                Instance = null;
        }

        // ----------------------------------------------------------------
        // Публичный API — музыка
        // ----------------------------------------------------------------

        /// <summary>Переключает музыкальное состояние идемпотентно (с кроссфейдом 1.5 с).</summary>
        public void SetMusicState(MusicState state)
        {
            if (_currentMusicState == state) return;
            _currentMusicState = state;

            AudioClip clip = state switch
            {
                MusicState.Menu    => _menuMusic,
                MusicState.Ambient => _ambientMusic,
                MusicState.Combat  => _combatMusic,
                _                  => null,
            };

            if (clip == null) return;

            if (_crossfadeCoroutine != null)
                StopCoroutine(_crossfadeCoroutine);

            _crossfadeCoroutine = StartCoroutine(CrossfadeMusic(clip));
        }

        // ----------------------------------------------------------------
        // Публичный API — SFX
        // ----------------------------------------------------------------

        /// <summary>Воспроизводит случайный клип из массива variants (2D или позиционно).</summary>
        public void PlaySfx(AudioClip[] variants, Vector3? worldPos = null, float volume = 1f)
        {
            if (variants == null || variants.Length == 0) return;

            int idx = AudioLogic.PickRandomIndex(variants.Length, -1);
            var clip = variants[idx];
            if (clip == null) return;

            var source = GetNextSfxSource();
            source.clip                  = clip;
            source.volume                = volume * (_mixer == null ? _sfxVol * _masterVol : 1f);
            source.spatialBlend          = worldPos.HasValue ? 1f : 0f;
            source.outputAudioMixerGroup = _sfxGroup;

            if (worldPos.HasValue)
                source.transform.position = worldPos.Value;

            source.Play();
        }

        /// <summary>Воспроизводит UI-звук (2D, группа UI).</summary>
        public void PlayUiClick()
        {
            PlayUiClipFrom(_uiClick, ref _prevUiClick);
        }

        public void PlayUiBack()
        {
            PlayUiClipFrom(_uiBack, ref _prevUiClick);
        }

        public void PlayUiConfirm()
        {
            PlayUiClipFrom(_uiConfirm, ref _prevUiConfirm);
        }

        public void PlayUiError()
        {
            PlayUiClipFrom(_uiError, ref _prevUiError);
        }

        // ----------------------------------------------------------------
        // Публичный API — GameOver
        // ----------------------------------------------------------------

        public void PlayVictory()
        {
            if (_victoryClip == null) return;
            PlayVoiceClip(_victoryClip);
        }

        public void PlayDefeat()
        {
            if (_defeatClip == null) return;
            PlayVoiceClip(_defeatClip);
        }

        /// <summary>
        /// Воспроизводит звук старта матча (M9).
        /// Если задан _matchStartClip — играет его, иначе молча завершается.
        /// </summary>
        public void PlayMatchStart()
        {
            if (_matchStartClip == null) return;
            PlayVoiceClip(_matchStartClip);
        }

        // ----------------------------------------------------------------
        // Публичный API — громкости (для SettingsService)
        // ----------------------------------------------------------------

        public enum VolumeCategory { Master, Music, Sfx, Ui, Voice }

        /// <summary>
        /// Устанавливает громкость категории.
        /// Если mixer подключён — пишет в экспонированный параметр.
        /// Иначе сохраняет в fallback-поле и применяет к source.volume напрямую.
        /// </summary>
        public void SetCategoryVolume(VolumeCategory cat, float value01)
        {
            float db = AudioLogic.VolumeToDb(value01);

            if (_mixer != null)
            {
                string paramName = cat switch
                {
                    VolumeCategory.Master => "MasterVol",
                    VolumeCategory.Music  => "MusicVol",
                    VolumeCategory.Sfx    => "SfxVol",
                    VolumeCategory.Ui     => "UiVol",
                    VolumeCategory.Voice  => "VoiceVol",
                    _                     => null,
                };
                if (paramName != null)
                    _mixer.SetFloat(paramName, db);
            }
            else
            {
                // Fallback — храним и применяем к источникам
                switch (cat)
                {
                    case VolumeCategory.Master: _masterVol = value01; break;
                    case VolumeCategory.Music:  _musicVol  = value01; ApplyMusicVolumeFallback(); break;
                    case VolumeCategory.Sfx:    _sfxVol    = value01; break;
                    case VolumeCategory.Ui:     _uiVol     = value01; ApplyUiVolumeFallback();    break;
                    case VolumeCategory.Voice:  _voiceVol  = value01; ApplyVoiceVolumeFallback(); break;
                }
            }
        }

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализирует менеджер с фейковыми клипами для PlayMode-тестов.
        /// Не вызывает Start/SubscribeToEvents.
        /// </summary>
        internal void InitForTest(
            AudioClip menuClip, AudioClip ambientClip, AudioClip combatClip,
            AudioClip[] unitAck = null)
        {
            _menuMusic    = menuClip;
            _ambientMusic = ambientClip;
            _combatMusic  = combatClip;
            _unitAck      = unitAck ?? System.Array.Empty<AudioClip>();

            BuildSources();
            // Сбрасываем состояние, чтобы SetMusicState сработал
            _currentMusicState = (MusicState)(-1);
        }

        /// <summary>Текущее музыкальное состояние (для тестирования SetMusicState).</summary>
        public MusicState CurrentMusicState => _currentMusicState;

        /// <summary>Время последнего голосового подтверждения (для тестирования rate-limit).</summary>
        internal float LastAckTime
        {
            get => _lastAckTime;
            set => _lastAckTime = value;
        }

        /// <summary>Прямое воспроизведение подтверждения (с проверкой rate-limit). Возвращает true если сыграло.</summary>
        internal bool TryPlayUnitAck()
        {
            return PlayUnitAckInternal();
        }

        // ----------------------------------------------------------------
        // Приватные методы — инициализация
        // ----------------------------------------------------------------

        private void BuildSources()
        {
            // 2 источника музыки
            _musicA = CreateSource("MusicA", _musicGroup, loop: true);
            _musicB = CreateSource("MusicB", _musicGroup, loop: true);

            // SFX-пул
            _sfxPool = new AudioSource[SfxPoolSize];
            for (int i = 0; i < SfxPoolSize; i++)
                _sfxPool[i] = CreateSource($"SFX_{i}", _sfxGroup, loop: false);

            // UI и Voice
            _uiSource    = CreateSource("UI",    _uiGroup,    loop: false);
            _voiceSource = CreateSource("Voice", _voiceGroup, loop: false);
        }

        private AudioSource CreateSource(string goName, AudioMixerGroup group, bool loop)
        {
            var go  = new GameObject($"[Audio] {goName}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake          = false;
            src.loop                 = loop;
            src.outputAudioMixerGroup = group;
            return src;
        }

        private void SubscribeToEvents()
        {
            // HeroShooter — подписываемся через поиск в сцене
            var heroShooter = Object.FindFirstObjectByType<HeroShooter>();
            if (heroShooter != null)
                heroShooter.ShotFired += OnHeroShotFired;

            // SelectionSystem
            var selectionSystem = Object.FindFirstObjectByType<SelectionSystem>();
            if (selectionSystem != null)
                selectionSystem.SelectionChanged += OnSelectionChanged;

            // CommandInput
            var commandInput = Object.FindFirstObjectByType<CommandInput>();
            if (commandInput != null)
                commandInput.OrderIssued += OnOrderIssued;

            // BuildingPlacer
            var placer = Object.FindFirstObjectByType<BuildingPlacer>();
            if (placer != null)
            {
                placer.BuildingPlaced  += OnBuildingPlaced;
                placer.PlacementFailed += OnPlacementFailed;
            }

            // Health.AnyDied — статическое событие
            Health.AnyDied += OnAnyHealthDied;

            // UnitCombat.AnyAttacked — статическое событие
            UnitCombat.AnyAttacked += OnAnyUnitAttacked;

            // ProductionBuilding — все существующие в сцене
            SubscribeToExistingProductionBuildings();

            // Новые здания через BuildingRegistry
            BuildingRegistry.BuildingRegistered += OnBuildingRegistered;
        }

        private void UnsubscribeFromEvents()
        {
            var heroShooter = Object.FindFirstObjectByType<HeroShooter>();
            if (heroShooter != null)
                heroShooter.ShotFired -= OnHeroShotFired;

            var selectionSystem = Object.FindFirstObjectByType<SelectionSystem>();
            if (selectionSystem != null)
                selectionSystem.SelectionChanged -= OnSelectionChanged;

            var commandInput = Object.FindFirstObjectByType<CommandInput>();
            if (commandInput != null)
                commandInput.OrderIssued -= OnOrderIssued;

            var placer = Object.FindFirstObjectByType<BuildingPlacer>();
            if (placer != null)
            {
                placer.BuildingPlaced  -= OnBuildingPlaced;
                placer.PlacementFailed -= OnPlacementFailed;
            }

            Health.AnyDied       -= OnAnyHealthDied;
            UnitCombat.AnyAttacked -= OnAnyUnitAttacked;

            BuildingRegistry.BuildingRegistered -= OnBuildingRegistered;

            // Отписываемся от ProductionBuilding
            UnsubscribeFromAllProductionBuildings();
        }

        private void SubscribeToExistingProductionBuildings()
        {
            var buildings = Object.FindObjectsByType<ProductionBuilding>(FindObjectsSortMode.None);
            foreach (var pb in buildings)
                pb.UnitProduced += OnUnitProduced;
        }

        private void UnsubscribeFromAllProductionBuildings()
        {
            var buildings = Object.FindObjectsByType<ProductionBuilding>(FindObjectsSortMode.None);
            foreach (var pb in buildings)
                pb.UnitProduced -= OnUnitProduced;
        }

        // ----------------------------------------------------------------
        // Обработчики событий
        // ----------------------------------------------------------------

        private void OnHeroShotFired(Vector3 origin, Vector3 end, bool hit)
        {
            _lastHeroShotTime = Time.time;
            PlaySfxVariant(_heroShot, ref _prevHeroShot, origin);
        }

        private void OnSelectionChanged()
        {
            // Играем ack только если выделены юниты (не здания)
            var sel = Object.FindFirstObjectByType<SelectionSystem>();
            if (sel != null && sel.Selected.Count > 0)
                PlayUnitAckInternal();
        }

        private void OnOrderIssued(Vector3 pos, UnitCommandType type)
        {
            PlayUiClick();
            PlayUnitAckInternal();
        }

        private void OnBuildingPlaced(Vector3 pos)
        {
            PlaySfxVariant(_buildPlace, ref _prevBuildPlace, pos);
        }

        private void OnPlacementFailed()
        {
            PlayUiError();
        }

        private void OnAnyHealthDied(Health health)
        {
            if (health == null) return;
            Vector3 pos = health.transform.position;
            PlaySfxVariant(_explosion, ref _prevExplosion, pos);
        }

        private void OnAnyUnitAttacked(Vector3 pos)
        {
            PlaySfxVariant(_unitShot, ref _prevUnitShot, pos);
        }

        private void OnUnitProduced(Unit unit)
        {
            PlayUiConfirm();
        }

        private void OnBuildingRegistered(Building building)
        {
            if (building == null) return;
            var pb = building.GetComponent<ProductionBuilding>();
            if (pb != null)
                pb.UnitProduced += OnUnitProduced;
        }

        // ----------------------------------------------------------------
        // Определение состояния боя
        // ----------------------------------------------------------------

        private void UpdateCombatState()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName == "MainMenu")
            {
                SetMusicState(MusicState.Menu);
                return;
            }

            bool inCombat = IsInCombat();
            SetMusicState(inCombat ? MusicState.Combat : MusicState.Ambient);
        }

        private bool IsInCombat()
        {
            // Герой стрелял недавно?
            if (Time.time - _lastHeroShotTime < HeroShotCombatWindow)
                return true;

            // Есть ли юнит игрока, который атакует или преследует?
            var allUnits = UnitRegistry.AllUnits;
            for (int i = 0; i < allUnits.Count; i++)
            {
                var u = allUnits[i];
                if (u == null || u.Faction != Faction.Player) continue;

                var combat = u.GetComponent<UnitCombat>();
                if (combat == null) continue;

                if (combat.CurrentCombatState == CombatState.Attacking ||
                    combat.CurrentCombatState == CombatState.Engaging)
                    return true;
            }

            return false;
        }

        // ----------------------------------------------------------------
        // Кроссфейд
        // ----------------------------------------------------------------

        private IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            const float FadeDuration = 1.5f;

            var incoming = _musicAActive ? _musicB : _musicA;
            var outgoing = _musicAActive ? _musicA : _musicB;

            float targetVol = _mixer == null ? _musicVol * _masterVol : 1f;

            incoming.clip   = newClip;
            incoming.volume = 0f;
            incoming.Play();

            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FadeDuration);

                outgoing.volume = Mathf.Lerp(targetVol, 0f, t);
                incoming.volume = Mathf.Lerp(0f, targetVol, t);
                yield return null;
            }

            outgoing.Stop();
            outgoing.clip   = null;
            incoming.volume = targetVol;

            _musicAActive = !_musicAActive;
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private AudioSource GetNextSfxSource()
        {
            if (_sfxPool == null || _sfxPool.Length == 0) return null;
            var src = _sfxPool[_sfxPoolIndex];
            _sfxPoolIndex = (_sfxPoolIndex + 1) % _sfxPool.Length;
            return src;
        }

        private void PlaySfxVariant(AudioClip[] clips, ref int prevIndex, Vector3 pos)
        {
            if (clips == null || clips.Length == 0) return;

            int idx = AudioLogic.PickRandomIndex(clips.Length, prevIndex);
            prevIndex = idx;

            var clip = clips[idx];
            if (clip == null) return;

            var src = GetNextSfxSource();
            if (src == null) return;

            src.clip                  = clip;
            src.volume                = _mixer == null ? _sfxVol * _masterVol : 1f;
            src.spatialBlend          = 1f;
            src.transform.position    = pos;
            src.outputAudioMixerGroup = _sfxGroup;
            src.Play();
        }

        private void PlayUiClipFrom(AudioClip[] clips, ref int prevIndex)
        {
            if (clips == null || clips.Length == 0) return;
            if (_uiSource == null) return;

            int idx = AudioLogic.PickRandomIndex(clips.Length, prevIndex);
            prevIndex = idx;

            var clip = clips[idx];
            if (clip == null) return;

            _uiSource.outputAudioMixerGroup = _uiGroup;
            _uiSource.volume = _mixer == null ? _uiVol * _masterVol : 1f;
            _uiSource.PlayOneShot(clip);
        }

        private void PlayVoiceClip(AudioClip clip)
        {
            if (clip == null || _voiceSource == null) return;
            _voiceSource.outputAudioMixerGroup = _voiceGroup;
            _voiceSource.volume = _mixer == null ? _voiceVol * _masterVol : 1f;
            _voiceSource.PlayOneShot(clip);
        }

        private bool PlayUnitAckInternal()
        {
            if (_unitAck == null || _unitAck.Length == 0) return false;
            if (_voiceSource == null) return false;

            float now = Time.time;
            if (now - _lastAckTime < AckRateLimit) return false;

            _lastAckTime = now;

            int idx = AudioLogic.PickRandomIndex(_unitAck.Length, _prevUnitAck);
            _prevUnitAck = idx;

            var clip = _unitAck[idx];
            if (clip == null) return false;

            _voiceSource.outputAudioMixerGroup = _voiceGroup;
            _voiceSource.volume = _mixer == null ? _voiceVol * _masterVol : 1f;
            _voiceSource.PlayOneShot(clip);
            return true;
        }

        // ----------------------------------------------------------------
        // Fallback-применение громкостей
        // ----------------------------------------------------------------

        private void ApplyMusicVolumeFallback()
        {
            float vol = _musicVol * _masterVol;
            if (_musicA != null) _musicA.volume = vol;
            if (_musicB != null) _musicB.volume = vol;
        }

        private void ApplyUiVolumeFallback()
        {
            if (_uiSource != null) _uiSource.volume = _uiVol * _masterVol;
        }

        private void ApplyVoiceVolumeFallback()
        {
            if (_voiceSource != null) _voiceSource.volume = _voiceVol * _masterVol;
        }
    }
}
