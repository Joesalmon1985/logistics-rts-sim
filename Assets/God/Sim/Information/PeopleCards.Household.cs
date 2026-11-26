using System.Collections.Generic;
using God.Sim.Population;
using God.Sim.Population.Households;

namespace God.Sim.Information
{
    /// <summary>
    /// The household tab of a person's card (OWNER: M4): its life stage, household, believed kin and care, as the faction's
    /// records hold them (FullInformation in stage 4). The family view lists the person's own believed kin by generation:
    /// what it believes, with how sure and from what cue, never the pedigree.
    /// </summary>
    public sealed class HouseholdCard
    {
        /// <summary>Lines for the tab, in order.</summary>
        public List<string> Lines = new List<string>();
        /// <summary>The family view: believed kin, oldest generation first (role, id, confidence, source).</summary>
        public List<string> Family = new List<string>();
        public int Household, Hut, StoreFood, Members, Children;
        public bool Pregnant, Ill;
        /// <summary>Growth (children's size, permille) and the partners it is with.</summary>
        public int Growth = 1000;
        public List<int> Partners = new List<int>();
    }

    /// <summary>Fills <see cref="HouseholdCard"/> for the inspector (OWNER: M4). Called by <c>PopulationKnowledge.Inspect</c>; read-only.</summary>
    public static class HouseholdCards
    {
        public static void Fill(GodState s, Npc n, NpcInspection card)
        {
            var c = card.Household;
            var hs = s.Households;
            if (hs == null || n.Life == null) return;
            var p = s.Population;
            var l = n.Life;
            // an M4 person's age is its own (founders included): the HUD age follows it
            card.HudAge = SimCalendar.HudAge(card.AgeMilli);
            c.Growth = l.Growth; c.Pregnant = l.PregFather != 0; c.Ill = l.Illness == 1;
            c.Lines.Add(StageWords(n.Stage) + ", age " + card.HudAge + (n.Stage == LifeStage.Child ? ", size " + l.Growth / 10 + "%" : "") + (l.Generation > 0 ? ", generation " + l.Generation : ", a founder"));
            if (c.Pregnant) c.Lines.Add("Expecting a child");
            if (c.Ill) c.Lines.Add("Ill with fever (" + l.IllDaysLeft + " days left)" + (l.TendedDay >= SimCalendar.Of(s).Day(s.Tick) - 1 ? ", tended" : ""));
            foreach (var pr in hs.Partnerships)
            {
                int other = pr.A == n.Id ? pr.B : pr.B == n.Id ? pr.A : 0;
                if (other == 0) continue;
                c.Partners.Add(other);
                c.Lines.Add("Partner: #" + other + " since day " + SimCalendar.Of(s).Day(pr.StartTick));
            }
            if (l.TryWith != 0 && l.TryingUntil >= s.Tick) c.Lines.Add("Wants a child with #" + l.TryWith);
            var h = n.Household != 0 ? hs.Find(n.Household) : null;
            if (h == null) c.Lines.Add("No household");
            else
            {
                c.Household = h.Id; c.Hut = h.Hut; c.Members = h.Members.Count;
                c.Lines.Add("Household " + h.Id + (h.Hut != 0 ? ", hut " + h.Hut : ", no hut") + ", " + h.Members.Count + " members");
                foreach (int m in h.Members)
                {
                    if (m == n.Id) continue;
                    var o = p.Find(m);
                    if (o == null) continue;
                    if (o.Stage == LifeStage.Child) c.Children++;
                    var role = KinData.RoleOf(n.Mind, m);
                    c.Lines.Add("  #" + m + " " + StageWords(o.Stage).ToLowerInvariant() + (role != KinRole.None ? ", " + RoleWords(role) : ", hut-mate"));
                }
                var bin = h.Bin != 0 ? Goods.BinById(p, h.Bin) : null;
                if (bin != null)
                {
                    int food = 0, other = 0;
                    foreach (var lot in bin.Lots) if (Wares.IsFood(lot.Ware)) food += lot.Count; else other += lot.Count;
                    c.StoreFood = food;
                    c.Lines.Add("Store: " + food + " food" + (other > 0 ? ", " + other + " other" : ""));
                }
            }
            if (l.OrphanSince >= 0) c.Lines.Add("Orphaned on day " + l.OrphanSince + ", needs a home");
            if (l.Fostered) c.Lines.Add(l.ToldFostered ? "Fostered (it knows)" : "Fostered (it was not told)");
            foreach (var a in hs.Care)
                if (a.Asker == n.Id || a.Carer == n.Id)
                    c.Lines.Add("Shared care with #" + (a.Asker == n.Id ? a.Carer : a.Asker) + ": given " + a.Given + ", returned " + a.Returned + " ticks");
            // the family view: its own beliefs, by generation
            var k = n.Mind?.Kin;
            if (k == null) return;
            var order = new[]
            {
                KinRole.Grandparent, KinRole.Mother, KinRole.Father, KinRole.StepParent, KinRole.FosterParent, KinRole.Uncle, KinRole.Aunt,
                KinRole.Partner, KinRole.FullSibling, KinRole.HalfSibling, KinRole.Cousin, KinRole.InLaw,
                KinRole.Daughter, KinRole.Son, KinRole.StepChild, KinRole.FosterChild, KinRole.Nephew, KinRole.Niece, KinRole.Grandchild,
            };
            foreach (var role in order)
                foreach (var b in k.Beliefs)
                {
                    if (b.Role != role) continue;
                    var o = p.Find(b.Other);
                    c.Family.Add(Indent(role) + RoleWords(role) + ": #" + b.Other + (o != null && !o.Alive ? " (dead)" : "") + "  (" + b.Confidence / 10 + "% sure, " + b.Source.ToString().ToLowerInvariant() + ")");
                }
        }

        private static string Indent(KinRole r)
        {
            switch (r)
            {
                case KinRole.Grandparent: return "";
                case KinRole.Mother: case KinRole.Father: case KinRole.StepParent: case KinRole.FosterParent: case KinRole.Uncle: case KinRole.Aunt: return "  ";
                case KinRole.Daughter: case KinRole.Son: case KinRole.StepChild: case KinRole.FosterChild: case KinRole.Nephew: case KinRole.Niece: return "      ";
                case KinRole.Grandchild: return "        ";
                default: return "    ";
            }
        }

        public static string StageWords(LifeStage st) => st == LifeStage.Child ? "Child" : st == LifeStage.Elder ? "Elder" : "Adult";

        public static string RoleWords(KinRole r)
        {
            switch (r)
            {
                case KinRole.FullSibling: return "sibling";
                case KinRole.HalfSibling: return "half-sibling";
                case KinRole.StepParent: return "step-parent";
                case KinRole.StepChild: return "step-child";
                case KinRole.FosterParent: return "foster parent";
                case KinRole.FosterChild: return "foster child";
                case KinRole.InLaw: return "in-law";
                default: return r.ToString().ToLowerInvariant();
            }
        }
    }
}
