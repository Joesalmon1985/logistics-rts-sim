using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Social;
using God.Sim.Tests;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M5 social learning, asserted on choices (ACCEPTANCE M5.5, M5.6 / L14, M5.11 / L16): a learner in a controlled glade with
    /// benchmark counterparts whose behaviour sets the consequences; the learner is never scripted. Seeds S50 = 1-50 unless
    /// stated; each arm pair shares its seed.
    /// </summary>
    [Parallelizable(ParallelScope.Children)]
    public class M5LearningTests
    {
        private const int Day = 1200;

        // ================================================================ asking for food (communication; relationships)

        private sealed class AskResult
        {
            /// <summary>Thinks with a request candidate, requests made; to the generous one and to the other (two-person scene).</summary>
            public int Opportunities, Asked, AskedGenerous, AskedOther, Fed;
        }

        /// <summary>
        /// A hungry learner with no food and no wild food, beside counterparts that carry food. <paramref name="generous"/>
        /// answers a request for food by giving one; <paramref name="other"/> (if any) never answers. Counted on days
        /// <paramref name="fromDay"/>..<paramref name="toDay"/>: thinks where asking was an option, and the requests made.
        /// </summary>
        private static AskResult RunAsk(ulong seed, LearningMode mode, bool generousGives, bool twoPeople, int fromDay, int toDay)
        {
            var lab = new M5SocialLab(seed);
            var g = new BenchSocialPolicy("bench.giver") { Offers = false, AnswersNeeds = generousGives };
            var gen = lab.Add(lab.At(0, 2), g, LearningMode.On, fed: true, (Ware.Berries, 4));
            Npc other = null;
            if (twoPeople) other = lab.Add(lab.At(3, 2), new BenchSocialPolicy("bench.ignorer") { Offers = false, AnswersNeeds = false }, LearningMode.On, fed: true, (Ware.Berries, 4));
            var l = lab.AddH(lab.Glade, null, mode, false, 600);
            // the generous one is a friend (a record the learner already holds; test-only)
            SocialFixtures.SetRelation(lab.Sim, l, gen, 400, 600);
            l.Mind.Social.Rel[l.Mind.Social.Find(gen.Id)].Affection = 400;
            if (other != null) { SocialFixtures.SetRelation(lab.Sim, l, other, 400, 600); l.Mind.Social.Rel[l.Mind.Social.Find(other.Id)].Affection = 400; }
            var r = new AskResult();
            long from = (long)(fromDay - 1) * Day, to = (long)toDay * Day;
            lab.Pop.Inspect = (id, sc) =>
            {
                if (id != l.Id || sc.Self.Tick < from || sc.Self.Tick >= to) return;
                for (int i = 0; i < sc.CandCount; i++) if (sc.Cand[i].Verb == SocialVerb.SayNeedFood) { r.Opportunities++; break; }
            };
            lab.KeepEvents = false;
            lab.OnEvents = evs =>
            {
                foreach (var e in evs)
                {
                    if (e.Tick < from || e.Tick >= to) continue;
                    if (e is GestureMade gm && gm.Speaker == l.Id && gm.Act == GestureAct.NeedFood)
                    {
                        r.Asked++;
                        if (gm.Target == gen.Id) r.AskedGenerous++; else if (other != null && gm.Target == other.Id) r.AskedOther++;
                    }
                    if (e is GiftGiven gg && gg.Receiver == l.Id) r.Fed++;
                }
            };
            lab.Step(toDay * Day);
            return r;
        }

        private static int Pct(int a, int b) => b == 0 ? 0 : 100 * a / b;

        /// <summary>
        /// M5.5 communication learning: a learner whose NeedFood to a friend is always answered with food uses it at least
        /// twice as often by day 5 (days 1-5, as a share of the thinks where it could ask) as one whose friend always ignores
        /// it, in at least 90% of 50 seeds.
        /// </summary>
        [Test]
        public void M5_5_NeedFoodAnsweredWithFood_IsUsedAtLeastTwiceAsOften_AsWhenIgnored()
        {
            int ok = 0, n = 50;
            var lines = new List<string>();
            var rows = Enumerable.Range(1, n).AsParallel().AsOrdered().Select(sd =>
            {
                var fed = RunAsk((ulong)sd, LearningMode.On, true, false, 1, 5);
                var ign = RunAsk((ulong)sd, LearningMode.On, false, false, 1, 5);
                return (sd, fed, ign);
            }).ToList();
            foreach (var (sd, fed, ign) in rows)
            {
                int rf = Pct(fed.Asked, fed.Opportunities), ri = Pct(ign.Asked, ign.Opportunities);
                bool pass = fed.Opportunities > 0 && (ign.Opportunities == 0 ? fed.Asked > 0 : rf >= 2 * ri && fed.Asked > 0);
                if (pass) ok++;
                lines.Add("seed " + sd + ": answered " + fed.Asked + "/" + fed.Opportunities + " (" + rf + "%), fed " + fed.Fed + "; ignored " + ign.Asked + "/" + ign.Opportunities + " (" + ri + "%)" + (pass ? "" : "  <- not 2x"));
            }
            foreach (var x in lines) TestContext.WriteLine(x);
            TestContext.WriteLine("seeds with at least 2x: " + ok + " of " + n + "; pooled answered " + Pct(rows.Sum(x => x.fed.Asked), rows.Sum(x => x.fed.Opportunities))
                + "%, ignored " + Pct(rows.Sum(x => x.ign.Asked), rows.Sum(x => x.ign.Opportunities)) + "%");
            Assert.GreaterOrEqual(ok, 45, "at least 2x in at least 90% of 50 seeds");
        }

        // ================================================================ choices between options (L16)

        /// <summary>One think of the learner: the options of interest it had and what it chose.</summary>
        private struct Choice { public bool A, B; public bool ChoseA, ChoseB; public int Day; }

        /// <summary>
        /// Runs a lab and records, for each think of <paramref name="learner"/> where option A or B was a candidate (per
        /// <paramref name="isA"/>/<paramref name="isB"/>), whether it chose A or B (the intent it started, D3 decision trace).
        /// </summary>
        private static List<Choice> Watch(M5SocialLab lab, Npc learner, Func<God.Sim.Cognition.Candidate, bool> isA, Func<God.Sim.Cognition.Candidate, bool> isB, int days)
        {
            var list = new List<Choice>();
            var cur = default(Choice); long curTick = -1;
            lab.Pop.Inspect = (id, sc) =>
            {
                if (id != learner.Id) return;
                if (curTick >= 0 && (cur.A || cur.B)) list.Add(cur);
                cur = new Choice { Day = sc.Self.Day }; curTick = sc.Self.Tick;
                for (int i = 0; i < sc.CandCount; i++) { if (isA(sc.Cand[i])) cur.A = true; if (isB(sc.Cand[i])) cur.B = true; }
            };
            lab.Pop.Started = (id, a) =>
            {
                if (id != learner.Id || lab.Sim.State.Tick != curTick) return;
                var c = new God.Sim.Cognition.Candidate { Verb = a.Verb, Kind = a.TargetKind, TargetId = a.TargetId, TargetNode = a.TargetNode };
                if (isA(c)) cur.ChoseA = true; else if (isB(c)) cur.ChoseB = true;
            };
            lab.KeepEvents = false;
            lab.Step(days * Day);
            if (cur.A || cur.B) list.Add(cur);
            return list;
        }

        /// <summary>The share (percent) of thinks with both options where it chose A, on days from..to.</summary>
        private static (int pct, int n) ShareA(List<Choice> l, int from, int to)
        {
            var both = l.Where(c => c.A && c.B && c.Day >= from && c.Day <= to && (c.ChoseA || c.ChoseB)).ToList();
            return (Pct(both.Count(c => c.ChoseA), both.Count), both.Count);
        }

        private static void Domain(string name, Func<ulong, LearningMode, (int pct, int n)> run, int n = 50)
        {
            int ok = 0;
            var rows = Enumerable.Range(1, n).AsParallel().AsOrdered().Select(sd => (sd, on: run((ulong)sd, LearningMode.On), fr: run((ulong)sd, LearningMode.Frozen))).ToList();
            foreach (var (sd, on, fr) in rows)
            {
                bool pass = on.n > 0 && fr.n > 0 && Math.Abs(on.pct - fr.pct) >= 20;
                if (pass) ok++;
                TestContext.WriteLine(name + " seed " + sd + ": learner " + on.pct + "% of " + on.n + ", frozen " + fr.pct + "% of " + fr.n + (pass ? "" : "  <-"));
            }
            TestContext.WriteLine(name + ": " + ok + " of " + n + " seeds differ by 20+ points; mean learner " + (int)rows.Average(x => x.on.pct) + "%, frozen " + (int)rows.Average(x => x.fr.pct) + "%");
            Assert.GreaterOrEqual(ok, n * 9 / 10, name + ": the learner's rate of the rewarding choice differs from Frozen's by 20+ points in 90% of seeds");
        }

        /// <summary>
        /// M5.11 / L16, relationships (D3 §11: it learns that a particular person is helpful): two friends carry food on the
        /// same node (equal cost to address), one gives when asked and the other never answers. On days 3-8, when it can ask either, the learner
        /// asks the generous one at a rate that differs by 20+ points from Frozen from birth (which has no reason to prefer
        /// one), in 90% of 50 seeds. The lesson is carried by the person (NpcRef): the two are otherwise alike.
        /// </summary>
        [Test]
        public void M5_11_L16_Relationships_LearnsWhichPersonHelps()
        {
            Domain("relationships", (seed, mode) =>
            {
                // a still scene: nobody wanders off to explore, and both stand within hand-over reach, so neither has to walk to help
                var lab = new M5SocialLab(seed, t => t.Population.ExploreNodes = 0);
                // which side the generous one stands on alternates with the seed, so place and id order cannot explain a preference
                bool first = seed % 2 == 0;
                Npc gen = null, oth = null;
                // which of the two is made first (lower id, first in sight order at equal distance) alternates with the seed
                for (int k = 0; k < 2; k++)
                {
                    if (k == 0 == first) gen = lab.Add(lab.At(0, 1), new BenchSocialPolicy("bench.giver") { Offers = false, AnswersNeeds = true }, LearningMode.On, true, (Ware.Berries, 4));
                    else oth = lab.Add(lab.At(0, 1), new BenchSocialPolicy("bench.ignorer") { Offers = false, AnswersNeeds = false }, LearningMode.On, true, (Ware.Berries, 4));
                }
                var l = lab.AddH(lab.Glade, null, mode, false, 600);
                foreach (var o in new[] { gen, oth }) { SocialFixtures.SetRelation(lab.Sim, l, o, 400, 600); l.Mind.Social.Rel[l.Mind.Social.Find(o.Id)].Affection = 400; }
                var ch = Watch(lab, l, c => c.Verb == SocialVerb.SayNeedFood && c.TargetId == gen.Id, c => c.Verb == SocialVerb.SayNeedFood && c.TargetId == oth.Id, 8);
                return ShareA(ch, 3, 8);
            });
        }


        /// <summary>
        /// M5.11 / L16, communication (which message works with whom): a hungry learner with no food and no wild food beside
        /// a friend who answers a request for food with food. Searching (exploring) is the instinct default; the rewarding
        /// choice is to ask that friend. On days 3-5, when it could ask, the learner asks at a rate that differs by 20+ points
        /// from Frozen from birth, in 90% of 50 seeds.
        /// </summary>
        [Test, Explicit("M5 outstanding target: 35 of 50 seeds at 20+ points (mean +20); asking is close to the instinct default here (see the M5 record)"), Category("M5-target")]
        public void M5_11_L16_Communication_LearnsToAskAFriendWhoAnswers()
        {
            Domain("communication", (seed, mode) =>
            {
                var r = RunAsk(seed, mode, true, false, 3, 5);
                return (Pct(r.Asked, r.Opportunities), r.Opportunities);
            });
        }

        // ================================================================ L14: avoidance after being cheated (M5.6)

        private sealed class L14Result
        {
            public int BaseOffers, BaseAccepted;          // S's offers before any betrayal
            public int SOffers, SAccepted;                // S's next 5 offers after 2 betrayals
            public int OtherOffers, OtherAccepted;        // fresh strangers' offers in the same period
            public int RecoverOffers, RecoverAccepted;    // strangers' offers on the last day, after 10 honest trades
            public int HonestTrades, Betrayals;
            public int LearnedS, LearnedStranger, TrustS;  // what was learned (NpcRef(S), NpcRel(Stranger)) and social trust in S
        }

        /// <summary>
        /// L14 (M5.6): a learner trades with stranger S, who takes the goods and leaves, twice; then S offers 5 more times
        /// while a fresh stranger offers too; then the learner trades honestly with other strangers. The social term is off
        /// (the default), so trust does not enter the choice: what changes the choice is learned.
        /// </summary>
        private static L14Result RunL14(ulong seed, LearningMode mode)
        {
            var lab = new M5SocialLab(seed, t => { t.Population.ExploreNodes = 0; t.Social.OfferCooldownTicks = 100; t.Social.DealTimeoutTicks = 240; });
            var l = lab.AddH(lab.Glade, null, mode, false, 300, (Ware.Roots, 3));
            var sp = new BenchSocialPolicy("bench.cheat") { Delivers = false, Only = l.Id };
            var sNpc = lab.Add(lab.At(0, 1), sp, LearningMode.On, true, (Ware.Fish, 3));
            var others = new List<(Npc n, BenchSocialPolicy p)>();
            for (int k = 0; k < 4; k++)
            {
                var p = new BenchSocialPolicy("bench.honest" + k) { Only = l.Id, Active = false };
                others.Add((lab.Add(lab.At(1 + k, 1), p, LearningMode.On, true, (Ware.Fish, 3)), p));
                lab.Sink(others[k].n, Ware.Roots);
            }
            lab.Sink(sNpc, Ware.Roots);
            var r = new L14Result();
            var accepted = new HashSet<int>();
            var offers = new List<(int speaker, int deal, long tick, int phase)>();
            int phase = 0, sOffersAfter = 0, completed = 0;
            lab.KeepEvents = false;
            lab.OnEvents = evs =>
            {
                foreach (var e in evs)
                {
                    if (e is GestureMade g && g.Act == GestureAct.Offer && g.Speaker != l.Id) offers.Add((g.Speaker, g.DealId, g.Tick, phase));
                    if (e is GestureMade a && a.Act == GestureAct.Accept && a.Speaker == l.Id) accepted.Add(a.DealId);
                    if (e is BetrayalRecorded b && b.Victim == l.Id && b.Offender == sNpc.Id) r.Betrayals++;
                    if (e is ExchangeClosed c && c.Outcome == DealOutcome.Completed && c.A != sNpc.Id && c.B != sNpc.Id) completed++;
                }
            };
            for (int t = 0; t < 12 * Day; t += 10)
            {
                lab.Step(10);
                if (phase == 0 && r.Betrayals >= 2) { phase = 1; others[0].p.Active = true; others[1].p.Active = true; }
                if (phase == 1) sOffersAfter = offers.Count(o => o.phase == 1 && o.speaker == sNpc.Id);
                if (phase == 1 && sOffersAfter >= 5) { phase = 2; sp.Active = false; foreach (var o in others) o.p.Active = true; r.HonestTrades = completed; }
                if (phase == 2 && completed - r.HonestTrades >= 10) { phase = 3; r.HonestTrades = completed - r.HonestTrades; }
                if (phase == 3 && offers.Count(o => o.phase == 3) >= 8) break;
            }
            foreach (var o in offers)
            {
                bool acc = accepted.Contains(o.deal);
                if (o.phase == 0 && o.speaker == sNpc.Id) { r.BaseOffers++; if (acc) r.BaseAccepted++; }
                if (o.phase == 1 && o.speaker == sNpc.Id && r.SOffers < 5) { r.SOffers++; if (acc) r.SAccepted++; }
                if (o.phase == 1 && o.speaker != sNpc.Id) { r.OtherOffers++; if (acc) r.OtherAccepted++; }
                if (o.phase == 3) { r.RecoverOffers++; if (acc) r.RecoverAccepted++; }
            }
            var st = l.Mind.Store;
            int Sum(uint key) { int at = st.Find(key, SocialVerb.SayAccept); return at >= 0 ? st.Entries[at].Stw + st.Entries[at].Ltw : 0; }
            r.LearnedS = Sum(Feature.NpcRef(sNpc.Id)); r.LearnedStranger = Sum(Feature.NpcRel((int)God.Sim.Cognition.NpcRelClass.Stranger));
            r.TrustS = SocialView.Trust(l.Mind, sNpc.Id);
            return r;
        }

        /// <summary>
        /// M5.6 / L14: after being cheated twice by stranger S, the learner accepts at most 20% of S's next 5 offers (baseline
        /// before the cheating at least 60%); other strangers' acceptance falls by at least 15 points but stays above S's; after
        /// 10 honest trades with other strangers, stranger acceptance is back above 50% within 5 days. Pooled over 20 seeds;
        /// learned entries and social trust are reported side by side.
        /// </summary>
        [Test, Explicit("M5 outstanding target: the learned avoidance is too slow and too general (see the M5 record)"), Category("M5-target")]
        public void M5_6_L14_AvoidanceAfterBeingCheated_AndRecovery()
        {
            var rows = Enumerable.Range(1, 20).AsParallel().AsOrdered().Select(sd => (sd, r: RunL14((ulong)sd, LearningMode.On))).ToList();
            foreach (var (sd, r) in rows)
                TestContext.WriteLine("seed " + sd + ": base " + r.BaseAccepted + "/" + r.BaseOffers + ", S after " + r.SAccepted + "/" + r.SOffers + ", others " + r.OtherAccepted + "/" + r.OtherOffers
                    + ", recover " + r.RecoverAccepted + "/" + r.RecoverOffers + " (honest trades " + r.HonestTrades + ", betrayals " + r.Betrayals + "); learned S " + r.LearnedS + ", stranger " + r.LearnedStranger + ", trust S " + r.TrustS);
            int b = Pct(rows.Sum(x => x.r.BaseAccepted), rows.Sum(x => x.r.BaseOffers)), sa = Pct(rows.Sum(x => x.r.SAccepted), rows.Sum(x => x.r.SOffers));
            int oa = Pct(rows.Sum(x => x.r.OtherAccepted), rows.Sum(x => x.r.OtherOffers)), ra = Pct(rows.Sum(x => x.r.RecoverAccepted), rows.Sum(x => x.r.RecoverOffers));
            TestContext.WriteLine("pooled: baseline " + b + "%, S's next five " + sa + "%, other strangers " + oa + "%, recovered " + ra + "%");
            Assert.GreaterOrEqual(b, 60, "baseline acceptance");
            Assert.LessOrEqual(sa, 20, "S's next offers");
            Assert.LessOrEqual(oa, b - 15, "other strangers fall by 15+ points");
            Assert.Greater(oa, sa, "but stay above S's");
            Assert.Greater(ra, 50, "recovered above 50%");
        }
    }
}
