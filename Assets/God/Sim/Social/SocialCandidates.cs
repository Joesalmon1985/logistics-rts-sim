using System;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Social
{
    /// <summary>
    /// M5's candidates (mind side; scratch and the mind only, gate G4.1): at most 4 per think after M3's and M4's, in a
    /// fixed priority. Each is a verb plus a seen person plus a composed message, and its content is composed from the
    /// gesture grammar's parts by this mind's own needs, carry and valuation. Nothing here decides: the policy scores the
    /// candidates (instinct, learned, social, personality, cost, exploration) and picks one, or none of them.
    /// <list type="number">
    /// <item>my side of an agreed deal (a hand-over), when the partner is in sight;</item>
    /// <item>answers to the newest addressed message I have read and not answered: accept, refuse or a counter-offer for an
    /// offer; give or refuse for a need of food or a want; cooperate or refuse for a request for help; flee when warned;</item>
    /// <item>my own messages: ask for food, offer a trade, offer a gift, ask for help, warn of an enemy; and seeking out a
    /// remembered trading partner.</item>
    /// </list>
    /// Targets carry <c>IntentTarget.None</c> and the person's handle in <c>TargetId</c>: the frozen M3.12 check
    /// (<c>Thinker.Known</c>) knows no person targets (an amendment is proposed); M5's own perception check is a test.
    /// </summary>
    internal static class SocialCandidates
    {
        public static void Build(ThinkScratch sc)
        {
            var m = sc.Mind?.Social;
            m?.ClearPending();
            if (m == null || m.Tuning == null || m.Tuning.Enabled == 0) return;
            bool say = m.Tuning.Gestures != 0;
            bool child = sc.Self.Stage == LifeStage.Child;
            if (!child) Deliveries(sc, m);
            if (say && !child) Answers(sc, m);
            if (say) Initiatives(sc, m, child);
        }

        // ---------------- helpers over the mind's own view ----------------

        /// <summary>Units of a ware it carries and owns (M3-M5 candidates offer only its own goods, Permits Own).</summary>
        public static int Held(ThinkScratch sc, Ware w)
        {
            int n = 0;
            for (int i = 0; i < sc.CarryCount; i++)
            {
                var l = sc.Carry[i];
                if (l.Empty || l.Ware != w || l.Owner.Kind != OwnerKind.Npc || l.Owner.Id != sc.Self.Id) continue;
                n += l.Count;
            }
            return n;
        }

        private static int FindPerson(ThinkScratch sc, int partnerAs, int handle)
        {
            for (int i = 0; i < sc.PeopleCount; i++)
            {
                var p = sc.People[i];
                if (p.Asleep) continue;
                if (partnerAs != 0 ? p.As == partnerAs : p.Id == handle) return i;
            }
            return -1;
        }

        private static int Trust(SocialMind m, int other) { int i = m.Find(other); return i < 0 ? 0 : m.Rel[i].Trust; }

        private static int Quality(int surplus) => surplus > 30 ? OfferState.Good : surplus >= -30 ? OfferState.Fair : OfferState.Poor;

        private static int Hunger(ThinkScratch sc) => sc.Self.Hunger;

        private static int Gain(ThinkScratch sc, SocialMind m, Ware w, int count) => Valuation.Gain(w, count, Held(sc, w), Hunger(sc), sc.Self.HasBed, sc.Self.CarryTool, m.Price);
        private static int Loss(ThinkScratch sc, SocialMind m, Ware w, int count) => Valuation.Loss(w, count, Held(sc, w), Hunger(sc), sc.Self.HasBed, sc.Self.CarryTool, m.Price);

        private static readonly ReadOnlyArray<Ware> Foods = new ReadOnlyArray<Ware>(Ware.Fish, Ware.Roots, Ware.Berries);
        private static readonly ReadOnlyArray<Ware> Tradeable = new ReadOnlyArray<Ware>(Ware.Berries, Ware.Roots, Ware.Fish, Ware.Log);

        /// <summary>The ware it holds most of (at least <paramref name="min"/>), other than <paramref name="except"/>; None if none.</summary>
        private static Ware Surplus(ThinkScratch sc, Ware except, int min)
        {
            Ware best = Ware.None; int most = min - 1;
            foreach (var w in Tradeable)
            {
                if (w == except) continue;
                int h = Held(sc, w);
                if (w == Ware.Log && !sc.Self.HasBed) continue; // a log is for its own hut first
                if (h > most) { most = h; best = w; }
            }
            return best;
        }

        /// <summary>The largest count of <paramref name="give"/> whose loss to it stays within <paramref name="gain"/> (0 if even one is too dear).</summary>
        private static int GiveCount(ThinkScratch sc, SocialMind m, Ware give, int gain)
        {
            int held = Held(sc, give), k = 0;
            while (k < held && k < 9 && Loss(sc, m, give, k + 1) <= gain) k++;
            return k;
        }

        // ---------------- adding one candidate ----------------

        private static void Add(ThinkScratch sc, SocialMind m, ref PendingCand p, int person, int msgAct, int quality, Ware topic)
        {
            if (m.PendingCount >= SocialMind.MaxPending) return;
            var who = sc.People[person];
            p.Handle = who.Id; p.As = who.As; p.Node = who.Node; p.Dist = who.Dist; p.Quality = (byte)quality;
            int at = Thinker.AddCandidate(sc, p.Verb, IntentTarget.None, p.Handle, p.Node, SocialClass.Person, p.Dist);
            if (at < 0) return;
            p.Cand = at;
            uint rel = Feature.NpcRel((int)Relations.ClassOf(sc, in who));
            uint id = who.As > 0 ? Feature.NpcRef(who.As) : 0;
            uint grp = Feature.NpcGroup(who.SameFaction ? NpcGroupValue.OwnFaction : NpcGroupValue.OtherFaction);
            uint act = msgAct != 0 ? Feature.MsgAct(msgAct) : 0;
            uint q = quality != 0 ? Feature.State(quality) : 0;
            uint cat = topic == Ware.None ? 0 : Feature.Category(Wares.IsFood(topic) ? ObjCategory.Food : ObjCategory.Raw);
            Thinker.TargetFeatures(sc, at, rel, id, grp, act, q, cat);
            m.Pending[m.PendingCount++] = p;
        }

        // ---------------- 1. my side of agreed deals ----------------

        private static void Deliveries(ThinkScratch sc, SocialMind m)
        {
            for (int i = m.DealCount - 1; i >= 0; i--)
            {
                var d = m.Deals[i];
                if (d.Status != DealStatus.Agreed || d.MyGiven >= d.MyCount || d.MyWare == Ware.None) continue;
                int held = Held(sc, d.MyWare);
                if (held <= 0) continue;
                int pi = FindPerson(sc, d.Partner, d.Handle);
                if (pi < 0) continue;
                int step = Math.Min(d.MyCount - d.MyGiven, held);
                if (Trust(m, d.Partner) < m.Tuning.IncrementalTrust) step = 1; // a wary pair trades one unit at a time
                var p = new PendingCand { Verb = SocialVerb.HandOver, DealId = d.DealId, Ware = d.MyWare, Count = step, AnswersMessage = d.MessageId };
                p.Surplus = d.TheirGiven;
                Add(sc, m, ref p, pi, (int)(d.Kind == DealKind.Help ? GestureAct.Cooperate : GestureAct.Accept), 0, d.MyWare);
                return; // one delivery per think
            }
        }

        // ---------------- 2. answers ----------------

        private static int InboxOf(SocialMind m, int messageId)
        {
            for (int i = 0; i < m.InboxCount; i++) if (m.Inbox[i].Msg.MessageId == messageId) return i;
            return -1;
        }

        private static void Answers(ThinkScratch sc, SocialMind m)
        {
            bool warned = false;
            for (int h = 0; h < sc.HeardCount; h++)
            {
                var msg = sc.Heard[h];
                if (msg.Act == GestureAct.Warn && !warned)
                {
                    warned = true;
                    int f = Thinker.AddCandidate(sc, Verb.Flee, IntentTarget.Node, 0, sc.Self.Node, ObjClass.EnemySoldier, 0);
                    if (f >= 0)
                    {
                        Thinker.TargetFeatures(sc, f, Feature.MsgAct((int)GestureAct.Warn), Feature.Category(ObjCategory.Danger));
                        if (m.PendingCount < SocialMind.MaxPending)
                            m.Pending[m.PendingCount++] = new PendingCand { Verb = Verb.Flee, Cand = f, Node = sc.Self.Node, AnswersMessage = msg.MessageId };
                    }
                }
            }
            for (int h = 0; h < sc.HeardCount; h++)
            {
                var msg = sc.Heard[h];
                bool open = msg.Target == 0 && msg.Act == GestureAct.Offer; // an offer to anyone watching
                if (msg.Target != sc.Self.Id && !open) continue;
                int ii = InboxOf(m, msg.MessageId);
                if (ii < 0) continue;
                var e = m.Inbox[ii];
                int pi = -1;
                for (int k = 0; k < sc.PeopleCount; k++) if (sc.People[k].Id == e.Handle && !sc.People[k].Asleep) { pi = k; break; }
                if (pi < 0) continue; // the speaker is out of sight: no answer can be made to it
                if (open)
                {
                    if (AnswerOpenOffer(sc, m, msg, pi)) return;
                    continue;
                }
                switch (msg.Act)
                {
                    case GestureAct.Offer: AnswerOffer(sc, m, msg, pi); return;
                    case GestureAct.NeedFood: AnswerNeed(sc, m, msg, pi, true); return;
                    case GestureAct.Want: AnswerNeed(sc, m, msg, pi, false); return;
                    case GestureAct.NeedHelp: AnswerHelp(sc, m, msg, pi); return;
                }
            }
        }

        private static void AnswerOffer(ThinkScratch sc, SocialMind m, in DecodedMessage msg, int pi)
        {
            int act = (int)GestureAct.Offer;
            var give = msg.Topic.Kind == TopicKind.Ware ? (Ware)msg.Topic.Id : Ware.None; // what they want from me
            int q = Math.Max(1, (int)msg.Quantity);
            var get = msg.Counter.Kind == TopicKind.Ware ? (Ware)msg.Counter.Id : Ware.None; // what they give me
            int g = Math.Max(1, msg.Counter.Sub);
            int di = m.FindDeal(msg.DealId);
            int rounds = di >= 0 ? m.Deals[di].Rounds : 1;
            if (give == Ware.None)
            {
                // an offered gift: take it or not
                int s = get == Ware.None ? 0 : Gain(sc, m, get, g);
                var a = new PendingCand { Verb = SocialVerb.SayAccept, DealId = msg.DealId, AnswersMessage = msg.MessageId, Surplus = s };
                Add(sc, m, ref a, pi, act, Quality(s), get);
                var r = new PendingCand { Verb = SocialVerb.SayRefuse, DealId = msg.DealId, AnswersMessage = msg.MessageId, Surplus = -s };
                Add(sc, m, ref r, pi, act, Quality(s), get);
                return;
            }
            if (Held(sc, give) >= q && get != Ware.None)
            {
                int surplus = Gain(sc, m, get, g) - Loss(sc, m, give, q);
                var a = new PendingCand { Verb = SocialVerb.SayAccept, DealId = msg.DealId, AnswersMessage = msg.MessageId, Surplus = surplus };
                Add(sc, m, ref a, pi, act, Quality(surplus), give);
                var r = new PendingCand { Verb = SocialVerb.SayRefuse, DealId = msg.DealId, AnswersMessage = msg.MessageId, Surplus = -surplus };
                Add(sc, m, ref r, pi, act, Quality(surplus), give);
                if (rounds < m.Tuning.MaxRounds && surplus <= 30 && g < 9)
                {
                    // counter-offer: the same goods, one more of theirs for mine
                    int s2 = Gain(sc, m, get, g + 1) - Loss(sc, m, give, q);
                    var c = new PendingCand
                    {
                        Verb = SocialVerb.SayOffer, Act = GestureAct.Offer, DealId = msg.DealId, AnswersMessage = msg.MessageId, Surplus = s2,
                        Topic = Messages.OfWare(get), Quantity = (byte)(g + 1), Counter = new MsgTopic { Kind = TopicKind.Ware, Id = (int)give, Sub = q },
                    };
                    Add(sc, m, ref c, pi, act, Quality(s2), give);
                }
                return;
            }
            // I lack what they want: refuse, or offer something else for their goods
            var rr = new PendingCand { Verb = SocialVerb.SayRefuse, DealId = msg.DealId, AnswersMessage = msg.MessageId };
            Add(sc, m, ref rr, pi, act, OfferState.Poor, give);
            if (get == Ware.None || rounds >= m.Tuning.MaxRounds) return;
            var x = Surplus(sc, get, 2);
            if (x == Ware.None) return;
            int want = Gain(sc, m, get, g);
            int k = GiveCount(sc, m, x, want);
            if (k <= 0) return;
            int s3 = want - Loss(sc, m, x, k);
            var cc = new PendingCand
            {
                Verb = SocialVerb.SayOffer, Act = GestureAct.Offer, DealId = msg.DealId, AnswersMessage = msg.MessageId, Surplus = s3,
                Topic = Messages.OfWare(get), Quantity = (byte)g, Counter = new MsgTopic { Kind = TopicKind.Ware, Id = (int)x, Sub = k },
            };
            Add(sc, m, ref cc, pi, act, Quality(s3), x);
        }

        /// <summary>An offer to anyone watching: only one who has what is wanted answers, by accepting it (or not at all).</summary>
        private static bool AnswerOpenOffer(ThinkScratch sc, SocialMind m, in DecodedMessage msg, int pi)
        {
            if (msg.Topic.Kind != TopicKind.Ware || msg.Counter.Kind != TopicKind.Ware) return false;
            var give = (Ware)msg.Topic.Id; int q = Math.Max(1, (int)msg.Quantity);
            var get = (Ware)msg.Counter.Id; int g = Math.Max(1, msg.Counter.Sub);
            if (Held(sc, give) < q) return false;
            int surplus = Gain(sc, m, get, g) - Loss(sc, m, give, q);
            if (surplus < -30) return false;
            var a = new PendingCand { Verb = SocialVerb.SayAccept, DealId = msg.DealId, AnswersMessage = msg.MessageId, Surplus = surplus };
            Add(sc, m, ref a, pi, (int)GestureAct.Offer, Quality(surplus), give);
            return true;
        }

        private static void AnswerNeed(ThinkScratch sc, SocialMind m, in DecodedMessage msg, int pi, bool food)
        {
            int act = (int)msg.Act;
            Ware w = Ware.None;
            if (msg.Topic.Kind == TopicKind.Ware) w = (Ware)msg.Topic.Id;
            if (food && (w == Ware.None || !Wares.IsFood(w) || Held(sc, w) == 0))
            {
                int most = 0;
                foreach (var f in Foods) { int h = Held(sc, f); if (h > most) { most = h; w = f; } }
            }
            if (w == Ware.None || Held(sc, w) == 0)
            {
                var r0 = new PendingCand { Verb = SocialVerb.SayRefuse, AnswersMessage = msg.MessageId };
                Add(sc, m, ref r0, pi, act, 0, Ware.None);
                return;
            }
            int count = Math.Min(Math.Max(1, (int)msg.Quantity), Held(sc, w));
            if (food) count = 1;
            var g = new PendingCand { Verb = SocialVerb.Give, Ware = w, Count = count, AnswersMessage = msg.MessageId, Surplus = -Loss(sc, m, w, count) };
            Add(sc, m, ref g, pi, act, 0, w);
            var r = new PendingCand { Verb = SocialVerb.SayRefuse, AnswersMessage = msg.MessageId };
            Add(sc, m, ref r, pi, act, 0, w);
        }

        private static void AnswerHelp(ThinkScratch sc, SocialMind m, in DecodedMessage msg, int pi)
        {
            int act = (int)GestureAct.NeedHelp;
            var w = msg.Topic.Kind == TopicKind.Ware ? (Ware)msg.Topic.Id : Ware.Log;
            int held = Held(sc, w);
            if (held > 0)
            {
                var c = new PendingCand
                {
                    Verb = SocialVerb.SayCooperate, Act = GestureAct.Cooperate, Ware = w, Count = Math.Min(held, Math.Max(1, (int)msg.Quantity)),
                    AnswersMessage = msg.MessageId, Topic = Messages.OfWare(w), Quantity = (byte)Math.Min(held, Math.Max(1, (int)msg.Quantity)),
                };
                Add(sc, m, ref c, pi, act, 0, w);
            }
            var r = new PendingCand { Verb = SocialVerb.SayRefuse, AnswersMessage = msg.MessageId };
            Add(sc, m, ref r, pi, act, 0, w);
        }

        // ---------------- 3. my own messages ----------------

        private static bool InDealWith(SocialMind m, int other)
        {
            if (other == 0) return false;
            return SocialMemory.HasOpenDeal(m, other);
        }

        private static void Initiatives(ThinkScratch sc, SocialMind m, bool child)
        {
            var self = sc.Self;
            var tu = sc.Tuning;
            int ttl = m.CommsTuning != null ? m.CommsTuning.MessageTtlTicks : 60;
            bool hungry = self.Hunger >= tu.BandHigh;
            // ask for food: hungry, carrying none, someone in sight visibly carries food
            if (hungry && !self.CarryFood)
            {
                int best = -1, bestScore = int.MinValue;
                for (int i = 0; i < sc.PeopleCount; i++)
                {
                    var p = sc.People[i];
                    if (p.Asleep || (p.CarryMask & CarryMask.Food) == 0) continue;
                    if (p.As != 0 && p.As == m.LastAskTarget && self.Tick - m.LastAskTick < ttl) continue; // asked a moment ago
                    int ri = m.Find(p.As);
                    int score = (ri >= 0 ? m.Rel[ri].Trust + m.Rel[ri].Affection : 0) - p.Dist;
                    if (score > bestScore) { bestScore = score; best = i; }
                }
                if (best >= 0)
                {
                    var c = new PendingCand { Verb = SocialVerb.SayNeedFood, Act = GestureAct.NeedFood };
                    Add(sc, m, ref c, best, 0, 0, Ware.None);
                }
            }
            if (child) return;
            // offer a trade: something it wants for something it has more of, to someone visibly carrying that kind of good
            Offer(sc, m);
            // offer a gift: sated, a food beyond its horizon, to a household mate or someone it likes
            if (self.Hunger < tu.ComfortHunger)
            {
                Ware f = Ware.None; int most = Valuation.FoodHorizon;
                foreach (var w in Foods) { int h = Held(sc, w); if (h > most) { most = h; f = w; } }
                if (f != Ware.None)
                {
                    int best = -1, bestAff = 100;
                    for (int i = 0; i < sc.PeopleCount; i++)
                    {
                        var p = sc.People[i];
                        if (p.Asleep || p.As == 0 || InDealWith(m, p.As)) continue;
                        int ri = m.Find(p.As);
                        if (ri < 0) continue;
                        int aff = m.Rel[ri].Affection + ((m.Rel[ri].Flags & RelFlags.HouseholdMate) != 0 ? 200 : 0);
                        if (aff > bestAff) { bestAff = aff; best = i; }
                    }
                    if (best >= 0)
                    {
                        var c = new PendingCand
                        {
                            Verb = SocialVerb.SayOffer, Act = GestureAct.Offer, Counter = new MsgTopic { Kind = TopicKind.Ware, Id = (int)f, Sub = 1 },
                            Ware = f, Count = 1, Surplus = -Loss(sc, m, f, 1),
                        };
                        Add(sc, m, ref c, best, 0, OfferState.Fair, f);
                    }
                }
            }
            // ask for help: building a hut with no log, someone in sight carries raw goods
            if (self.HasSite && !self.CarryLog)
            {
                for (int i = 0; i < sc.PeopleCount; i++)
                {
                    var p = sc.People[i];
                    if (p.Asleep || (p.CarryMask & CarryMask.Raw) == 0) continue;
                    if (p.As != 0 && p.As == m.LastAskTarget && self.Tick - m.LastAskTick < ttl) continue;
                    var c = new PendingCand { Verb = SocialVerb.SayNeedHelp, Act = GestureAct.NeedHelp, Topic = Messages.OfWare(Ware.Log), Quantity = 1 };
                    Add(sc, m, ref c, i, 0, 0, Ware.Log);
                    break;
                }
            }
            // warn others: an enemy in sight, people around, not warned a moment ago
            if (self.Tick - m.LastWarnTick > ttl && sc.PeopleCount > 0)
            {
                int enemyNode = -1;
                for (int i = 0; i < sc.SeenCount; i++) if (sc.Seen[i].Category == ObjCategory.Danger) { enemyNode = sc.Seen[i].Node; break; }
                if (enemyNode >= 0)
                {
                    int nearest = -1;
                    for (int i = 0; i < sc.PeopleCount; i++) if (!sc.People[i].Asleep) { nearest = i; break; }
                    if (nearest >= 0)
                    {
                        var c = new PendingCand { Verb = SocialVerb.SayWarn, Act = GestureAct.Warn, Topic = Messages.AtPlace(enemyNode), Time = MsgTime.Now, Count = 0 };
                        c.Ware = Ware.None;
                        Add(sc, m, ref c, nearest, 0, 0, Ware.None);
                        // a warning is broadcast: the handle only names who it looks at
                        if (m.PendingCount > 0 && m.Pending[m.PendingCount - 1].Verb == SocialVerb.SayWarn) m.Pending[m.PendingCount - 1].Broadcast = true;
                    }
                }
            }
            // seek a remembered trading partner: hungry, no food and nobody with food in sight, but goods to trade
            if (hungry && !self.CarryFood && Surplus(sc, Ware.None, 1) != Ware.None)
            {
                bool foodInSight = false;
                for (int i = 0; i < sc.PeopleCount; i++) if ((sc.People[i].CarryMask & CarryMask.Food) != 0) { foodInSight = true; break; }
                if (!foodInSight)
                {
                    int best = -1, bestScore = 0;
                    for (int r = 0; r < m.RelCount; r++)
                    {
                        var rel = m.Rel[r];
                        if (rel.LastNode < 0 || rel.DealsDone == 0 && rel.GiftsIn == 0) continue;
                        int d = sc.Grid.Distance(self.Node, rel.LastNode);
                        if (d <= 1 || d > tu.RecallNodes) continue;
                        bool seen = false;
                        for (int i = 0; i < sc.PeopleCount; i++) if (sc.People[i].As == rel.Other) { seen = true; break; }
                        if (seen) continue;
                        int score = rel.Trust + 100 * rel.DealsDone + 50 * rel.GiftsIn - d * 10;
                        if (best < 0 || score > bestScore) { best = r; bestScore = score; }
                    }
                    if (best >= 0 && m.PendingCount < SocialMind.MaxPending)
                    {
                        var rel = m.Rel[best];
                        int d = sc.Grid.Distance(self.Node, rel.LastNode);
                        int at = Thinker.AddCandidate(sc, SocialVerb.Seek, IntentTarget.None, rel.Other, rel.LastNode, SocialClass.Person, d, true);
                        if (at >= 0)
                        {
                            Thinker.TargetFeatures(sc, at, Feature.NpcRef(rel.Other), Feature.PlaceRef(rel.LastNode));
                            m.Pending[m.PendingCount++] = new PendingCand { Verb = SocialVerb.Seek, Cand = at, Handle = rel.Other, As = rel.Other, Node = rel.LastNode, Dist = d };
                        }
                    }
                }
            }
        }

        /// <summary>A want worth less than this (worth units) is not worth a trade.</summary>
        private const int MinWant = 100;

        /// <summary>It made a deal with this person that was refused or came to nothing a short while ago (it does not ask again at once).</summary>
        private static bool RecentlyFailed(ThinkScratch sc, SocialMind m, int other)
        {
            if (other == 0) return false;
            int cool = m.Tuning.OfferCooldownTicks;
            for (int k = 0; k < m.DealCount; k++)
            {
                var d = m.Deals[k];
                if (d.Partner != other) continue;
                if ((d.Status == DealStatus.Refused || d.Status == DealStatus.Abandoned || d.Status == DealStatus.Failed) && sc.Self.Tick - d.LastTick < cool) return true;
            }
            return false;
        }

        private static void Offer(ThinkScratch sc, SocialMind m)
        {
            var self = sc.Self;
            // one proposal at a time: an offer of mine still waiting for its answer
            for (int k = 0; k < m.DealCount; k++) if (m.Deals[k].Status == DealStatus.Proposed && m.Deals[k].IProposed) return;
            int best = -1, bestTrust = int.MinValue;
            Ware bestWant = Ware.None, bestGive = Ware.None; int bestK = 0, bestSurplus = 0; bool bestKnown = false;
            for (int i = 0; i < sc.PeopleCount; i++)
            {
                var p = sc.People[i];
                if (p.Asleep || p.Stage == LifeStage.Child || InDealWith(m, p.As) || RecentlyFailed(sc, m, p.As)) continue;
                Ware want = Ware.None; int wantGain = 0;
                if ((p.CarryMask & CarryMask.Food) != 0)
                    foreach (var f in Foods)
                    {
                        if (Held(sc, f) >= 2) continue;
                        int g = Gain(sc, m, f, 1);
                        if (g > wantGain) { wantGain = g; want = f; }
                    }
                if (want == Ware.None && !self.HasBed && (p.CarryMask & CarryMask.Raw) != 0 && Held(sc, Ware.Log) == 0) { want = Ware.Log; wantGain = Gain(sc, m, Ware.Log, 1); }
                if (want == Ware.None || wantGain < MinWant) continue;
                var give = Surplus(sc, want, 2);
                if (give == Ware.None) continue;
                int k = GiveCount(sc, m, give, wantGain);
                if (k <= 0) continue;
                // whom it believes to hold the wanted ware (it saw them show or hand it over lately) it addresses; else it asks anyone watching
                int ri = m.Find(p.As);
                bool known = ri >= 0 && (m.Rel[ri].Holds & SocialSim.HoldsBit(want)) != 0 && self.Tick - m.Rel[ri].HoldsTick < 2 * 1200;
                int score = (known ? 10000 : 0) + Trust(m, p.As) - p.Dist;
                if (score > bestTrust) { bestTrust = score; best = i; bestWant = want; bestGive = give; bestK = k; bestSurplus = wantGain - Loss(sc, m, give, k); bestKnown = known; }
            }
            if (best < 0) return;
            var c = new PendingCand
            {
                Verb = SocialVerb.SayOffer, Act = GestureAct.Offer, Topic = Messages.OfWare(bestWant), Quantity = 1,
                Counter = new MsgTopic { Kind = TopicKind.Ware, Id = (int)bestGive, Sub = bestK }, Ware = bestGive, Count = bestK, Surplus = bestSurplus,
                Broadcast = !bestKnown,
            };
            Add(sc, m, ref c, best, 0, Quality(bestSurplus), bestWant);
            if (!bestKnown && m.PendingCount > 0 && m.Pending[m.PendingCount - 1].Verb == SocialVerb.SayOffer) m.Pending[m.PendingCount - 1].Broadcast = true;
        }
    }
}
