# WARGAME — ЗАДАЧА 01: PHASE 1, ОСНОВА UNITY-ПРОЕКТА, ГЛАВНОЕ МЕНЮ И НАСТРОЙКИ

## ТВОЯ РОЛЬ
Ты — команда разработки игры WARGAME: Senior Unity Developer, Lead Gameplay Programmer, UI/UX Designer, QA Engineer. Делаешь настоящий рабочий проект.

## КОНТЕКСТ — ОБЯЗАТЕЛЬНО
1. Подключи репозиторий `gamerfashpro-sudo/wargame` (доступ push), клонируй: `git clone --depth 1 https://github.com/gamerfashpro-sudo/wargame /home/claude/wargame`.
2. Прочитай целиком `Documentation/DEV_CONTEXT.md` и `README.md`. Из `Documentation/ORIGINAL_BRIEF.md` прочитай разделы 2, 13, 14, 15, 19, 22, 24, 28. Посмотри `Documentation/reference_ui.png`.
3. Решения DEV_CONTEXT.md утверждены, не меняй без моего согласия.

## ПРАВИЛА
- Коротко, по делу. Автономно и много за раз. Спрашивай только ключевые решения (интерактивно, с вариантами).
- Никаких фиктивных кнопок и заглушек. Не реализованного пункта нет в меню.
- Unity в облаке нет: не утверждай, что проверено в Unity. Только стабильные API Unity 6.3 (совместимые с 6.6). Сомнительное сверяй по исходникам Unity на GitHub.
- Git: `git pull --rebase origin main` перед push, без force push/reset --hard, коммиты на русском, в конце `Co-Authored-By: Claude <noreply@anthropic.com>`.

## ЧТО УЖЕ ЕСТЬ (не переделывать)
`Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, шрифты в `Assets/_Project/Resources/Fonts/`, `Scripts/Core/EventBus.cs`, `Scripts/Core/Localization.cs` (L.T, таблица RU/EN — уже содержит строки меню, настроек, HUD, клавиш; дополняй её), 3D-модель танка `Assets/_Project/Art/Models/Military/Ground/MBT_Main.fbx`.

## ЗАДАЧА
Создай основу Unity-проекта и полностью рабочее главное меню с настройками. Архитектура строго по разделу 5 DEV_CONTEXT.md (без asmdef, мир и UI строятся кодом, Editor-скрипт Setup Project).

### 1. Editor (`Assets/_Project/Scripts/Editor/`)
- `WargameProjectSetup.cs`: меню `WARGAME/Setup Project` + автозапуск один раз ([InitializeOnLoad] + `EditorApplication.delayCall`, флаг в EditorPrefs/файле). Шаги идемпотентны, каждый в try/catch с понятным логом:
  1. URP: `UniversalRendererData` + 4 `UniversalRenderPipelineAsset` (Low/Medium/High/Ultra: разные shadowDistance, разрешение теней через SerializedObject, MSAA, renderScale), сохранить в `Assets/_Project/Settings/`; назначить `GraphicsSettings.defaultRenderPipeline` и `customRenderPipeline` каждому уровню качества через SerializedObject `ProjectSettings/QualitySettings.asset` (существующие уровни не удалять, распределить ассеты по ним).
  2. PlayerSettings: companyName/productName = WARGAME, Linear color space, FullScreenWindow по умолчанию, resizable window, run in background off.
  3. Материалы `Assets/_Project/Art/Materials/WG_*.mat` (URP Lit) по палитре из `BlenderAssets/Scripts/wg_pipeline.py` (PALETTE: WG_Olive, WG_OliveDark, WG_Sand, WG_Graphite, WG_Steel, WG_Rubber, WG_Glass, WG_Concrete, WG_Brick, WG_RoofDark, WG_Accent, WG_TeamColor, WG_Foliage, WG_Bark; цвета там линейные), + `WG_Camo_Woodland` с текстурой `Assets/_Project/Art/Textures/Shared/T_Camo_Woodland_A.png`. enableInstancing = true.
  4. Материалы в `Assets/_Project/Resources/Materials/`: `Terrain_Lit` (Universal Render Pipeline/Terrain/Lit), `Water` (URP Lit transparent, высокая гладкость), `Ghost_Valid`/`Ghost_Invalid` (прозрачные зелёный/красный для предпросмотра строительства в Phase 2), `Unlit_Color`.
  5. TMP Essential Resources: если `TMP_Settings` нет — импортировать (через `TMP_PackageResourceImporter.ImportResources` по рефлексии, иначе `EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources")`).
  6. Сцены `Assets/_Project/Scenes/MainMenu/MainMenu.unity` и `Assets/_Project/Scenes/Gameplay/Gameplay.unity` (пустые, без камеры — всё создаёт код), Build Settings: MainMenu первой.
  7. `ModelRegistry` (ScriptableObject) в `Assets/_Project/Resources/ModelRegistry.asset`: все FBX из `Assets/_Project/Art/Models/**` — id = имя файла → GameObject. Пункт меню `WARGAME/Refresh Model Registry`. После создания WG_*.mat — `SearchAndRemapMaterials` для всех FBX.
- `ModelImportSettings.cs` (AssetPostprocessor) для FBX в `Assets/_Project/Art/Models`: globalScale 1, useFileUnits/Convert Units, **bakeAxisConversion = true**, isReadable = false, animationType None, importCameras/Lights false, materialSearch Everywhere.

### 2. Ядро (`Scripts/Core/`, `Scripts/Audio/`, `Scripts/Input/`)
- `GameBootstrap` ([RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]): постоянный объект `WARGAME_Services` (DontDestroyOnLoad) с сервисами; на `sceneLoaded` по имени сцены вызывает сборку меню (`MainMenu`) или игры (`Gameplay` — пока только камера-заглушка мира не нужна: создай `GameplayEntry`-хук-событие, который заполнит Задача 03; до тех пор в Gameplay показывать ровную землю, небо и рабочее меню паузы Esc с выходом в главное меню).
- `GameSettings` (данные) + `SettingsManager`: JSON в persistentDataPath, Load/Save/Apply/ResetToDefaults, событие SettingsChanged. Применение: разрешение, режим окна (Fullscreen/Borderless/Windowed), VSync, лимит FPS (30/60/120/144/165/240/без), уровень качества, качество текстур (`QualitySettings.globalTextureMipmapLimit`), тени (Off/Low/Medium/High → shadowDistance текущего URP-ассета и тени света), сглаживание (Off/2x/4x/8x MSAA), масштаб рендера (0.5–1.0), дальность прорисовки (farClipPlane, применяет камера), качество эффектов (значение для будущих систем), постобработка вкл/выкл, громкости 5 каналов, язык, масштаб UI (CanvasScaler), видимость элементов HUD, параметры камеры (скорость, поворот, зум, край экрана, инверсия, плавность), скорость игры по умолчанию, пауза при сворачивании, подсказки.
- `KeyBindings`: действия CamForward/Back/Left/Right, CamRotLeft/Right, CamTiltUp/Down, CamFocus, CamFast, Pause, Speed1-3 → `UnityEngine.InputSystem.Key`, дефолты WASD/Q/E/R/F/F/LeftShift/Space/1/2/3, хранятся в настройках.
- `SceneFlow`: загрузка сцен с плавным затемнением (свой Canvas поверх всего, sortingOrder 1000).
- `AudioManager`: громкость по категориям Master/Music/SFX/UI/Ambient; синтезированные UI-звуки (hover — тихий тик, click — щелчок, confirm, back, error) через `AudioClip.Create`; пул AudioSource.

### 3. UI (`Scripts/UI/`) — единая дизайн-система
- `UITheme`: цвета (раздел 5 DEV_CONTEXT), размеры, шрифты (TMP_FontAsset.CreateFontAsset из Font в Resources, кэш), генерация спрайтов скруглённых прямоугольников (9-slice) кодом.
- `UIBuilder`: Canvas (Screen Space Overlay, CanvasScaler 1920×1080, match 0.5), EventSystem с `InputSystemUIInputModule`, панели, тексты, кнопки, разделители, вертикальные/горизонтальные группы, ScrollRect.
- Контролы: `UIButton` (hover: плавный сдвиг/подсветка, акцентная полоска слева, звук; press-анимация; disabled-состояние), `UISelector` («‹ значение ›»), `UIToggle` (переключатель-«таблетка» как на референсе), `UISlider` (свой, перетаскивание), `UITabs`, `UIConfirmDialog`. Все тексты обновляются при смене языка (LanguageChanged).
- `MainMenuController`:
  - Фон: в этой задаче — временный атмосферный фон: камера медленно облетает сцену с танком `MBT_Main` (из ModelRegistry) на холме, закатное освещение, туман, затемнение-виньетка слева под меню. В Задаче 03 фон будет заменён настоящим миром игры — сделай точку расширения `IMenuBackground`.
  - Слева: логотип WARGAME (Russo One, крупно, со звездой и линиями), слоган, кнопки: Играть, Настройки, Об игре, Авторы, Выход. Внизу версия.
  - «Играть» → панель «Новая игра»: название государства (TMP_InputField), цвет государства (6–8 образцов), карта (пока одна: «Остров Сторм»), вариант мира (seed), кнопка «Начать» → `GameSession` (статические параметры новой игры) → SceneFlow в Gameplay. Пунктов Continue/Sandbox/Scenarios пока НЕТ (появятся в своих фазах).
  - «Настройки» → `SettingsPanel` с вкладками Графика / Аудио / Управление / Геймплей / Интерфейс, все пункты из ТЗ раздела 15 и из `GameSettings`; переназначение клавиш (ожидание нажатия, Esc — отмена, конфликт — обмен); кнопки По умолчанию / Применить / Отмена; изменения применяются сразу и сохраняются. Панель переиспользуется в меню паузы.
  - Об игре, Авторы (шрифты OFL указать) — красивые панели. Выход — подтверждение, `Application.Quit()` (в редакторе — выход из Play Mode).
  - Анимации: плавное появление меню, переходы панелей (fade+slide), без раздражающих эффектов.

## ПРОВЕРКА
- Пройди глазами по каждому файлу: using, namespace, совпадение имён методов/классов между файлами, нет вызовов несуществующих API.
- Все строки — через L.T, есть RU и EN.
- Опиши в README: как открыть проект (Unity Hub → Add → папка репозитория), что при первом открытии Input System попросит перезапуск (нажать Yes), что Setup Project выполнится сам (или меню WARGAME/Setup Project), какую сцену открыть.

## В КОНЦЕ
Обнови статус в DEV_CONTEXT.md и README.md, commit + push, короткий отчёт: что сделано, что не проверено в Unity, следующий шаг.
