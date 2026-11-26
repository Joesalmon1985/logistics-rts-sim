using God.Sim.Comms;
using God.Sim.Population;

namespace God.Sim.Social
{
    /// <summary>M5's rows of the event catalogue (codes 210-229, 300-319, 340-354), appended to <c>EventCatalog.All</c> by F0.11. OWNER: M5.</summary>
    public static class SocialEvents
    {
        public static readonly ReadOnlyArray<EventInfo> Catalog = new ReadOnlyArray<EventInfo>(
            // ---- exchange and gifts (210-229) ----
            new EventInfo(typeof(ExchangeOpened), 210, 1, TelemetryDomain.Economy, "fact_transaction", note: "a deal opened by the first offer: the terms as the offerer meant them (analytics)"),
            new EventInfo(typeof(ExchangeStep), 211, 1, TelemetryDomain.Economy, "fact_transaction_line", true, note: "one hand-over of a deal, by its giver on its own think (Goods.Move HandOver)"),
            new EventInfo(typeof(ExchangeClosed), 212, 1, TelemetryDomain.Economy, "fact_transaction", note: "how a deal ended and each side's believed worth gain"),
            new EventInfo(typeof(GiftGiven), 213, 1, TelemetryDomain.Economy, "fact_transaction_line", true, note: "a gift hand-over (a deal of kind Gift or Help); the deal itself is in fact_transaction, and gifts add to fact_social_day by believed kin band"),
            // ---- social memory (300-319) ----
            new EventInfo(typeof(TrustChanged), 300, 1, TelemetryDomain.Social, "fact_social_interaction", true, note: "one relation's trust or familiarity changed, with its cause and the event it came from"),
            new EventInfo(typeof(RelationEvicted), 301, 1, TelemetryDomain.Social, "fact_social_interaction", true),
            new EventInfo(typeof(RecognitionError), 302, 1, TelemetryDomain.Social, "fact_social_interaction", true),
            new EventInfo(typeof(BetrayalRecorded), 303, 1, TelemetryDomain.Social, "fact_social_interaction"),
            new EventInfo(typeof(PersonDaySocial), 310, 1, TelemetryDomain.Social, "fact_person_day", note: "the soc_ columns of one living person x sim day: relations, trust, messages, gifts, deals"),
            // ---- communication (340-354) ----
            new EventInfo(typeof(GestureMade), 340, 1, TelemetryDomain.Comms, "fact_social_interaction", true, note: "a composed message (act x topic x quantity x counter x target x time) made by a gesture"),
            new EventInfo(typeof(MessageDecoded), 341, 1, TelemetryDomain.Comms, "fact_social_interaction", true, note: "one perceiving listener's decoded message (its belief)"),
            new EventInfo(typeof(ResponseChosen), 342, 1, TelemetryDomain.Comms, "fact_social_interaction", true, note: "the listener's own decision on an addressed message (a response verb, or ignored)"));
    }

    // ---------------------------------------------------------------- exchange (210-229)

    /// <summary>A deal opened by its first offer (ExchangeOpened, 210). The terms are as the offerer meant them (analytics; no mind reads them).</summary>
    public sealed class ExchangeOpened : DomainEvent
    {
        public int DealId;
        public int A, B;
        public DealKind Kind;
        public int MessageId;
        /// <summary>What A gives and what A wants, as meant.</summary>
        public Ware AWare, BWare;
        public int ACount, BCount;
        public bool SameHousehold;
    }

    /// <summary>One hand-over of a deal (211): the giver's own act, after <c>Goods.Move</c> returned Ok; carries the lot.</summary>
    public sealed class ExchangeStep : DealStepBase { }

    /// <summary>The shared fields of a hand-over event.</summary>
    public abstract class DealStepBase : DomainEvent
    {
        public int DealId;
        public int Step;
        public int Giver, Receiver;
        public int Lot;
        public Ware Ware;
        public int Count;
        public int Node;
    }

    /// <summary>How a deal ended (212): the outcome, what passed each way, and each side's believed worth gain (its own valuation).</summary>
    public sealed class ExchangeClosed : DomainEvent
    {
        public int DealId;
        public DealOutcome Outcome;
        public int A, B;
        public int AGiven, BGiven;
        public int AGain, BGain;
    }

    /// <summary>A gift (213): a hand-over of a deal of kind Gift or Help, with the giver's believed kin band; <see cref="TrueR"/> is analytics only (Full).</summary>
    public sealed class GiftGiven : DealStepBase
    {
        public KinBand GiverKinBand;
        public bool SameHousehold;
        /// <summary>The message the gift answers (a request), 0 for an offered gift.</summary>
        public int Answers;
        /// <summary>† Truth, analytics only: true relatedness permille (0 until M4's pedigree; never read by the sim).</summary>
        public int TrueR;
    }

    // ---------------------------------------------------------------- social memory (300-319)

    /// <summary>Why a relation changed (<see cref="TrustChanged.Cause"/>).</summary>
    public enum SocialCause : byte
    {
        None = 0, GiftReceived = 1, GiftGiven = 2, Barter = 3, Betrayal = 4, Refused = 5, Message = 6, Contact = 7, Decay = 8, Care = 9, Handover = 10,
    }

    /// <summary>One relation's trust or familiarity changed (300): in A's mind about B, before and after, with the cause and the event it came from (a deal, a message or a lot id).</summary>
    public sealed class TrustChanged : DomainEvent
    {
        public int A, B;
        public int Before, After;
        public int FamBefore, FamAfter;
        public SocialCause Cause;
        public int CauseRef;
    }

    /// <summary>A relation was forgotten to make room (301).</summary>
    public sealed class RelationEvicted : DomainEvent
    {
        public int Owner, Evicted;
        public int Importance, Trust, Grudge, Familiarity;
    }

    /// <summary>An observer did not recognise someone it knows (302): kind 1 = miss (treated as a stranger this think).</summary>
    public sealed class RecognitionError : DomainEvent
    {
        public int Observer, Observed;
        public byte Kind;
        public int Familiarity;
    }

    /// <summary>A victim recorded a betrayal (303): kind 1 = a deal partner took and did not hand over; 2 = promised help not given.</summary>
    public sealed class BetrayalRecorded : DomainEvent
    {
        public int Victim, Offender;
        public byte Kind;
        public int Severity, Value, DealId;
    }

    /// <summary>One living person's social day (310), emitted at the start of the next day: relations, trust, friends and enemies, grudge, messages, gifts, deals.</summary>
    public sealed class PersonDaySocial : DomainEvent
    {
        public int Npc, Day;
        public int RelCount, MeanTrust, Friends, Enemies, GrudgeSum;
        public int Sent, Received, Misread, GiftsGiven, GiftsReceived, Deals, Cheated;
    }

    // ---------------------------------------------------------------- communication (340-354)

    /// <summary>
    /// A gesture message (340), composed of its parts: act x topic x quantity x counter x target x time (COMMUNICATION 4.9).
    /// <see cref="Honest"/> is analytics only (whether the speaker meant it; always true in M5, lies come at M7).
    /// </summary>
    public sealed class GestureMade : DomainEvent
    {
        public int MessageId;
        public int Speaker;
        public GestureAct Act;
        public TopicKind TopicKind;
        public int TopicId, TopicSub;
        public int Quantity;
        public TopicKind CounterKind;
        public int CounterId, CounterSub;
        /// <summary>The addressee (true id; 0 broadcast).</summary>
        public int Target;
        public MsgTime Time;
        public int DealId;
        public int Node;
        /// <summary>The speaker shows the offered goods (it carries them): a display of the object.</summary>
        public bool Presented;
        public int Listeners;
        /// <summary>† Truth, analytics only.</summary>
        public bool Honest = true;
    }

    /// <summary>One perceiving listener's decoding of a message (341): its belief, possibly wrong. <see cref="Mismatch"/> is analytics only.</summary>
    public sealed class MessageDecoded : DomainEvent
    {
        public int MessageId;
        public int Listener;
        /// <summary>The speaker as the listener recognised it (0: not placed).</summary>
        public int Speaker;
        public GestureAct Act;
        public TopicKind TopicKind;
        public int TopicId, TopicSub;
        public int Quantity;
        public TopicKind CounterKind;
        public int CounterId, CounterSub;
        public int Target;
        public MsgTime Time;
        public int DealId;
        public int Dist;
        /// <summary>† Truth, analytics only: a field differs from what was sent.</summary>
        public bool Mismatch;
    }

    /// <summary>The listener's own decision about an addressed message (342): the learning verb it chose, or ignored (it let it expire).</summary>
    public sealed class ResponseChosen : DomainEvent
    {
        public int MessageId;
        public int Listener;
        public int Speaker;
        public byte Verb;
        public bool Ignored;
    }
}
