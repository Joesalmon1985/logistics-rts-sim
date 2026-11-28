using System;
using System.Collections.Generic;
using God.Sim;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Unified;
using TargetKind = God.Sim.Unified.TargetKind;

namespace God.Runner.Tests.Support
{
    /// <summary>
    /// UN6 test fixture (tests only, never in the game assembly): an <see cref="IBrainBody"/> over a toy world so the brain runtime can
    /// be exercised before UN2's encoder and motor merge. MECHANICAL: perception is a fixed-radius scan with a toy distance
    /// |node a - node b|; every submission is accepted and holds the body for <see cref="ActionTicks"/> ticks (<see cref="FakeMotor"/>
    /// counts them down); USE effects are whatever the test installs in <see cref="OnUse"/>. Deterministic and allocation-free per call.
    /// </summary>
    public sealed class FixtureBody : IBrainBody
    {
        public struct Thing { public int Class, Node, Id; public TargetKind Kind; }

        public bool On = true;
        public int Radius = 6;
        public int ActionTicks = 4;
        /// <summary>Controls the mask allows (bit = <see cref="Prim"/>); default all twelve.</summary>
        public ulong Controls = 0x1FFEUL;
        public readonly List<Thing> Things = new List<Thing>();
        /// <summary>Called on an accepted USE (the test's physics), with the target.</summary>
        public Action<GodState, Npc, PrimitiveControl> OnUse;
        public long Submits;
        public readonly long[] ByControl = new long[13];
        public readonly List<(long tick, int actor, Prim c)> Log = null;

        // the last sensed entities
        private readonly TargetKind[] _kind = new TargetKind[ObsSchema.Entities];
        private readonly int[] _id = new int[ObsSchema.Entities], _node = new int[ObsSchema.Entities], _dist = new int[ObsSchema.Entities], _class = new int[ObsSchema.Entities];
        private int _count;

        public bool Available(GodState s) => On;

        public static int PressureOf(GodState s, Npc npc, UnifiedNpc u, int c)
        {
            switch (c)
            {
                case 0: return npc.Drives[Drive.Hunger] / 1000;
                case 1: return npc.Drives[Drive.Fatigue] / 1000;
                case 2: return npc.Drives[Drive.Cold] / 1000;
                case 3: return npc.Drives[Drive.Fear] / 1000;
                case 4: return npc.Drives[Drive.Pain] / 1000;
                default: return u.Needs[c - NeedChannels.FirstExtension];
            }
        }

        public void Sense(GodState s, Npc npc, ref Percept p, short[] obs, ref ActMask m)
        {
            Array.Clear(obs, 0, obs.Length);
            var u = s.Unification.PersonOf(npc.Id);
            p.Actor = npc.Id; p.Tick = s.Tick;
            obs[ObsSchema.OffsetOf("facing") + (u.Facing % 6)] = 1000;
            obs[ObsSchema.OffsetOf("last_result")] = (short)u.LastResult;
            int needs = ObsSchema.OffsetOf("needs");
            for (int c = 0; c < NeedChannels.Count; c++) obs[needs + c] = (short)PressureOf(s, npc, u, c);
            // entities: persons and things within the radius, nearest first, then by id
            _count = 0;
            foreach (var o in s.Population.Npcs)
                if (o.Id != npc.Id && Bodies.IsLivingPerson(o)) Offer(TargetKind.Npc, o.Id, o.Node, 1, Dist(npc.Node, o.Node));
            foreach (var t in Things) Offer(t.Kind, t.Id, t.Node, t.Class, Dist(npc.Node, t.Node));
            int ent = ObsSchema.OffsetOf("entities");
            for (int e = 0; e < _count; e++)
            {
                int o = ent + e * ObsSchema.EntityFields;
                obs[o] = (short)_class[e]; obs[o + 1] = 0; obs[o + 2] = (short)_dist[e]; obs[o + 3] = (short)(_dist[e] <= 1 ? FallbackPolicy.InReachBit : 0);
            }
            m.Control = Controls;
            m.Target = (1UL << BrainHeads.TargetSelf) | (1UL << BrainHeads.TargetFacing);
            for (int e = 0; e < _count; e++) m.Target |= 1UL << e;
            m.Source = 1UL << BrainHeads.SourceEmpty;
            m.Slot = 0xF;
            m.Design = 1UL << BrainHeads.DesignNone;
            m.Gesture = 0xFFFFFFFFUL;
            m.Referent1 = m.Referent2 = (1UL << 22) - 1;
            m.Quantity = 0xFFFF;
        }

        private static int Dist(int a, int b) => a > b ? a - b : b - a;

        private void Offer(TargetKind kind, int id, int node, int cls, int d)
        {
            if (d > Radius) return;
            int at = _count;
            while (at > 0 && (_dist[at - 1] > d || (_dist[at - 1] == d && _id[at - 1] > id))) at--;
            if (at >= ObsSchema.Entities) return;
            int last = _count < ObsSchema.Entities ? _count : ObsSchema.Entities - 1;
            for (int i = last; i > at; i--) { _kind[i] = _kind[i - 1]; _id[i] = _id[i - 1]; _node[i] = _node[i - 1]; _dist[i] = _dist[i - 1]; _class[i] = _class[i - 1]; }
            _kind[at] = kind; _id[at] = id; _node[at] = node; _dist[at] = d; _class[at] = cls;
            if (_count < ObsSchema.Entities) _count++;
        }

        public PrimitiveControl Decode(GodState s, in Percept p, in BrainChoice c)
        {
            var pc = new PrimitiveControl { Control = (Prim)c.Control, Actor = p.Actor };
            if (c.Target >= 0 && c.Target < _count) pc.Target = new TargetRef(_kind[c.Target], _id[c.Target]);
            else if (c.Target == BrainHeads.TargetSelf) pc.Target = TargetRef.Self;
            else if (c.Target == BrainHeads.TargetFacing) pc.Target = new TargetRef(TargetKind.Facing, 0);
            if (c.Slot > 0) pc.Slot = (byte)c.Slot;
            if (c.Gesture >= 0) pc.Gesture = (byte)c.Gesture;
            if (c.Quantity >= 0) pc.Quantity = (short)c.Quantity;
            return pc;
        }

        public MotorStatus Submit(GodState s, EventBuffer ev, in PrimitiveControl c, ControlOrigin origin)
        {
            var u = s.Unification.PersonOf(c.Actor);
            if (u == null || c.Control == Prim.None) return MotorStatus.Invalid;
            u.ActionKind = c.Control;
            u.ActionTicksLeft = ActionTicks;
            u.LastResult = MotorStatus.Accepted;
            Submits++; ByControl[(int)c.Control]++;
            if (c.Control == Prim.Use && OnUse != null) OnUse(s, s.Population.Find(c.Actor), c);
            return MotorStatus.Accepted;
        }

        public bool Entity(in Percept p, int slot, out TargetKind kind, out int id, out int node)
        {
            if (slot < 0 || slot >= _count) { kind = TargetKind.None; id = 0; node = -1; return false; }
            kind = _kind[slot]; id = _id[slot]; node = _node[slot];
            return true;
        }

        public int PersonsNear(GodState s, int actor, int radius, int[] into, int[] distance)
        {
            var a = s.Population.Find(actor);
            if (a == null) return 0;
            int n = 0;
            foreach (var o in s.Population.Npcs)
            {
                if (o.Id == actor || !Bodies.IsLivingPerson(o)) continue;
                int d = Dist(a.Node, o.Node);
                if (d > radius) continue;
                int at = n;
                while (at > 0 && distance[at - 1] > d) at--;
                if (at >= into.Length) continue;
                int last = n < into.Length ? n : into.Length - 1;
                for (int i = last; i > at; i--) { into[i] = into[i - 1]; distance[i] = distance[i - 1]; }
                into[at] = o.Id; distance[at] = d;
                if (n < into.Length) n++;
            }
            return n;
        }
    }

    /// <summary>The fixture's motor: counts every unified body's action down one tick (order 247, before the person ticks).</summary>
    public sealed class FakeMotor : ISimSystem
    {
        public const int Order = 247;
        public void Tick(GodState s, EventBuffer ev)
        {
            foreach (var u in s.Unification.People) if (u.ActionTicksLeft > 0) u.ActionTicksLeft--;
        }
    }

    /// <summary>A fixture policy (tests only, <see cref="ControlOrigin.Fixture"/>): a scripted control per decision, cycling.</summary>
    public sealed class ScriptedPolicy : IPrimitivePolicy
    {
        public readonly PrimitiveControl[] Script;
        private int _i;
        public ScriptedPolicy(params PrimitiveControl[] script) { Script = script; }

        public PrimitiveControl Decide(in Percept p, short[] obs, ref BrainState b, in ActMask m, ref Rng rng)
        {
            var c = Script[_i++ % Script.Length];
            c.Actor = p.Actor;
            return c;
        }
    }

    /// <summary>Unified games for the UN6 tests.</summary>
    public static class UN6Games
    {
        public static GodSim Game(ulong seed, int npcsPerFaction = 3, Action<SimTuning> tune = null)
        {
            var t = new SimTuning();
            t.Unification.Enabled = 1;
            tune?.Invoke(t);
            var setup = new GameSetup(seed, GameSetup.NoHuman, 2) { Population = true, StartNpcsPerFaction = npcsPerFaction, Victory = VictorySpec.SandboxOnly() };
            return GodSim.Create(setup, t);
        }

        /// <summary>Attaches a fixture body and the fake motor (again after a load: neither is saved).</summary>
        public static FixtureBody Attach(GodSim sim, FixtureBody body = null)
        {
            body = body ?? new FixtureBody();
            sim.State.Unification.BrainRuntime.Body = body;
            sim.AddSystem(FakeMotor.Order, new FakeMotor());
            return body;
        }

        public static List<Npc> People(GodState s)
        {
            var l = new List<Npc>();
            foreach (var n in s.Population.Npcs) if (Bodies.IsLivingPerson(n)) l.Add(n);
            return l;
        }
    }
}
