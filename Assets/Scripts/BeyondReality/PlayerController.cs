using System.Collections.Generic;
using UnityEngine;

namespace BeyondReality
{
    /// <summary>
    /// 2D platformer physics on the projection of the voxel world for the current view.
    /// State: h (horizontal screen coordinate), y (feet height), depth (integer, along camera forward).
    /// Voxel (x,y,z) occupies [x-0.5, x+0.5] etc., so a block's top surface is at y + 0.5.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5.5f;
        public float gravity = 32f;
        public float jumpApex = 2.4f;      // blocks; the level design assumes you can climb 2 blocks
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
            ResolveDepth();

            if (Y < -6f) Respawn();
        }

        static int Cell(float c) => Mathf.FloorToInt(c + 0.5f);

        bool Solid(int i, int j) => world.IsSolidProjected(View, i, j);

        public bool Overlaps(int view, float h, float y)
        {
            int i0 = Cell(h - HalfWidth + Eps), i1 = Cell(h + HalfWidth - Eps);
            int j0 = Cell(y + Eps), j1 = Cell(y + Height - Eps);
            for (int i = i0; i <= i1; i++)
                for (int j = j0; j <= j1; j++)
                    if (world.IsSolidProjected(view, i, j)) return true;
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

        /// <summary>
        /// The FEZ trick: when standing on something, move the player along the (invisible) depth axis
        /// onto a voxel that is actually under them. Prefer the current depth, else the one nearest the camera.
        /// </summary>
        void ResolveDepth()
        {
            if (!Grounded) return;
            int row = Cell(Y - 0.05f);
            int center = Cell(H);
            int col = center;
            if (!Solid(col, row))
            {
                col = Cell(H - HalfWidth + Eps);
                if (!Solid(col, row)) col = Cell(H + HalfWidth - Eps);
            }
            List<int> depths = world.DepthsAt(View, col, row);
            if (depths == null) return;
            if (!depths.Contains(DepthCoord)) DepthCoord = depths[0];

            if (col == center) { safeWorld = Views.ToWorld(View, Mathf.Round(H), DepthCoord, Y); safeView = View; }
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

        /// <summary>Re-express the player's position in another view. Fails if they'd end up inside a projected block.</summary>
        public bool TryEnterView(int newView)
        {
            newView = Views.Wrap(newView);
            Vector3 wp = Views.ToWorld(View, H, DepthCoord, 0f);
            float nh = Views.H(newView, wp);
            int nd = Mathf.RoundToInt(Views.Depth(newView, wp));
            if (Overlaps(newView, nh, Y)) return false;

            View = newView; H = nh; DepthCoord = nd;
            vel.x = 0f;
            ResolveDepth();
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

        /// <summary>
        /// Orthographic trick: push the visual toward the camera along its forward axis.
        /// Screen position stays identical, but the player is always drawn in front of the world.
        /// </summary>
        public void UpdateVisual(Camera cam)
        {
            Vector3 f = cam.transform.forward, r = cam.transform.right, u = cam.transform.up;
            Vector3 pos = TruePosition - f * 25f;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            eye.position = pos + r * (facing * 0.14f) + u * 0.22f - f * (HalfWidth + 0.02f);
        }
    }
}
