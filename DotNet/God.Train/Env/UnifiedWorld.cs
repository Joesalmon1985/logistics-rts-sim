using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Unified;
using God.Train.Honesty;
using God.Train.Net;

namespace God.Train.Env
{
    /// <summary>The shape of one real training or evaluation episode.</summary>
    public sealed class UnifiedConfig
    {
        /// <summary>Persons in the one tribe (a single faction).</summary>
        public int Persons = 1;
        public int NeedMask = 0x033;
        /// <summary>Pain band (permille). UN1 defines none for hunger or thirst, so the thresholds file's default 800 applies.</summary>
        public int PainBand = 800;
        /// <summary>The game's own brain (<c>Unification.Brain.Kind</c>, <c>.Artifact</c>) when no policy is injected.</summary>
        public int BrainKind = God.Sim.Unified.BrainKind.Untrained, Artifact = 0;
        /// <summary>Diagnostics only (the game defaults are 1, 1, 300): the overlay, sampling and the temperament temperature spread.</summary>
        public int Overlay = 1, Sample = 1, TemperatureSpread = 300;
    }

    /// <summary>
    /// The real environment (slice 1): a <c>GodSim</c> with <c>Unification.Enabled = 1</c>, one tribe, the game's own world, needs,
    /// catalogue, executor and encoder. Persons decide <b>at the game's own decision site</b> (UN6 <c>PersonTickUN6</c> through the
    /// real <c>MotorBody</c>): observation = UN2's <c>Percept.Encode</c> + <c>ToVector</c> (Obs v1); legality = <c>Motor.Legal</c>;
    /// a choice decodes through <c>Percept.ControlOf</c> (Act v1); submission is <c>Motor.Submit</c> with the actor's origin; place
    /// and person memory, the overlay and the temperament temperature are the game's. The trainer's actor is injected per world
    /// (<c>BrainRuntime.Inject</c>): a network as a <see cref="CandidatePolicy"/> (the game's integer inference), the teacher through
    /// <see cref="ActorPolicy"/> (origin Teacher); <see cref="GameBrainActor"/> injects nothing and the game's own brain decides.
    /// The body reading is UN1's needs.
    /// <para>Held-out layouts on the real board (evaluation only, through <see cref="EvalAccess"/>): every held-out seed is a
    /// regenerated board (UN1's generator on that seed); on top, <c>mirrored</c> and <c>rotated</c> move the tribe's start to the
    /// passable node nearest the start's left-right mirror / 120-degree rotation about the board centre, and
    /// <c>unfamiliar_start</c> to a passable node far from the start the generator chose (outside the training start region).
    /// The board itself is not mirrored (the generator owns it).</para>
    /// </summary>
    public sealed class UnifiedWorld : ISiteWorld
    {
        public readonly GodSim Sim;
        public readonly UnifiedConfig Config;
        public readonly long Seed;
        public readonly Layout Layout;
        private readonly List<int> _ids = new List<int>();
        private readonly Dictionary<int, HashSet<int>> _visited = new Dictionary<int, HashSet<int>>();
        private readonly Dictionary<int, bool> _adult = new Dictionary<int, bool>();
        private readonly Dictionary<int, string> _cause = new Dictionary<int, string>();
        private readonly Dictionary<int, (int hunger, int thirst, int nutrition)> _last = new Dictionary<int, (int, int, int)>();
        private readonly WorldFacts _facts = new WorldFacts();
        private readonly List<SimEvent> _events = new List<SimEvent>();
        private Percept _p;
        private long _decisions0, _submitted0, _accepted0;
        private bool _evaluation;
        public string Violation;
        /// <summary>Accepted (ControlApplied) and refused-or-failed (ControlFailed) controls per Prim this episode.</summary>
        public readonly long[] Applied = new long[13], Failed = new long[13];
        public readonly Dictionary<string, long> Refused = new Dictionary<string, long>();
        public long TeacherEvents, Drinks, Forages, Sleeps;
        public int StartNode { get; private set; } = -1;

        public UnifiedWorld(UnifiedConfig cfg, long seed, Layout layout, EvalAccess access)
        {
            SeedPolicy.Check(seed, layout, access);
            Config = cfg; Seed = seed; Layout = layout;
            var tuning = new SimTuning();
            tuning.Unification.Enabled = 1;
            tuning.Unification.Needs.ActiveMask = cfg.NeedMask;
            tuning.Unification.Brain.Kind = cfg.BrainKind;
            tuning.Unification.Brain.Artifact = cfg.Artifact;
            tuning.Unification.UN6.Overlay = cfg.Overlay; tuning.Unification.UN6.Sample = cfg.Sample; tuning.Unification.UN6.TemperatureSpread = cfg.TemperatureSpread;
            // GodSim needs 2-6 factions: the second faction's founders are removed before tick 0 (killed, so every ledger stays
            // consistent; their bodies and packs lie at the other start, far away). The episode's tribe is the first faction.
            var setup = new GameSetup((ulong)seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = cfg.Persons, Victory = VictorySpec.SandboxOnly() };
            Sim = GodSim.Create(setup, tuning);
            var s = Sim.State;
            Faction first = s.Setup.Factions[0];
            foreach (var n in s.Population.Npcs.ToArray())
            {
                if (!Bodies.IsLivingPerson(n) || s.Unification.PersonOf(n.Id) == null) continue;
                if (n.Faction == first) _ids.Add(n.Id); else PopulationModule.Kill(s, new EventBuffer(), n, DeathCause.None);
            }
            _ids.Sort();
            if (_ids.Count > 0) StartNode = s.Population.Find(_ids[0]).Node;
            if (layout != Layout.Train) Relocate(layout);
            foreach (int id in _ids)
            {
                var n = s.Population.Find(id);
                _adult[id] = n.Stage == LifeStage.Adult;
                _visited[id] = new HashSet<int> { n.Node };
            }
        }

        // ---------------- IWorld / ISiteWorld ----------------

        public string Kind => "unified";
        public long Tick => Sim.State.Tick;
        public int TicksPerDay => Sim.State.Setup.TicksPerDay;
        public int NeedMask => Config.NeedMask;
        public int PainBand => Config.PainBand;
        public IReadOnlyList<int> People => _ids;
        public WorldFacts Facts => _facts;
        public GodState State => Sim.State;
        public BrainRuntime Runtime => Sim.State.Unification.BrainRuntime;

        public bool Alive(int person) { var n = Sim.State.Population.Find(person); return n != null && n.Alive; }
        public bool AdultAtStart(int person) => _adult.TryGetValue(person, out bool a) && a;
        public int IndexOf(int person) => _ids.BinarySearch(person);

        public void Bind(IActor actor, Sequence[] record, bool evaluation)
        {
            _evaluation = evaluation;
            var rt = Runtime;
            IPrimitivePolicy pol;
            if (actor is DaggerActor dg) dg.SetRecord(record);
            if (actor is ISitePolicySource src) pol = src.MakePolicy();
            else pol = new ActorPolicy(this, actor, record);
            if (pol == null) rt.Inject(null, ControlOrigin.Policy); // the game's own brain
            else rt.Inject(pol, actor.Origin);
            _decisions0 = rt.Decisions; _submitted0 = rt.Submitted; _accepted0 = rt.Accepted;
        }

        public long Decisions => Runtime.Decisions - _decisions0;
        public long TeacherSubmissions => TeacherEvents;
        public long[] AppliedBy => Applied;
        public string CauseOf(int person) => _cause.TryGetValue(person, out var c) ? c : "";

        /// <summary>Decisions are taken at the game's site during <see cref="Step"/>; the trainer's loop never decides here.</summary>
        public bool ReadyToDecide(int person) => false;

        public void Observe(int person, ref Percept p, short[] obs, ref ActMask mask)
        {
            var s = Sim.State;
            Percept.Encode(s, person, ref _p);
            Percept.ToVector(in _p, obs);
            mask = default;
            Motor.Legal(s, person, in _p, ref mask);
            p = _p;
        }

        /// <summary>The percept of the deciding person (the teacher reads its entity table).</summary>
        public ref Percept LastPercept => ref _p;
        internal void SetPercept(in Percept p) { _p = p; }

        public PrimitiveControl Decode(int person, in ActionChoice a)
        {
            if (_p.Actor != person) Percept.Encode(Sim.State, person, ref _p);
            return Percept.ControlOf(Sim.State, in _p, a.Control, a.Target, a.Source, a.Slot, a.Design, a.Gesture, a.R1, a.R2, a.Quantity);
        }

        public MotorStatus Submit(in PrimitiveControl c, ControlOrigin origin) => Motor.Submit(Sim.State, null, in c, origin);

        public void Step()
        {
            Sim.Step(1);
            var s = Sim.State;
            _events.Clear();
            Sim.DrainInto(_events);
            foreach (var e in _events)
            {
                switch (e)
                {
                    case ControlApplied a:
                        Applied[Clamp((int)a.Control)]++;
                        if (a.Origin == ControlOrigin.Teacher) Teacher();
                        break;
                    case ControlFailed f:
                        Failed[Clamp((int)f.Control)]++;
                        string key = f.Control + ":" + f.Status; Refused[key] = Refused.TryGetValue(key, out long r) ? r + 1 : 1;
                        if (f.Origin == ControlOrigin.Teacher) Teacher();
                        break;
                    case WaterUsed _: Drinks++; break;
                    case ForageCollected _: Forages++; break;
                    case Slept sl: if (!sl.Woke) Sleeps++; break;
                }
            }
            var rt = Runtime;
            _facts.Submissions = (int)(rt.Submitted - _submitted0); _facts.Accepted = (int)(rt.Accepted - _accepted0);
            foreach (int id in _ids)
            {
                var n = s.Population.Find(id);
                if (n == null) continue;
                if (n.Alive)
                {
                    _visited[id].Add(n.Node);
                    _last[id] = (Math.Min(1000, n.DriveOf(Drive.Hunger)), Unified.NeedActive(s, NeedChannel.Thirst) ? Needs.Pressure(s, id, NeedChannel.Thirst) : 0, n.Nutrition);
                }
                else if (!_cause.ContainsKey(id)) _cause[id] = Cause(id, n);
            }
        }

        private void Teacher()
        {
            TeacherEvents++;
            if (_evaluation && Violation == null) Violation = "a ControlOrigin.Teacher submission in an evaluation rollout";
        }

        /// <summary>
        /// The physical cause: the legacy body records any health loss as Starvation, so the cause is read from the last living tick:
        /// thirst at UN1's damage level, nutrition low (starvation), both, else the legacy cause.
        /// </summary>
        private string Cause(int id, Npc n)
        {
            var t = Sim.State.Tuning.Unification.UN1;
            if (_last.TryGetValue(id, out var l))
            {
                bool thirst = l.thirst >= t.ThirstDamageAt, starve = l.nutrition < Sim.State.Tuning.Population.NutritionLowFrom * 1000;
                if (thirst && starve) return "thirst+starvation";
                if (thirst) return "thirst";
                if (starve) return "starvation";
            }
            return n.DeathCause.ToString().ToLowerInvariant();
        }

        private static int Clamp(int k) => k < 0 || k > 12 ? 0 : k;

        public void ReadBody(int person, BodyReading into) => ReadBody(Sim.State, person, into);

        public BrainState BrainOf(int person) => Sim.State.Unification.PersonOf(person).Brain;

        public int CellsVisited(int person) => _visited.TryGetValue(person, out var v) ? v.Count : 0;

        public ulong StateHash() => Sim.StateHash();

        // ---------------- held-out start positions ----------------

        private void Relocate(Layout layout)
        {
            var s = Sim.State;
            var g = s.Territory.Grid;
            if (StartNode < 0) return;
            double x0 = g.X(StartNode), z0 = Lattice.Z(g.I(StartNode), g.J(StartNode));
            int target = -1;
            if (layout == Layout.Mirrored || layout == Layout.Rotated)
            {
                double tx, tz;
                if (layout == Layout.Mirrored) { tx = -x0; tz = z0; }
                else { double c = Math.Cos(2 * Math.PI / 3), sn = Math.Sin(2 * Math.PI / 3); tx = c * x0 - sn * z0; tz = sn * x0 + c * z0; }
                double best = double.MaxValue;
                for (int v = 0; v < g.Count; v++)
                {
                    if (!g.IsLand(v) || !Motor.Passable(s, v)) continue;
                    double dx = g.X(v) - tx, dz = Lattice.Z(g.I(v), g.J(v)) - tz, d = dx * dx + dz * dz;
                    if (d < best) { best = d; target = v; }
                }
            }
            else if (layout == Layout.UnfamiliarStart)
            {
                // the passable land nodes in the farthest quarter by lattice distance from the generator's start, one picked by the seed
                var cand = new List<(int d, int v)>();
                for (int v = 0; v < g.Count; v++) if (g.IsLand(v) && Motor.Passable(s, v)) cand.Add((g.Distance(StartNode, v), v));
                cand.Sort((a, b) => a.d != b.d ? b.d.CompareTo(a.d) : a.v.CompareTo(b.v));
                int top = Math.Max(1, cand.Count / 4);
                if (cand.Count > 0) target = cand[(int)((ulong)Seed * 2654435761UL % (ulong)top)].v;
            }
            if (target < 0) return; // regenerated: the held-out board alone
            foreach (int id in _ids)
            {
                var n = s.Population.Find(id);
                n.Node = target; n.Path = null; n.PathIdx = 0;
            }
            s.Population.IndexDirty = true;
            s.Unification.Index.MarkDirty();
            StartNode = target;
        }

        // ---------------- static helpers (kept from UN6t-a) ----------------

        /// <summary>A unified game shaped like UN1's <c>unif_world</c> scenario (six factions of three founders), the given need mask.</summary>
        public static GodSim Create(ulong seed, int needMask, int foundersPerFaction = 3)
        {
            SeedPolicy.Check((long)seed, Layout.Train, null);
            var tuning = new SimTuning();
            tuning.Unification.Enabled = 1;
            tuning.Unification.Needs.ActiveMask = needMask;
            var setup = new GameSetup(seed, GameSetup.NoHuman, 6) { Population = true, StartNpcsPerFaction = foundersPerFaction };
            return GodSim.Create(setup, tuning);
        }

        /// <summary>
        /// The body reading of a real person (UN1 needs): the five drives (Hunger, Energy = Fatigue, Warmth = Cold, Safety = Fear, Pain;
        /// R86 mapping) as permille, the seven extension pressures, health permille, alive. Inactive channels read 0.
        /// </summary>
        public static void ReadBody(GodState s, int npc, BodyReading into)
        {
            var n = s.Population.Find(npc);
            Array.Clear(into.Needs, 0, into.Needs.Length);
            if (n == null) { into.Alive = false; into.Health = 0; return; }
            into.Alive = n.Alive; into.Health = n.HealthPermille;
            for (int c = 0; c < NeedChannels.FirstExtension; c++) if (Unified.NeedActive(s, (NeedChannel)c)) into.Needs[c] = (short)Math.Min(1000, n.DriveOf(Drives[c]));
            for (int c = NeedChannels.FirstExtension; c < NeedChannels.Count; c++) if (Unified.NeedActive(s, (NeedChannel)c)) into.Needs[c] = (short)Needs.Pressure(s, npc, (NeedChannel)c);
        }

        private static readonly int[] Drives = { Drive.Hunger, Drive.Fatigue, Drive.Cold, Drive.Fear, Drive.Pain };

        /// <summary>
        /// Probes the merged simulation: builds a one-person unified world, observes, decodes and submits one WAIT. Null when every
        /// surface answers (the trainer may run on the real world), else the reason.
        /// </summary>
        public static string Unavailable(ulong seed)
        {
            SeedPolicy.Check((long)seed, Layout.Train, null);
            try
            {
                var w = new UnifiedWorld(new UnifiedConfig { Persons = 1, NeedMask = 0x033 }, (long)seed, Layout.Train, null);
                if (w.Sim.State.Unification == null) return "GodSim.Create did not create the unified state";
                if (w.People.Count == 0) return "the unified start has no persons";
                var p = default(Percept); var m = default(ActMask); var obs = new short[ObsSchema.Length];
                w.Observe(w.People[0], ref p, obs, ref m);
                if (m.Control == 0) return "Motor.Legal allows no control for an idle person";
                var c = w.Decode(w.People[0], ActionChoice.Of(Prim.Wait));
                if (w.Submit(in c, ControlOrigin.Policy) != MotorStatus.Accepted) return "Motor.Submit refused a WAIT";
                w.Step();
                return null;
            }
            catch (NotImplementedException e) { return "not merged: " + e.Message; }
            catch (Exception e) { return "unified game failed to start: " + e.GetType().Name + ": " + e.Message; }
        }
    }
}
