using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Policy;

namespace God.Sim.Social
{
    /// <summary>
    /// M5's learning verbs (SHARED_CONTRACTS 6.1: 48-59 and <c>Say(act)</c> = 64 + act) and the feature values M5 adds in
    /// its own blocks (<c>ObjState</c> 24-31, <c>ObjClass</c> 60-79).
    /// </summary>
    public static class SocialVerb
    {
        /// <summary>Hand food or goods to a person who asked (a gift in answer to a request).</summary>
        public const byte Give = 48;
        /// <summary>Hand over my side of an agreed deal (an exchange step, a promised help, an accepted gift offer).</summary>
        public const byte HandOver = 49;
        /// <summary>Walk to where a remembered person was last seen.</summary>
        public const byte Seek = 54;

        /// <summary><c>Say(act) = 64 + act</c> for the acts M5 composes (Want 1, Offer 2, Accept 3, Refuse 4, NeedFood 5, NeedHelp 6, Cooperate 7, Warn 10).</summary>
        public const byte SayWant = 65, SayOffer = 66, SayAccept = 67, SayRefuse = 68, SayNeedFood = 69, SayNeedHelp = 70, SayCooperate = 71, SayWarn = 74;

        public static bool IsSocial(byte v) => v == Give || v == HandOver || v == Seek || Messages.IsSay(v);
    }

    /// <summary>M5's values of <c>ObjState</c> (24-31): how good an offer looks to the one deciding on it, by its own valuation.</summary>
    public static class OfferState
    {
        public const int Good = 24, Fair = 25, Poor = 26;
    }

    /// <summary>M5's values of <c>ObjClass</c> (60-79).</summary>
    public static class SocialClass
    {
        /// <summary>A person (the target class of every social candidate).</summary>
        public const int Person = 60;
    }

    /// <summary>The verb and instinct rows M5 appends (F0.11).</summary>
    internal static class SocialRows
    {
        public static ReadOnlyArray<VerbInfo> Verbs() => new ReadOnlyArray<VerbInfo>(
            new VerbInfo(SocialVerb.Give, "give", VerbGroup.CoopGive, NpcVerb.HandOver, deferred: true),
            new VerbInfo(SocialVerb.HandOver, "hand over", VerbGroup.BarterGive, NpcVerb.HandOver, deferred: true),
            new VerbInfo(SocialVerb.Seek, "seek person", VerbGroup.Relationships, NpcVerb.Interact),
            new VerbInfo(SocialVerb.SayWant, "say want", VerbGroup.Communication, NpcVerb.Gesture, deferred: true),
            new VerbInfo(SocialVerb.SayOffer, "say offer", VerbGroup.Communication, NpcVerb.Gesture, deferred: true),
            new VerbInfo(SocialVerb.SayAccept, "say accept", VerbGroup.BarterKeep, NpcVerb.Gesture, deferred: true),
            new VerbInfo(SocialVerb.SayRefuse, "say refuse", VerbGroup.BarterKeep, NpcVerb.Gesture, deferred: true),
            new VerbInfo(SocialVerb.SayNeedFood, "say need food", VerbGroup.Communication, NpcVerb.Gesture, deferred: true),
            new VerbInfo(SocialVerb.SayNeedHelp, "say need help", VerbGroup.Communication, NpcVerb.Gesture, deferred: true),
            new VerbInfo(SocialVerb.SayCooperate, "say cooperate", VerbGroup.CoopGive, NpcVerb.Gesture, deferred: true),
            new VerbInfo(SocialVerb.SayWarn, "say warn", VerbGroup.Communication, NpcVerb.Gesture));

        /// <summary>
        /// Innate priors (starting points only; learning outweighs them): answer a good offer with acceptance and a poor one
        /// with refusal or a counter-offer; hand over one's side of an agreement; give to someone who says it needs food;
        /// promise help to someone who asks for it; ask for food when hungry with none known (below searching for it, M3's
        /// explore prior 300: asking is a starting option, what it brings is learned); warn others of an enemy; flee when warned.
        /// </summary>
        public static ReadOnlyArray<Instinct> Instincts()
        {
            uint offer = Feature.MsgAct((int)GestureAct.Offer);
            return new ReadOnlyArray<Instinct>(
                new Instinct(offer, Feature.State(OfferState.Good), SocialVerb.SayAccept, 350),
                new Instinct(offer, Feature.State(OfferState.Fair), SocialVerb.SayAccept, 150),
                new Instinct(offer, Feature.State(OfferState.Poor), SocialVerb.SayRefuse, 250),
                new Instinct(offer, Feature.State(OfferState.Poor), SocialVerb.SayOffer, 200),
                new Instinct(offer, 0, SocialVerb.SayRefuse, 50),
                new Instinct(Feature.MsgAct((int)GestureAct.Accept), 0, SocialVerb.HandOver, 350),
                new Instinct(Feature.MsgAct((int)GestureAct.Cooperate), 0, SocialVerb.HandOver, 350),
                new Instinct(Feature.MsgAct((int)GestureAct.NeedFood), 0, SocialVerb.Give, 200),
                new Instinct(Feature.MsgAct((int)GestureAct.NeedFood), 0, SocialVerb.SayRefuse, 100),
                new Instinct(Feature.MsgAct((int)GestureAct.NeedHelp), 0, SocialVerb.SayCooperate, 250),
                new Instinct(Feature.MsgAct((int)GestureAct.NeedHelp), 0, SocialVerb.SayRefuse, 100),
                new Instinct(Feature.MsgAct((int)GestureAct.Warn), 0, Verb.Flee, 300),
                new Instinct(Feature.DriveActive(Drive.Hunger), Feature.Situation(SituationKind.NoFoodKnown), SocialVerb.SayNeedFood, 200),
                new Instinct(Feature.DriveActive(Drive.Hunger), 0, SocialVerb.SayNeedFood, 50),
                new Instinct(Feature.DriveActive(Drive.Hunger), Feature.Situation(SituationKind.NoFoodKnown), SocialVerb.Seek, 150),
                new Instinct(Feature.State(OfferState.Good), 0, SocialVerb.SayOffer, 150),
                new Instinct(Feature.Self(SelfFeature.HasSite), 0, SocialVerb.SayNeedHelp, 150),
                new Instinct(Feature.Situation(SituationKind.EnemyInSight), 0, SocialVerb.SayWarn, 300));
        }
    }
}
