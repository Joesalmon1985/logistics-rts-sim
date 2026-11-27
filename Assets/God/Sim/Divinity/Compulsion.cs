using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 11 (DIVINE_PLAYER 4.3.3): temporary compulsion. A compulsion is a <c>Compelled</c> control spell opened only
    /// through <see cref="Control.TryAcquire"/> and closed only through <see cref="Control.Release"/> (or F1's sweep). The mind
    /// still runs; the divinity mind hook builds the compelled act as an ordinary candidate from the NPC's own physical state
    /// (it walks, holds and acts through <c>Match</c>/<c>Start</c>/<c>Act</c>); F1's override puts it in place of the policy's
    /// choice while it exists. When it cannot be built (nothing held, nothing there), the spell is <c>Blocked</c>
    /// (<see cref="CompulsionBlocked"/>) and retries at each think: never teleported, never conjured. OWNER: D2.
    /// </summary>
    public static class Compulsions
    {
        /// <summary>The expiry a command asks for (0: the default timeout), or -1 when out of range.</summary>
        public static int ExpireOf(GodState s, int expire)
        {
            var tu = s.Tuning.Divinity.Instruction;
            if (expire < 0 || expire > tu.MaxExpireTicks) return -1;
            return expire == 0 ? tu.CompulsionTimeoutTicks : expire;
        }

        internal static CommandResult Validate(GodState s, IssueCompulsion c)
        {
            if (s.Divinity == null) return Instructions.Reject(DivineReasons.DivinityOff);
            if (!DivineActs.Available(c.Act)) return Instructions.Reject(DivineReasons.ActUnavailable);
            if (ExpireOf(s, c.ExpireTicks) < 0) return Instructions.Reject(DivineReasons.BadTarget);
            if (c.Target.Kind != DivineTargetKind.Npc) return Instructions.Reject(DivineReasons.BadTarget);
            var body = s.Population.Find(c.Target.Id);
            if (body == null) return Instructions.Reject(DivineReasons.BadTarget);
            if (!Bodies.IsPerson(body)) return Instructions.Reject(DivineReasons.NotAPerson);
            if (!body.Alive) return Instructions.Reject(DivineReasons.TargetDead);
            // the precedence of contracts 11.1, checked without mutation (TryAcquire checks it again)
            var cur = Control.EntryOf(s, body.Id);
            if (cur.Kind != ControlKind.Self && (cur.Kind != ControlKind.Compelled || cur.God != c.God)) return Instructions.Reject(DivineReasons.AlreadyControlled);
            string why = DivineActs.CheckObject(s, c.Act, c.Target);
            if (why != null) return Instructions.Reject(why);
            return Mythic.CanSpend(s, c.God, InterventionKind.Compulsion, s.Tuning.Divinity.Mythic.Costs.IssueCompulsion);
        }

        internal static CommandResult Apply(GodState s, EventBuffer ev, IssueCompulsion c)
        {
            var r = Validate(s, c);
            if (!r.Ok) return r;
            long cost = s.Tuning.Divinity.Mythic.Costs.IssueCompulsion;
            r = Mythic.TrySpend(s, ev, c.God, InterventionKind.Compulsion, cost, Interventions.Peek(s));
            if (!r.Ok) return r;
            int id = Interventions.Open(s, ev, c.God, InterventionKind.Compulsion, c.Act, c.Target, DivineScope.Individual, cost);
            int expire = ExpireOf(s, c.ExpireTicks);
            var body = s.Population.Find(c.Target.Id);
            var topic = DivineActs.TopicOf(s, c.Act, c.Target);
            var entry = new ControlEntry
            {
                Body = body.Id, Kind = ControlKind.Compelled, God = c.God, Intervention = id, SinceTick = s.Tick, ExpiresTick = s.Tick + expire,
                Act = c.Act, Target = c.Target,
            };
            if (!Control.TryAcquire(s, ev, in entry, out string reason)) return Instructions.Reject(reason); // validated above: not expected
            var reg = Instructions.RegistryOf(s);
            var ins = new Instruction { Id = id, God = c.God, Kind = InterventionKind.Compulsion, Act = c.Act, Target = c.Target, Scope = DivineScope.Individual, Tick = s.Tick, Expires = entry.ExpiresTick };
            ins.Recipients.Add(new InstructionRecipient { Npc = body.Id, State = RecipientState.Perceived, DecodedAct = c.Act, PerceivedTick = s.Tick });
            reg.Items.Add(ins);
            reg.Issued++;
            // the compelled act as the mind holds it (truth: a compulsion is not decoded), first in the inbox while the spell lasts
            var m = Instructions.MindOf(s, body);
            m.Deliver(new DecodedDivine { Intervention = id, God = c.God, Kind = InterventionKind.Compulsion, Act = c.Act, Topic = topic, Expires = entry.ExpiresTick, Clarity = 255 });
            ev.Add(new CompulsionStarted
            {
                Intervention = id, God = c.God, Npc = body.Id, Act = c.Act, TargetKind = c.Target.Kind, TargetId = c.Target.Id, TargetNode = c.Target.Node,
                TargetRadius = c.Target.Radius, Expires = entry.ExpiresTick,
            });
            return CommandResult.Accept();
        }

        /// <summary>
        /// The end report of a compulsion spell (from <c>Control.EndedD2</c>, once per spell, CT1): <see cref="CompulsionEnded"/>,
        /// the compelled reward scaled, the inbox entry and the instruction record removed.
        /// </summary>
        internal static void Ended(GodState s, EventBuffer ev, in ControlEntry e, ControlEndReason why)
        {
            if (e.Kind != ControlKind.Compelled) return;
            ev.Add(new CompulsionEnded { Intervention = e.Intervention, God = e.God, Npc = e.Body, Reason = why, Started = e.Started, SinceTick = e.SinceTick });
            var npc = s.Population?.Find(e.Body);
            if (npc != null && npc.Alive) ScaleReward(s, npc);
            npc?.Mind?.Divine?.Remove(e.Intervention);
            // R63: the mind remembers having been compelled (SelfFeature.Compelled) for MemoryDays after a spell its body acted under
            if (e.Started && npc != null && npc.Alive && npc.Mind?.Divine != null)
                npc.Mind.Divine.CompelledUntil = s.Tick + (long)s.Tuning.Divinity.Instruction.MemoryDays * SimCalendar.Of(s).TicksPerDay;
            var reg = s.Divinity.Messages;
            if (reg == null) return;
            for (int i = 0; i < reg.Items.Count; i++) if (reg.Items[i].Id == e.Intervention) { reg.Items.RemoveAt(i); break; }
        }

        /// <summary>
        /// Contracts 11.3 learning: rewards earned under compulsion scale by <c>Instruction.CompelLearnPermille</c>. The body's
        /// drive changes are settled into the store's pending reward (the same formula as the population's think), then the
        /// pending reward is scaled; at 1000 (default) nothing is touched.
        /// </summary>
        internal static void ScaleReward(GodState s, Npc npc)
        {
            int permille = s.Tuning.Divinity.Instruction.CompelLearnPermille;
            if (permille == 1000 || npc.Mind == null) return;
            var tu = s.Tuning.Population;
            var lt = s.Tuning.Learning;
            int r = 0;
            for (int d = 0; d < Drive.Count; d++)
            {
                int prev = npc.RewardSnap[d], now = npc.Drives[d];
                if (prev == now) continue;
                r += Learner.DriveReward(lt.W(d), Cognition.Thinker.Comfort(tu, d) * 1000, prev, now) / 1000;
                npc.RewardSnap[d] = now;
            }
            Learner.AddReward(npc.Mind.Store, r);
            var st = npc.Mind.Store;
            st.RewardAcc = Fx.MulDiv(st.RewardAcc, permille, 1000);
        }

        /// <summary>
        /// Per tick (DivinitySystem's D2 slot): for every compelled body that thought last tick, Blocked when the mind could not
        /// build the compelled candidate at that think (<see cref="CompulsionBlocked"/> on each transition into Blocked); before
        /// a body's think, its compelled reward is scaled. Ascending body.
        /// </summary>
        internal static void Tick(GodState s, EventBuffer ev)
        {
            var t = s.Divinity.Control;
            if (t == null || t.Entries.Count == 0) return;
            var p = s.Population;
            int every = s.Tuning.Population.ThinkEvery;
            for (int i = 0; i < t.Entries.Count; i++)
            {
                var e = t.Entries[i];
                if (e.Kind != ControlKind.Compelled) continue;
                var npc = p.Find(e.Body);
                if (npc == null || !npc.Alive) continue;
                var m = npc.Mind?.Divine;
                if (npc.LastThinkTick == s.Tick - 1 && npc.LastThinkTick >= e.SinceTick && m != null && m.CompelTick != npc.LastThinkTick && e.Status != ControlStatus.Blocked)
                {
                    // the status write is the only field D2 changes on an entry (Control has no setter for Blocked: contracts 11.3)
                    e.Status = ControlStatus.Blocked;
                    t.Entries[i] = e;
                    ev.Add(new CompulsionBlocked { Intervention = e.Intervention, God = e.God, Npc = e.Body, Act = e.Act, Reason = "cannot " + e.Act + " now (not held, not known or cannot carry)" });
                }
                if ((s.Tick + npc.Id) % every == 0 || Control.ForceThink(s, npc)) ScaleReward(s, npc);
            }
        }
    }

    /// <summary><c>IssueCompulsion { God, Act, Target (the body), ExpireTicks }</c> (contracts 10.2, 11). OWNER: D2.</summary>
    public sealed class IssueCompulsionHandler : CommandHandler<IssueCompulsion>
    {
        protected override CommandResult Validate(GodState s, IssueCompulsion c) => Compulsions.Validate(s, c);
        protected override CommandResult Apply(GodState s, IssueCompulsion c, EventBuffer ev) => Compulsions.Apply(s, ev, c);
    }

    /// <summary><c>CancelCompulsion { God, Intervention }</c>: the god ends its own compulsion (<c>Cancelled</c>). OWNER: D2.</summary>
    public sealed class CancelCompulsionHandler : CommandHandler<CancelCompulsion>
    {
        protected override CommandResult Validate(GodState s, CancelCompulsion c)
        {
            if (s.Divinity == null) return Instructions.Reject(DivineReasons.DivinityOff);
            var ins = s.Divinity.Messages?.Find(c.Intervention);
            if (ins == null || ins.Kind != InterventionKind.Compulsion || ins.God != c.God || ins.Recipients.Count == 0) return Instructions.Reject(DivineReasons.BadTarget);
            var e = Control.EntryOf(s, ins.Recipients[0].Npc);
            if (e.Kind != ControlKind.Compelled || e.Intervention != c.Intervention) return Instructions.Reject(DivineReasons.BadTarget);
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, CancelCompulsion c, EventBuffer ev)
        {
            var r = Validate(s, c);
            if (!r.Ok) return r;
            Control.Release(s, ev, s.Divinity.Messages.Find(c.Intervention).Recipients[0].Npc, ControlEndReason.Cancelled);
            return CommandResult.Accept();
        }
    }

    /// <summary>D2's slot of the frozen control seam: the compulsion's end report.</summary>
    public static partial class Control
    {
        static partial void EndedD2(GodState s, EventBuffer ev, in ControlEntry e, ControlEndReason why) => Compulsions.Ended(s, ev, in e, why);
    }

    /// <summary>D2's slot of <see cref="DivinitySystem"/> (after <c>Control.Sweep</c> and movement): compulsions, then instructions.</summary>
    public sealed partial class DivinitySystem
    {
        partial void D2Tick(GodState s, EventBuffer ev)
        {
            Compulsions.Tick(s, ev);
            Instructions.Tick(s, ev);
        }
    }
}
