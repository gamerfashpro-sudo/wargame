# WARGAME — каталог 3D-моделей

Статусы: **Draft** (черновик) · **Ready (Blender)** (готово и проверено в Blender/FBX) · **Ready (Unity)** (проверено в Unity) · **Final**.

| Модель | Категория | Назначение | .blend | .fbx | Текстуры | Треуг. | LOD | Статус | Источник |
|---|---|---|---|---|---|---|---|---|---|
| MBT_Main | Military/Ground | Основной боевой танк. Подвижные части: башня, пушка, 16 колёс, гусеницы; маркеры Muzzle, Exhaust_L/R | `BlenderAssets/Source/Military/Ground/MBT_Main.blend` | `BlenderAssets/Exports/FBX/Military/Ground/MBT_Main.fbx` | `Shared/T_Camo_Woodland_A.png` + палитра WG_* | 5 812 | нет (план: LOD1/LOD2) | Ready (Blender) | Собственная (скрипт `Scripts/models/MBT_Main.py`) |

## Шаблоны

| Шаблон | Путь | Что внутри |
|---|---|---|
| Template_Vehicle | `BlenderAssets/Templates/Template_Vehicle.blend` | Коллекции Model/Helpers, пустышки Hull/Turret/Main_Gun/Muzzle/Tracks, габарит 3.6×7×2.4 м, человек 1.8 м, стрелка «перед», палитра WG_* |
| Template_Building | `BlenderAssets/Templates/Template_Building.blend` | Structure/Roof/Windows/Doors/TeamColor, участок 12×12 м |
| Template_Aircraft | `BlenderAssets/Templates/Template_Aircraft.blend` | Fuselage/Wings/Tail/Canopy/Gear/Rotor/Propeller, габарит 11×17 м |
| Template_Ship | `BlenderAssets/Templates/Template_Ship.blend` | Hull/Superstructure/Turret/Radar/Launcher, ватерлиния Z=0 |
| Template_Environment | `BlenderAssets/Templates/Template_Environment.blend` | Trunk/Foliage/Details |

## Общие текстуры

| Текстура | Путь | Размер | Источник |
|---|---|---|---|
| T_Camo_Woodland_A | `BlenderAssets/Exports/Textures/Shared/T_Camo_Woodland_A.png` | 1024², бесшовная | Собственная, процедурная (`Scripts/textures/make_camo.py`) |

## Очередь моделей (порядок по фазам игры)

1. Здания стартового поселения: жилой дом, склад, электростанция, казармы (Phase 1–2).
2. Грузовик логистики, БТР, лёгкий танк, артиллерия, РСЗО (Phase 2–3).
3. Деревья (2–3 вида), камни, дорожные элементы (Phase 1, наполнение карты).
4. Вертолёт, истребитель, транспортный и военный корабли (Phase 3).
