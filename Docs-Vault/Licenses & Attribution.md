# Licenses & Attribution

> Журнал всех внешних ассетов. **Правило:** ничего без проверенной лицензии. CC-BY → обязательная атрибуция в экране Credits игры.
> Статус «запланировано, не импортировано» — ассет выбран, но ещё не добавлен в проект.

---

## Таблица ассетов

| Ассет | Источник | Лицензия | Атрибуция требуется? | Где используется | Статус |
|---|---|---|---|---|---|
| Kenney — Sci-fi Sounds | [kenney.nl](https://kenney.nl/assets/sci-fi-sounds) | CC0 1.0 | Нет | SFX: выстрелы, взрывы | Импортировано |
| Kenney — Interface Sounds | [kenney.nl](https://kenney.nl/assets/interface-sounds) | CC0 1.0 | Нет | SFX: UI-клики, ошибки | Импортировано |
| Kenney — Impact Sounds | [kenney.nl](https://kenney.nl/assets/impact-sounds) | CC0 1.0 | Нет | SFX: попадания | Импортировано |
| Kenney — Voiceover Pack | [kenney.nl](https://kenney.nl/assets/voiceover-pack) | CC0 1.0 | Нет | Голоса юнитов (ack-реплики) | Импортировано |
| Kenney — Voiceover Pack (Fighter) | [kenney.nl](https://kenney.nl/assets/voiceover-pack-fighter) | CC0 1.0 | Нет | Голоса боевых юнитов | Импортировано |
| Kenney — Blaster Kit | [kenney.nl](https://kenney.nl/assets/blaster-kit) | CC0 1.0 | Нет | 3D-модели оружия | Запланировано, не импортировано |
| Kenney — Space Kit | [kenney.nl](https://kenney.nl/assets/space-kit) | CC0 1.0 | Нет | Пропсы, детали окружения | Запланировано, не импортировано |
| Kenney — UI Pack: Sci-Fi | [kenney.nl](https://kenney.nl/assets/ui-pack-sci-fi) | CC0 1.0 | Нет | UI: панели, кнопки, рамки | Запланировано, не импортировано |
| Kenney — Particle Pack | [kenney.nl](https://kenney.nl/assets/particle-pack) | CC0 1.0 | Нет | VFX: взрывы, вспышки | Запланировано, не импортировано |
| Quaternius — Animated Mech Pack | [quaternius.com](https://quaternius.com/) | CC0 1.0 | Нет | 3D-модели юнитов-мехов | Запланировано, не импортировано |
| Quaternius — Animated Tanks Pack | [quaternius.com](https://quaternius.com/) | CC0 1.0 | Нет | 3D-модели танков | Запланировано, не импортировано |
| Quaternius — Ultimate Modular Sci-Fi | [quaternius.com](https://quaternius.com/) | CC0 1.0 | Нет | 3D-модели зданий базы | Запланировано, не импортировано |
| game-icons.net (набор SVG) | [game-icons.net](https://game-icons.net/) | CC-BY 3.0 | **Да** — в Credits | Иконки способностей и ресурсов | Запланировано, не импортировано |
| Russo One (шрифт) | [fonts.google.com](https://fonts.google.com/specimen/Russo+One) | SIL OFL 1.1 | Нет (файл лицензии в репо) | Заголовки UI | Запланировано, не импортировано |
| Exo 2 (шрифт) | [fonts.google.com](https://fonts.google.com/specimen/Exo+2) | SIL OFL 1.1 | Нет (файл лицензии в репо) | Основной текст UI | Запланировано, не импортировано |
| Kevin MacLeod — "Floating Cities" | [incompetech.com](https://incompetech.com/music/royalty-free/index.html?isrc=USUAN1100588) | CC-BY 4.0 | **Да** — в Credits | Музыка: меню (MainMenu) | Импортировано |
| Kevin MacLeod — "Deep Haze" | [incompetech.com](https://incompetech.com/music/royalty-free/index.html?isrc=USUAN1100313) | CC-BY 4.0 | **Да** — в Credits | Музыка: эмбиент (Ambient) | Импортировано |
| Kevin MacLeod — "Volatile Reaction" | [incompetech.com](https://incompetech.com/music/royalty-free/index.html?isrc=USUAN1100825) | CC-BY 4.0 | **Да** — в Credits | Музыка: бой (Combat) | Импортировано |
| Sonniss GDC Audio Bundle | [gdc.sonniss.com](https://gdc.sonniss.com/) | Royalty-free (без атрибуции) | Нет | SFX: точечные профессиональные звуки | Запланировано, не импортировано |

---

## Правила работы с лицензиями

1. **Ничего не импортировать** без проверенной лицензии. Источник и лицензия фиксируются в этой таблице до добавления файлов в проект.
2. **CC0** — использование без ограничений, атрибуция не требуется, но приветствуется.
3. **CC-BY** — обязательна атрибуция в экране Credits. Шаблон: `"[Название]" by [Автор], [ссылка], CC-BY [версия]`.
4. **SIL OFL** — шрифт можно использовать, встраивать, распространять; файл лицензии (`OFL.txt`) должен лежать рядом с файлами шрифта в репозитории.
5. **CC-BY-NC** — только для некоммерческого использования (учебный диплом допустимо, публичное коммерческое распространение — нет). Помечать отдельно.
6. **Asset Store EULA** — файлы нельзя публиковать в открытом репозитории. Такие ассеты не используем (исключение: Unity Particle Pack, но конкретный статус — уточнить перед добавлением).
7. **Синти POLYGON и аналоги** — исключены по ADR-009 (нулевой бюджет) и ADR-007 (EULA запрещает открытый репо).

---

## Шаблон строки Credits в игре

```
Музыка:
  "Floating Cities" by Kevin MacLeod (incompetech.com)
  Licensed under Creative Commons Attribution 4.0 — https://creativecommons.org/licenses/by/4.0/

  "Deep Haze" by Kevin MacLeod (incompetech.com)
  Licensed under Creative Commons Attribution 4.0 — https://creativecommons.org/licenses/by/4.0/

  "Volatile Reaction" by Kevin MacLeod (incompetech.com)
  Licensed under Creative Commons Attribution 4.0 — https://creativecommons.org/licenses/by/4.0/

Иконки:
  game-icons.net, Lorc, Delapouite и другие авторы
  Licensed under Creative Commons Attribution 3.0
```
