using UnityEngine;

namespace DiplomaGame.Runtime.GameFeel
{
    /// <summary>
    /// Кратковременный флэш цвета на всех рендерерах Visual-потомка через MaterialPropertyBlock.
    /// Не аллоцирует в Update: таймер + преаллоцированный MPB.
    /// Рендереры кэшируются в Awake через поиск в дочернем объекте "Visual",
    /// включая SkinnedMeshRenderer.
    /// </summary>
    public sealed class HitFlashHandler : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Кэш рендереров и исходных цветов
        // ----------------------------------------------------------------

        private Renderer[]           _renderers;
        private Color[]              _originalColors;
        private MaterialPropertyBlock _mpb;

        // ----------------------------------------------------------------
        // Состояние таймера
        // ----------------------------------------------------------------

        private float _flashTimer;
        private float _flashDuration;
        private Color _flashColor;
        private bool  _flashing;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            CacheRenderers();
        }

        private void Update()
        {
            if (!_flashing) return;

            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _flashTimer = 0f;
                _flashing   = false;
                RestoreColors();
            }
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>Запускает флэш указанным цветом на duration секунд.</summary>
        public void TriggerFlash(Color color, float duration)
        {
            if (_renderers == null || _renderers.Length == 0)
                return;

            _flashColor    = color;
            _flashDuration = duration;
            _flashTimer    = duration;
            _flashing      = true;

            ApplyColor(color);
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void CacheRenderers()
        {
            // Ищем дочерний объект "Visual"; если нет — берём рендереры прямо с GO
            var visualTf = transform.Find("Visual");
            Transform searchRoot = visualTf != null ? visualTf : transform;

            var renderers = searchRoot.GetComponentsInChildren<Renderer>(true);
            // Фильтруем: MeshRenderer и SkinnedMeshRenderer
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is MeshRenderer || renderers[i] is SkinnedMeshRenderer)
                    count++;
            }

            _renderers      = new Renderer[count];
            _originalColors = new Color[count];
            int idx = 0;

            foreach (var r in renderers)
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    _renderers[idx] = r;
                    // Кэшируем исходный _BaseColor через MPB; если нет — fallback белый
                    r.GetPropertyBlock(_mpb);
                    if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                        _originalColors[idx] = r.sharedMaterial.GetColor("_BaseColor");
                    else
                        _originalColors[idx] = Color.white;
                    idx++;
                }
            }

            // Сбрасываем MPB после чтения
            _mpb.Clear();
        }

        private void ApplyColor(Color color)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                _mpb.Clear();
                if (_renderers[i].sharedMaterial != null &&
                    _renderers[i].sharedMaterial.HasProperty("_BaseColor"))
                {
                    _mpb.SetColor("_BaseColor", color);
                }
                _renderers[i].SetPropertyBlock(_mpb);
            }
        }

        private void RestoreColors()
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                _mpb.Clear();
                if (_renderers[i].sharedMaterial != null &&
                    _renderers[i].sharedMaterial.HasProperty("_BaseColor"))
                {
                    _mpb.SetColor("_BaseColor", _originalColors[i]);
                }
                _renderers[i].SetPropertyBlock(_mpb);
            }
        }
    }
}
