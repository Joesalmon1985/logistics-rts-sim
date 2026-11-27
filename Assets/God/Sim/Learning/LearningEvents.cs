namespace God.Sim.Learning
{
    /// <summary>
    /// One association changed (NPC_LEARNING 7): sampled, every update with |delta| at least 1000 and every update on the
    /// NPC-days with <c>hash(npc, day) % SampleEvery == 0</c>. The sampling uses a hash, never a sim random stream.
    /// </summary>
    public sealed class AssociationUpdated : DomainEvent
    {
        public int Npc;
        public uint Key;
        public byte Verb;
        public int Delta;
        public short NewStw, NewLtw;
        public Origin Origin;
    }

    /// <summary>A conjunction was recruited at sleep.</summary>
    public sealed class ConjunctionRecruited : DomainEvent
    {
        public int Npc;
        public int Slot;
        public uint A, B, C;
        public int Strength;
    }

    /// <summary>A conjunction slot was freed (its strength ran out, or it was replaced).</summary>
    public sealed class ConjunctionFreed : DomainEvent
    {
        public int Npc;
        public int Slot;
        public uint A, B, C;
    }

    /// <summary>A long-delayed outcome closed an episode (4.7).</summary>
    public sealed class EpisodeClosed : DomainEvent
    {
        public int Npc;
        public int EpisodeKey;
        public byte Verb;
        public int Return;
        public int DaysElapsed;
    }

    /// <summary>
    /// One chosen intent (NPC_LEARNING 7, the decision trace; warehouse <c>fact_decision</c>): the verb and target chosen and
    /// the score it won with, split into its terms (Total = Instinct + Learned + Social + Culture + Personality - Cost +
    /// Explore; Need is the drive-gated part of Instinct). Emitted only when the host asks for it
    /// (<see cref="EventBuffer.TraceDecisions"/>, set by Full recording of the learning domain), so it costs nothing otherwise
    /// and never changes the sim. <see cref="Continued"/>: the running intent was chosen again on the way to its target.
    /// </summary>
    public sealed class DecisionMade : DomainEvent
    {
        public int Npc;
        public byte Verb;
        public Policy.IntentTarget TargetKind;
        public int TargetId, TargetNode;
        public byte TargetClass;
        public int Total, Instinct, Need, Learned, Social, Culture, Personality, Cost, Explore;
        public int Candidates;
        public bool LearnedChangedWinner, ExploreWon, Continued;
        public byte SecondVerb;
        public int SecondTotal;
        /// <summary>v2 (R63, Checkpoint B): who drove the decision (contracts 15: 0 own, 1 compelled, 2 possessed, 3 avatar); warehouse <c>control_code</c>.</summary>
        public byte Control;
    }

    /// <summary>The learning sink of one NPC's think: turns the core's reports into events on the sim's buffer.</summary>
    public sealed class EventLearningSink : ILearningSink
    {
        private EventBuffer _ev;
        private int _npc;
        public bool Sampled { get; private set; }

        public void Bind(EventBuffer ev, int npc, bool sampled) { _ev = ev; _npc = npc; Sampled = sampled; }

        public void Updated(uint key, byte verb, int delta, short stw, short ltw, Origin origin) =>
            _ev?.Add(new AssociationUpdated { Npc = _npc, Key = key, Verb = verb, Delta = delta, NewStw = stw, NewLtw = ltw, Origin = origin });

        public void Recruited(int slot, uint a, uint b, uint c, int strength) =>
            _ev?.Add(new ConjunctionRecruited { Npc = _npc, Slot = slot, A = a, B = b, C = c, Strength = strength });

        public void Freed(int slot, uint a, uint b, uint c) => _ev?.Add(new ConjunctionFreed { Npc = _npc, Slot = slot, A = a, B = b, C = c });

        public void EpisodeClosed(int episodeKey, byte verb, int ret, int daysElapsed) =>
            _ev?.Add(new EpisodeClosed { Npc = _npc, EpisodeKey = episodeKey, Verb = verb, Return = ret, DaysElapsed = daysElapsed });

        /// <summary>The NPC-day sampling rule: a hash of (npc, day), never a random stream.</summary>
        public static bool IsSampledDay(int npc, long day, int every)
        {
            if (every <= 1) return true;
            unchecked
            {
                ulong h = 14695981039346656037UL;
                h = (h ^ (ulong)npc) * 1099511628211UL;
                h = (h ^ (ulong)day) * 1099511628211UL;
                h ^= h >> 29;
                return h % (ulong)every == 0;
            }
        }
    }
}
