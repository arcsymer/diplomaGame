using UnityEngine;

namespace DiplomaGame.Runtime.Data
{
    /// <summary>Одна запись таблицы локализации: ключ + русский текст + английский текст.</summary>
    [System.Serializable]
    public sealed class LocEntry
    {
        public string key;

        [TextArea(1, 4)]
        public string ru;

        [TextArea(1, 4)]
        public string en;
    }
}
