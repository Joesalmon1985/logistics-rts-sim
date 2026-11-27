using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Civ
{
    /// <summary>
    /// C3's part of <see cref="CivState"/> (F1.2: created empty by F1; OWNER: C3). Its sub-object type is declared below; the container hashes it in the fixed order.
    /// </summary>
    public sealed partial class CivState
    {
    }

    /// <summary>The state of an employment contract (spec 5.1). Offered: proposed, not yet accepted; Suspended: owner Q19 walk-out (slot kept).</summary>
    public enum ContractStatus : byte { None = 0, Offered = 1, Active = 2, Suspended = 3, Ended = 4 }

    /// <summary>
    /// Why a contract ended (event 252 codes 1-5; 6 and 7 close an offer that never became a contract, no spell, no event).
    /// Append only.
    /// </summary>
    public enum EndReason : byte { None = 0, Resigned = 1, Dismissed = 2, Died = 3, AssetGone = 4, Abandoned = 5, Refused = 6, Lapsed = 7 }

    /// <summary>What one unit of pay is earned by: a shift (<see cref="PayTerms.ShiftTicks"/> of work at the asset), one output unit (a recipe cycle), or a task. Append only.</summary>
    public enum PayPer : byte { None = 0, Shift = 1, Unit = 2, Task = 3 }

    /// <summary>The kind of a claim unit: goods of one ware today; money (K2) later. Append only.</summary>
    public enum PayUnitKind : byte { None = 0, Ware = 1, Money = 2 }

    /// <summary>
    /// What is owed per pay unit (spec 22: "a claim unit, ware today, money later"). The same shape as C2's planned
    /// <c>ClaimUnit { Kind; UnitRef; Amount }</c> (C2 spec 22): it is replaced by C2's type when C2 merges (a rename, no
    /// layout change). Today only <see cref="PayUnitKind.Ware"/>: <see cref="UnitRef"/> is the ware id.
    /// </summary>
    public struct PayUnit
    {
        public PayUnitKind Kind;
        public int UnitRef;
        public int Amount;

        public static PayUnit Of(Ware w, int count) => new PayUnit { Kind = PayUnitKind.Ware, UnitRef = (int)w, Amount = count };
        public Ware Ware => Kind == PayUnitKind.Ware ? (Ware)UnitRef : Ware.None;
    }

    /// <summary>The agreed terms of a contract (spec 5.1).</summary>
    public struct PayTerms
    {
        public PayUnit Pay;
        public PayPer Per;
        /// <summary>Work ticks at the asset that make one shift (<see cref="PayPer.Shift"/>).</summary>
        public int ShiftTicks;
    }

    /// <summary>
    /// One employment contract (spec 5.1, ECONOMY 3.6 and 4 "Jobs"). The id is a ContractId (contracts 1), allocated only by
    /// <see cref="Employment.Hire"/> from <see cref="CivState.NextContractId"/>. The employer is any <see cref="Owner"/> party
    /// (an NPC in C3a; an organisation in C3b); <see cref="Asset"/> is C1's general AssetId; <see cref="Slot"/> the staff
    /// slot on it; <see cref="JobKind"/> and <see cref="Role"/> are append-only value sets (I2 adds Operate / Operator).
    /// </summary>
    public sealed class EmploymentContract
    {
        /// <summary>Job kinds (append only): 1 work the asset (C1's Work act). I2 appends Operate.</summary>
        public const byte JobWork = 1;
        /// <summary>Who suspended or ended it.</summary>
        public const byte ByWorker = 1, ByEmployer = 2, BySim = 3;

        public int Id;
        public Owner Employer;
        /// <summary>The NpcId of the person who acts for the employer (C3a: the employer itself; C3b: the office-holder).</summary>
        public int Agent;
        public int Worker;
        public int Asset;
        public byte Slot;
        public byte JobKind = JobWork;
        /// <summary>The learning <c>Role</c> of the employer side (contracts 14: 10 Employer).</summary>
        public byte Role = Learning.RoleValue.Employer;
        public PayTerms Terms;
        public ContractStatus Status;
        public long SinceTick;
        /// <summary>When it was offered, started (accepted), and last changed status.</summary>
        public long OfferedTick, StartedTick, StatusTick;
        /// <summary>Pay units past their grace without pay (owed), and units due now (within the grace), with the tick each fell due.</summary>
        public int ArrearsUnits;
        public List<long> DueTicks = new List<long>();
        public EndReason Reason;
        /// <summary>Who suspended it (when Suspended) or ended it.</summary>
        public byte By;
        /// <summary>Work ticks towards the current shift.</summary>
        public int ShiftProgress;
        /// <summary>Counters over the whole contract.</summary>
        public int UnitsEarned, UnitsPaid, UnitsUnpaid, WorkTicks, OutputCount;
        /// <summary>The tick of the last work or pay progress (abandonment, <c>Civ.C3.AbandonTicks</c>).</summary>
        public long ProgressTick;
        /// <summary>The current day's counters (JobDay 253), reset at each day start.</summary>
        public int DayWorkTicks, DayOutput, DayPaid, DayUnpaid;
        /// <summary>It was open (Active or Suspended) at some time of the current day.</summary>
        public bool DayOpen;
        /// <summary>The message id of the offer (or of the latest re-offer) and its tick.</summary>
        public int OfferMessage;
        public long OfferMessageTick;

        public bool IsOpen => Status == ContractStatus.Active || Status == ContractStatus.Suspended;
        public int DueUnits => DueTicks.Count;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add((int)Employer.Kind); h.Add(Employer.Id); h.Add(Agent); h.Add(Worker); h.Add(Asset); h.Add(Slot); h.Add(JobKind); h.Add(Role);
            h.Add((int)Terms.Pay.Kind); h.Add(Terms.Pay.UnitRef); h.Add(Terms.Pay.Amount); h.Add((int)Terms.Per); h.Add(Terms.ShiftTicks);
            h.Add((int)Status); h.Add(SinceTick); h.Add(OfferedTick); h.Add(StartedTick); h.Add(StatusTick); h.Add(ArrearsUnits);
            h.Add(DueTicks.Count); foreach (var t in DueTicks) h.Add(t);
            h.Add((int)Reason); h.Add(By); h.Add(ShiftProgress);
            h.Add(UnitsEarned); h.Add(UnitsPaid); h.Add(UnitsUnpaid); h.Add(WorkTicks); h.Add(OutputCount); h.Add(ProgressTick);
            h.Add(DayWorkTicks); h.Add(DayOutput); h.Add(DayPaid); h.Add(DayUnpaid); h.Add(DayOpen ? 1 : 0); h.Add(OfferMessage); h.Add(OfferMessageTick);
        }
    }

    /// <summary>
    /// A C3 act in progress (one per person at most): the act code, the contract and its phase. The population's action
    /// carries the learning verb and target; this record carries what the verb alone does not say (which negotiation act).
    /// </summary>
    public struct EmploymentAct
    {
        public const byte Offer = 1, Dismiss = 2, Accept = 3, Refuse = 4, Pay = 5, Work = 6;
        public int Npc;
        public byte Act;
        public int Contract;
        /// <summary>Pay: 0 fetch from the store if needed, 1 walk to the worker. Others: unused.</summary>
        public byte Phase;
        public long StartTick;
    }

    /// <summary>C3's sub-object (OWNER: C3): contracts (ascending ContractId), the acts in progress (ascending NpcId), counters.</summary>
    public sealed partial class EmploymentState
    {
        public List<EmploymentContract> Contracts = new List<EmploymentContract>();
        public List<EmploymentAct> Acts = new List<EmploymentAct>();
        /// <summary>Counters (analytics and tests): offers made, refused, lapsed; contracts started; wage hand-overs; unpaid units; suspensions; resumptions.</summary>
        public long Offers, Refusals, Lapsed, Started, Wages, Unpaid, Suspensions, Resumptions;
        /// <summary>The last day the day-start pass ran.</summary>
        public int LastDay = -1;

        partial void Hash(ref StateHasher h)
        {
            h.Add(Contracts.Count);
            foreach (var c in Contracts) c.AddTo(ref h);
            h.Add(Acts.Count);
            foreach (var a in Acts) { h.Add(a.Npc); h.Add(a.Act); h.Add(a.Contract); h.Add(a.Phase); h.Add(a.StartTick); }
            h.Add(Offers); h.Add(Refusals); h.Add(Lapsed); h.Add(Started); h.Add(Wages); h.Add(Unpaid); h.Add(Suspensions); h.Add(Resumptions); h.Add(LastDay);
        }
    }
}
