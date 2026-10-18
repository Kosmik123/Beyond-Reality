using System.Collections.Generic;
using UnityEngine;

namespace BeyondReality
{
    /// <summary>
    /// Level data. Every box is {x0, y0, z0, x1, y1, z1} (inclusive voxel coordinates).
    /// The level spans 20x20x20 and is built for the depth-slice rule: walkways run along x (seen from
    /// north/south) or along z (seen from east/west), and rotating at a crossing switches between them.
    /// Checked with Tools/level_solver.py: all 10 coins are reachable and each needs 1–9 rotations.
    /// </summary>
    public static class Level
    {
        public static readonly int[][] Boxes =
        {
            new[] {  0,  0,  2,  8,  0,  2 },   // start walkway (slice z=2)
            new[] {  8,  0,  3,  8,  0, 13 },   // walkway along z at x=8
            new[] {  8,  0, 14,  8,  3, 16 },   // block: +3 step up
            new[] {  9,  3, 16, 19,  3, 16 },   // high walkway along x at z=16
            new[] { 14,  6, 16, 15,  6, 16 },   // ledge above it
            new[] { 19,  3,  4, 19,  3, 15 },   // east walkway along z at x=19
            new[] { 19,  3, 17, 19,  3, 19 },   // north spur
            new[] { 16,  0,  4, 18,  0,  4 },   // low pit south-east
            new[] { 12,  6,  7, 18,  6,  7 },   // bridge to the tower (z=7)
            new[] { 12,  9,  8, 12,  9,  8 },   // step 1 (x=12 plane)
            new[] { 12, 11, 10, 12, 11, 10 },   // step 2
            new[] {  4, 11, 10, 11, 11, 10 },   // sky walkway along x at z=10
            new[] {  4, 11,  4,  4, 11,  9 },   // sky walkway along z at x=4
            new[] {  5, 11,  4, 10, 11,  4 },   // sky walkway along x at z=4
            new[] {  3, 14,  4,  3, 14,  4 },   // west perch
            new[] {  4,  8,  0,  4,  8,  3 },   // hidden ledge under the sky walkway (visible from the start)
            new[] { 10, 14,  5, 10, 14,  5 },   // tower step
            new[] {  9,  0,  6, 11, 17,  8 },   // central tower
            new[] { 10, 18,  7, 10, 18,  7 },   // spire
        };

        // ordered by the number of rotations needed (solver): 1, 2, 2, 3, 4, 5, 7, 7, 8, 9
        public static readonly Vector3Int[] Coins =
        {
            new Vector3Int(8, 1, 6),  new Vector3Int(12, 4, 16), new Vector3Int(15, 7, 16), new Vector3Int(19, 4, 19),
            new Vector3Int(16, 1, 4), new Vector3Int(12, 10, 8), new Vector3Int(4, 12, 7),  new Vector3Int(4, 9, 0),
            new Vector3Int(3, 15, 4), new Vector3Int(10, 19, 7),
        };

        /// <summary>Empty voxel the player starts in (feet rest on the voxel below).</summary>
        public static readonly Vector3Int PlayerStart = new Vector3Int(1, 1, 2);
        public const int StartView = 0;

        public static IEnumerable<Vector3Int> Voxels()
        {
            foreach (var b in Boxes)
                for (int x = b[0]; x <= b[3]; x++)
                    for (int y = b[1]; y <= b[4]; y++)
                        for (int z = b[2]; z <= b[5]; z++)
                            yield return new Vector3Int(x, y, z);
        }
    }
}
