using System.Collections.Generic;
using UnityEngine;
using God.Sim;

namespace God.Game
{
    /// <summary>
    /// P1 debug view of land, dice and territory, drawn on top of the island: HQs and build circles, faint dots on
    /// buildable lattice nodes, flags, roads, buildings (placeholder blocks), piles at hex centres, and a tiny dice
    /// tick that flashes on a roll (no number is ever shown). It only reads <see cref="TerritoryState"/>; it never
    /// mutates the sim. Meshes are rebuilt when the state changes.
    /// </summary>
    public sealed class TerritoryView : MonoBehaviour
    {
        public static readonly Color PlayerColour = new Color(0.20f, 0.45f, 0.95f);
        public static readonly Color EnemyColour = new Color(0.92f, 0.25f, 0.22f);

        private const float DotY = 0.25f, RoadY = 0.30f, RingY = 0.34f;
        private const float TickSeconds = 0.9f;

        /// <summary>Set by the economy view's owner: how many economy buildings are finished (sites are drawn by the economy view).</summary>
        public System.Func<int> Built;

        private GodSim _sim;
        private GameObject _lit, _flat;
        private Material _litMat, _flatMat;
        private int _signature = int.MinValue;
        private long _seenRollTick;
        private float _tickTime = -10f;

        public GodSim Sim => _sim;
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
                int h = t.NextId * 31 + t.Hqs.Count * 7 + t.Buildings.Count * 13 + t.Flags.Count * 17 + t.Roads.Count * 19;
                foreach (var hq in t.Hqs) if (hq.Site) h = h * 31 + hq.Id;
                if (Built != null) h = h * 31 + Built.Invoke();
                for (int i = 0; i < t.Piles.Length; i++) h = h * 31 + t.Piles[i];
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

            // Build circles and the faint dots of buildable nodes.
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
                    flat.Strip(centre + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (float)c.Radius,
                               centre + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (float)c.Radius, 0.7f, col);
                }
            }
            foreach (Faction f in new[] { Faction.Player, Faction.Enemy })
            {
                var dot = Color.Lerp(FactionColour(f), Color.white, 0.75f);
                foreach (int n in t.BuildableNodes(f))
                {
                    if (t.NodeBuilding[n] != 0 || t.NodeFlag[n] != 0 || t.NodeRoad[n] != 0) continue;
                    Dot(flat, P(g, n, DotY), 0.45f, dot);
                }
            }

            // Roads (thin lines under the flags).
            var roadCol = new Color(0.86f, 0.72f, 0.42f);
            foreach (var r in t.Roads)
                for (int i = 1; i < r.Nodes.Length; i++)
                    flat.Strip(P(g, r.Nodes[i - 1], RoadY), P(g, r.Nodes[i], RoadY), 1.0f, roadCol);

            // Flags: pole and banner.
            foreach (var fl in t.Flags)
            {
                var p = P(g, fl.Node, 0f);
                lit.Cylinder(p, 0.22f, 0.22f, 3.2f, 6, new Color(0.95f, 0.95f, 0.9f));
                lit.Box(p + new Vector3(0.9f, 2.7f, 0f), new Vector3(1.8f, 1.0f, 0.12f), FactionColour(fl.Faction));
            }

            // Buildings: HQ, medium and small placeholder blocks.
            foreach (var b in t.Buildings)
            {
                var eco = s.Economy?.Find(b.Id);
                if (eco != null && !eco.Built) continue; // a construction site: drawn by the economy view
                var col = FactionColour(b.Faction);
                var p = P(g, b.Node, 0f);
                switch (b.Size)
                {
                    case BuildingSize.Hq:
                        lit.Box(p + Vector3.up * 3f, new Vector3(9f, 6f, 9f), Color.Lerp(col, Color.white, 0.25f));
                        lit.Box(p + Vector3.up * 7f, new Vector3(6f, 2f, 6f), col);
                        break;
                    case BuildingSize.Medium:
                        lit.Box(p + Vector3.up * 2f, new Vector3(6.5f, 4f, 6.5f), Color.Lerp(col, Color.white, 0.35f));
                        lit.Box(p + Vector3.up * 4.4f, new Vector3(5f, 0.8f, 5f), col);
                        break;
                    default:
                        lit.Box(p + Vector3.up * 1.5f, new Vector3(3.6f, 3f, 3.6f), Color.Lerp(col, Color.white, 0.45f));
                        lit.Box(p + Vector3.up * 3.2f, new Vector3(2.8f, 0.6f, 2.8f), col);
                        break;
                }
            }

            // Piles: small coloured stacks beside each hex centre.
            for (int h = 0; h < t.Piles.Length; h++)
            {
                var kind = TerritoryState.PileKind(s.Board, h);
                var col = KindColour(kind);
                var c = new Vector3((float)g.Topology.HexX(h), 0f, (float)g.Topology.HexZ(h));
                for (int i = 0; i < t.Piles[h]; i++)
                {
                    var o = c + new Vector3((i - 1) * 6.5f, 0f, -10f);
                    lit.Box(o + Vector3.up * 1.2f, new Vector3(5f, 2.4f, 5f), col);
                    lit.Box(o + Vector3.up * 3.1f, new Vector3(3.2f, 1.4f, 3.2f), col * 1.12f);
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
