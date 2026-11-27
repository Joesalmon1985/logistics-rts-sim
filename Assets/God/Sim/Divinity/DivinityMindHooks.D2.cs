using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D2's mind hooks (contracts 10.3 steps 2-4, 11.3): they see the think scratch only (layers 2, 3, 6), never the true
    /// state. The decoded instructions enter <c>ThinkScratch.Divine</c>, become ordinary candidates of the NPC's own
    /// knowledge and physical state, and weigh by the divine term; the NPC's own policy decides. OWNER: D2.
    /// </summary>
    public static partial class DivinityMindHooks
    {
        /// <summary>The feature key of an instruction kind (<c>Feature.Divine(Suggested 1 | Commanded 2 | Compelled 3)</c>).</summary>
        public static uint DivineKey(InterventionKind k) => Feature.Make(FeatureKind.Divine,
            k == InterventionKind.Command ? DivineFeatureValue.Commanded : k == InterventionKind.Compulsion ? DivineFeatureValue.Compelled : DivineFeatureValue.Suggested);

        static partial void VerbsD2(List<VerbInfo> into)
        {
            foreach (var r in DivineActs.Rows) into.Add(r);
        }

        /// <summary>
        /// Step 2: the compulsion over this body first (while its spell lasts), then the newest unexpired instructions, at most
        /// <c>ThinkScratch.MaxDivine</c> (2). Read at the first think at or after the delivery tick (delivery happens at the tick
        /// head, before the population system: no order dependence).
        /// </summary>
        static partial void FillDivineD2(ThinkScratch sc)
        {
            var m = sc.Mind?.Divine;
            if (m == null || m.InboxCount == 0) return;
            long tick = sc.Self.Tick;
            if (sc.Control.Kind == ControlKind.Compelled)
                for (int i = 0; i < m.InboxCount; i++)
                    if (m.Inbox[i].Kind == InterventionKind.Compulsion && m.Inbox[i].Intervention == sc.Control.Intervention) { sc.Divine[sc.DivineCount++] = m.Inbox[i]; break; }
            for (int i = m.InboxCount - 1; i >= 0 && sc.DivineCount < ThinkScratch.MaxDivine; i--)
            {
                var d = m.Inbox[i];
                if (d.Kind == InterventionKind.Compulsion || d.Expires <= tick) continue;
                sc.Divine[sc.DivineCount++] = d;
            }
        }

        /// <summary>
        /// Step 3 (quota 4): the walk-then-act candidate each read instruction names (<see cref="DivineActs.Candidate"/>), with
        /// its divine feature key; the compelled candidate is marked in <c>sc.ControlCand</c> for F1's override. What was read
        /// is recorded in the mind for the response (consumed by DivinitySystem's D2 slot at the next tick).
        /// </summary>
        static partial void AddCandidatesD2(ThinkScratch sc)
        {
            var m = sc.Mind?.Divine;
            if (m == null || sc.DivineCount == 0) return;
            var read = m.ReadOrCreate();
            int n = 0;
            for (int k = 0; k < sc.DivineCount; k++)
            {
                var d = sc.Divine[k];
                var r = new DivineReading { Intervention = d.Intervention, Kind = d.Kind, God = d.God };
                int at = DivineActs.Candidate(sc, d.Act, d.Topic, DivineKey(d.Kind), ref r);
                if (d.Kind == InterventionKind.Compulsion)
                {
                    if (at >= 0 && sc.Control.Kind == ControlKind.Compelled && d.Intervention == sc.Control.Intervention) { sc.ControlCand = at; m.CompelTick = sc.Self.Tick; }
                    continue;
                }
                if (n < read.Length) read[n++] = r;
            }
            m.ReadCount = n;
            if (n > 0) m.ReadTick = sc.Self.Tick;
        }

        /// <summary>
        /// Step 4, D2's part: <c>authority(kind) x Belief x max(Trust, Fear) / 1e6</c> for each instruction read now whose
        /// candidate this is (plus the configured Commanded prior for a command). Belief, trust and fear are the mind's own
        /// <c>GodBelief</c> for the sending god (D6); without one, the tuning defaults. Clamped to +-2000 by the dispatcher.
        /// </summary>
        static partial void DivineTermD2(ThinkScratch sc, int cand, ref int term)
        {
            var m = sc.Mind?.Divine;
            if (m == null || m.ReadTick != sc.Self.Tick || m.ReadCount == 0) return;
            var c = sc.Cand[cand];
            var tu = m.Tuning ?? (m.Tuning = new InstructionTuning()); // bound at delivery; a fixture-made mind gets the defaults
            bool pain = InPain(sc);
            for (int k = 0; k < m.ReadCount; k++)
            {
                var r = m.Read[k];
                if (!r.Matches(c.Verb, c.Kind, c.TargetId, c.TargetNode)) continue;
                int w = Weight(m, tu, r.Kind, r.God);
                if (pain) w = System.Math.Min(w, PainCap(tu, r.Kind));
                term += w;
            }
        }

        /// <summary>True while any drive of the mind's own body is in the pain band (its felt state, layer 6).</summary>
        public static bool InPain(ThinkScratch sc)
        {
            for (int d = 0; d < Drive.Count; d++) if (Thinker.Band(sc.Tuning, d, Thinker.DriveValue(sc.Self, d)) >= 3) return true;
            return false;
        }

        /// <summary>The cap of an instruction's positive term in the pain band (int.MaxValue when off).</summary>
        public static int PainCap(InstructionTuning tu, InterventionKind kind)
        {
            if (tu.PainBandCap < 0) return int.MaxValue;
            if (kind == InterventionKind.Command) return tu.PainBandCap;
            return tu.AuthorityCommand <= 0 ? tu.PainBandCap : (int)((long)tu.PainBandCap * tu.AuthoritySuggestion / tu.AuthorityCommand);
        }

        /// <summary>The divine weight of an instruction of a kind from a god, by this mind's beliefs (vp, unclamped).</summary>
        public static int Weight(DivineMind m, InstructionTuning tu, InterventionKind kind, int god)
        {
            int belief = tu.DefaultBelief, trust = tu.DefaultTrust, fear = 0;
            for (int i = 0; i < m.GodCount; i++)
            {
                if (m.Gods[i].God != god) continue;
                belief = m.Gods[i].Belief; trust = m.Gods[i].Trust; fear = m.Gods[i].Fear;
                break;
            }
            long auth = kind == InterventionKind.Command ? tu.AuthorityCommand : tu.AuthoritySuggestion;
            // max(Trust, Fear); "no fear" (0) leaves trust alone, so deep distrust breeds defiance (DIVINE_PLAYER 4.3.1)
            long w = auth * belief * (fear > 0 ? System.Math.Max(trust, fear) : trust) / 1_000_000;
            if (kind == InterventionKind.Command) w += tu.CommandedPrior;
            return (int)System.Math.Max(-1_000_000, System.Math.Min(1_000_000, w));
        }

        static partial void DescribeD2(uint f, ref string text)
        {
            if (text != null || Feature.KindOf(f) != FeatureKind.Divine) return;
            switch (Feature.ValueOf(f))
            {
                case DivineFeatureValue.Suggested: text = "a god suggested it"; break;
                case DivineFeatureValue.Commanded: text = "a god commanded it"; break;
                case DivineFeatureValue.Compelled: text = "compelled by a god"; break;
            }
        }
    }
}
