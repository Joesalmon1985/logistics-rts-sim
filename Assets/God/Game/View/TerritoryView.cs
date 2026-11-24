using System.Collections.Generic;
using UnityEngine;
using God.Sim;

namespace God.Game
{
    /// <summary>
    /// The territory view: roads, flags and raw piles as models (<see cref="GroundLayer"/>, P4b), a soft glow on
    /// buildable spots only while placing (<see cref="ShowBuildable"/>), the P1 build circles and node dots only as a
    /// debug overlay (<see cref="ShowDebugOverlay"/>), and a tiny dice tick that flashes on a roll (no number is ever
    /// shown). Buildings are drawn by <see cref="BuildingLayer"/>. It only reads <see cref="TerritoryState"/>.
    /// </summary>
    public sealed class TerritoryView : MonoBehaviour
    {
        public static readonly Color PlayerColour = new Color(0.20f, 0.45f, 0.95f);
        public static readonly Color EnemyColour = new Color(0.92f, 0.25f, 0.22f);

        private const float DotY = 0.25f, RingY = 0.34f;
        private const float TickSeconds = 0.9f;

        /// <summary>Set by the economy view's owner: how many economy buildings are finished (sites are drawn by the economy view).</summary>
        public System.Func<int> Built;

        private GodSim _sim;
        private GroundLayer _ground;
        private bool _debug;
        private GameObject _lit, _flat;
        private Material _litMat, _flatMat;
        private int _signature = int.MinValue;
        private long _seenRollTick;
        private float _tickTime = -10f;

        public GodSim Sim => _sim;
        /// <summary>Roads, flags, piles and the buildable glow (P4b).</summary>
        public GroundLayer Ground => _ground;

        /// <summary>Shows (placing mode) or hides the soft glow on the spots where a building can go.</summary>
        public void ShowBuildable(bool on) => _ground.ShowBuildable(on, Faction.Player);
        public void ShowBuildable(bool on, Faction f) => _ground.ShowBuildable(on, f);
        public bool BuildableShown => _ground.BuildableShown;

        /// <summary>The tiny dice tick drawn by this view; the HUD draws its own and turns this off.</summary>
        public bool ShowDiceTick = true;

        /// <summary>The HUD's placing-mode switch (P5): shows the soft glow on the player's buildable spots.</summary>
        public bool OverlayVisible
        {
            get => BuildableShown;
            set => ShowBuildable(value, Faction.Player);
        }

        /// <summary>Debug only: the P1 build circles and the dots on buildable nodes. Off by default.</summary>
        public bool ShowDebugOverlay
        {
            get => _debug;
            set { if (_debug == value) return; _debug = value; _signature = int.MinValue; }
        }
        /// <summary>Rebuilds so far (for tests).</summary>
        public int Rebuilds { get; private set; }
        /// <summary>Seconds since the last roll flash started.</summary>
        public float SinceRoll => Time.unscaledTime - _tickTime;

        public static TerritoryView Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("TerritoryView");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<TerritoryView>();
            v._sim = sim;
            v._litMat = MaterialFactory.CreateVertexColored(Color.white, "Territory lit");
            v._flatMat = MaterialFactory.CreateUnlit(Color.white, "Territory flat");
            v._seenRollTick = sim.State.Territory.LastRollTick;
            v._ground = GroundLayer.Create(sim, go.transform);
            return v;
        }

        private void LateUpdate()
        {
            var t = _sim.State.Territory;
            if (t == null) return;
            int sig = Signature(t);
            if (sig != _signature) { _signature = sig; Rebuild(_sim.State); }

            if (t.LastRollTick != _seenRollTick)
            {
                _seenRollTick = t.LastRollTick;
                _tickTime = Time.unscaledTime;
                PulseProducers(_sim.State);
            }
        }

        private int Signature(TerritoryState t)
        {
            unchecked
            {
                int h = t.NextId * 31 + t.Hqs.Count * 7 + t.Buildings.Count * 13 + t.Flags.Count * 17 + t.Roads.Count * 19 + (_debug ? 1 : 0);
                foreach (var hq in t.Hqs) if (hq.Site) h = h * 31 + hq.Id;
                if (Built != null) h = h * 31 + Built.Invoke();
                return h;
            }
        }

        /// <summary>The hexes that produced on the last roll glow briefly. The number itself is never shown.</summary>
        private static void PulseProducers(GodState s)
        {
            var hexes = new List<int>();
            int total = s.Territory.LastTotal;
            for (int h = 0; h < s.Board.HexNumber.Length; h++)
                if (s.Board.HexNumber[h] == total && RawKinds.OfTerrain(s.Board.HexTerrain[h]) != RawKind.None) hexes.Add(h);
            HexDressingView.Pulse(hexes);
        }

        private static Color FactionColour(Faction f) => f == Faction.Player ? PlayerColour : EnemyColour;

        private static Vector3 P(NodeGrid g, int node, float y) => new Vector3((float)g.X(node), y, (float)g.Z(node));

        private void Rebuild(GodState s)
        {
            Rebuilds++;
            if (_lit != null) Destroy(_lit);
            if (_flat != null) Destroy(_flat);
            var t = s.Territory; var g = t.Grid;
            var lit = new MeshBuilder();
            var flat = new MeshBuilder();
            if (_debug)
            {
                // Debug overlay: build circles and the faint dots of buildable nodes.
                foreach (var hq in t.Hqs)
                {
                    if (hq.Site) continue;
                    var col = FactionColour(hq.Faction);
                    var c = t.CircleOf(hq);
                    var centre = new Vector3((float)c.X, RingY, (float)c.Z);
                    const int seg = 72;
                    for (int k = 0; k < seg; k++)
                    {
                        float a0 = k * Mathf.PI * 2f / seg, a1 = (k + 1) * Mathf.PI * 2f / seg;
                        var p0 = centre + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (float)c.Radius;
                        var p1 = centre + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (float)c.Radius;
                        p0.y = ModelKit.GroundY(p0.x, p0.z) + RingY; p1.y = ModelKit.GroundY(p1.x, p1.z) + RingY;
                        flat.Strip(p0, p1, 0.7f, col);
                    }
                }
                foreach (Faction f in new[] { Faction.Player, Faction.Enemy })
                {
                    var dot = Color.Lerp(FactionColour(f), Color.white, 0.75f);
                    foreach (int n in t.BuildableNodes(f))
                    {
                        if (t.NodeBuilding[n] != 0 || t.NodeFlag[n] != 0 || t.NodeRoad[n] != 0) continue;
                        var q = P(g, n, DotY); q.y += ModelKit.GroundY(q.x, q.z);
                        Dot(flat, q, 0.45f, dot);
                    }
                }
            }
            _lit = lit.ToObject("Territory lit", transform, _litMat);
            _flat = flat.ToObject("Territory flat", transform, _flatMat);
            foreach (var r in _lit.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (var r in _flat.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void Dot(MeshBuilder mb, Vector3 c, float half, Color col)
        {
            mb.Quad(c + new Vector3(-half, 0, -half), c + new Vector3(-half, 0, half), c + new Vector3(half, 0, half), c + new Vector3(half, 0, -half), col);
        }

        public static Color KindColour(RawKind k)
        {
            switch (k)
            {
                case RawKind.Log: return new Color(0.50f, 0.33f, 0.16f);
                case RawKind.Clay: return new Color(0.80f, 0.40f, 0.22f);
                case RawKind.Grain: return new Color(0.92f, 0.80f, 0.25f);
                case RawKind.Rock: return new Color(0.60f, 0.62f, 0.66f);
                default: return Color.magenta;
            }
        }

        /// <summary>The tiny dice tick: a small square in the top-left corner that flashes on every roll. No number.</summary>
        private void OnGUI()
        {
            if (!ShowDiceTick) return;
            float since = SinceRoll;
            float k = Mathf.Clamp01(1f - since / TickSeconds);
            float size = 14f + 10f * k;
            var rect = new Rect(14f + (24f - size) * 0.5f, 14f + (24f - size) * 0.5f, size, size);
            var old = GUI.color;
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.45f);
            GUI.DrawTexture(new Rect(10f, 10f, 32f, 32f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.85f, 0.35f, 0.35f + 0.65f * k);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
