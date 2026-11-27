using System.Collections.Generic;

namespace God.Sim.Policy
{
    /// <summary>One record of the organisation as the decider may read it (layer 4; no truth field).</summary>
    public readonly struct InstitutionRecordView
    {
        public readonly int Id; public readonly long Tick; public readonly int Recorder; public readonly int Kind; public readonly int Ref; public readonly int Ware; public readonly int Count;
        public InstitutionRecordView(int id, long tick, int recorder, int kind, int reference, int ware, int count) { Id = id; Tick = tick; Recorder = recorder; Kind = kind; Ref = reference; Ware = ware; Count = count; }
    }

    /// <summary>A service request in the organisation's ring as written by its receiver (no true need).</summary>
    public readonly struct InstitutionRequestView
    {
        public readonly int Id; public readonly int Kind; public readonly int Requester; public readonly long Opened; public readonly int Status; public readonly int SeenNeed;
        public InstitutionRequestView(int id, int kind, int requester, long opened, int status, int seenNeed) { Id = id; Kind = kind; Requester = requester; Opened = opened; Status = status; SeenNeed = seenNeed; }
    }

    /// <summary>An office of the organisation as its record says (kind, powers, holder; 0 vacant).</summary>
    public readonly struct InstitutionOfficeView
    {
        public readonly int Index; public readonly int Kind; public readonly uint Powers; public readonly int Holder; public readonly bool IsHead;
        public InstitutionOfficeView(int index, int kind, uint powers, int holder, bool isHead) { Index = index; Kind = kind; Powers = powers; Holder = holder; IsHead = isHead; }
    }

    /// <summary>A policy record (key, value).</summary>
    public readonly struct InstitutionPolicyView
    {
        public readonly int Key; public readonly int Value;
        public InstitutionPolicyView(int key, int value) { Key = key; Value = value; }
    }

    /// <summary>
    /// What one office-holder reads when it decides as an organisation (contracts 9, 20.11; ORGANISATIONS 4.4): built ONLY from
    /// the organisation's own records (layer 4: its charter, offices, roster, policies, requests and records as written) and
    /// the decider's own mind (its <see cref="MindView"/> and its own trust in each member). Never the true state: the store
    /// counts are as last recorded (stale or wrong when the recorder missed something), never the bin. It holds no
    /// <see cref="GodState"/> reference (I-D5-1, ORG-INV-4). Keyed by <see cref="OrgId"/> (C6 rewrote the placeholder); C10 adds
    /// fields additively on request (R28). Lists are owned by the institution director and valid only during <c>Decide</c>.
    /// </summary>
    public readonly struct InstitutionView
    {
        public readonly int OrgId;
        public readonly int DeciderNpcId;
        public readonly long Tick;
        /// <summary>The decider's office index, its kind (<c>OfficeKind</c>) and powers (<c>OrgPower</c> bits).</summary>
        public readonly int Office, OfficeKind;
        public readonly uint Powers;
        /// <summary>The routine and the policy decision rules (<c>DecisionKind</c>).</summary>
        public readonly int RoutineRule, PolicyRule;
        public readonly IReadOnlyList<InstitutionRecordView> Records;
        public readonly IReadOnlyList<InstitutionRequestView> Requests;
        public readonly IReadOnlyList<InstitutionOfficeView> Offices;
        public readonly IReadOnlyList<InstitutionPolicyView> Policies;
        /// <summary>The roster as recorded (NpcIds ascending) and the decider's own trust in each (its social memory; 0 unknown).</summary>
        public readonly IReadOnlyList<int> Members, MemberTrust;
        /// <summary>The stock as last recorded, ware-indexed (recorded deposits minus recorded withdrawals).</summary>
        public readonly IReadOnlyList<int> StoreRecorded;
        public readonly int OpenProposals;
        public readonly MindView Mind;

        public InstitutionView(int orgId, int decider, long tick, int office, int officeKind, uint powers, int routineRule, int policyRule,
            IReadOnlyList<InstitutionRecordView> records, IReadOnlyList<InstitutionRequestView> requests, IReadOnlyList<InstitutionOfficeView> offices,
            IReadOnlyList<InstitutionPolicyView> policies, IReadOnlyList<int> members, IReadOnlyList<int> memberTrust, IReadOnlyList<int> storeRecorded,
            int openProposals, MindView mind)
        {
            OrgId = orgId; DeciderNpcId = decider; Tick = tick; Office = office; OfficeKind = officeKind; Powers = powers; RoutineRule = routineRule; PolicyRule = policyRule;
            Records = records; Requests = requests; Offices = offices; Policies = policies; Members = members; MemberTrust = memberTrust; StoreRecorded = storeRecorded;
            OpenProposals = openProposals; Mind = mind;
        }

        public int PolicyValue(int key, int fallback = 0)
        {
            if (Policies != null) foreach (var p in Policies) if (p.Key == key) return p.Value;
            return fallback;
        }
    }

    /// <summary>An enemy soldier a soldier sees: id, position (mm), squared distance and distance (mm, rounded down).</summary>
    public readonly struct SeenUnit
    {
        public readonly int Id; public readonly int X, Z; public readonly long Dist2; public readonly int Edge;
        public SeenUnit(int id, int x, int z, long dist2, int edge) { Id = id; X = x; Z = z; Dist2 = dist2; Edge = edge; }
    }

    /// <summary>An enemy building a soldier sees: id, position (mm) and the distance to its edge (mm).</summary>
    public readonly struct SeenBuilding
    {
        public readonly int Id; public readonly int X, Z; public readonly int Edge;
        public SeenBuilding(int id, int x, int z, int edge) { Id = id; X = x; Z = z; Edge = edge; }
    }

    /// <summary>
    /// What one soldier on duty perceives now (MILITARY_DESIGN "IMilitaryPolicy"), built by the military system from the
    /// unit's own sight: the enemies in sight (units at war with it, within its sight and, for a guard, its chase area),
    /// in the sim's unit and building order, plus its own orders (guard post, its faction's marked targets) and kit.
    /// Stage 3 soldiers are <c>MilUnit</c>s, not NPCs, so the NPC parts (morale, rations, pay) wait for M9.
    /// </summary>
    public readonly struct SoldierObservation
    {
        public readonly int UnitId;
        public readonly Faction Faction;
        public readonly int X, Z;
        public readonly long Tick;
        /// <summary>The kit's reach and sight (mm), and whether it shoots.</summary>
        public readonly int RangeMm, SightMm;
        public readonly bool Ranged;
        /// <summary>True when it guards a defend flag (its post).</summary>
        public readonly bool OnGuard;
        /// <summary>Enemy units in sight, in the sim's unit order.</summary>
        public readonly IReadOnlyList<SeenUnit> Enemies;
        /// <summary>Enemy buildings in sight, in the sim's building order.</summary>
        public readonly IReadOnlyList<SeenBuilding> EnemyBuildings;
        /// <summary>The ids its faction has marked as targets, in marking order (an order known to every soldier of the faction).</summary>
        public readonly IReadOnlyList<int> Marked;
        /// <summary>Where the first marked target stands (mm), when it still stands; <see cref="HasMarchTarget"/> false otherwise.</summary>
        public readonly bool HasMarchTarget;
        public readonly int MarchX, MarchZ;

        public SoldierObservation(int unitId, Faction faction, int x, int z, long tick, int rangeMm, int sightMm, bool ranged, bool onGuard,
            IReadOnlyList<SeenUnit> enemies, IReadOnlyList<SeenBuilding> enemyBuildings, IReadOnlyList<int> marked,
            bool hasMarchTarget, int marchX, int marchZ)
        {
            UnitId = unitId; Faction = faction; X = x; Z = z; Tick = tick; RangeMm = rangeMm; SightMm = sightMm; Ranged = ranged; OnGuard = onGuard;
            Enemies = enemies; EnemyBuildings = enemyBuildings; Marked = marked; HasMarchTarget = hasMarchTarget; MarchX = marchX; MarchZ = marchZ;
        }
    }
}
