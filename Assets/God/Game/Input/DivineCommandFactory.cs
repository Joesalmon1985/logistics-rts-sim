using God.Sim;
using God.Sim.Divinity;

namespace God.Game
{
    /// <summary>The act names a <see cref="DivineIntent"/> carries (U2a). Plain strings, so a request stays plain data.</summary>
    public static class DivineActs
    {
        /// <summary>Arg = <see cref="DivineAct"/>, Arg2 = expiry in ticks.</summary>
        public const string Suggest = "suggest", Command = "command", Compel = "compel";
        /// <summary>Arg = <see cref="MiracleKind"/>, Arg2 = magnitude.</summary>
        public const string Miracle = "miracle";
        /// <summary>Arg = the intervention id of the compulsion.</summary>
        public const string CancelCompulsion = "cancel_compulsion";
        /// <summary>Arg = template id, Arg2 = importance, Rules = the structured rules, Scope = the recipients.</summary>
        public const string Reveal = "reveal";
        /// <summary>Arg = <see cref="DivinityMode"/> (sandbox).</summary>
        public const string SetMythicMode = "set_mythic_mode";
    }

    /// <summary>
    /// U1a's rebase point, done by U2a: maps a <see cref="DivineRequest"/> (plain data) to the F1 divine command types. A request
    /// that has no command (a group, which the HUD expands into individuals first; a person miracle aimed at a place) returns
    /// null. It holds no rule: validity, cost and cooldown are the gate's (R13).
    /// </summary>
    public sealed class DivineCommandFactory : IDivineCommandFactory
    {
        public Command Create(DivineRequest r)
        {
            if (r == null) return null;
            switch (r.Act)
            {
                case DivineActs.Suggest:
                    if (!Target(r, out var t1, out var s1)) return null;
                    return new IssueSuggestion { God = r.God, Act = (DivineAct)r.Arg, Target = t1, Scope = s1, ExpireTicks = r.Arg2 };
                case DivineActs.Command:
                    if (!Target(r, out var t2, out var s2)) return null;
                    return new IssueCommand { God = r.God, Act = (DivineAct)r.Arg, Target = t2, Scope = s2, ExpireTicks = r.Arg2 };
                case DivineActs.Compel:
                    // a compulsion forces one body: the target is the person
                    if (r.Scope != TargetScope.Person || r.Persons == null || r.Persons.Length != 1) return null;
                    return new IssueCompulsion { God = r.God, Act = (DivineAct)r.Arg, Target = new DivineTarget(DivineTargetKind.Npc, r.Persons[0]), ExpireTicks = r.Arg2 };
                case DivineActs.CancelCompulsion:
                    return new CancelCompulsion { God = r.God, Intervention = r.Arg };
                case DivineActs.Miracle:
                    return Miracle(r);
                case DivineActs.Reveal:
                    {
                        Target(r, out var rt, out _);
                        return new RevealCommandment { God = r.God, Template = r.Arg, Rules = r.Rules, Importance = r.Arg2, Recipients = rt };
                    }
                case DivineActs.SetMythicMode:
                    return new SetMythicMode { God = r.God, Mode = (DivinityMode)r.Arg };
                default:
                    return null;
            }
        }

        /// <summary>The sim target and scope of a request's selection scope (false for a group: it is expanded before this).</summary>
        public static bool Target(DivineRequest r, out DivineTarget t, out DivineScope s)
        {
            t = default; s = DivineScope.None;
            switch (r.Scope)
            {
                case TargetScope.Person:
                    if (r.Persons == null || r.Persons.Length != 1) return false;
                    t = new DivineTarget(DivineTargetKind.Npc, r.Persons[0]); s = DivineScope.Individual; return true;
                case TargetScope.Household:
                    t = new DivineTarget(DivineTargetKind.Household, r.Id); s = DivineScope.Household; return true;
                case TargetScope.Institution:
                    t = new DivineTarget(DivineTargetKind.Org, r.Id); s = DivineScope.Org; return true;
                case TargetScope.Area:
                    t = new DivineTarget(DivineTargetKind.Location, 0, r.Area.CentreNode, r.Area.RadiusSteps); s = DivineScope.Location; return true;
                case TargetScope.Location:
                    if (r.Node < 0) return false;
                    t = new DivineTarget(DivineTargetKind.Location, 0, r.Node, 0); s = DivineScope.Location; return true;
                default:
                    return false;
            }
        }

        private static Command Miracle(DivineRequest r)
        {
            var kind = (MiracleKind)r.Arg;
            DivineTarget t;
            switch (kind)
            {
                case MiracleKind.HealInjury: case MiracleKind.HealDisease: case MiracleKind.Bless: case MiracleKind.Protect: case MiracleKind.SmitePerson:
                case MiracleKind.RevealKnowledge: case MiracleKind.Rejuvenate:
                    if (r.Scope == TargetScope.Person && r.Persons != null && r.Persons.Length == 1) t = new DivineTarget(DivineTargetKind.Npc, r.Persons[0]);
                    else if (r.Scope == TargetScope.Area) { t = new DivineTarget(DivineTargetKind.Location, 0, r.Area.CentreNode, r.Area.RadiusSteps); kind = (MiracleKind)((int)MiracleKind.Area + (int)kind); }
                    else return null;
                    break;
                case MiracleKind.SmiteBuilding:
                    if (r.Scope == TargetScope.Object && r.Object == ObjectKind.Building) t = new DivineTarget(DivineTargetKind.Building, r.Id);
                    else if (r.Scope == TargetScope.Location && r.Node >= 0) t = new DivineTarget(DivineTargetKind.Hut, 0, r.Node);
                    else return null;
                    break;
                case MiracleKind.SmiteSource:
                    if (r.Scope == TargetScope.Object && r.Object == ObjectKind.Source) t = new DivineTarget(DivineTargetKind.Source, r.Id);
                    else if (r.Scope == TargetScope.Location && r.Node >= 0) t = new DivineTarget(DivineTargetKind.Location, 0, r.Node);
                    else return null;
                    break;
                case MiracleKind.CreateResource: case MiracleKind.DestroyResource: case MiracleKind.Consecrate:
                    if (r.Scope == TargetScope.Location && r.Node >= 0) t = new DivineTarget(DivineTargetKind.Location, 0, r.Node);
                    else if (r.Scope == TargetScope.Person && r.Persons != null && r.Persons.Length == 1) t = new DivineTarget(DivineTargetKind.Npc, r.Persons[0]);
                    else return null;
                    break;
                default:
                    return null;
            }
            return new CastMiracle { God = r.God, Kind = kind, Target = t, Magnitude = r.Arg2 };
        }
    }
}
