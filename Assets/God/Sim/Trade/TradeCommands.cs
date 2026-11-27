using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// <c>Faction</c> sends an envoy to <see cref="To"/> with an offer: it gives <see cref="Give"/> and wants <see cref="Want"/>
    /// (1 to 8 units each way). The give wares leave one of the sender's HQs at once, the one nearest the target that has
    /// them all free in stock (or <see cref="FromHq"/> when set). Refused when the target is out of the game, at war with the
    /// sender, or has no finished HQ.
    /// </summary>
    public sealed class OfferTrade : FactionCommand
    {
        public Faction To;
        public WareCount[] Give;
        public WareCount[] Want;
        /// <summary>HQ to take the wares from; 0 chooses.</summary>
        public int FromHq;
        public OfferTrade(Faction from, Faction to, WareCount[] give, WareCount[] want, int fromHq = 0) : base(from)
        { To = to; Give = give; Want = want; FromHq = fromHq; }
    }

    /// <summary>The target answers an offer whose envoy has arrived. Only the target may answer.</summary>
    public sealed class AnswerTrade : FactionCommand
    {
        public int OfferId;
        public bool Accept;
        public AnswerTrade(Faction by, int offerId, bool accept) : base(by) { OfferId = offerId; Accept = accept; }
    }

    public sealed class OfferTradeHandler : CommandHandler<OfferTrade>
    {
        private struct Plan { public WareCount[] Give, Want; public EcoBuilding From, Target; }

        private static CommandResult Check(GodState s, OfferTrade c, out Plan plan)
        {
            plan = default;
            var t = s.Trade;
            if (t == null) return CommandResult.RejectDefault(Reasons.Disabled);
            var r = FactionChecks.Pair(s, c, c.To); if (!r.Ok) return r;
            if (s.Factions.AtWar(c.Faction, c.To)) return CommandResult.Reject(TradeReasons.AtWar, "You are at war with them.");
            var give = TradeRules.Normalize(c.Give, out string bad);
            var want = bad == null ? TradeRules.Normalize(c.Want, out bad) : null;
            if (bad == TradeReasons.BadWare) return CommandResult.Reject(bad, "That ware cannot be traded.");
            if (bad != null) return CommandResult.Reject(bad, "Counts must be whole numbers.");
            int tg = TradeRules.Total(give), tw = TradeRules.Total(want);
            if (tg < 1 || tw < 1) return CommandResult.Reject(TradeReasons.BadAmount, "An offer needs at least 1 ware each way.");
            if (tg > TradeTuning.MaxWaresPerSide || tw > TradeTuning.MaxWaresPerSide)
                return CommandResult.Reject(TradeReasons.BadAmount, "At most " + TradeTuning.MaxWaresPerSide + " wares each way.");
            foreach (var g in give) foreach (var w in want)
                    if (g.Ware == w.Ware) return CommandResult.Reject(TradeReasons.SameWare, "Offer and ask different wares.");
            if (t.EnvoyCountOf(c.Faction) >= TradeTuning.MaxEnvoysPerFaction)
                return CommandResult.Reject(TradeReasons.TooManyEnvoys, "You already have " + TradeTuning.MaxEnvoysPerFaction + " envoys out.");

            var anyHq = s.Economy.HqsOf(c.To);
            if (anyHq.Count == 0) return CommandResult.Reject(TradeReasons.NoTargetHq, "They have no HQ to send an envoy to.");
            var mine = s.Economy.HqsOf(c.Faction);
            if (mine.Count == 0) return CommandResult.Reject(TradeReasons.NoStock, "You have no finished HQ to send from.");
            // The target HQ is the one nearest the first candidate sender HQ; the sender HQ is then the one with the wares nearest that target.
            var probe = TradeRules.PickSenderHq(s, c.Faction, give, TradeRules.HqX(s, anyHq[0]), TradeRules.HqZ(s, anyHq[0]), c.FromHq);
            if (probe == null) return CommandResult.Reject(TradeReasons.NoStock, "No single HQ of yours has those wares free in stock.");
            var target = TradeRules.NearestHq(s, c.To, TradeRules.HqX(s, probe), TradeRules.HqZ(s, probe));
            var from = TradeRules.PickSenderHq(s, c.Faction, give, TradeRules.HqX(s, target), TradeRules.HqZ(s, target), c.FromHq);
            int sx = TradeRules.HqX(s, from), sz = TradeRules.HqZ(s, from), gx = TradeRules.HqX(s, target), gz = TradeRules.HqZ(s, target);
            var g2 = s.Territory.Grid;
            if (!Pathing.LineClear(g2, sx, sz, gx, gz))
            {
                int goal = Pathing.NearestLandNode(g2, gx, gz);
                if (goal < 0 || Pathing.FindWaypoints(g2, sx, sz, goal) == null) return CommandResult.Reject(TradeReasons.NoPath, "No overland way to them.");
            }
            plan = new Plan { Give = give, Want = want, From = from, Target = target };
            return CommandResult.Accept();
        }

        protected override CommandResult Validate(GodState s, OfferTrade c) => Check(s, c, out _);

        protected override CommandResult Apply(GodState s, OfferTrade c, EventBuffer ev)
        {
            var r = Check(s, c, out var p); if (!r.Ok) return r;
            var t = s.Trade;
            var env = new Envoy
            {
                Id = t.NextId++, Faction = c.Faction, Target = c.To, Stage = EnvoyStage.ToTarget,
                HomeHq = p.From.Id, TargetHq = p.Target.Id,
            };
            env.Body.Id = env.Id; env.Body.Faction = c.Faction;
            env.Body.X = TradeRules.HqX(s, p.From); env.Body.Z = TradeRules.HqZ(s, p.From);
            env.Body.FaceX = TradeRules.HqX(s, p.Target) - env.Body.X; env.Body.FaceZ = TradeRules.HqZ(s, p.Target) - env.Body.Z;
            foreach (var l in p.Give)
            {
                p.From.Stock[(int)l.Ware] -= l.Count;
                env.Cargo[(int)l.Ware] += l.Count;
                t.Departed[(int)l.Ware] += l.Count;
            }
            t.Envoys.Add(env);
            t.Offers.Add(new TradeOffer
            {
                Id = env.Id, From = c.Faction, To = c.To, Give = p.Give, Want = p.Want, Stage = OfferStage.InFlight,
                SentTick = s.Tick, TargetHq = p.Target.Id,
            });
            ev.Add(new TradeOffered { OfferId = env.Id, From = c.Faction, To = c.To, Give = p.Give, Want = p.Want, HqId = p.From.Id });
            return r;
        }
    }

    public sealed class AnswerTradeHandler : CommandHandler<AnswerTrade>
    {
        private static CommandResult Check(GodState s, AnswerTrade c)
        {
            if (!c.ValidIn(s)) return CommandResult.RejectDefault(Reasons.NotOwner);
            var o = s.Trade?.OfferById(c.OfferId);
            if (o == null) return CommandResult.RejectDefault(Reasons.NoSuchThing);
            if (o.To != c.Faction) return CommandResult.RejectDefault(Reasons.NotOwner);
            if (o.Stage != OfferStage.Pending) return CommandResult.Reject(TradeReasons.NotPending, "That envoy has not arrived yet.");
            return CommandResult.Accept();
        }

        protected override CommandResult Validate(GodState s, AnswerTrade c) => Check(s, c);

        protected override CommandResult Apply(GodState s, AnswerTrade c, EventBuffer ev)
        {
            var r = Check(s, c); if (!r.Ok) return r;
            TradeRules.Answer(s, c.OfferId, c.Accept, ev);
            return r;
        }
    }
}
