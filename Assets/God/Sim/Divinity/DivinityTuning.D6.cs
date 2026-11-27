namespace God.Sim.Divinity
{
    /// <summary>
    /// D6's numbers, <c>Divinity.Doctrine</c> (COMMANDMENTS 4.2-4.6; contracts 13). Fields only in this file (contracts
    /// 20.F1.1). The costs are D1's (<c>Divinity.Mythic.Costs.RevealCommandment</c> etc.). Every value is a configurable
    /// default (DEV_LOG "D6 record", design decisions). OWNER: D6.
    /// </summary>
    public sealed partial class DoctrineTuning
    {
        // ---------------- revelation (4.3) ----------------
        /// <summary>People a revelation may reach; a target resolving to more is refused <c>bad_target</c>.</summary>
        public int MaxRecipients = 32;
        /// <summary>Chance (per mille) that a recipient perceives the revelation (draw <c>commandment-perceive</c>).</summary>
        public int RevealPerceive = 1000;
        /// <summary>Chance (per mille) at belief 0 that a rule is copied altered at revelation; scaled by (1000 - belief) / 1000.</summary>
        public int RevealBlurPermille = 60;
        /// <summary>1: people within gesture range who attend a recipient hear the revelation too, at half salience (Source Witnessed).</summary>
        public int Witnesses = 1;
        /// <summary>Salience of a revealed entry: <c>RevealSalienceBase + Belief / 2</c>.</summary>
        public int RevealSalienceBase = 400;
        /// <summary>Adoption of a revealed entry: <c>Belief x Loyalty / 1000 - RevealAdoptionOffset</c>.</summary>
        public int RevealAdoptionOffset = 200;
        /// <summary>A told revocation moves adoption by this (towards rejection).</summary>
        public int RevokeShift = -600;

        // ---------------- the divine term (4.4) ----------------
        /// <summary><c>ImportanceVp[1..5]</c> of a Required or Prohibited rule (vp); an Encouraged rule weighs <see cref="EncouragedPermille"/> of it.</summary>
        public int Importance1Vp = 150, Importance2Vp = 300, Importance3Vp = 500, Importance4Vp = 800, Importance5Vp = 1200;
        public int EncouragedPermille = 500;

        // ---------------- states (4.5) ----------------
        /// <summary>Salience below this: Heard. Adoption at or above <see cref="AdoptFrom"/>: Adopted; at or below <see cref="RejectBelow"/>: Rejected.</summary>
        public int HeardBelow = 200, AdoptFrom = 300, RejectBelow = -300;
        /// <summary>Salience lost per day (an entry at 0 is forgotten).</summary>
        public int SalienceDecay = 10;
        /// <summary>Salience gained by acting on a held rule (per decision that matched).</summary>
        public int SalienceActed = 20;
        /// <summary>Exposure salience: <c>ExposureSalience x rate / 100</c>; a new entry from an exposure starts at that plus <see cref="NewEntrySalience"/>.</summary>
        public int ExposureSalience = 150, NewEntrySalience = 100;
        /// <summary>Daily pull of adoption towards the belief target (per mille of the gap).</summary>
        public int AdoptPullPermille = 50;
        /// <summary>The belief, faith and loyalty a mind without a <c>GodBelief</c> for the god gives it (D2's instruction defaults give belief).</summary>
        public int DefaultFaith = 300, DefaultLoyalty = 500;
        /// <summary>
        /// Experience: when the sum of the learned weights (Stw + Ltw) of the person's own rule keys for the rule's verb is below
        /// <see cref="ExperienceRejectBelow"/> (vp), adoption moves by -<see cref="ExperiencePull"/> a day; above
        /// <see cref="ExperienceAdoptAbove"/>, by +<see cref="ExperiencePull"/> / 2.
        /// </summary>
        public int ExperienceRejectBelow = -40, ExperienceAdoptAbove = 40, ExperiencePull = 100;

        // ---------------- transmission (4.5) ----------------
        /// <summary>Channel rates (exposure strength, per cent): telling, household, witnessing a revelation is half of the revelation's.</summary>
        public int TellRate = 80, HouseholdRate = 120;
        /// <summary>1: a held commandment can be told (the TellCommandment candidate).</summary>
        public int Telling = 1;
        /// <summary>The telling candidate's divine term at full adoption and salience (vp), raised by loyalty: <c>x (1000 + Loyalty) / 1000</c>.</summary>
        public int TellVp = 300;
        /// <summary>Ticks between a person's tellings, and before it tells the same person again.</summary>
        public int TellEveryTicks = 240, TellSameTicks = 2400;
        /// <summary>Salience a person needs to tell (Heard entries are not told).</summary>
        public int TellMinSalience = 200;
        /// <summary>Ticks a telling gesture lasts.</summary>
        public int TellTicks = 6;
        /// <summary>Extra field-error chance (per mille) of a telling, added to M5's decode model.</summary>
        public int TellNoisePermille = 0;
        /// <summary>1: the daily household pass exposes household members to an adult's adopted commandments.</summary>
        public int HouseholdPass = 1;
        /// <summary>Field-error chance (per mille) of a household exposure.</summary>
        public int HouseholdNoisePermille = 20;
        /// <summary>Esteem (vp) assumed for a model the listener has no relation with (M5 trust otherwise).</summary>
        public int StrangerEsteem = 200;

        // ---------------- Tier 2: beliefs, religions, affiliation (4.1, 4.2, 4.6) ----------------
        /// <summary>
        /// 1: Tier 2 runs: religions are founded (one per god), founders affiliated and seeded with beliefs, beliefs move from
        /// evidence, affiliation changes by conversion. 0 (default until the lead's re-pin): D2's instruction defaults stand.
        /// </summary>
        public int Beliefs;
        public FounderBeliefs Founders = new FounderBeliefs();
        public BeliefEvidence Evidence = new BeliefEvidence();
        /// <summary>Conversion: days the strongest god must lead, and the loyalty it needs; belief below which an affiliation lapses.</summary>
        public int ConvertDays = 3, ConvertFrom = 200, LapseBelief = 100;
        /// <summary>1: a conversion keeps the other affiliations open.</summary>
        public int KeepOtherAffiliations;
    }

    /// <summary><c>Divinity.Doctrine.Founders</c>: the seeded beliefs of a founding religion's people (mean +- spread, keyed per person).</summary>
    public sealed class FounderBeliefs
    {
        public int Belief = 600, BeliefSpread = 200;
        public int Faith = 300, FaithSpread = 200;
        public int Trust = 0, Fear = 100;
        public int Loyalty = 300, LoyaltySpread = 200;
    }

    /// <summary><c>Divinity.Doctrine.Evidence</c> (COMMANDMENTS 4.2): belief, faith, trust, fear, loyalty deltas by what the person perceived.</summary>
    public sealed class BeliefEvidence
    {
        public int InstructionBelief = 40;
        public int HealedBelief = 120, HealedFaith = 60, HealedTrust = 150, HealedFear = -20, HealedLoyalty = 80;
        public int SmittenBelief = 150, SmittenTrust = -250, SmittenFear = 300, SmittenLoyalty = -80;
        public int GoodsBelief = 80, GoodsFaith = 30, GoodsTrust = 80, GoodsLoyalty = 40;
        public int SignBelief = 60;
        public int SmiteWitnessedBelief = 60, SmiteWitnessedFear = 120;
        public int HealWitnessedBelief = 60, HealWitnessedFaith = 20, HealWitnessedTrust = 40, HealWitnessedLoyalty = 20;
        /// <summary>Daily decay towards 0 (per mille of the value): belief, faith, trust, fear, loyalty.</summary>
        public int DecayBelief = 10, DecayFaith = 10, DecayTrust = 10, DecayFear = 20, DecayLoyalty = 10;
    }
}
