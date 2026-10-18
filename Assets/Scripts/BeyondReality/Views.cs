using UnityEngine;

namespace BeyondReality
{
    /// <summary>
    /// Four camera views around the vertical axis.
    /// View 0 looks north (+Z), 1 east (+X), 2 south (-Z), 3 west (-X).
    /// "H" is the horizontal screen coordinate, "depth" is the coordinate along the camera's forward axis.
    /// </summary>
    public static class Views
    {
        static readonly Vector2Int[] R = { new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(0, 1) };
        static readonly Vector2Int[] F = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };
        static readonly string[] Names = { "Północ", "Wschód", "Południe", "Zachód" };

        public static int Wrap(int v) => ((v % 4) + 4) % 4;
        public static float Yaw(int v) => v * 90f;
        public static string Name(int v) => Names[Wrap(v)];

        public static Vector3 Right(int v) { var r = R[Wrap(v)]; return new Vector3(r.x, 0f, r.y); }
        public static Vector3 Forward(int v) { var f = F[Wrap(v)]; return new Vector3(f.x, 0f, f.y); }

        public static int H(int v, Vector3Int p) { var r = R[Wrap(v)]; return p.x * r.x + p.z * r.y; }
        public static int Depth(int v, Vector3Int p) { var f = F[Wrap(v)]; return p.x * f.x + p.z * f.y; }

        public static float H(int v, Vector3 p) { var r = R[Wrap(v)]; return p.x * r.x + p.z * r.y; }
        public static float Depth(int v, Vector3 p) { var f = F[Wrap(v)]; return p.x * f.x + p.z * f.y; }

        /// <summary>World position from projected coordinates (y is passed through).</summary>
        public static Vector3 ToWorld(int v, float h, float depth, float y) => Right(v) * h + Forward(v) * depth + Vector3.up * y;
    }
}
