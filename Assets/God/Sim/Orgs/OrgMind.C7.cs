using Newtonsoft.Json;

namespace God.Sim.Orgs
{
    /// <summary>
    /// C7's part of a person's organisation mind (OWNER: C7): whom it believes to be healers, its own cursor over heard
    /// messages, its asks, the treatments it gave, and teaching offers it heard or made. Null until something of C7's enters
    /// the mind (the start culture, being treated, a request or an offer heard), so a mind without it hashes as before.
    /// </summary>
    public sealed class HealthMind
    {
        public const int HealerCap = 3;
        /// <summary>Believed healers (as it recognises them), most recent last.</summary>
        public int H0, H1, H2;
        /// <summary>It knows the healing practice (it may treat).</summary>
        public bool Healer;
        /// <summary>The highest message id it has read for C7's purposes.</summary>
        public int LastMsg;
        /// <summary>The tick it last asked for help.</summary>
        public int LastAsk;
        /// <summary>Treatments it gave (as it remembers them) outside any practice, and in all.</summary>
        public int GivenInformal, Given;
        /// <summary>The practice it runs (0 none), as it believes.</summary>
        public int Practice;

        // ---- education ----
        /// <summary>A teaching offer it heard (learner side): who offers, the topic, when; and whether it is attending.</summary>
        public int OfferFrom, OfferTopic, OfferTick;
        public bool Attending;
        /// <summary>Its last offer (teacher side): to whom, when.</summary>
        public int OfferedTo, OfferedTick;
        /// <summary>Topics it can teach (bit per topic id), and its own cursor over heard teaching gestures.</summary>
        public int TeachTopics, LastTeachMsg;
        /// <summary>It believes a lesson it offered is still open (it resumes it after a distraction).</summary>
        public bool Teaching;

        public int HealerAt(int i) => i == 0 ? H0 : i == 1 ? H1 : H2;
        public bool Believes(int healer) => healer != 0 && (H0 == healer || H1 == healer || H2 == healer);

        public void AddHealer(int healer)
        {
            if (healer == 0 || Believes(healer)) return;
            if (H0 == 0) H0 = healer; else if (H1 == 0) H1 = healer; else if (H2 == 0) H2 = healer;
            else { H0 = H1; H1 = H2; H2 = healer; }
        }

        public void AddTo(ref StateHasher h)
        {
            h.Add(H0); h.Add(H1); h.Add(H2); h.Add(Healer ? 1 : 0); h.Add(LastMsg); h.Add(LastAsk); h.Add(GivenInformal); h.Add(Given); h.Add(Practice);
            h.Add(OfferFrom); h.Add(OfferTopic); h.Add(OfferTick); h.Add(Attending ? 1 : 0); h.Add(OfferedTo); h.Add(OfferedTick); h.Add(TeachTopics); h.Add(LastTeachMsg); h.Add(Teaching ? 1 : 0);
        }
    }

    public sealed partial class OrgMind
    {
        /// <summary>C7's part (null until C7 writes it).</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public HealthMind Health;
        [JsonIgnore] public OrgC7Tuning C7;

        public HealthMind HealthOrCreate() => Health ?? (Health = new HealthMind());

        partial void HashC7(ref StateHasher h)
        {
            if (Health == null) return;
            h.Add(7);
            Health.AddTo(ref h);
        }
    }
}
