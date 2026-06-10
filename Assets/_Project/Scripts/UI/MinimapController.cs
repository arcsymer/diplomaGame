using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Управляет миникартой: ортографическая камера сверху рендерит в RenderTexture,
    /// которую отображает RawImage в углу экрана.
    /// </summary>
    public sealed class MinimapController : MonoBehaviour
    {
        [SerializeField] private Camera    minimapCamera;
        [SerializeField] private RawImage  minimapDisplay;

        // Используется для проверки (Forge проставляет RT)
        public Camera MinimapCamera => minimapCamera;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            if (minimapCamera != null && minimapDisplay != null)
                minimapDisplay.texture = minimapCamera.targetTexture;
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        internal void InitForTest(Camera cam, RawImage display)
        {
            minimapCamera  = cam;
            minimapDisplay = display;
        }
    }
}
