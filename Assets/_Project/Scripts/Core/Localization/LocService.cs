using System;
using System.Collections.Generic;
using DiplomaGame.Runtime.Data;
using UnityEngine;

namespace DiplomaGame.Runtime.Core.Localization
{
    /// <summary>
    /// Статический сервис локализации.
    /// CurrentLanguage хранится в PlayerPrefs под ключом "Settings.Language" ("ru"/"en").
    /// Get(key) возвращает строку в текущем языке; если ключ не найден — возвращает сам ключ.
    /// Initialize(table) — идемпотентно: повторный вызов с тем же ассетом ничего не делает.
    /// </summary>
    public static class LocService
    {
        private const string PrefKey = "Settings.Language";

        private static LocLanguage _currentLanguage = LocLanguage.Ru;

        // Dictionary-кэш: key → (ru, en)
        private static readonly Dictionary<string, (string ru, string en)> _cache =
            new Dictionary<string, (string, string)>(128, StringComparer.Ordinal);

        // Ссылка на последнюю проинициализированную таблицу (для идемпотентности)
        private static LocTable _initializedTable;

        /// <summary>Вызывается при смене языка. Подписчики обязаны обновить свои тексты.</summary>
        public static event Action LanguageChanged;

        // ----------------------------------------------------------------
        // CurrentLanguage
        // ----------------------------------------------------------------

        /// <summary>Текущий язык интерфейса.</summary>
        public static LocLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage == value) return;
                _currentLanguage = value;
                PlayerPrefs.SetString(PrefKey, value == LocLanguage.En ? "en" : "ru");
                PlayerPrefs.Save();
                LanguageChanged?.Invoke();
            }
        }

        // ----------------------------------------------------------------
        // Initialize
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализирует кэш из LocTable. Идемпотентно: повторный вызов с тем же ассетом пропускается.
        /// Вызывается из LocServiceBootstrap.Awake.
        /// </summary>
        public static void Initialize(LocTable table)
        {
            if (table == null) return;
            if (ReferenceEquals(_initializedTable, table)) return;

            _initializedTable = table;
            _cache.Clear();

            if (table.Entries == null) return;

            foreach (var entry in table.Entries)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;
                _cache[entry.key] = (entry.ru, entry.en);
            }
        }

        // ----------------------------------------------------------------
        // LoadLanguage (вызывается из Bootstrap после Initialize)
        // ----------------------------------------------------------------

        /// <summary>Загружает язык из PlayerPrefs. Вызывается из LocServiceBootstrap.</summary>
        public static void LoadLanguage()
        {
            string saved = PlayerPrefs.GetString(PrefKey, "ru");
            _currentLanguage = saved == "en" ? LocLanguage.En : LocLanguage.Ru;
            // Не стреляем событием — это начальная загрузка, LocalizedText подпишется в OnEnable.
        }

        // ----------------------------------------------------------------
        // Get
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает локализованную строку по ключу.
        /// Fallback: если ключ не найден или строка пуста — возвращает сам ключ.
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;

            if (!_cache.TryGetValue(key, out var pair)) return key;

            string text = _currentLanguage == LocLanguage.En
                ? (string.IsNullOrEmpty(pair.en) ? pair.ru : pair.en)
                : pair.ru;

            return string.IsNullOrEmpty(text) ? key : text;
        }

        // ----------------------------------------------------------------
        // Internal — для тестов (сброс состояния)
        // ----------------------------------------------------------------

        /// <summary>Сбрасывает сервис в начальное состояние. Только для EditMode-тестов.</summary>
        internal static void ResetForTests()
        {
            _cache.Clear();
            _initializedTable = null;
            _currentLanguage  = LocLanguage.Ru;
            LanguageChanged   = null;
        }
    }
}
