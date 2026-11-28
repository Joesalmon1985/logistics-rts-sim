using System;
using System.Collections.Generic;
using God.Sim.Unified;
using God.Train.Env;
using God.Train.Net;

namespace God.Train.Teacher
{
    /// <summary>
    /// <b>TRAINING ONLY.</b> The scripted, labelled demonstrator (origin <see cref="ControlOrigin.Teacher"/>) for the stub world. It
    /// reads truth and uses a path oracle (breadth-first search over the true lattice); it exists only in <c>God.Train</c> and test
    /// fixtures, never in a game scenario, and an evaluation that meets it aborts as invalid. Its demonstrations are mechanics
    /// evidence for behaviour cloning, never an evaluation result. Rules (in order): drink when thirsty and water is in reach; eat
    /// a carried food lot when hungry; forage when hungry and a stocked forage place is in reach; sleep when tired; walk (oracle
    /// path) to water when thirsty, to a stocked forage place when hungry; forage one spare lot when in reach; otherwise wait.
    /// </summary>
    public sealed class StubTeacher : IActor
    {
        public const int NeedAt = 300, TiredAt = 600;
        public ControlOrigin Origin => ControlOrigin.Teacher;
        public void Begin(IWorld w, long seed) { if (!(w is StubWorld)) throw new InvalidOperationException("StubTeacher drives the stub world only"); }

        public ActionChoice Act(IWorld w, int personIndex, int person, short[] obs, in ActMask m)
        {
            var sw = (StubWorld)w; var p = sw.P(person);
            int hunger = p.Needs[(int)NeedChannel.Hunger], thirst = p.Needs[(int)NeedChannel.Thirst], energy = p.Needs[(int)NeedChannel.Energy];
            int food = -1, room = -1;
            for (int s = 0; s < p.Carry.Length; s++) { if (p.Carry[s] != 0 && food < 0) food = s; if (p.Carry[s] == 0 && room < 0) room = s; }
            int waterSlot = -1, forageSlot = -1;
            for (int k = 0; k < sw.Slots.Length; k++)
            {
                var x = sw.Slots[k];
                if (x.cell < 0 || sw.Dist(p.Cell, x.cell) > StubWorld.Reach) continue;
                if (x.cls == StubWorld.ClassWater && waterSlot < 0) waterSlot = k;
                if (x.cls == StubWorld.ClassForage && x.stock > 0 && forageSlot < 0) forageSlot = k;
            }
            if (thirst >= NeedAt && waterSlot >= 0) return Use(waterSlot, Heads.SourceEmpty);
            if (hunger >= NeedAt && food >= 0) return Use(Heads.TargetSelf, food);
            if (hunger >= NeedAt && forageSlot >= 0 && room >= 0) return Use(forageSlot, Heads.SourceEmpty);
            if (energy >= TiredAt) return ActionChoice.Of(Prim.Sleep);
            if (thirst >= NeedAt) { var a = Towards(sw, p, StubWorld.ClassWater, in m); if (a.HasValue) return a.Value; }
            if (hunger >= NeedAt && food < 0) { var a = Towards(sw, p, StubWorld.ClassForage, in m); if (a.HasValue) return a.Value; }
            return ActionChoice.Of(Prim.Wait);
        }

        private static ActionChoice Use(int target, int source)
        {
            var a = ActionChoice.Of(Prim.Use); a.Target = target; a.Source = source; a.Slot = 0; a.Design = Heads.DesignNone;
            return a;
        }

        /// <summary>The path oracle: first step of a shortest true path to a cell within reach of the nearest wanted thing.</summary>
        private static ActionChoice? Towards(StubWorld sw, StubWorld.Person p, int cls, in ActMask m)
        {
            // the nearest perceived wanted thing: step or turn towards its sector when that step is free; else the path oracle
            foreach (var x in sw.Slots)
            {
                if (x.cell < 0 || x.cls != cls || (cls == StubWorld.ClassForage && x.stock <= 0)) continue;
                int rel = sw.Sector(p, x.cell);
                if (rel != 0) return ActionChoice.Of(rel <= 3 ? Prim.TurnRight : Prim.TurnLeft);
                if (ActMask.Allows(m.Control, (int)Prim.StepForward)) return ActionChoice.Of(Prim.StepForward);
                break;
            }
            return Oracle(sw, p, cls);
        }

        private static ActionChoice? Oracle(StubWorld sw, StubWorld.Person p, int cls)
        {
            int n = sw.CellQ.Length;
            var goal = new bool[n];
            foreach (var t in sw.Things)
            {
                if (t.Class != cls || (cls == StubWorld.ClassForage && t.Stock <= 0)) continue;
                for (int c = 0; c < n; c++) if (sw.Passable(c) && sw.Dist(c, t.Cell) <= StubWorld.Reach) goal[c] = true;
            }
            var firstDir = new int[n]; for (int i = 0; i < n; i++) firstDir[i] = -2;
            var q = new Queue<int>(); q.Enqueue(p.Cell); firstDir[p.Cell] = -1;
            int found = -1;
            while (q.Count > 0)
            {
                int c = q.Dequeue();
                if (goal[c] && c != p.Cell) { found = c; break; }
                for (int rel = 0; rel < 6; rel++)
                {
                    int dir = (p.Facing + rel) % 6, nb = sw.Neighbour(c, dir);
                    if (!sw.Passable(nb) || firstDir[nb] != -2) continue;
                    firstDir[nb] = firstDir[c] == -1 ? dir : firstDir[c];
                    q.Enqueue(nb);
                }
            }
            if (found < 0) return null;
            int r = (firstDir[found] - p.Facing + 6) % 6;
            Prim ctl = r == 0 ? Prim.StepForward : r == 1 ? Prim.StepRight : r == 3 ? Prim.StepBack : r == 5 ? Prim.StepLeft : r == 2 ? Prim.TurnRight : Prim.TurnLeft;
            return ActionChoice.Of(ctl);
        }
    }
}
