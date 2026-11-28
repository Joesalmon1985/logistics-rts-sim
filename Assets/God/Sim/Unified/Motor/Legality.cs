namespace God.Sim.Unified
{
    /// <summary>
    /// UN2 (contracts 23.6, D9.2 a, EP-01): the physical legality mask over the <see cref="ActSchema"/> heads. A bit is set when the
    /// option is physically possible now: the target exists and is perceived, it is within reach where the control needs reach, there
    /// is room, the way is passable, the body is not busy, asleep or dead, and a design is one the body knows. There is no social,
    /// moral, ownership or "advisable" mask: an attempt that would be resisted is still a legal attempt. The motor's own answer
    /// (<see cref="Check"/>) is the truth; the mask is its summary for the brain and is built only when the body decides.
    /// </summary>
    public static partial class Motor
    {
        public const int TargetSelf = ObsSchema.Entities, TargetFacing = ObsSchema.Entities + 1, TargetNone = ObsSchema.Entities + 2;
        public const int SourceEmpty = ObsSchema.CarrySlots;
        public const int ReferentSelf = ObsSchema.Entities + ObsSchema.CarrySlots, ReferentNone = ObsSchema.Entities + ObsSchema.CarrySlots + 1;
        public const int DesignNone = 32;

        public static void Legal(GodState s, int actor, in Percept p, ref ActMask mask)
        {
            mask = default;
            mask.Control = ActMask.With(0, (int)Prim.None);
            mask.Target = ActMask.With(0, TargetNone);
            mask.Source = ActMask.With(0, SourceEmpty);
            mask.Design = ActMask.With(0, DesignNone);
            mask.Gesture = ActMask.With(0, 0);
            mask.Referent1 = ActMask.With(0, ReferentNone);
            mask.Referent2 = ActMask.With(0, ReferentNone);
            mask.Quantity = 0xFFFF;
            mask.Slot = 0xF;
            if (!Unified.On(s)) return;
            var npc = s.Population?.Find(actor);
            var u = s.Unification.PersonOf(actor);
            if (npc == null || u == null || !npc.Alive || npc.Action.Asleep || u.Busy) return;
            // targets and referents: perceived entities, self, the facing cell (when it exists), none
            for (int e = 0; e < p.EntityCount; e++)
            {
                mask.Target = ActMask.With(mask.Target, e);
                mask.Referent1 = ActMask.With(mask.Referent1, e);
                mask.Referent2 = ActMask.With(mask.Referent2, e);
            }
            mask.Target = ActMask.With(mask.Target, TargetSelf);
            if (s.Territory.Grid.Neighbour(npc.Node, u.Facing) >= 0) mask.Target = ActMask.With(mask.Target, TargetFacing);
            mask.Referent1 = ActMask.With(mask.Referent1, ReferentSelf); mask.Referent2 = ActMask.With(mask.Referent2, ReferentSelf);
            bool carrying = false;
            for (int k = 0; k < ObsSchema.CarrySlots; k++)
                if (p.CarryLot != null && p.CarryLot[k] != 0)
                {
                    carrying = true;
                    mask.Source = ActMask.With(mask.Source, k);
                    mask.Referent1 = ActMask.With(mask.Referent1, ObsSchema.Entities + k);
                    mask.Referent2 = ActMask.With(mask.Referent2, ObsSchema.Entities + k);
                }
            // designs: the i-th design the body knows (by row) is option i
            int di = 0;
            for (int row = 0; row < BrainState.DesignBits && di < DesignNone; row++)
                if (Knowledge.Knows(u.Brain, KnowledgeKind.Design, row)) mask.Design = ActMask.With(mask.Design, di++);
            if (s.Social != null) for (int g = 1; g <= MaxGesture; g++) mask.Gesture = ActMask.With(mask.Gesture, g);
            // controls
            ulong c = mask.Control;
            c = ActMask.With(c, (int)Prim.TurnLeft); c = ActMask.With(c, (int)Prim.TurnRight);
            var grid = s.Territory.Grid;
            for (var st = Prim.StepForward; st <= Prim.StepRight; st++)
            {
                int to = StepNode(grid, npc.Node, u.Facing, st);
                if (to >= 0 && Passable(s, to)) c = ActMask.With(c, (int)st);
            }
            // TAKE: any perceived person, lot or building the motor would act on (a resisted attempt is a legal attempt)
            for (int e = 0; e < p.EntityCount && !ActMask.Allows(c, (int)Prim.Take); e++)
            {
                var k = p.EntityKind[e];
                if (k != SpatialKind.Npc && k != SpatialKind.Lot && k != SpatialKind.Building) continue;
                var probe = new PrimitiveControl { Control = Prim.Take, Actor = actor, Target = Percept.TargetOf(in p, e) };
                var r = Check(s, in probe);
                if (r == MotorStatus.Accepted || r == MotorStatus.Resisted) c = ActMask.With(c, (int)Prim.Take);
            }
            if (carrying) c = ActMask.With(c, (int)Prim.Drop);
            // USE: any perceived target (or self, or the facing cell) with any source the resolver accepts at slot 0
            for (int t = 0; t < TargetNone && !ActMask.Allows(c, (int)Prim.Use); t++)
            {
                if (!ActMask.Allows(mask.Target, t)) continue;
                var target = Percept.TargetOf(in p, t);
                for (int src = 0; src <= SourceEmpty && !ActMask.Allows(c, (int)Prim.Use); src++)
                {
                    if (!ActMask.Allows(mask.Source, src)) continue;
                    var probe = new PrimitiveControl { Control = Prim.Use, Actor = actor, Target = target, SourceLot = Percept.SourceOf(in p, src) };
                    if (Check(s, in probe) == MotorStatus.Accepted) c = ActMask.With(c, (int)Prim.Use);
                }
            }
            if (s.Social != null) c = ActMask.With(c, (int)Prim.Gesture);
            c = ActMask.With(c, (int)Prim.Sleep); c = ActMask.With(c, (int)Prim.Wait);
            mask.Control = c;
        }
    }
}
