# WARGAME — правила моделирования и экспорта

Единый стандарт для всех 3D-моделей проекта. Общий код конвейера: `BlenderAssets/Scripts/wg_pipeline.py`.

## 1. Где что лежит

| Что | Путь |
|---|---|
| Исходники Blender (.blend) | `BlenderAssets/Source/<Категория>/<Имя>.blend` |
| Скрипт, который строит модель | `BlenderAssets/Scripts/models/<Имя>.py` |
| Шаблоны | `BlenderAssets/Templates/Template_*.blend` |
| Экспорт FBX | `BlenderAssets/Exports/FBX/<Категория>/<Имя>.fbx` |
| Текстуры (оригиналы) | `BlenderAssets/Exports/Textures/<Имя>/` |
| Копия для Unity | `Assets/_Project/Art/Models/<Категория>/<Имя>.fbx` и `Assets/_Project/Art/Textures/<Имя>/` |
| Превью-рендеры | `BlenderAssets/Documentation/Previews/<Имя>.png` |
| Каталог моделей | `BlenderAssets/Documentation/ASSET_CATALOG.md` |

Категории: `Buildings`, `Military/Ground`, `Military/Aircraft`, `Military/Naval`, `Military/Artillery`, `Military/Drones`, `Environment`, `Props`, `Civilian`, `Vehicles`.

Каждая модель воспроизводима: её можно пересобрать скриптом (`python3 BlenderAssets/Scripts/models/<Имя>.py`), а `.blend` можно править вручную в Blender 5.3+. Если правишь `.blend` руками, перед перезаписью сохрани копию `<Имя>_backup_ГГГГММДД.blend` (в Git LFS она не попадает, если не нужна).

## 2. Масштаб и оси

- 1 Blender unit = **1 метр**. Unit Scale = 1.0.
- **Z — вверх.** Перед модели смотрит в **−Y** (вид Front, Numpad 1).
- При экспорте (−Z forward, Y up) + импорте в Unity с **Bake Axis Conversion** модель смотрит в **+Z** с поворотом (0,0,0) и масштабом 1.
- Origin корневого объекта стоит **на земле** (Z = 0), по центру опорной площади.
- На всех объектах масштаб применён (1,1,1). Поворот на корне 0.

Ориентиры размеров: человек 1,8 м; дверь 2,1 × 1 м; этаж 3 м; ОБТ ~7 × 3,6 × 2,4 м (корпус без ствола); грузовик ~9 × 2,5 × 3,2 м; истребитель ~17 × 11 м.

## 3. Структура и имена

Корень — Empty с именем модели (`MBT_Main`). Дочерние объекты — PascalCase с подчёркиваниями:

```
MBT_Main (Empty, origin на земле)
├── Hull
│   ├── Turret        (origin = центр погона, вращение по Z)
│   │   └── Main_Gun  (origin = цапфы, вращение по X)
│   ├── Left_Tracks / Right_Tracks
│   ├── Wheel_L_01..N / Wheel_R_01..N  (origin = ось колеса)
│   └── Details
```

Типовые имена подвижных частей, которые ищут скрипты Unity: `Turret`, `Main_Gun`, `Muzzle` (Empty на конце ствола), `Wheel_*`, `Rotor_Main`, `Rotor_Tail`, `Propeller_*`, `Door_*`, `Radar`, `Launcher`.

Точки-маркеры (Empty): `Muzzle`, `Exhaust_*`, `FX_Smoke`, `Seat_*`. Unity использует их для эффектов выстрела, дыма и т. п.

Коллекции в .blend: `<Имя>_Model` (то, что экспортируется), `<Имя>_Helpers` (габариты, референсы; не экспортируются).

## 4. Геометрия (бюджеты)

| Класс | Треугольники LOD0 |
|---|---|
| Основная техника (танк, самолёт, корабль) | 4 000 – 12 000 |
| Лёгкая техника, грузовики | 2 000 – 6 000 |
| Крупные здания | 1 000 – 6 000 |
| Малые здания, пропсы | 100 – 1 500 |
| Деревья, камни | 150 – 1 200 |

- Фаски (bevel 1–3 см) на видимых рёбрах ради бликов. Подразделения (subdivision) не используем.
- Затенение: smooth по углу ~35°, жёсткие края остаются чёткими.
- Нормали наружу, без n-угольников на кривых поверхностях, без скрытых внутренних граней.
- LOD: `_LOD0`, `_LOD1` (~50%), `_LOD2` (~20%). Делаем для техники и крупных зданий после утверждения LOD0.

## 5. UV и материалы

- У каждого меша есть UV (минимум Smart UV Project, отступ 0,02).
- Материалы — только из палитры `PALETTE` в `wg_pipeline.py` (`WG_Olive`, `WG_Graphite`, `WG_Rubber`, …). Так все модели выглядят в одном стиле и в Unity переиспользуют один набор материалов.
- `WG_TeamColor` — зоны, которые Unity перекрашивает в цвет страны (флажки, полосы, крыши казарм).
- Текстуры: PNG, степень двойки (512/1024/2048). Альбедо в sRGB, остальные карты в Linear. Пути в .blend относительные.

## 6. Экспорт FBX (делает `export_fbx()`)

- Selected objects: корень + все потомки; типы Empty + Mesh.
- Apply Scalings: **FBX Units Scale**; Forward **−Z**; Up **Y**; **Apply Transform выключено**.
  Причина: «Apply Transform» (экспериментальный) ломает вложенные части — башня и пушка получают лишний поворот на 90°. Проверено `Scripts/tools/fbx_inspect.py`. Без него поворот осей лежит только на корне, а Unity убирает его галочкой Bake Axis Conversion.
- Smoothing: Face; Tangent Space включено; Leaf Bones выключено; текстуры не встраиваются (Path Mode: Strip).

## 7. Импорт в Unity (настройки модели)

- Model: Scale Factor 1, **Convert Units** включено, **Bake Axis Conversion включено** (обязательно, иначе корень будет повёрнут на −90° по X).
- Read/Write выключено. Mesh Compression: Low для техники, Medium для окружения.
- Normals: Import; Tangents: Calculate Mikktspace.
- Materials: Location — Use External Materials (Legacy) или ремап на материалы проекта `Assets/_Project/Art/Materials/WG_*.mat`.
- Animation Type: None (подвижные части крутятся скриптами).

> Статус: иерархия, оси и масштаб проверены чтением FBX (`fbx_inspect.py`) и повторным импортом в Blender: все части стоят на своих местах. Проверка в Unity — при первом открытии проекта (Unity пока не установлен). До этой проверки экспорт считается предварительным.

## 8. Чек-лист перед коммитом

1. `validate()` не выдаёт проблем, `fbx_inspect.py` показывает поворот только на корне (масштаб, UV, материалы).
2. Габариты соответствуют реальным (отчёт `bounds_m`).
3. Origin корня на земле, `min_z ≈ 0`.
4. Подвижные части — отдельные объекты с правильными origin.
5. .blend сохранён, FBX экспортирован, текстуры на месте, превью отрендерено.
6. Строка в `ASSET_CATALOG.md` обновлена; сторонний ассет — запись в `ASSET_LICENSES.md`.
