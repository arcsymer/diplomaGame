using System.IO;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Статический класс без состояния. Сбор статистики по проекту.
    /// Вынесен отдельно для тестируемости.
    /// </summary>
    public static class ForgeStats
    {
        private const string ScriptsRoot = "Assets/_Project";
        private const string TestsRoot   = "Assets/_Project/Tests";

        public struct Snapshot
        {
            public int  ScriptCount;
            public long TotalLines;
            public int  SceneCount;
            public int  TestCount;
        }

        /// <summary>
        /// Собирает актуальный срез статистики по файловой системе.
        /// </summary>
        public static Snapshot Collect()
        {
            var snap = new Snapshot
            {
                ScriptCount = CountFiles(ScriptsRoot, "*.cs"),
                TotalLines  = CountLines(ScriptsRoot, "*.cs"),
                SceneCount  = CountFiles("Assets/_Project/Scenes", "*.unity"),
                TestCount   = CountFiles(TestsRoot, "*.cs"),
            };
            return snap;
        }

        // ----------------------------------------------------------------
        // Внутренние утилиты
        // ----------------------------------------------------------------

        private static int CountFiles(string root, string pattern)
        {
            if (!Directory.Exists(root)) return 0;
            return Directory.GetFiles(root, pattern, SearchOption.AllDirectories).Length;
        }

        private static long CountLines(string root, string pattern)
        {
            if (!Directory.Exists(root)) return 0;

            long total = 0;
            foreach (var file in Directory.GetFiles(root, pattern, SearchOption.AllDirectories))
            {
                try
                {
                    total += File.ReadAllLines(file).Length;
                }
                catch
                {
                    // Файл заблокирован или недоступен — пропускаем
                }
            }
            return total;
        }
    }
}
