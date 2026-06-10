using DiplomaGame.Runtime.Core;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Корень HUD. Хранит ссылки на RTS-блок и TPS-блок и переключает их
    /// в ответ на GameModeController.ModeChanged.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private GameObject         rtsBlock;
        [SerializeField] private GameObject         tpsBlock;
        [SerializeField] private GameModeController modeController;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            if (modeController != null)
                modeController.ModeChanged += OnModeChanged;
        }

        private void Start()
        {
            // Применяем текущий режим при старте
            if (modeController != null)
                ApplyMode(modeController.CurrentMode);
        }

        private void OnDestroy()
        {
            if (modeController != null)
                modeController.ModeChanged -= OnModeChanged;
        }

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализация без зависимостей сцены — для PlayMode-тестов.
        /// </summary>
        internal void InitForTest(GameObject rts, GameObject tps, GameModeController controller)
        {
            rtsBlock       = rts;
            tpsBlock       = tps;
            modeController = controller;
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void OnModeChanged(GameMode mode)
        {
            ApplyMode(mode);
        }

        private void ApplyMode(GameMode mode)
        {
            bool isRts = mode == GameMode.Rts;

            if (rtsBlock != null)
                rtsBlock.SetActive(isRts);

            if (tpsBlock != null)
                tpsBlock.SetActive(!isRts);
        }
    }
}
