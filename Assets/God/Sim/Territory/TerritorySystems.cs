using System.Collections.Generic;

namespace God.Sim
{
    // ---------------- events ----------------

    /// <summary>A roll of the hidden Catan dice. The view may show a tick or pulse, never the number.</summary>
    public sealed class DiceRolled : DomainEvent
    {
        public int Die1, Die2, Total;
    }

    /// <summary>A pile appeared at a hex centre.</summary>
    public sealed class PileAdded : DomainEvent
    {
        public int Hex;
        public RawKind Kind;
        /// <summary>Piles on the hex after this one.</summary>
        public int Count;
    }

    /// <summary>A pile was due but the hex already held the cap, so it was wasted.</summary>
    public sealed class PileWasted : DomainEvent
    {
        public int Hex;
        public RawKind Kind;
    }

    public sealed class HqPlaced : DomainEvent { public Faction Faction; public int HqId; public int Corner; }
    public sealed class BuildingPlaced : DomainEvent { public Faction Faction; public int BuildingId; public int Node; public BuildingSize Size; }
    public sealed class BuildingRemoved : DomainEvent { public Faction Faction; public int BuildingId; public int Node; }
    public sealed class FlagPlaced : DomainEvent { public Faction Faction; public int FlagId; public int Node; }
    public sealed class RoadBuilt : DomainEvent { public Faction Faction; public int RoadId; }
    public sealed class RoadRemoved : DomainEvent { public Faction Faction; public int RoadId; }

    // ---------------- dice clock ----------------

    /// <summary>
    /// Every <see cref="TerritoryTuning.DiceIntervalTicks"/> ticks rolls 2d6 from the keyed stream <c>Rng.Fork("dice", roll)</c>. Each hex whose hidden
    /// number equals the total gets one pile (cap <see cref="TerritoryTuning.PileCap"/>; extras are wasted). A 7 does
    /// nothing. The desert (number 0) never matches.
    /// </summary>
    public sealed class DiceSystem : ISimSystem
    {
        public void Tick(GodState s, EventBuffer ev)
        {
            if (s.Tick % TerritoryTuning.DiceIntervalTicks != 0) return;
            var t = s.Territory;
            // M0: each roll has its own stream, keyed by the roll number (never by how many draws came before).
            var r = s.Rng.Fork("dice", t.RollCount);
            int d1 = r.NextRange(1, 7), d2 = r.NextRange(1, 7), total = d1 + d2;
            t.LastRollTick = s.Tick; t.LastDie1 = d1; t.LastDie2 = d2; t.LastTotal = total; t.RollCount++;
            ev.Add(new DiceRolled { Die1 = d1, Die2 = d2, Total = total });
            if (total == 7) return;
            var b = s.Board;
            for (int h = 0; h < b.HexNumber.Length; h++)
            {
                if (b.HexNumber[h] != total) continue;
                var kind = RawKinds.OfTerrain(b.HexTerrain[h]);
                if (kind == RawKind.None) continue;
                if (t.Piles[h] >= TerritoryTuning.PileCap)
                {
                    t.WastedPiles++;
                    ev.Add(new PileWasted { Hex = h, Kind = kind });
                }
                else
                {
                    t.Piles[h]++;
                    ev.Add(new PileAdded { Hex = h, Kind = kind, Count = t.Piles[h] });
                }
            }
        }
    }

    // ---------------- install ----------------

    public static class TerritoryModule
    {
        /// <summary>Order of the dice system among the sim systems.</summary>
        public const int DiceOrder = 10;

        /// <summary>Registers the territory command handlers and the dice clock.</summary>
        public static void Install(GodSim sim)
        {
            sim.RegisterHandler(new PlaceHqHandler());
            sim.RegisterHandler(new PlaceBuildingHandler());
            sim.RegisterHandler(new RemoveBuildingHandler());
            sim.RegisterHandler(new BuildRoadHandler());
            sim.RegisterHandler(new RemoveRoadHandler());
            sim.RegisterHandler(new PlaceFlagHandler());
            sim.AddSystem(DiceOrder, new DiceSystem());
        }

        /// <summary>FNV-1a over the territory: piles and dice, buildings, roads, flags and HQs (the "territory" hash part).</summary>
        public static ulong Hash(GodState s)
        {
            var h = new StateHasher();
            void AddArr(int[] a) { h.Add(a.Length); foreach (int x in a) h.Add(x); }
            var t = s.Territory;
            AddArr(t.Piles); h.Add(t.RollCount); h.Add(t.WastedPiles); h.Add(t.NextId);
            h.Add(t.LastRollTick); h.Add(t.LastDie1); h.Add(t.LastDie2); h.Add(t.LastTotal);
            foreach (var b in t.Buildings) { h.Add(b.Id); h.Add(b.Node); h.Add(b.FlagId); h.Add((int)b.Faction); }
            foreach (var r in t.Roads) { h.Add(r.Id); h.Add((int)r.Faction); AddArr(r.Nodes); }
            foreach (var f in t.Flags) { h.Add(f.Id); h.Add((int)f.Faction); h.Add(f.Node); }
            foreach (var hq in t.Hqs) { h.Add(hq.Id); h.Add((int)hq.Faction); h.Add(hq.Site ? 1 : 0); h.Add(hq.Citadel ? 1 : 0); }
            return h.Value;
        }

        /// <summary>Creates the territory state and puts one finished HQ per faction on its start corner (<see cref="StartCorners.Pick"/>).</summary>
        public static void StartGame(GodState s)
        {
            s.Territory = TerritoryState.Create(s.Board.Radius, s.Board.Shape);
            var starts = StartCorners.Pick(s.Board, s.Seed, s.FactionCount);
            for (int i = 0; i < starts.Length; i++) TerritoryRules.AddHq(s, (Faction)i, starts[i], null);
        }
    }
}
