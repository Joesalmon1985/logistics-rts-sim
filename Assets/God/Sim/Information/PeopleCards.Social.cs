using System.Collections.Generic;
using God.Sim.Learning;
using God.Sim.Population;
using God.Sim.Social;

namespace God.Sim.Information
{
    /// <summary>One known person on the relations tab: the record and what the NPC has learned about dealing with that person.</summary>
    public sealed class RelationRow
    {
        public int Other;
        public string Class = "";
        public int Trust, Familiarity, Affection, Grudge;
        public int DealsDone, Cheated, GiftsIn, GiftsOut;
        /// <summary>The learned values keyed on this person (NpcRef), strongest first: "say accept -310".</summary>
        public List<string> Learned = new List<string>();
        /// <summary>The summed long-term learned value of its entries about this person (positive: dealing with them paid).</summary>
        public int LearnedSum;
    }

    /// <summary>The relations tab of a person's card (OWNER: M5): its known people (trust, grudge, familiarity, what it learned about each), its open deals and the messages it last heard, as its own memory holds them.</summary>
    public sealed class SocialCard
    {
        /// <summary>Lines for the tab, in order: open deals and recent messages.</summary>
        public List<string> Lines = new List<string>();
        /// <summary>Known people, most important first (at most 12).</summary>
        public List<RelationRow> Rows = new List<RelationRow>();
        public int RelCount, RelCap;
    }

    /// <summary>Fills <see cref="SocialCard"/> for the inspector (OWNER: M5). Called by <c>PopulationKnowledge.Inspect</c>; read-only.</summary>
    public static class SocialCards
    {
        public const int MaxRows = 12;

        public static void Fill(GodState s, Npc n, NpcInspection card)
        {
            var m = n.Mind?.Social;
            var c = card.Relations;
            if (m == null) return;
            c.RelCount = m.RelCount; c.RelCap = m.RelCap;
            int tpd = SimCalendar.Of(s).TicksPerDay;
            // most important first (ties by slot)
            var order = new List<int>();
            for (int i = 0; i < m.RelCount; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                int ia = SocialMemory.Importance(m, m.Rel[a], (int)s.Tick, tpd), ib = SocialMemory.Importance(m, m.Rel[b], (int)s.Tick, tpd);
                return ia != ib ? ib.CompareTo(ia) : a.CompareTo(b);
            });
            var st = n.Mind.Store;
            for (int k = 0; k < order.Count && k < MaxRows; k++)
            {
                var r = m.Rel[order[k]];
                var row = new RelationRow
                {
                    Other = r.Other, Class = SocialMemory.ClassOf(m, r).ToString(), Trust = r.Trust, Familiarity = r.Familiarity, Affection = r.Affection,
                    Grudge = r.Grudge, DealsDone = r.DealsDone, Cheated = r.Cheated, GiftsIn = r.GiftsIn, GiftsOut = r.GiftsOut,
                };
                LearnedAbout(st, r.Other, row);
                c.Rows.Add(row);
            }
            for (int i = 0; i < m.DealCount; i++)
            {
                var d = m.Deals[i];
                c.Lines.Add("deal #" + d.DealId + " with #" + d.Partner + ": " + d.Kind + ", " + d.Status + "; I give " + d.MyGiven + "/" + d.MyCount + " " + Wares.Name(d.MyWare)
                    + ", I get " + d.TheirGiven + "/" + d.TheirCount + " " + Wares.Name(d.TheirWare));
            }
            for (int i = m.InboxCount - 1; i >= 0; i--)
            {
                var e = m.Inbox[i];
                c.Lines.Add("heard #" + e.Msg.MessageId + " from " + (e.Msg.Speaker == 0 ? "someone" : "#" + e.Msg.Speaker) + ": " + Words(e.Msg) + (e.Answered ? " (answered)" : ""));
            }
        }

        /// <summary>A decoded message in words: "offer: wants 1 fish, gives 3 berries".</summary>
        public static string Words(in Comms.DecodedMessage msg)
        {
            string w = msg.Act.ToString().ToLowerInvariant();
            if (msg.Topic.Kind == Comms.TopicKind.Ware) w += ": " + (msg.Act == Comms.GestureAct.Offer ? "wants " : "") + (msg.Quantity > 0 ? msg.Quantity + " " : "") + Wares.Name((Ware)msg.Topic.Id);
            else if (msg.Topic.Kind == Comms.TopicKind.Place) w += ": at #" + msg.Topic.Id;
            if (msg.Counter.Kind == Comms.TopicKind.Ware) w += ", gives " + msg.Counter.Sub + " " + Wares.Name((Ware)msg.Counter.Id);
            return w;
        }

        private static void LearnedAbout(AssociationStore st, int other, RelationRow row)
        {
            if (st == null || st.Mode == LearningMode.Off) return;
            uint key = Feature.NpcRef(other);
            int at = st.Find(key, 0);
            if (at < 0) at = ~at;
            var tmp = new List<(int w, string text)>();
            for (int i = at; i < st.Count && st.Entries[i].Key == key; i++)
            {
                var e = st.Entries[i];
                int w = e.Stw + e.Ltw;
                row.LearnedSum += e.Ltw;
                tmp.Add((w, VerbTable.Name(e.Verb) + " " + (w >= 0 ? "+" : "") + w));
            }
            tmp.Sort((a, b) => System.Math.Abs(b.w).CompareTo(System.Math.Abs(a.w)));
            for (int i = 0; i < tmp.Count && i < 4; i++) row.Learned.Add(tmp[i].text);
        }
    }
}
