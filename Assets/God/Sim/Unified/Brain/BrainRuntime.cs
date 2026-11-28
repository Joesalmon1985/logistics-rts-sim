using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN6 (§D9.2, spec 5.3-5.7): one game's brain runtime: the policy of <c>Unification.Brain.Kind</c>, the body it senses and acts
    /// through, the per-decision scratch (one percept, one vector, one mask: the decision path allocates nothing), the overlay's learning
    /// numbers and the cost counters. Built when a unified game is created or loaded (<see cref="Build"/>), held in
    /// <see cref="UnifiedState.BrainRuntime"/>, never saved or hashed: everything a decision changes lives in the person's
    /// <see cref="BrainState"/>, its overlay store, its policy <c>Rng</c> and the world.
    /// <para>The trainer (UN6t) and tests may replace <see cref="Policy"/> or <see cref="Body"/> per game (never a static). A replaced
    /// policy that is not a <see cref="BrainPolicy"/> is labelled by <see cref="Origin"/> (a test fixture submits as
    /// <see cref="ControlOrigin.Fixture"/>, the teacher as <see cref="ControlOrigin.Teacher"/>).</para>
    /// </summary>
    public sealed class BrainRuntime
    {
        public IPrimitivePolicy Policy;
        public IBrainBody Body;
        public ControlOrigin Origin = ControlOrigin.Policy;

        public readonly int Kind;
        public readonly string Label;
        public readonly string WeightsSha256;
        public readonly BrainArtifact Artifact;

        // ---- scratch (made once) ----
        public Percept P;
        public readonly short[] Obs = new short[ObsSchema.Length];
        public ActMask Mask;
        public readonly int[] Bias = new int[ActSchema.Heads[0].Length];
        public readonly uint[] Keys = new uint[AssociationStore.MaxKeys];
        public readonly short[] Now = new short[NeedChannels.Count];
        internal readonly Rng Draw = new Rng();
        internal readonly int[] Near = new int[ObsSchema.Entities], NearDist = new int[ObsSchema.Entities];
        internal readonly LearningTuning OverlayTune;
        public readonly LearningCaps Caps;
        internal readonly SimCalendar Calendar;
        internal readonly int NeedsOff, EntOff, LastResultOff, CarryOff, PlacesOff;

        // ---- cost counters (UNP, UN11a; not state) ----
        public long Decisions, Macs, Skipped, Submitted, Accepted, PlanExpansions;
        public int MaxMacs;
        /// <summary>The multiply-accumulates and decisions of the tick <see cref="CountTick"/> (per-tick totals for the benchmarks).</summary>
        public long CountTick = -1, TickMacs, TickDecisions;

        private BrainRuntime(GodState s, int kind, string label, string sha, BrainArtifact artifact, IPrimitivePolicy policy)
        {
            Kind = kind; Label = label; WeightsSha256 = sha; Artifact = artifact; Policy = policy; _own = policy;
            Body = new MotorBody();
            var t = s.Tuning.Unification.UN6;
            OverlayTune = new LearningTuning { EpisodesOn = false, Entries = t.OverlayEntries, Conjunctions = 0, Traces = t.OverlayTraces, Episodes = 0, Moments = 0, GammaPermille = t.OverlayGamma };
            Caps = new LearningCaps(t.OverlayEntries < 1 ? 1 : t.OverlayEntries, 0, t.OverlayTraces < 1 ? 1 : t.OverlayTraces, 0, 0);
            Calendar = SimCalendar.Of(s);
            NeedsOff = ObsSchema.OffsetOf("needs"); EntOff = ObsSchema.OffsetOf("entities"); LastResultOff = ObsSchema.OffsetOf("last_result");
            CarryOff = ObsSchema.OffsetOf("carry"); PlacesOff = ObsSchema.OffsetOf("places");
        }

        /// <summary>
        /// The runtime of <c>Unification.Brain.Kind</c>: 0 the untrained network (weights from <c>UN6.UntrainedSeed</c>), 1 the network
        /// over the verified artifact <c>Brain.Artifact</c> (refused with <see cref="BrainRefused"/> on any mismatch), 2 the labelled
        /// fallback. A loaded game must run the brain its save names (kind and weights hash), else it is refused too.
        /// </summary>
        public static BrainRuntime Build(GodState s)
        {
            var bt = s.Tuning.Unification.Brain;
            BrainRuntime rt;
            switch (bt.Kind)
            {
                case BrainKind.Untrained:
                {
                    var shape = BrainShape.Default;
                    var w = BrainNet.UntrainedWeights(shape, (ulong)s.Tuning.Unification.UN6.UntrainedSeed);
                    var p = new UntrainedPolicy(new BrainNet(shape, w));
                    rt = new BrainRuntime(s, BrainKind.Untrained, p.Label, BrainNet.WeightsSha256(w), null, p);
                    break;
                }
                case BrainKind.Trained:
                {
                    var a = ArtifactLoader.LoadVerified(bt.Artifact);
                    var p = new TrainedPolicy(a);
                    rt = new BrainRuntime(s, BrainKind.Trained, p.Label, a.WeightsSha256, a, p);
                    break;
                }
                case BrainKind.Fallback:
                {
                    var p = new FallbackPolicy();
                    rt = new BrainRuntime(s, BrainKind.Fallback, p.Label, "", null, p);
                    break;
                }
                default: throw new BrainRefused("Unification.Brain.Kind " + bt.Kind + " is not 0 (Untrained), 1 (Trained) or 2 (Fallback)");
            }
            var rec = s.Unification.Brain;
            if (rec.AttachedTick >= 0)
            {
                if (rec.Kind != rt.Kind || rec.WeightsSha256 != rt.WeightsSha256)
                    throw new BrainRefused("the save ran brain kind " + rec.Kind + " (" + rec.Label + ", weights " + rec.WeightsSha256 + ") but this game would run kind " + rt.Kind + " (" + rt.Label + ", weights " + rt.WeightsSha256 + ")");
            }
            else
            {
                rec.Kind = rt.Kind; rec.ArtifactVersion = rt.Kind == BrainKind.Trained ? bt.Artifact : 0; rec.WeightsSha256 = rt.WeightsSha256; rec.Label = rt.Label;
            }
            return rt;
        }

        /// <summary>
        /// Replaces this game's deciding policy (the trainer's network, an evaluation arm, the teacher, a test fixture), with the origin
        /// its controls are submitted under. Per game, never static: several sims in one process run different policies in parallel.
        /// The game's brain record (kind, label, hash) is unchanged and an injected policy is never reported as trained
        /// (<see cref="Brain.Badge"/> requires the game's own <see cref="TrainedPolicy"/>). Null restores the game's own policy.
        /// </summary>
        public void Inject(IPrimitivePolicy policy, ControlOrigin origin)
        {
            Policy = policy ?? Own;
            Origin = policy == null ? ControlOrigin.Policy : origin;
        }

        /// <summary>The policy of the game's own brain kind (what <see cref="Inject"/>(null) restores).</summary>
        public IPrimitivePolicy Own => _own;
        private IPrimitivePolicy _own;

        /// <summary>True when the deciding policy is not the game's own (an injected one).</summary>
        public bool Injected => !ReferenceEquals(Policy, _own);

        internal void Count(long tick, int macs)
        {
            if (tick != CountTick) { CountTick = tick; TickMacs = 0; TickDecisions = 0; }
            Decisions++; TickDecisions++; Macs += macs; TickMacs += macs;
            if (macs > MaxMacs) MaxMacs = macs;
        }
    }
}
