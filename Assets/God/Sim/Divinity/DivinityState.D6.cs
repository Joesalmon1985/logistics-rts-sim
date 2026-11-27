using System.Collections.Generic;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D6's part of <see cref="DivinityState"/> (F1.2: created empty by F1; OWNER: D6). Its sub-object types are declared below; the container hashes them in the fixed order.
    /// </summary>
    public sealed partial class DivinityState
    {
    }

    /// <summary>
    /// D6's sub-object, <c>DivinityState.Doctrine</c> (contracts 3, 13.2; COMMANDMENTS 3): the gods' commandments (ascending
    /// id, with their revision history), religions (ascending id) and affiliation spells (in opening order), and the counters.
    /// Created on the first reveal (or when Tier 2 founds religions): absent before, so an idle game's <c>divine</c> part is
    /// unchanged (pin P2). OWNER: D6.
    /// </summary>
    public sealed partial class DoctrineState
    {
        public int NextCommandmentId = 1;
        public int NextReligionId = 1;
        public List<Commandment> Commandments = new List<Commandment>();
        public List<Religion> Religions = new List<Religion>();
        public List<Affiliation> Affiliations = new List<Affiliation>();
        /// <summary>Tier 2: the founding religions exist (once).</summary>
        public bool Founded;
        /// <summary>Telemetry counters (hashed): learned, forgotten, told, stance changes.</summary>
        public int Learned, Forgotten, Told, StanceChanges;

        public Commandment Find(int id)
        {
            int lo = 0, hi = Commandments.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = Commandments[mid].Id;
                if (v == id) return Commandments[mid];
                if (v < id) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        public Religion FindReligion(int id)
        {
            foreach (var r in Religions) if (r.Id == id) return r;
            return null;
        }

        partial void Hash(ref StateHasher h)
        {
            h.Add(NextCommandmentId); h.Add(NextReligionId); h.Add(Founded ? 1 : 0);
            h.Add(Learned); h.Add(Forgotten); h.Add(Told); h.Add(StanceChanges);
            h.Add(Commandments.Count);
            foreach (var c in Commandments) c.AddTo(ref h);
            h.Add(Religions.Count);
            foreach (var r in Religions) r.AddTo(ref h);
            h.Add(Affiliations.Count);
            foreach (var a in Affiliations) a.AddTo(ref h);
        }
    }

    /// <summary>
    /// Contracts 13.2: a god's commandment (structured rules, never text). <see cref="Recipients"/> is the target of the
    /// revelation, re-resolved at a revision or revocation (the god speaks again to the same people or place; everyone else
    /// learns of it from people). OWNER: D6.
    /// </summary>
    public sealed class Commandment
    {
        public int Id, God, Revision;
        public int Template;
        public CommandmentRule[] Rules;
        public int Importance;
        public long IntroducedTick;
        public bool Revoked;
        public DivineTarget Recipients;
        /// <summary>The intervention of the revelation (and of the latest revision or revocation).</summary>
        public int Intervention, LastIntervention;
        public List<CommandmentRevision> History = new List<CommandmentRevision>();

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add(God); h.Add(Revision); h.Add(Template); h.Add(Importance); h.Add(IntroducedTick); h.Add(Revoked ? 1 : 0);
            Recipients.AddTo(ref h); h.Add(Intervention); h.Add(LastIntervention);
            h.Add(Rules?.Length ?? 0);
            if (Rules != null) foreach (var r in Rules) r.AddTo(ref h);
            h.Add(History.Count);
            foreach (var v in History) v.AddTo(ref h);
        }
    }

    /// <summary>One superseded revision (its rules and importance) or the revocation (<see cref="Revoked"/>), with the tick it ended.</summary>
    public sealed class CommandmentRevision
    {
        public int Revision;
        public CommandmentRule[] Rules;
        public int Importance;
        public long EndedTick;
        public bool Revoked;
        public int Intervention;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Revision); h.Add(Importance); h.Add(EndedTick); h.Add(Revoked ? 1 : 0); h.Add(Intervention);
            h.Add(Rules?.Length ?? 0);
            if (Rules != null) foreach (var r in Rules) r.AddTo(ref h);
        }
    }

    /// <summary>COMMANDMENTS 3: a religion (a tradition: neither a god nor an organisation). Founding religions: one per god, empty canon.</summary>
    public sealed class Religion
    {
        public int Id;
        public int[] Gods;
        public int[] Canon = new int[0];
        public int ParentReligion;
        public long FoundedTick;

        public bool Worships(int god)
        {
            if (Gods != null) foreach (var g in Gods) if (g == god) return true;
            return false;
        }

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add(ParentReligion); h.Add(FoundedTick);
            h.Add(Gods?.Length ?? 0); if (Gods != null) foreach (var g in Gods) h.Add(g);
            h.Add(Canon?.Length ?? 0); if (Canon != null) foreach (var c in Canon) h.Add(c);
        }
    }

    /// <summary>
    /// Contracts 3: one religious affiliation spell (several per person allowed). Never stands in for citizenship (C5), the
    /// community of origin (<c>Npc.Faction</c>) or belief. <see cref="To"/> -1 while open.
    /// </summary>
    public struct Affiliation
    {
        public int Npc, Religion, Org;
        public long From, To;

        public bool Open => To < 0;

        public void AddTo(ref StateHasher h) { h.Add(Npc); h.Add(Religion); h.Add(Org); h.Add(From); h.Add(To); }
    }
}
