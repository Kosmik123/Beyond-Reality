#!/usr/bin/env python3
"""
Level solver for Beyond Reality.
Reads Assets/Scripts/BeyondReality/Level.cs and checks that every coin is reachable,
and how many view rotations each one needs (0 = obtainable without rotating).

Usage:  python Tools/level_solver.py [path/to/Level.cs]

Movement model (must stay conservative vs. PlayerController):
  the player collides only with voxels in its own depth slice (everything in front or behind is ignored);
  walk 1 column (fits a 1-high tunnel); jumps (jumpApex 3.2): +3 up 1 column, +2 up 2 columns,
  +1 up 3 columns, level over a 2-column gap; every jump needs one free row of headroom above
  the landing height, at the take-off column and along the path; fall straight down; rotate view only while standing: the new slice is the player's current voxel
  column, then the player falls onto the ground of that slice. Coins count only in the player's voxel.
"""
import re, sys
from collections import deque
from pathlib import Path

RIGHT = [(1, 0), (0, -1), (-1, 0), (0, 1)]   # view 0 looks +Z (north)
FWD   = [(0, 1), (1, 0), (0, -1), (-1, 0)]
JUMPS = ((0, 3), (1, 3), (2, 2), (3, 1))     # (height gain, max columns); walking is separate
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
    def solid(v, h, y, d):
        x = RIGHT[v][0] * h + FWD[v][0] * d; z = RIGHT[v][1] * h + FWD[v][1] * d
        return (x, y, z) in blocks
    def world(v, h, y, d):
        return (RIGHT[v][0] * h + FWD[v][0] * d, y, RIGHT[v][1] * h + FWD[v][1] * d)
    miny = min(y for _, y, _ in blocks) - 3
    def land(v, h, r, d):
        while not solid(v, h, r - 1, d) and r > miny: r -= 1
        return r if r > miny else None
    coin_at = {c: i for i, c in enumerate(coins)}
    dist = {start: 0}; q = deque([start]); got = {}
    while q:
        s = q.popleft(); v, h, r, d = s; cd = dist[s]
        i = coin_at.get(world(v, h, r, d))
        if i is not None: got[i] = min(got.get(i, 99), cd)
        nxt = []
        if allow_rot:
            x, _, z = world(v, h, r, d)
            for dv in (1, 3):
                v2 = (v + dv) % 4
                h2, d2 = dot(x, z, RIGHT[v2]), dot(x, z, FWD[v2])
                if solid(v2, h2, r, d2): continue
                r2 = land(v2, h2, r, d2)
                if r2 is not None: nxt.append(((v2, h2, r2, d2), 1))
        for dh in (-1, 1):                                    # walk (and step off ledges)
            if not solid(v, h + dh, r, d):
                rr = land(v, h + dh, r, d)
                if rr is not None: nxt.append(((v, h + dh, rr, d), 0))
        for k, md in JUMPS:
            if any(solid(v, h, r + i, d) for i in range(1, k + 2)): continue
            for dh in range(-md, md + 1):
                if dh == 0: continue
                st = 1 if dh > 0 else -1
                if any(solid(v, h + st * i, r + j, d) for i in range(1, abs(dh) + 1) for j in (k, k + 1)): continue
                rr = land(v, h + dh, r + k, d)
                if rr is not None: nxt.append(((v, h + dh, rr, d), 0))
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
