"""Мини-читатель бинарного FBX: печатает GlobalSettings (оси, масштаб) и трансформы Model-узлов.

python3 BlenderAssets/Scripts/tools/fbx_inspect.py path/to/model.fbx
Нужен, чтобы проверять экспорт без Unity: что именно лежит в файле.
"""
import struct
import sys
import zlib


def read_node(f, ver):
    if ver >= 7500:
        end, nprops, plen = struct.unpack("<QQQ", f.read(24))
    else:
        end, nprops, plen = struct.unpack("<III", f.read(12))
    nlen = f.read(1)[0]
    name = f.read(nlen).decode()
    if end == 0:
        return None
    props = []
    for _ in range(nprops):
        t = chr(f.read(1)[0])
        if t == "Y": props.append(struct.unpack("<h", f.read(2))[0])
        elif t == "C": props.append(bool(f.read(1)[0]))
        elif t == "I": props.append(struct.unpack("<i", f.read(4))[0])
        elif t == "F": props.append(struct.unpack("<f", f.read(4))[0])
        elif t == "D": props.append(struct.unpack("<d", f.read(8))[0])
        elif t == "L": props.append(struct.unpack("<q", f.read(8))[0])
        elif t in "SR":
            ln = struct.unpack("<I", f.read(4))[0]
            raw = f.read(ln)
            props.append(raw.decode("utf-8", "replace") if t == "S" else raw)
        elif t in "fdlib":
            n, enc, clen = struct.unpack("<III", f.read(12))
            data = f.read(clen)
            if enc:
                data = zlib.decompress(data)
            fmt = {"f": "f", "d": "d", "l": "q", "i": "i", "b": "?"}[t]
            props.append(list(struct.unpack("<%d%s" % (n, fmt), data)) if n < 64 else f"<array {n}>")
    children = []
    while f.tell() < end:
        c = read_node(f, ver)
        if c is None:
            break
        children.append(c)
    f.seek(end)
    return (name, props, children)


def load(path):
    with open(path, "rb") as f:
        assert f.read(21) == b"Kaydara FBX Binary  \x00"
        f.read(2)
        ver = struct.unpack("<I", f.read(4))[0]
        nodes = []
        while True:
            n = read_node(f, ver)
            if n is None:
                break
            nodes.append(n)
    return ver, nodes


def find(nodes, name):
    return [n for n in nodes if n[0] == name]


def p70(node):
    out = {}
    for pn in find(node[2], "Properties70"):
        for p in pn[2]:
            out[p[1][0]] = p[1][4:]
    return out


def main(path):
    ver, nodes = load(path)
    print("FBX version", ver)
    gs = find(nodes, "GlobalSettings")[0]
    props = p70(gs)
    for k in ("UpAxis", "UpAxisSign", "FrontAxis", "FrontAxisSign", "CoordAxis", "CoordAxisSign",
              "UnitScaleFactor", "OriginalUnitScaleFactor"):
        print(f"  {k} = {props.get(k)}")
    objects = find(nodes, "Objects")[0]
    ids = {}
    for m in find(objects[2], "Model"):
        uid, name, kind = m[1][0], m[1][1].split("\x00")[0], m[1][2]
        ids[uid] = name
        pr = p70(m)
        def r(k):
            v = pr.get(k)
            return tuple(round(x, 3) for x in v) if v else None
        print(f"  {name:18s} {kind:6s} T={r('Lcl Translation')} R={r('Lcl Rotation')} "
              f"S={r('Lcl Scaling')} PreR={r('PreRotation')}")
    conns = find(nodes, "Connections")[0]
    print("  hierarchy:")
    for c in conns[2]:
        if c[1][0] == "OO" and c[1][1] in ids:
            print(f"    {ids[c[1][1]]} -> {ids.get(c[1][2], 'ROOT')}")


if __name__ == "__main__":
    main(sys.argv[1])
