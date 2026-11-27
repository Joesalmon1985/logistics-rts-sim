using System.Collections.Generic;
using God.Sim;
using UnityEngine;

namespace God.Game
{
    /// <summary>
    /// Stage 3 units: small soldiers dressed in their kit (<see cref="UnitModels"/>: the weapon in hand, a big or small
    /// shield, a helmet, a breastplate or leather jerkin, all marked in the faction colour). They face their movement
    /// (and their foe while fighting), walk smoothly between sim ticks, and strike in the manner of their weapon: a chop
    /// for sword and axe, a thrust for the spear, an aimed draw for the bow, a whirl for the sling. Bows and slings loose a
    /// quick arrow or stone; every blow or missile shows how it ended (a flash, a big red-hot flash for a crit, a clang
    /// and pale puff for a block, a grey puff for a miss). A small health bar shows only when damaged; on death they fall
    /// over and sink away. Figures are pooled per faction and kit. Reads the sim only.
    /// </summary>
    public sealed class UnitLayer : MonoBehaviour
    {
        public const float InfantryScale = 1.2f;

        private sealed class Fig
        {
            public GameObject Root; public Transform Body, LegL, LegR, ArmL, ArmR;
            public Kit Kit; public Faction Faction;
            public Vector3 Pos; public float Yaw, Phase, Roll; public bool Placed;
            public int LastCooldown; public bool LastFiring;
            public float DeathT = -1f;
        }

        private GodSim _sim;
        private readonly Dictionary<int, Fig> _figs = new Dictionary<int, Fig>();
        private readonly List<Fig> _dying = new List<Fig>();
        private readonly Dictionary<long, Stack<Fig>> _pool = new Dictionary<long, Stack<Fig>>();
        private readonly HashSet<int> _alive = new HashSet<int>();
        private readonly List<int> _gone = new List<int>();

        public int Count => _figs.Count;
        public int DyingNow => _dying.Count;
        public int DeathsSoFar { get; private set; }
        /// <summary>Strikes drawn since the layer was made (melee and missiles), for tests.</summary>
        public int StrikesSoFar { get; private set; }
        /// <summary>Soldiers drawn now of a faction (every unit is infantry since stage 3).</summary>
        public int CountOf(UnitKind k, Faction f) { int n = 0; foreach (var x in _figs.Values) if (x.Faction == f) n++; return n; }
        /// <summary>Soldiers drawn now of a faction holding a weapon.</summary>
        public int CountOf(Ware weapon, Faction f) { int n = 0; foreach (var x in _figs.Values) if (x.Faction == f && x.Kit.Weapon == weapon) n++; return n; }
        /// <summary>The figure of a unit, or null (for tests: its parts are named after the kit).</summary>
        public GameObject FigureOf(int unitId) => _figs.TryGetValue(unitId, out var f) ? f.Root : null;

        public static UnitLayer Create(GodSim sim, Transform parent)
        {
            var go = new GameObject("UnitLayer");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<UnitLayer>();
            l._sim = sim;
            FxLayer.Ensure(parent);
            return l;
        }

        private static long PoolKey(Kit k, Faction f) => ((((long)k.Weapon * 32 + (int)k.Shield) * 32 + (int)k.Head) * 32 + (int)k.Body) * 8 + (int)f;

        private Fig Take(Kit k, Faction f)
        {
            Fig fig = null;
            if (_pool.TryGetValue(PoolKey(k, f), out var st) && st.Count > 0) fig = st.Pop();
            if (fig == null) fig = Build(k, f);
            fig.Placed = false; fig.DeathT = -1f; fig.Roll = 0f;
            fig.Root.transform.localScale = Vector3.one * InfantryScale;
            fig.Body.localRotation = Quaternion.identity; fig.Body.localPosition = Vector3.zero;
            fig.Root.SetActive(true);
            return fig;
        }

        private void Release(Fig f)
        {
            f.Root.SetActive(false);
            long key = PoolKey(f.Kit, f.Faction);
            if (!_pool.TryGetValue(key, out var st)) _pool[key] = st = new Stack<Fig>();
            st.Push(f);
        }

        private Fig Build(Kit k, Faction fa)
        {
            var f = new Fig { Kit = k, Faction = fa, Root = new GameObject("Soldier " + fa + " " + k) };
            f.Root.transform.SetParent(transform, false);
            var body = new GameObject("Body"); body.transform.SetParent(f.Root.transform, false); f.Body = body.transform;
            ModelKit.Part("Torso " + Wares.Name(k.Head) + " " + Wares.Name(k.Body), f.Body, UnitModels.Body(fa, k.Head, k.Body));
            f.LegL = ModelKit.Part("LegL", f.Body, UnitModels.Leg(fa)).transform; f.LegL.localPosition = new Vector3(-PeopleModels.LegX, PeopleModels.Hip, 0);
            f.LegR = ModelKit.Part("LegR", f.Body, UnitModels.Leg(fa)).transform; f.LegR.localPosition = new Vector3(PeopleModels.LegX, PeopleModels.Hip, 0);
            f.ArmL = ModelKit.Part("Shield arm " + Wares.Name(k.Shield), f.Body, UnitModels.ShieldArm(fa, k.Shield)).transform; f.ArmL.localPosition = new Vector3(-PeopleModels.ShoulderX, PeopleModels.Shoulder, 0);
            f.ArmR = ModelKit.Part("Weapon arm " + Wares.Name(k.Weapon), f.Body, UnitModels.WeaponArm(fa, k.Weapon)).transform; f.ArmR.localPosition = new Vector3(PeopleModels.ShoulderX, PeopleModels.Shoulder, 0);
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
                    if (!_figs.TryGetValue(u.Id, out var f)) { f = Take(u.Kit, u.Faction); f.LastCooldown = u.Cooldown; f.LastFiring = u.IsFiring; _figs[u.Id] = f; }
                    Animate(f, u, m, dt, fx);
                }
            _gone.Clear();
            foreach (var kv in _figs) if (!_alive.Contains(kv.Key)) _gone.Add(kv.Key);
            foreach (int id in _gone)
            {
                var f = _figs[id]; _figs.Remove(id);
                f.DeathT = 0f; DeathsSoFar++;
                _dying.Add(f);
                if (fx != null) fx.Dust(f.Pos, 0.6f, 3, new Color(0.6f, 0.55f, 0.46f));
            }
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                var f = _dying[i];
                f.DeathT += dt;
                // A quick fall, then it sinks into the ground and is gone.
                float fall = Mathf.Clamp01(f.DeathT / 0.35f);
                float sink = Mathf.Clamp01((f.DeathT - 0.9f) / 1.1f);
                f.Body.localRotation = Quaternion.Euler(0, 0, 88f * fall * fall);
                f.Root.transform.position = f.Pos + Vector3.down * (sink * 1.8f);
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

            // Aim: where its target stands (chest height of a soldier, a wall of a building).
            Vector3? aim = null;
            if (u.HasTarget)
            {
                if (u.TargetKind == TargetKind.Unit) { var tu = m.UnitById(u.TargetId); if (tu != null) aim = Ground(tu.X, tu.Z, 1.4f); }
                else { var tb = m.BuildingById(u.TargetId); if (tb != null) aim = Ground(tb.X, tb.Z, tb.IsHq ? 4f : 2.5f); }
            }

            float wantYaw = (u.FaceX == 0 && u.FaceZ == 0) ? f.Yaw : Mathf.Atan2(u.FaceX, u.FaceZ) * Mathf.Rad2Deg;
            if (dist > 0.01f) wantYaw = ModelKit.Yaw(moved);
            if (aim.HasValue && speed < 0.6f) wantYaw = ModelKit.Yaw(aim.Value - f.Pos);
            f.Yaw = Mathf.LerpAngle(f.Yaw, wantYaw, 1f - Mathf.Exp(-dt * 9f));
            f.Root.transform.SetPositionAndRotation(f.Pos, Quaternion.Euler(0, f.Yaw, 0));

            bool fired = u.Cooldown > f.LastCooldown || (u.IsFiring && !f.LastFiring);
            f.LastCooldown = u.Cooldown; f.LastFiring = u.IsFiring;

            float walk = Mathf.Clamp01(speed / 1.5f);
            f.Phase += speed * dt * 2.6f;
            float swing = Mathf.Sin(f.Phase) * 34f * walk;
            f.LegL.localRotation = Quaternion.Euler(swing, 0, 0);
            f.LegR.localRotation = Quaternion.Euler(-swing, 0, 0);
            f.Body.localPosition = Vector3.up * (Mathf.Abs(Mathf.Cos(f.Phase)) * 0.07f * walk);
            if (fired) f.Roll = 1f;
            f.Roll = Mathf.MoveTowards(f.Roll, 0f, dt * 4f);
            bool fighting = u.Activity == UnitActivity.Fighting && speed < 0.6f;
            Pose(f, swing, fighting);

            if (fired && aim.HasValue && fx != null)
            {
                StrikesSoFar++;
                var hit = FxLayer.ImpactOf(u.LastOutcome);
                if (Gear.IsRanged(f.Kit.Weapon))
                {
                    var from = f.Root.transform.TransformPoint(new Vector3(0.4f, 1.9f, 0.6f));
                    fx.Missile(from, aim.Value, f.Kit.Weapon == Ware.Sling, hit);
                }
                else
                {
                    // The blow lands at once: a swing flash on the foe (or the wall) by how it ended.
                    var at = aim.Value - ModelKit.Dir(f.Yaw) * (u.TargetKind == TargetKind.Unit ? 0.3f : 0f);
                    fx.Strike(at, hit == FxLayer.Impact.None ? FxLayer.Impact.Hit : hit);
                }
            }

            if (u.Health < u.MaxHealth && fx != null)
                fx.HealthBar(f.Pos + Vector3.up * UnitModels.BarHeight, UnitModels.BarWidth, u.HealthFraction);
        }

        /// <summary>Arm poses by weapon: <c>Roll</c> runs 1 to 0 after a strike.</summary>
        private static void Pose(Fig f, float swing, bool fighting)
        {
            float r = f.Roll;
            float shield = f.Kit.Shield != Ware.None ? (fighting ? -55f : -35f) : -10f - swing * 0.4f;
            float arm;
            switch (f.Kit.Weapon)
            {
                case Ware.Spear:
                    // Held upright on the march, levelled in a fight, thrust forward on a strike.
                    arm = fighting ? Mathf.Lerp(-70f, -100f, Mathf.Sin(r * Mathf.PI)) : -15f - swing * 0.4f;
                    break;
                case Ware.Bow:
                    // Raised and aimed while fighting; the off hand draws.
                    arm = fighting ? Mathf.Lerp(-88f, -78f, r) : -10f - swing * 0.4f;
                    shield = fighting ? Mathf.Lerp(-85f, -60f, r) : shield;
                    break;
                case Ware.Sling:
                    // A whirl overhead that snaps forward on the throw.
                    arm = fighting ? (r > 0f ? Mathf.Lerp(-80f, -170f, r) : -150f + Mathf.Sin(Time.time * 18f) * 20f) : -10f - swing * 0.4f;
                    break;
                default:
                    // Sword and axe: raised, then chopped down.
                    arm = fighting ? Mathf.Lerp(-40f, -140f, r) : -15f - swing * 0.4f;
                    break;
            }
            f.ArmR.localRotation = Quaternion.Euler(arm, 0, 0);
            f.ArmL.localRotation = Quaternion.Euler(shield, 0, 0);
        }
    }
}
