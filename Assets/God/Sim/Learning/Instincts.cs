namespace God.Sim.Learning
{
    /// <summary>
    /// One innate prior (NPC_LEARNING_DESIGN 3.4): when the context holds <see cref="A"/> (and <see cref="B"/>, if set), the
    /// verb gets <see cref="Prior"/> value points. Instincts are a separate score term and are never written into the store,
    /// so "how much is learned" can be measured and learning switched off.
    /// </summary>
    public readonly struct Instinct
    {
        public readonly uint A, B;
        public readonly byte Verb;
        public readonly short Prior;

        public Instinct(uint a, uint b, byte verb, int prior)
        {
            A = a; B = b; Verb = verb; Prior = (short)Fx.Clamp(prior, -2000, 2000);
        }
    }

    /// <summary>
    /// The instinct table of an NPC: at most 32 rules in a fixed order. M3 has two fixed tables (the genome arrives at M4):
    /// <see cref="Default"/> for founders and <see cref="None"/> for naive test NPCs (NPC_LEARNING L1, LS1).
    /// </summary>
    public enum InstinctSet : byte { Default = 0, None = 1 }

    public static class InstinctTable
    {
        /// <summary>
        /// The default innate priors of M3 (the earlier draft's expected-relief estimates, now instincts). Hunger and food
        /// make eating and foraging attractive; tiredness makes sleeping where one is attractive (LS3: the instinct default
        /// is to sleep on the spot, not to walk to a hut); pain makes rest attractive; fear and an enemy in sight make
        /// flight attractive; having no bed makes gathering materials and building mildly attractive, so an instinct-only
        /// NPC still builds a hut slowly.
        /// </summary>
        public static readonly ReadOnlyArray<Instinct> Default = new ReadOnlyArray<Instinct>(
            new Instinct(Feature.DriveActive(Drive.Hunger), Feature.Category(ObjCategory.Food), Verb.Eat, 600),
            new Instinct(Feature.DriveActive(Drive.Hunger), Feature.Category(ObjCategory.Food), Verb.ForageAt, 250),
            new Instinct(Feature.DriveBand(Drive.Hunger, 1), Feature.Category(ObjCategory.Food), Verb.Eat, 250),
            new Instinct(Feature.DriveActive(Drive.Hunger), Feature.Situation(SituationKind.NoFoodKnown), Verb.Explore, 300),
            new Instinct(Feature.DriveActive(Drive.Fatigue), 0, Verb.Sleep, 500),
            new Instinct(Feature.DriveActive(Drive.Pain), 0, Verb.Rest, 400),
            new Instinct(Feature.DriveActive(Drive.Fear), Feature.Situation(SituationKind.EnemyInSight), Verb.Flee, 500),
            new Instinct(Feature.Self(SelfFeature.NoBed), Feature.Category(ObjCategory.Raw), Verb.Gather, 150),
            new Instinct(Feature.Self(SelfFeature.CarryingLog), Feature.Category(ObjCategory.Shelter), Verb.Build, 250),
            new Instinct(Feature.Self(SelfFeature.NoBed), Feature.Class(ObjClass.HutSite), Verb.Build, 100));

        public static readonly ReadOnlyArray<Instinct> Empty = new ReadOnlyArray<Instinct>();

        public static ReadOnlyArray<Instinct> Of(InstinctSet s) => s == InstinctSet.None ? Empty : Default;

        /// <summary>The summed prior of every rule matching a sorted key set for a verb.</summary>
        public static int Prior(ReadOnlyArray<Instinct> rules, uint[] keys, int offset, int n, byte verb)
        {
            int sum = 0;
            for (int r = 0; r < rules.Count; r++)
            {
                var rule = rules[r];
                if (rule.Verb != verb) continue;
                if (!Contains(keys, offset, n, rule.A)) continue;
                if (rule.B != 0 && !Contains(keys, offset, n, rule.B)) continue;
                sum += rule.Prior;
            }
            return sum;
        }

        /// <summary>
        /// The part of <see cref="Prior"/> that a need drives: the rules whose first condition is a drive (hunger, tiredness,
        /// pain, fear). The rest of the prior is situational instinct (no bed, carrying a log). For the "why" of a decision.
        /// </summary>
        public static int NeedPrior(ReadOnlyArray<Instinct> rules, uint[] keys, int offset, int n, byte verb)
        {
            int sum = 0;
            for (int r = 0; r < rules.Count; r++)
            {
                var rule = rules[r];
                if (rule.Verb != verb) continue;
                var k = Feature.KindOf(rule.A);
                if (k != FeatureKind.DriveActive && k != FeatureKind.DriveBand) continue;
                if (!Contains(keys, offset, n, rule.A)) continue;
                if (rule.B != 0 && !Contains(keys, offset, n, rule.B)) continue;
                sum += rule.Prior;
            }
            return sum;
        }

        /// <summary>Binary search in a sorted key range.</summary>
        public static bool Contains(uint[] keys, int offset, int n, uint key)
        {
            int lo = offset, hi = offset + n - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                uint k = keys[mid];
                if (k == key) return true;
                if (k < key) lo = mid + 1; else hi = mid - 1;
            }
            return false;
        }
    }
}
