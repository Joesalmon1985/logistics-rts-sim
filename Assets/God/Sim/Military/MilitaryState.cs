using System;
using System.Collections.Generic;

namespace God.Sim
{
    public sealed partial class GodState
    {
        /// <summary>Units, building health, marks, defend flags, waves and the game result (P3). Null until <see cref="MilitaryModule.StartGame"/>.</summary>
        public MilitaryState Military;
    }

    public enum TargetKind { None = 0, Unit = 1, Building = 2 }

    public enum GameOutcome { Playing = 0, Won = 1, Lost = 2 }

    /// <summary>What a unit is doing now (its current order as the brain resolved it).</summary>
    public enum UnitActivity
    {
        Idle = 0,
        /// <summary>Dev order: walk to a point, see <see cref="DevMoveUnit"/>.</summary>
        Moving = 1,
        /// <summary>Shooting or closing on a target in sight.</summary>
        Fighting = 2,
        /// <summary>Walking to the first marked target (player) or the wave target (enemy).</summary>
        Marching = 3,
        /// <summary>Standing at its defend anchor.</summary>
        Guarding = 4,
        /// <summary>Walking back to its defend anchor.</summary>
        Returning = 5,
        /// <summary>Waiting near the nearest HQ.</summary>
        Waiting = 6,
    }

    public sealed class MilUnit
    {
        public int Id;
        public Faction Faction;
        public UnitKind Kind;
        /// <summary>Ground position in millimetres.</summary>
        public int X, Z;
        public int Health;
        public int MaxHealth;
        /// <summary>Ticks until the weapon is ready (0 = ready).</summary>
        public int Cooldown;
        /// <summary>Ticks the "just fired" flash still shows (0 = not firing).</summary>
        public int FireFlash;
        /// <summary>Last non-zero movement step (mm), for facing.</summary>
        public int FaceX, FaceZ;
        public UnitActivity Activity;
        public TargetKind TargetKind;
        public int TargetId;
        /// <summary>Defend flag this unit guards, 0 for none.</summary>
        public int GuardFlagId;
        /// <summary>True while a guard walks back to its anchor (hysteresis).</summary>
        public bool Returning;
        /// <summary>Enemy wave group this unit belongs to (0 = none).</summary>
        public int WaveId;
        /// <summary>Dev move order goal (mm), valid while HasGoal.</summary>
        public int GoalX, GoalZ;
        public bool HasGoal;

        // Path cache (derived, not part of the state hash): smoothed waypoints as node ids.
        public int[] Path;
        public int PathIdx;
        public int PathGoalNode = -1;

        public float HealthFraction => MaxHealth <= 0 ? 0f : (float)Health / MaxHealth;
        public bool IsFiring => FireFlash > 0;
        public bool HasTarget => TargetKind != TargetKind.None;
        public double PosX => X / 1000.0;
        public double PosZ => Z / 1000.0;

        /// <summary>Heading in degrees, 0 = +X, counter-clockwise towards +Z; view use only.</summary>
        public float FacingDegrees => (float)(Math.Atan2(FaceZ, FaceX) * (180.0 / Math.PI));
    }

    /// <summary>Health of one building or HQ, plus where it stands (mm).</summary>
    public sealed class BuildingRecord
    {
        public int Id;
        public Faction Faction;
        public int Node;
        public BuildingSize Size;
        public string Kind;
        public int Health;
        public int MaxHealth;
        public int X, Z;
        public bool IsHq => Size == BuildingSize.Hq;
        public float HealthFraction => MaxHealth <= 0 ? 0f : (float)Health / MaxHealth;
        public bool Damaged => Health < MaxHealth;
    }

    /// <summary>What a destroyed building leaves for 30 s.</summary>
    public sealed class Rubble
    {
        public int Id;
        public Faction Faction;
        public int Node;
        public BuildingSize Size;
        public string Kind;
        public int X, Z;
        public int TicksLeft;
    }

    /// <summary>A player defend flag: an anchor (own building or HQ, or a land node) and a guard count 0..5.</summary>
    public sealed class DefendFlag
    {
        public int Id;
        /// <summary>Building or HQ id of the anchor, 0 when it is a bare node.</summary>
        public int AnchorBuildingId;
        public int Node;
        public int X, Z;
        public int Guards;
    }

    /// <summary>One enemy wave: its members attack the target (a player building or unit) and re-pick when it dies.</summary>
    public sealed class WaveGroup
    {
        public int Id;
        public TargetKind TargetKind;
        public int TargetId;
    }

    /// <summary>Military state (P3). Plain data plus read-only queries: this is the snapshot API for views.</summary>
    public sealed class MilitaryState
    {
        public List<MilUnit> Units = new List<MilUnit>();
        public int NextUnitId = 1;

        /// <summary>Health records of every building and HQ in territory, ascending id.</summary>
        public List<BuildingRecord> Buildings = new List<BuildingRecord>();
        public List<Rubble> Rubbles = new List<Rubble>();
        public int NextRubbleId = 1;

        /// <summary>Marked enemy buildings or HQs, in the order they were marked.</summary>
        public List<int> Marked = new List<int>();
        /// <summary>Defend flags, oldest first.</summary>
        public List<DefendFlag> Defends = new List<DefendFlag>();
        public int NextDefendId = 1;

        // Waves.
        public int WaveNumber;
        /// <summary>Tick the next wave spawns at.</summary>
        public long NextWaveTick = MilitaryTuning.WaveIntervalTicks;
        public bool WavesStopped;
        /// <summary>Dev switch for tests: false holds the wave clock without ending it for good.</summary>
        public bool WavesEnabled = true;
        public List<WaveGroup> WaveGroups = new List<WaveGroup>();
        public int NextWaveGroupId = 1;

        public GameOutcome Outcome;
        public long GameOverTick;

        /// <summary>Last known node and faction of every building ever seen, by id (for orders whose factory is gone).</summary>
        public Dictionary<int, (Faction faction, int node)> Known = new Dictionary<int, (Faction, int)>();

        private readonly Dictionary<int, BuildingRecord> _byId = new Dictionary<int, BuildingRecord>();

        // ---------------- snapshot API ----------------

        public bool IsGameOver => Outcome != GameOutcome.Playing;

        /// <summary>Ticks until the next wave, or -1 when waves have stopped.</summary>
        public long TicksToNextWave(long tick) => WavesStopped ? -1 : Math.Max(0, NextWaveTick - tick);

        public MilUnit UnitById(int id) { foreach (var u in Units) if (u.Id == id) return u; return null; }

        public BuildingRecord BuildingById(int id) => _byId.TryGetValue(id, out var r) ? r : null;

        /// <summary>Current health of a building or HQ, or -1 when it is not there.</summary>
        public int BuildingHealth(int id) => BuildingById(id)?.Health ?? -1;

        public float BuildingHealthFraction(int id) => BuildingById(id)?.HealthFraction ?? 0f;

        public IReadOnlyList<int> MarkedTargets => Marked;

        /// <summary>1-based position in the marked list, or 0 when it is not marked.</summary>
        public int MarkOrder(int buildingId) => Marked.IndexOf(buildingId) + 1;

        public DefendFlag DefendById(int id) { foreach (var f in Defends) if (f.Id == id) return f; return null; }
        public DefendFlag DefendAtBuilding(int buildingId) { foreach (var f in Defends) if (f.AnchorBuildingId == buildingId && buildingId != 0) return f; return null; }
        public DefendFlag DefendAtNode(int node) { foreach (var f in Defends) if (f.AnchorBuildingId == 0 && f.Node == node) return f; return null; }

        /// <summary>Guards currently assigned to a flag.</summary>
        public int GuardCount(int flagId)
        {
            int n = 0;
            foreach (var u in Units) if (u.GuardFlagId == flagId) n++;
            return n;
        }

        public WaveGroup WaveById(int id) { foreach (var w in WaveGroups) if (w.Id == id) return w; return null; }

        public int CountUnits(Faction f)
        {
            int n = 0;
            foreach (var u in Units) if (u.Faction == f) n++;
            return n;
        }

        // ---------------- internal ----------------

        internal void IndexBuildings()
        {
            _byId.Clear();
            foreach (var r in Buildings) _byId[r.Id] = r;
        }

        /// <summary>FNV-1a hash over the whole military state and the territory it sits on, for determinism tests.</summary>
        public ulong Hash(GodState s)
        {
            ulong h = 14695981039346656037UL;
            void Mix(long v)
            {
                unchecked
                {
                    for (int i = 0; i < 8; i++) { h ^= (ulong)((v >> (8 * i)) & 0xFF); h *= 1099511628211UL; }
                }
            }
            Mix(s.Tick);
            foreach (var u in Units)
            {
                Mix(u.Id); Mix((int)u.Faction); Mix((int)u.Kind); Mix(u.X); Mix(u.Z); Mix(u.Health); Mix(u.Cooldown);
                Mix(u.FaceX); Mix(u.FaceZ); Mix((int)u.Activity); Mix((int)u.TargetKind); Mix(u.TargetId); Mix(u.GuardFlagId); Mix(u.WaveId);
            }
            Mix(-1);
            foreach (var b in Buildings) { Mix(b.Id); Mix(b.Health); }
            Mix(-2);
            foreach (var r in Rubbles) { Mix(r.Id); Mix(r.Node); Mix(r.TicksLeft); }
            Mix(-3);
            foreach (int m in Marked) Mix(m);
            Mix(-4);
            foreach (var d in Defends) { Mix(d.Id); Mix(d.AnchorBuildingId); Mix(d.Node); Mix(d.Guards); }
            Mix(-5);
            Mix(WaveNumber); Mix(NextWaveTick); Mix(WavesStopped ? 1 : 0); Mix((int)Outcome); Mix(GameOverTick);
            foreach (var w in WaveGroups) { Mix(w.Id); Mix((int)w.TargetKind); Mix(w.TargetId); }
            var t = s.Territory;
            if (t != null)
            {
                Mix(t.NextId); Mix(t.Hqs.Count); Mix(t.Buildings.Count); Mix(t.Flags.Count); Mix(t.Roads.Count);
                foreach (var b in t.Buildings) { Mix(b.Id); Mix(b.Node); }
            }
            return h;
        }
    }
}
