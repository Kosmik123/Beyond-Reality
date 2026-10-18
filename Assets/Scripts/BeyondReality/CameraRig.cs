using UnityEngine;

namespace BeyondReality
{
    /// <summary>
    /// Orthographic camera that orbits the focus point in 90° steps around the vertical axis.
    /// Outside of turns the clip planes cut out a single depth slice (the player's), so only the layer
    /// the player interacts with is rendered. During a turn it shows the whole world and briefly tilts
    /// down so the player glimpses it in 3D.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public float distance = 60f;
        public float orthoSize = 7f;
        public float turnDuration = 0.4f;
        public float turnTilt = 20f;       // max pitch during a turn
        public float followSharpness = 6f;

        public int View { get; private set; }
        public bool IsRotating => t < 1f;
        public Camera Cam { get; private set; }

        float fromYaw, toYaw, t = 1f;
        float shakeTime;
        Vector3 focus;
        int sliceDepth;

        public void Init(int view, Vector3 focusPoint)
        {
            Cam = GetComponent<Camera>();
            Cam.orthographic = true;
            Cam.orthographicSize = orthoSize;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.09f, 0.07f, 0.16f);
            SnapTo(view, focusPoint);
        }

        public void SnapTo(int view, Vector3 focusPoint)
        {
            View = Views.Wrap(view);
            fromYaw = toYaw = Views.Yaw(View);
            t = 1f;
            focus = focusPoint;
            sliceDepth = Mathf.RoundToInt(Views.Depth(View, focusPoint));
            Apply(0f);
        }

        /// <summary>dir = +1 (clockwise / E key) or -1 (Q key).</summary>
        public void Rotate(int dir)
        {
            View = Views.Wrap(View + dir);
            fromYaw = toYaw;
            toYaw += 90f * dir;
            t = 0f;
        }

        public void Shake() => shakeTime = 0.25f;

        public void Tick(Vector3 focusTarget, float dt)
        {
            if (t < 1f) t = Mathf.Min(1f, t + dt / turnDuration);
            focus = Vector3.Lerp(focus, focusTarget, 1f - Mathf.Exp(-followSharpness * dt));
            if (shakeTime > 0f) shakeTime -= dt;
            sliceDepth = Mathf.RoundToInt(Views.Depth(View, focusTarget));
            Apply(dt);
        }

        void Apply(float dt)
        {
            float s = Mathf.SmoothStep(0f, 1f, t);
            float yaw = Mathf.Lerp(fromYaw, toYaw, s);
            float pitch = turnTilt * Mathf.Sin(Mathf.PI * s);
            var rot = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 shake = Vector3.zero;
            if (shakeTime > 0f)
                shake = rot * Vector3.right * (Mathf.Sin(Time.time * 70f) * 0.12f * (shakeTime / 0.25f));

            transform.rotation = rot;
            transform.position = focus - rot * Vector3.forward * distance + shake;
            ApplyClipping();
        }

        /// <summary>
        /// Slice [depth-0.5, depth+0.5] seen from the camera. The near plane sits just in front of the slice's
        /// front faces; the far plane just before its back faces, so the front faces of the next layer are cut.
        /// </summary>
        void ApplyClipping()
        {
            if (IsRotating)
            {
                Cam.nearClipPlane = 0.1f;
                Cam.farClipPlane = distance * 2f + 50f;
                return;
            }
            float camDepth = Views.Depth(View, transform.position);
            Cam.nearClipPlane = sliceDepth - 0.55f - camDepth;
            Cam.farClipPlane = sliceDepth + 0.45f - camDepth;
        }
    }
}
