using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Population;
using God.Sim.Unified;
using NUnit.Framework;
using TargetKind = God.Sim.Unified.TargetKind;
using UBuilding = God.Sim.Unified.Building;

namespace God.Runner.Tests.Support
{
    /// <summary>One scripted control: submitted at the start of tick <see cref="Tick"/> (before that tick's systems run).</summary>
    public struct ScriptedControl
    {
        public long Tick;
        public PrimitiveControl Control;
        public ScriptedControl(long tick, PrimitiveControl c) { Tick = tick; Control = c; }
    }

    /// <summary>
    /// UN2 (spec 5.11): the primitive fixture harness every unified package's mechanical tests use. It builds a small unified world
    /// (a stub unified game: the island board, a few people with their founder packs removed, placed where the test wants, plus
    /// ground lots, bins and buildings made through the sim's own APIs), submits scripted <see cref="PrimitiveControl"/> streams
    /// (origin <see cref="ControlOrigin.Fixture"/> unless the test says otherwise), advances ticks, exposes the physical state and its
    /// hashes, records the stream it submitted, saves and loads, and replays a stream. Mechanics only: a fixture stream is scripted
    /// and is never presented as autonomous behaviour.
    /// <para>Use: <c>var f = PrimitiveFixture.Create(seed); var a = f.Person(0); f.Place(a, f.InteriorNode()); f.Submit(f.C(Prim.TurnLeft, a)); f.Step();</c></para>
    /// </summary>
    public sealed class PrimitiveFixture
    {
        public GodSim Sim { get; private set; }
        public GodState S => Sim.State;
        public UnifiedState U => S.Unification;
        /// <summary>Every event seen so far (submissions and ticks), in order.</summary>
        public readonly List<SimEvent> Events = new List<SimEvent>();
        /// <summary>Every control this fixture submitted (with the tick it was submitted at): a replayable stream.</summary>
        public readonly List<ScriptedControl> Recorded = new List<ScriptedControl>();
        /// <summary>The status of every submission, parallel to <see cref="Recorded"/>.</summary>
        public readonly List<MotorStatus> Results = new List<MotorStatus>();

        private readonly EventBuffer _ev = new EventBuffer();
        private InteractionBook _book;
        private EffectTable _effects;

        private PrimitiveFixture(GodSim sim) { Sim = sim; }

        /// <summary>
        /// A stub unified game: two factions of <paramref name="perFaction"/> people, every person's founder pack dropped on its node
        /// and (with <paramref name="clearGround"/>) consumed, so tests start with empty hands and an empty ground (the ledger books
        /// it, conservation holds). <paramref name="tune"/> may change the tuning (the switch is set to 1 after it).
        /// </summary>
        public static PrimitiveFixture Create(ulong seed = 1, int perFaction = 2, Action<SimTuning> tune = null, bool divinity = false, bool clearGround = true)
        {
            var t = new SimTuning();
            if (divinity) t.Divinity.Enabled = 1;
            tune?.Invoke(t);
            t.Unification.Enabled = 1;
            var setup = new GameSetup(seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = perFaction, Victory = VictorySpec.SandboxOnly() };
            var f = new PrimitiveFixture(GodSim.Create(setup, t));
            foreach (var n in f.People()) f.Free(n);
            if (clearGround) f.ClearGround();
            return f;
        }

        // ---------------- world building ----------------

        /// <summary>The living people, ascending id.</summary>
        public List<Npc> People() => S.Population.Npcs.Where(Bodies.IsLivingPerson).OrderBy(n => n.Id).ToList();
        public Npc Person(int i) => People()[i];
        public Npc Npc(int id) => S.Population.Find(id);
        public UnifiedNpc Rec(Npc n) => U.PersonOf(n.Id);
        public NodeGrid Grid => S.Territory.Grid;

        /// <summary>Drops everything a person carries on its node (an ordinary DROP move with the person as mover).</summary>
        public void Free(Npc n)
        {
            for (int i = 0; i < n.Carry.Length; i++)
                if (!n.Carry[i].Empty) Assert.IsTrue(Goods.Move(S, _ev, Holder.Carry(n.Id), n.Carry[i].Id, n.Carry[i].Count, Holder.Ground(n.Node), MoveReason.Drop, n.Id).Ok);
            Drain(); U.Index.Reset();
        }

        /// <summary>Consumes every ground lot (ledger booked as Wear), so a test starts with an empty ground.</summary>
        public void ClearGround()
        {
            while (S.Population.Lots.Count > 0)
            {
                var g = S.Population.Lots[0];
                Assert.IsTrue(Goods.Consume(S, _ev, Holder.Ground(g.Node), g.Lot.Id, g.Lot.Count, ConsumeReason.Wear, 0));
            }
            Drain(); U.Index.Reset();
        }

        /// <summary>Consumes every ground lot at one node.</summary>
        public void ClearGroundAt(int node)
        {
            for (int i = S.Population.Lots.Count - 1; i >= 0; i--)
            {
                var g = S.Population.Lots[i];
                if (g.Node == node) Assert.IsTrue(Goods.Consume(S, _ev, Holder.Ground(node), g.Lot.Id, g.Lot.Count, ConsumeReason.Wear, 0));
            }
            Drain(); U.Index.Reset();
        }

        /// <summary>Sets a body's node and facing directly (test setup, not a control).</summary>
        public void Place(Npc n, int node, byte facing = 0)
        {
            Assert.IsTrue(Grid.IsLand(node), "place on land");
            n.Node = node; n.Path = null; n.PathGoal = -1;
            Rec(n).Facing = facing;
            S.Population.IndexDirty = true;
            U.Index.Reset();
        }

        /// <summary>A land node all six of whose neighbours are land (the k-th such node by id, from the island's middle out).</summary>
        public int InteriorNode(int k = 0)
        {
            var g = Grid;
            var all = new List<int>();
            for (int v = 0; v < g.Count; v++)
            {
                if (!g.IsLand(v)) continue;
                bool ok = true;
                for (int d = 0; d < 6 && ok; d++) { int n = g.Neighbour(v, d); ok = n >= 0 && g.IsLand(n); }
                if (ok) all.Add(v);
            }
            Assert.Greater(all.Count, k, "interior nodes");
            return all[all.Count / 2 + k];
        }

        /// <summary>A land node with a sea (or off-board) neighbour in direction <paramref name="dir"/>.</summary>
        public int CoastNode(out int dir)
        {
            var g = Grid;
            for (int v = 0; v < g.Count; v++)
            {
                if (!g.IsLand(v)) continue;
                for (int d = 0; d < 6; d++)
                {
                    int n = g.Neighbour(v, d);
                    if (n >= 0 && !g.IsLand(n)) { dir = d; return v; }
                }
            }
            dir = -1; Assert.Fail("no coast"); return -1;
        }

        public int Ground(int node, Ware w, int count = 1)
        {
            int lot = Goods.Produce(S, _ev, Holder.Ground(node), w, count, 0, 0, Owner.None, ProduceReason.Placed, 0, 0);
            Assert.Greater(lot, 0); Drain(); U.Index.Reset();
            return lot;
        }

        public int InCarry(Npc n, Ware w, int count = 1)
        {
            int lot = Goods.Produce(S, _ev, Holder.Carry(n.Id), w, count, 0, 0, Owner.None, ProduceReason.Placed, 0, 0);
            Assert.Greater(lot, 0, "room in the carry"); Drain();
            return lot;
        }

        public int NewBin(BinKind kind, int node, int weightCap = 0) => Goods.NewBin(S, kind, Owner.None, node, weightCap);

        public int InBin(int bin, Ware w, int count = 1)
        {
            int lot = Goods.Produce(S, _ev, Holder.Bin(bin), w, count, 0, 0, Owner.None, ProduceReason.Placed, 0, 0);
            Assert.Greater(lot, 0); Drain();
            return lot;
        }

        /// <summary>A finished building record at a node (catalogue id <paramref name="cat"/>) with the bins asked for (UN3b's Buildings.Create later).</summary>
        public UBuilding NewBuilding(ushort cat, int node, bool storage = false, bool workOut = false, bool fixtures = false)
        {
            var b = new UBuilding { Id = U.NextBuildingId++, Cat = cat, Node = node, Condition = 1000, Completed = S.Tick };
            if (storage) b.Storage = NewBin(BinKind.Storage, node);
            if (workOut) b.WorkOut = NewBin(BinKind.WorkOutput, node);
            if (fixtures) b.Fixtures = NewBin(BinKind.Fixtures, node);
            U.Buildings.Add(b);
            U.Index.Reset();
            return b;
        }

        /// <summary>Code-defined interaction rows and effect handlers (run-time setting, re-applied after a load).</summary>
        public void UseTables(InteractionBook book, EffectTable effects)
        {
            _book = book; _effects = effects;
            U.UseTables(book, effects);
        }

        // ---------------- controls ----------------

        public PrimitiveControl C(Prim p, Npc actor, TargetRef target = default, int source = 0, byte slot = 0) =>
            new PrimitiveControl { Control = p, Actor = actor.Id, Target = target, SourceLot = source, Slot = slot };

        public static TargetRef T(TargetKind k, int id) => new TargetRef(k, id);

        public MotorStatus Submit(in PrimitiveControl c, ControlOrigin origin = ControlOrigin.Fixture)
        {
            _ev.CurrentTick = S.Tick;
            var st = Motor.Submit(S, _ev, in c, origin);
            Recorded.Add(new ScriptedControl(S.Tick, c));
            Results.Add(st);
            Drain();
            return st;
        }

        public void Step(int ticks = 1)
        {
            for (int i = 0; i < ticks; i++) { Sim.Step(); Sim.DrainInto(Events); }
        }

        /// <summary>Steps until the body is idle (at most <paramref name="max"/> ticks); returns the ticks stepped.</summary>
        public int UntilIdle(Npc n, int max = 1000)
        {
            int k = 0;
            while (Motor.Busy(S, n.Id) && k < max) { Step(); k++; }
            return k;
        }

        /// <summary>
        /// The scripted-displacement hook (UN6t T5): interrupts what <paramref name="npc"/> is doing (<see cref="InterruptReason.Divine"/>;
        /// a sleeper is woken) and walks it away from where it stood, one primitive at a time through <see cref="Motor.Submit"/> with
        /// origin <see cref="ControlOrigin.Fixture"/> (turn towards the passable neighbour farthest from the start, ties by the lower
        /// direction, then step), stepping <paramref name="sim"/> until the body is at least <paramref name="cells"/> lattice steps from
        /// its start or <paramref name="maxTicks"/> ran out. Works on any unified <c>GodSim</c> (a fixture's or a trainer's). Returns
        /// the ticks stepped; <paramref name="submitted"/> receives every control sent (a replayable stream). Scripted: never an
        /// evaluation population's behaviour.
        /// </summary>
        public static int Displace(GodSim sim, int npc, int cells, int maxTicks = 600, List<ScriptedControl> submitted = null, EventBuffer ev = null, List<MotorStatus> statuses = null)
        {
            var s = sim.State; var g = s.Territory.Grid;
            var body = s.Population.Find(npc);
            Assert.IsNotNull(body, "no such body");
            int start = body.Node;
            ev = ev ?? new EventBuffer();
            ev.CurrentTick = s.Tick;
            Motor.Interrupt(s, ev, npc, InterruptReason.Divine);
            int t = 0;
            for (; t < maxTicks && body.Alive && g.Distance(start, body.Node) < cells; t++)
            {
                if (!Motor.Busy(s, npc) && !body.Action.Asleep)
                {
                    int best = -1, bestD = -1;
                    for (int d = 0; d < 6; d++)
                    {
                        int x = g.Neighbour(body.Node, d);
                        if (x >= 0 && Motor.Passable(s, x) && g.Distance(start, x) > bestD) { bestD = g.Distance(start, x); best = d; }
                    }
                    if (best < 0) break; // boxed in
                    int face = s.Unification.PersonOf(npc).Facing;
                    var c = new PrimitiveControl { Actor = npc };
                    c.Control = best == face ? Prim.StepForward : ((best - face + 6) % 6 <= 3 ? Prim.TurnRight : Prim.TurnLeft);
                    ev.CurrentTick = s.Tick;
                    var st = Motor.Submit(s, ev, in c, ControlOrigin.Fixture);
                    submitted?.Add(new ScriptedControl(s.Tick, c)); statuses?.Add(st);
                }
                sim.Step();
            }
            return t;
        }

        /// <summary><see cref="Displace(GodSim,int,int,int,List{ScriptedControl},EventBuffer,List{MotorStatus})"/> on this fixture (recorded and its events kept).</summary>
        public int Displace(Npc n, int cells, int maxTicks = 600)
        {
            var sent = new List<ScriptedControl>(); var sts = new List<MotorStatus>();
            int t = Displace(Sim, n.Id, cells, maxTicks, sent, _ev, sts);
            Recorded.AddRange(sent); Results.AddRange(sts);
            Drain(); Sim.DrainInto(Events);
            return t;
        }

        /// <summary>Plays a stream: each control is submitted at its tick (relative to <paramref name="from"/>), then ticks run up to <paramref name="until"/>.</summary>
        public void Play(IReadOnlyList<ScriptedControl> stream, ControlOrigin origin, long until, long from = 0, Action<PrimitiveFixture> atTick = null)
        {
            stream = stream.Select((x, k) => (x, k)).OrderBy(p => p.x.Tick).ThenBy(p => p.k).Select(p => p.x).ToList(); // by tick, stable
            int i = 0;
            while (i < stream.Count && stream[i].Tick + from < S.Tick) i++;
            while (S.Tick < until)
            {
                while (i < stream.Count && stream[i].Tick + from == S.Tick) { Submit(stream[i].Control, origin); i++; }
                atTick?.Invoke(this);
                Step();
            }
        }

        private void Drain() { _ev.DrainInto(Events); }

        public IEnumerable<T> Of<T>() where T : SimEvent => Events.OfType<T>();

        // ---------------- hashes, invariants, save ----------------

        public ulong Hash() => Sim.StateHash();

        public ulong Part(string name)
        {
            var names = GodSim.PartNamesOf(S);
            int i = Array.IndexOf(names, name);
            Assert.GreaterOrEqual(i, 0, name);
            return Sim.StateHashParts()[i];
        }

        /// <summary>The state hash with every body's <c>LastResult</c> neutralised (a refusal records its status and nothing else).</summary>
        public ulong HashIgnoringLastResult()
        {
            var keep = U.People.Select(p => p.LastResult).ToArray();
            foreach (var p in U.People) p.LastResult = MotorStatus.Accepted;
            ulong h = Hash();
            for (int i = 0; i < keep.Length; i++) U.People[i].LastResult = keep[i];
            return h;
        }

        public List<string> Invariants()
        {
            var bad = new List<string>();
            UnifiedInvariants.Check(S, bad);
            return bad;
        }

        public string Save() => SaveFile.From(Sim).ToJson();

        /// <summary>A fixture over a loaded copy (the same run-time tables re-applied).</summary>
        public PrimitiveFixture Load(string json)
        {
            var f = new PrimitiveFixture(SaveFile.FromJson(json).ToSim());
            if (_book != null || _effects != null) f.UseTables(_book, _effects);
            return f;
        }

        // ---------------- assertion helpers ----------------

        /// <summary>
        /// U-P4 / QA-01: builds the same world three times with <paramref name="build"/>, plays <paramref name="stream"/> with origins
        /// Policy, Possessed and Fixture to <paramref name="until"/>, and asserts the unification and population part hashes (and the
        /// whole hash) are equal. Returns the three fixtures.
        /// </summary>
        public static PrimitiveFixture[] AssertOriginNeutral(Func<PrimitiveFixture> build, IReadOnlyList<ScriptedControl> stream, long until)
        {
            var origins = new[] { ControlOrigin.Policy, ControlOrigin.Possessed, ControlOrigin.Fixture };
            var fs = new PrimitiveFixture[3];
            for (int k = 0; k < 3; k++) { fs[k] = build(); fs[k].Play(stream, origins[k], until); }
            for (int k = 1; k < 3; k++)
            {
                Assert.AreEqual(fs[0].Part("unification"), fs[k].Part("unification"), "unification part, origin " + origins[k]);
                Assert.AreEqual(fs[0].Part("population"), fs[k].Part("population"), "population part, origin " + origins[k]);
                Assert.AreEqual(fs[0].Hash(), fs[k].Hash(), "whole hash, origin " + origins[k]);
                CollectionAssert.AreEqual(fs[0].Results, fs[k].Results, "statuses, origin " + origins[k]);
            }
            return fs;
        }

        /// <summary>
        /// U-P6 stub: plays <paramref name="stream"/> straight to <paramref name="until"/> recording hashes every <paramref name="every"/>
        /// ticks; again with a save and load at <paramref name="saveAt"/>; asserts equal hashes at every checkpoint after the load.
        /// </summary>
        public static void AssertSaveLoadParity(Func<PrimitiveFixture> build, IReadOnlyList<ScriptedControl> stream, long saveAt, long until, int every = 5,
            Action<PrimitiveFixture> atSave = null)
        {
            var straight = build();
            var hs = new Dictionary<long, ulong>();
            straight.Play(stream, ControlOrigin.Fixture, until, 0, f => { if (f.S.Tick % every == 0) hs[f.S.Tick] = f.Hash(); });
            var a = build();
            a.Play(stream, ControlOrigin.Fixture, saveAt);
            atSave?.Invoke(a);
            var b = a.Load(a.Save());
            Assert.AreEqual(a.Hash(), b.Hash(), "a load restores the hash");
            int n = 0;
            b.Play(stream, ControlOrigin.Fixture, until, 0, f => { if (f.S.Tick % every == 0) { Assert.AreEqual(hs[f.S.Tick], f.Hash(), "tick " + f.S.Tick); n++; } });
            Assert.AreEqual(straight.Hash(), b.Hash(), "final");
            Assert.Greater(n, 0, "checkpoints compared");
        }
    }
}
