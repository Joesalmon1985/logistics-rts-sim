using System.Collections.Generic;

namespace God.Sim.Unified
{
    /// <summary>One hex's raw-material books (design 4.2, Geo-1/Geo-2): the dice allowance waiting, wasted, materialised and collected. Rows ascend by hex.</summary>
    public sealed class HexSupply
    {
        public int Hex;
        public byte Type;
        public int Pending, Wasted;
        public long IssuedEver, MaterialisedEver, CollectedEver;
        /// <summary>The hex's raw nodes: ids <see cref="NodeFirst"/>..<see cref="NodeFirst"/> + <see cref="NodeCount"/> - 1 in <see cref="UnifiedState.RawNodes"/> (contiguous, generated once).</summary>
        public int NodeFirst, NodeCount;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Hex); h.Add(Type); h.Add(Pending); h.Add(Wasted); h.Add(IssuedEver); h.Add(MaterialisedEver); h.Add(CollectedEver); h.Add(NodeFirst); h.Add(NodeCount);
        }
    }

    /// <summary>UN1's saved state (contracts 23.8, 23.14): hex types, supply books, the unified dice clock; hashed in the <c>AddToUN1</c> slot. Public fields, ascending-id lists, no dictionaries.</summary>
    public sealed partial class UnifiedState
    {
        /// <summary>The <see cref="HexType"/> of every board hex (index = hex id), filled at the world start; saved, never regenerated on load.</summary>
        public HexType[] HexKinds = new HexType[0];
        /// <summary>One row per hex that has raw nodes, ascending hex id.</summary>
        public List<HexSupply> Supply = new List<HexSupply>();
        /// <summary>The unified dice clock (replaces <c>DiceSystem</c>): rolls so far and the last roll (2d6 from <c>Rng.Fork("dice", RollCount)</c>).</summary>
        public int RollCount, LastDie1, LastDie2, LastTotal;
        public long LastRollTick;
        /// <summary>Foods foraged so far, by forage index (<see cref="HexTypes.ForageIndex"/>).</summary>
        public long[] ForagedEver = new long[8];

        partial void AddToUN1(ref StateHasher h)
        {
            h.Add(HexKinds.Length); foreach (var k in HexKinds) h.Add((int)k);
            h.Add(Supply.Count); foreach (var x in Supply) x.AddTo(ref h);
            h.Add(RollCount); h.Add(LastDie1); h.Add(LastDie2); h.Add(LastTotal); h.Add(LastRollTick);
            foreach (long f in ForagedEver) h.Add(f);
        }
    }

    public sealed partial class UnifiedNpc
    {
        /// <summary>The fractional remainder of each extension need's rise (UN1), so the daily rates stay exact in whole pressure points.</summary>
        public int[] NeedFrac = new int[NeedChannels.Extensions + 1];
        /// <summary>The person's rate multipliers (permille, 0 = not yet computed), refreshed at each day start (age and stage change slowly).</summary>
        public short[] NeedMul = new short[NeedChannels.Extensions];

        partial void AddToUN1(ref StateHasher h)
        {
            foreach (int f in NeedFrac) h.Add(f);
            foreach (short m in NeedMul) h.Add(m);
        }
    }
}
