#!/usr/bin/env python3
"""
Level solver for Beyond Reality.
Reads Assets/Scripts/BeyondReality/Level.cs and checks that every coin is reachable,
and how many view rotations each one needs (0 = obtainable without rotating).

Usage:  python Tools/level_solver.py [path/to/Level.cs]

Movement model (must stay conservative vs. PlayerController):
  walk 1 column; jump +1 up and up to 2 columns sideways; jump +2 up and 1 column sideways;
  fall straight down; rotate view only while standing, refused if the player would overlap
  a projected block. Depth snap: keep current depth if a voxel is under the player, else the
  voxel nearest the camera.
"""
import re, sys
from collections import deque
from pathlib import Path

RIGHT = [(1, 0), (0, -1), (-1, 0), (0, 1)]   # view 0 looks +Z (north)
FWD   = [(0, 1), (1, 0), (0, -1), (-1, 0)]
JUMPS = ((0, 1), (1, 2), (2, 1))             # (height gain, max columns)
dot = lambda x, z, v: x * v[0] + z * v[1]

def parse(path):
    src = Path(path).read_text(encoding="utf-8")
    boxes_src = src.split("Boxes =", 1)[1].split("};", 1)[0]
    boxes = [tuple(map(int, m.split(","))) for m in re.findall(r"new\[\]\s*\{([^}]*)\}", boxes_src)]
    coins_src = src.split("Coins =", 1)[1].split("};", 1)[0]
    coins = [tuple(map(int, m.split(","))) for m in re.findall(r"Vector3Int\(([^)]*)\)", coins_src)]
    start = tuple(map(int, re.search(r"PlayerStart\s*=\s*new Vector3Int\(([^)]*)\)", src).group(1).split(",")))
    view = int(re.search(r"StartView\s*=\s*(\d+)", src).group(1))
    return boxes, coins, start, view

def voxels(boxes):
    b = set()
    for x0, y0, z0, x1, y1, z1 in boxes:
        for x in range(x0, x1 + 1):
            for y in range(y0, y1 + 1):
                for z in range(z0, z1 + 1):
                    b.add((x, y, z))
    return b

def solve(blocks, coins, start, allow_rot=True):
    proj = []
    for v in range(4):
        d = {}
        for x, y, z in blocks:
            d.setdefault((dot(x, z, RIGHT[v]), y), []).append(dot(x, z, FWD[v]))
        for k in d: d[k].sort()
        proj.append(d)
    miny = min(y for _, y, _ in blocks) - 3
    dist = {start: 0}; q = deque([start]); got = {}
    while q:
        s = q.popleft(); v, h, r, d = s; P = proj[v]; cd = dist[s]
        for i, c in enumerate(coins):
            if (dot(c[0], c[2], RIGHT[v]), c[1]) == (h, r):
                got[i] = min(got.get(i, 99), cd)
        nxt = []
        if allow_rot:
            for dv in (1, 3):
                v2 = (v + dv) % 4
                x = RIGHT[v][0] * h + FWD[v][0] * d; z = RIGHT[v][1] * h + FWD[v][1] * d
                h2, d2 = dot(x, z, RIGHT[v2]), dot(x, z, FWD[v2])
                if (h2, r) not in proj[v2]:
                    ds = proj[v2].get((h2, r - 1))
                    if ds and d2 not in ds: d2 = ds[0]
                    nxt.append(((v2, h2, r, d2), 1))
        for k, md in JUMPS:
            for dh in range(-md, md + 1):
                if dh == 0: continue
                st = 1 if dh > 0 else -1
                if any((h, r + i) in P for i in range(1, k + 1)): continue
                if any((h + st * i, r + k) in P for i in range(1, abs(dh) + 1)): continue
                hh, rr = h + dh, r + k
                while (hh, rr - 1) not in P and rr > miny: rr -= 1
                if rr <= miny: continue
                ds = P[(hh, rr - 1)]
                nxt.append(((v, hh, rr, d if d in ds else ds[0]), 0))
        for n, w in nxt:
            nd = cd + w
            if nd < dist.get(n, 99):
                dist[n] = nd
                (q.appendleft if w == 0 else q.append)(n)
    return got

def main():
    path = sys.argv[1] if len(sys.argv) > 1 else "Assets/Scripts/BeyondReality/Level.cs"
    boxes, coins, start, view = parse(path)
    blocks = voxels(boxes)
    ok = True
    if start in blocks: print("ERROR: player start is inside a block"); ok = False
    for c in coins:
        if c in blocks: print(f"ERROR: coin {c} is inside a block"); ok = False
    s = (view, dot(start[0], start[2], RIGHT[view]), start[1], dot(start[0], start[2], FWD[view]))
    got = solve(blocks, coins, s)
    for i, c in enumerate(coins):
        print(f"coin {i:2d} {c}: " + (f"reachable, min rotations {got[i]}" if i in got else "UNREACHABLE"))
    if len(got) < len(coins): ok = False
    print(f"\n{len(got)}/{len(coins)} reachable; "
          f"{sum(1 for r in got.values() if r == 0)} obtainable without rotating; voxels: {len(blocks)}")
    sys.exit(0 if ok else 1)

if __name__ == "__main__":
    main()
