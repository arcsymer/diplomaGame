using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Главный Editor-тул проекта. Всё, что требует ручной работы в редакторе,
    /// делается кнопкой здесь — одноразовые скрипты запрещены (см. CLAUDE.md).
    /// </summary>
    public class ProjectForge : EditorWindow
    {
        private List<IForgeTab> _tabs;
        private int _selectedTab;

        [MenuItem("Tools/Project Forge")]
        public static void OpenWindow()
        {
            var window = GetWindow<ProjectForge>("Project Forge");
            window.minSize = new Vector2(480, 320);
            window.Show();
        }

        private void OnEnable()
        {
            _tabs = BuildTabList();
            _selectedTab = 0;
        }

        private List<IForgeTab> BuildTabList()
        {
            return new List<IForgeTab>
            {
                new ScenesTab(),
                new BuildTab(),
                new ValidationTab(),
                new ReportsTab(),
                new PrefabsTab(),
                new ManagersTab(),
                new ConfigTab(),
                new NavMeshTab(),
                new StubTab("UI",        "M6"),
                new StubTab("Audio",     "M7"),
                new StubTab("VFX",       "M8"),
            };
        }

        private void OnGUI()
        {
            if (_tabs == null)
                OnEnable();

            DrawToolbar();
            GUILayout.Space(6);
            DrawSelectedTab();
        }

        private void DrawToolbar()
        {
            var titles = new string[_tabs.Count];
            for (int i = 0; i < _tabs.Count; i++)
                titles[i] = _tabs[i].Title;

            // Переносим строку, если вкладок слишком много для одной строки
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _selectedTab = GUILayout.Toolbar(_selectedTab, titles, EditorStyles.toolbarButton);
            }
        }

        private void DrawSelectedTab()
        {
            if (_selectedTab >= 0 && _selectedTab < _tabs.Count)
                _tabs[_selectedTab].OnGUI();
        }
    }
}
