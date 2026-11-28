using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN1 (design 4.6, spec 5.6): the environment books as invariants, called from <see cref="UnifiedInvariants.Check"/> through the UN1
    /// slot. <c>Geo-1</c> per hex <c>IssuedEver = Pending + Wasted + MaterialisedEver</c>; <c>Geo-2</c> <c>Σ MaterialisedEver = Σ Stock + Σ CollectedEver</c>;
    /// <c>Geo-3</c> stocks within 0..Cap and pending within 0..AllowanceCap; <c>Geo-4</c> no raw ware is ever produced as Foraged (UN3b's collection is the only raw producer; UN0's goods fixtures place raws by other reasons)
    /// (only UN3b's collection makes a raw lot); <c>Geo-5</c> (determinism) is a test. Also: no legacy source in a unified world, seaweed
    /// only on coast nodes.
    /// </summary>
    public static partial class UnifiedHooks
    {
        static partial void CheckUN1(GodState s, List<string> bad)
        {
            var u = s.Unification;
            if (!Unified.WorldOn(s)) return;
            var t = s.Tuning.Unification.UN1;
            long mat = 0, stock = 0, taken = 0;
            foreach (var h in u.Supply)
            {
                if (h.IssuedEver != h.Pending + h.Wasted + h.MaterialisedEver) bad.Add("Geo-1: hex " + h.Hex + " issued " + h.IssuedEver + " != pending " + h.Pending + " + wasted " + h.Wasted + " + materialised " + h.MaterialisedEver);
                if (h.Pending < 0 || h.Pending > t.AllowanceCap) bad.Add("Geo-3: hex " + h.Hex + " pending " + h.Pending + " outside 0.." + t.AllowanceCap);
                mat += h.MaterialisedEver; taken += h.CollectedEver;
            }
            foreach (var r in u.RawNodes)
            {
                stock += r.Stock;
                if (r.Stock < 0 || r.Stock > r.Cap) bad.Add("Geo-3: raw node " + r.Id + " stock " + r.Stock + " outside 0.." + r.Cap);
            }
            if (mat != stock + taken) bad.Add("Geo-2: materialised " + mat + " != stock " + stock + " + collected " + taken);
            var l = s.Population.Ledger;
            Ware[] raws = { Ware.Log, Ware.Clay, Ware.Stone, Ware.Fish, Ware.Hay, Ware.Sheep };
            foreach (var w in raws)
            {
                if (l.ProducedBy(ProduceReason.Foraged, w) != 0) bad.Add("Geo-4: raw ware " + w + " produced as Foraged");
            }
            if (s.Population.Sources.Count != 0) bad.Add("Unif-2: a legacy resource source exists in a unified world");
            var g = s.Territory.Grid;
            foreach (var f in u.ForagePlaces)
                if (f.Ware == (short)Ware.Seaweed && !UnifiedBoard.IsCoast(g, f.Node)) bad.Add("G1: seaweed place " + f.Id + " is not on a coast node");
            foreach (var r in u.RawNodes)
                if (r.Ware == (short)Ware.Fish && !UnifiedBoard.IsCoast(g, r.Node)) bad.Add("G1: fish node " + r.Id + " is not on a coast node");
        }
    }
}
