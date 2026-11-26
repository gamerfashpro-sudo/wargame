"""Создаёт шаблоны BlenderAssets/Templates/Template_*.blend.

Запуск:  python3 BlenderAssets/Scripts/make_templates.py
     или blender -b -P BlenderAssets/Scripts/make_templates.py
"""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import bpy  # noqa: E402
import wg_pipeline as wg  # noqa: E402

# имя -> (размер габаритного бокса (x, y, z) в метрах, подсказка, список подвижных частей)
TEMPLATES = {
    "Vehicle":     ((3.6, 7.0, 2.4), "ОБТ ~7x3.6x2.4 м, перед в -Y",
                    ["Hull", "Turret", "Main_Gun", "Muzzle", "Left_Tracks", "Right_Tracks", "Details"]),
    "Building":    ((12.0, 12.0, 9.0), "3 этажа по 3 м, участок 12x12 м",
                    ["Structure", "Roof", "Windows", "Doors", "Details", "TeamColor"]),
    "Aircraft":    ((11.0, 17.0, 4.5), "Истребитель ~17x11 м, нос в -Y",
                    ["Fuselage", "Wings", "Tail", "Canopy", "Gear", "Propeller_01", "Rotor_Main", "Muzzle"]),
    "Ship":        ((14.0, 90.0, 20.0), "Корабль ~90x14 м, ватерлиния на Z=0, нос в -Y",
                    ["Hull", "Superstructure", "Turret", "Main_Gun", "Radar", "Launcher", "Details"]),
    "Environment": ((6.0, 6.0, 12.0), "Дерево до 12 м / камень / пропс",
                    ["Trunk", "Foliage", "Details"]),
}


def build(kind, bbox, hint, parts):
    wg.new_scene()
    model = wg.collection(f"{kind}_Model")
    helpers = wg.collection(f"{kind}_Helpers")

    root = wg.root_empty(f"NEW_{kind}", model)
    for part in parts:
        e = bpy.data.objects.new(part, None)
        e.empty_display_type = "PLAIN_AXES"
        e.empty_display_size = 0.5
        model.objects.link(e)
        e.parent = root

    # габаритный ориентир (не экспортируется)
    ref = wg.box("REF_Bounds", bbox, (0, 0, bbox[2] / 2), helpers)
    ref.display_type = "WIRE"
    ref.hide_render = True
    # человек 1.8 м для масштаба
    human = wg.box("REF_Human_1.8m", (0.5, 0.3, 1.8), (bbox[0] / 2 + 1.0, 0, 0.9), helpers)
    human.display_type = "WIRE"
    human.hide_render = True
    # стрелка «перед» (-Y)
    arrow = wg.cylinder("REF_Front_-Y", 0.15, 1.5, (0, -bbox[1] / 2 - 1.0, 0.1), axis="Y",
                        verts=8, col=helpers, radius2=0.0)
    arrow.hide_render = True

    # палитра материалов — сохраняется в шаблоне (fake user)
    for name in wg.PALETTE:
        wg.material(name).use_fake_user = True

    note = bpy.data.texts.new("README_TEMPLATE")
    note.write(
        f"Шаблон WARGAME: {kind}\n{hint}\n\n"
        "1. File > Save As -> BlenderAssets/Source/<Категория>/<Имя>.blend\n"
        f"2. Переименуй NEW_{kind} в имя модели; пустышки частей замени мешами (имена сохрани).\n"
        "3. Материалы бери только WG_* (уже в файле).\n"
        "4. Перед смотрит в -Y, origin корня на земле, масштаб применён.\n"
        "5. Экспорт — wg_pipeline.export_fbx(); правила — Documentation/MODELING_GUIDELINES.md\n"
    )
    wg.save_blend(wg.BLENDER_ASSETS / "Templates" / f"Template_{kind}.blend")


if __name__ == "__main__":
    for k, (bbox, hint, parts) in TEMPLATES.items():
        build(k, bbox, hint, parts)
        print("OK", k)
