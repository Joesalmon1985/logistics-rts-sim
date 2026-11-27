using God.Sim.Civ;
using God.Sim.Population;

namespace God.Sim.Information
{
    /// <summary>
    /// C3's rows of the economy tab (OWNER: C3; read-only): the contracts the person holds (truth: the record, with its
    /// status, units earned, paid and in arrears, and its wage history counters), then the person's own beliefs of each:
    /// the terms as it understood them and the arrears it believes (layer 6; they may differ from the record).
    /// </summary>
    public static partial class CivCards
    {
        static partial void FillC3(GodState s, Npc n, CivCard c)
        {
            void Row(byte kind, KnowledgeLayer layer, int reference, string text) { c.Rows.Add(new CivCardRow(kind, layer, reference, text)); c.Lines.Add("[" + layer + "] " + text); }
            var e = s.Civ?.Employment;
            if (e != null)
                foreach (var k in e.Contracts)
                {
                    if (k.Worker != n.Id && k.Agent != n.Id) continue;
                    bool worker = k.Worker == n.Id;
                    Row(CivCardRow.ContractHeld, KnowledgeLayer.Truth, k.Id, "contract #" + k.Id + (worker ? " working for " + k.Employer : " employing person " + k.Worker) + " on asset #" + k.Asset
                        + ": " + k.Status.ToString().ToLowerInvariant() + (k.Status == ContractStatus.Ended ? " (" + k.Reason.ToString().ToLowerInvariant() + ")" : "")
                        + ", pay " + k.Terms.Pay.Amount + " " + k.Terms.Pay.Ware.ToString().ToLowerInvariant() + " per " + k.Terms.Per.ToString().ToLowerInvariant()
                        + "; units earned " + k.UnitsEarned + ", paid " + k.UnitsPaid + ", unpaid " + k.UnitsUnpaid + ", arrears " + k.ArrearsUnits);
                }
            var cm = n.Mind?.Civ;
            if (cm == null) return;
            foreach (var j in cm.Jobs)
            {
                Row(CivCardRow.TermsBelieved, KnowledgeLayer.Belief, j.Contract, "believes contract #" + j.Contract + " (" + (j.Side == JobBelief.AsWorker ? "as worker" : "as employer") + ") is "
                    + j.Status.ToString().ToLowerInvariant() + ", pay " + j.PayCount + " " + j.PayWare.ToString().ToLowerInvariant() + " per " + j.Per.ToString().ToLowerInvariant()
                    + ", trust " + j.Trust);
                Row(CivCardRow.ArrearsBelieved, KnowledgeLayer.Belief, j.Contract, "believes units worked " + j.UnitsWorked + ", paid " + j.UnitsPaid + ", unpaid " + j.Unpaid);
            }
        }
    }
}
