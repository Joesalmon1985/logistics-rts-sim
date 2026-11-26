using System;
using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>The kinds of victory condition a game can list (STATE_DESIGN "Victory conditions").</summary>
    public enum VictoryKind
    {
        /// <summary>Today's rule: a faction with at least <see cref="VictorySpec.Target"/> VP wins (most VP, ties to the lower id).</summary>
        VictoryPoints = 0,
        /// <summary>Today's rule: when at most one faction is left, it wins (nobody when none is left).</summary>
        LastStanding = 1,
        /// <summary>Never ends the game. Factions can still be eliminated; the run stops at the scenario's length.</summary>
        Sandbox = 2,
    }

    /// <summary>
    /// One entry of <see cref="GameSetup.Victory"/>: plain data, so the list saves, hashes and appears in scenario files.
    /// <see cref="VictoryConditions.Create"/> turns it into the rule object.
    /// </summary>
    public sealed class VictorySpec
    {
        public VictoryKind Kind;
        /// <summary>VP needed for <see cref="VictoryKind.VictoryPoints"/> (10 by default); unused by the others.</summary>
        public int Target;

        public VictorySpec() { }
        public VictorySpec(VictoryKind kind, int target = 0) { Kind = kind; Target = target; }

        public static VictorySpec VictoryPoints(int target = FactionTuning.VictoryPoints) => new VictorySpec(VictoryKind.VictoryPoints, target);
        public static VictorySpec LastStanding() => new VictorySpec(VictoryKind.LastStanding);
        public static VictorySpec Sandbox() => new VictorySpec(VictoryKind.Sandbox);

        /// <summary>The stage 3 rules (the default of a plain <see cref="GameSetup"/>): 10 VP, then last standing.</summary>
        public static List<VictorySpec> Stage3Default() => new List<VictorySpec> { VictoryPoints(), LastStanding() };

        /// <summary>A sandbox game: nothing ends it.</summary>
        public static List<VictorySpec> SandboxOnly() => new List<VictorySpec> { Sandbox() };

        public override string ToString() => Kind == VictoryKind.VictoryPoints ? "VictoryPoints(" + Target + ")" : Kind.ToString();
    }

    /// <summary>What a victory condition reads: the true state (these are rules of the game, not policies) and the factions still in.</summary>
    public readonly struct VictoryInputs
    {
        public readonly GodState State;
        /// <summary>Factions of the game not eliminated, ascending.</summary>
        public readonly List<Faction> Alive;
        public VictoryInputs(GodState state, List<Faction> alive) { State = state; Alive = alive; }
    }

    /// <summary>None, or the end of the game with a winner (a faction id, or -1 for nobody) and the reason.</summary>
    public readonly struct VictoryResult
    {
        public readonly bool Ended;
        public readonly int Winner;
        public readonly GameEndReason Reason;
        public VictoryResult(int winner, GameEndReason reason) { Ended = true; Winner = winner; Reason = reason; }
        public static VictoryResult None => default;
    }

    /// <summary>
    /// A way to win (STATE_DESIGN). The faction system calls each condition of <see cref="GameSetup.Victory"/> in list
    /// order every tick, after eliminations; the first that returns a result ends the game. A condition rewards the
    /// faction only: it never feeds an NPC's reward and no institution sees it.
    /// </summary>
    public interface IVictoryCondition
    {
        string Name { get; }
        VictoryResult Evaluate(in VictoryInputs inputs);
    }

    /// <summary>Most VP at or above the target wins; ties go to the lower id (the stage 3 rule, unchanged).</summary>
    public sealed class VictoryPointsCondition : IVictoryCondition
    {
        public readonly int Target;
        public VictoryPointsCondition(int target) { Target = target > 0 ? target : FactionTuning.VictoryPoints; }
        public string Name => "VictoryPoints" + Target;

        public VictoryResult Evaluate(in VictoryInputs inputs)
        {
            int winner = -1, bestVp = -1;
            foreach (var f in inputs.Alive)
            {
                int vp = FactionRules.VictoryPoints(inputs.State, f);
                if (vp >= Target && vp > bestVp) { bestVp = vp; winner = (int)f; }
            }
            return winner >= 0 ? new VictoryResult(winner, GameEndReason.VictoryPoints) : VictoryResult.None;
        }
    }

    /// <summary>At most one faction left: it wins, or nobody when none is left (the stage 3 rule, unchanged).</summary>
    public sealed class LastStandingCondition : IVictoryCondition
    {
        public string Name => "LastStanding";

        public VictoryResult Evaluate(in VictoryInputs inputs)
        {
            if (inputs.Alive.Count > 1) return VictoryResult.None;
            return new VictoryResult(inputs.Alive.Count == 1 ? (int)inputs.Alive[0] : -1, GameEndReason.LastStanding);
        }
    }

    /// <summary>Never ends the game.</summary>
    public sealed class SandboxCondition : IVictoryCondition
    {
        public string Name => "Sandbox";
        public VictoryResult Evaluate(in VictoryInputs inputs) => VictoryResult.None;
    }

    public static class VictoryConditions
    {
        public static IVictoryCondition Create(VictorySpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            switch (spec.Kind)
            {
                case VictoryKind.VictoryPoints: return new VictoryPointsCondition(spec.Target);
                case VictoryKind.LastStanding: return new LastStandingCondition();
                case VictoryKind.Sandbox: return new SandboxCondition();
                default: throw new ArgumentOutOfRangeException(nameof(spec), "Unknown victory kind " + spec.Kind);
            }
        }

        /// <summary>The conditions of a setup, in its order (an empty or missing list behaves as the stage 3 default).</summary>
        public static List<IVictoryCondition> Of(GameSetup setup)
        {
            var specs = setup?.Victory;
            if (specs == null || specs.Count == 0) specs = VictorySpec.Stage3Default();
            var l = new List<IVictoryCondition>(specs.Count);
            foreach (var v in specs) l.Add(Create(v));
            return l;
        }

        /// <summary>True when no condition of the setup can ever end the game (only Sandbox entries).</summary>
        public static bool IsSandbox(GameSetup setup)
        {
            var specs = setup?.Victory;
            if (specs == null || specs.Count == 0) return false;
            foreach (var v in specs) if (v.Kind != VictoryKind.Sandbox) return false;
            return true;
        }
    }
}
