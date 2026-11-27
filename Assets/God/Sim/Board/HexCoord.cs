using System;

namespace God.Sim
{
    /// <summary>Axial hex coordinate (pointy-top). s = -q - r.</summary>
    public readonly struct HexCoord : IEquatable<HexCoord>
    {
        public readonly int Q;
        public readonly int R;

        public HexCoord(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int S => -Q - R;

        /// <summary>Distance in hex steps.</summary>
        public int DistanceTo(HexCoord o) =>
            (Math.Abs(Q - o.Q) + Math.Abs(R - o.R) + Math.Abs(S - o.S)) / 2;

        public int Ring => (Math.Abs(Q) + Math.Abs(R) + Math.Abs(S)) / 2;

        /// <summary>Axial neighbour directions, index 0..5 (E, NE, NW, W, SW, SE with +Z as north).</summary>
        public static readonly ReadOnlyArray<HexCoord> Directions = new ReadOnlyArray<HexCoord>(
            new HexCoord(1, 0), new HexCoord(0, 1), new HexCoord(-1, 1),
            new HexCoord(-1, 0), new HexCoord(0, -1), new HexCoord(1, -1));

        public HexCoord Neighbour(int dir) => new HexCoord(Q + Directions[dir].Q, R + Directions[dir].R);

        public bool Equals(HexCoord o) => Q == o.Q && R == o.R;
        public override bool Equals(object obj) => obj is HexCoord o && Equals(o);
        public override int GetHashCode() => (Q * 397) ^ R;
        public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
        public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);
        public override string ToString() => "(" + Q + "," + R + ")";
    }


    public enum Terrain
    {
        /// <summary>Open ground at the rim of a larger board; yields nothing.</summary>
        Meadow = 0,
        /// <summary>Gives logs.</summary>
        Forest = 1,
        /// <summary>Gives clay.</summary>
        Hills = 2,
        /// <summary>Gives grain.</summary>
        Fields = 3,
        /// <summary>Gives stone or ore.</summary>
        Mountain = 4,
        /// <summary>Grazing land. Not dealt on the island; kept for the dressing view.</summary>
        Pasture = 5,
        /// <summary>The one desert tile of the island: yields nothing and has no number.</summary>
        Desert = 6,
    }

    public static class TerrainInfo
    {
        /// <summary>Number of terrains (length of per-terrain tables).</summary>
        public const int Count = 7;

        /// <summary>Lower-case terrain name for sentences.</summary>
        public static string DisplayName(Terrain t) => t.ToString().ToLowerInvariant();

        /// <summary>True for terrains that receive production numbers (everything except Meadow and Desert).</summary>
        public static bool IsResource(Terrain t) => t != Terrain.Meadow && t != Terrain.Desert;
    }
}
