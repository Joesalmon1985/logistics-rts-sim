using System.Collections.Generic;
using God.Sim.Orgs;
using God.Sim.Population;

namespace God.Sim.Information
{
    /// <summary>F1.14: the organisations tab of a person's card (OWNER: C6). Its rows carry their knowledge layer.</summary>
    public sealed class OrgCard
    {
        public KnowledgeLayer Layer = KnowledgeLayer.Belief;
        /// <summary>Lines for the tab, in order; each starts with its layer in brackets.</summary>
        public List<string> Lines = new List<string>();
        /// <summary>The structured rows (U5 draws them).</summary>
        public List<OrgCardRow> Rows = new List<OrgCardRow>();
    }

    /// <summary>One row. Kind: 1 role held (truth of the record), 2 believed membership and terms (belief), 3 practice known (belief), 4 opportunity believed (belief).</summary>
    public readonly struct OrgCardRow
    {
        public const byte Role = 1, BelievedMembership = 2, Practice = 3, Opportunity = 4;
        public readonly byte Kind;
        public readonly KnowledgeLayer Layer;
        public readonly int Org;
        public readonly string Text;
        public OrgCardRow(byte kind, KnowledgeLayer layer, int org, string text) { Kind = kind; Layer = layer; Org = org; Text = text; }
    }

    /// <summary>One line of an organisation's records as the organisation wrote them (layer 4, kept apart from divine observation; U5 draws it).</summary>
    public readonly struct OrgRecordLine
    {
        public readonly long Tick; public readonly int Recorder; public readonly RecordKind Kind; public readonly int Ref; public readonly Ware Ware; public readonly int Count;
        public OrgRecordLine(long tick, int recorder, RecordKind kind, int reference, Ware ware, int count) { Tick = tick; Recorder = recorder; Kind = kind; Ref = reference; Ware = ware; Count = count; }
    }

    /// <summary>The organisation card data (U5): the record's identity, offices and roster, and its records as written (layer 4).</summary>
    public sealed class OrganisationCard
    {
        public KnowledgeLayer Layer = KnowledgeLayer.InstitutionRecord;
        public int Org;
        public string Name;
        public OrgStatus Status;
        public long Founded;
        public List<string> Offices = new List<string>();
        public List<int> Members = new List<int>();
        public List<OrgRecordLine> Records = new List<OrgRecordLine>();
        public List<string> Policies = new List<string>();
    }

    /// <summary>
    /// Fills <see cref="OrgCard"/> for the inspector (OWNER: C6). Called by <c>PopulationKnowledge.Inspect</c>; read-only.
    /// Roles held are the organisations' records (truth of the institution); memberships, terms, practices and opportunities
    /// are the person's own beliefs (layer 6), which may be wrong or stale.
    /// </summary>
    public static partial class OrgCards
    {
        static partial void FillC7(GodState s, Npc n, OrgCard c);

        public static void Fill(GodState s, Npc n, NpcInspection card)
        {
            var c = card.Orgs;
            var os = s.Organisations;
            if (os == null) return;
            void Row(byte kind, KnowledgeLayer layer, int org, string text) { c.Rows.Add(new OrgCardRow(kind, layer, org, text)); c.Lines.Add("[" + layer + "] " + text); }
            foreach (var org in os.Orgs)
            {
                if (!org.IsMember(n.Id)) continue;
                string office = "";
                foreach (var o in org.Offices) if (o.Holder == n.Id) office = ", " + o.Kind.ToString().ToLowerInvariant();
                Row(OrgCardRow.Role, KnowledgeLayer.InstitutionRecord, org.Id, "member of " + NameOf(org) + " #" + org.Id + office);
            }
            FillC7(s, n, c); // C7 (flagged additive edit): health and teaching rows
            var m = n.Mind?.Orgs;
            if (m == null) return;
            for (int i = 0; i < m.Count; i++)
            {
                var b = m.Beliefs[i];
                Row(OrgCardRow.BelievedMembership, KnowledgeLayer.Belief, b.Org, "believes " + (b.MyRole != 0 ? "it is a member of" : "there is") + " organisation #" + b.Org
                    + (b.Ration > 0 ? ", ration " + b.Ration : "") + ", trust " + b.Trust);
            }
            foreach (var t in OrgTemplates.Rows) if (m.Knows(t.Id)) Row(OrgCardRow.Practice, KnowledgeLayer.Belief, 0, "knows the practice of a " + t.Name);
            for (int i = 0; i < m.OppCount; i++)
            {
                var o = m.Opportunities[i];
                Row(OrgCardRow.Opportunity, KnowledgeLayer.Belief, 0, "believes a " + (OrgTemplates.Get(o.Template)?.Name ?? "body") + " would help (evidence " + o.NeedEvidence + ", " + o.AcceptedCount() + " agreed)");
            }
        }

        /// <summary>The organisation card (U5): the record and its records as written, never the true store or the true outcomes.</summary>
        public static OrganisationCard CardOf(GodState s, int orgId)
        {
            var org = s.Organisations?.Find(orgId);
            if (org == null) return null;
            var c = new OrganisationCard { Org = org.Id, Name = NameOf(org), Status = org.Status, Founded = org.Founded };
            foreach (var o in org.Offices) c.Offices.Add(o.Kind.ToString().ToLowerInvariant() + ": " + (o.Holder == 0 ? "vacant" : "#" + o.Holder));
            foreach (var r in org.Roles) if (!c.Members.Contains(r.Npc)) c.Members.Add(r.Npc);
            foreach (var r in org.Records) c.Records.Add(new OrgRecordLine(r.Tick, r.Recorder, r.Kind, r.Ref, r.Ware, r.Count));
            foreach (var p in org.Policies) c.Policies.Add(p.Key + " = " + p.Value);
            return c;
        }

        private static string NameOf(Organisation org) => OrgTemplates.Get(org.Template)?.Name ?? org.Type.ToString().ToLowerInvariant();
    }
}
