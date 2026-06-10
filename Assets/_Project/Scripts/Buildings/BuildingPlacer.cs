using System;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiplomaGame.Runtime.Buildings
{
    /// <summary>
    /// Режим строительства: призрак здания следует за мышью,
    /// LMB на валидной позиции инстанцирует настоящий префаб.
    /// Активен только в RTS-режиме. Горячие клавиши: B (Barracks), E (Extractor), Escape/RMB — отмена.
    /// </summary>
    public sealed class BuildingPlacer : MonoBehaviour
    {
        [SerializeField] private ResourceBank    _bank;
        [SerializeField] private GameModeController _modeController;

        [Header("Building Data")]
        [SerializeField] private BuildingData    _barracksData;
        [SerializeField] private BuildingData    _extractorData;

        [Header("Prefabs")]
        [SerializeField] private GameObject      _barracksPrefab;
        [SerializeField] private GameObject      _extractorPrefab;

        [Header("Ghost Materials")]
        [SerializeField] private Material        _ghostValid;
        [SerializeField] private Material        _ghostInvalid;

        // ----------------------------------------------------------------
        // События (M7 Audio шина)
        // ----------------------------------------------------------------

        /// <summary>Вызывается при успешной постройке здания. Параметр — мировая позиция.</summary>
        public event Action<Vector3> BuildingPlaced;

        /// <summary>Вызывается при попытке постройки при нехватке ресурсов.</summary>
        public event Action PlacementFailed;

        // ----------------------------------------------------------------
        // Состояние
        // ----------------------------------------------------------------

        private bool         _isPlacing;
        private BuildingData _currentData;
        private GameObject   _currentPrefab;
        private GameObject   _ghostInstance;

        // Маска для OverlapBox: слои Buildings + Units (по умолчанию — Default + все)
        private static readonly int PlacementMask = ~0;

        private const float NodeSearchRadius = 5f;
        private const float GhostGroundY     = 0f; // высота по умолчанию при отсутствии raycast

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Update()
        {
            if (_modeController != null && _modeController.CurrentMode != GameMode.Rts)
            {
                if (_isPlacing) CancelPlacement();
                return;
            }

            HandleHotkeys();

            if (_isPlacing)
                UpdateGhost();
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает true, пока активен режим размещения здания.
        /// Используется PauseController для блокировки паузы в том же кадре.
        /// </summary>
        public bool IsPlacing => _isPlacing;

        /// <summary>Начинает режим размещения для заданного типа здания.</summary>
        public void StartPlacement(BuildingData data, GameObject prefab)
        {
            CancelPlacement();

            _currentData   = data;
            _currentPrefab = prefab;
            _isPlacing     = true;

            CreateGhost(prefab);
        }

        /// <summary>Отменяет режим размещения и уничтожает призрак.</summary>
        public void CancelPlacement()
        {
            _isPlacing     = false;
            _currentData   = null;
            _currentPrefab = null;

            if (_ghostInstance != null)
            {
                Destroy(_ghostInstance);
                _ghostInstance = null;
            }
        }

        // ----------------------------------------------------------------
        // Горячие клавиши
        // ----------------------------------------------------------------

        private void HandleHotkeys()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.bKey.wasPressedThisFrame && _barracksData != null && _barracksPrefab != null)
            {
                StartPlacement(_barracksData, _barracksPrefab);
                return;
            }

            if (Keyboard.current.eKey.wasPressedThisFrame && _extractorData != null && _extractorPrefab != null)
            {
                StartPlacement(_extractorData, _extractorPrefab);
                return;
            }

            if (_isPlacing)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    CancelPlacement();
                    return;
                }

                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
                {
                    CancelPlacement();
                    return;
                }
            }
        }

        // ----------------------------------------------------------------
        // Обновление призрака
        // ----------------------------------------------------------------

        private void UpdateGhost()
        {
            if (_ghostInstance == null) return;

            Vector3 worldPos = GetMouseWorldPosition();
            _ghostInstance.transform.position = worldPos;

            bool valid = IsCurrentPositionValid(worldPos);
            ApplyGhostMaterial(valid);

            // LMB для подтверждения
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (valid)
                    ConfirmPlacement(worldPos);
                // Если невалидно — не ставим, продолжаем размещение
            }
        }

        private bool IsCurrentPositionValid(Vector3 worldPos)
        {
            if (_currentData == null || _currentPrefab == null) return false;

            // Проверяем пересечение с другими объектами
            var  bounds      = GetPrefabBounds(_currentPrefab);
            bool overlaps    = Physics.CheckBox(worldPos + bounds.center, bounds.extents * 0.9f,
                                               Quaternion.identity, PlacementMask,
                                               QueryTriggerInteraction.Ignore);

            bool needsNode   = _currentData.BuildingType == BuildingType.Extractor;
            bool hasNode     = needsNode && HasNodeNearby(worldPos, NodeSearchRadius);

            return PlacementLogic.IsPlacementValid(overlaps, needsNode, hasNode);
        }

        private void ConfirmPlacement(Vector3 worldPos)
        {
            if (_bank == null)
            {
                Debug.LogWarning("[BuildingPlacer] ResourceBank не задан.");
                CancelPlacement();
                return;
            }

            // Пытаемся списать стоимость
            // Определяем фракцию игрока (всегда Player для BuildingPlacer)
            if (!_bank.TrySpend(Units.Faction.Player, _currentData.Cost))
            {
                Debug.Log($"[BuildingPlacer] Недостаточно ресурсов для {_currentData.DisplayName} (cost={_currentData.Cost}).");
                PlacementFailed?.Invoke();
                return;
            }

            var go = Instantiate(_currentPrefab, worldPos, Quaternion.identity);
            go.SetActive(true);
            BuildingPlaced?.Invoke(worldPos);

            // Фракция у нового инстанса — Player (сцен-инстансы врага расставляются через Forge).
            // Данные и фракция уже задаются в Building._faction через серийный префаб,
            // но для динамически поставленного здания гарантируем через InitForTest.
            var building = go.GetComponent<Building>();
            if (building != null)
                building.InitForTest(_currentData, Units.Faction.Player, _bank);

            CancelPlacement();
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private void CreateGhost(GameObject prefab)
        {
            _ghostInstance = Instantiate(prefab);
            _ghostInstance.name = "Ghost_" + prefab.name;

            // Отключаем все MonoBehaviour-компоненты и коллайдеры у призрака
            foreach (var mb in _ghostInstance.GetComponentsInChildren<MonoBehaviour>())
                mb.enabled = false;

            foreach (var col in _ghostInstance.GetComponentsInChildren<Collider>())
                col.enabled = false;
        }

        private void ApplyGhostMaterial(bool valid)
        {
            if (_ghostInstance == null) return;

            var mat = valid ? _ghostValid : _ghostInvalid;
            if (mat == null) return;

            foreach (var mr in _ghostInstance.GetComponentsInChildren<MeshRenderer>())
                mr.sharedMaterial = mat;
        }

        private static Vector3 GetMouseWorldPosition()
        {
            if (Camera.main == null || Mouse.current == null)
                return Vector3.zero;

            var mousePos = Mouse.current.position.ReadValue();
            var ray      = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                return hit.point;

            // Fallback: пересечение с горизонтальной плоскостью Y=0
            float t = -ray.origin.y / ray.direction.y;
            return t > 0f ? ray.origin + ray.direction * t : Vector3.zero;
        }

        private static Bounds GetPrefabBounds(GameObject prefab)
        {
            // Пытаемся получить bounds из первого Renderer; fallback — unit cube
            var renderers = prefab.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                // bounds в мировых координатах; нам нужны локальные размеры
                return new Bounds(Vector3.zero, b.size);
            }

            return new Bounds(Vector3.zero, Vector3.one * 2f);
        }

        private static bool HasNodeNearby(Vector3 origin, float radius)
        {
            var colliders = Physics.OverlapSphere(origin, radius);
            foreach (var col in colliders)
            {
                if (col.GetComponent<Economy.ResourceNode>() != null)
                    return true;
            }

            return false;
        }
    }
}
