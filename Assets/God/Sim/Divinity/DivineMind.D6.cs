using Newtonsoft.Json;

namespace God.Sim.Divinity
{
    /// <summary>One held rule that weighed a candidate at a think (the chosen one becomes a <c>CommandmentChoiceMade</c>).</summary>
    public struct CommandmentMatch
    {
        public int Commandment;
        public Modality Modality;
        public byte Verb;
        public Policy.IntentTarget Kind;
        public int TargetId, TargetNode;
        public int Term;
    }

    /// <summary>
    /// Contracts 13.1 (D6): one commandment as this person knows it. <see cref="Interpretation"/> is what it believes the
    /// first rule says (possibly misunderstood), <see cref="Interpretation2"/> the second (<see cref="RuleCount"/> 1 or 2;
    /// <c>CommandmentRules.MaxRules</c> = 2). <see cref="Importance"/> as it was told. <see cref="ToldRevoked"/>: it was told the
    /// god revoked it. <see cref="VariantMask"/> is analytics truth (which fields differ from the revision it was told),
    /// never read by a rule. OWNER: D6.
    /// </summary>
    public partial struct KnownCommandment
    {
        public int Commandment, Revision;
        public CommandmentSource Source;
        public int FromNpc;
        /// <summary>-1000 rejected .. 1000 adopted.</summary>
        public short Adoption;
        /// <summary>0 .. 1000; 0 is forgotten.</summary>
        public short Salience;
        public CommandmentRule Interpretation;
        public CommandmentRule Interpretation2;
        public byte RuleCount;
        public byte Importance;
        public int God;
        public bool ToldRevoked;
        public long LearnedTick;
        /// <summary>The stance band last reported (<see cref="CommandmentStance"/>), for <c>CommandmentStanceChanged</c>.</summary>
        public CommandmentStance Stance;
        public byte VariantMask;

        public CommandmentRule Rule(int i) => i == 0 ? Interpretation : Interpretation2;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Commandment); h.Add(Revision); h.Add((int)Source | (RuleCount << 8) | (Importance << 16) | ((int)Stance << 24)); h.Add(FromNpc);
            h.Add(Adoption); h.Add(Salience); h.Add(God); h.Add(ToldRevoked ? 1 : 0); h.Add(LearnedTick); h.Add(VariantMask);
            Interpretation.AddTo(ref h);
            if (RuleCount > 1) Interpretation2.AddTo(ref h);
        }
    }

    /// <summary>COMMANDMENTS 4.5: the knowledge states (bands of salience and adoption).</summary>
    public enum CommandmentStance : byte { None = 0, Heard = 1, Learned = 2, Adopted = 3, Rejected = 4 }

    /// <summary>
    /// D6's part of <see cref="DivineMind"/>: the known commandments (F1's <c>Known</c>, cap 8, eviction lowest salience then
    /// lowest id), what the person knows of itself that its rules ask (its community, its affiliated gods) and when it last
    /// told someone. OWNER: D6.
    /// </summary>
    public sealed partial class DivineMind
    {
        /// <summary>Its community of origin as it knows it (faction index + 1; 0 unknown).</summary>
        public int Community;
        /// <summary>Bit (god - 1): the gods of the religions it is affiliated with (it knows its own affiliations).</summary>
        public int AffiliatedGods;
        public long LastTellTick = -1_000_000;
        public int LastTold;
        /// <summary>Tier 2: days in a row the strongest god was not among its affiliated gods (conversion, COMMANDMENTS 4.6).</summary>
        public int ConvertDays;
        /// <summary>Tier 2: the founder seed has been applied (once).</summary>
        public bool Seeded;

        /// <summary>The game's doctrine tuning (bound at creation and after a load; not state).</summary>
        [JsonIgnore] public DoctrineTuning DTuning;

        public const int MatchCap = 6;
        /// <summary>The matches of the last think (saved, consumed the same tick by the doctrine system; not in the mind hash).</summary>
        public long MatchTick = -1;
        public int MatchCount;
        public CommandmentMatch[] Matches;

        partial void HashKnownD6(ref StateHasher h)
        {
            for (int i = 0; i < KnownCount; i++) Known[i].AddTo(ref h);
            // only when any differs from its default, so a mind D2 created (no D6 state) hashes as before
            if (Community == 0 && AffiliatedGods == 0 && LastTellTick == -1_000_000 && LastTold == 0 && ConvertDays == 0 && !Seeded) return;
            h.Add(Community); h.Add(AffiliatedGods); h.Add(LastTellTick); h.Add(LastTold); h.Add(ConvertDays); h.Add(Seeded ? 1 : 0);
        }

        public int IndexOfKnown(int commandment)
        {
            for (int i = 0; i < KnownCount; i++) if (Known[i].Commandment == commandment) return i;
            return -1;
        }

        /// <summary>
        /// Adds a known entry (cap 8: when full the entry with the lowest salience, then the lowest id, is evicted and returned
        /// in <paramref name="evicted"/>; it is forgotten). Returns the index of the new entry.
        /// </summary>
        public int AddKnown(in KnownCommandment k, out KnownCommandment evicted)
        {
            evicted = default;
            if (Known == null) Known = new KnownCommandment[KnownCap];
            if (KnownCount >= KnownCap)
            {
                int low = 0;
                for (int i = 1; i < KnownCount; i++)
                    if (Known[i].Salience < Known[low].Salience || Known[i].Salience == Known[low].Salience && Known[i].Commandment < Known[low].Commandment) low = i;
                evicted = Known[low];
                RemoveKnownAt(low);
            }
            Known[KnownCount] = k;
            return KnownCount++;
        }

        public void RemoveKnownAt(int i)
        {
            for (int j = i + 1; j < KnownCount; j++) Known[j - 1] = Known[j];
            KnownCount--;
            Known[KnownCount] = default;
        }

        /// <summary>This mind's belief entry about a god (-1 none).</summary>
        public int IndexOfGod(int god)
        {
            for (int i = 0; i < GodCount; i++) if (Gods[i].God == god) return i;
            return -1;
        }
    }
}
