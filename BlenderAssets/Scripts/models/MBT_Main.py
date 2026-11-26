"""MBT_Main — основной боевой танк WARGAME (оригинальный дизайн).

python3 BlenderAssets/Scripts/models/MBT_Main.py [--no-render]

Результат:
  BlenderAssets/Source/Military/Ground/MBT_Main.blend
  BlenderAssets/Exports/FBX/Military/Ground/MBT_Main.fbx
  Assets/_Project/Art/Models/Military/Ground/MBT_Main.fbx (копия для Unity)
  BlenderAssets/Documentation/Previews/MBT_Main.png
Иерархия: MBT_Main > Hull > (Turret > Main_Gun > Muzzle), Wheel_*, Left/Right_Tracks, Details
"""
import math
import shutil
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import bpy  # noqa: E402  (bpy должен импортироваться раньше bmesh)
import bmesh  # noqa: E402
from mathutils import Vector  # noqa: E402

import wg_pipeline as wg  # noqa: E402

NAME = "MBT_Main"
CATEGORY = "Military/Ground"

# --- основные размеры (метры) ---
HULL_LEN = 7.0          # корпус по Y: перед -3.5, корма +3.5
HULL_W = 3.6            # ширина по надгусеничным полкам
TRACK_W = 0.62
TRACK_X = HULL_W / 2 - TRACK_W / 2 - 0.02
TRACK_T = 0.075         # толщина ленты
WHEEL_R = 0.37
WHEEL_Z = WHEEL_R + TRACK_T   # лента касается земли на Z=0
WHEEL_YS = [-2.45, -1.47, -0.49, 0.49, 1.47, 2.45]
IDLER = (-3.12, 0.82, 0.30)      # (y, z, r) — ленивец спереди
SPROCKET = (3.12, 0.84, 0.33)    # ведущее колесо сзади
TURRET_C = Vector((0.0, -0.15, 1.60))   # центр погона


# ----------------------------------------------------------------------------
def track_ring(name, x, col):
    """Гусеница — замкнутая лента вокруг колёс (выпуклая оболочка окружностей)."""
    circles = [(y, WHEEL_Z, WHEEL_R) for y in WHEEL_YS] + [IDLER, SPROCKET]
    n = 72

    def support(theta, extra):
        d = Vector((math.cos(theta), math.sin(theta)))  # (y, z)
        best = max(circles, key=lambda c: c[0] * d.x + c[1] * d.y + c[2] + extra)
        return (best[0] + (best[2] + extra) * d.x, best[1] + (best[2] + extra) * d.y)

    outer = [support(2 * math.pi * i / n, TRACK_T) for i in range(n)]
    inner = [support(2 * math.pi * i / n, 0.0) for i in range(n)]
    hw = TRACK_W / 2
    bm = bmesh.new()
    lo = [bm.verts.new((x - hw, y, z)) for y, z in outer]
    ro = [bm.verts.new((x + hw, y, z)) for y, z in outer]
    li = [bm.verts.new((x - hw, y, z)) for y, z in inner]
    ri = [bm.verts.new((x + hw, y, z)) for y, z in inner]
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((lo[i], lo[j], ro[j], ro[i]))      # внешняя поверхность
        bm.faces.new((ri[i], ri[j], li[j], li[i]))      # внутренняя
        bm.faces.new((li[i], li[j], lo[j], lo[i]))      # левый торец
        bm.faces.new((ro[i], ro[j], ri[j], ri[i]))      # правый торец
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.004)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    col.objects.link(obj)
    return obj


def loft(name, bottom, top, z0, z1, col, offset=(0, 0)):
    """Призма-лофт между двумя контурами (x, y) одинаковой длины."""
    ox, oy = offset
    bm = bmesh.new()
    b = [bm.verts.new((x + ox, y + oy, z0)) for x, y in bottom]
    t = [bm.verts.new((x + ox, y + oy, z1)) for x, y in top]
    bm.faces.new(list(reversed(b)))
    bm.faces.new(t)
    n = len(bottom)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((b[i], b[j], t[j], t[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.bevel(bm, geom=list(bm.edges), offset=0.025, segments=1, affect="EDGES")
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    col.objects.link(obj)
    return obj


def finish(obj, mat, smooth=35):
    if mat is not None:
        wg.assign(obj, mat)
    wg.smooth_by_angle(obj, smooth)
    return obj


# ----------------------------------------------------------------------------
def build():
    wg.new_scene()
    col = wg.collection(f"{NAME}_Model")
    helpers = wg.collection(f"{NAME}_Helpers")

    tex = wg.TEX_DIR / "Shared" / "T_Camo_Woodland_A.png"
    m_camo = wg.material("WG_Camo_Woodland", tex)
    m_camo.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.72
    m_dark = wg.material("WG_OliveDark")
    m_metal = wg.material("WG_Graphite")
    m_rubber = wg.material("WG_Rubber")
    m_steel = wg.material("WG_Steel")
    m_glass = wg.material("WG_Glass")
    m_team = wg.material("WG_TeamColor")

    root = wg.root_empty(NAME, col)

    # ---------------- корпус ----------------
    # верхний корпус (над гусеницами): боковой профиль (y, z)
    upper = wg.hull_from_profile("Hull_Upper", [
        (-3.50, 1.06), (-2.55, 1.50), (2.95, 1.60), (3.50, 1.52), (3.50, 1.06),
    ], HULL_W, col, bevel=0.03)
    # нижний корпус между гусеницами, с наклонным носом
    lower = wg.hull_from_profile("Hull_Lower", [
        (-3.42, 1.07), (-2.90, 0.50), (3.05, 0.50), (3.42, 0.95), (3.42, 1.07),
    ], 2 * (TRACK_X - TRACK_W / 2) - 0.02, col, bevel=0.02)
    hull_parts = [upper, lower]
    # люк мехвода и приборы наблюдения
    hull_parts.append(wg.box("hatch", (0.75, 0.55, 0.06), (0.0, -2.30, 1.555), col, bevel=0.015))
    for dx in (-0.28, 0.0, 0.28):
        hull_parts.append(wg.box("periscope", (0.18, 0.08, 0.07), (dx, -2.62, 1.55), col))
    # решётки МТО на корме
    for i in range(5):
        hull_parts.append(wg.box("grille", (2.4, 0.16, 0.04), (0, 1.55 + i * 0.30, 1.615), col))
    hull = wg.join("Hull", hull_parts)
    finish(hull, m_camo)

    # ---------------- детали корпуса (неподвижные) ----------------
    det = []
    # бортовые экраны: 5 секций на сторону
    for side in (-1, 1):
        x = side * (HULL_W / 2 + 0.035)
        for i in range(5):
            y0 = -3.30 + i * 1.30
            det.append(wg.box("skirt", (0.06, 1.26, 0.52), (x, y0 + 0.63, 0.80), col, bevel=0.012))
        # ящики ЗИП на полке
        det.append(wg.box("bin", (0.38, 1.6, 0.30), (side * 1.55, 1.2, 1.75), col, bevel=0.02))
        # фары
        det.append(wg.cylinder("light", 0.09, 0.10, (side * 1.30, -3.25, 1.30), axis="Y", verts=12, col=col))
        # буксирные крюки
        det.append(wg.box("hook", (0.12, 0.18, 0.12), (side * 0.85, -3.52, 0.75), col, bevel=0.02))
        det.append(wg.box("hook", (0.12, 0.18, 0.12), (side * 0.85, 3.50, 0.80), col, bevel=0.02))
    # кормовой ящик
    det.append(wg.box("rear_box", (2.2, 0.35, 0.40), (0, 3.62, 1.25), col, bevel=0.03))
    details = wg.join("Details", det)
    finish(details, m_dark)

    # ---------------- колёса ----------------
    wheels = []

    def wheel(name, y, z, r, x, mat_tire, hub=True):
        parts = [wg.cylinder(name + "_t", r, 0.48, (x, y, z), axis="X", verts=20, col=col)]
        wg.assign(parts[0], mat_tire)
        if hub:
            for s in (-1, 1):
                h = wg.cylinder(name + "_h", r * 0.55, 0.06, (x + s * 0.25, y, z), axis="X", verts=12, col=col,
                                radius2=r * 0.40)
                wg.assign(h, m_dark)
                parts.append(h)
        w = wg.join(name, parts)
        wg.smooth_by_angle(w, 40)
        wg.set_origin(w, (x, y, z))
        wheels.append(w)
        return w

    for side, tag in ((-1, "L"), (1, "R")):
        x = side * TRACK_X
        for i, y in enumerate(WHEEL_YS):
            wheel(f"Wheel_{tag}_{i + 1:02d}", y, WHEEL_Z, WHEEL_R, x, m_rubber)
        wheel(f"Wheel_{tag}_Idler", IDLER[0], IDLER[1], IDLER[2], x, m_dark)
        wheel(f"Wheel_{tag}_Sprocket", SPROCKET[0], SPROCKET[1], SPROCKET[2], x, m_metal)

    # ---------------- гусеницы ----------------
    lt = finish(track_ring("Left_Tracks", -TRACK_X, col), m_rubber, 30)
    rt = finish(track_ring("Right_Tracks", TRACK_X, col), m_rubber, 30)

    # ---------------- башня ----------------
    c = TURRET_C
    bottom = [(-0.55, -2.05), (0.55, -2.05), (1.62, -1.05), (1.62, 1.85), (1.25, 2.35),
              (-1.25, 2.35), (-1.62, 1.85), (-1.62, -1.05)]
    top = [(-0.40, -1.70), (0.40, -1.70), (1.45, -0.92), (1.50, 1.75), (1.12, 2.22),
           (-1.12, 2.22), (-1.50, 1.75), (-1.45, -0.92)]
    t_parts = [loft("turret_body", bottom, top, c.z, c.z + 0.72, col, (c.x, c.y))]
    t_parts.append(wg.cylinder("ring", 1.15, 0.12, (c.x, c.y, c.z + 0.02), verts=24, col=col))
    turret = wg.join("Turret", t_parts)
    finish(turret, m_camo)
    wg.set_origin(turret, c)

    # детали башни (вращаются вместе с ней)
    td = []
    top_z = c.z + 0.72
    # командирская башенка + панорама
    td.append(wg.cylinder("cupola", 0.36, 0.22, (0.72, c.y + 0.55, top_z + 0.11), verts=16, col=col))
    td.append(wg.box("cdr_sight", (0.30, 0.30, 0.34), (0.72, c.y - 0.05, top_z + 0.17), col, bevel=0.02))
    # прицел наводчика
    td.append(wg.box("gunner_sight", (0.45, 0.50, 0.30), (-0.85, c.y - 0.95, top_z + 0.15), col, bevel=0.02))
    # люк заряжающего
    td.append(wg.cylinder("hatch2", 0.32, 0.06, (-0.70, c.y + 0.70, top_z + 0.03), verts=16, col=col))
    # кормовая корзина
    td.append(wg.box("bustle_rack", (2.4, 0.45, 0.30), (0, c.y + 2.50, c.z + 0.45), col, bevel=0.02))
    # дымовые гранатомёты (по 4 на сторону)
    for side in (-1, 1):
        for k in range(4):
            td.append(wg.cylinder("smoke", 0.055, 0.30, (side * (1.10 + k * 0.10), c.y - 1.10 + k * 0.05, top_z + 0.08),
                                  axis="Y", verts=8, col=col))
    # антенны
    for side in (-1, 1):
        td.append(wg.cylinder("antenna", 0.012, 1.4, (side * 1.20, c.y + 1.95, top_z + 0.70), verts=6, col=col))
    turret_det = wg.join("Turret_Details", td)
    finish(turret_det, m_dark)
    # опознавательные полосы цвета страны
    team = []
    for side in (-1, 1):
        team.append(wg.box("team", (0.02, 0.9, 0.16), (side * 1.585, c.y + 0.60, c.z + 0.38), col))
    team_obj = wg.join("TeamColor", team)
    wg.assign(team_obj, m_team)
    # стекло прицелов
    glass = []
    glass.append(wg.box("g1", (0.36, 0.02, 0.16), (-0.85, c.y - 1.21, top_z + 0.17), col))
    glass.append(wg.box("g2", (0.22, 0.02, 0.18), (0.72, c.y - 0.21, top_z + 0.20), col))
    glass_obj = wg.join("Optics_Glass", glass)
    wg.assign(glass_obj, m_glass)

    # ---------------- пушка ----------------
    trunnion = Vector((0.0, c.y - 1.62, c.z + 0.38))
    g = []
    g.append(wg.box("mantlet", (0.95, 0.55, 0.55), (0, trunnion.y - 0.15, trunnion.z), col, bevel=0.04))
    barrel_len = 5.2
    g.append(wg.cylinder("barrel", 0.085, barrel_len, (0, trunnion.y - 0.40 - barrel_len / 2, trunnion.z),
                         axis="Y", verts=14, col=col, radius2=0.075))
    g.append(wg.cylinder("evacuator", 0.13, 0.75, (0, trunnion.y - 2.70, trunnion.z), axis="Y", verts=14, col=col))
    g.append(wg.cylinder("muzzle_ring", 0.10, 0.20, (0, trunnion.y - 0.40 - barrel_len + 0.10, trunnion.z),
                         axis="Y", verts=14, col=col))
    gun = wg.join("Main_Gun", g)
    finish(gun, m_metal, 40)
    # мантлет — камуфляж, ствол — металл
    gun.data.materials[0] = m_camo
    bm = bmesh.new()
    bm.from_mesh(gun.data)
    gun.data.materials.append(m_metal)
    for f in bm.faces:
        f.material_index = 0 if f.calc_center_median().y > trunnion.y - 0.45 else 1
    bm.to_mesh(gun.data)
    bm.free()
    wg.set_origin(gun, trunnion)

    muzzle = bpy.data.objects.new("Muzzle", None)
    muzzle.empty_display_type = "SINGLE_ARROW"
    muzzle.empty_display_size = 0.5
    muzzle.location = (0, trunnion.y - 0.40 - barrel_len - 0.05, trunnion.z)
    col.objects.link(muzzle)

    # маркеры эффектов на корпусе
    fx = []
    for side, nm in ((-1, "Exhaust_L"), (1, "Exhaust_R")):
        e = bpy.data.objects.new(nm, None)
        e.empty_display_size = 0.3
        e.location = (side * 1.10, 3.55, 1.20)
        col.objects.link(e)
        fx.append(e)

    # ---------------- иерархия ----------------
    hull.parent = root
    for o in (details, lt, rt, turret, *wheels, *fx):
        wg.parent_keep(o, hull)
    for o in (turret_det, team_obj, glass_obj, gun):
        wg.parent_keep(o, turret)
    wg.parent_keep(muzzle, gun)

    # UV
    for o in [hull, details, lt, rt, turret, turret_det, team_obj, glass_obj, gun, *wheels]:
        wg.uv_unwrap(o)

    # габаритный ориентир (в Helpers, не экспортируется)
    ref = wg.box("REF_Human_1.8m", (0.5, 0.3, 1.8), (2.6, -1.0, 0.9), helpers)
    ref.display_type = "WIRE"
    ref.hide_render = True
    return root


def main(render=True):
    root = build()
    report = wg.validate(root)
    blend = wg.SOURCE_DIR / CATEGORY / f"{NAME}.blend"
    fbx = wg.FBX_DIR / CATEGORY / f"{NAME}.fbx"
    wg.save_blend(blend)
    wg.export_fbx(fbx, root)
    # копия в Unity-проект (Git LFS хранит одинаковые файлы один раз)
    unity_fbx = wg.UNITY_MODELS_DIR / CATEGORY / f"{NAME}.fbx"
    unity_fbx.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(fbx, unity_fbx)
    tex_src = wg.TEX_DIR / "Shared" / "T_Camo_Woodland_A.png"
    tex_dst = wg.UNITY_TEX_DIR / "Shared" / tex_src.name
    tex_dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(tex_src, tex_dst)
    wg.write_report(report, wg.BLENDER_ASSETS / "Documentation" / "Reports" / f"{NAME}.json")
    if render:
        wg.render_preview(wg.BLENDER_ASSETS / "Documentation" / "Previews" / f"{NAME}.png",
                          target=(0, -0.8, 1.1), distance=17, elev_deg=20, azim_deg=-40)
    print("REPORT", report["tris_total"], report["bounds_m"], report["min_z"], report["problems"])


if __name__ == "__main__":
    main(render="--no-render" not in sys.argv)
