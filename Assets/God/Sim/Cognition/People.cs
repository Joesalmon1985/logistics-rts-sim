using System;
using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Cognition
{
    /// <summary>The relationship class of a person (the <c>NpcRel</c> feature value, SHARED_CONTRACTS 4.4). Frozen by F0.</summary>
    public enum NpcRelClass : byte
    {
        Unknown = 0, Stranger = 1, Acquaintance = 2, Friend = 3, Kin = 4, Partner = 5, HouseholdMate = 6, Enemy = 7, Rival = 8,
        Debtor = 9, Creditor = 10, Employer = 11, Employee = 12, Cheat = 13,
    }

    /// <summary>
    /// One person an NPC sees now (layer 2; SHARED_CONTRACTS F0.7). Field layout frozen by F0. The sim fills the visible
    /// facts; M4 fills <see cref="Stage"/>-dependent displays and <see cref="SameHousehold"/> through its hook, M5 recognition
    /// (<see cref="As"/>) and visible gestures through its own. <see cref="Id"/> is an opaque handle for intents only;
    /// features and memory use <see cref="As"/> (13.12).
    /// </summary>
    public struct SeenPerson
    {
        /// <summary>The true NpcId: an opaque handle a policy may name in an intent, never a feature or a memory key.</summary>
        public int Id;
        /// <summary>Who the observer takes it to be (recognition, M5; perfect = <see cref="Id"/> by default; 0 = a stranger it cannot place).</summary>
        public int As;
        public Faction Faction;
        public bool SameFaction;
        public int Node;
        public int Dist;
        public LifeStage Stage;
        /// <summary>The observer knows it shares its household (M4; false with no households).</summary>
        public bool SameHousehold;
        /// <summary>The learning verb of what it is visibly doing (0 idle).</summary>
        public byte Doing;
        public bool Asleep;
        /// <summary>A6: what it visibly carries, by class (<see cref="CarryMask"/> bits), and whether it holds (has equipped) a tool.</summary>
        public byte CarryMask;
        public bool HoldsTool;
        /// <summary>A visible display (M4: begging, courtship, care request): its kind, intensity 0-3 and the person it is aimed at.</summary>
        public byte Display, DisplayLevel;
        public int DisplayTarget;
        /// <summary>A gesture visibly being made now (M5: the <c>GestureAct</c>, 0 none). Seen, not yet decoded.</summary>
        public byte Gesture;
    }
}

namespace God.Sim.Population
{
    /// <summary>Bits of <see cref="SeenPerson.CarryMask"/> (A6): the classes of goods a person visibly carries.</summary>
    public static class CarryMask
    {
        public const byte Food = 1, Raw = 2, Tool = 4, Other = 8;

        public static byte Of(Ware w) => Wares.IsFood(w) ? Food : w == Ware.Axe || Wares.IsGear(w) ? Tool : Wares.IsRaw(w) ? Raw : Other;
    }

    /// <summary>
    /// The per-tick snapshot of living people and their places (F0.7), taken once at the start of the population tick so
    /// perception never depends on who moved first within the tick. A cache rebuilt from state, never saved. Entries are in
    /// ascending NPC id; <see cref="CellOf"/> lists the entries of each spatial cell in that order.
    /// </summary>
    public sealed class PeopleIndex
    {
        public struct Entry
        {
            public int Id;
            public int Node;
            public Faction Faction;
            public LifeStage Stage;
            public int Household;
            public byte Doing;
            public bool Asleep, HoldsTool;
            public byte CarryMask;
        }

        public Entry[] Entries = new Entry[64];
        public int Count;
        public long Tick = -1;
        private List<int>[] _cells = Array.Empty<List<int>>();

        public List<int> CellOf(int cell) => _cells[cell];

        /// <summary>Snapshots the living NPCs of <paramref name="p"/> into cells (<paramref name="cellOfNode"/> maps a node to its cell).</summary>
        public void Build(PopulationState p, long tick, int[] cellOfNode, int cells)
        {
            if (_cells.Length != cells) { _cells = new List<int>[cells]; for (int i = 0; i < cells; i++) _cells[i] = new List<int>(); }
            else foreach (var c in _cells) c.Clear();
            Count = 0;
            Tick = tick;
            foreach (var n in p.Npcs)
            {
                if (!n.Alive) continue;
                if (Count == Entries.Length) Array.Resize(ref Entries, Entries.Length * 2);
                byte mask = 0; bool tool = false;
                foreach (var l in n.Carry) { if (l.Empty) continue; mask |= Population.CarryMask.Of(l.Ware); if (Goods.IsEquipped(l)) tool = true; }
                Entries[Count] = new Entry
                {
                    Id = n.Id, Node = n.Node, Faction = n.Faction, Stage = n.Stage, Household = n.Household, Doing = n.Action.Verb,
                    Asleep = n.Action.Asleep, CarryMask = mask, HoldsTool = tool,
                };
                _cells[cellOfNode[n.Node]].Add(Count);
                Count++;
            }
        }
    }
}
