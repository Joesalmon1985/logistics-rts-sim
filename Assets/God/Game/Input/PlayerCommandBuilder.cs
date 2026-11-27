using System;
using God.Sim;

namespace God.Game
{
    /// <summary>How wide a divine request reaches. Household, area, institution scopes carry ONE id or spec, never a list (CC-4).</summary>
    public enum TargetScope { Person, Group, Household, Institution, Area, Location, Object }

    /// <summary>What the player wants done, apart from the selection: an act name (suggest, bless, heal, ...) and an argument. Plain data.</summary>
    public readonly struct DivineIntent
    {
        public readonly string Act;
        public readonly int Arg;
        public DivineIntent(string act, int arg = 0) { Act = act; Arg = arg; }
    }

    /// <summary>
    /// A divine command as plain data, before it becomes a sim command type: who (the god), what, and the scope taken from the
    /// selection. Identical intents and selections give equal requests (pure).
    /// </summary>
    public sealed class DivineRequest
    {
        public int God;
        public string Act;
        public int Arg;
        public TargetScope Scope;
        /// <summary>Person and Group scopes only: the explicit hand-picked ids. Null for every other scope.</summary>
        public int[] Persons;
        /// <summary>Household, Institution and Object scopes: the one id.</summary>
        public int Id;
        /// <summary>Area scope only.</summary>
        public AreaSpec Area;
        /// <summary>Location scope only (also the click node of an Object).</summary>
        public int Node = -1;

        public bool SameAs(DivineRequest o)
        {
            if (o == null || God != o.God || Act != o.Act || Arg != o.Arg || Scope != o.Scope || Id != o.Id || !Area.Equals(o.Area) || Node != o.Node) return false;
            if ((Persons == null) != (o.Persons == null)) return false;
            if (Persons == null) return true;
            if (Persons.Length != o.Persons.Length) return false;
            for (int i = 0; i < Persons.Length; i++) if (Persons[i] != o.Persons[i]) return false;
            return true;
        }

        public override string ToString() => "god " + God + " " + Act + " " + Scope + (Persons != null ? " [" + string.Join(",", Persons) + "]" : "") + (Id != 0 ? " id " + Id : "") + (Scope == TargetScope.Area ? " " + Area : "") + (Node >= 0 ? " node " + Node : "");
    }

    /// <summary>
    /// Turns a <see cref="DivineRequest"/> into the sim's command object. The seam for F1a: F1 defines the concrete divine command
    /// types; one small class implementing this maps requests to them. Until then the runtime has none (builds nothing) and tests
    /// bring their own.
    /// </summary>
    public interface IDivineCommandFactory
    {
        /// <summary>The command for a request, or null when this factory has no command for the act.</summary>
        Command Create(DivineRequest request);
    }

    /// <summary>The outcome of building and sending one command.</summary>
    public readonly struct IssueResult
    {
        public readonly DivineRequest Request;
        public readonly Command Command;
        public readonly CommandResult Result;
        /// <summary>True when the command went to the gate's queue (applied at the next tick that runs).</summary>
        public readonly bool Submitted;
        public IssueResult(DivineRequest r, Command c, CommandResult res, bool submitted) { Request = r; Command = c; Result = res; Submitted = submitted; }
    }

    /// <summary>
    /// The one path every divine command of the player takes (the popover and radial menu of U2 submit through it): a pure function
    /// of the intent and the selection ids, stamped with the god that holds the Human seat now (contracts 2.1: asked every time, so
    /// SwitchGod is followed), checked with <c>Gate.Check</c> before <c>Submit</c>. A refusal is returned, never swallowed.
    /// </summary>
    public sealed class PlayerCommandBuilder
    {
        private readonly Func<int> _humanGod;
        private readonly ICommandGate _gate;
        /// <summary>The mapping to the sim's command types (null until F1a's types exist).</summary>
        public IDivineCommandFactory Factory;

        public PlayerCommandBuilder(Func<int> humanGod, ICommandGate gate, IDivineCommandFactory factory = null)
        {
            _humanGod = humanGod; _gate = gate; Factory = factory;
        }

        /// <summary>The request for an intent and a selection (no sim, no Unity: EditMode testable). Null for nothing selected or an unusable kind.</summary>
        public static DivineRequest Request(int god, DivineIntent intent, SelectionSnapshot sel)
        {
            if (sel == null) return null;
            var r = new DivineRequest { God = god, Act = intent.Act, Arg = intent.Arg };
            switch (sel.Kind)
            {
                case SelectionKind.Person: r.Scope = TargetScope.Person; r.Persons = new[] { sel.Primary }; break;
                case SelectionKind.Group: r.Scope = TargetScope.Group; r.Persons = (int[])sel.Ids.Clone(); Array.Sort(r.Persons); break;
                case SelectionKind.Household: r.Scope = TargetScope.Household; r.Id = sel.Ids.Length > 0 ? sel.Ids[0] : 0; break;
                case SelectionKind.Institution: r.Scope = TargetScope.Institution; r.Id = sel.Primary; break;
                case SelectionKind.Area: r.Scope = TargetScope.Area; r.Area = sel.Area; break;
                case SelectionKind.Location: r.Scope = TargetScope.Location; r.Node = sel.Node; break;
                case SelectionKind.Object: r.Scope = TargetScope.Object; r.Id = sel.Primary; break;
                default: return null;
            }
            return r;
        }

        /// <summary>The god the commands are stamped with now.</summary>
        public int God => _humanGod();

        public DivineRequest Request(DivineIntent intent, SelectionSnapshot sel) => Request(_humanGod(), intent, sel);

        /// <summary>The command for an intent and a selection (null when there is no factory, no selection or no command for the act).</summary>
        public Command Build(DivineIntent intent, SelectionSnapshot sel)
        {
            var r = Request(intent, sel);
            return r == null || Factory == null ? null : Factory.Create(r);
        }

        /// <summary>Builds, checks, and submits. A refusal comes back in the result with its reason; nothing is sent then.</summary>
        public IssueResult Issue(DivineIntent intent, SelectionSnapshot sel)
        {
            var r = Request(intent, sel);
            if (r == null) return new IssueResult(null, null, CommandResult.Reject("no_selection", "Select someone or somewhere first."), false);
            if (Factory == null) return new IssueResult(r, null, CommandResult.Reject(Reasons.Disabled, "That action is not available."), false);
            var c = Factory.Create(r);
            if (c == null) return new IssueResult(r, null, CommandResult.Reject(Reasons.Disabled, "That action is not available."), false);
            return Send(r, c);
        }

        /// <summary>Checks a command already built (for example kept from before a SwitchGod) and submits it only when the gate accepts.</summary>
        public IssueResult Send(DivineRequest r, Command c)
        {
            var check = _gate.Check(c);
            if (!check.Ok) return new IssueResult(r, c, check, false);
            var sub = _gate.Submit(c);
            return new IssueResult(r, c, sub, sub.Ok);
        }
    }
}
