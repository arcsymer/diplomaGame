using System;
using System.Collections;
using System.Collections.Generic;
using DiplomaGame.Runtime.Audio;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.Units;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Runtime.Core
{
    /// <summary>
    /// Арбитр матча. Следит за HQ обеих фракций и объявляет победу/поражение.
    /// Также управляет респауном героя (8 с, телепорт к PlayerBaseSpawn, полное HP).
    /// Размещается на GameManagers.
    /// </summary>
    public sealed class GameWatcher : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные поля
        // ----------------------------------------------------------------

        [SerializeField] private GameOverController _gameOver;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>Вызывается при завершении матча. true — победа игрока.</summary>
        public static event Action<bool> MatchEnded;

        // ----------------------------------------------------------------
        // Внутреннее состояние
        // ----------------------------------------------------------------

        private Health _playerHQHealth;
        private Health _enemyHQHealth;
        private Health _heroHealth;

        private HeroController _heroController;

        // Визуальный дочерний объект героя (для скрытия на время респауна)
        private GameObject _heroVisual;

        // Флаг: WatchHQs вызван напрямую (тесты) — Start не должен искать HQ самостоятельно
        private bool _hqsInitializedExternally;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Start()
        {
            // GameOverController — берём из SerializedObject или ищем в сцене
            if (_gameOver == null)
                _gameOver = UnityEngine.Object.FindFirstObjectByType<GameOverController>();

            // HQ подписка только если не была проставлена извне (через WatchHQs в тестах)
            if (!_hqsInitializedExternally)
                FindAndSubscribeHQs();

            FindAndSubscribeHero();

            AudioManager.Instance?.PlayMatchStart();
        }

        private void OnDestroy()
        {
            UnsubscribeHQs();
            UnsubscribeHero();
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Позволяет напрямую задать GameOverController без SerializedObject (PlayMode-тесты).
        /// </summary>
        internal void InitForTest(GameOverController gameOver)
        {
            _gameOver = gameOver;
        }

        /// <summary>
        /// Подписывается на конкретные Health-компоненты HQ (для PlayMode-тестов).
        /// Устанавливает флаг, который предотвращает повторный поиск в Start().
        /// </summary>
        internal void WatchHQs(Health playerHQ, Health enemyHQ)
        {
            _hqsInitializedExternally = true;

            UnsubscribeHQs();

            _playerHQHealth = playerHQ;
            _enemyHQHealth  = enemyHQ;

            if (_playerHQHealth != null)
                _playerHQHealth.Died += OnPlayerHQDied;

            if (_enemyHQHealth != null)
                _enemyHQHealth.Died += OnEnemyHQDied;
        }

        // ----------------------------------------------------------------
        // Инициализация — поиск HQ
        // ----------------------------------------------------------------

        private void FindAndSubscribeHQs()
        {
            var buildings = new List<Building>(32);

            BuildingRegistry.GetBuildings(Faction.Player, buildings);
            foreach (var b in buildings)
            {
                if (b.Data != null && b.Data.BuildingType == BuildingType.Headquarters)
                {
                    _playerHQHealth = b.GetComponent<Health>();
                    if (_playerHQHealth != null)
                        _playerHQHealth.Died += OnPlayerHQDied;
                    break;
                }
            }

            BuildingRegistry.GetBuildings(Faction.Enemy, buildings);
            foreach (var b in buildings)
            {
                if (b.Data != null && b.Data.BuildingType == BuildingType.Headquarters)
                {
                    _enemyHQHealth = b.GetComponent<Health>();
                    if (_enemyHQHealth != null)
                        _enemyHQHealth.Died += OnEnemyHQDied;
                    break;
                }
            }
        }

        private void UnsubscribeHQs()
        {
            if (_playerHQHealth != null)
                _playerHQHealth.Died -= OnPlayerHQDied;

            if (_enemyHQHealth != null)
                _enemyHQHealth.Died -= OnEnemyHQDied;
        }

        // ----------------------------------------------------------------
        // Инициализация — поиск Героя
        // ----------------------------------------------------------------

        private void FindAndSubscribeHero()
        {
            _heroController = UnityEngine.Object.FindFirstObjectByType<HeroController>();
            if (_heroController == null) return;

            _heroHealth = _heroController.GetComponent<Health>();
            if (_heroHealth == null) return;

            _heroHealth.Died += OnHeroDied;

            // Кэшируем дочерний Visual объект героя (для скрытия)
            var visualTf = _heroController.transform.Find("Visual");
            _heroVisual  = visualTf != null ? visualTf.gameObject : null;
        }

        private void UnsubscribeHero()
        {
            if (_heroHealth != null)
                _heroHealth.Died -= OnHeroDied;
        }

        // ----------------------------------------------------------------
        // Обработчики смерти HQ
        // ----------------------------------------------------------------

        private void OnEnemyHQDied()
        {
            if (_gameOver != null && _gameOver.IsShown) return;
            _gameOver?.ShowVictory();
            MatchEnded?.Invoke(true);
        }

        private void OnPlayerHQDied()
        {
            if (_gameOver != null && _gameOver.IsShown) return;
            _gameOver?.ShowDefeat();
            MatchEnded?.Invoke(false);
        }

        // ----------------------------------------------------------------
        // Обработчик смерти героя → корутина респауна
        // ----------------------------------------------------------------

        private void OnHeroDied()
        {
            StartCoroutine(RespawnHeroCoroutine());
        }

        private IEnumerator RespawnHeroCoroutine()
        {
            const float RespawnDelay = 8f;

            // Скрываем визуал; CharacterController и NavMeshAgent отключаем
            SetHeroActive(false);

            yield return new WaitForSeconds(RespawnDelay);

            // Телепорт к маркеру
            Vector3 spawnPos = GetPlayerBaseSpawnPosition();
            if (_heroController != null)
                _heroController.transform.position = spawnPos;

            // Восстанавливаем здоровье
            if (_heroHealth != null)
                _heroHealth.Init(_heroHealth.MaxHp);

            // Включаем компоненты обратно
            SetHeroActive(true);
        }

        private void SetHeroActive(bool active)
        {
            if (_heroController == null) return;

            // Скрываем/показываем визуальный дочерний объект
            if (_heroVisual != null)
                _heroVisual.SetActive(active);

            // CharacterController (не деактивируем GO — только компонент)
            var cc = _heroController.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = active;

            // NavMeshAgent
            var agent = _heroController.GetComponent<NavMeshAgent>();
            if (agent != null)
                agent.enabled = active;
        }

        private static Vector3 GetPlayerBaseSpawnPosition()
        {
            var marker = GameObject.Find("PlayerBaseSpawn");
            return marker != null ? marker.transform.position : Vector3.zero;
        }
    }
}
