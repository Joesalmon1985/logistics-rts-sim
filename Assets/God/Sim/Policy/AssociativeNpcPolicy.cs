using System;
using God.Sim.Cognition;
using God.Sim.Learning;

namespace God.Sim.Policy
{
    /// <summary>
    /// The default civilian policy (COMMUNICATION_AND_COGNITION 4.7, NPC_LEARNING 4.2): for each candidate in build order,
    /// <c>score = Instinct + Learned + Social + Culture + Personality - Cost + Explore</c>; the winner takes all, a tie
    /// goes to the lowest index. Exactly one exploration draw is made per candidate, always, so turning a term off never
    /// shifts the stream. It holds no state: what the NPC has learned lives in its mind, read through the
    /// <see cref="MindView"/>, so swapping policies never erases it.
    /// </summary>
    public sealed class AssociativeNpcPolicy : INpcPolicy
    {
        public string Name => Population.Policies.Associative;
        public PolicyLevel Level => PolicyLevel.Civilian;

        public NpcIntent Decide(in NpcObservation obs, MindView mind, ref Rng rng)
        {
            var why = default(DecisionRecord);
            return DecideExplained(obs, mind, ref rng, ref why);
        }

        /// <summary>As <see cref="Decide"/>, also filling the "why" of the decision (terms of the winner, whether learning or exploration changed it).</summary>
        public NpcIntent DecideExplained(in NpcObservation obs, MindView mind, ref Rng rng, ref DecisionRecord why)
        {
            var cands = obs.Candidates;
            int n = cands.Count;
            why = default;
            why.Tick = (int)obs.Tick;
            why.Candidates = n;
            if (n == 0) return NpcIntent.Nothing;
            int amp = mind.ExploreAmplitude, novelty = mind.Novelty;
            int best = -1, second = -1, bestNoLearn = -1, bestNoExplore = -1;
            long bestScore = long.MinValue, secondScore = long.MinValue, bestNL = long.MinValue, bestNE = long.MinValue;
            CandidateScore bestTerms = default;
            int bestExplore = 0, maxLearned = int.MinValue;
            for (int i = 0; i < n; i++)
            {
                var t = mind.Terms(i);
                if (t.Learned > maxLearned) maxLearned = t.Learned;
                int explore = rng.NextInt(amp + 1) + (t.FamiliarTarget == 0 ? novelty : 0);
                long total = (long)t.Instinct + t.Learned + t.Social + t.Culture + t.Personality - t.Cost + explore;
                long noLearn = total - t.Learned, noExplore = total - explore;
                if (total > bestScore) { second = best; secondScore = bestScore; best = i; bestScore = total; bestTerms = t; bestExplore = explore; }
                else if (total > secondScore) { second = i; secondScore = total; }
                if (noLearn > bestNL) { bestNL = noLearn; bestNoLearn = i; }
                if (noExplore > bestNE) { bestNE = noExplore; bestNoExplore = i; }
            }
            var c = cands[best];
            why.Verb = c.Verb; why.TargetKind = c.Kind; why.TargetId = c.TargetId; why.TargetNode = c.TargetNode; why.TargetClass = c.Class;
            why.Instinct = bestTerms.Instinct; why.Learned = bestTerms.Learned; why.Social = bestTerms.Social; why.Culture = bestTerms.Culture;
            why.Personality = bestTerms.Personality; why.Cost = bestTerms.Cost; why.Explore = bestExplore;
            why.Total = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, bestScore));
            why.MaxLearned = maxLearned;
            why.LearnedChangedWinner = bestNoLearn != best;
            why.ExploreWon = bestNoExplore != best;
            if (second >= 0) { why.SecondVerb = cands[second].Verb; why.SecondTotal = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, secondScore)); }
            Span<uint> keys = stackalloc uint[3];
            Span<short> stw = stackalloc short[3];
            int k = mind.TopKeys(best, keys, stw);
            if (k > 0) { why.Key0 = keys[0]; why.Stw0 = stw[0]; }
            if (k > 1) { why.Key1 = keys[1]; why.Stw1 = stw[1]; }
            if (k > 2) { why.Key2 = keys[2]; why.Stw2 = stw[2]; }
            return ToIntent(c);
        }

        /// <summary>The intent for a candidate: node targets carry the node as their id; lots, huts and units their id; X is the node.</summary>
        public static NpcIntent ToIntent(in Candidate c) =>
            new NpcIntent(c.Intent, c.Kind, c.Kind == IntentTarget.Node ? c.TargetNode : c.TargetId, c.TargetNode, 0, c.Verb);
    }
}
