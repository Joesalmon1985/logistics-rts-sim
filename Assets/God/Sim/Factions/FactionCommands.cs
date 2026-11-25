namespace God.Sim
{
    /// <summary>Refusal codes of the faction commands.</summary>
    public static class FactionReasons
    {
        public const string SameFaction = "same_faction";
        public const string NotInGame = "not_in_game";
        public const string AlreadyAtWar = "already_at_war";
        public const string NotAtWar = "not_at_war";
        public const string AlreadyOffered = "already_offered";
        public const string NotAnHq = "not_an_hq";
        public const string AlreadyCitadel = "already_citadel";
        public const string AlreadyUpgrading = "already_upgrading";
        public const string HqNotFinished = "hq_not_finished";

        public static CommandResult Refuse(string code, string message) => CommandResult.Reject(code, message);
    }

    /// <summary><c>Faction</c> declares war on <see cref="Against"/>. It takes effect when applied, for both sides.</summary>
    public sealed class DeclareWar : FactionCommand
    {
        public Faction Against;
        public DeclareWar(Faction by, Faction against) : base(by) { Against = against; }
    }

    /// <summary>
    /// <c>Faction</c> offers peace to <see cref="To"/>. The pair is at peace once both have offered (the second offer makes
    /// peace at once). An offer stands until peace is made or one side declares war again.
    /// </summary>
    public sealed class OfferPeace : FactionCommand
    {
        public Faction To;
        public OfferPeace(Faction by, Faction to) : base(by) { To = to; }
    }

    /// <summary>
    /// Starts the Citadel upgrade of one of <c>Faction</c>'s finished HQs. The HQ keeps working; its own free stock goes in
    /// first and the rest of the cost is carried there like a site's. Once all wares are in, 30 s of work finish it.
    /// </summary>
    public sealed class UpgradeToCitadel : FactionCommand
    {
        public int HqId;
        public UpgradeToCitadel(Faction f, int hqId) : base(f) { HqId = hqId; }
    }

    internal static class FactionChecks
    {
        public static CommandResult Pair(GodState s, FactionCommand c, Faction other)
        {
            if (!c.ValidIn(s)) return CommandResult.RejectDefault(Reasons.NotOwner);
            if (!s.Setup.InGame((int)other) || s.Factions.IsEliminated(other))
                return FactionReasons.Refuse(FactionReasons.NotInGame, "That faction is not in the game.");
            if (other == c.Faction) return FactionReasons.Refuse(FactionReasons.SameFaction, "A faction cannot do that with itself.");
            return CommandResult.Accept();
        }
    }

    public sealed class DeclareWarHandler : CommandHandler<DeclareWar>
    {
        protected override CommandResult Validate(GodState s, DeclareWar c)
        {
            var r = FactionChecks.Pair(s, c, c.Against); if (!r.Ok) return r;
            if (s.Factions.AtWar(c.Faction, c.Against)) return FactionReasons.Refuse(FactionReasons.AlreadyAtWar, "You are already at war with them.");
            return r;
        }

        protected override CommandResult Apply(GodState s, DeclareWar c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            s.Factions.SetWar(c.Faction, c.Against, true);
            ev.Add(new WarDeclared { By = c.Faction, Against = c.Against });
            return r;
        }
    }

    public sealed class OfferPeaceHandler : CommandHandler<OfferPeace>
    {
        protected override CommandResult Validate(GodState s, OfferPeace c)
        {
            var r = FactionChecks.Pair(s, c, c.To); if (!r.Ok) return r;
            if (!s.Factions.AtWar(c.Faction, c.To)) return FactionReasons.Refuse(FactionReasons.NotAtWar, "You are not at war with them.");
            if (s.Factions.HasOfferedPeace(c.Faction, c.To)) return FactionReasons.Refuse(FactionReasons.AlreadyOffered, "You have already offered peace.");
            return r;
        }

        protected override CommandResult Apply(GodState s, OfferPeace c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            var fs = s.Factions;
            fs.SetOffer(c.Faction, c.To, true);
            ev.Add(new PeaceOffered { By = c.Faction, To = c.To });
            if (fs.HasOfferedPeace(c.To, c.Faction))
            {
                fs.SetWar(c.Faction, c.To, false);
                FactionRules.DropMarksBetween(s, c.Faction, c.To, ev);
                ev.Add(new PeaceMade { A = c.To, B = c.Faction });
            }
            return r;
        }
    }

    public sealed class UpgradeToCitadelHandler : CommandHandler<UpgradeToCitadel>
    {
        protected override CommandResult Validate(GodState s, UpgradeToCitadel c)
        {
            if (!c.ValidIn(s)) return CommandResult.RejectDefault(Reasons.NotOwner);
            var hq = s.Territory.HqOf(c.HqId);
            if (hq == null) return FactionReasons.Refuse(FactionReasons.NotAnHq, "That is not an HQ.");
            if (hq.Faction != c.Faction) return CommandResult.RejectDefault(Reasons.NotOwner);
            if (hq.Site) return FactionReasons.Refuse(FactionReasons.HqNotFinished, "The HQ must be finished first.");
            if (hq.Citadel) return FactionReasons.Refuse(FactionReasons.AlreadyCitadel, "It is already a Citadel.");
            var rec = s.Economy?.Find(c.HqId);
            if (rec == null) return FactionReasons.Refuse(FactionReasons.NotAnHq, "That is not an HQ.");
            if (rec.Upgrading) return FactionReasons.Refuse(FactionReasons.AlreadyUpgrading, "It is already being upgraded.");
            return CommandResult.Accept();
        }

        protected override CommandResult Apply(GodState s, UpgradeToCitadel c, EventBuffer ev)
        {
            var r = Validate(s, c); if (!r.Ok) return r;
            EconomySystem.StartUpgrade(s.Economy.Find(c.HqId));
            ev.Add(new CitadelStarted { Faction = c.Faction, HqId = c.HqId });
            return r;
        }
    }
}
