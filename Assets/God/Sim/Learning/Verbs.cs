using God.Sim.Policy;

namespace God.Sim.Learning
{
    /// <summary>
    /// Verb ids of the learning store (NPC_LEARNING_DESIGN 3.2): a byte below <see cref="Max"/> (128 since F0,
    /// SHARED_CONTRACTS 12.4). The numbers are data and never reused. M3 uses the survival, gathering and equipment verbs;
    /// ranges: core 0-31, M4 32-47 (and 17, 18), M5 48-59 and Say(act) 64-95, lab 60-62, M6+ 96-127.
    /// </summary>
    public static class Verb
    {
        public const byte None = 0;
        // survival
        public const byte Eat = 1, Sleep = 2, Rest = 3, Flee = 4, Hide = 5, SeekShelter = 6, Approach = 7, Avoid = 8;
        // gathering and production
        public const byte Gather = 9, ForageAt = 10, Haul = 11, Build = 12, Craft = 13, WorkShift = 14;
        // equipment and goods
        public const byte PickUp = 15, Drop = 16, Store = 17, TakeFromStore = 18, Equip = 19;
        // searching (M3: walk out to look for things; the survival "search" of COMMUNICATION 4.3)
        public const byte Explore = 20;
        // controlled lab verbs (NPC_LEARNING L3): never offered on the island
        public const byte TestA = 60, TestB = 61, TestFiller = 62;
        public const int Max = 128;
        // ---- F1 (contracts 14): reserved rows 96-127. Civ 96-103 and 124-126, Orgs 104-115, Divinity 116-123; 127 reserved.
        //      Each package adds its rows only through its mind-hook partial (CivMindHooks / OrgMindHooks / DivinityMindHooks). ----
        public const byte CivFirst = 96, CivLast = 103, CivMilitaryFirst = 124, CivMilitaryLast = 126, OrgsFirst = 104, OrgsLast = 115, DivinityFirst = 116, DivinityLast = 123;
        public static bool IsCiv(byte v) => v >= CivFirst && v <= CivLast || v >= CivMilitaryFirst && v <= CivMilitaryLast;
        public static bool IsOrgs(byte v) => v >= OrgsFirst && v <= OrgsLast;
        public static bool IsDivinity(byte v) => v >= DivinityFirst && v <= DivinityLast;
    }

    /// <summary>The 16 verb groups of <see cref="MindView.AssocSummary"/> (the 10 domains, Survival, Barter and Cooperation split by sign).</summary>
    public enum VerbGroup : byte
    {
        SurvivalSeek = 0, SurvivalAvoid = 1, Gathering = 2, Production = 3, Equipment = 4, BarterGive = 5, BarterKeep = 6,
        CoopGive = 7, CoopTake = 8, Relationships = 9, Communication = 10, Employment = 11, Military = 12, Faction = 13, Search = 14, Lab = 15,
    }

    /// <summary>One row of the verb table: its name, group, the intent verb it becomes, and whether it opens an episode (4.7).</summary>
    public readonly struct VerbInfo
    {
        public readonly byte Id;
        public readonly string Name;
        public readonly VerbGroup Group;
        public readonly NpcVerb Intent;
        /// <summary>A long-delayed outcome may close its trace later (an episode, 4.7). M3: <see cref="Verb.Build"/> (the bed right arrives when the hut is finished); M3b: <see cref="Verb.ForageAt"/> and <see cref="Verb.PickUp"/> (the fate of a food lot: eaten or spoiled), <see cref="Verb.Equip"/> (what a tool adds to the work).</summary>
        public readonly bool Deferred;

        public VerbInfo(byte id, string name, VerbGroup group, NpcVerb intent, bool deferred = false)
        {
            Id = id; Name = name; Group = group; Intent = intent; Deferred = deferred;
        }
    }

    /// <summary>The verb table (data). New verbs are rows; the store never changes for them.</summary>
    public static class VerbTable
    {
        /// <summary>M3's rows (frozen after F0).</summary>
        public static readonly ReadOnlyArray<VerbInfo> Core = new ReadOnlyArray<VerbInfo>(
            new VerbInfo(Verb.Eat, "eat", VerbGroup.SurvivalSeek, NpcVerb.Eat),
            new VerbInfo(Verb.Sleep, "sleep", VerbGroup.SurvivalSeek, NpcVerb.Sleep),
            new VerbInfo(Verb.Rest, "rest", VerbGroup.SurvivalSeek, NpcVerb.Rest),
            new VerbInfo(Verb.Flee, "flee", VerbGroup.SurvivalAvoid, NpcVerb.Flee),
            new VerbInfo(Verb.SeekShelter, "seek shelter", VerbGroup.SurvivalSeek, NpcVerb.SeekShelter),
            new VerbInfo(Verb.Gather, "gather", VerbGroup.Gathering, NpcVerb.Gather),
            new VerbInfo(Verb.ForageAt, "forage", VerbGroup.Gathering, NpcVerb.ForageAt, deferred: true),
            new VerbInfo(Verb.Build, "build", VerbGroup.Production, NpcVerb.Build, deferred: true),
            new VerbInfo(Verb.PickUp, "pick up", VerbGroup.Equipment, NpcVerb.PickUp, deferred: true),
            new VerbInfo(Verb.Equip, "take up", VerbGroup.Equipment, NpcVerb.Equip, deferred: true),
            new VerbInfo(Verb.Explore, "explore", VerbGroup.Search, NpcVerb.Explore),
            new VerbInfo(Verb.TestA, "test A", VerbGroup.Lab, NpcVerb.LabAct),
            new VerbInfo(Verb.TestB, "test B", VerbGroup.Lab, NpcVerb.LabAct),
            new VerbInfo(Verb.TestFiller, "filler", VerbGroup.Lab, NpcVerb.LabAct));

        /// <summary>Every row: M3's, then M4's (<c>HouseholdHooks.Verbs</c>), then M5's (<c>SocialHooks.Verbs</c>) (F0.11), then (F1) Civ's, Orgs' and Divinity's.</summary>
        public static readonly ReadOnlyArray<VerbInfo> Rows = Concat(Core, Population.Households.HouseholdMindHooks.Verbs, Social.SocialMindHooks.Verbs,
            Civ.CivMindHooks.Verbs(), Orgs.OrgMindHooks.Verbs(), Divinity.DivinityMindHooks.Verbs());

        private static ReadOnlyArray<VerbInfo> Concat(params ReadOnlyArray<VerbInfo>[] parts)
        {
            var l = new System.Collections.Generic.List<VerbInfo>();
            foreach (var p in parts) foreach (var v in p) l.Add(v);
            return new ReadOnlyArray<VerbInfo>(l);
        }

        public static bool TryGet(byte verb, out VerbInfo info)
        {
            for (int i = 0; i < Rows.Count; i++) if (Rows[i].Id == verb) { info = Rows[i]; return true; }
            info = default;
            return false;
        }

        public static string Name(byte verb) => TryGet(verb, out var i) ? i.Name : "verb" + verb;
        public static bool IsDeferred(byte verb) => TryGet(verb, out var i) && i.Deferred;
        public static VerbGroup GroupOf(byte verb) => TryGet(verb, out var i) ? i.Group : VerbGroup.Lab;
        public static NpcVerb IntentOf(byte verb) => TryGet(verb, out var i) ? i.Intent : NpcVerb.None;
    }
}
