"""WARGAME — общий конвейер Blender для библиотеки 3D-ассетов.

Используется всеми скриптами моделей (BlenderAssets/Scripts/models/*.py) и шаблонов.
Работает и в обычном Blender 5.x, и в модуле `bpy` (pip install bpy==5.3.0).

Соглашения (подробно — Documentation/MODELING_GUIDELINES.md):
  * 1 Blender unit = 1 метр, Z — вверх.
  * Перед модели смотрит в -Y (вид Front в Blender) -> в Unity станет +Z.
  * Origin корня — на земле, по центру опорной площади.
  * Подвижные части — отдельные объекты, origin в точке вращения.
"""
from __future__ import annotations

import json
import math
import os
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

REPO_ROOT = Path(__file__).resolve().parents[2]
BLENDER_ASSETS = REPO_ROOT / "BlenderAssets"
SOURCE_DIR = BLENDER_ASSETS / "Source"
FBX_DIR = BLENDER_ASSETS / "Exports" / "FBX"
TEX_DIR = BLENDER_ASSETS / "Exports" / "Textures"
UNITY_MODELS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "Models"
UNITY_TEX_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "Textures"

# ----------------------------------------------------------------------------
# Единая палитра WARGAME (линейные цвета, sRGB-значения в комментариях)
# ----------------------------------------------------------------------------
PALETTE = {
    "WG_Olive":        ((0.155, 0.170, 0.085, 1), 0.70, 0.0),   # #6E7350 армейский олив
    "WG_OliveDark":    ((0.075, 0.085, 0.045, 1), 0.75, 0.0),
    "WG_Sand":         ((0.430, 0.360, 0.220, 1), 0.80, 0.0),
    "WG_Graphite":     ((0.030, 0.032, 0.036, 1), 0.55, 0.6),   # стволы, металл
    "WG_Steel":        ((0.300, 0.300, 0.310, 1), 0.40, 1.0),
    "WG_Rubber":       ((0.018, 0.018, 0.018, 1), 0.90, 0.0),   # гусеницы, шины
    "WG_Glass":        ((0.020, 0.040, 0.050, 1), 0.08, 0.0),
    "WG_Concrete":     ((0.380, 0.370, 0.350, 1), 0.90, 0.0),
    "WG_Brick":        ((0.300, 0.110, 0.070, 1), 0.85, 0.0),
    "WG_RoofDark":     ((0.050, 0.055, 0.060, 1), 0.70, 0.2),
    "WG_Accent":       ((0.900, 0.480, 0.040, 1), 0.50, 0.0),   # оранжевый акцент UI (#F2B33D)
    "WG_TeamColor":    ((0.800, 0.800, 0.800, 1), 0.60, 0.0),   # перекрашивается в Unity цветом страны
    "WG_Foliage":      ((0.040, 0.090, 0.025, 1), 0.85, 0.0),
    "WG_Bark":         ((0.090, 0.060, 0.035, 1), 0.90, 0.0),
}


# ----------------------------------------------------------------------------
# Сцена
# ----------------------------------------------------------------------------
def new_scene() -> bpy.types.Scene:
    """Чистая сцена с метрическими единицами."""
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = "METERS"
    return scene


def collection(name: str, parent: bpy.types.Collection | None = None) -> bpy.types.Collection:
    col = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    parent = parent or bpy.context.scene.collection
    if col.name not in parent.children:
        parent.children.link(col)
    return col


def link_to(obj: bpy.types.Object, col: bpy.types.Collection) -> bpy.types.Object:
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    return obj


# ----------------------------------------------------------------------------
# Материалы
# ----------------------------------------------------------------------------
def material(name: str, image_path: Path | None = None) -> bpy.types.Material:
    """Материал из палитры WARGAME (Principled BSDF). image_path — альбедо (опционально)."""
    mat = bpy.data.materials.get(name)
    if mat:
        return mat
    color, rough, metal = PALETTE.get(name, ((0.5, 0.5, 0.5, 1), 0.6, 0.0))
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    mat.diffuse_color = color
    if image_path is not None:
        img = bpy.data.images.load(str(image_path), check_existing=True)
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = img
        tex.location = (-400, 200)
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    return mat


def assign(obj: bpy.types.Object, mat: bpy.types.Material) -> None:
    obj.data.materials.clear()
    obj.data.materials.append(mat)


# ----------------------------------------------------------------------------
# Геометрия
# ----------------------------------------------------------------------------
def _new_obj(name: str, bm: bmesh.types.BMesh, col: bpy.types.Collection) -> bpy.types.Object:
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    col.objects.link(obj)
    return obj


def box(name, size, loc=(0, 0, 0), col=None, bevel=0.0, segments=1) -> bpy.types.Object:
    """Параллелепипед size=(x,y,z) с центром в loc. bevel — фаска (м)."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    bmesh.ops.translate(bm, vec=Vector(loc), verts=bm.verts)
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=segments,
                        affect="EDGES", profile=0.5)
    return _new_obj(name, bm, col or bpy.context.scene.collection)


def hull_from_profile(name, profile_yz, width, col=None, bevel=0.0) -> bpy.types.Object:
    """Тело выдавливанием бокового профиля (список (y, z)) на ширину по X."""
    bm = bmesh.new()
    hw = width / 2
    left = [bm.verts.new((-hw, y, z)) for y, z in profile_yz]
    right = [bm.verts.new((hw, y, z)) for y, z in profile_yz]
    bm.faces.new(list(reversed(left)))
    bm.faces.new(right)
    n = len(profile_yz)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((left[i], left[j], right[j], right[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=1, affect="EDGES")
    return _new_obj(name, bm, col or bpy.context.scene.collection)


def cylinder(name, radius, depth, loc=(0, 0, 0), axis="Z", verts=16, col=None,
             radius2=None, cap=True) -> bpy.types.Object:
    """Цилиндр/конус вдоль оси axis с центром в loc."""
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=cap, segments=verts, radius1=radius,
                          radius2=radius if radius2 is None else radius2, depth=depth)
    if axis == "X":
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0),
                         matrix=__import__("mathutils").Matrix.Rotation(math.radians(90), 3, "Y"))
    elif axis == "Y":
        bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0),
                         matrix=__import__("mathutils").Matrix.Rotation(math.radians(90), 3, "X"))
    bmesh.ops.translate(bm, vec=Vector(loc), verts=bm.verts)
    return _new_obj(name, bm, col or bpy.context.scene.collection)


def set_origin(obj: bpy.types.Object, world_point) -> None:
    """Перенести origin в мировую точку (геометрия остаётся на месте).
    Объект должен быть без родителя и без поворота/масштаба."""
    p = Vector(world_point)
    delta = p - obj.location
    obj.data.transform(__import__("mathutils").Matrix.Translation(-delta))
    obj.location = p


def join(name: str, objs: list[bpy.types.Object]) -> bpy.types.Object:
    """Слить объекты в один меш (материалы сохраняются)."""
    target = objs[0]
    bm = bmesh.new()
    mats: list = []
    for o in objs:
        me = o.data.copy()
        me.transform(o.matrix_world)
        offset = len(mats)
        for m in o.data.materials:
            mats.append(m)
        tmp = bmesh.new()
        tmp.from_mesh(me)
        for f in tmp.faces:
            f.material_index += offset
        tmp.to_mesh(me)
        tmp.free()
        bm.from_mesh(me)
        bpy.data.meshes.remove(me)
    col = target.users_collection[0]
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for m in mats:
        me.materials.append(m)
    obj = bpy.data.objects.new(name, me)
    col.objects.link(obj)
    _dedupe_materials(obj)
    return obj


def _dedupe_materials(obj):
    me = obj.data
    uniq, remap = [], {}
    for i, m in enumerate(me.materials):
        if m not in uniq:
            uniq.append(m)
        remap[i] = uniq.index(m)
    for p in me.polygons:
        p.material_index = remap.get(p.material_index, 0)
    me.materials.clear()
    for m in uniq:
        me.materials.append(m)


def smooth_by_angle(obj, angle_deg=35.0):
    """Плоское/гладкое затенение по углу (жёсткие края остаются чёткими)."""
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    lim = math.radians(angle_deg)
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) == 2 and e.calc_face_angle(0) > lim:
            e.smooth = False
    bm.to_mesh(me)
    bm.free()


def uv_unwrap(obj, island_margin=0.02):
    """Smart UV Project для объекта (нужно для текстур и запекания)."""
    bpy.ops.object.select_all(action="DESELECT")
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=island_margin)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.select_set(False)


def parent_keep(child, parent):
    """Привязать child к parent, сохранив мировое положение.
    view_layer.update() обязателен: без него matrix_world родителя устаревшая
    (после смены location/parent) и ребёнок смещается."""
    bpy.context.view_layer.update()
    child.parent = parent
    child.matrix_parent_inverse = parent.matrix_world.inverted()


def root_empty(name: str, col) -> bpy.types.Object:
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = "ARROWS"
    e.empty_display_size = 1.0
    col.objects.link(e)
    return e


# ----------------------------------------------------------------------------
# Сохранение / экспорт / проверка
# ----------------------------------------------------------------------------
def save_blend(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(path), relative_remap=True, compress=True)
    bpy.ops.file.make_paths_relative()
    bpy.ops.wm.save_mainfile(compress=True)


def export_fbx(path: Path, root: bpy.types.Object) -> None:
    """Экспорт иерархии root в FBX с настройками для Unity."""
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in [root, *root.children_recursive]:
        o.hide_set(False)
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        object_types={"EMPTY", "MESH"},
        apply_scale_options="FBX_SCALE_UNITS",
        global_scale=1.0,
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=False,  # True ломает вложенные иерархии (проверено fbx_inspect.py)
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_tspace=True,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="STRIP",
        embed_textures=False,
    )


def validate(root: bpy.types.Object) -> dict:
    """Проверки game-ready: масштаб, UV, материалы, треугольники. Возвращает отчёт."""
    report = {"root": root.name, "objects": [], "tris_total": 0, "problems": []}
    for o in [root, *root.children_recursive]:
        entry = {"name": o.name, "type": o.type,
                 "parent": o.parent.name if o.parent else None}
        if any(abs(s - 1.0) > 1e-4 for s in o.scale):
            report["problems"].append(f"{o.name}: масштаб не применён {tuple(o.scale)}")
        if o.type == "MESH":
            me = o.data
            tris = sum(len(p.vertices) - 2 for p in me.polygons)
            entry["tris"] = tris
            report["tris_total"] += tris
            if not me.uv_layers:
                report["problems"].append(f"{o.name}: нет UV")
            if not me.materials:
                report["problems"].append(f"{o.name}: нет материала")
        report["objects"].append(entry)
    dims_min = Vector((1e9, 1e9, 1e9))
    dims_max = Vector((-1e9, -1e9, -1e9))
    for o in root.children_recursive:
        if o.type != "MESH":
            continue
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            dims_min = Vector(map(min, dims_min, w))
            dims_max = Vector(map(max, dims_max, w))
    report["bounds_m"] = [round(v, 3) for v in (dims_max - dims_min)]
    report["min_z"] = round(dims_min.z, 3)
    return report


def write_report(report: dict, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")


def render_preview(path: Path, target=(0, 0, 1.2), distance=14.0, elev_deg=22.0,
                   azim_deg=-35.0, res=(1280, 720), samples=48, ground_size=200.0) -> None:
    """Быстрый превью-рендер Cycles (CPU): небо + солнце + земля.
    ground_size — сторона плоскости земли (для больших моделей и дальней камеры)."""
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = samples
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = res
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX"
    world = scene.world or bpy.data.worlds.new("WG_PreviewWorld")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    sky = world.node_tree.nodes.new("ShaderNodeTexSky")
    world.node_tree.links.new(sky.outputs["Color"], bg.inputs["Color"])
    bg.inputs["Strength"].default_value = 0.35
    sun_data = bpy.data.lights.new("WG_PreviewSun", "SUN")
    sun_data.energy = 3.5
    sun_data.angle = math.radians(3)
    sun = bpy.data.objects.new("WG_PreviewSun", sun_data)
    sun.rotation_euler = (math.radians(35), 0, math.radians(-30))
    scene.collection.objects.link(sun)
    ground = box("WG_PreviewGround", (ground_size, ground_size, 0.02), (0, 0, -0.011))
    gm = bpy.data.materials.new("WG_PreviewGroundMat")
    gm.use_nodes = True
    gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.12, 0.11, 0.08, 1)
    gm.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.95
    ground.data.materials.append(gm)
    cam_data = bpy.data.cameras.new("WG_PreviewCam")
    cam_data.lens = 50
    cam = bpy.data.objects.new("WG_PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    t = Vector(target)
    el, az = math.radians(elev_deg), math.radians(azim_deg)
    cam.location = t + Vector((math.sin(az) * math.cos(el), -math.cos(az) * math.cos(el),
                               math.sin(el))) * distance
    cam.rotation_euler = (t - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    path.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    # убрать превью-объекты, чтобы они не попали в .blend
    for o in (sun, ground, cam):
        bpy.data.objects.remove(o, do_unlink=True)
