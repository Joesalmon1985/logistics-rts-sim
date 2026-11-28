using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN2 (D9.4 a, MOTOR-1): facing and steps. Facing is one of six world sectors of 60 degrees, sector f centred on bearing 60 f
    /// (0 = +Z, increasing towards +X, i.e. clockwise seen from above); <c>TurnRight</c> adds one sector, <c>TurnLeft</c> removes one.
    /// A step resolves to the lattice neighbour whose bearing is nearest the wanted bearing (forward: the facing; back: its opposite;
    /// left/right: -90/+90 degrees), ties by lower node id; no neighbour within 30 degrees, an impassable node (sea, off the board)
    /// or an occupied footprint (a building) is <see cref="MotorStatus.Blocked"/>. A step moves exactly one lattice edge when it
    /// completes; there is no MoveTo, no jump and no pather behind it.
    /// </summary>
    public static partial class Motor
    {
        /// <summary>The bearing in degrees of lattice direction <paramref name="dir"/> (<c>NodeGrid.DI/DJ</c>): 60 dir.</summary>
        public static int BearingOf(int dir) => dir * 60;

        /// <summary>The bearing a step control wants, from the facing sector.</summary>
        public static int WantedBearing(byte facing, Prim step)
        {
            int b = facing * 60;
            switch (step)
            {
                case Prim.StepBack: b += 180; break;
                case Prim.StepLeft: b -= 90; break;
                case Prim.StepRight: b += 90; break;
            }
            return ((b % 360) + 360) % 360;
        }

        /// <summary>
        /// The node a step from <paramref name="node"/> with <paramref name="facing"/> leads to by geometry alone (-1 when no lattice
        /// neighbour lies within 30 degrees of the wanted bearing); passability is not checked here.
        /// </summary>
        public static int StepNode(NodeGrid g, int node, byte facing, Prim step)
        {
            if (!g.IsValid(node)) return -1;
            int want = WantedBearing(facing, step);
            int best = -1, bestDiff = int.MaxValue;
            for (int dir = 0; dir < 6; dir++)
            {
                int n = g.Neighbour(node, dir);
                if (n < 0) continue;
                int d = System.Math.Abs(BearingOf(dir) - want) % 360;
                if (d > 180) d = 360 - d;
                if (d > 30) continue;
                if (d < bestDiff || d == bestDiff && n < best) { best = n; bestDiff = d; }
            }
            return best;
        }

        /// <summary>True when a body may stand on <paramref name="node"/>: walkable land, no building footprint.</summary>
        public static bool Passable(GodState s, int node)
        {
            var ix = s.Unification.Index;
            return ix.Walkable(s, node) && !ix.Any(s, node, SpatialKind.Building);
        }

        private static MotorStatus Step(GodState s, Npc npc, UnifiedNpc u, in PrimitiveControl c, ControlOrigin origin, bool apply)
        {
            int to = StepNode(s.Territory.Grid, npc.Node, u.Facing, c.Control);
            if (to < 0 || !Passable(s, to)) return MotorStatus.Blocked;
            return Start(s, u, c.Control, s.Tuning.Unification.UN2.StepTicks, new TargetRef(TargetKind.Node, to), origin, apply);
        }

        /// <summary>The step's edge is taken now (re-checked: the node may have become impassable meanwhile).</summary>
        private static MotorStatus FinishStep(GodState s, Npc npc, UnifiedNpc r)
        {
            int to = r.ActionTarget.Id;
            var g = s.Territory.Grid;
            if (!g.IsValid(to) || g.Distance(npc.Node, to) != 1 || !Passable(s, to)) return MotorStatus.Blocked;
            npc.Node = to;
            s.Population.IndexDirty = true;
            s.Unification.Index.MarkDirty(); // the next entity query rebuilds (passability checks do not: buildings do not move)
            return MotorStatus.Accepted;
        }
    }
}
