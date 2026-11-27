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
        /// <summary>D6: the five separate fields per god (layer Belief: what the person believes), ascending god.</summary>
        public List<GodBelief> Gods = new List<GodBelief>();
        /// <summary>D6: its religious affiliations (open spells), shown apart from its citizenship and community.</summary>
        public List<Affiliation> Affiliations = new List<Affiliation>();
        /// <summary>D6: community of origin (<c>Npc.Faction</c>) and citizenship (the faction stand-in until C5), shown separately.</summary>
        public int Community, Citizenship;
        /// <summary>D6: what the person thinks each commandment it knows says, with its state (rendered to text by the Game layer).</summary>
        public List<KnownCommandmentRow> Commandments = new List<KnownCommandmentRow>();
    }

    /// <summary>D6: one known commandment on the card: the person's interpretation (its belief) next to the god's current revision (divine observation).</summary>
    public struct KnownCommandmentRow
    {
        public int Commandment, God, Revision, CurrentRevision;
        public CommandmentStance Stance;
        public int Adoption, Salience;
        public CommandmentSource Source;
        public int FromNpc;
        public bool ToldRevoked, Revoked;
        /// <summary>Its interpretation (1 or 2 rules) and the god's current rules.</summary>
        public CommandmentRule[] Interpretation, Current;
        /// <summary>Its interpretation differs from the revision it holds as issued (analytics truth shown to the god).</summary>
        public bool Misunderstood;
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
            FillD6(s, n, c); // D6: beliefs, affiliations, commandments
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

    /// <summary>D6's rows of the divine card (OWNER: D6). Read-only.</summary>
    public static partial class DivineCards
    {
        private static void FillD6(GodState s, Npc n, DivineCard c)
        {
            c.Gods.Clear(); c.Affiliations.Clear(); c.Commandments.Clear();
            c.Community = (int)n.Faction; c.Citizenship = Beliefs.CitizenshipOf(s, n);
            if (s.Divinity == null) return;
            var m = n.Mind?.Divine;
            if (m != null) for (int i = 0; i < m.GodCount; i++) c.Gods.Add(m.Gods[i]);
            var st = s.Divinity.Doctrine;
            if (st == null) return;
            foreach (var a in st.Affiliations) if (a.Npc == n.Id && a.Open) c.Affiliations.Add(a);
            if (m == null) return;
            var tu = s.Tuning.Divinity.Doctrine;
            for (int i = 0; i < m.KnownCount; i++)
            {
                var k = m.Known[i];
                var cm = st.Find(k.Commandment);
                var rules = k.RuleCount > 1 ? new[] { k.Interpretation, k.Interpretation2 } : new[] { k.Interpretation };
                c.Commandments.Add(new KnownCommandmentRow
                {
                    Commandment = k.Commandment, God = k.God, Revision = k.Revision, CurrentRevision = cm?.Revision ?? 0, Stance = Doctrine.StanceOf(tu, k),
                    Adoption = k.Adoption, Salience = k.Salience, Source = k.Source, FromNpc = k.FromNpc, ToldRevoked = k.ToldRevoked, Revoked = cm?.Revoked ?? false,
                    Interpretation = rules, Current = cm?.Rules, Misunderstood = Doctrine.VariantMask(s, k) != 0,
                });
                c.Lines.Add("Commandment " + k.Commandment + " (rev " + k.Revision + "): " + Doctrine.StanceOf(tu, k) + ", adoption " + k.Adoption + ", salience " + k.Salience);
            }
            for (int i = 0; i < m.GodCount; i++)
            {
                var g = m.Gods[i];
                c.Lines.Add("God " + g.God + ": belief " + g.Belief + ", faith " + g.Faith + ", trust " + g.Trust + ", fear " + g.Fear + ", loyalty " + g.Loyalty);
            }
        }
    }
}
