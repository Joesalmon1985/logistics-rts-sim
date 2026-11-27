using System;
using System.Collections.Generic;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 8 (D3; MYTHIC 6): the miracles. A miracle is a <see cref="CastMiracle"/> whose <c>Apply</c> changes truth only
    /// through the owning modules' functions (<see cref="Bodies"/>, <see cref="Goods"/>, <see cref="PopulationMiracles"/>,
    /// <see cref="MilitaryRules"/>) and then reports it: one <see cref="MiracleCast"/> and one <see cref="MiracleEffectApplied"/>
    /// per affected thing with before and after. Common rules: preconditions are read from true state (a god's power, not an
    /// NPC's perception); <c>Validate</c> never mutates; <c>Apply</c> refuses before spending when the effect would change
    /// nothing (I-MY-6), then <see cref="Mythic.TrySpend"/>, <see cref="Interventions.Open"/>, the effect. Every created or
    /// destroyed unit is booked by <see cref="DivineLedger"/> (C7). There is no new death cause: a smiting death is
    /// <c>DeathCause.Violence</c> through <c>PopulationModule.Kill</c>, attributed in <see cref="MiracleEffectApplied"/> (contracts 20.4).
    /// OWNER: D3.
    /// </summary>
    public static partial class Miracles
    {
        public const int SystemOrder = 252;

        /// <summary>Refusal codes of the miracles (the common ones are <see cref="DivineReasons"/>).</summary>
        public const string NoEffect = "no_effect", BadMagnitude = "bad_magnitude", NoRoom = "no_room", TooClose = "too_close", AlreadyActive = "already_active";

        public static string Sentence(string code)
        {
            switch (code)
            {
                case NoEffect: return "That would change nothing.";
                case BadMagnitude: return "That amount is not allowed.";
                case NoRoom: return "There is no room for it there.";
                case TooClose: return "Another god's sacred place is too near.";
                case AlreadyActive: return "That blessing is already on them.";
                default: return DivineReasons.Sentence(code);
            }
        }

        private static CommandResult Refuse(string code) => CommandResult.Reject(code, Sentence(code));

        /// <summary>Registers the handler and the system (called by <c>DivinityModule.Install</c>).</summary>
        public static void Install(GodSim sim)
        {
            sim.RegisterHandler(new CastMiracleHandler());
            sim.AddSystem(SystemOrder, new MiraclesSystem());
        }

        // ---------------- the K1 seam (RevealTechnology 11) ----------------

        /// <summary>
        /// The seam for K1 (industrial knowledge and designs): validate and apply <see cref="MiracleKind.RevealTechnology"/> given the
        /// god, the target person (<c>target.Id</c>), a design id (<c>magnitude</c>) and the intervention id (0 while only validating).
        /// Empty in D3: <paramref name="available"/> stays false, so the kind is refused <c>act_unavailable</c>. K1 implements it in its
        /// own file, sets <paramref name="available"/>, the cost, and a refusal code when it refuses. Revelation confers knowledge only, never ownership.
        /// </summary>
        static partial void RevealTechnology(GodState s, EventBuffer ev, int god, DivineTarget target, int magnitude, int intervention, bool apply, ref bool available, ref string refusal, ref long cost);

        // ---------------- magnitude packing ----------------

        /// <summary>CreateResource carries the ware and the count in <c>Magnitude</c>: <c>ware &lt;&lt; 8 | count</c>.</summary>
        public static int PackCreate(Ware ware, int count) => ((int)ware << 8) | (count & 255);
        public static Ware CreateWare(int magnitude) => (Ware)((magnitude >> 8) & 255);
        public static int CreateCount(int magnitude) => magnitude & 255;

        /// <summary>The wares a miracle may create (basic personal wares: food, logs, the axe).</summary>
        public static bool CanCreate(Ware w) => w == Ware.Berries || w == Ware.Roots || w == Ware.Fish || w == Ware.Log || w == Ware.Axe;

        // ---------------- queries ----------------

        /// <summary>True while a person has an active blessing of the kind (Bless or Protect).</summary>
        public static bool Has(GodState s, int npc, MiracleKind kind)
        {
            var m = s.Divinity?.Miracles;
            if (m == null || m.Blessings.Count == 0) return false;
            int i = m.IndexOf(npc, kind);
            return i >= 0 && m.Blessings[i].Until > s.Tick;
        }

        // ---------------- planning (never mutates) ----------------

        /// <summary>What a validated miracle will do: the cost and the resolved things.</summary>
        internal sealed class Plan
        {
            public CommandResult Result = CommandResult.Accept();
            public long Cost;
            public Npc Npc;
            public Hut Hut;
            public BuildingRecord Building;
            public ResourceSource Source;
            public int Amount, Count;
            public Ware Ware;
            public Holder Holder;
            public Owner Holding;
            public int Node, Radius;
            public ItemLot Lot;
            public int Place;
        }

        private static Plan Fail(string code) => new Plan { Result = Refuse(code) };

        private static Plan PersonTarget(GodState s, DivineTarget t, out Npc npc)
        {
            npc = null;
            if (t.Kind != DivineTargetKind.Npc) return Fail(DivineReasons.BadTarget);
            npc = s.Population?.Find(t.Id);
            if (npc == null) return Fail(DivineReasons.BadTarget);
            if (!Bodies.IsPerson(npc)) return Fail(DivineReasons.NotAPerson);
            if (!npc.Alive) return Fail(DivineReasons.TargetDead);
            return null;
        }

        internal static Plan MakePlan(GodState s, CastMiracle c)
        {
            if (s.Divinity?.Gods == null || s.Population == null) return Fail(DivineReasons.DivinityOff);
            var mt = s.Tuning.Divinity.Mythic;
            var tu = s.Tuning.Divinity.Miracles;
            var p = s.Population;
            Plan pl;
            switch (c.Kind)
            {
                case MiracleKind.HealInjury:
                {
                    if ((pl = PersonTarget(s, c.Target, out var n)) != null) return pl;
                    if (c.Magnitude < 1 || c.Magnitude > 1000) return Fail(BadMagnitude);
                    int eff = Math.Min(c.Magnitude * 1000, n.Injury);
                    if (eff <= 0) return Fail(NoEffect);
                    int permille = (eff + 999) / 1000;
                    return Spend(s, c, new Plan { Npc = n, Amount = eff, Cost = Math.Max(mt.Costs.HealInjuryMin, mt.Costs.HealInjuryPer100 * permille / 100) });
                }
                case MiracleKind.HealDisease:
                {
                    if ((pl = PersonTarget(s, c.Target, out var n)) != null) return pl;
                    if (n.Life == null || n.Life.Illness != 1) return Fail(NoEffect);
                    return Spend(s, c, new Plan { Npc = n, Amount = tu.HealDiseaseHealth * 1000, Cost = mt.Costs.HealDisease });
                }
                case MiracleKind.Bless:
                case MiracleKind.Protect:
                {
                    if ((pl = PersonTarget(s, c.Target, out var n)) != null) return pl;
                    if (Has(s, n.Id, c.Kind)) return Fail(AlreadyActive);
                    return Spend(s, c, new Plan { Npc = n, Cost = c.Kind == MiracleKind.Bless ? mt.Costs.Bless : mt.Costs.Protect });
                }
                case MiracleKind.SmitePerson:
                {
                    if ((pl = PersonTarget(s, c.Target, out var n)) != null) return pl;
                    if (c.Magnitude < 1 || c.Magnitude > tu.SmiteMax) return Fail(BadMagnitude);
                    int dmg = c.Magnitude * 1000;
                    if (Has(s, n.Id, MiracleKind.Protect)) dmg = (int)((long)dmg * tu.ProtectPermille / 1000);
                    if (dmg <= 0) return Fail(NoEffect);
                    return Spend(s, c, new Plan { Npc = n, Amount = c.Magnitude * 1000, Cost = Math.Max(mt.Costs.SmitePersonMin, mt.Costs.SmitePersonPer100 * c.Magnitude / 100) });
                }
                case MiracleKind.SmiteBuilding:
                {
                    if (c.Target.Kind == DivineTargetKind.Hut)
                    {
                        var hut = p.HutById(c.Target.Id);
                        if (hut == null) return Fail(DivineReasons.BadTarget);
                        return Spend(s, c, new Plan { Hut = hut, Node = hut.Node, Cost = mt.Costs.SmiteBuilding });
                    }
                    if (c.Target.Kind == DivineTargetKind.Building)
                    {
                        var rec = s.Military?.BuildingById(c.Target.Id);
                        if (rec == null || rec.IsHq) return Fail(DivineReasons.BadTarget);
                        return Spend(s, c, new Plan { Building = rec, Node = rec.Node, Cost = mt.Costs.SmiteBuilding });
                    }
                    return Fail(DivineReasons.BadTarget);
                }
                case MiracleKind.SmiteSource:
                {
                    if (c.Target.Kind != DivineTargetKind.Source) return Fail(DivineReasons.BadTarget);
                    var src = p.SourceById(c.Target.Id);
                    if (src == null) return Fail(DivineReasons.BadTarget);
                    if (c.Magnitude < 0) return Fail(BadMagnitude);
                    if (src.Stock <= 0) return Fail(NoEffect);
                    int amount = c.Magnitude == 0 ? src.Stock : Math.Min(c.Magnitude, src.Stock);
                    return Spend(s, c, new Plan { Source = src, Amount = amount, Node = src.Node, Cost = mt.Costs.SmiteSource });
                }
                case MiracleKind.CreateResource: return PlanCreate(s, c);
                case MiracleKind.DestroyResource: return PlanDestroy(s, c);
                case MiracleKind.Consecrate: return PlanConsecrate(s, c);
                case MiracleKind.RevealKnowledge: return PlanReveal(s, c);
                case MiracleKind.RevealTechnology:
                {
                    bool available = false; string refusal = null; long cost = 0;
                    RevealTechnology(s, null, c.God, c.Target, c.Magnitude, 0, false, ref available, ref refusal, ref cost);
                    if (!available) return Fail(DivineReasons.ActUnavailable);
                    if (refusal != null) return Fail(refusal);
                    return Spend(s, c, new Plan { Cost = cost });
                }
                default: return Fail(DivineReasons.ActUnavailable);
            }
        }

        /// <summary>The Mythic check last (it is the only part of Validate that reads the god): the plan, or the refusal.</summary>
        private static Plan Spend(GodState s, CastMiracle c, Plan pl)
        {
            pl.Result = Mythic.CanSpend(s, c.God, InterventionKind.Miracle, pl.Cost);
            return pl;
        }

        private static bool FindLot(GodState s, int lotId, out Holder holder, out ItemLot lot, out int node, out bool estate)
        {
            var p = s.Population;
            foreach (var g in p.Lots) if (g.Lot.Id == lotId) { holder = Holder.Ground(g.Node); lot = g.Lot; node = g.Node; estate = false; return true; }
            foreach (var b in p.Bins)
                foreach (var l in b.Lots)
                    if (l.Id == lotId) { holder = Holder.Bin(b.Id); lot = l; node = b.Node; estate = b.Kind == BinKind.Estate || l.Owner.Kind == OwnerKind.Estate; return true; }
            foreach (var n in p.Npcs)
            {
                if (!Bodies.IsLivingPerson(n)) continue;
                foreach (var l in n.Carry)
                    if (!l.Empty && l.Id == lotId) { holder = Holder.Carry(n.Id); lot = l; node = n.Node; estate = l.Owner.Kind == OwnerKind.Estate; return true; }
            }
            holder = default; lot = default; node = -1; estate = false;
            return false;
        }

        /// <summary>The room check of <c>Goods.Produce</c> for a carry, mirrored so a miracle refuses before it spends (the F0 rule: weight, and a free or mergeable slot).</summary>
        private static bool CarryFits(GodState s, Npc n, Ware w, int count, Owner owner)
        {
            if (n == null || !n.Alive) return false;
            int free = s.Tuning.Population.CarryWeight - PopulationModule.CarryWeight(n);
            if (free / PopulationModule.Weight(w) < count) return false;
            foreach (var l in n.Carry) if (l.Empty || l.Ware == w && w != Ware.Axe && l.Owner.Same(owner)) return true;
            return false;
        }

        private static Plan PlanCreate(GodState s, CastMiracle c)
        {
            var tu = s.Tuning.Divinity.Miracles;
            var costs = s.Tuning.Divinity.Mythic.Costs;
            var w = CreateWare(c.Magnitude);
            int count = CreateCount(c.Magnitude);
            if (!CanCreate(w) || count < 1 || count > tu.CreateMax) return Fail(BadMagnitude);
            long cost = count * (w == Ware.Axe ? costs.CreateToolPerUnit : costs.CreatePerUnit);
            var p = s.Population;
            var pl = new Plan { Ware = w, Count = count, Cost = cost };
            switch (c.Target.Kind)
            {
                case DivineTargetKind.Location:
                    if (!s.Territory.Grid.IsLand(c.Target.Node)) return Fail(DivineReasons.BadTarget);
                    pl.Holder = Holder.Ground(c.Target.Node); pl.Holding = Owner.None; pl.Node = c.Target.Node;
                    break;
                case DivineTargetKind.Npc:
                {
                    Plan f;
                    if ((f = PersonTarget(s, c.Target, out var n)) != null) return f;
                    pl.Npc = n; pl.Holder = Holder.Carry(n.Id); pl.Holding = Owner.Npc(n.Id); pl.Node = n.Node;
                    if (!CarryFits(s, n, w, count, pl.Holding)) return Fail(NoRoom);
                    break;
                }
                case DivineTargetKind.Bin:
                {
                    var b = Goods.BinById(p, c.Target.Id);
                    if (b == null || b.Kind == BinKind.Estate) return Fail(DivineReasons.BadTarget);
                    if (b.WeightCap != 0 && b.Weight + PopulationModule.Weight(w) * count > b.WeightCap) return Fail(NoRoom);
                    pl.Holder = Holder.Bin(b.Id); pl.Holding = b.Owner; pl.Node = b.Node;
                    break;
                }
                default: return Fail(DivineReasons.BadTarget);
            }
            return Spend(s, c, pl);
        }

        private static Plan PlanDestroy(GodState s, CastMiracle c)
        {
            if (c.Target.Kind != DivineTargetKind.Lot) return Fail(DivineReasons.BadTarget);
            if (!FindLot(s, c.Target.Id, out var holder, out var lot, out int node, out bool estate)) return Fail(DivineReasons.BadTarget);
            if (estate) return Fail(DivineReasons.BadTarget); // an open estate stays intact
            if (c.Magnitude < 1 || c.Magnitude > lot.Count) return Fail(BadMagnitude);
            long cost = Math.Max(1, s.Tuning.Divinity.Mythic.Costs.DestroyPerUnit * c.Magnitude);
            return Spend(s, c, new Plan { Holder = holder, Lot = lot, Count = c.Magnitude, Ware = lot.Ware, Node = node, Cost = cost });
        }

        private static Plan PlanConsecrate(GodState s, CastMiracle c)
        {
            var tu = s.Tuning.Divinity.Miracles;
            if (c.Target.Kind != DivineTargetKind.Location) return Fail(DivineReasons.BadTarget);
            var g = s.Territory.Grid;
            if (!g.IsLand(c.Target.Node)) return Fail(DivineReasons.BadTarget);
            int radius = c.Target.Radius > 0 ? c.Target.Radius : tu.SacredRadius;
            var m = s.Divinity.Miracles;
            if (m != null)
                foreach (var pl in m.Places)
                {
                    int d = g.Distance(pl.Node, c.Target.Node);
                    if (pl.God == c.God && pl.Node == c.Target.Node) return Fail(NoEffect);
                    if (pl.God != c.God && d < tu.SacredSpacing) return Fail(TooClose);
                }
            return Spend(s, c, new Plan { Node = c.Target.Node, Radius = radius, Cost = s.Tuning.Divinity.Mythic.Costs.Consecrate });
        }

        // ---------------- the handler ----------------

        /// <summary>The handler of <see cref="CastMiracle"/> (D3; contracts 8).</summary>
        public sealed class CastMiracleHandler : CommandHandler<CastMiracle>
        {
            protected override CommandResult Validate(GodState s, CastMiracle c) => MakePlan(s, c).Result;

            protected override CommandResult Apply(GodState s, CastMiracle c, EventBuffer ev)
            {
                var pl = MakePlan(s, c);
                if (!pl.Result.Ok) return pl.Result;
                var r = Mythic.TrySpend(s, ev, c.God, InterventionKind.Miracle, pl.Cost, Interventions.Peek(s));
                if (!r.Ok) return r;
                var scope = c.Target.Kind == DivineTargetKind.Npc ? DivineScope.Individual : c.Target.Kind == DivineTargetKind.Location ? DivineScope.Location : DivineScope.None;
                int iv = Interventions.Open(s, ev, c.God, InterventionKind.Miracle, DivineAct.None, c.Target, scope, pl.Cost);
                var m = DivineLedger.State(s);
                m.Records.Add(new MiracleRecord { Intervention = iv, God = c.God, Kind = c.Kind, Target = c.Target, Magnitude = c.Magnitude, Cost = pl.Cost, Tick = s.Tick });
                ev.Add(new MiracleCast
                {
                    Intervention = iv, God = c.God, Kind = c.Kind, TargetKind = c.Target.Kind, TargetId = c.Target.Id, TargetNode = c.Target.Node,
                    TargetRadius = c.Target.Radius, Magnitude = c.Magnitude, Cost = pl.Cost,
                });
                Do(s, ev, c, pl, iv);
                return CommandResult.Accept();
            }
        }

        internal static void Effect(EventBuffer ev, int iv, byte thing, int id, string field, long before, long after, bool death = false, DeathCause cause = DeathCause.None) =>
            ev.Add(new MiracleEffectApplied { Intervention = iv, ThingKind = thing, ThingId = id, Field = field, Before = before, After = after, Death = death, Cause = cause });

        private static void Sign(GodState s, int iv, int god, MiracleKind kind, int node)
        {
            var m = s.Divinity.Miracles;
            var tu = s.Tuning.Divinity.Miracles;
            bool loud = kind == MiracleKind.SmitePerson || kind == MiracleKind.SmiteBuilding || kind == MiracleKind.SmiteSource;
            m.Signs.Add(new DivineSign { Intervention = iv, God = god, Kind = (byte)kind, Node = node, Radius = (byte)Math.Min(255, tu.SignNodes), Loudness = (byte)(loud ? 2 : 1), Until = s.Tick + tu.SignTicks });
            while (m.Signs.Count > MiracleState.SignRing) m.Signs.RemoveAt(0);
        }

        private static int Ticks(GodState s, int days) => days * SimCalendar.Of(s).TicksPerDay;

        private static void Do(GodState s, EventBuffer ev, CastMiracle c, Plan pl, int iv)
        {
            var tu = s.Tuning.Divinity.Miracles;
            var m = s.Divinity.Miracles;
            switch (c.Kind)
            {
                case MiracleKind.HealInjury:
                {
                    var n = pl.Npc;
                    int inj = n.Injury, pain = n.PainExtra;
                    Bodies.Heal(s, ev, n, HealKind.Injury, pl.Amount, iv);
                    // the pain share falls by the same share (MYTHIC 6.1); the drive follows at once (relief is paid at the next settle)
                    if (inj > 0) n.PainExtra -= (int)((long)n.PainExtra * pl.Amount / inj);
                    n.Drives[Drive.Pain] = Math.Max(n.Injury, n.PainExtra);
                    Effect(ev, iv, MiracleThing.Person, n.Id, "injury", inj, n.Injury);
                    if (pain != n.PainExtra) Effect(ev, iv, MiracleThing.Person, n.Id, "pain_extra", pain, n.PainExtra);
                    Sign(s, iv, c.God, c.Kind, n.Node);
                    break;
                }
                case MiracleKind.HealDisease:
                {
                    var n = pl.Npc;
                    int health = n.Health;
                    Bodies.Heal(s, ev, n, HealKind.Illness, pl.Amount, iv);
                    Effect(ev, iv, MiracleThing.Person, n.Id, "illness", 1, n.Life.Illness);
                    if (health != n.Health) Effect(ev, iv, MiracleThing.Person, n.Id, "health", health, n.Health);
                    Sign(s, iv, c.God, c.Kind, n.Node);
                    break;
                }
                case MiracleKind.Bless:
                case MiracleKind.Protect:
                {
                    var n = pl.Npc;
                    long until = s.Tick + Ticks(s, c.Kind == MiracleKind.Bless ? tu.BlessDays : tu.ProtectDays);
                    var b = new Blessing { Npc = n.Id, Kind = c.Kind, Until = until, God = c.God, Intervention = iv };
                    int at = m.IndexOf(n.Id, c.Kind);
                    if (at >= 0) m.Blessings[at] = b; else m.Blessings.Insert(~at, b); // an expired one still on file is replaced
                    Effect(ev, iv, MiracleThing.Person, n.Id, c.Kind == MiracleKind.Bless ? "blessing" : "protection", 0, until);
                    Sign(s, iv, c.God, c.Kind, n.Node);
                    break;
                }
                case MiracleKind.SmitePerson: Smite(s, ev, c, pl, iv); break;
                case MiracleKind.SmiteBuilding: SmiteBuilding(s, ev, c, pl, iv); break;
                case MiracleKind.SmiteSource:
                {
                    var src = pl.Source;
                    int before = PopulationMiracles.SetSourceStock(s, src, src.Stock - pl.Amount);
                    Effect(ev, iv, MiracleThing.Source, src.Id, "stock", before, src.Stock);
                    if (tu.SmiteBlights != 0)
                    {
                        long until = s.Tick + Ticks(s, tu.BlightDays);
                        var bl = new SourceBlight { Source = src.Id, Stock = src.Stock, Intervention = iv, Until = until };
                        int at = 0;
                        while (at < m.Blights.Count && m.Blights[at].Source < src.Id) at++;
                        if (at < m.Blights.Count && m.Blights[at].Source == src.Id) m.Blights[at] = bl; else m.Blights.Insert(at, bl);
                        Effect(ev, iv, MiracleThing.Source, src.Id, "regrowth_until", 0, until);
                    }
                    Sign(s, iv, c.God, c.Kind, src.Node);
                    Witnesses(s, ev, iv, src.Node, 0);
                    break;
                }
                case MiracleKind.CreateResource: Create(s, ev, c, pl, iv); break;
                case MiracleKind.DestroyResource: Destroy(s, ev, c, pl, iv); break;
                case MiracleKind.Consecrate:
                {
                    m.Places.Add(new ConsecratedPlace { Id = iv, God = c.God, Node = pl.Node, Radius = pl.Radius, SinceTick = s.Tick });
                    Effect(ev, iv, MiracleThing.Place, iv, "place", 0, pl.Node + 1);
                    ev.Add(new LocationConsecrated { Intervention = iv, God = c.God, Node = pl.Node, Radius = pl.Radius });
                    Sign(s, iv, c.God, c.Kind, pl.Node);
                    break;
                }
                case MiracleKind.RevealKnowledge: Reveal(s, ev, c, pl, iv); break;
                case MiracleKind.RevealTechnology:
                {
                    bool available = false; string refusal = null; long cost = 0;
                    RevealTechnology(s, ev, c.God, c.Target, c.Magnitude, iv, true, ref available, ref refusal, ref cost);
                    break;
                }
            }
        }

        // ---------------- smite ----------------

        private static void Smite(GodState s, EventBuffer ev, CastMiracle c, Plan pl, int iv)
        {
            var tu = s.Tuning.Divinity.Miracles;
            var n = pl.Npc;
            int inj = n.Injury, health = n.Health, node = n.Node, id = n.Id;
            Bodies.Damage(s, ev, n, pl.Amount, DamageCause.Divine, 0, iv);
            Effect(ev, iv, MiracleThing.Person, id, "injury", inj, n.Injury);
            if (n.Alive)
            {
                Effect(ev, iv, MiracleThing.Person, id, "health", health, n.Health);
                int fear = n.Drives[Drive.Fear];
                n.Drives[Drive.Fear] = Math.Min(1000000, fear + tu.SmiteFear * 1000);
                if (fear != n.Drives[Drive.Fear]) Effect(ev, iv, MiracleThing.Person, id, "fear", fear, n.Drives[Drive.Fear]);
            }
            else Effect(ev, iv, MiracleThing.Person, id, "health", health, 0, true, n.DeathCause); // the divine attribution of a Violence death (contracts 20.4)
            Sign(s, iv, c.God, c.Kind, node);
            Witnesses(s, ev, iv, node, id);
        }

        /// <summary>Everyone awake within sight and hearing of a smite is afraid (a drive rise through the normal channel); nobody is told why.</summary>
        private static void Witnesses(GodState s, EventBuffer ev, int iv, int node, int except)
        {
            var tu = s.Tuning.Divinity.Miracles;
            if (tu.WitnessFear <= 0) return;
            var g = s.Territory.Grid;
            foreach (var w in s.Population.Npcs)
            {
                if (!Bodies.IsLivingPerson(w) || w.Id == except || w.Action.Asleep) continue;
                if (g.Distance(w.Node, node) > tu.WitnessNodes) continue;
                int fear = w.Drives[Drive.Fear];
                int after = Math.Min(1000000, fear + tu.WitnessFear * 1000);
                if (after == fear) continue;
                w.Drives[Drive.Fear] = after;
                Effect(ev, iv, MiracleThing.Witness, w.Id, "fear", fear, after);
            }
        }

        private static void SmiteBuilding(GodState s, EventBuffer ev, CastMiracle c, Plan pl, int iv)
        {
            var tu = s.Tuning.Divinity.Miracles;
            if (pl.Building != null)
            {
                var rec = pl.Building;
                int id = rec.Id, node = rec.Node;
                MilitaryRules.Destroy(s, rec, ev);
                Effect(ev, iv, MiracleThing.Building, id, "standing", 1, 0);
                Sign(s, iv, c.God, c.Kind, node);
                Witnesses(s, ev, iv, node, 0);
                return;
            }
            var hut = pl.Hut;
            // the store is emptied first, through Goods: scattered on the ground at the bin's own node, or burnt (SmiteBurnsContents)
            foreach (var bin in PopulationMiracles.HutStoreBins(s, hut))
            {
                while (bin.Lots.Count > 0)
                {
                    var l = bin.Lots[0];
                    if (tu.SmiteBurnsContents != 0)
                    {
                        if (!Goods.Consume(s, ev, Holder.Bin(bin.Id), l.Id, l.Count, ConsumeReason.Miracle, 0, iv)) throw new InvalidOperationException("smite hut: consume failed");
                        DivineLedger.BookDestroyed(s, ev, c.God, iv, l.Ware, l.Count, l.Id, Holder.Bin(bin.Id), bin.Node);
                        Effect(ev, iv, MiracleThing.Lot, l.Id, "burnt", l.Count, 0);
                    }
                    else
                    {
                        var r = Goods.Move(s, ev, Holder.Bin(bin.Id), l.Id, l.Count, Holder.Ground(bin.Node), MoveReason.Drop, 0, OwnerRule.Set, Owner.None);
                        if (!r.Ok) throw new InvalidOperationException("smite hut: scatter failed: " + r.Status);
                        Effect(ev, iv, MiracleThing.Lot, r.IntoLot, "scattered", l.Count, 0);
                    }
                }
            }
            int hutId = hut.Id, hnode = hut.Node;
            int lost = PopulationMiracles.RemoveHut(s, ev, hut);
            Effect(ev, iv, MiracleThing.Hut, hutId, "standing", 1, 0);
            if (lost > 0) Effect(ev, iv, MiracleThing.Hut, hutId, "residents_homeless", 0, lost);
            Sign(s, iv, c.God, c.Kind, hnode);
            Witnesses(s, ev, iv, hnode, 0);
        }

        // ---------------- create and destroy ----------------

        private static void Create(GodState s, EventBuffer ev, CastMiracle c, Plan pl, int iv)
        {
            var p = s.Population;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            int spoil = Wares.IsFood(pl.Ware) ? day + PopulationModule.SpoilDays(s.Tuning.Ecology, pl.Ware) : -1;
            int uses = pl.Ware == Ware.Axe ? s.Tuning.Population.AxeUses : 0;
            int newLot = p.NextLotId;
            int into = Goods.Produce(s, ev, pl.Holder, pl.Ware, pl.Count, spoil, uses, pl.Holding, ProduceReason.Miracle, 0, iv);
            if (into == 0) throw new InvalidOperationException("miracle create: no room after the room check");
            DivineLedger.BookCreated(s, ev, c.God, iv, pl.Ware, pl.Count, newLot, into, pl.Holder);
            if (!pl.Holding.IsNone)
                ev.Add(new OwnershipChanged
                {
                    AssetKind = OwnershipChanged.AssetLot, Lot = into, Ware = pl.Ware, Count = pl.Count, FromKind = OwnerKind.None, FromId = 0,
                    ToKind = pl.Holding.Kind, ToId = pl.Holding.Id, Basis = OwnershipBasis.Miracle, Actor = 0,
                });
            Effect(ev, iv, MiracleThing.Lot, into, "created", 0, pl.Count);
            Sign(s, iv, c.God, c.Kind, pl.Node);
        }

        private static void Destroy(GodState s, EventBuffer ev, CastMiracle c, Plan pl, int iv)
        {
            var lot = pl.Lot;
            if (!Goods.Consume(s, ev, pl.Holder, lot.Id, pl.Count, ConsumeReason.Miracle, 0, iv)) throw new InvalidOperationException("miracle destroy: consume failed");
            DivineLedger.BookDestroyed(s, ev, c.God, iv, pl.Ware, pl.Count, lot.Id, pl.Holder, pl.Node);
            // the loss is booked against the owner like spoilage, so the owner's learning feels it (where the goods are credited at once)
            if (lot.Owner.Kind == OwnerKind.Npc && !Goods.CreditedLater(s, pl.Ware))
            {
                var o = s.Population.Find(lot.Owner.Id);
                if (o != null && o.Alive && o.Mind != null) Learner.AddReward(o.Mind.Store, -GoodsWorth.Of(s, o, pl.Ware) * pl.Count);
            }
            Effect(ev, iv, MiracleThing.Lot, lot.Id, "destroyed", lot.Count, lot.Count - pl.Count);
            Sign(s, iv, c.God, c.Kind, pl.Node);
        }

        // ---------------- the invariants ----------------

        /// <summary>D3's invariants (called by <c>DivinityModule.Check</c>): C7, blessings sorted and unique, places ascending, the ring bounded.</summary>
        public static void Check(GodState s, List<string> bad)
        {
            if (s.Divinity == null) return;
            DivineLedger.Check(s, bad);
            var m = s.Divinity.Miracles;
            if (m == null) return;
            for (int i = 1; i < m.Blessings.Count; i++)
            {
                var a = m.Blessings[i - 1]; var b = m.Blessings[i];
                if (a.Npc > b.Npc || a.Npc == b.Npc && (int)a.Kind >= (int)b.Kind) bad.Add("D3: blessings not sorted or duplicate at " + b.Npc + "/" + b.Kind);
            }
            for (int i = 1; i < m.Places.Count; i++) if (m.Places[i].Id <= m.Places[i - 1].Id) bad.Add("D3: places not ascending at " + m.Places[i].Id);
            for (int i = 1; i < m.Records.Count; i++) if (m.Records[i].Intervention <= m.Records[i - 1].Intervention) bad.Add("D3: records not ascending at " + m.Records[i].Intervention);
            if (m.Signs.Count > MiracleState.SignRing) bad.Add("D3: sign ring over " + MiracleState.SignRing);
            if (m.Ledger != null && m.Ledger.Created.Length != (s.FactionCount + 1) * Wares.PersonalCount) bad.Add("D3: ledger size");
        }
    }

    /// <summary>
    /// D3's system (order 252, after the population at 250): only while miracle state exists. Per tick it holds the stock of a
    /// blighted source (regrowth suspended; O(blights)); at each day start it ends expired blessings and signs and pays the
    /// Bless recovery (<c>Health</c> +<c>BlessRecover</c> permille when Nutrition is above 300), one
    /// <see cref="MiracleEffectApplied"/> per change.
    /// </summary>
    public sealed class MiraclesSystem : ISimSystem
    {
        public void Tick(GodState s, EventBuffer ev)
        {
            var m = s.Divinity?.Miracles;
            if (m == null) return;
            var p = s.Population;
            for (int i = m.Blights.Count - 1; i >= 0; i--)
            {
                var b = m.Blights[i];
                var src = p.SourceById(b.Source);
                if (src == null || s.Tick >= b.Until) { m.Blights.RemoveAt(i); continue; }
                if (src.Stock < b.Stock) { b.Stock = src.Stock; m.Blights[i] = b; }
                if (src.Stock > b.Stock) src.Stock = b.Stock;
                src.EmptyDays = 0;
            }
            long day = SimCalendar.Of(s).Day(s.Tick);
            if (day <= m.DayDone) return;
            m.DayDone = day;
            var tu = s.Tuning.Divinity.Miracles;
            for (int i = 0; i < m.Blessings.Count; i++)
            {
                var b = m.Blessings[i];
                if (b.Kind != MiracleKind.Bless || b.Until <= s.Tick) continue;
                var n = p.Find(b.Npc);
                if (n == null || !Bodies.IsLivingPerson(n) || n.Nutrition <= 300000 || n.Health >= 1000000) continue;
                int before = n.Health;
                n.Health = Math.Min(1000000, n.Health + tu.BlessRecover * 1000);
                Miracles.Effect(ev, b.Intervention, MiracleThing.Person, n.Id, "health", before, n.Health);
            }
            for (int i = m.Blessings.Count - 1; i >= 0; i--) if (m.Blessings[i].Until <= s.Tick) m.Blessings.RemoveAt(i);
            for (int i = m.Signs.Count - 1; i >= 0; i--) if (m.Signs[i].Until <= s.Tick) m.Signs.RemoveAt(i);
        }
    }
}
