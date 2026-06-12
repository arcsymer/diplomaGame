using DiplomaGame.Runtime.Data;
using UnityEngine;

namespace DiplomaGame.Runtime.Core.Localization
{
    /// <summary>
    /// MonoBehaviour-загрузчик системы локализации.
    /// Добавляется на объект GameManagers (или аналог) в Sandbox и MainMenu.
    /// Awake: инициализирует LocService и загружает язык из PlayerPrefs.
    /// </summary>
    public sealed class LocServiceBootstrap : MonoBehaviour
    {
        [SerializeField] private LocTable _locTable;

        private void Awake()
        {
            LocService.Initialize(_locTable);
            LocService.LoadLanguage();
        }
    }
}
