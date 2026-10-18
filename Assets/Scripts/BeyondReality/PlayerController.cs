using UnityEngine;

namespace BeyondReality
{
    /// <summary>
    /// 2D platformer physics on a single depth slice of the voxel world for the current view:
    /// only voxels at the player's depth are solid, everything in front or behind is ignored.
    /// State: h (horizontal screen coordinate), y (feet height), depth (integer, along camera forward).
    /// Voxel (x,y,z) occupies [x-0.5, x+0.5] etc., so a block's top surface is at y + 0.5.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5.5f;
        public float gravity = 32f;
        public float jumpApex = 3.2f;      // blocks; the level design assumes you can climb 3 blocks
        public float maxFallSpeed = 20f;
        public float coyoteTime = 0.08f;
        public float jumpBuffer = 0.12f;

        public const float HalfWidth = 0.3f;
        public const float Height = 0.9f;
        const float Eps = 0.001f;

        public int View { get; private set; }
        public float H { get; private set; }
        public float Y { get; private set; }
        public int DepthCoord { get; private set; }
        public bool Grounded { get; private set; }

        public Vector3 TruePosition => Views.ToWorld(View, H, DepthCoord, Y + Height * 0.5f);

        VoxelWorld world;
        Vector2 vel;
        float coyote, buffer;
        int facing = 1;
        bool jumpCut;

        // last safe spot for respawning
        Vector3 safeWorld; int safeView;

        Transform body, eye;

        public void Init(VoxelWorld w, Vector3Int startVoxel, int view)
        {
            world = w;
            BuildVisual();
            Place(startVoxel, view);
        }

        public void Place(Vector3Int voxel, int view)
        {
            View = Views.Wrap(view);
            H = Views.H(View, voxel);
            DepthCoord = Views.Depth(View, voxel);
            Y = voxel.y - 0.5f;
            vel = Vector2.zero;
            Grounded = true;
            safeWorld = Views.ToWorld(View, H, DepthCoord, Y); safeView = View;
        }

        // ------------------------------------------------------------ physics

        public void Tick(float move, bool jumpPressed, bool jumpHeld, float dt)
        {
            if (move != 0) facing = move > 0 ? 1 : -1;

            coyote = Grounded ? coyoteTime : coyote - dt;
            buffer = jumpPressed ? jumpBuffer : buffer - dt;

            vel.x = move * moveSpeed;
            vel.y = Mathf.Max(vel.y - gravity * dt, -maxFallSpeed);

            if (buffer > 0f && coyote > 0f)
            {
                vel.y = Mathf.Sqrt(2f * gravity * jumpApex);
                buffer = coyote = 0f;
                jumpCut = false;
            }
            if (!jumpHeld && vel.y > 0f && !jumpCut) { vel.y *= 0.45f; jumpCut = true; }   // variable jump height

            MoveX(vel.x * dt);
            MoveY(vel.y * dt);
            UpdateSafeSpot();

            if (Y < -6f) Respawn();
        }

        static int Cell(float c) => Mathf.FloorToInt(c + 0.5f);

        bool Solid(int i, int j) => SolidIn(View, i, DepthCoord, j);
        bool SolidIn(int view, int i, int depth, int j) => world.IsSolid(Views.ToVoxel(view, i, depth, j));

        public bool Overlaps(int view, float h, int depth, float y)
        {
            int i0 = Cell(h - HalfWidth + Eps), i1 = Cell(h + HalfWidth - Eps);
            int j0 = Cell(y + Eps), j1 = Cell(y + Height - Eps);
            for (int i = i0; i <= i1; i++)
                for (int j = j0; j <= j1; j++)
                    if (SolidIn(view, i, depth, j)) return true;
            return false;
        }

        void MoveX(float dx)
        {
            if (dx == 0f) return;
            float nh = H + dx;
            int j0 = Cell(Y + Eps), j1 = Cell(Y + Height - Eps);
            int i = dx > 0 ? Cell(nh + HalfWidth - Eps) : Cell(nh - HalfWidth + Eps);
            for (int j = j0; j <= j1; j++)
            {
                if (!Solid(i, j)) continue;
                nh = dx > 0 ? i - 0.5f - HalfWidth : i + 0.5f + HalfWidth;
                vel.x = 0f;
                break;
            }
            H = nh;
        }

        void MoveY(float dy)
        {
            float ny = Y + dy;
            int i0 = Cell(H - HalfWidth + Eps), i1 = Cell(H + HalfWidth - Eps);
            Grounded = false;
            if (dy <= 0f)
            {
                int j = Cell(ny);
                for (int i = i0; i <= i1; i++)
                {
                    if (!Solid(i, j) || Y < j + 0.5f - Eps) continue;
                    ny = j + 0.5f; vel.y = 0f; Grounded = true;
                    break;
                }
            }
            else
            {
                int j = Cell(ny + Height);
                for (int i = i0; i <= i1; i++)
                {
                    if (!Solid(i, j)) continue;
                    ny = j - 0.5f - Height; vel.y = 0f;
                    break;
                }
            }
            Y = ny;
        }

        /// <summary>Remember the spot as a respawn point while standing squarely on a block.</summary>
        void UpdateSafeSpot()
        {
            if (!Grounded || !Solid(Cell(H), Cell(Y - 0.05f))) return;
            safeWorld = Views.ToWorld(View, Mathf.Round(H), DepthCoord, Y); safeView = View;
        }

        void Respawn()
        {
            View = safeView;
            H = Views.H(View, safeWorld);
            DepthCoord = Mathf.RoundToInt(Views.Depth(View, safeWorld));
            Y = safeWorld.y;
            vel = Vector2.zero;
            Grounded = true;
        }

        public int SafeView => safeView;

        // ------------------------------------------------------------ view rotation

        /// <summary>
        /// Re-express the player's position in another view. The new depth slice is the voxel column
        /// the player occupies now: the centre column first, else an edge column that has ground under it.
        /// Fails only if the player would end up inside a block of the new slice.
        /// </summary>
        public bool TryEnterView(int newView)
        {
            newView = Views.Wrap(newView);
            int row = Cell(Y - 0.05f);
            int c0 = Cell(H), c1 = Cell(H - HalfWidth + Eps), c2 = Cell(H + HalfWidth - Eps);
            int fallback = int.MinValue; float fallbackH = 0f, bestH = 0f;
            int best = int.MinValue;
            foreach (int col in new[] { c0, c1, c2 })
            {
                Vector3 wp = Views.ToWorld(View, col, DepthCoord, 0f);
                float nh = Mathf.Round(Views.H(newView, wp));
                int nd = Mathf.RoundToInt(Views.Depth(newView, wp));
                if (Overlaps(newView, nh, nd, Y)) continue;
                if (fallback == int.MinValue) { fallback = nd; fallbackH = nh; }
                if (SolidIn(newView, Mathf.RoundToInt(nh), nd, row)) { best = nd; bestH = nh; break; }
            }
            if (best == int.MinValue) { best = fallback; bestH = fallbackH; }
            if (best == int.MinValue) return false;

            View = newView; H = bestH; DepthCoord = best;
            vel.x = 0f;
            return true;
        }

        // ------------------------------------------------------------ visuals

        void BuildVisual()
        {
            body = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            Destroy(body.GetComponent<Collider>());
            body.name = "Body";
            body.SetParent(transform, false);
            body.localScale = new Vector3(HalfWidth * 2f, Height, HalfWidth * 2f);
            body.GetComponent<MeshRenderer>().sharedMaterial = VoxelWorld.MakeMaterial(new Color(1f, 0.45f, 0.35f));

            eye = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            Destroy(eye.GetComponent<Collider>());
            eye.name = "Eye";
            eye.SetParent(transform, false);
            eye.localScale = Vector3.one * 0.12f;
            eye.GetComponent<MeshRenderer>().sharedMaterial = VoxelWorld.MakeMaterial(new Color(0.1f, 0.08f, 0.15f));
        }

        /// <summary>The player is drawn at its true position inside the level (the camera shows only its slice).</summary>
        public void UpdateVisual(Camera cam)
        {
            Vector3 f = cam.transform.forward, r = cam.transform.right, u = cam.transform.up;
            Vector3 pos = TruePosition;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            eye.position = pos + r * (facing * 0.14f) + u * 0.22f - f * (HalfWidth + 0.02f);
        }
    }
}
