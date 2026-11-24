using System;

namespace God.Sim
{
    /// <summary>
    /// The 5 m walking lattice in the Sim: a triangular lattice with its origin at the board centre, cell (i, j) at
    /// X = 5 * (sqrt 3 / 2) * i, Z = 5 * (i / 2 + j); the six steps are (0,1), (1,0), (1,-1), (0,-1), (-1,0), (-1,1).
    /// P1 builds the fine plot grid and road nodes on top of it.
    /// </summary>
    public static class Lattice
    {
        public const double Step = 5.0;

        private static readonly double ColumnX = Step * Math.Sqrt(3.0) * 0.5;

        /// <summary>World X of a cell's centre.</summary>
        public static double X(int i, int j) => ColumnX * i;

        /// <summary>World Z of a cell's centre.</summary>
        public static double Z(int i, int j) => Step * (i * 0.5 + j);

        /// <summary>The nearest cell to a world point (cube rounding, exactly as WalkLattice.ToCell).</summary>
        public static void CellOf(double x, double z, out int i, out int j)
        {
            double q = x / ColumnX;
            double r = z / Step - q * 0.5;
            double cx = q, cz = r, cy = -cx - cz;
            double rx = Math.Round(cx), ry = Math.Round(cy), rz = Math.Round(cz);
            double dx = Math.Abs(rx - cx), dy = Math.Abs(ry - cy), dz = Math.Abs(rz - cz);
            if (dx > dy && dx > dz) rx = -ry - rz;
            else if (dy <= dz) rz = -rx - ry;
            i = (int)rx;
            j = (int)rz;
        }

        /// <summary>Steps between two cells (each step one of the six moves).</summary>
        public static int Distance(int i0, int j0, int i1, int j1)
        {
            int di = i1 - i0, dj = j1 - j0;
            return (Math.Abs(di) + Math.Abs(dj) + Math.Abs(di + dj)) / 2;
        }
    }
}
