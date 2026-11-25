using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Tall faction standards (P10): every finished HQ flies a pole with a big flag in its faction colour, a Citadel two flags
    /// and a gold finial. They grow with the camera height, so each faction's bases read at full-island zoom as well as up
    /// close. View only: it reads HQs and Citadels from the sim.
    /// </summary>
    public sealed class StandardLayer : MonoBehaviour
    {
        /// <summary>Camera height at which a standard is drawn at its natural size; higher up it grows in step.</summary>
        public const float RefHeight = 80f;
        public const float MaxScale = 4.8f;

        private sealed class SView { public int Id; public Faction Faction; public bool Citadel; public GameObject Root; public Transform Cloth; public float Seed; }

        private GodSim _sim;
        private readonly Dictionary<int, SView> _views = new Dictionary<int, SView>();
        private readonly HashSet<int> _alive = new HashSet<int>();
        private readonly List<int> _gone = new List<int>();
        private float _time;

        /// <summary>Standards flying now, for tests.</summary>
        public int Count => _views.Count;
        /// <summary>The scale the standards have this frame.</summary>
        public float CurrentScale { get; private set; } = 1f;

        public static StandardLayer Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("StandardLayer");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<StandardLayer>();
            l._sim = sim;
            return l;
        }

        /// <summary>The standard's scale for a camera height: natural up to <see cref="RefHeight"/>, growing in step above it.</summary>
        public static float ScaleFor(float cameraHeight) => Mathf.Clamp(cameraHeight / RefHeight, 1f, MaxScale);

        private void LateUpdate()
        {
            var s = _sim.State; var t = s.Territory;
            if (t == null) return;
            _time += Mathf.Min(Time.deltaTime, 0.1f);
            var cam = Camera.main;
            CurrentScale = cam != null ? ScaleFor(cam.transform.position.y) : 1f;
            float yaw = cam != null ? cam.transform.eulerAngles.y : 0f;
            var g = t.Grid;
            _alive.Clear();
            foreach (var hq in t.Hqs)
            {
                if (hq.Site) continue;
                _alive.Add(hq.Id);
                if (_views.TryGetValue(hq.Id, out var v) && (v.Faction != hq.Faction || v.Citadel != hq.Citadel)) { Destroy(v.Root); _views.Remove(hq.Id); v = null; }
                if (v == null)
                {
                    v = new SView { Id = hq.Id, Faction = hq.Faction, Citadel = hq.Citadel, Seed = (hq.Id * 0.37f) % 1f };
                    v.Root = new GameObject("Standard " + hq.Id);
                    v.Root.transform.SetParent(transform, false);
                    v.Root.transform.position = ModelKit.Node(g, hq.Node) + (hq.Citadel ? new Vector3(-10.5f, 0f, -10.5f) : new Vector3(-6.5f, 0f, -6.5f));
                    var pole = ModelKit.Part("Pole", v.Root.transform, PoleMesh(hq.Citadel));
                    var cloth = ModelKit.Part("Cloth", v.Root.transform, ClothMesh(hq.Faction, hq.Citadel));
                    cloth.transform.localPosition = new Vector3(0f, PoleHeight(hq.Citadel) - 0.3f, 0f);
                    v.Cloth = cloth.transform;
                    _views[hq.Id] = v;
                }
                v.Root.transform.localScale = Vector3.one * CurrentScale;
                // The cloth turns to the camera (a flat cloth seen edge-on would vanish) and sways a little.
                v.Cloth.rotation = Quaternion.Euler(0f, yaw + 12f * Mathf.Sin(_time * 1.4f + v.Seed * 6f), 0f);
            }
            _gone.Clear();
            foreach (var kv in _views) if (!_alive.Contains(kv.Key)) _gone.Add(kv.Key);
            foreach (int id in _gone) { Destroy(_views[id].Root); _views.Remove(id); }
        }

        private static float PoleHeight(bool citadel) => citadel ? 18f : 14f;

        private static Mesh PoleMesh(bool citadel)
            => ModelKit.Cached("standard/pole/" + citadel, mb =>
            {
                float h = PoleHeight(citadel);
                mb.Cylinder(Vector3.zero, 0.34f, 0.2f, h, 6, ModelKit.DarkWood);
                mb.Cylinder(Vector3.zero, 0.7f, 0.5f, 1.2f, 6, ModelKit.Rock);
                var gold = new Color(0.95f, 0.76f, 0.22f);
                mb.Cone(Vector3.up * h, citadel ? 0.62f : 0.4f, citadel ? 1.5f : 1.0f, 6, gold);
            });

        /// <summary>The cloth: its hoist at the pole (local origin), flying along +X. A Citadel has a second, lower flag and a pale band.</summary>
        private static Mesh ClothMesh(Faction f, bool citadel)
            => ModelKit.Cached("standard/cloth/" + (int)f + "/" + citadel, mb =>
            {
                var col = ModelKit.Accent(FactionLook.Colour(f));
                float w = citadel ? 5.0f : 3.8f, h = citadel ? 3.0f : 2.4f;
                Flag(mb, 0f, w, h, col);
                if (citadel) Flag(mb, -h - 0.5f, w * 0.8f, h * 0.7f, col * 0.9f);
            });

        private static void Flag(MeshBuilder mb, float top, float w, float h, Color col)
        {
            var a = new Vector3(0.25f, top, 0f); var b = new Vector3(w, top - h * 0.12f, 0f);
            var d = new Vector3(w, top - h, 0f); var e = new Vector3(0.25f, top - h, 0f);
            ModelKit.Q2(mb, a, e, d, b, col);
            // A pale band across the middle, so two neighbouring colours still read apart.
            var band = Color.Lerp(col, Color.white, 0.6f);
            ModelKit.Q2(mb, a + new Vector3(0, -h * 0.38f, 0.02f), a + new Vector3(0, -h * 0.58f, 0.02f), new Vector3(w, top - h * 0.58f, 0.02f), new Vector3(w, top - h * 0.38f, 0.02f), band);
        }
    }
}
