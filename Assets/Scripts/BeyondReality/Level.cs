using System.Collections.Generic;
using UnityEngine;

namespace BeyondReality
{
    /// <summary>
    /// Level data. Every box is {x0, y0, z0, x1, y1, z1} (inclusive voxel coordinates).
    /// This demo level was checked with a solver: all 10 coins are reachable,
    /// and none of them can be collected without rotating the view.
    /// </summary>
    public static class Level
    {
        public static readonly int[][] Boxes =
        {
            new[] { 0, 0, 0, 11, 0, 11 },   // floor
            new[] { 5, 1, 5, 6, 7, 6 },     // central tower
            new[] { 0, 8, 1, 2, 8, 3 },
            new[] { 0, 8, 5, 1, 8, 5 },
            new[] { 8, 11, 5, 10, 11, 7 },
            new[] { 7, 9, 5, 7, 9, 5 },
            new[] { 3, 8, 3, 5, 8, 5 },
            new[] { 3, 1, 9, 5, 1, 10 },
            new[] { 5, 1, 8, 5, 4, 9 },
            new[] { 1, 4, 1, 2, 4, 3 },
            new[] { 5, 4, 7, 7, 4, 9 },
            new[] { 10, 4, 5, 10, 4, 5 },
            new[] { 1, 1, 9, 3, 1, 10 },
            new[] { 4, 1, 9, 6, 1, 11 },
            new[] { 8, 6, 4, 9, 6, 6 },
            new[] { 10, 1, 5, 10, 8, 6 },
            new[] { 2, 4, 9, 3, 4, 9 },
            new[] { 8, 8, 3, 8, 8, 5 },
            new[] { 0, 4, 9, 2, 4, 11 },
            new[] { 8, 2, 3, 10, 2, 3 },
            new[] { 9, 2, 1, 11, 3, 2 },
        };

        public static readonly Vector3Int[] Coins =
        {
            new Vector3Int(8, 12, 5), new Vector3Int(0, 5, 11), new Vector3Int(1, 5, 1),  new Vector3Int(10, 3, 3),
            new Vector3Int(7, 5, 9),  new Vector3Int(3, 9, 5),  new Vector3Int(8, 7, 4),  new Vector3Int(0, 9, 2),
            new Vector3Int(3, 5, 9),  new Vector3Int(10, 9, 6),
        };

        /// <summary>Empty voxel the player starts in (feet rest on the voxel below).</summary>
        public static readonly Vector3Int PlayerStart = new Vector3Int(0, 1, 0);
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
