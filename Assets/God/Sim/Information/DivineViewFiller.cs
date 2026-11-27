using System;
using System.Collections.Generic;
using God.Sim.Divinity;

namespace God.Sim.Information
{
    /// <summary>
    /// Contracts 9 (INFORMATION 12.3, layer 8): the single adapter between true state and a god's <see cref="IDivineView"/>,
    /// as <see cref="ViewFiller"/> is for faction views. Only the <c>Omniscient</c> profile exists in this run (the others,
    /// Tier 3, are refused). The view is a live read-only wrapper built on demand (never per tick by the sim): reading it
    /// changes nothing (I-D5-3, I-D5-6). Person items wrap the same <see cref="PopulationKnowledge"/> sighting output the
    /// faction view gives, tagged <see cref="KnowledgeLayer.DivineObservation"/>; an omniscient god also sees the dead.
    /// OWNER: D1.
    /// </summary>
    public static class DivineViewFiller
    {
        /// <summary>A god's view. Throws for a game without gods, an unknown god, or a non-omniscient profile.</summary>
        public static IDivineView For(GodState s, int god)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            var reg = s.Divinity?.Gods ?? throw new InvalidOperationException("The game has no gods (Divinity.Enabled = 0 or no population).");
            var g = reg.Of(god) ?? throw new ArgumentOutOfRangeException(nameof(god), "No god " + god + ".");
            if (g.Profile != null && g.Profile.Default != DivinePerception.Omniscient)
                throw new NotSupportedException("Only the Omniscient divine observation profile exists in this run (Perceived, Reported, Blind are Tier 3).");
            return new OmniscientDivineView(s, god);
        }

        public static IDivineView For(GodSim sim, int god) => For(sim?.State ?? throw new ArgumentNullException(nameof(sim)), god);
    }

    internal sealed class OmniscientDivineView : IDivineView
    {
        private readonly GodState _s;
        private readonly PopulationKnowledge _seen;
        private readonly List<NpcSighting> _buf = new List<NpcSighting>();

        public OmniscientDivineView(GodState s, int god)
        {
            _s = s; God = god;
            _seen = new PopulationKnowledge(s, DivinityIds.OriginFactionOf(god));
        }

        public int God { get; }
        public long AsOfTick => _s.Tick;
        public DivinityMode Mode => Mythic.ModeOf(_s);
        public int GodCount => _s.Divinity?.Gods?.Gods.Count ?? 0;

        public DivineObservationProfile Profile
        {
            get
            {
                var p = _s.Divinity?.Gods?.Of(God)?.Profile;
                return p == null ? new DivineObservationProfile() : new DivineObservationProfile { Default = p.Default, Channels = p.Channels };
            }
        }

        public bool TryPerson(int npc, out DivinePersonView person)
        {
            var sight = _seen.See(npc);
            if (sight.HasValue)
            {
                var v = sight.Value;
                person = new DivinePersonView { Npc = v.Id, Alive = true, Node = v.Node, Faction = v.Faction, Asleep = v.Asleep, Verb = v.Verb, Layer = KnowledgeLayer.DivineObservation };
                return true;
            }
            var n = _s.Population?.Find(npc);
            if (n == null || !global::God.Sim.Population.Bodies.IsPerson(n)) { person = default; return false; }
            person = new DivinePersonView { Npc = n.Id, Alive = n.Alive, Node = n.Node, Faction = n.Faction, Layer = KnowledgeLayer.DivineObservation };
            return true;
        }

        public void People(List<DivinePersonView> into)
        {
            into.Clear();
            _seen.Sightings(_buf);
            foreach (var v in _buf)
                into.Add(new DivinePersonView { Npc = v.Id, Alive = true, Node = v.Node, Faction = v.Faction, Asleep = v.Asleep, Verb = v.Verb, Layer = KnowledgeLayer.DivineObservation });
            _buf.Clear();
        }

        public bool TryGod(int god, out DivineGodView view)
        {
            var g = _s.Divinity?.Gods?.Of(god);
            if (g == null) { view = default; return false; }
            view = new DivineGodView
            {
                God = g.Id, Name = g.Name, OriginFaction = g.OriginFaction, Seat = g.Seat, PolicyName = g.PolicyName, Mythic = g.Mythic,
                MythicMax = g.MythicMax, RegenPerDay = _s.Tuning.Divinity.Mythic.RegenPerDay, Mode = Mode,
                CooldownUntil = g.CooldownUntil == null ? new long[0] : (long[])g.CooldownUntil.Clone(), AvatarBody = g.AvatarBody,
                Profile = g.Profile?.Default ?? DivinePerception.Omniscient, Layer = KnowledgeLayer.DivineObservation,
            };
            return true;
        }

        // ---- D4 region (additive): embodiment views ----

        private long Rate(ControlKind kind, out long nominal)
        {
            nominal = Mythic.UpkeepOf(_s, kind);
            return Mode == DivinityMode.Standard ? nominal : 0;
        }

        private long Left(long rate) => rate <= 0 ? long.MaxValue : (_s.Divinity?.Gods?.Of(God)?.Mythic ?? 0) / rate;

        public bool TryPossession(out PossessionView view)
        {
            view = default;
            var ps = _s.Divinity?.Avatars?.PossessionOfGod(God);
            if (ps == null) return false;
            var e = Control.EntryOf(_s, ps.Npc);
            if (e.Kind != ControlKind.Possessed) return false;
            long rate = Rate(ControlKind.Possessed, out long nominal);
            view = new PossessionView
            {
                Intervention = ps.Intervention, God = God, Npc = ps.Npc, Node = _s.Population?.Find(ps.Npc)?.Node ?? 0, StartedTick = ps.StartTick,
                UpkeepPerTick = rate, NominalUpkeepPerTick = nominal, TicksLeft = Left(rate), Input = e.Input, Heading = e.Heading, Gait = e.Gait,
                LeaseUntil = e.LeaseUntil, Act = e.Act, InputsApplied = ps.InputsApplied, InputsRefused = ps.InputsRefused, Actions = ps.ActionsStarted,
            };
            return true;
        }

        public bool TryAvatar(out AvatarView view)
        {
            view = default;
            var g = _s.Divinity?.Gods?.Of(God);
            if (g == null || g.AvatarBody == 0) return false;
            var body = _s.Population?.Find(g.AvatarBody);
            if (body == null) return false;
            var av = _s.Divinity.Avatars?.ManifestationOfGod(God);
            var e = Control.EntryOf(_s, body.Id);
            long rate = Rate(ControlKind.AvatarBody, out long nominal);
            if (!body.Alive) { rate = 0; nominal = 0; }
            int n = 0;
            foreach (var l in body.Carry) if (!l.Empty) n++;
            var wares = new Ware[n]; var counts = new int[n];
            n = 0;
            foreach (var l in body.Carry) if (!l.Empty) { wares[n] = l.Ware; counts[n] = l.Count; n++; }
            int k = (int)InterventionKind.Avatar;
            view = new AvatarView
            {
                God = God, Body = body.Id, Node = body.Node, Alive = body.Alive, Intervention = av?.Intervention ?? 0, Health = body.HealthPermille,
                CarryWares = wares, CarryCounts = counts, StartedTick = av?.StartTick ?? 0, UpkeepPerTick = rate, NominalUpkeepPerTick = nominal,
                TicksLeft = body.Alive ? Left(rate) : 0, CooldownUntil = g.CooldownUntil != null && k < g.CooldownUntil.Length ? g.CooldownUntil[k] : 0,
                Observers = av?.Observers.Count ?? 0, Actions = av?.ActionsStarted ?? 0, Input = e.Input, Heading = e.Heading,
            };
            return true;
        }

        public void Gods(List<DivineGodView> into)
        {
            into.Clear();
            int n = GodCount;
            for (int i = 1; i <= n; i++) if (TryGod(i, out var v)) into.Add(v);
        }
    }
}
