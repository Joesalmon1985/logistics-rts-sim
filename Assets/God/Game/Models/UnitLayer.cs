using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// P4b units: small soldiers (spear and javelins for the player, crossbows for the enemy, shields in the faction
    /// colour) and four-wheeled war wagons with a turning bolt thrower. They face their movement (soldiers turn to their
    /// target while shooting), walk or roll smoothly between sim ticks, fire tracers with a muzzle flash, show a small
    /// health bar only when damaged, and on death fall over and sink away. Figures are pooled. Reads the sim only.
    /// </summary>
    public sealed class UnitLayer : MonoBehaviour
    {
        public const float InfantryScale = 1.2f;

        private sealed class Fig
        {
            public GameObject Root; public Transform Body, LegL, LegR, ArmL, ArmR, Thrower; public Transform[] Wheels;
            public UnitKind Kind; public Faction Faction;
            public Vector3 Pos; public float Yaw, Phase, Roll; public bool Placed;
            public int LastCooldown; public bool LastFiring;
            public float DeathT = -1f; public float Fall;
        }

        private GodSim _sim;
        private readonly Dictionary<int, Fig> _figs = new Dictionary<int, Fig>();
        private readonly List<Fig> _dying = new List<Fig>();
        private readonly Dictionary<int, Stack<Fig>> _pool = new Dictionary<int, Stack<Fig>>();
        private readonly HashSet<int> _alive = new HashSet<int>();
        private readonly List<int> _gone = new List<int>();

        public int Count => _figs.Count;
        public int DyingNow => _dying.Count;
        public int DeathsSoFar { get; private set; }
        public int CountOf(UnitKind k, Faction f) { int n = 0; foreach (var x in _figs.Values) if (x.Kind == k && x.Faction == f) n++; return n; }

        public static UnitLayer Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("UnitLayer");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<UnitLayer>();
            l._sim = sim;
            FxLayer.Ensure(parent);
            return l;
        }

        private static int PoolKey(UnitKind k, Faction f) => (int)k * FactionInfo.Max + (int)f;

        private Fig Take(UnitKind k, Faction f)
        {
            Fig fig = null;
            if (_pool.TryGetValue(PoolKey(k, f), out var st) && st.Count > 0) fig = st.Pop();
            if (fig == null) fig = k == UnitKind.Infantry ? BuildInfantry(f) : BuildWagon(f);
            fig.Placed = false; fig.DeathT = -1f; fig.Fall = 0f;
            fig.Root.transform.localScale = Vector3.one * (k == UnitKind.Infantry ? InfantryScale : 1f);
            fig.Body.localRotation = Quaternion.identity; fig.Body.localPosition = Vector3.zero;
            fig.Root.SetActive(true);
            return fig;
        }

        private void Release(Fig f)
        {
            f.Root.SetActive(false);
            int key = PoolKey(f.Kind, f.Faction);
            if (!_pool.TryGetValue(key, out var st)) _pool[key] = st = new Stack<Fig>();
            st.Push(f);
        }

        private Fig BuildInfantry(Faction fa)
        {
            var f = new Fig { Kind = UnitKind.Infantry, Faction = fa, Root = new GameObject("Infantry " + fa) };
            f.Root.transform.SetParent(transform, false);
            var body = new GameObject("Body"); body.transform.SetParent(f.Root.transform, false); f.Body = body.transform;
            ModelKit.Part("Torso", f.Body, UnitModels.InfantryBody(fa));
            f.LegL = ModelKit.Part("LegL", f.Body, UnitModels.Leg(fa)).transform; f.LegL.localPosition = new Vector3(-PeopleModels.LegX, PeopleModels.Hip, 0);
            f.LegR = ModelKit.Part("LegR", f.Body, UnitModels.Leg(fa)).transform; f.LegR.localPosition = new Vector3(PeopleModels.LegX, PeopleModels.Hip, 0);
            f.ArmL = ModelKit.Part("Shield arm", f.Body, UnitModels.ShieldArm(fa)).transform; f.ArmL.localPosition = new Vector3(-PeopleModels.ShoulderX, PeopleModels.Shoulder, 0);
            f.ArmR = ModelKit.Part("Weapon arm", f.Body, UnitModels.WeaponArm(fa)).transform; f.ArmR.localPosition = new Vector3(PeopleModels.ShoulderX, PeopleModels.Shoulder, 0);
            return f;
        }

        private Fig BuildWagon(Faction fa)
        {
            var f = new Fig { Kind = UnitKind.Vehicle, Faction = fa, Root = new GameObject("Wagon " + fa) };
            f.Root.transform.SetParent(transform, false);
            var body = new GameObject("Body"); body.transform.SetParent(f.Root.transform, false); f.Body = body.transform;
            ModelKit.Part("Bed", f.Body, UnitModels.WagonBody(fa));
            f.Wheels = new Transform[4];
            int i = 0;
            foreach (float z in new[] { 1.1f, -1.1f })
                foreach (float x in new[] { -1.15f, 1.15f })
                {
                    var w = ModelKit.Part("Wheel", f.Body, UnitModels.WagonWheel()).transform;
                    w.localPosition = new Vector3(x, UnitModels.WagonWheelR, z);
                    f.Wheels[i++] = w;
                }
            f.Thrower = ModelKit.Part("Thrower", f.Body, UnitModels.Thrower(fa)).transform;
            f.Thrower.localPosition = new Vector3(0, 1.15f, 0.2f);
            return f;
        }

        private static Vector3 Ground(int xMm, int zMm, float lift = 0f) => ModelKit.OnGround(xMm / 1000f, zMm / 1000f, lift);

        private void LateUpdate()
        {
            var s = _sim.State; var m = s.Military;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            var fx = FxLayer.Instance;
            _alive.Clear();
            if (m != null)
                foreach (var u in m.Units)
                {
                    _alive.Add(u.Id);
                    if (!_figs.TryGetValue(u.Id, out var f)) { f = Take(u.Kind, u.Faction); f.LastCooldown = u.Cooldown; f.LastFiring = u.IsFiring; _figs[u.Id] = f; }
                    Animate(f, u, m, dt, fx);
                }
            _gone.Clear();
            foreach (var kv in _figs) if (!_alive.Contains(kv.Key)) _gone.Add(kv.Key);
            foreach (int id in _gone)
            {
                var f = _figs[id]; _figs.Remove(id);
                f.DeathT = 0f; DeathsSoFar++;
                _dying.Add(f);
                if (fx != null)
                {
                    fx.Dust(f.Pos, f.Kind == UnitKind.Vehicle ? 1.6f : 0.6f, f.Kind == UnitKind.Vehicle ? 8 : 3, new Color(0.6f, 0.55f, 0.46f));
                    if (f.Kind == UnitKind.Vehicle) fx.Spark(f.Pos + Vector3.up * 1.4f, 1.6f, new Color(1f, 0.6f, 0.2f), 0.3f);
                }
            }
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                var f = _dying[i];
                f.DeathT += dt;
                // A quick fall, then it sinks into the ground and is gone.
                float fall = Mathf.Clamp01(f.DeathT / 0.35f);
                float sink = Mathf.Clamp01((f.DeathT - 0.9f) / 1.1f);
                if (f.Kind == UnitKind.Infantry) f.Body.localRotation = Quaternion.Euler(0, 0, 88f * fall * fall);
                else f.Body.localRotation = Quaternion.Euler(14f * fall, 0, 18f * fall);
                f.Root.transform.position = f.Pos + Vector3.down * (sink * 1.8f);
                if (fx != null && f.Kind == UnitKind.Vehicle && f.DeathT < 1.5f && fx.Rand(0f, 1f) < 0.4f) fx.Smoke(f.Pos + Vector3.up * 1.2f, new Color(0.22f, 0.21f, 0.2f), 0.7f, 1.8f, 2f);
                if (sink >= 1f) { _dying.RemoveAt(i); Release(f); }
            }
        }

        private void Animate(Fig f, MilUnit u, MilitaryState m, float dt, FxLayer fx)
        {
            var target = Ground(u.X, u.Z);
            if (!f.Placed || (target - f.Pos).sqrMagnitude > 400f)
            {
                f.Pos = target; f.Placed = true;
                f.Yaw = (u.FaceX == 0 && u.FaceZ == 0) ? 180f : Mathf.Atan2(u.FaceX, u.FaceZ) * Mathf.Rad2Deg;
            }
            var prev = f.Pos;
            f.Pos = Vector3.Lerp(f.Pos, target, 1f - Mathf.Exp(-dt * 12f));
            f.Pos.y = ModelKit.GroundY(f.Pos.x, f.Pos.z);
            var moved = f.Pos - prev; moved.y = 0;
            float dist = moved.magnitude;
            float speed = dist / Mathf.Max(dt, 1e-4f);

            // Aim: where its target stands.
            Vector3? aim = null;
            if (u.HasTarget)
            {
                if (u.TargetKind == TargetKind.Unit) { var tu = m.UnitById(u.TargetId); if (tu != null) aim = Ground(tu.X, tu.Z, tu.Kind == UnitKind.Vehicle ? 1.4f : 1.4f); }
                else { var tb = m.BuildingById(u.TargetId); if (tb != null) aim = Ground(tb.X, tb.Z, tb.IsHq ? 4f : 2.5f); }
            }

            float wantYaw = (u.FaceX == 0 && u.FaceZ == 0) ? f.Yaw : Mathf.Atan2(u.FaceX, u.FaceZ) * Mathf.Rad2Deg;
            if (dist > 0.01f) wantYaw = ModelKit.Yaw(moved);
            if (f.Kind == UnitKind.Infantry && aim.HasValue && speed < 0.6f) wantYaw = ModelKit.Yaw(aim.Value - f.Pos);
            f.Yaw = Mathf.LerpAngle(f.Yaw, wantYaw, 1f - Mathf.Exp(-dt * 9f));
            f.Root.transform.SetPositionAndRotation(f.Pos, Quaternion.Euler(0, f.Yaw, 0));

            bool fired = u.Cooldown > f.LastCooldown || (u.IsFiring && !f.LastFiring);
            f.LastCooldown = u.Cooldown; f.LastFiring = u.IsFiring;

            if (f.Kind == UnitKind.Infantry)
            {
                float walk = Mathf.Clamp01(speed / 1.5f);
                f.Phase += speed * dt * 2.6f;
                float swing = Mathf.Sin(f.Phase) * 34f * walk;
                f.LegL.localRotation = Quaternion.Euler(swing, 0, 0);
                f.LegR.localRotation = Quaternion.Euler(-swing, 0, 0);
                f.Body.localPosition = Vector3.up * (Mathf.Abs(Mathf.Cos(f.Phase)) * 0.07f * walk);
                f.ArmL.localRotation = Quaternion.Euler(-35f, 0, 0);
                if (fired) f.Roll = 1f;
                f.Roll = Mathf.MoveTowards(f.Roll, 0f, dt * 4f);
                // The throw: the weapon arm goes back, then snaps forward.
                float arm = !FactionLook.Stone(f.Faction) ? Mathf.Lerp(-15f - swing * 0.4f, -125f, f.Roll) : Mathf.Lerp(-80f, -95f, f.Roll);
                f.ArmR.localRotation = Quaternion.Euler(arm, 0, 0);
            }
            else
            {
                f.Phase += dist / UnitModels.WagonWheelR * Mathf.Rad2Deg;
                foreach (var w in f.Wheels) w.localRotation = Quaternion.Euler(f.Phase, 0, 0);
                f.Body.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 9f) * 0.8f * Mathf.Clamp01(speed / 3f), 0, 0);
                float turretYaw = 0f;
                if (aim.HasValue) turretYaw = Mathf.DeltaAngle(f.Yaw, ModelKit.Yaw(aim.Value - f.Pos));
                f.Thrower.localRotation = Quaternion.Slerp(f.Thrower.localRotation, Quaternion.Euler(0, turretYaw, 0), 1f - Mathf.Exp(-dt * 8f));
            }

            if (fired && aim.HasValue && fx != null)
            {
                Vector3 from;
                if (f.Kind == UnitKind.Infantry) from = f.Root.transform.TransformPoint(new Vector3(0.4f, 1.9f, 0.6f));
                else from = f.Thrower.TransformPoint(new Vector3(0, 0.6f, 1.5f));
                bool heavy = f.Kind == UnitKind.Vehicle;
                var col = heavy ? new Color(1f, 0.72f, 0.32f) : !FactionLook.Stone(f.Faction) ? new Color(1f, 0.92f, 0.62f) : new Color(1f, 0.80f, 0.55f);
                fx.Shoot(from, aim.Value, heavy, col);
            }

            if (u.Health < u.MaxHealth && fx != null)
                fx.HealthBar(f.Pos + Vector3.up * UnitModels.BarHeight(u.Kind), UnitModels.BarWidth(u.Kind), u.HealthFraction);
        }
    }
}
