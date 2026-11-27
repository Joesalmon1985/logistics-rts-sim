using System;
using System.Collections.Generic;
using God.Sim.Cognition;
using God.Sim.Learning;
using God.Sim.Policy;
using God.Sim.Population;
using God.Sim.Population.Households;
using God.Sim.Social;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D6's mind hooks (contracts 10.3 step 4, 13.2; COMMANDMENTS 4.4). They see the think scratch only (layers 2, 3, 6): the
    /// person's own interpretation, its own view of itself, the people it sees and the target features of its own
    /// candidates, never <see cref="GodState"/> (reflection test). <b>Never a restriction (I-CR-1):</b> nothing here removes,
    /// filters, reorders or blocks a candidate; a held rule only adds a bounded term and a feature key to candidates that
    /// already exist, and one telling candidate may be added. OWNER: D6.
    /// </summary>
    public static partial class DivinityMindHooks
    {
        /// <summary>The feature key of a rule's modality: <c>Feature.Divine(CommandmentRequired 5 | Prohibited 6 | Encouraged 7)</c>.</summary>
        public static uint RuleKey(Modality m) => Feature.Make(FeatureKind.Divine,
            m == Modality.Required ? DivineFeatureValue.CommandmentRequired : m == Modality.Prohibited ? DivineFeatureValue.CommandmentProhibited : DivineFeatureValue.CommandmentEncouraged);

        static partial void VerbsD6(List<VerbInfo> into)
        {
            into.Add(new VerbInfo(Doctrine.TellVerb, "tell commandment", VerbGroup.Communication, NpcVerb.Gesture));
        }

        private static DoctrineTuning Tu(DivineMind m) => m.DTuning ?? (m.DTuning = new DoctrineTuning());

        /// <summary>Whether an entry weighs now (Learned or Adopted, positive adoption): the term and the matches.</summary>
        private static bool Weighs(DoctrineTuning tu, in KnownCommandment k)
        {
            if (k.Adoption <= 0) return false;
            var st = Doctrine.StanceOf(tu, k);
            return st == CommandmentStance.Learned || st == CommandmentStance.Adopted;
        }

        /// <summary>Whether an entry carries its key (any state but Heard and Rejected), so learning can confirm or erode it.</summary>
        private static bool Keyed(DoctrineTuning tu, in KnownCommandment k)
        {
            var st = Doctrine.StanceOf(tu, k);
            return st == CommandmentStance.Learned || st == CommandmentStance.Adopted;
        }

        /// <summary>
        /// After every other module's candidates: each existing candidate a held interpretation matches gets that rule's feature
        /// key in its target features (when room is left; sorted), the matches are noted for the choice record, and, when the
        /// person has something it may tell and someone awake in sight, one <see cref="Doctrine.TellVerb"/> candidate.
        /// </summary>
        static partial void AddCandidatesD6(ThinkScratch sc)
        {
            var m = sc.Mind?.Divine;
            if (m == null || m.KnownCount == 0) return;
            var tu = Tu(m);
            m.MatchCount = 0;
            m.MatchTick = sc.Self.Tick;
            for (int c = 0; c < sc.CandCount; c++)
            {
                for (int i = 0; i < m.KnownCount; i++)
                {
                    var k = m.Known[i];
                    if (!Keyed(tu, k)) continue;
                    for (int r = 0; r < k.RuleCount; r++)
                    {
                        var rule = k.Rule(r);
                        if (!Matches(sc, m, k, rule, c)) continue;
                        AddKey(sc, c, RuleKey(rule.Modality));
                        if (!Weighs(tu, k)) continue;
                        if (m.Matches == null) m.Matches = new CommandmentMatch[DivineMind.MatchCap];
                        if (m.MatchCount < DivineMind.MatchCap)
                        {
                            var cd = sc.Cand[c];
                            m.Matches[m.MatchCount++] = new CommandmentMatch
                            {
                                Commandment = k.Commandment, Modality = rule.Modality, Verb = cd.Verb, Kind = cd.Kind, TargetId = cd.TargetId, TargetNode = cd.TargetNode,
                                Term = RuleTerm(sc, tu, k, rule),
                            };
                        }
                    }
                }
            }
            if (tu.Telling == 0 || sc.Self.Tick - m.LastTellTick < tu.TellEveryTicks || Doctrine.Tellable(tu, m) < 0) return;
            for (int i = 0; i < sc.PeopleCount; i++)
            {
                var p = sc.People[i];
                if (p.Asleep || p.Body != BodyKind.Person) continue;
                if (p.Id == m.LastTold && sc.Self.Tick - m.LastTellTick < tu.TellSameTicks) continue;
                int at = Thinker.AddCandidate(sc, Doctrine.TellVerb, IntentTarget.Npc, p.Id, p.Node, (byte)SocialClass.Person, p.Dist);
                Thinker.TargetFeatures(sc, at, Feature.Class(SocialClass.Person));
                break;
            }
        }

        /// <summary>Inserts a key into a candidate's target features (sorted, once, while there is room).</summary>
        private static void AddKey(ThinkScratch sc, int c, uint key)
        {
            ref var cand = ref sc.Cand[c];
            int o = cand.TargetOffset, n = cand.TargetCount;
            for (int i = 0; i < n; i++) if (sc.TargetKeys[o + i] == key) return;
            if (n >= ThinkScratch.MaxTarget) return;
            int at = n;
            while (at > 0 && sc.TargetKeys[o + at - 1] > key) { sc.TargetKeys[o + at] = sc.TargetKeys[o + at - 1]; at--; }
            sc.TargetKeys[o + at] = key;
            cand.TargetCount = n + 1;
        }

        /// <summary>
        /// Contracts 10.3 step 4, D6's part: the sum over the person's weighing entries and their matching rules of
        /// <c>sign x ImportanceVp x Adoption/1000 x Salience/1000 x (1000 + 2 Conformity)/1000</c> (clamped +-2000 by the dispatcher);
        /// for its own telling candidate, <c>TellVp x Adoption x Salience x (1000 + Loyalty)/1000</c> of what it would tell.
        /// </summary>
        static partial void DivineTermD6(ThinkScratch sc, int cand, ref int term)
        {
            var m = sc.Mind?.Divine;
            if (m == null || m.KnownCount == 0) return;
            var tu = Tu(m);
            var c = sc.Cand[cand];
            if (c.Verb == Doctrine.TellVerb)
            {
                int t = Doctrine.Tellable(tu, m);
                if (t < 0) return;
                var k = m.Known[t];
                Beliefs.Read(m, k.God, out _, out _, out _, out _, out int loyalty);
                term += (int)((long)tu.TellVp * k.Adoption / 1000 * k.Salience / 1000 * (1000 + Fx.Clamp(loyalty, -1000, 1000)) / 1000);
                return;
            }
            for (int i = 0; i < m.KnownCount; i++)
            {
                var k = m.Known[i];
                if (!Weighs(tu, k)) continue;
                for (int r = 0; r < k.RuleCount; r++)
                {
                    var rule = k.Rule(r);
                    if (Matches(sc, m, k, rule, cand)) term += RuleTerm(sc, tu, k, rule);
                }
            }
        }

        /// <summary>The term of one matching rule (vp, signed).</summary>
        public static int RuleTerm(ThinkScratch sc, DoctrineTuning tu, in KnownCommandment k, in CommandmentRule r)
        {
            int w = ImportanceVp(tu, k.Importance);
            if (r.Modality == Modality.Encouraged) w = w * tu.EncouragedPermille / 1000;
            int conf = sc.Traits != null && sc.Traits.Length > Trait.Conformity ? sc.Traits[Trait.Conformity] : 0;
            long v = (long)w * k.Adoption / 1000 * k.Salience / 1000 * (1000 + 2 * conf) / 1000;
            return (int)(r.Modality == Modality.Prohibited ? -v : v);
        }

        public static int ImportanceVp(DoctrineTuning tu, int importance)
        {
            switch (importance)
            {
                case 1: return tu.Importance1Vp;
                case 2: return tu.Importance2Vp;
                case 3: return tu.Importance3Vp;
                case 4: return tu.Importance4Vp;
                case 5: return tu.Importance5Vp;
                default: return 0;
            }
        }

        // ---------------- matching, by the person's own view (4.4) ----------------

        /// <summary>Does a rule of the person's interpretation match candidate <paramref name="cand"/>, as it sees things?</summary>
        public static bool Matches(ThinkScratch sc, DivineMind m, in KnownCommandment k, in CommandmentRule r, int cand)
        {
            var c = sc.Cand[cand];
            if (r.LearningVerb == 0 || r.LearningVerb != c.Verb) return false;
            if (r.ObjClass != 0 && r.ObjClass != c.Class) return false;
            if (r.Ware != Ware.None && !WareFits(r.Ware, c.Class)) return false;
            if (!Applies(sc, m, k, r)) return false;
            if (!InScope(sc, m, r.Scope)) return false;
            if (!Holds(sc, cand, r.Condition)) return false;
            var also = (ConditionKind)(r.ConditionParam & 0xFF);
            return also == ConditionKind.Always || Holds(sc, cand, also);
        }

        private static bool WareFits(Ware w, int cls)
        {
            switch (w)
            {
                case Ware.Berries: return cls == ObjClass.BerryBush || cls == ObjClass.FoodLot;
                case Ware.Roots: return cls == ObjClass.RootPatch || cls == ObjClass.FoodLot;
                case Ware.Fish: return cls == ObjClass.FishSpot || cls == ObjClass.FoodLot;
                case Ware.Log: return cls == ObjClass.WoodStand || cls == ObjClass.LogPile;
                default: return cls == SocialClass.Person && Wares.IsFood(w);
            }
        }

        private static bool Applies(ThinkScratch sc, DivineMind m, in KnownCommandment k, in CommandmentRule r)
        {
            var stage = sc.Self.Stage;
            int sel = (r.ConditionParam >> 8) & 0xFF;
            switch (r.Applies)
            {
                case ApplySelector.Everyone: return true;
                case ApplySelector.Adults: return stage != LifeStage.Child;
                case ApplySelector.Children: return stage == LifeStage.Child;
                case ApplySelector.Elders: return stage == LifeStage.Elder;
                case ApplySelector.Affiliated: return k.God >= 1 && k.God <= 30 && (m.AffiliatedGods >> (k.God - 1) & 1) != 0;
                case ApplySelector.Community: return sel != 0 && m.Community == sel;
                case ApplySelector.HouseholdHeads: return sc.Self.Household != 0 && stage != LifeStage.Child;
                default: return false; // Role: no roles before C6
            }
        }

        private static bool InScope(ThinkScratch sc, DivineMind m, in DivineTarget t)
        {
            switch (t.Kind)
            {
                case DivineTargetKind.None: return true;
                case DivineTargetKind.Location: return sc.Grid != null && t.Node >= 0 && t.Node < sc.Grid.Count && sc.Grid.Distance(sc.Self.Node, t.Node) <= t.Radius;
                case DivineTargetKind.Household: return sc.Self.Household != 0 && sc.Self.Household == t.Id;
                case DivineTargetKind.Org: return m.Community == t.Id + 1;
                default: return false;
            }
        }

        private static bool HasTargetKey(ThinkScratch sc, in Candidate c, uint key)
        {
            for (int i = 0; i < c.TargetCount; i++) if (sc.TargetKeys[c.TargetOffset + i] == key) return true;
            return false;
        }

        private static bool HasBase(ThinkScratch sc, uint key)
        {
            for (int i = 0; i < sc.BaseCount; i++) if (sc.Base[i] == key) return true;
            return false;
        }

        private static int PersonIndex(ThinkScratch sc, in Candidate c)
        {
            if (c.Kind != IntentTarget.Npc || c.TargetId == 0) return -1;
            for (int i = 0; i < sc.PeopleCount; i++) if (sc.People[i].Id == c.TargetId) return i;
            return -1;
        }

        /// <summary>A condition, from the scratch only (the person's view): never the true state.</summary>
        public static bool Holds(ThinkScratch sc, int cand, ConditionKind cond)
        {
            var c = sc.Cand[cand];
            int pi;
            switch (cond)
            {
                case ConditionKind.Always: return true;
                case ConditionKind.TargetIsChild: pi = PersonIndex(sc, c); return pi >= 0 && sc.People[pi].Stage == LifeStage.Child;
                case ConditionKind.TargetIsElder: pi = PersonIndex(sc, c); return pi >= 0 && sc.People[pi].Stage == LifeStage.Elder;
                case ConditionKind.TargetIsStranger: pi = PersonIndex(sc, c); return pi >= 0 && (!sc.People[pi].SameFaction || sc.People[pi].As == 0);
                case ConditionKind.TargetBegging:
                    if (HasTargetKey(sc, c, Feature.State(HState.Begging1)) || HasTargetKey(sc, c, Feature.State(HState.Begging2)) || HasTargetKey(sc, c, Feature.State(HState.Begging3))) return true;
                    pi = PersonIndex(sc, c); return pi >= 0 && sc.People[pi].Display == DisplayKind.Beg;
                case ConditionKind.TargetInjured: case ConditionKind.TargetSick:
                    if (HasTargetKey(sc, c, Feature.State(HState.Sick))) return true;
                    pi = PersonIndex(sc, c); return pi >= 0 && sc.People[pi].Display == DisplayKind.Sick;
                case ConditionKind.CarryingFood: return sc.Self.CarryFood;
                case ConditionKind.StockLow: return HasTargetKey(sc, c, Feature.State(ObjState.StockLow));
                case ConditionKind.IOwe: return c.Verb == SocialVerb.HandOver; // a hand-over is my side of an agreed deal, as I believe
                case ConditionKind.EnemyInSight: return HasBase(sc, Feature.Situation(SituationKind.EnemyInSight));
                case ConditionKind.AtHome: return sc.Self.AtHome;
                default: return false; // HouseholdSurplus, NotPermitted, TargetApologised, NearShrine: not perceivable on this branch
            }
        }

        static partial void DescribeD6(uint f, ref string text)
        {
            if (text != null || Feature.KindOf(f) != FeatureKind.Divine) return;
            switch (Feature.ValueOf(f))
            {
                case DivineFeatureValue.CommandmentRequired: text = "a commandment I hold requires it"; break;
                case DivineFeatureValue.CommandmentProhibited: text = "a commandment I hold forbids it"; break;
                case DivineFeatureValue.CommandmentEncouraged: text = "a commandment I hold encourages it"; break;
            }
        }
    }
}
