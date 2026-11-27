using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;

namespace God.Sim.Social
{
    /// <summary>
    /// M5's sim side (may read and write state; never called from Cognition or Policy): the module's entry points,
    /// perception of people (recognition, contact), delivery of heard messages into each listener's own memory, reading
    /// them at the listener's think, the social acts (gestures, gifts, hand-overs, seeking someone), what a receiver
    /// perceives of a hand-over, the deal checks each mind makes about its own deals, and the daily forgetting pass.
    /// Every belief here changes only from what that NPC perceived or did itself (M5.16); the telemetry
    /// <see cref="DealTrace"/> is written beside the minds and never read by them.
    /// </summary>
    public static class SocialSim
    {
        public const int SystemOrder = 245;

        public static bool On(GodState s) => s.Social != null;

        // ================================================================ module

        public static void StartGame(GodState s)
        {
            if (s.Population == null || s.Tuning.Social.Enabled == 0) return;
            s.Social = new SocialState();
            foreach (var n in s.Population.Npcs) if (Population.Bodies.IsPerson(n)) NewMind(s, n);
        }

        public static void NewMind(GodState s, Npc npc)
        {
            if (s.Social == null || npc.Mind == null) return;
            npc.Mind.Social = new SocialMind(Math.Max(1, s.Tuning.Social.RelSlots), Math.Max(1, s.Tuning.Comms.InboxSlots))
            {
                Tuning = s.Tuning.Social, CommsTuning = s.Tuning.Comms,
            };
        }

        public static void Install(GodSim sim)
        {
            var s = sim.State;
            if (s.Social == null) return;
            BindTuning(s);
            sim.AddSystem(SystemOrder, new SocialSystem());
        }

        /// <summary>The minds decide by the game's tuning (rebound after a load and every day).</summary>
        public static void BindTuning(GodState s)
        {
            foreach (var n in s.Population.Npcs)
            {
                var m = n.Mind?.Social;
                if (m == null || !Population.Bodies.IsPerson(n)) continue;
                m.Tuning = s.Tuning.Social; m.CommsTuning = s.Tuning.Comms;
            }
        }

        public static void Check(GodState s, List<string> bad)
        {
            if (s.Social == null || s.Population == null) return;
            var soc = s.Social;
            for (int i = 1; i < soc.Deals.Count; i++) if (soc.Deals[i].DealId <= soc.Deals[i - 1].DealId) bad.Add("social: deal traces not in ascending id");
            foreach (var n in s.Population.Npcs)
            {
                var m = n.Mind?.Social;
                if (m == null || !Population.Bodies.IsPerson(n)) continue;
                if (m.RelCount < 0 || m.RelCount > m.Rel.Length) bad.Add("social: npc " + n.Id + " RelCount " + m.RelCount);
                if (m.InboxCount < 0 || m.InboxCount > m.Inbox.Length) bad.Add("social: npc " + n.Id + " inbox " + m.InboxCount);
                if (m.DealCount < 0 || m.DealCount > SocialMind.MaxDeals) bad.Add("social: npc " + n.Id + " deals " + m.DealCount);
                for (int i = 0; i < m.RelCount; i++)
                {
                    var r = m.Rel[i];
                    if (r.Other <= 0 || r.Other == n.Id) bad.Add("social: npc " + n.Id + " relation to " + r.Other);
                    if (r.Familiarity < 0 || r.Familiarity > 1000 || r.Trust < -1000 || r.Trust > 1000 || r.Grudge < 0 || r.Grudge > 1000)
                        bad.Add("social: npc " + n.Id + " relation " + r.Other + " out of bounds");
                    for (int j = 0; j < i; j++) if (m.Rel[j].Other == r.Other) bad.Add("social: npc " + n.Id + " two relations to " + r.Other);
                }
            }
        }

        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            // the dead keep their record; others' relations to it fade by the daily pass. Its open deals stay to time out.
        }

        // ================================================================ helpers

        private static int Day(GodState s) => (int)SimCalendar.Of(s).Day(s.Tick);
        private static int Tpd(GodState s) => SimCalendar.Of(s).TicksPerDay;

        private static SocialMind MindOf(Npc n) => n?.Mind?.Social;

        /// <summary>The relation slot of <paramref name="other"/> in <paramref name="npc"/>'s mind, made if needed (eviction may emit <see cref="RelationEvicted"/>); -1 when it stays a stranger.</summary>
        internal static int RelationOf(GodState s, EventBuffer ev, Npc npc, int other)
        {
            var m = MindOf(npc);
            if (m == null || other <= 0 || other == npc.Id) return -1;
            int i = m.Find(other);
            if (i >= 0) return i;
            var o = s.Population.Find(other);
            int start = SocialMemory.StrangerTrust(npc.Traits[Trait.Trustfulness], o != null && o.Faction == npc.Faction);
            i = SocialMemory.Touch(m, other, (int)s.Tick, Tpd(s), start, 0, out var gone, out int imp);
            if (gone.Other != 0)
                ev?.Add(new RelationEvicted { Owner = npc.Id, Evicted = gone.Other, Importance = imp, Trust = gone.Trust, Grudge = gone.Grudge, Familiarity = gone.Familiarity });
            return i;
        }

        private static void Changed(EventBuffer ev, Npc npc, in RelChange c, SocialCause cause, int causeRef)
        {
            if (!c.Changed) return;
            ev?.Add(new TrustChanged
            {
                A = npc.Id, B = c.Other, Before = c.TrustBefore, After = c.TrustAfter, FamBefore = c.FamBefore, FamAfter = c.FamAfter, Cause = cause, CauseRef = causeRef,
            });
        }

        private static RelChange Begin(in Relation r) => new RelChange { Other = r.Other, TrustBefore = r.Trust, FamBefore = r.Familiarity };
        private static void End(ref RelChange c, in Relation r) { c.TrustAfter = r.Trust; c.FamAfter = r.Familiarity; }

        private static LearnTraits Traits(GodState s, Npc n) => PopulationSystem.TraitsOf(s, n);

        private static EventLearningSink Sink(GodState s, EventBuffer ev, Npc n)
        {
            var sink = s.Population.GoodsSink;
            sink.Bind(ev, n.Id, EventLearningSink.IsSampledDay(n.Id, Day(s), s.Tuning.Learning.SampleEvery));
            return sink;
        }

        /// <summary>A social outcome for the decisions waiting on <paramref name="key"/>; when none waits, an ordinary reward.</summary>
        internal static void Outcome(GodState s, EventBuffer ev, Npc n, int key, int reward)
        {
            var st = n.Mind?.Store;
            if (st == null) return;
            if (key != 0 && Learner.Outcome(st, s.Tuning.Learning, Traits(s, n), key, reward, Day(s), Sink(s, ev, n)) > 0) return;
            Learner.AddReward(st, reward);
        }

        /// <summary>Episode keys of M5 (SHARED_CONTRACTS 1): 8 deal, 9 reciprocity with a person, 10 message. Ids wrap below 2^24.</summary>
        public static int DealKey(int dealId) => dealId == 0 ? 0 : EpisodeKey.Make(EpisodeKey.Deal, dealId & EpisodeKey.MaxId);
        public static int MessageKey(int msgId) => msgId == 0 ? 0 : EpisodeKey.Make(EpisodeKey.Message, msgId & EpisodeKey.MaxId);
        public static int ReciprocityKey(int other) => other <= 0 ? 0 : EpisodeKey.Make(EpisodeKey.Reciprocity, other & EpisodeKey.MaxId);

        /// <summary>
        /// The decisions waiting on a deal or a message now wait on the food lot that came of it, so its realised value
        /// reaches them when it is eaten (M3b lot episodes); for other goods the goods channel has paid at once and the
        /// waiting decisions close.
        /// </summary>
        private static void Realise(GodState s, EventBuffer ev, Npc n, int key, Ware w, int lot)
        {
            var st = n.Mind?.Store;
            if (st == null || key == 0) return;
            if (lot != 0 && Goods.CreditedLater(s, w)) SocialLearning.Rekey(st, key, Learner.LotEpisodeKey(lot));
            else Learner.Outcome(st, s.Tuning.Learning, Traits(s, n), key, 0, Day(s), Sink(s, ev, n));
        }

        /// <summary>The bit of <see cref="Relation.Holds"/> for a ware (the tradeable foods and logs), 0 for others.</summary>
        public static byte HoldsBit(Ware w) => w == Ware.Berries ? (byte)1 : w == Ware.Roots ? (byte)2 : w == Ware.Fish ? (byte)4 : w == Ware.Log ? (byte)8 : (byte)0;

        private static bool Night(GodState s) => PopulationSystem.TimeOf(SimCalendar.Of(s), s.Tick) == TimeOfDay.Night;

        // ================================================================ perception of people

        /// <summary>
        /// Recognition (SOCIAL 4.1): whether the observer places a person it knows. A known person is missed with the chance
        /// of <see cref="SocialMemory.MissPermille"/> (it is then a stranger this think: its relation is not read); someone
        /// with no relation is a new face (its true id becomes the new memory's key). The draw is the observer's keyed
        /// stream <c>recognise(observer, tick)</c> per person, so it does not depend on who else is seen or in what order.
        /// </summary>
        private static int Recognise(GodState s, Npc observer, SocialMind m, int seenId, int dist)
        {
            if (m.RecTick != s.Tick) { m.RecTick = s.Tick; m.RecCount = 0; }
            int known = m.RecognisedAs(seenId);
            if (known >= 0) return known;
            int asId = seenId;
            int ri = m.Find(seenId);
            if (ri >= 0)
            {
                bool night = Night(s);
                int sight = night ? s.Tuning.Population.SightNight : s.Tuning.Population.SightDay;
                int miss = SocialMemory.MissPermille(s.Tuning.Social, m.Rel[ri].Familiarity, night, dist * 2 > sight);
                var rng = s.Social.Scratch;
                rng.SetToFork(s.Rng, "recognise", observer.Id, (s.Tick << 20) | (long)(seenId & 0xFFFFF));
                if (rng.NextInt(1000) < miss) asId = 0;
            }
            if (m.RecCount < m.RecTrue.Length) { m.RecTrue[m.RecCount] = seenId; m.RecAs[m.RecCount] = asId; m.RecCount++; }
            return asId;
        }

        /// <summary>The F0 hook before the observer's think: its identity for the seen person (recognition, already drawn at its think tick) and the gesture it visibly makes.</summary>
        public static void FillPerson(GodState s, Npc observer, Npc seen, ref SeenPerson p)
        {
            var m = MindOf(observer);
            if (m == null) return;
            Recognise(s, observer, m, seen.Id, p.Dist);
            var sm = MindOf(seen);
            if (sm != null && Messages.IsSay(seen.Action.Verb) && seen.Action.Verb == sm.Act.Verb && s.Tick - sm.Act.StartTick <= s.Tuning.Social.GestureTicks)
                p.Gesture = (byte)Messages.ActOf(seen.Action.Verb);
        }

        /// <summary>
        /// At the observer's think tick, before it thinks: the people it sees (the same nearest 8 within its sight, by
        /// distance then id, as the population's senses take them from the same positions), recognised once; for each it
        /// places, where it was seen and a day of contact (familiarity); a stranger seen on a second day within 5 days becomes
        /// a relation (SOCIAL 4.1).
        /// </summary>
        private static void Contacts(GodState s, EventBuffer ev, Npc npc)
        {
            var m = MindOf(npc);
            if (npc.Action.Asleep) return;
            var p = s.Population;
            var g = s.Territory.Grid;
            var tu = s.Tuning.Social;
            int sight = Night(s) ? s.Tuning.Population.SightNight : s.Tuning.Population.SightDay;
            var near = s.Social.Near;
            int n = 0;
            foreach (var o in p.Npcs)
            {
                if (!Population.Bodies.IsLivingPerson(o) || o.Id == npc.Id) continue;
                int d = g.Distance(npc.Node, o.Node);
                if (d > sight) continue;
                int at = n;
                while (at > 0 && (near[2 * (at - 1)] > d || near[2 * (at - 1)] == d && near[2 * (at - 1) + 1] > o.Id)) at--;
                if (at >= ThinkScratch.MaxPeople) continue;
                int last = Math.Min(n, ThinkScratch.MaxPeople - 1);
                for (int j = last; j > at; j--) { near[2 * j] = near[2 * (j - 1)]; near[2 * j + 1] = near[2 * (j - 1) + 1]; }
                near[2 * at] = d; near[2 * at + 1] = o.Id;
                if (n < ThinkScratch.MaxPeople) n++;
            }
            int tick = (int)s.Tick, day = Day(s), tpd = Tpd(s);
            for (int k = 0; k < n; k++)
            {
                int other = near[2 * k + 1], dist = near[2 * k];
                var o = p.Find(other);
                int asId = Recognise(s, npc, m, other, dist);
                if (asId == 0) continue; // missed: a stranger now
                int ri = m.Find(other);
                if (ri < 0)
                {
                    if (tu.SecondSighting == 0) continue;
                    bool again = false, today = false;
                    for (int q = 0; q < m.Seen.Length; q++)
                    {
                        if (m.Seen[q].Other != other) continue;
                        if (m.Seen[q].Tick / tpd == day) today = true;
                        else if (tick - m.Seen[q].Tick <= 5 * tpd) again = true;
                    }
                    if (!again)
                    {
                        if (!today) { m.Seen[m.SeenNext] = new SeenEntry { Other = other, Tick = tick }; m.SeenNext = (m.SeenNext + 1) % m.Seen.Length; }
                        continue;
                    }
                    ri = RelationOf(s, ev, npc, other);
                    if (ri < 0) continue;
                }
                ref var r = ref m.Rel[ri];
                r.LastSeenTick = tick; r.LastNode = o.Node;
                if (r.LastContactDay != day)
                {
                    var c = Begin(r);
                    r.LastContactDay = day;
                    SocialMemory.Familiar(ref r, tu.FamDayContact);
                    End(ref c, r);
                    Changed(ev, npc, c, SocialCause.Contact, 0);
                }
            }
        }

        // ================================================================ hearing (at delivery) and reading (at the listener's think)

        /// <summary>A listener perceived a message (called by <see cref="Messages"/> at the gesture): into its inbox; an addressed message from someone it places makes or warms a relation.</summary>
        public static void Heard(GodState s, EventBuffer ev, Npc listener, Npc speaker, int speakerAs, in DecodedMessage msg, bool addressed, bool presented)
        {
            var m = MindOf(listener);
            if (m == null) return;
            if (m.InboxCount == m.Inbox.Length) DropInbox(s, ev, listener, 0);
            m.Inbox[m.InboxCount++] = new InboxEntry { Msg = msg, Handle = speaker.Id, Node = speaker.Node, Addressed = addressed, Presented = presented };
            if (presented && speakerAs != 0 && msg.Counter.Kind == TopicKind.Ware)
            {
                int hi = m.Find(speakerAs);
                if (hi >= 0) { m.Rel[hi].Holds |= HoldsBit((Ware)msg.Counter.Id); m.Rel[hi].HoldsTick = (int)s.Tick; }
            }
            if (addressed && speakerAs != 0)
            {
                int ri = RelationOf(s, ev, listener, speakerAs);
                if (ri >= 0)
                {
                    ref var r = ref m.Rel[ri];
                    var c = Begin(r);
                    SocialMemory.Familiar(ref r, s.Tuning.Social.FamMessage);
                    r.LastSeenTick = (int)s.Tick; r.LastNode = speaker.Node;
                    End(ref c, r);
                    Changed(ev, listener, c, SocialCause.Message, msg.MessageId);
                }
            }
        }

        private static void DropInbox(GodState s, EventBuffer ev, Npc npc, int i)
        {
            var m = MindOf(npc);
            var e = m.Inbox[i];
            if (e.Addressed && !e.Answered && NeedsAnswer(e.Msg.Act))
                ev?.Add(new ResponseChosen { MessageId = e.Msg.MessageId, Listener = npc.Id, Speaker = e.Msg.Speaker, Verb = 0, Ignored = true });
            for (int k = i; k < m.InboxCount - 1; k++) m.Inbox[k] = m.Inbox[k + 1];
            m.InboxCount--;
            m.Inbox[m.InboxCount] = default;
        }

        private static bool NeedsAnswer(GestureAct a) => a == GestureAct.Offer || a == GestureAct.NeedFood || a == GestureAct.Want || a == GestureAct.NeedHelp;

        /// <summary>
        /// At the NPC's think tick (before it thinks): it reads what it heard before this tick and updates its own beliefs
        /// from it (a deal agreed, refused or countered; help promised), forgets messages past their time (an unanswered
        /// addressed one was ignored), and checks its own deals: no answer to its offer, an agreement nobody started, a
        /// partner who took and did not hand over (a betrayal, as it believes), help promised and not given.
        /// </summary>
        public static void AtThink(GodState s, EventBuffer ev, Npc npc)
        {
            var m = MindOf(npc);
            if (m == null) return;
            int tick = (int)s.Tick;
            var ct = s.Tuning.Comms;
            Contacts(s, ev, npc);
            for (int i = 0; i < m.InboxCount; i++)
            {
                ref var e = ref m.Inbox[i];
                if (e.Read || e.Msg.Tick >= tick) continue;
                e.Read = true;
                if (e.Msg.Target == npc.Id || e.Msg.Target == 0 && e.Msg.Act == GestureAct.Offer) Read(s, ev, npc, e);
            }
            for (int i = 0; i < m.InboxCount;)
            {
                if (tick - m.Inbox[i].Msg.Tick > ct.MessageTtlTicks) DropInbox(s, ev, npc, i);
                else i++;
            }
            CheckDeals(s, ev, npc);
            if (m.LastAskMsg != 0 && tick - m.LastAskTick > ct.MessageTtlTicks)
            {
                // asked for food or help, nothing came: the request went unanswered
                Outcome(s, ev, npc, MessageKey(m.LastAskMsg), s.Tuning.Social.Learn.Ignored);
                m.LastAskMsg = 0; m.LastAskTarget = 0;
            }
        }

        private static int NewDealSlot(SocialMind m, int tick)
        {
            if (m.DealCount < m.Deals.Length) return m.DealCount++;
            int old = -1;
            for (int i = 0; i < m.DealCount; i++)
            {
                var st = m.Deals[i].Status;
                if (st == DealStatus.Agreed || st == DealStatus.Proposed) continue;
                if (old < 0 || m.Deals[i].LastTick < m.Deals[old].LastTick) old = i;
            }
            if (old < 0) old = 0; // all open: the oldest gives way
            for (int k = old; k < m.DealCount - 1; k++) m.Deals[k] = m.Deals[k + 1];
            m.Deals[m.DealCount - 1] = default;
            return m.DealCount - 1;
        }

        private static void Read(GodState s, EventBuffer ev, Npc npc, in InboxEntry e)
        {
            var m = MindOf(npc);
            var msg = e.Msg;
            int tick = (int)s.Tick;
            int di = m.FindDeal(msg.DealId);
            switch (msg.Act)
            {
                case GestureAct.Offer:
                {
                    if (di < 0) { di = NewDealSlot(m, tick); m.Deals[di] = new DealMemory { DealId = msg.DealId, OpenedTick = tick, Rounds = 0 }; }
                    ref var d = ref m.Deals[di];
                    d.Partner = msg.Speaker; d.Handle = e.Handle; d.MessageId = msg.MessageId; d.LastTick = tick; d.IProposed = false;
                    d.Status = DealStatus.Proposed; d.Rounds++;
                    d.MyWare = msg.Topic.Kind == TopicKind.Ware ? (Ware)msg.Topic.Id : Ware.None; d.MyCount = msg.Topic.Kind == TopicKind.Ware ? (byte)Math.Max(1, (int)msg.Quantity) : (byte)0;
                    d.TheirWare = msg.Counter.Kind == TopicKind.Ware ? (Ware)msg.Counter.Id : Ware.None; d.TheirCount = (byte)Math.Max(0, msg.Counter.Sub);
                    d.Kind = d.MyWare == Ware.None ? DealKind.Gift : DealKind.Spot;
                    break;
                }
                case GestureAct.Accept:
                    if (di >= 0 && m.Deals[di].Status == DealStatus.Proposed && m.Deals[di].IProposed)
                    {
                        // my offer accepted (to anyone watching: the first who accepts is my partner)
                        m.Deals[di].Status = DealStatus.Agreed; m.Deals[di].LastTick = tick; m.Deals[di].Handle = e.Handle;
                        if (m.Deals[di].Partner == 0) m.Deals[di].Partner = msg.Speaker;
                    }
                    break;
                case GestureAct.Refuse:
                    if (di >= 0 && m.Deals[di].Status == DealStatus.Proposed && m.Deals[di].IProposed)
                    {
                        m.Deals[di].Status = DealStatus.Refused; m.Deals[di].LastTick = tick;
                        RefusedBy(s, ev, npc, msg.Speaker, msg.MessageId);
                        Outcome(s, ev, npc, DealKey(msg.DealId), s.Tuning.Social.Learn.Refused);
                    }
                    else if (m.LastAskMsg != 0 && (msg.Speaker == m.LastAskTarget || msg.Speaker == 0))
                    {
                        RefusedBy(s, ev, npc, msg.Speaker, msg.MessageId);
                        Outcome(s, ev, npc, MessageKey(m.LastAskMsg), s.Tuning.Social.Learn.Refused);
                        m.LastAskMsg = 0; m.LastAskTarget = 0;
                    }
                    break;
                case GestureAct.Cooperate:
                    // they promise help I asked for: I expect it from them (my belief; nothing makes them keep it)
                    if (di < 0) di = NewDealSlot(m, tick);
                    m.Deals[di] = new DealMemory
                    {
                        DealId = msg.DealId, Partner = msg.Speaker, Handle = e.Handle, MessageId = msg.MessageId, Cause = m.LastAskMsg, OpenedTick = tick, LastTick = tick,
                        Kind = DealKind.Help, Status = DealStatus.Agreed, TheirWare = msg.Topic.Kind == TopicKind.Ware ? (Ware)msg.Topic.Id : Ware.None,
                        TheirCount = (byte)Math.Max(1, (int)msg.Quantity),
                    };
                    break;
            }
        }

        private static void RefusedBy(GodState s, EventBuffer ev, Npc npc, int other, int msgId)
        {
            var m = MindOf(npc);
            int ri = RelationOf(s, ev, npc, other);
            if (ri < 0) return;
            var c = Begin(m.Rel[ri]);
            SocialMemory.Refused(s.Tuning.Social, ref m.Rel[ri], (int)s.Tick);
            End(ref c, m.Rel[ri]);
            Changed(ev, npc, c, SocialCause.Refused, msgId);
        }

        private static int WorthOf(SocialMind m, Ware w, int count) => count * Valuation.PriceOf(m.Price, w);

        private static void CheckDeals(GodState s, EventBuffer ev, Npc npc)
        {
            var m = MindOf(npc);
            var tu = s.Tuning.Social;
            int tick = (int)s.Tick, ttl = s.Tuning.Comms.MessageTtlTicks;
            for (int i = 0; i < m.DealCount; i++)
            {
                ref var d = ref m.Deals[i];
                if (d.Status == DealStatus.Proposed && !d.IProposed && tick - d.LastTick > ttl) { d.Status = DealStatus.Abandoned; continue; }
                if (d.Status == DealStatus.Proposed && d.IProposed && tick - d.LastTick > ttl)
                {
                    d.Status = DealStatus.Abandoned; // no answer perceived
                    Outcome(s, ev, npc, DealKey(d.DealId), tu.Learn.Ignored);
                    continue;
                }
                if (d.Status != DealStatus.Agreed) continue;
                bool iDone = d.MyGiven >= d.MyCount, theyDone = d.TheirGiven >= d.TheirCount;
                if (d.MyGiven > 0 && !theyDone && tick - d.LastTick > tu.DealTimeoutTicks)
                {
                    // I handed over and they have not: as I believe it, they took and left
                    d.Status = DealStatus.Failed;
                    int v = Valuation.ValueUnits(WorthOf(m, d.MyWare, d.MyGiven));
                    int sev = SocialMemory.SeverityOf(v);
                    Betrayal(s, ev, npc, d.Partner, sev, v, d.DealId, 1);
                    Outcome(s, ev, npc, DealKey(d.DealId), tu.Learn.Betrayed);
                    m.DayCheated++;
                    continue;
                }
                if (d.Kind == DealKind.Help && d.MyCount == 0 && d.TheirGiven == 0 && tick - d.LastTick > tu.DealTimeoutTicks)
                {
                    d.Status = DealStatus.Failed;
                    Betrayal(s, ev, npc, d.Partner, tu.UnkeptHelpSeverity, 0, d.DealId, 2);
                    Outcome(s, ev, npc, MessageKey(d.Cause), tu.Learn.Refused);
                    continue;
                }
                if (d.MyGiven == 0 && d.TheirGiven == 0 && tick - d.LastTick > tu.DealAbandonTicks) { d.Status = DealStatus.Abandoned; continue; }
                // they handed over and I kept it all this while: in my own records the deal is over (I took it)
                if (!iDone && d.TheirGiven > 0 && tick - d.LastTick > tu.DealTimeoutTicks) { d.Status = DealStatus.Abandoned; continue; }
                if (iDone && theyDone) d.Status = DealStatus.Done;
            }
        }

        private static void Betrayal(GodState s, EventBuffer ev, Npc npc, int other, int sev, int value, int dealId, byte kind)
        {
            var m = MindOf(npc);
            ev?.Add(new BetrayalRecorded { Victim = npc.Id, Offender = other, Kind = kind, Severity = sev, Value = value, DealId = dealId });
            int ri = RelationOf(s, ev, npc, other);
            if (ri < 0) return;
            var c = Begin(m.Rel[ri]);
            SocialMemory.Betrayed(ref m.Rel[ri], sev, (int)s.Tick);
            if (kind == 1 && m.Rel[ri].Cheated < 255) m.Rel[ri].Cheated++;
            End(ref c, m.Rel[ri]);
            Changed(ev, npc, c, SocialCause.Betrayal, dealId);
        }

        // ================================================================ the acts

        private static bool Ends(Npc npc)
        {
            npc.Action = new NpcAction { Since = npc.Action.Since };
            npc.Path = null; npc.PathGoal = -1;
            if (npc.Mind != null) Learner.EndOngoing(npc.Mind.Store);
            var m = MindOf(npc);
            if (m != null) m.Act = default;
            return true;
        }

        private static bool Frustrate(GodState s, Npc npc)
        {
            if (npc.Mind != null) Learner.AddReward(npc.Mind.Store, s.Tuning.Learning.Frustration);
            return Ends(npc);
        }

        private static void Answered(GodState s, EventBuffer ev, Npc npc, int messageId, byte verb)
        {
            if (messageId == 0) return;
            var m = MindOf(npc);
            for (int i = 0; i < m.InboxCount; i++)
            {
                if (m.Inbox[i].Msg.MessageId != messageId) continue;
                if (m.Inbox[i].Answered) return;
                m.Inbox[i].Answered = true;
                ev?.Add(new ResponseChosen { MessageId = messageId, Listener = npc.Id, Speaker = m.Inbox[i].Msg.Speaker, Verb = verb, Ignored = false });
                return;
            }
        }

        private static DealTrace OpenTrace(GodState s, EventBuffer ev, Npc a, int b, int dealId, DealKind kind, int messageId, Ware aWare, int aCount, Ware bWare, int bCount)
        {
            var t = new DealTrace
            {
                DealId = dealId, A = a.Id, B = b, Kind = kind, OpenedTick = (int)s.Tick, LastStepTick = (int)s.Tick, MessageId = messageId,
                AWare = aWare, ACount = aCount, BWare = bWare, BCount = bCount, Agreed = kind != DealKind.Spot,
            };
            s.Social.Deals.Add(t);
            var o = s.Population.Find(b);
            ev?.Add(new ExchangeOpened
            {
                DealId = dealId, A = a.Id, B = b, Kind = kind, MessageId = messageId, AWare = aWare, ACount = aCount, BWare = bWare, BCount = bCount,
                SameHousehold = o != null && a.Household != 0 && a.Household == o.Household,
            });
            return t;
        }

        /// <summary>Closes a deal trace with its outcome and each side's believed gain (telemetry).</summary>
        internal static void CloseTrace(GodState s, EventBuffer ev, DealTrace t, DealOutcome outcome)
        {
            t.Outcome = outcome;
            var p = s.Population;
            var a = p.Find(t.A); var b = p.Find(t.B);
            t.AGain = BelievedGain(a, t.BWare, t.BGiven, t.AWare, t.AGiven);
            t.BGain = BelievedGain(b, t.AWare, t.AGiven, t.BWare, t.BGiven);
            ev?.Add(new ExchangeClosed { DealId = t.DealId, Outcome = outcome, A = t.A, B = t.B, AGiven = t.AGiven, BGiven = t.BGiven, AGain = t.AGain, BGain = t.BGain });
            s.Social.Deals.Remove(t);
        }

        /// <summary>The worth one side believes it gained: what it got minus what it gave, valued by its own valuation now.</summary>
        private static int BelievedGain(Npc n, Ware got, int gotCount, Ware gave, int gaveCount)
        {
            var m = MindOf(n);
            if (m == null) return 0;
            int hunger = n.DriveOf(Drive.Hunger);
            bool bed = n.HomeHut != 0, tool = false;
            foreach (var l in n.Carry) if (!l.Empty && l.Ware == Ware.Axe) tool = true;
            int g = got == Ware.None || gotCount <= 0 ? 0 : Valuation.Gain(got, gotCount, Math.Max(0, PopulationModule.CountOf(n, got) - gotCount), hunger, bed, tool, m.Price);
            int l2 = gave == Ware.None || gaveCount <= 0 ? 0 : Valuation.Gain(gave, gaveCount, PopulationModule.CountOf(n, gave), hunger, bed, tool, m.Price);
            return g - l2;
        }

        public static bool TryStart(GodState s, EventBuffer ev, Npc npc, in Candidate c, int day)
        {
            var m = MindOf(npc);
            if (m == null) return false;
            if (c.Verb == Verb.Flee)
            {
                int fi = m.PendingOf(Verb.Flee, 0);
                if (fi >= 0) Answered(s, ev, npc, m.Pending[fi].AnswersMessage, Verb.Flee);
                return false; // M3's Act runs the flight
            }
            if (!SocialVerb.IsSocial(c.Verb)) return false;
            int pi = m.PendingOf(c.Verb, c.TargetId);
            // re-choosing the running walk (to give or to seek) continues it
            if (m.Act.Verb == c.Verb && m.Act.Handle == c.TargetId && (c.Verb == SocialVerb.Give || c.Verb == SocialVerb.HandOver || c.Verb == SocialVerb.Seek)) return true;
            if (pi < 0) return Frustrate(s, npc);
            var p = m.Pending[pi];
            int tick = (int)s.Tick;
            m.Act = new SocialAct { Verb = c.Verb, Handle = p.Handle, TargetAs = p.As, DealId = p.DealId, Ware = p.Ware, Count = p.Count, StartTick = tick };
            Answered(s, ev, npc, p.AnswersMessage, c.Verb);
            var soc = s.Social;
            if (c.Verb == SocialVerb.Seek || c.Verb == SocialVerb.HandOver) return true;
            if (c.Verb == SocialVerb.Give)
            {
                int dealId = soc.NextDealId++;
                int di = NewDealSlot(m, tick);
                m.Deals[di] = new DealMemory
                {
                    DealId = dealId, Partner = p.As, Handle = p.Handle, Cause = p.AnswersMessage, OpenedTick = tick, LastTick = tick, Kind = DealKind.Gift,
                    Status = DealStatus.Agreed, IProposed = true, MyWare = p.Ware, MyCount = (byte)p.Count,
                };
                m.Act.DealId = dealId;
                OpenTrace(s, ev, npc, p.Handle, dealId, DealKind.Gift, p.AnswersMessage, p.Ware, p.Count, Ware.None, 0);
                return true;
            }
            // ---- a gesture: compose it from its parts, update my own memory of what I said, make it ----
            var act = Messages.ActOf(c.Verb);
            int target = p.Broadcast ? 0 : p.Handle;
            int dealOf = p.DealId;
            switch (act)
            {
                case GestureAct.Offer:
                {
                    var give = p.Counter.Kind == TopicKind.Ware ? (Ware)p.Counter.Id : Ware.None;
                    var get = p.Topic.Kind == TopicKind.Ware ? (Ware)p.Topic.Id : Ware.None;
                    int di = m.FindDeal(dealOf);
                    if (dealOf == 0 || di < 0)
                    {
                        dealOf = soc.NextDealId++;
                        di = NewDealSlot(m, tick);
                        m.Deals[di] = new DealMemory { DealId = dealOf, OpenedTick = tick };
                        OpenTrace(s, ev, npc, p.Broadcast ? 0 : p.Handle, dealOf, get == Ware.None ? DealKind.Gift : DealKind.Spot, 0, give, p.Counter.Sub, get, p.Quantity);
                    }
                    else
                    {
                        var t = soc.Trace(dealOf);
                        if (t != null)
                        {
                            if (t.A == npc.Id) { t.AWare = give; t.ACount = p.Counter.Sub; t.BWare = get; t.BCount = p.Quantity; }
                            else { t.BWare = give; t.BCount = p.Counter.Sub; t.AWare = get; t.ACount = p.Quantity; }
                        }
                    }
                    ref var d = ref m.Deals[di];
                    d.Partner = p.Broadcast ? 0 : p.As; d.Handle = p.Broadcast ? 0 : p.Handle; d.IProposed = true; d.Status = DealStatus.Proposed; d.Rounds++; d.LastTick = tick;
                    d.MyWare = give; d.MyCount = (byte)Math.Max(0, p.Counter.Sub); d.TheirWare = get; d.TheirCount = get == Ware.None ? (byte)0 : (byte)Math.Max(1, (int)p.Quantity);
                    d.Kind = get == Ware.None ? DealKind.Gift : DealKind.Spot;
                    m.Act.DealId = dealOf;
                    break;
                }
                case GestureAct.Accept:
                {
                    int di = m.FindDeal(dealOf);
                    if (di >= 0) { m.Deals[di].Status = DealStatus.Agreed; m.Deals[di].LastTick = tick; m.Deals[di].Handle = p.Handle; }
                    var t = soc.Trace(dealOf);
                    // an offer to anyone watching: the first to accept is its other side (telemetry)
                    if (t != null) { if (t.B == 0 && t.A != npc.Id) t.B = npc.Id; t.Agreed = true; t.LastStepTick = tick; }
                    break;
                }
                case GestureAct.Refuse:
                {
                    int di = m.FindDeal(dealOf);
                    if (di >= 0) { m.Deals[di].Status = DealStatus.Refused; m.Deals[di].LastTick = tick; }
                    var t = soc.Trace(dealOf);
                    if (t != null) CloseTrace(s, ev, t, DealOutcome.Refused);
                    break;
                }
                case GestureAct.Cooperate:
                {
                    dealOf = soc.NextDealId++;
                    int di = NewDealSlot(m, tick);
                    m.Deals[di] = new DealMemory
                    {
                        DealId = dealOf, Partner = p.As, Handle = p.Handle, Cause = p.AnswersMessage, OpenedTick = tick, LastTick = tick, Kind = DealKind.Help,
                        Status = DealStatus.Agreed, IProposed = true, MyWare = p.Ware, MyCount = (byte)p.Count,
                    };
                    OpenTrace(s, ev, npc, p.Handle, dealOf, DealKind.Help, p.AnswersMessage, p.Ware, p.Count, Ware.None, 0);
                    m.Act.DealId = dealOf;
                    break;
                }
                case GestureAct.NeedFood: case GestureAct.NeedHelp: case GestureAct.Want:
                    m.LastAskTarget = p.As; m.LastAskTick = tick;
                    break;
                case GestureAct.Warn:
                    m.LastWarnTick = tick;
                    break;
            }
            bool presented = p.Counter.Kind == TopicKind.Ware && PopulationModule.CountOf(npc, (Ware)p.Counter.Id) > 0;
            var msg = Messages.Compose(act, p.Topic, p.Quantity, p.Counter, target, p.Time, dealOf, presented);
            int id = Messages.Emit(s, ev, npc, msg);
            m.Act.MessageId = id;
            if (act == GestureAct.NeedFood || act == GestureAct.NeedHelp || act == GestureAct.Want) m.LastAskMsg = id;
            int dj = m.FindDeal(dealOf);
            if (dj >= 0) m.Deals[dj].MessageId = id;
            var tr = soc.Trace(dealOf);
            if (tr != null && tr.MessageId == 0) tr.MessageId = id;
            return true;
        }

        public static bool TryAct(GodState s, EventBuffer ev, Npc npc, int day)
        {
            var a = npc.Action;
            if (!SocialVerb.IsSocial(a.Verb)) return false;
            var m = MindOf(npc);
            if (m == null) return Ends(npc);
            var tu = s.Tuning.Social;
            if (Messages.IsSay(a.Verb))
            {
                if (s.Tick >= m.Act.StartTick + tu.GestureTicks) Ends(npc);
                return true;
            }
            if (a.Verb == SocialVerb.Seek)
            {
                if (SocialWalk.Step(s, npc, a.TargetNode, Reach.Touch) || npc.Path == null) Ends(npc);
                return true;
            }
            // Give or HandOver: walk to the person, then hand the goods over (one Goods.Move by this actor)
            var p = s.Population;
            var target = p.Find(m.Act.Handle);
            int di = m.FindDeal(m.Act.DealId);
            if (target == null || !target.Alive || di < 0 || m.Deals[di].Status != DealStatus.Agreed) return Frustrate(s, npc);
            if (s.Tick - a.Since > tu.HandOverWalkTicks) return Frustrate(s, npc);
            var g = s.Territory.Grid;
            if (g.Distance(npc.Node, target.Node) > Reach.HandOver)
            {
                SocialWalk.Step(s, npc, target.Node, Reach.HandOver);
                if (npc.Path == null && g.Distance(npc.Node, target.Node) > Reach.HandOver) return Frustrate(s, npc);
                if (g.Distance(npc.Node, target.Node) > Reach.HandOver) return true;
            }
            ref var d = ref m.Deals[di];
            int lotId = 0, have = 0;
            foreach (var l in npc.Carry)
                if (!l.Empty && l.Ware == m.Act.Ware && l.Owner.Kind == OwnerKind.Npc && l.Owner.Id == npc.Id && l.Count > have) { lotId = l.Id; have = l.Count; }
            int count = Math.Min(Math.Min(m.Act.Count, have), Math.Max(0, d.MyCount - d.MyGiven));
            if (lotId == 0 || count <= 0) return Frustrate(s, npc);
            var basis = d.Kind == DealKind.Spot ? OwnershipBasis.Exchanged : OwnershipBasis.Gift;
            var r = Goods.Move(s, ev, Holder.Carry(npc.Id), lotId, count, Holder.Carry(target.Id), MoveReason.HandOver, npc.Id, OwnerRule.ToReceiver,
                purposeRef: d.DealId, basis: basis);
            if (!r.Ok) return Frustrate(s, npc);
            int tick = (int)s.Tick;
            d.MyGiven = (byte)Math.Min(255, d.MyGiven + count); d.LastTick = tick;
            if (d.Partner == 0) d.Partner = target.Id; // close enough to hand over: it knows whom it gave to
            var t = s.Social.Trace(d.DealId);
            int step = 1;
            if (t != null)
            {
                if (t.A == npc.Id) t.AGiven += count; else if (t.B == npc.Id) t.BGiven += count;
                t.Steps++; step = t.Steps; t.LastStepTick = tick;
            }
            if (d.Kind == DealKind.Spot)
                ev?.Add(new ExchangeStep { DealId = d.DealId, Step = step, Giver = npc.Id, Receiver = target.Id, Lot = r.Lot, Ware = m.Act.Ware, Count = count, Node = npc.Node });
            else
                ev?.Add(new GiftGiven
                {
                    DealId = d.DealId, Step = step, Giver = npc.Id, Receiver = target.Id, Lot = r.Lot, Ware = m.Act.Ware, Count = count, Node = npc.Node,
                    GiverKinBand = KinData.BandOf(npc.Mind, target.Id), SameHousehold = npc.Household != 0 && npc.Household == target.Household, Answers = d.Cause,
                });
            // the giver's own memory: it knows what it gave
            m.DayGiven++;
            int ri = RelationOf(s, ev, npc, d.Partner != 0 ? d.Partner : target.Id);
            if (ri >= 0)
            {
                ref var rel = ref m.Rel[ri];
                var c = Begin(rel);
                SocialMemory.Familiar(ref rel, tu.FamInteraction);
                if (d.Kind != DealKind.Spot) SocialMemory.GiftGiven(ref rel, Valuation.ValueUnits(WorthOf(m, m.Act.Ware, count)), tick);
                End(ref c, rel);
                Changed(ev, npc, c, d.Kind == DealKind.Spot ? SocialCause.Handover : SocialCause.GiftGiven, d.DealId);
            }
            Completed(s, ev, npc, di);
            if (t != null && t.AGiven >= t.ACount && t.BGiven >= t.BCount) CloseTrace(s, ev, t, DealOutcome.Completed);
            return Ends(npc);
        }

        /// <summary>Both sides of my deal have passed (as I saw it): a completed barter in my memory, and the trade teaches my price beliefs.</summary>
        private static void Completed(GodState s, EventBuffer ev, Npc npc, int di)
        {
            var m = MindOf(npc);
            ref var d = ref m.Deals[di];
            if (d.Status != DealStatus.Agreed || d.MyGiven < d.MyCount || d.TheirGiven < d.TheirCount) return;
            d.Status = DealStatus.Done;
            m.DayDeals++;
            if (d.Kind != DealKind.Spot) return;
            Valuation.Observe(m.Price, d.MyWare, d.MyCount, d.TheirWare, d.TheirCount, 4);
            int ri = RelationOf(s, ev, npc, d.Partner);
            if (ri < 0) return;
            var c = Begin(m.Rel[ri]);
            SocialMemory.BarterCompleted(s.Tuning.Social, ref m.Rel[ri], (int)s.Tick);
            End(ref c, m.Rel[ri]);
            Changed(ev, npc, c, SocialCause.Barter, d.DealId);
        }

        /// <summary>
        /// Test fixture (<c>SocialFixtures.Meet</c>): <paramref name="giver"/> gives one of its own lots to an adjacent
        /// <paramref name="receiver"/> as a gift, through the same path as a decided gift (a gift deal, one
        /// <c>Goods.Move</c> HandOver, <see cref="GiftGiven"/>, both minds' memories). Not called by the sim.
        /// </summary>
        public static bool GiftNow(GodState s, EventBuffer ev, Npc giver, Npc receiver, Ware ware, int count)
        {
            var m = MindOf(giver);
            if (m == null) return false;
            int tick = (int)s.Tick;
            int dealId = s.Social.NextDealId++;
            int di = NewDealSlot(m, tick);
            m.Deals[di] = new DealMemory
            {
                DealId = dealId, Partner = receiver.Id, Handle = receiver.Id, OpenedTick = tick, LastTick = tick, Kind = DealKind.Gift,
                Status = DealStatus.Agreed, IProposed = true, MyWare = ware, MyCount = (byte)count,
            };
            OpenTrace(s, ev, giver, receiver.Id, dealId, DealKind.Gift, 0, ware, count, Ware.None, 0);
            var saved = giver.Action;
            giver.Action = new NpcAction { Verb = SocialVerb.Give, TargetKind = IntentTarget.Npc, TargetId = receiver.Id, TargetNode = receiver.Node, Since = s.Tick };
            m.Act = new SocialAct { Verb = SocialVerb.Give, Handle = receiver.Id, TargetAs = receiver.Id, DealId = dealId, Ware = ware, Count = count, StartTick = tick };
            TryAct(s, ev, giver, Day(s));
            bool ok = m.FindDeal(dealId) >= 0 && m.Deals[m.FindDeal(dealId)].MyGiven >= count;
            giver.Action = saved;
            return ok;
        }

        // ================================================================ a hand-over, as the receiver perceives it

        /// <summary>
        /// Every hand-over between two living people (A3; <c>Goods.Move</c> calls it): the receiver saw it. Of an M5 deal,
        /// its memory of the deal moves on (a completed barter, a gift received, a promised help kept); any other hand-over
        /// (M4's feeding) is help received. Its learning: the decisions that waited on the deal or on its request for food
        /// now wait on the lot (realised value), and a gift back from someone it gave to closes that reciprocity episode.
        /// </summary>
        public static void OnHandOver(GodState s, EventBuffer ev, in HandOver h)
        {
            if (s.Social == null) return;
            var p = s.Population;
            var rec = p.Find(h.Receiver);
            var m = MindOf(rec);
            if (m == null) return;
            var tu = s.Tuning.Social;
            int tick = (int)s.Tick;
            var t = h.PurposeRef != 0 ? s.Social.Trace(h.PurposeRef) : null;
            bool ours = t != null && (t.A == h.Giver || t.B == h.Giver);
            int giverAs = h.Giver; // close enough to touch: recognised
            m.DayReceived++;
            int value = Valuation.ValueUnits(h.Count * Valuation.PriceOf(m.Price, h.Ware));
            int di = ours ? m.FindDeal(h.PurposeRef) : -1;
            int ri = RelationOf(s, ev, rec, giverAs);
            if (ri >= 0) { m.Rel[ri].Holds |= HoldsBit(h.Ware); m.Rel[ri].HoldsTick = tick; }
            RelChange c = ri >= 0 ? Begin(m.Rel[ri]) : default;
            if (ri >= 0) SocialMemory.Familiar(ref m.Rel[ri], tu.FamInteraction);
            if (di >= 0 && m.Deals[di].Kind == DealKind.Spot)
            {
                ref var d = ref m.Deals[di];
                d.TheirGiven = (byte)Math.Min(255, d.TheirGiven + h.Count); d.LastTick = tick;
                if (ri >= 0) { End(ref c, m.Rel[ri]); Changed(ev, rec, c, SocialCause.Handover, h.PurposeRef); }
                Realise(s, ev, rec, DealKey(d.DealId), h.Ware, h.Lot);
                Completed(s, ev, rec, di);
            }
            else
            {
                // a gift, help, an accepted gift offer, or care from M4's feeding
                if (di >= 0) { m.Deals[di].TheirGiven = (byte)Math.Min(255, m.Deals[di].TheirGiven + h.Count); m.Deals[di].LastTick = tick; }
                if (ri >= 0)
                {
                    SocialMemory.GiftReceived(tu, ref m.Rel[ri], value, tick);
                    End(ref c, m.Rel[ri]);
                    Changed(ev, rec, c, ours ? SocialCause.GiftReceived : SocialCause.Care, ours ? h.PurposeRef : h.Lot);
                }
                if (ours)
                {
                    Realise(s, ev, rec, DealKey(h.PurposeRef), h.Ware, h.Lot);
                    if (m.LastAskMsg != 0 && (m.LastAskTarget == giverAs || m.LastAskTarget == 0) && tick - m.LastAskTick <= s.Tuning.Comms.MessageTtlTicks * 4)
                    {
                        // my request was answered: the asking learns what it brought
                        Realise(s, ev, rec, MessageKey(m.LastAskMsg), h.Ware, h.Lot);
                        m.LastAskMsg = 0; m.LastAskTarget = 0;
                    }
                    if (di >= 0 && m.Deals[di].Kind == DealKind.Help && m.Deals[di].Cause != 0) Realise(s, ev, rec, MessageKey(m.Deals[di].Cause), h.Ware, h.Lot);
                    // someone I gave to gives to me: reciprocity
                    if (ri >= 0 && m.Rel[ri].HelpGiven > 0)
                        Outcome(s, ev, rec, ReciprocityKey(giverAs), tu.Learn.Reciprocated);
                    if (di >= 0) Completed(s, ev, rec, di);
                }
            }
        }

        // ================================================================ episode keys, time use

        public static int EpisodeKeyOf(Npc npc, in Candidate c)
        {
            var m = MindOf(npc);
            if (m == null || !SocialVerb.IsSocial(c.Verb) || m.Act.Verb != c.Verb) return 0;
            switch (c.Verb)
            {
                case SocialVerb.HandOver: case SocialVerb.SayOffer: case SocialVerb.SayAccept: case SocialVerb.SayRefuse: return DealKey(m.Act.DealId);
                case SocialVerb.SayNeedFood: case SocialVerb.SayNeedHelp: case SocialVerb.SayWant: return MessageKey(m.Act.MessageId);
                case SocialVerb.Give: case SocialVerb.SayCooperate: return ReciprocityKey(m.Act.TargetAs);
                default: return 0;
            }
        }

        public static int ActivityOf(byte verb) => SocialVerb.IsSocial(verb) ? Activity.Social : Activity.Other;

        // ================================================================ the daily pass

        /// <summary>
        /// Day start (SOCIAL 4.7): the social day row of the day that ended, the flags and kin cue refreshed from M4's reads
        /// (or hut rights alone), every relation's forgetting (familiarity, grudge, trust drift, fear), old deals forgotten.
        /// </summary>
        public static void DayPass(GodState s, EventBuffer ev, int day)
        {
            var soc = s.Social;
            bool rowDue = soc.DayDone >= 0;
            soc.DayDone = day;
            BindTuning(s);
            var p = s.Population;
            var d = s.Tuning.Social.Decay;
            int tick = (int)s.Tick, tpd = Tpd(s);
            bool households = false;
            foreach (var n in p.Npcs) if (Population.Bodies.IsLivingPerson(n) && n.Household != 0) { households = true; break; }
            foreach (var n in p.Npcs)
            {
                var m = MindOf(n);
                if (!Population.Bodies.IsLivingPerson(n) || m == null) continue;
                if (rowDue) Row(ev, n, m, day - 1);
                for (int i = 0; i < m.RelCount; i++)
                {
                    ref var r = ref m.Rel[i];
                    var o = p.Find(r.Other);
                    r.KinCue = (byte)Math.Min(255, KinData.RHat(n.Mind, r.Other) / 2);
                    byte flags = 0;
                    if (KinData.RoleOf(n.Mind, r.Other) == KinRole.Partner) flags |= RelFlags.Partner;
                    if (o != null && o.Alive && (households ? n.Household != 0 && o.Household == n.Household : n.HomeHut != 0 && o.HomeHut == n.HomeHut)) flags |= RelFlags.HouseholdMate;
                    r.Flags = flags;
                    int prior = SocialMemory.StrangerTrust(n.Traits[Trait.Trustfulness], o != null && o.Faction == n.Faction);
                    SocialMemory.DayPass(d, ref r, n.Traits[Trait.Vengefulness], r.LastContactDay == day - 1, prior);
                }
                for (int i = 0; i < m.DealCount;)
                {
                    var st = m.Deals[i].Status;
                    bool closed = st != DealStatus.Agreed && st != DealStatus.Proposed;
                    if (closed && tick - m.Deals[i].LastTick > 2 * tpd)
                    {
                        for (int k = i; k < m.DealCount - 1; k++) m.Deals[k] = m.Deals[k + 1];
                        m.DealCount--; m.Deals[m.DealCount] = default;
                    }
                    else i++;
                }
                m.DaySent = 0; m.DayHeard = 0; m.DayMisread = 0; m.DayGiven = 0; m.DayReceived = 0; m.DayDeals = 0; m.DayCheated = 0;
            }
        }

        private static void Row(EventBuffer ev, Npc n, SocialMind m, int day)
        {
            int sum = 0, friends = 0, enemies = 0, grudge = 0;
            for (int i = 0; i < m.RelCount; i++)
            {
                var r = m.Rel[i];
                sum += r.Trust; grudge += r.Grudge;
                var k = SocialMemory.ClassOf(m, r);
                if (k == NpcRelClass.Friend) friends++;
                if (k == NpcRelClass.Enemy) enemies++;
            }
            ev?.Add(new PersonDaySocial
            {
                Npc = n.Id, Day = day, RelCount = m.RelCount, MeanTrust = m.RelCount > 0 ? sum / m.RelCount : 0, Friends = friends, Enemies = enemies, GrudgeSum = grudge,
                Sent = m.DaySent, Received = m.DayHeard, Misread = m.DayMisread, GiftsGiven = m.DayGiven, GiftsReceived = m.DayReceived, Deals = m.DayDeals, Cheated = m.DayCheated,
            });
        }

        /// <summary>Deal traces nobody can still complete close by timeout (telemetry): defected, partial or abandoned.</summary>
        public static void CloseStale(GodState s, EventBuffer ev)
        {
            var soc = s.Social;
            int limit = s.Tuning.Social.DealTimeoutTicks + 2 * s.Tuning.Population.ThinkEvery;
            for (int i = 0; i < soc.Deals.Count;)
            {
                var t = soc.Deals[i];
                if (s.Tick - t.LastStepTick <= limit) { i++; continue; }
                DealOutcome o;
                bool aDone = t.AGiven >= t.ACount, bDone = t.BGiven >= t.BCount;
                if (!t.Agreed) o = DealOutcome.Abandoned;
                else if (t.AGiven == 0 && t.BGiven == 0) o = DealOutcome.Abandoned;
                else if (aDone && t.BGiven == 0 || bDone && t.AGiven == 0) o = DealOutcome.Defected;
                else o = DealOutcome.Partial;
                CloseTrace(s, ev, t, o);
            }
        }
    }
}
