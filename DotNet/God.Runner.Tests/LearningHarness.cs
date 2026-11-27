using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using God.Sim;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Runner.Tests
{
    /// <summary>One think of one NPC as a test sees it afterwards: what it chose, its drives then, and what it attended.</summary>
    public struct ThinkRow
    {
        public long Tick;
        public int Day;
        public byte Verb;
        public byte TargetClass;
        public int TargetNode;
        public int Hunger, Fatigue, Cold, Pain;
        public byte[] Attended;
        public bool Asleep;
        public int HutAt;
        public bool LearnedChangedWinner;
        public int Learned, Instinct, Explore;
        public bool CarryTool;

        public bool Attends(int cls) => Attended != null && Attended.Contains((byte)cls);
    }

    /// <summary>
    /// Steps a controlled scenario tick by tick and records every think of the watched NPCs (from their minds' last
    /// decision, read after the tick). Read-only: it never changes the sim.
    /// </summary>
    public sealed class Watch
    {
        public readonly GodSim Sim;
        public readonly Dictionary<int, List<ThinkRow>> Rows = new Dictionary<int, List<ThinkRow>>();
        private readonly int[] _ids;

        public Watch(GodSim sim, params int[] ids)
        {
            Sim = sim; _ids = ids;
            foreach (int id in ids) Rows[id] = new List<ThinkRow>();
        }

        public int TicksPerDay => Sim.State.Setup.TicksPerDay;

        public void RunDays(double days, Action<long> everyTick = null)
        {
            long until = Sim.State.Tick + (long)(days * TicksPerDay);
            while (Sim.State.Tick < until)
            {
                Sim.Step();
                Sim.DrainEvents();
                everyTick?.Invoke(Sim.State.Tick);
                foreach (int id in _ids)
                {
                    var npc = Sim.State.Population.Find(id);
                    if (npc == null || !npc.Alive || npc.LastThinkTick != Sim.State.Tick) continue;
                    var m = npc.Mind;
                    var row = new ThinkRow
                    {
                        Tick = Sim.State.Tick, Day = (int)(Sim.State.Tick / TicksPerDay), Verb = m.Last.Verb, TargetClass = m.Last.TargetClass, TargetNode = m.Last.TargetNode,
                        Hunger = npc.DriveOf(Drive.Hunger), Fatigue = npc.DriveOf(Drive.Fatigue), Cold = npc.DriveOf(Drive.Cold), Pain = npc.DriveOf(Drive.Pain),
                        Attended = m.Attended.Take(m.AttendedCount).Select(a => a.Class).ToArray(), Asleep = npc.Action.Asleep,
                        LearnedChangedWinner = m.Last.LearnedChangedWinner, Learned = m.Last.Learned, Instinct = m.Last.Instinct, Explore = m.Last.Explore,
                        CarryTool = PopulationModule.CountOf(npc, Ware.Axe) > 0,
                    };
                    Rows[id].Add(row);
                }
            }
        }

        public static T[] Seeds<T>(int from, int to, Func<ulong, T> run) =>
            Enumerable.Range(from, to - from + 1).AsParallel().AsOrdered().WithDegreeOfParallelism(Math.Max(2, Environment.ProcessorCount - 2)).Select(i => run((ulong)i)).ToArray();

        public static double Pct(int n, int d) => d == 0 ? double.NaN : 100.0 * n / d;
    }
}
