using System.Collections.Generic;

namespace God.Sim
{
    // ---------------- events ----------------

    /// <summary>A roll of the hidden Catan dice. The view may show a tick or pulse, never the number.</summary>
    public sealed class DiceRolled : SimEvent
    {
        public int Die1, Die2, Total;
    }

    /// <summary>A pile appeared at a hex centre.</summary>
    public sealed class PileAdded : SimEvent
    {
        public int Hex;
        public RawKind Kind;
        /// <summary>Piles on the hex after this one.</summary>
        public int Count;
    }

    /// <summary>A pile was due but the hex already held the cap, so it was wasted.</summary>
    public sealed class PileWasted : SimEvent
    {
        public int Hex;
        public RawKind Kind;
    }

    public sealed class HqPlaced : SimEvent { public Faction Faction; public int HqId; public int Corner; }
    public sealed class BuildingPlaced : SimEvent { public Faction Faction; public int BuildingId; public int Node; public BuildingSize Size; }
    public sealed class BuildingRemoved : SimEvent { public Faction Faction; public int BuildingId; public int Node; }
    public sealed class FlagPlaced : SimEvent { public Faction Faction; public int FlagId; public int Node; }
    public sealed class RoadBuilt : SimEvent { public Faction Faction; public int RoadId; }
    public sealed class RoadRemoved : SimEvent { public Faction Faction; public int RoadId; }

    // ---------------- dice clock ----------------

    /// <summary>
    /// Every <see cref="TerritoryTuning.DiceIntervalTicks"/> ticks rolls 2d6 with the sim Rng. Each hex whose hidden
    /// number equals the total gets one pile (cap <see cref="TerritoryTuning.PileCap"/>; extras are wasted). A 7 does
    /// nothing. The desert (number 0) never matches.
    /// </summary>
    public sealed class DiceSystem : ISimSystem
    {
        public void Tick(GodState s, EventBuffer ev)
        {
            if (s.Tick % TerritoryTuning.DiceIntervalTicks != 0) return;
            var t = s.Territory;
            int d1 = s.Rng.NextRange(1, 7), d2 = s.Rng.NextRange(1, 7), total = d1 + d2;
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

    // ---------------- start corners ----------------

    public static class StartCorners
    {
        /// <summary>
        /// Two land corners as far apart as possible on the corner graph (hex edges). Among all pairs at the greatest
        /// distance one is picked with a stream forked from the seed, and the seed also decides which side gets which.
        /// </summary>
        public static void Pick(Board board, ulong seed, out int player, out int enemy)
        {
            var topo = board.Topology;
            var land = BoardGenerator.LandCorners(board, topo);
            int best = -1;
            var pairs = new List<(int, int)>();
            for (int i = 0; i < land.Count; i++)
                for (int j = i + 1; j < land.Count; j++)
                {
                    int d = topo.CornerDistance(land[i], land[j]);
                    if (d > best) { best = d; pairs.Clear(); }
                    if (d == best) pairs.Add((land[i], land[j]));
                }
            var rng = new Rng(seed).Fork("start.corners");
            var pick = pairs[rng.NextInt(pairs.Count)];
            if (rng.NextInt(2) == 0) { player = pick.Item1; enemy = pick.Item2; }
            else { player = pick.Item2; enemy = pick.Item1; }
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

        /// <summary>Creates the territory state and puts the two starting HQs down.</summary>
        public static void StartGame(GodState s)
        {
            s.Territory = TerritoryState.Create(s.Board.Radius);
            StartCorners.Pick(s.Board, s.Seed, out int p, out int e);
            TerritoryRules.AddHq(s, Faction.Player, p, null);
            TerritoryRules.AddHq(s, Faction.Enemy, e, null);
        }
    }
}
