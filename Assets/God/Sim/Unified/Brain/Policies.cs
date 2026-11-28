namespace God.Sim.Unified
{
    /// <summary>
    /// UN6: the brains of <c>Unification.Brain.Kind</c> behind UN0's <see cref="IPrimitivePolicy"/>. The runtime sets the per-decision
    /// context (the body that decodes a choice, the overlay's control biases, the temperature) before each <see cref="Decide"/>.
    /// <see cref="Kind"/> and <see cref="Label"/> are the honest name of what decides: only <see cref="TrainedPolicy"/> over a verified
    /// "TRAINED" artifact is ever reported as trained.
    /// </summary>
    public abstract class BrainPolicy : IPrimitivePolicy
    {
        public abstract int Kind { get; }
        public abstract string Label { get; }

        /// <summary>Per-decision context (set by the runtime; not saved).</summary>
        public IBrainBody Body;
        public GodState State;
        public int[] Bias;
        public int Temperature = 1000;
        public bool Sample = true;

        /// <summary>The last factorised choice and its multiply-accumulates.</summary>
        public BrainChoice LastChoice;
        public int LastMacs;

        public abstract PrimitiveControl Decide(in Percept p, short[] obs, ref BrainState b, in ActMask m, ref Rng rng);
    }

    /// <summary>The frozen shared network over some weights (untrained or a verified artifact).</summary>
    public abstract class NetPolicy : BrainPolicy
    {
        public readonly BrainNet Net;

        protected NetPolicy(BrainNet net) { Net = net; }

        public override PrimitiveControl Decide(in Percept p, short[] obs, ref BrainState b, in ActMask m, ref Rng rng)
        {
            Net.Bias = Bias;
            Net.Temperature = Temperature;
            LastChoice = Net.Act(obs, b.Recurrent, in m, Sample ? rng : null);
            LastMacs = Net.Macs;
            return Body != null ? Body.Decode(State, in p, in LastChoice) : new PrimitiveControl { Control = (Prim)LastChoice.Control, Actor = p.Actor };
        }
    }

    /// <summary><c>Brain.Kind = 0</c>: the same network with deterministic random weights from a fixed seed: valid, incompetent, labelled UNTRAINED.</summary>
    public sealed class UntrainedPolicy : NetPolicy
    {
        public UntrainedPolicy(BrainNet net) : base(net) { }
        public override int Kind => BrainKind.Untrained;
        public override string Label => "UNTRAINED";
    }

    /// <summary><c>Brain.Kind = 1</c>: the network over a verified artifact (never constructed without one).</summary>
    public sealed class TrainedPolicy : NetPolicy
    {
        public readonly BrainArtifact Artifact;

        public TrainedPolicy(BrainArtifact verified) : base(new BrainNet(BrainShape.Parse(verified.NetShape), verified.Weights) { InputMap = verified.InputMap }) { Artifact = verified; }
        public override int Kind => BrainKind.Trained;
        /// <summary>"TRAINED" only for a "TRAINED" artifact; else the artifact's status is named (e.g. "ARTIFACT v3 SELF-TEST").</summary>
        public override string Label => Artifact.StatusIsTrained ? "TRAINED" : "ARTIFACT v" + Artifact.Version + " " + Artifact.Status;
    }

    /// <summary>
    /// <c>Brain.Kind = 2</c>: the labelled FALLBACK, a minimal scripted safety controller for runs where no trained artifact passed
    /// TR-01. It is not the legacy <c>Eat/Flee/Build</c> rules and knows no goods, buildings or recipes: it chooses among the legal
    /// primitives by a fixed need-pressure ordering, reading only the observation vector (documented in the UN6 record):
    /// <list type="number">
    /// <item>the most pressing need (ties: lower channel) at or above <see cref="Urgent"/>: Energy -> SLEEP if legal; any other ->
    /// USE the nearest legal target in reach (empty hand when legal, interaction slot 0);</item>
    /// <item>a need at or above <see cref="Restless"/> and something perceived: approach the nearest perceived entity (its sector 0 ->
    /// STEP_FORWARD, 1-3 -> TURN_RIGHT, 4-5 -> TURN_LEFT; in reach -> USE it);</item>
    /// <item>otherwise explore: STEP_FORWARD, else TURN_RIGHT, else WAIT.</item>
    /// </list>
    /// Never reported as trained.
    /// </summary>
    public sealed class FallbackPolicy : BrainPolicy
    {
        public const int Urgent = 600, Restless = 300;
        /// <summary>UN2's entity state bit "in reach" (<c>Percept.Bits.InReach</c>).</summary>
        public const int InReachBit = 512;

        private readonly int _needs, _entities;

        public FallbackPolicy() { _needs = ObsSchema.OffsetOf("needs"); _entities = ObsSchema.OffsetOf("entities"); }

        public override int Kind => BrainKind.Fallback;
        public override string Label => "FALLBACK";

        public override PrimitiveControl Decide(in Percept p, short[] obs, ref BrainState b, in ActMask m, ref Rng rng)
        {
            LastMacs = 0;
            LastChoice = Choose(obs, in m);
            return Body != null ? Body.Decode(State, in p, in LastChoice) : new PrimitiveControl { Control = (Prim)LastChoice.Control, Actor = p.Actor };
        }

        private static bool Legal(ulong head, int option) => ActMask.Allows(head, option);

        /// <summary>The fixed ordering above (pure function of the vector and the mask).</summary>
        public BrainChoice Choose(short[] obs, in ActMask m)
        {
            int top = -1, topP = -1;
            for (int c = 0; c < NeedChannels.Count; c++) { int v = obs[_needs + c]; if (v > topP) { topP = v; top = c; } }
            int near = NearestPerceived(obs);
            if (topP >= Urgent)
            {
                if (top == (int)NeedChannel.Energy && Legal(m.Control, (int)Prim.Sleep)) return BrainChoice.Of(Prim.Sleep);
                int t = NearestInReach(obs, in m);
                if (t >= 0 && Legal(m.Control, (int)Prim.Use)) return UseOf(t, in m);
            }
            if (topP >= Restless && near >= 0)
            {
                int st = obs[_entities + near * ObsSchema.EntityFields + 3];
                if ((st & InReachBit) != 0 && Legal(m.Target, near) && Legal(m.Control, (int)Prim.Use)) return UseOf(near, in m);
                int sector = obs[_entities + near * ObsSchema.EntityFields + 1];
                if (sector == 0 && Legal(m.Control, (int)Prim.StepForward)) return BrainChoice.Of(Prim.StepForward);
                if (sector >= 1 && sector <= 3 && Legal(m.Control, (int)Prim.TurnRight)) return BrainChoice.Of(Prim.TurnRight);
                if (sector >= 4 && sector <= 5 && Legal(m.Control, (int)Prim.TurnLeft)) return BrainChoice.Of(Prim.TurnLeft);
            }
            if (Legal(m.Control, (int)Prim.StepForward)) return BrainChoice.Of(Prim.StepForward);
            if (Legal(m.Control, (int)Prim.TurnRight)) return BrainChoice.Of(Prim.TurnRight);
            return BrainChoice.Of(Prim.Wait);
        }

        private static BrainChoice UseOf(int target, in ActMask m)
        {
            var a = BrainChoice.Of(Prim.Use);
            a.Target = target;
            a.Source = Legal(m.Source, BrainHeads.SourceEmpty) ? BrainHeads.SourceEmpty : First(m.Source, BrainHeads.SourceEmpty + 1);
            a.Slot = Legal(m.Slot, 0) ? 0 : First(m.Slot, 4);
            a.Design = Legal(m.Design, BrainHeads.DesignNone) ? BrainHeads.DesignNone : -1;
            return a;
        }

        private static int First(ulong head, int n) { for (int k = 0; k < n; k++) if (Legal(head, k)) return k; return -1; }

        /// <summary>The first (nearest) entity slot with a non-zero class, -1 when nothing is perceived.</summary>
        private int NearestPerceived(short[] obs)
        {
            for (int e = 0; e < ObsSchema.Entities; e++) if (obs[_entities + e * ObsSchema.EntityFields] != 0) return e;
            return -1;
        }

        private int NearestInReach(short[] obs, in ActMask m)
        {
            for (int e = 0; e < ObsSchema.Entities; e++)
            {
                int o = _entities + e * ObsSchema.EntityFields;
                if (obs[o] != 0 && (obs[o + 3] & InReachBit) != 0 && Legal(m.Target, e)) return e;
            }
            return -1;
        }
    }
}
