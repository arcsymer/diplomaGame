using UnityEngine;

namespace DiplomaGame.Runtime.Data
{
    /// <summary>
    /// ScriptableObject — таблица локализации.
    /// Содержит массив LocEntry (key / ru / en).
    /// Один ассет на весь проект: Assets/_Project/Data/Localization/LocTable.asset.
    /// </summary>
    [CreateAssetMenu(menuName = "DiplomaGame/Loc Table", fileName = "LocTable")]
    public sealed class LocTable : ScriptableObject
    {
        [SerializeField] private LocEntry[] _entries = new LocEntry[0];

        /// <summary>Все записи таблицы (read-only снаружи).</summary>
        public LocEntry[] Entries => _entries;
    }
}
