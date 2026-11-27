using God.Sim.Policy;

namespace God.Sim.Social
{
    /// <summary>
    /// A benchmark counterpart for controlled social scenarios (STRATEGY_BENCHMARKS: <c>bench.*</c> policies; ACCEPTANCE
    /// M5.5-M5.9, L14, L16, LS2): it reads only its observation (the candidates its own mind built) and picks by fixed rules,
    /// so a test controls the consequences a learner meets without scripting the learner. It never reads true state.
    /// <list type="bullet">
    /// <item>honest trader: accepts offers, delivers its side, makes offers when <see cref="Offers"/>;</item>
    /// <item>take-and-leave (<see cref="Delivers"/> false): agrees, takes what it is given, never hands over its side;</item>
    /// <item>giver (<see cref="AnswersNeeds"/>): answers a request for food by giving; an ignorer answers nothing.</item>
    /// </list>
    /// Otherwise it rests where it is.
    /// </summary>
    public sealed class BenchSocialPolicy : INpcPolicy
    {
        public string Name { get; }
        public PolicyLevel Level => PolicyLevel.Civilian;

        public bool Offers = true, Accepts = true, Delivers = true, AnswersNeeds = false, Refuses = false;
        /// <summary>Only deal with this person (its handle, the true id it sees); 0 anyone.</summary>
        public int Only;
        /// <summary>The test may pause its offers (a phase of a scenario); its other rules stay.</summary>
        public bool Active = true;

        public BenchSocialPolicy(string name) { Name = name; }

        public NpcIntent Decide(in NpcObservation obs, MindView mind, ref Rng rng)
        {
            var cands = obs.Candidates;
            int pick = -1, best = 0, rest = -1;
            for (int i = 0; i < cands.Count; i++)
            {
                var c = cands[i];
                if (c.Verb == Learning.Verb.Rest) { rest = i; continue; }
                // Only binds answers and hand-overs; an offer to anyone watching faces whoever is nearest
                if (Only != 0 && c.Kind == IntentTarget.Npc && c.TargetId != Only && c.Verb != SocialVerb.SayOffer) continue;
                int pri = 0;
                switch (c.Verb)
                {
                    case SocialVerb.HandOver: pri = Delivers ? 6 : 0; break;
                    case SocialVerb.SayAccept: pri = Accepts ? 5 : 0; break;
                    case SocialVerb.Give: pri = AnswersNeeds ? 5 : 0; break;
                    case SocialVerb.SayRefuse: pri = Refuses || !Accepts ? 3 : 0; break;
                    case SocialVerb.SayOffer: pri = Offers && Active ? 2 : 0; break;
                }
                if (pri > best) { best = pri; pick = i; }
            }
            if (pick < 0) pick = rest;
            return pick < 0 ? NpcIntent.Nothing : AssociativeNpcPolicy.ToIntent(cands[pick]);
        }
    }
}
