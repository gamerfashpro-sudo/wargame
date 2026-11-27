# WARGAME — контекст разработки (читать первым в каждом новом чате)

Этот файл — единый источник принятых решений. Новый чат обязан прочитать его целиком до начала работы и не менять зафиксированные решения без вопроса владельцу.

## 1. Что за проект
- 3D Military RTS Sandbox для Windows (EXE), Unity 6 + C# + URP. Вдохновение — Roblox City Wars (Ostro Games), но **ничего не копировать** (модели, UI, код, название).
- Островное государство: строительство, экономика, логистика, армия (земля/воздух/море/ракеты), ИИ Easy/Normal/Hard, дипломатия, полноценный Sandbox Mode с панелью из 9 вкладок, кинематографичное главное меню на реальной сцене игры, сохранения с версионированием.
- **Сложность: средняя.** Чуть аркадно, но не слишком легко. Не уровень Broken Arrow.
- Полное ТЗ владельца: `Documentation/ORIGINAL_BRIEF.md` (разделы 1–28).
- Референсы UI: тёмные графитовые полупрозрачные панели, оранжево-жёлтый акцент (#F2B33D), белый текст; меню слева (Играть / Настройки / Об игре / Выход), логотип WARGAME со звездой, слоган «СТРОЙ. РАЗВИВАЙСЯ. ПОБЕЖДАЙ.»; HUD: ресурсы сверху слева, время и скорость сверху справа, мини-карта снизу слева, панель строительства снизу справа; настройки — вкладки слева, строки «название < значение >», переключатели, слайдер, кнопки «По умолчанию / Применить / Отмена».

## 2. Решения владельца (утверждены, не пересматривать)
| Вопрос | Решение |
|---|---|
| Старые файлы Voice Checklist в репо | Удалены |
| Язык интерфейса | Русский + английский, переключатель в настройках (по умолчанию русский) |
| 3D-модели | Облачный Blender: `pip install bpy==5.3.0` в среде Claude; .blend/.fbx сразу в репозиторий. Blender 5.3 на ПК владельца — только для просмотра |
| Git LFS | **Не используется** (облако не имеет доступа к хранилищу LFS). Бинарники в обычном Git, лимит 100 МБ/файл |
| Unity | У владельца установлен **Unity 6.6 (6000.6.10f1)**. Рекомендована 6.3 LTS (6000.3.29f1). Код должен работать в обеих: только API, доступные в 6.3. `ProjectSettings/ProjectVersion.txt` = 6000.3.29f1 |
| Режим работы | Работать автономно и много за один заход, не останавливаться после мелочей. Ключевые решения — спрашивать через интерактивные вопросы с вариантами |
| Общение | Коротко, по делу, без воды |

## 3. Ограничения среды Claude (облако)
- Unity в облаке **нет**. Скомпилировать/запустить проект Claude не может. Владелец открывает проект на своём ПК. Если подключён Unity MCP (`com.unity.ai.assistant`, инструменты вида `Unity_ReadConsole`) — использовать его для проверки компиляции.
- Сеть: GitHub (git и raw.githubusercontent.com) и pip работают; nuget, packages.unity.com, download.unity3d.com, LFS — заблокированы. Не обходить.
- Не утверждать, что код скомпилирован, если это не проверено в Unity. Честно писать «не проверено в Unity».

## 4. Репозиторий и Git
- `gamerfashpro-sudo/wargame`, ветка `main`. Клон: `git clone --depth 1 https://github.com/gamerfashpro-sudo/wargame /home/claude/wargame`.
- Перед push: `git pull --rebase origin main` (параллельно могут работать другие чаты). Push: `git -c push.negotiate=true push origin main`.
- Запрещено: force push, `git reset --hard`, удаление истории. Коммиты осмысленные, на русском, логическими группами.
- Не коммитить Library/, Temp/, Logs/, Builds/, UserSettings/, *.blend1.

## 5. Архитектура Unity-проекта (принятые технические решения)
- **Без asmdef**: весь код в Assembly-CSharp (+ папки `Editor/` → Assembly-CSharp-Editor). Так пакеты подключаются автоматически, меньше риск ошибок имён сборок.
- Пространства имён: `WG.Core`, `WG.CameraSystem`, `WG.World`, `WG.UI`, `WG.Audio`, `WG.Weather`, `WG.Building`, `WG.Economy`, `WG.Military`, `WG.AI`, `WG.Sandbox`, `WG.Save`, `WG.EditorTools`.
- **Мир и UI строятся кодом во время выполнения.** Сцены `MainMenu` и `Gameplay` почти пустые; `GameBootstrap` ([RuntimeInitializeOnLoadMethod]) создаёт постоянные сервисы и по имени загруженной сцены собирает меню или игру. Это исключает ручную правку YAML сцен без Unity.
- **Editor-скрипт `WARGAME > Setup Project`** (и автозапуск один раз через [InitializeOnLoad]) создаёт то, что требует редактора: URP-ассеты и рендерер, привязку к уровням качества, материалы `Assets/_Project/Art/Materials/WG_*.mat` по палитре, материалы в `Resources` (террейн, вода, призрак-предпросмотр), сцены `Assets/_Project/Scenes/MainMenu/MainMenu.unity` и `.../Gameplay/Gameplay.unity`, Build Settings, PlayerSettings (WARGAME, Linear, полноэкранный), импорт TMP Essential Resources, `ModelRegistry` (ScriptableObject в Resources: id модели → FBX). Все шаги идемпотентны.
- `AssetPostprocessor` для FBX из `Assets/_Project/Art/Models`: Scale 1, Convert Units, **Bake Axis Conversion = true** (обязательно), Read/Write off, ремап материалов WG_* на проектные `.mat`.
- UI: **uGUI + TextMeshPro, создаётся кодом**. Свои лёгкие контролы (кнопка с hover-анимацией, селектор «< значение >», переключатель, слайдер, вкладки). Шрифты: `Assets/_Project/Resources/Fonts/RussoOne-Regular.ttf` (заголовки), `PT_Sans-Web-Regular/Bold.ttf` (текст); TMP_FontAsset создаётся динамически из Font (кириллица есть).
- Ввод: **Input System** (`Keyboard.current`, `Mouse.current`), EventSystem с `InputSystemUIInputModule` (+ `AssignDefaultActions()`). Переназначаемые клавиши — свой `KeyBindings` (enum действие → `Key`), хранится в настройках.
- Настройки: JSON в `Application.persistentDataPath/settings.json`, применяются сразу и сохраняются между запусками.
- Локализация: `WG.Core.L.T("ключ")`, таблица в `Scripts/Core/Localization.cs` (уже есть). Новые строки добавлять туда же, всегда RU и EN.
- События: `WG.Core.EventBus` (уже есть, `Scripts/Core/EventBus.cs`).
- Растительность и камни: GPU instancing через `Graphics.RenderMeshInstanced` чанками с отсечением по фрустуму и дистанции (не Terrain trees).
- Аудио без сторонних файлов на старте: UI-звуки синтезируются кодом (`AudioClip.Create`). Громкости по категориям — свой `AudioManager` (AudioMixer кодом не создаётся).
- Если модели нет в `ModelRegistry` — код использует запасную геометрию из примитивов, а не падает.
- Палитра UI: фон панелей #14171B (альфа ~0.86), панели светлее #1E2329, разделители #2C333B, акцент #F2B33D, текст #E8E6E1, вторичный #9AA0A6, успех #5FBF6A, опасность #E0563B.

## 6. 3D-библиотека (уже есть)
- Конвейер: `BlenderAssets/Scripts/wg_pipeline.py`; правила: `BlenderAssets/Documentation/MODELING_GUIDELINES.md`; каталог: `BlenderAssets/Documentation/ASSET_CATALOG.md`; проверка FBX без Unity: `BlenderAssets/Scripts/tools/fbx_inspect.py`.
- Соглашения: 1 юнит = 1 м, Z вверх, перед модели в −Y, корень — Empty с именем модели на земле, подвижные части отдельно с origin в точке вращения, экспорт `bake_space_transform=False` (Apply Transform ломает вложенные иерархии — проверено).
- Готово: `MBT_Main` (танк, 5 812 треуг.), 5 шаблонов, камуфляж `T_Camo_Woodland_A`.
- Копии FBX для Unity: `Assets/_Project/Art/Models/<Категория>/`.

## 7. Статус фаз
| Фаза | Статус |
|---|---|
| Репозиторий, структура, 3D-конвейер | ✅ |
| Phase 1 — Unity основа (меню, мир, камера, HUD, настройки) | 🔄 в работе: manifest, ProjectVersion, шрифты, EventBus, локализация готовы |
| Phase 2–8 | ⏳ |

Каждый чат в конце обновляет таблицу статуса здесь и в README.md.
