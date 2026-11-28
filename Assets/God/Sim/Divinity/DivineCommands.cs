using God.Sim.Information;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 10.1: the base of every divine command. It derives from <see cref="Command"/>, never from
    /// <c>FactionCommand</c>, so <c>FactionCommandSet</c>, AI-director admissibility and the HUD's faction set are unchanged.
    /// <see cref="God"/> is the acting GodId (<c>PlayerId</c> unused, 0). Every concrete class is plain data with public fields
    /// (saved in <c>Pending</c>, hashed in <c>pending</c>, logged in the replay as JSON). F1 lands them with <b>no handler</b>:
    /// each is refused with <c>disabled</c> until its package registers one (contracts 0.1 F1.10). OWNER: F1 (types frozen;
    /// handlers to D1-D7).
    /// <para>Gates in <c>GodSim.Check</c> (F1): no <c>GodState.Divinity</c> → <c>divinity_off</c>; an external command whose
    /// <see cref="God"/> does not hold the <c>Human</c> seat → <c>not_your_god</c>. In-step commands of AI-seated gods are
    /// admitted by the god director (D1, <c>DivineCommandSet.Admissible</c>; LEAD_RULINGS R1).</para>
    /// </summary>
    public abstract class DivineCommand : Command
    {
        public int God;
    }

    // ---------------- Suggestion = 1, Command = 2 (D2) ----------------

    public sealed class IssueSuggestion : DivineCommand
    {
        public DivineAct Act;
        public DivineTarget Target;
        public DivineScope Scope;
        public int ExpireTicks;
    }

    public sealed class IssueCommand : DivineCommand
    {
        public DivineAct Act;
        public DivineTarget Target;
        public DivineScope Scope;
        public int ExpireTicks;
    }

    // ---------------- Compulsion = 3 (D2) ----------------

    public sealed class IssueCompulsion : DivineCommand
    {
        public DivineAct Act;
        public DivineTarget Target;
        public int ExpireTicks;
    }

    public sealed class CancelCompulsion : DivineCommand
    {
        public int Intervention;
    }

    // ---------------- Possession = 4 (D4) ----------------

    public sealed class BeginPossession : DivineCommand
    {
        public int Npc;
    }

    public sealed class EndPossession : DivineCommand
    {
    }

    // ---------------- Commandment = 5 (D6) ----------------

    public sealed class RevealCommandment : DivineCommand
    {
        public int Template;
        public CommandmentRule[] Rules;
        public int Importance;
        public DivineTarget Recipients;
    }

    public sealed class ReviseCommandment : DivineCommand
    {
        public int Commandment;
        public CommandmentRule[] Rules;
        public int Importance;
    }

    public sealed class RevokeCommandment : DivineCommand
    {
        public int Commandment;
    }

    // ---------------- Miracle = 6 (D3, D7) ----------------

    public sealed class CastMiracle : DivineCommand
    {
        public MiracleKind Kind;
        public DivineTarget Target;
        public int Magnitude;
    }

    public sealed class Resurrect : DivineCommand
    {
        public int Npc;
        public ResurrectionVariant Variant;
        public int Node;
    }

    // ---------------- Avatar = 7 (D5) ----------------

    public sealed class ManifestAvatar : DivineCommand
    {
        public int Node;
    }

    public sealed class DismissAvatar : DivineCommand
    {
    }

    // ---------------- control input (not an intervention; handler in Control.cs, registered by D4/D5) ----------------

    /// <summary>
    /// Contracts 11.3: one movement or action input for a body the god controls (possessed person or its avatar).
    /// <see cref="Heading"/> is a sector <c>0 .. HeadingSectors - 1</c> (north = board +Z, clockwise); <see cref="Gait"/> 1 walk,
    /// 2 run (= walk until a run speed exists).
    /// </summary>
    public sealed class DivineControlInput : DivineCommand
    {
        public int Body;
        public ControlInputKind Kind;
        public byte Heading;
        public byte Gait;
        public int Node;
        public DivineAct Act;
        public DivineTarget Target;
        /// <summary>
        /// UN0 (contracts 23.5, R84): the control of a <see cref="ControlInputKind.Primitive"/> input (its <c>Actor</c> is
        /// <see cref="Body"/>); null for every other kind, and then not written (so older commands' JSON and pending hash are unchanged).
        /// </summary>
        [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)] public Unified.PrimitiveControl? Primitive;
    }

    // ---------------- configuration (external only; D1) ----------------

    /// <summary>Moves the <c>Human</c> seat from one god to another (sandbox; refused outside sandbox purpose by the host).</summary>
    public sealed class SwitchGod : DivineCommand
    {
        public int From;
        public int To;
    }

    public sealed class SetGodPolicy : DivineCommand
    {
        public string Policy;
    }

    public sealed class SetMythicMode : DivineCommand
    {
        public DivinityMode Mode;
    }

    public sealed class SetDivineProfile : DivineCommand
    {
        public DivineObservationProfile Profile;
    }
}
