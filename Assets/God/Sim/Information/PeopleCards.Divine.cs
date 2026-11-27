using System.Collections.Generic;
using God.Sim.Divinity;
using God.Sim.Population;

namespace God.Sim.Information
{
    /// <summary>F1.14: the divine tab of a person's card (OWNER: D2/D6). Its rows carry their knowledge layer.</summary>
    public sealed class DivineCard
    {
        public KnowledgeLayer Layer = KnowledgeLayer.Belief;
        /// <summary>Lines for the tab, in order (empty in F1).</summary>
        public List<string> Lines = new List<string>();
        /// <summary>D2 (INFORMATION 12.4): one row per instruction this person is or was a recipient of.</summary>
        public List<DivineInboxRow> Inbox = new List<DivineInboxRow>();
    }

    /// <summary>
    /// D2: one instruction on the card as three items when they differ: what the god issued (divine observation), whether it
    /// was perceived and how it was answered (truth of the sim's record), and what the person understood (its belief).
    /// </summary>
    public struct DivineInboxRow
    {
        public int Intervention, God;
        public InterventionKind Kind;
        /// <summary>Issued (layer <c>DivineObservation</c>).</summary>
        public DivineAct IssuedAct;
        /// <summary>Perceived, and the response so far (<c>None</c>: undecided).</summary>
        public RecipientState State;
        public CommandResponse Response;
        /// <summary>As understood (layer <c>Belief</c>; <c>None</c> when not in its inbox now).</summary>
        public DivineAct UnderstoodAct;
        public long Expires;
        /// <summary>The understood act differs from the issued one (three items shown).</summary>
        public bool Differs => UnderstoodAct != DivineAct.None && UnderstoodAct != IssuedAct;
    }

    /// <summary>Fills <see cref="DivineCard"/> for the inspector (OWNER: D2/D6). Called by <c>PopulationKnowledge.Inspect</c>; read-only.</summary>
    public static partial class DivineCards
    {
        public static void Fill(GodState s, Npc n, NpcInspection card)
        {
            var c = card.Divine;
            c.Inbox.Clear();
            c.Lines.Clear();
            var reg = s.Divinity?.Messages;
            if (reg == null) return;
            var m = n.Mind?.Divine;
            foreach (var ins in reg.Items)
            {
                int i = ins.IndexOf(n.Id);
                if (i < 0) continue;
                var r = ins.Recipients[i];
                var row = new DivineInboxRow
                {
                    Intervention = ins.Id, God = ins.God, Kind = ins.Kind, IssuedAct = ins.Act, State = r.State, Response = r.Response, Expires = ins.Expires,
                };
                int e = m?.IndexOf(ins.Id) ?? -1;
                if (e >= 0) row.UnderstoodAct = m.Inbox[e].Act;
                c.Inbox.Add(row);
                string line = ins.Kind + " " + ins.Act + " from god " + ins.God + ": " + (r.State == RecipientState.Responded ? r.Response.ToString() : r.State.ToString());
                if (row.Differs) line += " (understood: " + row.UnderstoodAct + ")";
                c.Lines.Add(line);
            }
        }
    }
}
