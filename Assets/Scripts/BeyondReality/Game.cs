using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BeyondReality
{
    /// <summary>
    /// Builds the whole game at runtime and drives it. Just press Play in any scene
    /// (auto-bootstrap), or add this component to an empty GameObject.
    /// Controls: A/D or arrows – move, Space/W/Up – jump, Q/E – rotate view, R – restart.
    /// </summary>
    public class Game : MonoBehaviour
    {
        /// <summary>Set to false if you'd rather place the Game component in a scene yourself.</summary>
        public static bool AutoBootstrap = true;

        public float coinPickupRadius = 0.6f;

        VoxelWorld world;
        CameraRig rig;
        PlayerController player;
        readonly List<Transform> coins = new List<Transform>();
        readonly List<Vector3Int> coinCells = new List<Vector3Int>();
        int collected;
        float timer;
        bool won;
        GUIStyle hud, big;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (!AutoBootstrap) return;
#if UNITY_2023_1_OR_NEWER
            if (FindAnyObjectByType<Game>() != null) return;
#else
            if (FindObjectOfType<Game>() != null) return;
#endif
            new GameObject("BeyondReality Game").AddComponent<Game>();
        }

        void Start()
        {
            world = new VoxelWorld(Level.Voxels());
            world.BuildVisuals().transform.SetParent(transform, false);

            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera));
            camGo.tag = "MainCamera";
            rig = camGo.GetComponent<CameraRig>();
            if (rig == null) rig = camGo.AddComponent<CameraRig>();

            player = new GameObject("Player").AddComponent<PlayerController>();
            player.Init(world, Level.PlayerStart, Level.StartView);
            rig.Init(Level.StartView, player.TruePosition);

            EnsureLight();

            var coinMat = VoxelWorld.MakeMaterial(new Color(1f, 0.82f, 0.2f), emissive: true);
            foreach (var c in Level.Coins)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(go.GetComponent<Collider>());
                go.name = "Coin";
                go.transform.SetParent(transform, false);
                go.transform.position = c;
                go.transform.localScale = new Vector3(0.55f, 0.05f, 0.55f);
                go.GetComponent<MeshRenderer>().sharedMaterial = coinMat;
                coins.Add(go.transform);
                coinCells.Add(c);
            }
        }

        void EnsureLight()
        {
#if UNITY_2023_1_OR_NEWER
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            var lights = FindObjectsOfType<Light>();
#endif
            foreach (var l in lights) if (l.type == LightType.Directional) return;
            var go = new GameObject("Sun");
            var sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        void Restart()
        {
            collected = 0; timer = 0f; won = false;
            foreach (var c in coins) c.gameObject.SetActive(true);
            player.Place(Level.PlayerStart, Level.StartView);
            rig.SnapTo(Level.StartView, player.TruePosition);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            if (Pressed(Key_R)) { Restart(); return; }

            if (!rig.IsRotating)
            {
                int turn = Pressed(Key_E) ? 1 : Pressed(Key_Q) ? -1 : 0;
                if (turn != 0 && player.Grounded)
                {
                    if (player.TryEnterView(rig.View + turn)) rig.Rotate(turn);
                    else rig.Shake();
                }
            }

            if (!rig.IsRotating)
            {
                float move = (Held(Key_D) || Held(Key_Right) ? 1f : 0f) - (Held(Key_A) || Held(Key_Left) ? 1f : 0f);
                bool jumpPressed = Pressed(Key_Space) || Pressed(Key_W) || Pressed(Key_Up);
                bool jumpHeld = Held(Key_Space) || Held(Key_W) || Held(Key_Up);
                player.Tick(move, jumpPressed, jumpHeld, dt);

                if (player.View != rig.View) rig.SnapTo(player.View, player.TruePosition); // respawned into another view
                CollectCoins();
            }

            if (!won) timer += Time.deltaTime;
        }

        void LateUpdate()
        {
            rig.Tick(player.TruePosition, Time.deltaTime);
            player.UpdateVisual(rig.Cam);

            float spin = Time.time * 120f;
            for (int i = 0; i < coins.Count; i++)
            {
                coins[i].rotation = Quaternion.Euler(0f, spin, 90f);
                coins[i].position = coinCells[i] + Vector3.up * (Mathf.Sin(Time.time * 3f + i) * 0.08f);
            }
        }

        /// <summary>A coin is collected when it is in the player's depth slice and overlaps the player on screen.</summary>
        void CollectCoins()
        {
            int v = player.View;
            float ph = player.H, pc = player.Y + PlayerController.Height * 0.5f;
            for (int i = 0; i < coins.Count; i++)
            {
                if (!coins[i].gameObject.activeSelf) continue;
                var c = coinCells[i];
                if (Views.Depth(v, c) == player.DepthCoord && Mathf.Abs(Views.H(v, c) - ph) < coinPickupRadius && Mathf.Abs(c.y - pc) < coinPickupRadius + 0.1f)
                {
                    coins[i].gameObject.SetActive(false);
                    collected++;
                    if (collected == coins.Count) won = true;
                }
            }
        }

        void OnGUI()
        {
            float s = Screen.height / 720f;
            if (hud == null)
            {
                hud = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            }
            hud.fontSize = Mathf.RoundToInt(22 * s);
            big.fontSize = Mathf.RoundToInt(48 * s);
            hud.normal.textColor = big.normal.textColor = Color.white;

            GUI.Label(new Rect(16 * s, 12 * s, 600 * s, 40 * s), $"Monety: {collected}/{coins.Count}", hud);
            GUI.Label(new Rect(16 * s, 40 * s, 600 * s, 40 * s), $"Patrzysz na: {Views.Name(rig.View)}", hud);
            GUI.Label(new Rect(Screen.width - 200 * s, 12 * s, 190 * s, 40 * s), $"{timer:0.0} s", hud);

            var hint = new GUIStyle(hud) { fontSize = Mathf.RoundToInt(15 * s), fontStyle = FontStyle.Normal };
            hint.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            GUI.Label(new Rect(16 * s, Screen.height - 34 * s, 900 * s, 30 * s),
                "A/D – ruch   Spacja – skok   Q/E – obrót świata   R – restart", hint);

            if (won)
                GUI.Label(new Rect(0, 0, Screen.width, Screen.height), $"Wszystkie monety!\n{timer:0.0} s   (R – jeszcze raz)", big);
        }

        // ------------------------------------------------------------ input (works with both input systems)

#if ENABLE_INPUT_SYSTEM
        static Key Key_A => Key.A; static Key Key_D => Key.D; static Key Key_W => Key.W;
        static Key Key_Q => Key.Q; static Key Key_E => Key.E; static Key Key_R => Key.R;
        static Key Key_Space => Key.Space; static Key Key_Left => Key.LeftArrow;
        static Key Key_Right => Key.RightArrow; static Key Key_Up => Key.UpArrow;
        static bool Held(Key k) { var kb = Keyboard.current; return kb != null && kb[k].isPressed; }
        static bool Pressed(Key k) { var kb = Keyboard.current; return kb != null && kb[k].wasPressedThisFrame; }
#else
        static KeyCode Key_A => KeyCode.A; static KeyCode Key_D => KeyCode.D; static KeyCode Key_W => KeyCode.W;
        static KeyCode Key_Q => KeyCode.Q; static KeyCode Key_E => KeyCode.E; static KeyCode Key_R => KeyCode.R;
        static KeyCode Key_Space => KeyCode.Space; static KeyCode Key_Left => KeyCode.LeftArrow;
        static KeyCode Key_Right => KeyCode.RightArrow; static KeyCode Key_Up => KeyCode.UpArrow;
        static bool Held(KeyCode k) => Input.GetKey(k);
        static bool Pressed(KeyCode k) => Input.GetKeyDown(k);
#endif
    }
}
