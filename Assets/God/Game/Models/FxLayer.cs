using System.Collections.Generic;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P4b effects, all in two dynamic meshes rebuilt each frame (one draw each): lit smoke and dust puffs that rise,
    /// swell and shrink away; unlit tracers (arrows, sling stones, heavy shots on an arc), blow impacts (hit, crit, block, miss), muzzle flashes, sparks, and small
    /// billboard health bars. Other layers queue into it during their LateUpdate; it builds last. View code only; time is
    /// wall-clock animation time, never sim state.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class FxLayer : MonoBehaviour
    {
        public static FxLayer Instance { get; private set; }

        private struct Puff { public Vector3 P, V; public float Age, Life, Size; public Color C; public float Twist; }
        private struct Shot { public Vector3 From, To; public float Age, Life, Arc, Len, Width; public Color C; public bool Heavy; public Impact Hit; }

        /// <summary>How a blow or missile ends where it lands (from <see cref="God.Sim.AttackOutcome"/>).</summary>
        public enum Impact { None = 0, Hit = 1, Crit = 2, Blocked = 3, Miss = 4, Building = 5 }
        private struct Bar { public Vector3 P; public float W, F; }
        private struct Flash { public Vector3 P; public float Age, Life, Size; public Color C; }

        private const int MaxPuffs = 900, MaxShots = 300;
        private readonly List<Puff> _puffs = new List<Puff>(256);
        private readonly List<Shot> _shots = new List<Shot>(64);
        private readonly List<Flash> _flashes = new List<Flash>(64);
        private readonly List<Bar> _bars = new List<Bar>(64);
        private readonly MeshBuilder _lit = new MeshBuilder(), _unlit = new MeshBuilder();
        private Mesh _litMesh, _unlitMesh;
        private PropRandom _rnd = new PropRandom(4242);

        /// <summary>Shots in flight last frame (for tests).</summary>
        public int ShotsDrawn { get; private set; }
        public int BarsDrawn { get; private set; }
        public int PuffsDrawn { get; private set; }
        /// <summary>Shots started since the layer was made (for tests).</summary>
        public int ShotsFired { get; private set; }
        /// <summary>Impacts shown since the layer was made, by kind (for tests): misses, blocks, crits.</summary>
        public int MissesShown { get; private set; }
        public int BlocksShown { get; private set; }
        public int CritsShown { get; private set; }

        public static Impact ImpactOf(God.Sim.AttackOutcome o)
        {
            switch (o)
            {
                case God.Sim.AttackOutcome.Miss: return Impact.Miss;
                case God.Sim.AttackOutcome.Blocked: return Impact.Blocked;
                case God.Sim.AttackOutcome.Crit: return Impact.Crit;
                case God.Sim.AttackOutcome.Building: return Impact.Building;
                case God.Sim.AttackOutcome.Hit: return Impact.Hit;
                default: return Impact.None;
            }
        }

        public static FxLayer Ensure(Transform parent)
        {
            if (Instance != null) return Instance;
            var go = new GameObject("FxLayer");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<FxLayer>();
            fx._litMesh = new Mesh { name = "Fx lit" }; fx._litMesh.MarkDynamic();
            fx._unlitMesh = new Mesh { name = "Fx unlit" }; fx._unlitMesh.MarkDynamic();
            ModelKit.Part("Fx lit", go.transform, fx._litMesh, ModelKit.Lit);
            ModelKit.Part("Fx unlit", go.transform, fx._unlitMesh, ModelKit.Unlit);
            Instance = fx;
            return fx;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public float Rand(float a, float b) => _rnd.Range(a, b);

        // ---------------------------------------------------------------- emit

        /// <summary>One smoke puff rising from a point.</summary>
        public void Smoke(Vector3 p, Color c, float size, float rise = 1.6f, float life = 2.6f)
        {
            if (_puffs.Count >= MaxPuffs) return;
            var drift = new Vector3(0.6f + _rnd.Range(-0.3f, 0.3f), rise, 0.35f + _rnd.Range(-0.3f, 0.3f));
            _puffs.Add(new Puff { P = p + new Vector3(_rnd.Range(-0.2f, 0.2f), 0, _rnd.Range(-0.2f, 0.2f)), V = drift, Life = life * _rnd.Range(0.8f, 1.2f), Size = size * _rnd.Range(0.8f, 1.2f), C = c * _rnd.Range(0.92f, 1.06f), Twist = _rnd.Range(0f, 6f) });
        }

        /// <summary>A ring of dust puffs (a collapse, an impact).</summary>
        public void Dust(Vector3 p, float radius, int count, Color c)
        {
            for (int i = 0; i < count && _puffs.Count < MaxPuffs; i++)
            {
                var d = ModelKit.Dir(_rnd.Range(0f, 360f));
                _puffs.Add(new Puff { P = p + d * _rnd.Range(0f, radius), V = d * _rnd.Range(0.5f, 2.2f) + Vector3.up * _rnd.Range(0.4f, 1.4f), Life = _rnd.Range(0.9f, 1.8f), Size = _rnd.Range(0.5f, 1.0f) * Mathf.Clamp(radius * 0.2f, 0.5f, 1.1f), C = c * _rnd.Range(0.9f, 1.08f), Twist = _rnd.Range(0f, 6f) });
            }
        }

        /// <summary>A shot from a weapon to a point. Heavy shots fly on an arc and raise dust where they land.</summary>
        public void Shoot(Vector3 from, Vector3 to, bool heavy, Color c)
        {
            if (_shots.Count >= MaxShots) return;
            float dist = (to - from).magnitude;
            _shots.Add(new Shot { From = from, To = to, Life = heavy ? Mathf.Clamp(dist / 34f, 0.25f, 0.6f) : Mathf.Clamp(dist / 70f, 0.08f, 0.22f), Arc = heavy ? dist * 0.18f : dist * 0.04f, Len = heavy ? 2.6f : 1.4f, Width = heavy ? 0.32f : 0.16f, C = c, Heavy = heavy });
            _flashes.Add(new Flash { P = from, Life = heavy ? 0.16f : 0.09f, Size = heavy ? 0.9f : 0.45f, C = new Color(1f, 0.86f, 0.45f) });
            ShotsFired++;
        }

        /// <summary>
        /// A quick missile: an arrow (a thin pale streak on a low arc) or a sling stone (a short dark streak on a higher
        /// arc). Where it lands it shows its <paramref name="hit"/>: a miss falls short beside the target in a little puff.
        /// </summary>
        public void Missile(Vector3 from, Vector3 to, bool stone, Impact hit)
        {
            if (_shots.Count >= MaxShots) return;
            if (hit == Impact.Miss) to += new Vector3(_rnd.Range(-1.2f, 1.2f), -1.1f, _rnd.Range(-1.2f, 1.2f));
            float dist = (to - from).magnitude;
            _shots.Add(new Shot
            {
                From = from, To = to, Life = Mathf.Clamp(dist / (stone ? 40f : 55f), 0.12f, 0.6f), Arc = dist * (stone ? 0.12f : 0.07f),
                Len = stone ? 0.35f : 1.1f, Width = stone ? 0.2f : 0.09f, C = stone ? new Color(0.42f, 0.40f, 0.38f) : new Color(0.95f, 0.88f, 0.66f), Hit = hit,
            });
            ShotsFired++;
        }

        /// <summary>
        /// Where a blow or missile lands: a hit flashes, a crit flashes big and red-hot, a block clangs (a steel spark and
        /// a small pale puff), a miss raises a small grey puff.
        /// </summary>
        public void Strike(Vector3 at, Impact hit)
        {
            switch (hit)
            {
                case Impact.Hit: Spark(at, 0.55f, new Color(1f, 0.92f, 0.7f), 0.1f); break;
                case Impact.Crit:
                    CritsShown++;
                    Spark(at, 1.1f, new Color(1f, 0.42f, 0.2f), 0.18f); Spark(at + Vector3.up * 0.2f, 0.6f, new Color(1f, 0.95f, 0.8f), 0.1f); break;
                case Impact.Blocked:
                    BlocksShown++;
                    Spark(at, 0.7f, new Color(0.85f, 0.92f, 1f), 0.14f);
                    if (_puffs.Count < MaxPuffs) _puffs.Add(new Puff { P = at, V = Vector3.up * 0.8f, Life = 0.6f, Size = 0.35f, C = new Color(0.9f, 0.9f, 0.86f), Twist = _rnd.Range(0f, 6f) });
                    break;
                case Impact.Miss:
                    MissesShown++;
                    if (_puffs.Count < MaxPuffs) _puffs.Add(new Puff { P = at, V = Vector3.up * 0.6f + ModelKit.Dir(_rnd.Range(0f, 360f)) * 0.5f, Life = 0.7f, Size = 0.4f, C = new Color(0.62f, 0.6f, 0.56f), Twist = _rnd.Range(0f, 6f) });
                    break;
                case Impact.Building: Spark(at, 0.6f, new Color(1f, 0.8f, 0.5f), 0.12f); Dust(at, 0.6f, 2, new Color(0.6f, 0.55f, 0.46f)); break;
            }
        }

        public void Spark(Vector3 p, float size, Color c, float life = 0.12f) => _flashes.Add(new Flash { P = p, Life = life, Size = size, C = c });

        /// <summary>A health bar this frame (call every frame while it should show).</summary>
        public void HealthBar(Vector3 p, float width, float fraction) => _bars.Add(new Bar { P = p, W = width, F = Mathf.Clamp01(fraction) });

        // ---------------------------------------------------------------- build

        private void LateUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            var cam = Camera.main;
            Vector3 right = cam != null ? cam.transform.right : Vector3.right, up = cam != null ? cam.transform.up : Vector3.up, fwd = cam != null ? cam.transform.forward : Vector3.forward;
            _lit.Clear(); _unlit.Clear();

            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                var p = _puffs[i];
                p.Age += dt;
                if (p.Age >= p.Life) { _puffs.RemoveAt(i); continue; }
                p.P += p.V * dt; p.V *= 1f - 0.6f * dt;
                _puffs[i] = p;
                float k = p.Age / p.Life;
                float s = p.Size * (0.5f + 0.9f * Mathf.Sqrt(k)) * (k > 0.7f ? Mathf.Lerp(1f, 0.15f, (k - 0.7f) / 0.3f) : 1f);
                ModelKit.Lump(_lit, p.P - Vector3.up * s * 0.5f, s * 0.7f, s, Color.Lerp(p.C, p.C * 1.15f, k), 6, p.Twist + k);
            }
            PuffsDrawn = _puffs.Count;

            int shots = 0;
            for (int i = _shots.Count - 1; i >= 0; i--)
            {
                var s = _shots[i];
                s.Age += dt;
                if (s.Age >= s.Life)
                {
                    if (s.Hit != Impact.None) Strike(s.To, s.Hit);
                    else if (s.Heavy) { Dust(s.To, 1.2f, 5, new Color(0.62f, 0.55f, 0.44f)); Spark(s.To + Vector3.up * 0.4f, 1.1f, new Color(1f, 0.7f, 0.3f), 0.14f); }
                    else Spark(s.To + Vector3.up * 0.2f, 0.4f, new Color(1f, 0.9f, 0.6f), 0.08f);
                    _shots.RemoveAt(i); continue;
                }
                _shots[i] = s;
                float k = s.Age / s.Life;
                var head = Arc(s, k);
                float back = Mathf.Clamp01(k - s.Len / Mathf.Max(1f, (s.To - s.From).magnitude));
                var tail = Arc(s, back);
                Streak(_unlit, tail, head, s.Width, s.C, fwd);
                // The bright head of the missile.
                Billboard(_unlit, head, s.Width * 1.6f, s.Width * 1.6f, Color.Lerp(s.C, Color.white, 0.5f), right, up);
                shots++;
            }
            ShotsDrawn = shots;

            for (int i = _flashes.Count - 1; i >= 0; i--)
            {
                var f = _flashes[i];
                f.Age += dt;
                if (f.Age >= f.Life) { _flashes.RemoveAt(i); continue; }
                _flashes[i] = f;
                float s = f.Size * (1f - f.Age / f.Life * 0.5f);
                Star(_unlit, f.P, s, f.C, right, up);
            }

            foreach (var b in _bars)
            {
                Billboard(_unlit, b.P, b.W + 0.18f, 0.34f, new Color(0.06f, 0.06f, 0.06f), right, up);
                var c = b.F > 0.6f ? new Color(0.30f, 0.86f, 0.30f) : b.F > 0.3f ? new Color(0.95f, 0.80f, 0.20f) : new Color(0.92f, 0.22f, 0.16f);
                float w = b.W * b.F;
                Billboard(_unlit, b.P - right * (b.W - w) * 0.5f - fwd * 0.05f, w, 0.2f, c, right, up);
            }
            BarsDrawn = _bars.Count;
            _bars.Clear();

            _lit.ApplyTo(_litMesh);
            _unlit.ApplyTo(_unlitMesh);
        }

        private static Vector3 Arc(Shot s, float k) => Vector3.Lerp(s.From, s.To, k) + Vector3.up * (4f * s.Arc * k * (1f - k));

        private static void Streak(MeshBuilder mb, Vector3 a, Vector3 b, float w, Color c, Vector3 fwd)
        {
            var d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            var side = Vector3.Cross(d.normalized, fwd);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
            side = side.normalized * (w * 0.5f);
            var tailC = Color.Lerp(c, new Color(0.4f, 0.35f, 0.3f), 0.6f);
            mb.Tri(a, b + side, b - side, tailC);
            mb.Tri(a, b - side, b + side, tailC);
            mb.Quad(b - side, b + side, b + side + d.normalized * w, b - side + d.normalized * w, c);
            mb.Quad(b - side, b - side + d.normalized * w, b + side + d.normalized * w, b + side, c);
        }

        private static void Billboard(MeshBuilder mb, Vector3 c, float w, float h, Color col, Vector3 right, Vector3 up)
        {
            var r = right * (w * 0.5f); var u = up * (h * 0.5f);
            mb.Quad(c - r - u, c - r + u, c + r + u, c + r - u, col);
        }

        private static void Star(MeshBuilder mb, Vector3 c, float s, Color col, Vector3 right, Vector3 up)
        {
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f + 0.4f;
                var d = (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * s;
                var n = (right * -Mathf.Sin(a) + up * Mathf.Cos(a)) * s * 0.28f;
                mb.Tri(c - n, c + n, c + d, col);
                mb.Tri(c + n, c - n, c + d, col);
            }
        }
    }
}
