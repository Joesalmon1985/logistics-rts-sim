using System.Collections.Generic;
using God.Sim.Orgs;

namespace God.Sim.Policy
{
    /// <summary>
    /// The default decision procedure of an office-holder deciding as its organisation (ORGANISATIONS 4.4 step 5; generalises
    /// INSTITUTIONAL_STATE's manager utility): from its own reading of the records and its own mind only. It fills a vacant
    /// office with the member it trusts most (its own social memory); with the ration service, it proposes a smaller ration
    /// when the recorded stock per member is low and a larger one when it is high. It reads nothing else (ORG-INV-4).
    /// </summary>
    public sealed class MemberUtilityPolicy : IInstitutionPolicy
    {
        public const string RegistryName = "MemberUtility";
        public string Name => RegistryName;
        public PolicyLevel Level => PolicyLevel.Institution;

        public void Decide(in InstitutionView view, ref Rng rng, List<Command> outCommands)
        {
            InstitutionPolicies.FillVacancies(view, outCommands, byTrust: true);
            if ((view.Powers & (uint)OrgPower.Propose) == 0 || view.OpenProposals > 0) return;
            int ration = view.PolicyValue((int)OrgPolicyKey.RationPerRequest, 0);
            if (ration <= 0 || view.StoreRecorded == null || view.Members == null || view.Members.Count == 0) return;
            int stock = 0;
            for (int w = 0; w < view.StoreRecorded.Count; w++) if (Wares.IsFood((Ware)w) && (Ware)w != Ware.Nightshade) stock += System.Math.Max(0, view.StoreRecorded[w]);
            int perMember = stock / view.Members.Count;
            int want = perMember < 1 ? ration - 1 : perMember > 4 ? ration + 1 : ration;
            want = System.Math.Max(1, System.Math.Min(4, want));
            if (want != ration)
                outCommands.Add(new OrgSetPolicy { Org = view.OrgId, Actor = view.DeciderNpcId, Key = OrgPolicyKey.RationPerRequest, Value = want });
        }
    }

    /// <summary>The benchmark control: fills vacancies with the lowest-id member on the roster, never changes a policy.</summary>
    public sealed class RuleFollowerPolicy : IInstitutionPolicy
    {
        public const string RegistryName = "RuleFollower";
        public string Name => RegistryName;
        public PolicyLevel Level => PolicyLevel.Institution;

        public void Decide(in InstitutionView view, ref Rng rng, List<Command> outCommands) => InstitutionPolicies.FillVacancies(view, outCommands, byTrust: false);
    }

    public static class InstitutionPolicies
    {
        /// <summary>The policy of a registry name (null for an unknown name). "none" gives the default.</summary>
        public static IInstitutionPolicy Create(string name)
        {
            if (string.IsNullOrEmpty(name) || name == PolicyRegistry.None || name == MemberUtilityPolicy.RegistryName) return new MemberUtilityPolicy();
            if (name == RuleFollowerPolicy.RegistryName) return new RuleFollowerPolicy();
            return null;
        }

        /// <summary>For each vacant office (as recorded): propose the member who holds no office, by its own trust (or the lowest id).</summary>
        public static void FillVacancies(in InstitutionView view, List<Command> outCommands, bool byTrust)
        {
            if ((view.Powers & (uint)OrgPower.Propose) == 0 || view.Offices == null || view.Members == null) return;
            foreach (var o in view.Offices)
            {
                if (o.Holder != 0) continue;
                int best = 0, bestTrust = int.MinValue;
                for (int i = 0; i < view.Members.Count; i++)
                {
                    int m = view.Members[i];
                    bool holds = false;
                    foreach (var x in view.Offices) if (x.Holder == m) holds = true;
                    if (holds) continue;
                    int t = byTrust && view.MemberTrust != null && i < view.MemberTrust.Count ? view.MemberTrust[i] : 0;
                    if (m == view.DeciderNpcId) t -= 1; // others first, at equal trust
                    if (t > bestTrust) { bestTrust = t; best = m; }
                }
                if (best == 0) continue;
                outCommands.Add(new OrgAppoint { Org = view.OrgId, Actor = view.DeciderNpcId, Office = o.Index, Npc = best });
                return; // one appointment per think
            }
        }

        /// <summary>
        /// A present member's vote, by its own interest (from its own body and what it carries): a larger ration is welcome when
        /// it is hungry or has no food; a smaller one when it is fed and carries food; anything else is accepted.
        /// </summary>
        public static bool MemberVote(int hunger, int foodInHand, DecisionWhat what, OrgPolicyKey key, int current, int proposed)
        {
            if (what != DecisionWhat.SetPolicy || key != OrgPolicyKey.RationPerRequest) return true;
            if (proposed > current) return hunger >= 400 || foodInHand == 0;
            if (proposed < current) return hunger < 400 && foodInHand >= 2;
            return true;
        }
    }
}
