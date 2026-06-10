using System.Collections;
using System.IO;
using UnityEngine;

namespace DiplomaGame.Runtime.Core
{
    /// <summary>
    /// Автоматическая съёмка скриншотов для README: активируется только
    /// аргументом командной строки "-autoshot &lt;папка&gt;".
    /// Снимает RTS-вид, переключается в TPS, снимает его и закрывает игру.
    /// </summary>
    public sealed class ScreenshotDirector : MonoBehaviour
    {
        [SerializeField] private GameModeController modeController;

        private void Start()
        {
            string dir = GetAutoshotDir();
            if (string.IsNullOrEmpty(dir)) return;

            Directory.CreateDirectory(dir);
            StartCoroutine(CaptureRoutine(dir));
        }

        private IEnumerator CaptureRoutine(string dir)
        {
            // Даём сцене, HUD и пост-обработке прогреться
            yield return new WaitForSeconds(2.5f);

            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "rts-mode.png"));
            yield return new WaitForSeconds(0.5f);

            if (modeController == null)
                modeController = FindAnyObjectByType<GameModeController>();
            if (modeController != null)
                modeController.SetMode(GameMode.Tps);

            // Ждём 3.5s: бленд Cinemachine занимает ~2с + запас на прогрев TPS HUD.
            yield return new WaitForSeconds(3.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "tps-mode.png"));

            yield return new WaitForSeconds(1f);
            Application.Quit();
        }

        private static string GetAutoshotDir()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-autoshot")
                    return args[i + 1];
            }
            return null;
        }

        /// <summary>
        /// Игра стартует с MainMenu (сцена 0), а директор живёт в Sandbox —
        /// при -autoshot сразу переходим в игровую сцену.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoshotBootstrap()
        {
            if (string.IsNullOrEmpty(GetAutoshotDir())) return;

            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.name == "MainMenu")
                UnityEngine.SceneManagement.SceneManager.LoadScene("Sandbox");
        }
    }
}
