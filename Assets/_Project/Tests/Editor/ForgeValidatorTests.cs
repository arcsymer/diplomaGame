using System.Collections.Generic;
using NUnit.Framework;
using DiplomaGame.Editor;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode smoke-тесты для ForgeValidator.
    /// Запускаются через Window → General → Test Runner (EditMode).
    /// </summary>
    [TestFixture]
    public class ForgeValidatorTests
    {
        // ------------------------------------------------------------
        // Тест 1: Validate() никогда не возвращает null
        // ------------------------------------------------------------
        [Test]
        public void Validate_ReturnsNonNull()
        {
            List<string> result = ForgeValidator.Validate();
            Assert.IsNotNull(result, "ForgeValidator.Validate() не должен возвращать null.");
        }

        // ------------------------------------------------------------
        // Тест 2: После Bootstrap все обязательные папки существуют
        // ------------------------------------------------------------
        [Test]
        public void Bootstrap_CreatesAllRequiredFolders()
        {
            ForgeValidator.BootstrapProjectStructure();

            string[] required =
            {
                "Assets/_Project/Scripts",
                "Assets/_Project/Scenes",
                "Assets/_Project/Prefabs",
                "Assets/_Project/Data",
                "Assets/_Project/Art",
                "Assets/_Project/Audio",
                "Assets/_Project/UI",
                "Assets/_Project/VFX",
            };

            foreach (var folder in required)
            {
                Assert.IsTrue(
                    UnityEditor.AssetDatabase.IsValidFolder(folder),
                    $"Папка должна существовать после Bootstrap: {folder}");
            }
        }

        // ------------------------------------------------------------
        // Тест 3: После Bootstrap Validate() не сообщает об отсутствии папок
        // ------------------------------------------------------------
        [Test]
        public void Validate_AfterBootstrap_NoFolderIssues()
        {
            ForgeValidator.BootstrapProjectStructure();

            List<string> issues = ForgeValidator.Validate();

            foreach (var issue in issues)
                Assert.IsFalse(
                    issue.StartsWith("Отсутствует папка:"),
                    $"После Bootstrap не должно быть проблем с папками. Найдено: {issue}");
        }
    }
}
