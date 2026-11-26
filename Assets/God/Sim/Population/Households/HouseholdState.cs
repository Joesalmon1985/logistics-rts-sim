using System.Collections.Generic;
using God.Sim.Population.Genetics;
using Newtonsoft.Json;

namespace God.Sim.Population.Households
{
    /// <summary>
    /// A household (HOUSEHOLDS 3): people who sleep in one hut and share a store. Members sorted, at most 12; the store is
    /// one <see cref="LotBin"/> of kind <see cref="BinKind.HouseholdStore"/> while it has a hut (SHARED_CONTRACTS 5).
    /// </summary>
    public sealed class Household
    {
        public int Id;
        public Faction Faction;
        /// <summary>The hut it lives in (building id), 0 for none.</summary>
        public int Hut;
        /// <summary>Its store's bin id, 0 for none.</summary>
        public int Bin;
        public long FormedTick;
        public List<int> Members = new List<int>();
        /// <summary>The pooling norm (HOUSEHOLDS "Norms"): 0 adults only, 1 all members, 2 contributors only. Default 1.</summary>
        public byte StoreAccess = 1;
        public const int MaxMembers = 12;
    }

    /// <summary>A partnership (SHARED_CONTRACTS 12.3: keyed by its own id, so a pair may partner twice and concurrency is possible, Q5).</summary>
    public sealed class Partnership
    {
        public int Id, A, B;
        public long StartTick;
        /// <summary>Who proposed (the other accepted).</summary>
        public int Initiator;
    }

    /// <summary>
    /// An informal shared-childcare arrangement between households (Q15): the asker's household's children may be minded and
    /// fed by the carer, and the asker returns turns. Each side's record of care given and returned (ticks); nothing enforces it.
    /// </summary>
    public sealed class CareArrangement
    {
        public int Id;
        public int Asker, Carer;
        public int AskerHousehold, CarerHousehold;
        public long AgreedTick;
        /// <summary>Care ticks the carer gave the asker's household's children, and the asker gave the carer's household's children.</summary>
        public int Given, Returned;
    }

    /// <summary>Counters of the life cycle (tests and reports; reconciled with the events).</summary>
    public sealed class LifeStats
    {
        public int Births, Immigrants, Emigrants, Conceptions, Miscarriages, Partnerships, PartnershipsEnded, Fostered, Orphaned;
        public int Infections, Estates, EstatesSettled;
    }

    /// <summary>
    /// The household module's state (SHARED_CONTRACTS 9.3): public fields, ascending-id lists, no dictionaries. OWNER: M4. Its
    /// hash part ("household") is appended only while <c>GodState.Households</c> is non-null.
    /// </summary>
    public sealed class HouseholdState
    {
        public int NextHouseholdId = 1, NextPartnershipId = 1, NextCareId = 1;
        public List<Household> Households = new List<Household>();
        public List<Partnership> Partnerships = new List<Partnership>();
        public List<CareArrangement> Care = new List<CareArrangement>();
        /// <summary>Open estates, in order of death tick then deceased id (MATERIAL 2.6).</summary>
        public List<Estate.EstateRecord> Estates = new List<Estate.EstateRecord>();
        /// <summary>Per hex: how fouled it is (0..FoulMax), the sanitation term of disease.</summary>
        public int[] Fouling;
        public LifeStats Stats = new LifeStats();
        /// <summary>The last day the households' daily work ran.</summary>
        public long DayDone = -1;
        /// <summary>The last day immigration was evaluated.</summary>
        public long ImmigrationDay = -1;
        /// <summary>Immigrants per faction this year (the 15% cap), and the year counted.</summary>
        public int[] ImmigrantsThisYear = new int[FactionInfo.Max];
        public long ImmigrationYear = -1;
        /// <summary>True once the founders' genomes, households and partnerships were announced as events (the first life-cycle tick).</summary>
        public bool Announced;

        /// <summary>True while M4 itself creates an NPC (a birth or an immigrant), so <c>NewMind</c> gives it M4's parts. A cache, not state.</summary>
        [JsonIgnore] public bool Creating;
        /// <summary>The walker's path search (a cache built from the grid).</summary>
        [JsonIgnore] public NodePather Pather;

        public Household Find(int id)
        {
            int lo = 0, hi = Households.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int v = Households[mid].Id;
                if (v == id) return Households[mid];
                if (v < id) lo = mid + 1; else hi = mid - 1;
            }
            return null;
        }

        public Partnership PartnershipById(int id) { foreach (var p in Partnerships) if (p.Id == id) return p; return null; }
        public CareArrangement CareById(int id) { foreach (var c in Care) if (c.Id == id) return c; return null; }

        public void AddTo(ref StateHasher h)
        {
            h.Add(NextHouseholdId); h.Add(NextPartnershipId); h.Add(NextCareId);
            h.Add(Households.Count);
            foreach (var x in Households)
            {
                h.Add(x.Id); h.Add((int)x.Faction); h.Add(x.Hut); h.Add(x.Bin); h.Add(x.FormedTick); h.Add(x.StoreAccess);
                h.Add(x.Members.Count); foreach (int m in x.Members) h.Add(m);
            }
            h.Add(Partnerships.Count);
            foreach (var p in Partnerships) { h.Add(p.Id); h.Add(p.A); h.Add(p.B); h.Add(p.StartTick); h.Add(p.Initiator); }
            h.Add(Care.Count);
            foreach (var c in Care) { h.Add(c.Id); h.Add(c.Asker); h.Add(c.Carer); h.Add(c.AskerHousehold); h.Add(c.CarerHousehold); h.Add(c.AgreedTick); h.Add(c.Given); h.Add(c.Returned); }
            h.Add(Estates.Count);
            foreach (var e in Estates) e.AddTo(ref h);
            if (Fouling != null) foreach (int f in Fouling) h.Add(f);
            var st = Stats;
            h.Add(st.Births); h.Add(st.Immigrants); h.Add(st.Emigrants); h.Add(st.Conceptions); h.Add(st.Miscarriages); h.Add(st.Partnerships);
            h.Add(st.PartnershipsEnded); h.Add(st.Fostered); h.Add(st.Orphaned); h.Add(st.Infections); h.Add(st.Estates); h.Add(st.EstatesSettled);
            h.Add(DayDone); h.Add(ImmigrationDay); h.Add(ImmigrationYear); h.Add(Announced ? 1 : 0); foreach (int n in ImmigrantsThisYear) h.Add(n);
        }
    }

    /// <summary>What a person shows that others can see (SHARED_CONTRACTS 7.2): displays, never messages, never misread.</summary>
    public static class DisplayKind
    {
        public const byte None = 0, Beg = 1, Propose = 2, AskCare = 3, Trying = 4, Sick = 5, NeedsHome = 6, CarePartner = 7, Partnered = 8;
    }

    /// <summary>
    /// M4's part of one person (POPULATION 3, BIOLOGICAL 3): sex, parents (the truth: never read by a mind), genome and
    /// phenotype, growth, pregnancy, illness, today's care counters and the display it shows. Null for an NPC M4 did not make
    /// (an M3 fixture NPC stays an ageless adult), so M3 games and fixtures hash as before.
    /// </summary>
    public sealed class NpcLife
    {
        /// <summary>0 female, 1 male. Read only by reproduction (conception) and the phenotype builder (G4: SameSex_EqualCapabilities).</summary>
        public byte Sex;
        public int Mother, BioFather;
        public int Generation;
        public byte[] Genome;
        public Phenotype Pheno;
        /// <summary>The developmental noise of each trait (drawn once at birth from "develop").</summary>
        public int[] Noise;
        public int MaxAgeMilli;
        public int Growth;
        /// <summary>Childhood nutrition: the sum of day-end nutrition (permille) and the days counted, for the adult phenotype.</summary>
        public long ChildNutSum;
        public int ChildNutDays;
        public bool Matured;
        public long LastBirthTick = -1;
        /// <summary>A pregnancy: the biological father (0 none), its start and due ticks.</summary>
        public int PregFather;
        public long PregStart, PregDue;
        /// <summary>"Try for a child" held until this tick, with this partner.</summary>
        public long TryingUntil;
        public int TryWith;
        /// <summary>0 susceptible, 1 infected, 2 recovered (immune until <see cref="ImmuneUntilDay"/>).</summary>
        public byte Illness;
        public int IllDaysLeft, IllDays, ImmuneUntilDay, Severity;
        /// <summary>The last day it was tended.</summary>
        public int TendedDay = -1;
        /// <summary>Checks today and those with no adult within reach (children's protection).</summary>
        public int AttendChecks, AloneChecks;
        /// <summary>Yesterday's share of checks with no adult near (permille).</summary>
        public int AloneShare;
        public int CareGiven, CareReceived;
        public byte Display, DisplayLevel;
        public int DisplayTarget;
        public long DisplayUntil;
        /// <summary>Work competence accumulator (permille per work tick; a work tick counts once 1000 are in).</summary>
        public int WorkAcc;
        /// <summary>The day it became an orphan (-1 none), and whether it was told it is fostered.</summary>
        public int OrphanSince = -1;
        public bool Fostered, ToldFostered;
        /// <summary>Who has fed it on how many distinct days (the raising cue): the main carer and its count, and the last day counted.</summary>
        public int CueCarer, CueDays, CueDay = -1;
        public int LastDaily = -1;
        /// <summary>Foraged and gathered work ticks yesterday (the work hazard).</summary>
        public int WorkTicks;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Sex); h.Add(Mother); h.Add(BioFather); h.Add(Generation); h.Add(GenomeOps.Hash(Genome));
            var p = Pheno;
            h.Add(p.Strength); h.Add(p.Endurance); h.Add(p.Size); h.Add(p.Health); h.Add(p.Fertility); h.Add(p.Lifespan); h.Add(p.Perception);
            h.Add(p.Features); h.Add(p.Complexion); h.Add(p.Build); h.Add(p.HungerGain); h.Add(p.FearGain); h.Add(p.SocialGain); h.Add(p.NurtureGain);
            h.Add(p.LearnRate); h.Add(p.ForgetRate); h.Add(p.Curiosity);
            if (Noise != null) foreach (int n in Noise) h.Add(n);
            h.Add(MaxAgeMilli); h.Add(Growth); h.Add(ChildNutSum); h.Add(ChildNutDays); h.Add(Matured ? 1 : 0); h.Add(LastBirthTick);
            h.Add(PregFather); h.Add(PregStart); h.Add(PregDue); h.Add(TryingUntil); h.Add(TryWith);
            h.Add(Illness); h.Add(IllDaysLeft); h.Add(IllDays); h.Add(ImmuneUntilDay); h.Add(Severity); h.Add(TendedDay);
            h.Add(AttendChecks); h.Add(AloneChecks); h.Add(AloneShare); h.Add(CareGiven); h.Add(CareReceived);
            h.Add(Display | (DisplayLevel << 8)); h.Add(DisplayTarget); h.Add(DisplayUntil); h.Add(WorkAcc);
            h.Add(OrphanSince); h.Add((Fostered ? 1 : 0) | (ToldFostered ? 2 : 0)); h.Add(CueCarer); h.Add(CueDays); h.Add(CueDay); h.Add(LastDaily); h.Add(WorkTicks);
        }
    }
}
