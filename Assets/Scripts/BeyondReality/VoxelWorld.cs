using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BeyondReality
{
    /// <summary>
    /// Holds the 3D voxel grid. Gameplay collision only looks at the voxels in the player's depth slice
    /// (see PlayerController), so the grid itself is all the game needs.
    /// </summary>
    public class VoxelWorld
    {
        readonly HashSet<Vector3Int> solid = new HashSet<Vector3Int>();

        public BoundsInt Bounds { get; private set; }

        public VoxelWorld(IEnumerable<Vector3Int> voxels)
        {
            foreach (var v in voxels) solid.Add(v);
            Vector3Int min = Vector3Int.one * int.MaxValue, max = Vector3Int.one * int.MinValue;
            foreach (var p in solid) { min = Vector3Int.Min(min, p); max = Vector3Int.Max(max, p); }
            Bounds = new BoundsInt(min, max - min + Vector3Int.one);
        }

        public bool IsSolid(Vector3Int p) => solid.Contains(p);

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
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = ShadowCastingMode.Off;   // hidden layers must not shade the visible slice
            }
            return root;
        }

        /// <summary>
        /// Copies a template material from Resources/BeyondReality so the shader (and the _EMISSION variant)
        /// is guaranteed to be in builds; Shader.Find alone returns null there because nothing references it.
        /// </summary>
        public static Material MakeMaterial(Color c, bool emissive = false)
        {
            bool srp = GraphicsSettings.currentRenderPipeline != null;
            Material m = null;
            var template = srp ? Resources.Load<Material>(emissive ? "BeyondReality/VoxelLitEmissive" : "BeyondReality/VoxelLit") : null;
            if (template != null) m = new Material(template);
            else
            {
                Shader shader = srp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Hidden/InternalErrorShader");
                if (shader == null) { Debug.LogError("Beyond Reality: brak shadera dla materiału voxeli."); return null; }
                m = new Material(shader);
            }
            m.color = c;
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
