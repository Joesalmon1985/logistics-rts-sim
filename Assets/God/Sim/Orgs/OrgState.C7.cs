using System.Collections.Generic;

namespace God.Sim.Orgs
{
    /// <summary>
    /// C7's part of <see cref="OrgState"/> (F1.2: created empty by F1; OWNER: C7). Its sub-object type is declared below; the container hashes it in the fixed order.
    /// </summary>
    public sealed partial class OrgState
    {
        /// <summary>C7's sub-object, created on the first treatment or teaching (null until then: hashes as before).</summary>
        public HealthEducationState HealthOrCreate() => Health ?? (Health = new HealthEducationState());
    }

    /// <summary>A treatment in progress (one per healer): the healer walks to the patient and stays within <c>Reach.Touch</c>.</summary>
    public struct Treatment
    {
        public int Request, Org, Healer, Patient;
        public int Skill, RemedyLot, SeenNeed;
        public long StartTick;
        public int Progress;
        /// <summary>Truth at the start (analytics only).</summary>
        public int SeverityBefore, InjuryBefore, HealthBefore;
    }

    /// <summary>
    /// The effect of a completed treatment on one patient (one record per patient, the latest treatment): the multipliers the
    /// healer's skill and remedy give on the tended days, applied to the patient's actual body by <see cref="Healthcare"/>'s hook
    /// into the M4 path (no faction or organisation modifier exists).
    /// </summary>
    public struct TreatedPatient
    {
        public int Patient, Healer;
        /// <summary>The sim day the treatment completed (its tending counts that day and the next, as M4's TendedDay).</summary>
        public int Day;
        public long Tick;
        /// <summary>The treated severity multiplier (permille, replaces M4's TendSeverityPermille on those days) and injury recovery multiplier (permille).</summary>
        public int SeverityPermille, InjuryPermille;
        /// <summary>Injury recovery is multiplied until this tick (one day after completion).</summary>
        public long InjuryUntil;
        /// <summary>Health credited ahead of the patient's daily disease step that could not fit under the maximum (credited after it).</summary>
        public int Pending;
        public int PendingDay;
        /// <summary>The last day for which the disease credit was applied (each day once).</summary>
        public int CreditedDay;
        public bool RecoveryCredited;
    }

    /// <summary>A teaching session in progress (one per teacher, at most <c>MaxLearnersPerSession</c> learners).</summary>
    public struct TeachSession
    {
        public int Teacher, Topic, Org;
        public byte Kind;
        public long StartTick;
        public int Progress;
        public int L0, L1, L2, L3;
        public int Learner(int i) => i == 0 ? L0 : i == 1 ? L1 : i == 2 ? L2 : L3;
        public void SetLearner(int i, int v) { if (i == 0) L0 = v; else if (i == 1) L1 = v; else if (i == 2) L2 = v; else L3 = v; }
        public int LearnerCount => (L0 != 0 ? 1 : 0) + (L1 != 0 ? 1 : 0) + (L2 != 0 ? 1 : 0) + (L3 != 0 ? 1 : 0);
    }

    /// <summary>C7's sub-object (OWNER: C7): treatments in progress, their effects on patients, teaching sessions, counters.</summary>
    public sealed partial class HealthEducationState
    {
        /// <summary>Ascending healer id.</summary>
        public List<Treatment> Active = new List<Treatment>();
        /// <summary>Ascending patient id.</summary>
        public List<TreatedPatient> Treated = new List<TreatedPatient>();
        /// <summary>Ascending teacher id.</summary>
        public List<TeachSession> Sessions = new List<TeachSession>();
        public int TreatmentsStarted, TreatmentsCompleted, RemediesUsed, HerbsGathered, SessionsHeld, EntriesCopied;
        /// <summary>Health credited by treatments in total (micro; analytics).</summary>
        public long HealthCredited, InjuryHealed;

        partial void Hash(ref StateHasher h)
        {
            h.Add(Active.Count);
            foreach (var t in Active)
            {
                h.Add(t.Request); h.Add(t.Org); h.Add(t.Healer); h.Add(t.Patient); h.Add(t.Skill); h.Add(t.RemedyLot); h.Add(t.SeenNeed); h.Add(t.StartTick); h.Add(t.Progress);
                h.Add(t.SeverityBefore); h.Add(t.InjuryBefore); h.Add(t.HealthBefore);
            }
            h.Add(Treated.Count);
            foreach (var t in Treated)
            {
                h.Add(t.Patient); h.Add(t.Healer); h.Add(t.Day); h.Add(t.Tick); h.Add(t.SeverityPermille); h.Add(t.InjuryPermille); h.Add(t.InjuryUntil);
                h.Add(t.Pending); h.Add(t.PendingDay); h.Add(t.CreditedDay); h.Add(t.RecoveryCredited ? 1 : 0);
            }
            h.Add(Sessions.Count);
            foreach (var t in Sessions) { h.Add(t.Teacher); h.Add(t.Topic); h.Add(t.Org); h.Add(t.Kind); h.Add(t.StartTick); h.Add(t.Progress); h.Add(t.L0); h.Add(t.L1); h.Add(t.L2); h.Add(t.L3); }
            h.Add(TreatmentsStarted); h.Add(TreatmentsCompleted); h.Add(RemediesUsed); h.Add(HerbsGathered); h.Add(SessionsHeld); h.Add(EntriesCopied); h.Add(HealthCredited); h.Add(InjuryHealed);
        }

        public int IndexOfTreatment(int healer) { for (int i = 0; i < Active.Count; i++) if (Active[i].Healer == healer) return i; return -1; }

        public int IndexOfTreated(int patient)
        {
            int lo = 0, hi = Treated.Count - 1;
            while (lo <= hi) { int mid = (lo + hi) >> 1; int v = Treated[mid].Patient; if (v == patient) return mid; if (v < patient) lo = mid + 1; else hi = mid - 1; }
            return ~lo;
        }

        public int IndexOfSession(int teacher) { for (int i = 0; i < Sessions.Count; i++) if (Sessions[i].Teacher == teacher) return i; return -1; }
    }
}
