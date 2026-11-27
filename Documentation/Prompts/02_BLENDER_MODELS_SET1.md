# WARGAME — ЗАДАЧА 02: 3D-МОДЕЛИ, НАБОР 1 (ЗДАНИЯ, ПРИРОДА, ТЕХНИКА)

## ТВОЯ РОЛЬ
Ты — 3D Environment Designer, Technical Artist и 3D Artist игры WARGAME. Создаёшь собственные game-ready модели в Blender для библиотеки ассетов проекта.

## КОНТЕКСТ — ОБЯЗАТЕЛЬНО
1. Подключи репозиторий `gamerfashpro-sudo/wargame` (доступ push), клонируй: `git clone --depth 1 https://github.com/gamerfashpro-sudo/wargame /home/claude/wargame`.
2. Прочитай целиком `Documentation/DEV_CONTEXT.md`, `BlenderAssets/Documentation/MODELING_GUIDELINES.md`, `BlenderAssets/Documentation/ASSET_CATALOG.md`, `BlenderAssets/Scripts/wg_pipeline.py`, эталонный скрипт `BlenderAssets/Scripts/models/MBT_Main.py`. Из `Documentation/ORIGINAL_BRIEF.md` — разделы 4, 24, 26 и «TASK: CREATE A PROFESSIONAL BLENDER 3D ASSET LIBRARY».
3. Blender — облачный: `pip install --break-system-packages bpy==5.3.0 numpy pillow` (если ещё не стоит), скрипты запускаются `python3`. Всегда `import bpy` ДО `import bmesh`.

## ПРАВИЛА
- Коротко, по делу. Автономно, сделай весь список за один заход. Спрашивай только ключевые решения.
- Только оригинальный дизайн: не воспроизводи реальные защищённые образцы техники и ассеты других игр.
- Не трогай файлы вне `BlenderAssets/` и `Assets/_Project/Art/`.
- Известные ловушки (уже исправлены — не возвращай): экспорт с `bake_space_transform=False` (Apply Transform ломает вложенные части — не меняй настройки экспорта); для привязки перемещённых объектов всегда `wg.parent_keep()` (внутри вызывает `view_layer.update()`).
- В `wg_pipeline.py` можно ДОБАВЛЯТЬ помощники (крыша-призма, арка, конусное дерево, процедурные текстуры), но не менять поведение существующих функций. После любой правки конвейера перезапусти `python3 BlenderAssets/Scripts/models/MBT_Main.py --no-render` — REPORT должен показать 5812 треуг. и пустой список проблем.
- Git: `git pull --rebase origin main` перед push, без force push/reset --hard, коммиты на русском логическими группами (например: здания / природа / техника), в конце `Co-Authored-By: Claude <noreply@anthropic.com>`. Удаляй *.blend1.

## ЗАДАЧА
Для каждой модели — отдельный скрипт `BlenderAssets/Scripts/models/<Имя>.py` по образцу MBT_Main.py (build() + main(): validate → save .blend → export FBX → копия FBX в `Assets/_Project/Art/Models/<Категория>/` → копия текстур в `Assets/_Project/Art/Textures/...` → отчёт JSON → превью-рендер). Соглашения: 1 юнит = 1 м, Z вверх, перед в −Y, корень — Empty с именем модели, origin на земле (min_z = 0), подвижные части — отдельные дочерние объекты с origin в точке вращения, у всех мешей UV и материал WG_*, масштаб применён. Важные объекты детальнее (фаски, слои деталей, правильные пропорции) — не «кучка коробок».

Порядок (здания первыми — они нужны карте):

**Buildings**
1. `House_Small` — 2-этажный дом ~9×11×8 м, скатная крыша (WG_RoofDark), окна (WG_Glass), дверь, крыльцо, труба. 400–1500 треуг.
2. `Apartment_Block` — 5 этажей ~16×12×16 м, плоская крыша с кондиционерами и выходом на крышу, сетка окон, козырёк входа. ≤3000.
3. `Warehouse` — склад ~22×32×9 м, «профлист», 2–3 рулонных ворот, погрузочная рампа. ≤2000.
4. `Factory` — завод ~32×40×12 м, шедовая крыша, труба 28 м (дочерний `Chimney`, маркер-Empty `FX_Smoke` наверху), трубопроводы. ≤4000.
5. `Power_Plant` — ТЭС: главный корпус ~24×30×18 м + градирня ~22 м диаметр × 35 м (маркер `FX_Steam`) + трансформаторный двор. ≤5000.
6. `Barracks` — казармы ~12×32×5 м, флагшток с флагом из WG_TeamColor (дочерний `Flag`). ≤1500.
7. `Hangar` — арочный ангар ~32×40×12 м, открытый фасад, дочерние `Door` панели. ≤2500.
8. `Control_Tower` — диспетчерская вышка, база ~8×8, высота ~22 м, стеклянная кабина, дочерний вращающийся `Radar`. ≤2000.

**Environment/Nature** (дёшево — будут тысячи копий через instancing)
9. `Tree_Pine` — ель ~11 м, ярусы-конусы (WG_Foliage) + ствол (WG_Bark). 150–400.
10. `Tree_Broadleaf` — лиственное ~9 м, low-poly крона из кластеров. 200–500.
11. `Rock_A` — валун ~3×2.5×1.8 м, неровный. 80–300. Для деревьев и камня — один корень и максимум 2 меша (ствол/крона), чтобы их легко было сливать для instancing.

**Military/Ground**
12. `Truck_Military` — 6×6 грузовик ~2.5×8.5×3.2 м, кабина, кузов с тентом, 6 колёс отдельно: `Wheel_FL, Wheel_FR, Wheel_ML, Wheel_MR, Wheel_RL, Wheel_RR` (origin на оси), маркер `Exhaust`. Камуфляж как у MBT_Main (`BlenderAssets/Exports/Textures/Shared/T_Camo_Woodland_A.png`). ≤4000.

**Military/Aircraft**
13. `Fighter_Jet` — одноместный истребитель, размах ~11, длина ~17, высота ~4.5 м, нос в −Y, фонарь (WG_Glass), origin на земле под центром масс, шасси выпущено (дочерний `Gear`), маркеры `Engine_FX`, `Muzzle`. Серая схема (добавь в PALETTE `WG_AirGrey`). ≤5000.

**Military/Naval**
14. `Frigate` — фрегат ~14×100×22 м, ватерлиния на Z=0 (корпус ниже), нос в −Y, дочерние `Turret` (ось вращения вертикальная) → `Main_Gun` (origin в цапфах) → маркер `Muzzle`, вращающийся `Radar`, `Launcher`. Морская серая палитра (`WG_NavalGrey`). ≤8000.

## ПРОВЕРКА КАЖДОЙ МОДЕЛИ
- REPORT без проблем, габариты соответствуют, min_z ≈ 0 (у фрегата отрицательный — это нормально, отметь).
- `python3 BlenderAssets/Scripts/tools/fbx_inspect.py <fbx>`: поворот (−90,0,0) только у корня, иерархия правильная.
- Посмотри превью-рендер (`BlenderAssets/Documentation/Previews/<Имя>.png`) инструментом Read. Висящие части, дыры, чёрные/вывернутые грани, неверные пропорции — исправь и перерендерь. Подбери `target/distance/ground_size` в `render_preview`, чтобы модель целиком была в кадре.
- Строка в `ASSET_CATALOG.md` (те же колонки, что у MBT_Main), убрать модель из «Очереди моделей». Документация на русском.

## В КОНЦЕ
Обнови статус в `Documentation/DEV_CONTEXT.md` (раздел 6 «Готово») и README.md, commit + push. Короткий отчёт таблицей: модель, путь FBX в Assets, треугольники, габариты, имена подвижных частей и маркеров, нерешённые проблемы. Пришли 3–4 лучших превью файлами.
