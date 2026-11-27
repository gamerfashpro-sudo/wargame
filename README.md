# WARGAME

Военная стратегия-песочница для Windows на Unity 6 (URP, C#): строительство государства, экономика, логистика, армия, сражения с ИИ, дипломатия и полноценный Sandbox Mode. Сложность средняя: немного аркадности, но не «казуалка».

## Текущий статус

| Часть | Состояние |
|---|---|
| Репозиторий, структура папок | ✅ готово |
| Библиотека 3D-ассетов: конвейер, шаблоны, правила | ✅ готово |
| Первая модель: основной боевой танк `MBT_Main` | ✅ готов в Blender и FBX, ⏳ проверка в Unity |
| Unity Phase 1, задача 01: запуск, настройки (5 вкладок, сохранение), переназначение клавиш, синтез UI-звуков, главное меню (Новая игра, Настройки, Об игре, Авторы, Выход), пауза, Editor-настройка проекта | ✅ написано, ⏳ **не проверено в Unity** |
| Задачи 02 (модели), 03 (мир, камера, HUD), 04 (проверка в Unity) | ⏳ `Documentation/Prompts/` |

Временные решения:
- Фон главного меню и игровая сцена — временные (закат, холмы, танк, облёт камеры). Настоящий мир — задача 03.
- В «Новой игре» одна карта — «Остров Сторм».

## Как открыть в Unity

1. Unity Hub → **Add project from disk** → папка репозитория. Версия: **Unity 6.3 LTS (6000.3.29f1)**; 6.6 тоже подойдёт (Hub предложит сменить версию — соглашайтесь).
2. На вопрос про новую систему ввода (Input System, перезапуск редактора) — **Yes**.
3. После импорта автоматически запустится настройка проекта (в консоли строки `[WARGAME] ✔ …`): URP, уровни качества, материалы, TMP, сцены, Build Settings, реестр моделей. Вручную — меню **WARGAME → Setup Project**.
4. Откройте `Assets/_Project/Scenes/MainMenu/MainMenu.unity` → **Play**.
5. Если в консоли красные ошибки — скопируйте их в чат (или используйте промпт `Documentation/Prompts/04_UNITY_CHECK_FIX.md`).

## Разработка по задачам

Контекст и решения — `Documentation/DEV_CONTEXT.md`, исходное ТЗ — `Documentation/ORIGINAL_BRIEF.md`. Готовые промпты для новых чатов — `Documentation/Prompts/` (01 и 02 параллельно → 03 → 04).

## Структура

```
Assets/_Project/        игровой проект Unity (Art, Prefabs, Scenes, Scripts, ScriptableObjects…)
BlenderAssets/
  Source/               исходники .blend по категориям
  Exports/FBX/          экспорт FBX для Unity
  Exports/Textures/     текстуры
  Templates/            шаблоны Template_*.blend
  Scripts/              Python-скрипты: конвейер, модели, текстуры, инструменты проверки
  Documentation/        ASSET_CATALOG.md, MODELING_GUIDELINES.md, превью и отчёты
Documentation/          документация по игре
ASSET_LICENSES.md       лицензии сторонних материалов
```

## 3D-модели

Каждая модель — это скрипт `BlenderAssets/Scripts/models/<Имя>.py`, исходник `.blend` (редактируется в Blender 5.3+) и экспорт `.fbx`. Пересобрать модель:

```
pip install bpy==5.3.0 numpy pillow
python3 BlenderAssets/Scripts/models/MBT_Main.py
```

Правила — `BlenderAssets/Documentation/MODELING_GUIDELINES.md`, каталог — `BlenderAssets/Documentation/ASSET_CATALOG.md`.

## Git

- Бинарные файлы (.blend, .fbx, .png, .wav и т. д.) хранятся **обычным Git, без LFS**: облачная среда разработки не имеет сетевого доступа к хранилищу LFS. Лимит GitHub — 100 МБ на файл. Модели и текстуры держим в пределах нескольких МБ.
- Если позже понадобится LFS (квота GitHub Free: 10 ГиБ хранения и 10 ГиБ трафика в месяц), инструкция есть в `.gitattributes`.
- В репозиторий не попадают Library/, Temp/, Logs/, Builds/, UserSettings/ и файлы IDE.
- Готовые сборки .exe публикуются через GitHub Releases, а не коммитом.
