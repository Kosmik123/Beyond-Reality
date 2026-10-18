using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BeyondReality
{
    /// <summary>
    /// Holds the 3D voxel grid and, for each of the 4 views, its 2D projection:
    /// a projected cell (h, y) is solid if ANY voxel along the depth axis is solid.
    /// Gameplay collision uses only these projections.
    /// </summary>
    public class VoxelWorld
    {
        readonly HashSet<Vector3Int> solid = new HashSet<Vector3Int>();
        // per view: (h, y) -> depths of solid voxels, sorted nearest-to-camera first
        readonly Dictionary<Vector2Int, List<int>>[] proj = new Dictionary<Vector2Int, List<int>>[4];

        public BoundsInt Bounds { get; private set; }

        public VoxelWorld(IEnumerable<Vector3Int> voxels)
        {
            foreach (var v in voxels) solid.Add(v);
            Rebuild();
        }

        public bool IsSolid(Vector3Int p) => solid.Contains(p);

        public bool IsSolidProjected(int view, int h, int y) => proj[Views.Wrap(view)].ContainsKey(new Vector2Int(h, y));

        /// <summary>Depths of solid voxels in a projected cell (nearest first), or null if empty.</summary>
        public List<int> DepthsAt(int view, int h, int y)
        {
            proj[Views.Wrap(view)].TryGetValue(new Vector2Int(h, y), out var list);
            return list;
        }

        void Rebuild()
        {
            Vector3Int min = Vector3Int.one * int.MaxValue, max = Vector3Int.one * int.MinValue;
            foreach (var p in solid) { min = Vector3Int.Min(min, p); max = Vector3Int.Max(max, p); }
            Bounds = new BoundsInt(min, max - min + Vector3Int.one);

            for (int v = 0; v < 4; v++)
            {
                var d = new Dictionary<Vector2Int, List<int>>();
                foreach (var p in solid)
                {
                    var key = new Vector2Int(Views.H(v, p), p.y);
                    if (!d.TryGetValue(key, out var list)) d[key] = list = new List<int>();
                    list.Add(Views.Depth(v, p));
                }
                foreach (var list in d.Values) list.Sort();
                proj[v] = d;
            }
        }

        // ---------------------------------------------------------------- rendering

        public GameObject BuildVisuals()
        {
            var root = new GameObject("Voxels");
            var cache = new Dictionary<int, Material>();
            int maxY = Mathf.Max(1, Bounds.yMax - 1);

            foreach (var p in solid)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(go.GetComponent<Collider>());
                go.name = $"V {p.x},{p.y},{p.z}";
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = p;

                bool checker = ((p.x + p.y + p.z) & 1) == 0;
                int key = p.y * 2 + (checker ? 1 : 0);
                if (!cache.TryGetValue(key, out var mat))
                {
                    Color c = p.y == 0
                        ? new Color(0.22f, 0.24f, 0.33f)
                        : Color.HSVToRGB(Mathf.Lerp(0.50f, 0.86f, p.y / (float)maxY), 0.45f, 0.92f);
                    if (checker) c *= 0.86f;
                    c.a = 1f;
                    cache[key] = mat = MakeMaterial(c);
                }
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }
            return root;
        }

        public static Material MakeMaterial(Color c, bool emissive = false)
        {
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Shader shader = srp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            var m = new Material(shader) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (emissive && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 0.6f);
            }
            return m;
        }
    }
}
