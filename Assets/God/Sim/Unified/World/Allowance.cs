using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN1 (spec 5.6, design 4): the unified dice clock and the gradual materialisation. <c>DiceSystem</c> is not installed in a unified
    /// game, so the clock is here: every <c>TerritoryTuning.DiceIntervalTicks</c> it rolls 2d6 from the same keyed stream
    /// <c>Rng.Fork("dice", RollCount)</c> as Stage 3 (so the totals equal Stage 3's for the same seed). A matching hex gets its allowance
    /// (<c>Pending</c>, capped, the rest wasted); every <c>MaterialiseIntervalTicks</c> each hex moves <c>MaterialiseUnits</c> to its
    /// emptiest raw node. Raw nodes are not lots; only <see cref="World.TakeRaw"/> lowers a stock.
    /// </summary>
    public static partial class UnifiedHooks
    {
        static partial void TickWorld(GodState s, EventBuffer ev)
        {
            if (!Unified.WorldOn(s)) return;
            var u = s.Unification; var t = s.Tuning.Unification.UN1;
            if (s.Tick % TerritoryTuning.DiceIntervalTicks == 0) Roll(s, ev, u, t);
            if (t.MaterialiseIntervalTicks > 0 && s.Tick % t.MaterialiseIntervalTicks == 0) Materialise(s, ev, u, t);
            var cal = SimCalendar.Of(s);
            if (cal.IsDayEnd(s.Tick)) DayRow(s, ev, u, cal);
        }

        static void Roll(GodState s, EventBuffer ev, UnifiedState u, UN1Tuning t)
        {
            // the same stream as the legacy DiceSystem: keyed by the roll number, never by how many draws came before
            var r = s.Rng.Fork("dice", u.RollCount);
            int d1 = r.NextRange(1, 7), d2 = r.NextRange(1, 7), total = d1 + d2;
            u.LastRollTick = s.Tick; u.LastDie1 = d1; u.LastDie2 = d2; u.LastTotal = total;
            int roll = u.RollCount++;
            if (total == 7) return;
            var b = s.Board;
            foreach (var sup in u.Supply)
            {
                if (b.HexNumber[sup.Hex] != total) continue;
                int amount = sup.Type == (byte)HexType.Pastures ? t.SheepAllowancePerRoll : t.AllowancePerRoll;
                int room = t.AllowanceCap - sup.Pending; if (room < 0) room = 0;
                int grant = amount < room ? amount : room, waste = amount - grant;
                sup.IssuedEver += amount;
                if (grant > 0) { sup.Pending += grant; ev?.Add(new AllowanceGranted { Hex = sup.Hex, Roll = roll, Amount = grant, Pending = sup.Pending }); }
                if (waste > 0) { sup.Wasted += waste; ev?.Add(new AllowanceWasted { Hex = sup.Hex, Roll = roll, Amount = waste }); }
            }
        }

        static void Materialise(GodState s, EventBuffer ev, UnifiedState u, UN1Tuning t)
        {
            foreach (var sup in u.Supply)
            {
                if (sup.Pending <= 0) continue;
                RawNode best = null;
                for (int i = 0; i < sup.NodeCount; i++)
                {
                    var n = World.RawNodeById(u, sup.NodeFirst + i);
                    if (n == null || n.Stock >= n.Cap) continue;
                    if (best == null || n.Stock < best.Stock) best = n; // ties: the lowest id (ascending scan, strict less)
                }
                if (best == null) continue; // every node is full: the unit waits
                int units = t.MaterialiseUnits < sup.Pending ? t.MaterialiseUnits : sup.Pending;
                int space = best.Cap - best.Stock; if (units > space) units = space;
                if (units <= 0) continue;
                best.Stock += units; sup.Pending -= units; sup.MaterialisedEver += units;
                ev?.Add(new RawMaterialised { Hex = sup.Hex, Node = best.Node, Ware = (Ware)best.Ware, Units = units, Stock = best.Stock });
            }
        }

        static void DayRow(GodState s, EventBuffer ev, UnifiedState u, SimCalendar cal)
        {
            int alive = 0; foreach (var n in s.Population.Npcs) if (Bodies.IsLivingPerson(n)) alive++;
            int pending = 0, stock = 0;
            foreach (var x in u.Supply) pending += x.Pending;
            foreach (var x in u.RawNodes) stock += x.Stock;
            var f = new int[u.ForagedEver.Length];
            for (int i = 0; i < f.Length; i++) f[i] = (int)u.ForagedEver[i];
            ev?.Add(new UnifiedDay { Day = (int)cal.Day(s.Tick), Alive = alive, Pending = pending, Stock = stock, Foraged = f });
        }
    }
}
