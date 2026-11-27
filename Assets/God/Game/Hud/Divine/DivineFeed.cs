using System;
using System.Collections.Generic;
using System.Reflection;
using God.Sim;
using God.Sim.Divinity;

namespace God.Game
{
    /// <summary>The player's answer to one sent instruction, built only from sim events (never a guess).</summary>
    public sealed class InstructionChip
    {
        public int Serial;
        public string Label;
        public InterventionKind Kind;
        public DivineAct Act;
        public int God;
        /// <summary>Commands sent for this chip (a group is one command per person).</summary>
        public int Submitted;
        /// <summary>Of those, how many the sim has opened as interventions (accepted at a tick).</summary>
        public int Accepted;
        public readonly HashSet<int> Interventions = new HashSet<int>();
        public int Perceived, Followed, Ignored, Refused, Lapsed, Misread;
        public long Charged, Nominal;
        public long SubmittedTick;
        /// <summary>The sim's reason when a queued command was refused at its tick, or a blocked compulsion; null otherwise.</summary>
        public string Blocked;

        public bool Pending => Accepted + Rejected < Submitted;
        public int Rejected;
        public int Responded => Followed + Ignored + Refused + Lapsed + Misread;
        public int Delivered => Math.Max(Perceived, Responded - Lapsed);
        public int NotReached => Math.Max(0, Accepted - Delivered);

        public string Text()
        {
            if (Pending && Accepted == 0 && Rejected == 0) return Label + ": pending (queued for the next tick)";
            var s = Label + ": " + Delivered + " delivered, " + Followed + " followed, " + Refused + " refused";
            if (Ignored > 0) s += ", " + Ignored + " ignored";
            if (Misread > 0) s += ", " + Misread + " misread";
            if (Lapsed > 0) s += ", " + Lapsed + " lapsed";
            s += ", " + NotReached + " not reached";
            if (Blocked != null) s += " (blocked: " + Blocked + ")";
            if (Rejected > 0 && Blocked == null) s += " (" + Rejected + " refused at its tick)";
            return s;
        }
    }

    /// <summary>One miracle's effect as the sim reported it (before and after), for the cards and the tray.</summary>
    public sealed class MiracleNote
    {
        public int Intervention, God;
        public string Kind, Field;
        public int ThingId;
        public long Before, After, Count;
        public string Text;
    }

    /// <summary>A sign for the cue layer: what happened and to whom or where.</summary>
    public struct CueSignal
    {
        public CueKind Kind;
        public int Person;
        public int Node;
    }

    /// <summary>
    /// Turns the sim's events into what the divine HUD shows: instruction chips, miracle notes, the last spend, per-person divine rows and cue
    /// signals. Events of D1 are read by type; D2 and D3 events (814, 815-817, 825-828) by <b>name and field name</b>, so a branch without them still
    /// compiles and a later merge needs no edit here (R13: thin adapter in one place). Pure (no Unity), so it is EditMode testable.
    /// </summary>
    public sealed class DivineFeed
    {
        private readonly List<InstructionChip> _chips = new List<InstructionChip>();
        private readonly Dictionary<int, InstructionChip> _byIntervention = new Dictionary<int, InstructionChip>();
        private readonly List<MiracleNote> _notes = new List<MiracleNote>();
        private readonly Dictionary<int, List<DivineRow>> _rows = new Dictionary<int, List<DivineRow>>();
        private readonly List<CueSignal> _signals = new List<CueSignal>();
        private int _serial;

        public int God;
        public IReadOnlyList<InstructionChip> Chips => _chips;
        public IReadOnlyList<MiracleNote> Notes => _notes;
        /// <summary>The balance after the last spend (milli), -1 before any, and what it cost.</summary>
        public long LastBalance = -1, LastCharged, LastNominal;
        public bool LastWaived;
        public int Spends;
        /// <summary>Created resources reported by the ledger events, by count.</summary>
        public long CreatedUnits, DestroyedUnits;
        public int MaxChips = 12;

        /// <summary>Raised for every cue the events imply (the cue layer draws them).</summary>
        public event Action<CueSignal> Cue;

        /// <summary>Signals produced since the last call (tests read them too).</summary>
        public List<CueSignal> TakeSignals() { var l = new List<CueSignal>(_signals); _signals.Clear(); return l; }

        /// <summary>The divine rows of a person: what was issued, perceived, understood, answered.</summary>
        public IReadOnlyList<DivineRow> RowsOf(int npc) => _rows.TryGetValue(npc, out var l) ? l : (IReadOnlyList<DivineRow>)Array.Empty<DivineRow>();

        // ---------------------------------------------------------------- sending side

        /// <summary>Records a command the player sent (accepted by the gate, waiting for its tick). Returns the chip (a group shares one).</summary>
        public InstructionChip Sent(string label, InterventionKind kind, DivineAct act, int god, long tick, InstructionChip join = null)
        {
            if (join != null) { join.Submitted++; return join; }
            var c = new InstructionChip { Serial = ++_serial, Label = label, Kind = kind, Act = act, God = god, Submitted = 1, SubmittedTick = tick };
            _chips.Add(c);
            while (_chips.Count > MaxChips) { var old = _chips[0]; _chips.RemoveAt(0); foreach (int i in old.Interventions) _byIntervention.Remove(i); }
            return c;
        }

        // ---------------------------------------------------------------- events

        public void Accept(SimEvent e)
        {
            if (e == null) return;
            switch (e)
            {
                case MythicSpent ms: OnSpent(ms); return;
                case DivineActionAccepted da: OnAccepted(da); return;
                case CommandRejected cr: OnRejected(cr); return;
                case Sim.Population.NpcDied nd: Cue?.Invoke(new CueSignal { Kind = CueKind.Death, Person = nd.Npc, Node = nd.Node }); return;
            }
            string n = e.GetType().Name;
            switch (n)
            {
                case "CommandPerceived": OnPerceived(e); break;
                case "CommandResponded": OnResponded(e); break;
                case "CompulsionStarted": Signal(CueKind.Compel, e); break;
                case "CompulsionBlocked": OnBlocked(e); break;
                case "MiracleCast": OnMiracleCast(e); break;
                case "MiracleEffectApplied": OnEffect(e); break;
                case "ResourceCreated": { CreatedUnits += Num(e, "Count"); Signal(CueKind.Create, e, node: (int)Num(e, "Node")); break; }
                case "ResourceDestroyed": DestroyedUnits += Num(e, "Count"); break;
                case "DivineSuggestionIssued": Signal(CueKind.Suggest, e); break;
                case "DivineCommandIssued": Signal(CueKind.Command, e); break;
            }
        }

        private void OnSpent(MythicSpent ms)
        {
            Spends++;
            if (ms.God == God || God == 0) { LastBalance = ms.Balance; LastCharged = ms.Amount; LastNominal = ms.Nominal; LastWaived = ms.Waived; }
            if (_byIntervention.TryGetValue(ms.Intervention, out var c)) { c.Charged += ms.Amount; c.Nominal += ms.Nominal; }
            else pendingSpend[ms.Intervention] = ms;
        }

        private readonly Dictionary<int, MythicSpent> pendingSpend = new Dictionary<int, MythicSpent>();

        private void OnAccepted(DivineActionAccepted da)
        {
            // the first chip of the same kind (and act, when the event names one) that still waits for its intervention
            InstructionChip hit = null;
            foreach (var c in _chips)
            {
                if (c.God != da.God || c.Kind != da.Kind || c.Accepted + c.Rejected >= c.Submitted) continue;
                if (da.Act != DivineAct.None && c.Act != DivineAct.None && c.Act != da.Act && da.Kind != InterventionKind.Miracle) continue;
                hit = c; break;
            }
            if (hit == null) return;
            hit.Accepted++; hit.Interventions.Add(da.Intervention); _byIntervention[da.Intervention] = hit;
            if (pendingSpend.TryGetValue(da.Intervention, out var ms)) { hit.Charged += ms.Amount; hit.Nominal += ms.Nominal; pendingSpend.Remove(da.Intervention); }
            if (da.TargetKind == DivineTargetKind.Npc) AddRow(da.TargetId, new DivineRow(Sim.Information.KnowledgeLayer.DivineObservation, "Issued", Describe(da)));
            var kind = da.Kind == InterventionKind.Suggestion ? CueKind.Suggest : da.Kind == InterventionKind.Command ? CueKind.Command : da.Kind == InterventionKind.Compulsion ? CueKind.Compel : CueKind.None;
            if (kind != CueKind.None) Emit(new CueSignal { Kind = kind, Person = da.TargetKind == DivineTargetKind.Npc ? da.TargetId : 0, Node = da.TargetNode });
        }

        private static string Describe(DivineActionAccepted da) => da.Kind + (da.Act != DivineAct.None ? " " + da.Act : "") + " (" + da.Charged / 1000 + " Mythic)";

        private void OnRejected(CommandRejected cr)
        {
            if (!(cr.Command is DivineCommand dc)) return;
            foreach (var c in _chips)
                if (c.God == dc.God && c.Accepted + c.Rejected < c.Submitted && Fits(c, cr.Command)) { c.Rejected++; c.Blocked = cr.Result.Reason; return; }
        }

        private static bool Fits(InstructionChip c, Command cmd)
        {
            switch (c.Kind)
            {
                case InterventionKind.Suggestion: return cmd is IssueSuggestion;
                case InterventionKind.Command: return cmd is IssueCommand;
                case InterventionKind.Compulsion: return cmd is IssueCompulsion;
                case InterventionKind.Miracle: return cmd is CastMiracle;
                case InterventionKind.Commandment: return cmd is RevealCommandment;
                default: return false;
            }
        }

        private void OnPerceived(SimEvent e)
        {
            int iv = (int)Num(e, "Intervention"), npc = (int)Num(e, "Npc");
            if (_byIntervention.TryGetValue(iv, out var c)) c.Perceived++;
            AddRow(npc, new DivineRow(Sim.Information.KnowledgeLayer.Observed, "Perceived", "heard it" + (Str(e, "Heard") is string h && h.Length > 0 ? ": " + h : "")));
        }

        private void OnResponded(SimEvent e)
        {
            int iv = (int)Num(e, "Intervention"), npc = (int)Num(e, "Npc");
            string resp = Str(e, "Response") ?? "";
            if (_byIntervention.TryGetValue(iv, out var c))
            {
                if (resp.IndexOf("Follow", StringComparison.OrdinalIgnoreCase) >= 0) c.Followed++;
                else if (resp.IndexOf("Refus", StringComparison.OrdinalIgnoreCase) >= 0) c.Refused++;
                else if (resp.IndexOf("Ignor", StringComparison.OrdinalIgnoreCase) >= 0) c.Ignored++;
                else if (resp.IndexOf("Lapse", StringComparison.OrdinalIgnoreCase) >= 0) c.Lapsed++;
                else if (resp.IndexOf("Misread", StringComparison.OrdinalIgnoreCase) >= 0) c.Misread++;
            }
            AddRow(npc, new DivineRow(Sim.Information.KnowledgeLayer.Observed, "Response", resp.Length > 0 ? resp : "answered"));
            string understood = Str(e, "Understood");
            if (!string.IsNullOrEmpty(understood)) AddRow(npc, new DivineRow(Sim.Information.KnowledgeLayer.Belief, "As understood", understood));
        }

        private void OnBlocked(SimEvent e)
        {
            string why = Str(e, "Reason") ?? "blocked";
            int iv = (int)Num(e, "Intervention");
            if (_byIntervention.TryGetValue(iv, out var c)) c.Blocked = why;
            else foreach (var k in _chips) if (k.Kind == InterventionKind.Compulsion && k.Blocked == null && k.Accepted > 0) { k.Blocked = why; break; }
        }

        private void OnMiracleCast(SimEvent e)
        {
            string kind = Str(e, "Kind") ?? "Miracle";
            int target = (int)Num(e, "TargetId");
            var cue = kind.IndexOf("Heal", StringComparison.OrdinalIgnoreCase) >= 0 || kind.IndexOf("Bless", StringComparison.OrdinalIgnoreCase) >= 0 || kind.IndexOf("Protect", StringComparison.OrdinalIgnoreCase) >= 0 ? CueKind.Heal
                : kind.IndexOf("Smite", StringComparison.OrdinalIgnoreCase) >= 0 ? CueKind.Smite : kind.IndexOf("Create", StringComparison.OrdinalIgnoreCase) >= 0 ? CueKind.Create : CueKind.None;
            if (cue != CueKind.None) Emit(new CueSignal { Kind = cue, Person = target, Node = (int)Num(e, "TargetNode") });
        }

        private void OnEffect(SimEvent e)
        {
            var n = new MiracleNote { Intervention = (int)Num(e, "Intervention"), Field = Str(e, "Field"), ThingId = (int)Num(e, "ThingId"), Before = Num(e, "Before"), After = Num(e, "After"), Kind = Str(e, "ThingKind") };
            n.Text = (n.Kind ?? "thing") + " " + n.ThingId + ": " + n.Field + " " + n.Before + " -> " + n.After;
            _notes.Add(n); if (_notes.Count > 40) _notes.RemoveAt(0);
            if (string.Equals(n.Kind, "Npc", StringComparison.OrdinalIgnoreCase) || string.Equals(n.Kind, "Person", StringComparison.OrdinalIgnoreCase))
                AddRow(n.ThingId, new DivineRow(Sim.Information.KnowledgeLayer.DivineObservation, "Miracle", n.Field + " " + n.Before + " -> " + n.After));
        }

        // ---------------------------------------------------------------- helpers

        private void Signal(CueKind k, SimEvent e, int node = 0)
        {
            Emit(new CueSignal { Kind = k, Person = (int)Num(e, "Npc"), Node = node != 0 ? node : (int)Num(e, "Node") });
        }

        private void Emit(CueSignal s) { _signals.Add(s); Cue?.Invoke(s); if (_signals.Count > 64) _signals.RemoveAt(0); }

        private void AddRow(int npc, DivineRow row)
        {
            if (npc == 0) return;
            if (!_rows.TryGetValue(npc, out var l)) _rows[npc] = l = new List<DivineRow>();
            l.Add(row); if (l.Count > 20) l.RemoveAt(0);
        }

        private static readonly Dictionary<(Type, string), MemberInfo> Members = new Dictionary<(Type, string), MemberInfo>();

        private static MemberInfo Member(Type t, string name)
        {
            if (Members.TryGetValue((t, name), out var m)) return m;
            m = (MemberInfo)t.GetField(name, BindingFlags.Public | BindingFlags.Instance) ?? t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            Members[(t, name)] = m;
            return m;
        }

        private static object Get(object o, string name)
        {
            var m = Member(o.GetType(), name);
            if (m is FieldInfo f) return f.GetValue(o);
            if (m is PropertyInfo p) return p.GetValue(o);
            return null;
        }

        /// <summary>A numeric (or enum, or bool) field by name; 0 when the event has no such field.</summary>
        public static long Num(object o, string name)
        {
            var v = Get(o, name);
            if (v == null) return 0;
            if (v is bool b) return b ? 1 : 0;
            if (v is Enum en) return Convert.ToInt64(en);
            try { return Convert.ToInt64(v); } catch { return 0; }
        }

        /// <summary>A field by name as text (an enum as its name); null when absent.</summary>
        public static string Str(object o, string name) => Get(o, name)?.ToString();
    }

    /// <summary>One row of a person's Divine tab: its knowledge layer is required (a row without one is a bug the tests catch).</summary>
    public readonly struct DivineRow
    {
        public readonly Sim.Information.KnowledgeLayer Layer;
        public readonly string Head, Text;
        public DivineRow(Sim.Information.KnowledgeLayer layer, string head, string text) { Layer = layer; Head = head; Text = text; }

        public static string LayerLabel(Sim.Information.KnowledgeLayer l)
        {
            switch (l)
            {
                case Sim.Information.KnowledgeLayer.Truth: return "truth";
                case Sim.Information.KnowledgeLayer.Observed: return "observed";
                case Sim.Information.KnowledgeLayer.Communicated: return "told";
                case Sim.Information.KnowledgeLayer.Belief: return "belief";
                case Sim.Information.KnowledgeLayer.DivineObservation: return "divine sight";
                default: return l.ToString().ToLowerInvariant();
            }
        }
    }
}
