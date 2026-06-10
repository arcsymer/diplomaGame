using UnityEditor;
using UnityEngine;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Заглушка для вкладок, которые будут реализованы в будущих майлстоунах.
    /// </summary>
    internal sealed class StubTab : IForgeTab
    {
        private readonly string _milestone;

        public string Title { get; }

        public StubTab(string title, string milestone)
        {
            Title = title;
            _milestone = milestone;
        }

        public void OnGUI()
        {
            GUILayout.Space(20);
            GUILayout.Label($"Появится в {_milestone}...", EditorStyles.centeredGreyMiniLabel);
        }
    }
}
