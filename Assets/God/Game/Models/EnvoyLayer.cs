using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P8 trade envoys: every envoy in the sim is drawn as a herald in its faction's colour leading a pack donkey, with a
    /// bundle on the donkey for each ware it carries (a long trip shows what is at stake). Positions follow the sim's
    /// integer ground positions, smoothed between ticks; the donkey trails the herald on a rope. A damaged envoy shows a
    /// small health bar. Figures are pooled. Reads the sim only.
    /// </summary>
    public sealed class EnvoyLayer : MonoBehaviour
    {
        public const float Scale = 1.15f;
        private const int Slots = 3;

        private sealed class Fig
        {
            public GameObject Herald, Donkey;
            public Transform Body, LegL, LegR, ArmL, ArmR, Rope;
            public Transform[] DLegs = new Transform[4];
            public MeshFilter BodyF, ArmLF, ArmRF, DonkeyF;
            public Transform[] Bundle = new Transform[Slots]; public MeshFilter[] BundleF = new MeshFilter[Slots];
            public Ware[] Shown = new Ware[Slots];
            public Vector3 Pos, DPos; public float Yaw, DYaw, Phase, DPhase; public bool Placed;
            public Faction Faction = (Faction)(-1);
            public int Id;
        }

        private GodSim _sim;
        private readonly Dictionary<int, Fig> _figs = new Dictionary<int, Fig>();
        private readonly Stack<Fig> _pool = new Stack<Fig>();
        private readonly HashSet<int> _alive = new HashSet<int>();
        private readonly List<int> _gone = new List<int>();

        /// <summary>Envoys drawn now (for tests).</summary>
        public int Count => _figs.Count;
        /// <summary>Bundles shown on all donkeys now.</summary>
        public int BundlesNow { get; private set; }
        public bool HasFigure(int envoyId) => _figs.ContainsKey(envoyId);
        /// <summary>The herald's position in the world, for tests and the HUD's picking.</summary>
        public bool TryGetPosition(int envoyId, out Vector3 p)
        {
            if (_figs.TryGetValue(envoyId, out var f)) { p = f.Pos; return true; }
            p = default; return false;
        }

        public static EnvoyLayer Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("EnvoyLayer");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<EnvoyLayer>();
            l._sim = sim;
            FxLayer.Ensure(parent);
            return l;
        }

        private Fig Take(Envoy e)
        {
            var fig = _pool.Count > 0 ? _pool.Pop() : Build();
            fig.Id = e.Id; fig.Placed = false;
            if (fig.Faction != e.Faction)
            {
                fig.Faction = e.Faction;
                fig.BodyF.sharedMesh = EnvoyModels.HeraldBody(e.Faction);
                fig.ArmLF.sharedMesh = EnvoyModels.HeraldArm(e.Faction);
                fig.ArmRF.sharedMesh = EnvoyModels.StaffArm(e.Faction);
                fig.DonkeyF.sharedMesh = EnvoyModels.DonkeyBody(e.Faction);
            }
            for (int i = 0; i < Slots; i++) { fig.Shown[i] = Ware.None; fig.BundleF[i].sharedMesh = null; }
            fig.Herald.SetActive(true); fig.Donkey.SetActive(true); fig.Rope.gameObject.SetActive(true);
            return fig;
        }

        private Fig Build()
        {
            var f = new Fig();
            f.Herald = new GameObject("Envoy"); f.Herald.transform.SetParent(transform, false);
            f.Herald.transform.localScale = Vector3.one * Scale;
            var body = new GameObject("Body"); body.transform.SetParent(f.Herald.transform, false); f.Body = body.transform;
            f.BodyF = ModelKit.Part("Torso", f.Body, null).GetComponent<MeshFilter>();
            f.LegL = ModelKit.Part("LegL", f.Body, EnvoyModels.HeraldLeg()).transform; f.LegL.localPosition = new Vector3(-PeopleModels.LegX, PeopleModels.Hip, 0);
            f.LegR = ModelKit.Part("LegR", f.Body, EnvoyModels.HeraldLeg()).transform; f.LegR.localPosition = new Vector3(PeopleModels.LegX, PeopleModels.Hip, 0);
            var al = ModelKit.Part("ArmL", f.Body, null); f.ArmL = al.transform; f.ArmL.localPosition = new Vector3(-PeopleModels.ShoulderX, PeopleModels.Shoulder, 0); f.ArmLF = al.GetComponent<MeshFilter>();
            var ar = ModelKit.Part("StaffArm", f.Body, null); f.ArmR = ar.transform; f.ArmR.localPosition = new Vector3(PeopleModels.ShoulderX, PeopleModels.Shoulder, 0); f.ArmRF = ar.GetComponent<MeshFilter>();

            f.Donkey = new GameObject("Pack donkey"); f.Donkey.transform.SetParent(transform, false);
            f.Donkey.transform.localScale = Vector3.one * Scale;
            var db = new GameObject("Body"); db.transform.SetParent(f.Donkey.transform, false);
            f.DonkeyF = ModelKit.Part("Donkey", db.transform, null).GetComponent<MeshFilter>();
            int i = 0;
            foreach (float z in new[] { 0.55f, -0.55f })
                foreach (float x in new[] { -0.2f, 0.2f })
                {
                    var leg = ModelKit.Part("Leg", db.transform, EnvoyModels.DonkeyLeg()).transform;
                    leg.localPosition = new Vector3(x, EnvoyModels.DonkeyHip, z);
                    f.DLegs[i++] = leg;
                }
            // Bundle slots: on top of the back, and one each side.
            Vector3[] at = { new Vector3(0, 0f, 0), new Vector3(-0.42f, -0.2f, 0), new Vector3(0.42f, -0.2f, 0) };
            for (int s = 0; s < Slots; s++)
            {
                var b = ModelKit.Part("Bundle" + s, db.transform, null);
                float k = s == 0 ? 0.62f : 0.5f;
                b.transform.localScale = Vector3.one * k;
                b.transform.localPosition = new Vector3(at[s].x, EnvoyModels.BackY + 0.04f + at[s].y - PeopleModels.HeadTop * k, at[s].z);
                f.Bundle[s] = b.transform; f.BundleF[s] = b.GetComponent<MeshFilter>();
            }
            f.Rope = ModelKit.Part("Rope", transform, EnvoyModels.Rope()).transform;
            return f;
        }

        private static Vector3 Ground(int xMm, int zMm, float lift = 0f) => ModelKit.OnGround(xMm / 1000f, zMm / 1000f, lift);

        private void LateUpdate()
        {
            var tr = _sim.State.Trade;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            _alive.Clear();
            BundlesNow = 0;
            if (tr != null)
                foreach (var e in tr.Envoys)
                {
                    _alive.Add(e.Id);
                    if (!_figs.TryGetValue(e.Id, out var f)) { f = Take(e); _figs[e.Id] = f; }
                    Animate(f, e, dt);
                }
            _gone.Clear();
            foreach (var kv in _figs) if (!_alive.Contains(kv.Key)) _gone.Add(kv.Key);
            var fx = FxLayer.Instance;
            foreach (int id in _gone)
            {
                var f = _figs[id]; _figs.Remove(id);
                if (fx != null) fx.Dust(f.Pos, 0.7f, 4, new Color(0.62f, 0.56f, 0.46f));
                f.Herald.SetActive(false); f.Donkey.SetActive(false); f.Rope.gameObject.SetActive(false);
                _pool.Push(f);
            }
        }

        private void Animate(Fig f, Envoy e, float dt)
        {
            var target = Ground(e.X, e.Z);
            if (!f.Placed || (target - f.Pos).sqrMagnitude > 900f)
            {
                f.Pos = target; f.Placed = true;
                f.Yaw = (e.Body.FaceX == 0 && e.Body.FaceZ == 0) ? 180f : Mathf.Atan2(e.Body.FaceX, e.Body.FaceZ) * Mathf.Rad2Deg;
                f.DYaw = f.Yaw;
                var back = ModelKit.Dir(f.Yaw) * -EnvoyModels.Trail * Scale;
                f.DPos = f.Pos + back; f.DPos.y = ModelKit.GroundY(f.DPos.x, f.DPos.z);
            }
            var prev = f.Pos;
            f.Pos = Vector3.Lerp(f.Pos, target, 1f - Mathf.Exp(-dt * 12f));
            f.Pos.y = ModelKit.GroundY(f.Pos.x, f.Pos.z);
            var moved = f.Pos - prev; moved.y = 0;
            float speed = moved.magnitude / Mathf.Max(dt, 1e-4f);
            float wantYaw = (e.Body.FaceX == 0 && e.Body.FaceZ == 0) ? f.Yaw : Mathf.Atan2(e.Body.FaceX, e.Body.FaceZ) * Mathf.Rad2Deg;
            if (moved.sqrMagnitude > 1e-5f) wantYaw = ModelKit.Yaw(moved);
            f.Yaw = Mathf.LerpAngle(f.Yaw, wantYaw, 1f - Mathf.Exp(-dt * 10f));
            f.Herald.transform.SetPositionAndRotation(f.Pos, Quaternion.Euler(0, f.Yaw, 0));

            // The donkey keeps a rope's length behind the herald and faces the way it is pulled.
            float trail = EnvoyModels.Trail * Scale;
            var toLeader = f.Pos - f.DPos; toLeader.y = 0;
            float d = toLeader.magnitude;
            var dprev = f.DPos;
            if (d > trail)
            {
                var goal = f.Pos - toLeader / d * trail;
                f.DPos = Vector3.Lerp(f.DPos, goal, 1f - Mathf.Exp(-dt * 14f));
                if (d > trail * 4f) f.DPos = goal;
            }
            f.DPos.y = ModelKit.GroundY(f.DPos.x, f.DPos.z);
            var dmoved = f.DPos - dprev; dmoved.y = 0;
            float dspeed = dmoved.magnitude / Mathf.Max(dt, 1e-4f);
            if (toLeader.sqrMagnitude > 0.04f) f.DYaw = Mathf.LerpAngle(f.DYaw, ModelKit.Yaw(toLeader), 1f - Mathf.Exp(-dt * 8f));
            f.Donkey.transform.SetPositionAndRotation(f.DPos, Quaternion.Euler(0, f.DYaw, 0));

            // Walking.
            float walk = Mathf.Clamp01(speed / 2.5f);
            f.Phase += speed * dt * 2.2f;
            float swing = Mathf.Sin(f.Phase) * 32f * walk;
            f.LegL.localRotation = Quaternion.Euler(swing, 0, 0);
            f.LegR.localRotation = Quaternion.Euler(-swing, 0, 0);
            f.Body.localPosition = Vector3.up * (Mathf.Abs(Mathf.Cos(f.Phase)) * 0.07f * walk);
            f.ArmL.localRotation = Quaternion.Euler(-swing, 0, 0);
            f.ArmR.localRotation = Quaternion.Euler(-12f, 0, 0);

            float dwalk = Mathf.Clamp01(dspeed / 2.5f);
            f.DPhase += dspeed * dt * 2.6f;
            float ds = Mathf.Sin(f.DPhase) * 26f * dwalk;
            f.DLegs[0].localRotation = Quaternion.Euler(ds, 0, 0);    // front left
            f.DLegs[1].localRotation = Quaternion.Euler(-ds, 0, 0);   // front right
            f.DLegs[2].localRotation = Quaternion.Euler(-ds, 0, 0);   // rear left
            f.DLegs[3].localRotation = Quaternion.Euler(ds, 0, 0);    // rear right
            f.Donkey.transform.GetChild(0).localPosition = Vector3.up * (Mathf.Abs(Mathf.Cos(f.DPhase)) * 0.04f * dwalk);

            // The rope: from the herald's hand to the donkey's halter.
            var hand = f.Herald.transform.TransformPoint(new Vector3(-0.42f, 1.0f, 0.2f));
            var halter = f.Donkey.transform.TransformPoint(new Vector3(0, 1.6f, 1.3f));
            var span = halter - hand;
            if (span.sqrMagnitude > 0.001f)
            {
                f.Rope.position = hand;
                f.Rope.rotation = Quaternion.LookRotation(span, Vector3.up);
                f.Rope.localScale = new Vector3(1, 1, span.magnitude);
            }

            ShowCargo(f, e);

            if (e.Health < TradeTuning.EnvoyHealth && FxLayer.Instance != null)
                FxLayer.Instance.HealthBar(f.Pos + Vector3.up * 3.2f, 1.6f, e.HealthFraction);
        }

        /// <summary>One bundle per ware (the biggest first), two more of the biggest for a heavy load (7 or more).</summary>
        private void ShowCargo(Fig f, Envoy e)
        {
            var kinds = e.CargoList();
            kinds.Sort((a, b) => b.Count != a.Count ? b.Count.CompareTo(a.Count) : ((int)a.Ware).CompareTo((int)b.Ware));
            int total = e.CargoTotal;
            int slots = kinds.Count == 0 ? 0 : Mathf.Min(Slots, Mathf.Max(kinds.Count, 1 + (total - 1) / 3));
            for (int s = 0; s < Slots; s++)
            {
                Ware w = s < slots ? kinds[s % kinds.Count].Ware : Ware.None;
                if (w != f.Shown[s]) { f.Shown[s] = w; f.BundleF[s].sharedMesh = w == Ware.None ? null : EnvoyModels.Bundle(w); }
                if (w != Ware.None) BundlesNow++;
            }
        }
    }
}
