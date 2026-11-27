using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Civ
{
    /// <summary>
    /// One believed employment relation (C3, layer 6): what this mind believes of a contract it is a party to, from its own
    /// experience only: the terms as it understood them (a decoded gesture can be misread), the status as it last knew
    /// it, the units it believes worked, paid and unpaid, and its own trust in the other side (risen by pay, fallen by
    /// pay that did not come). Written by the sim at the moments the person perceives them (its own acts, a hand-over it
    /// receives or makes, a gesture it decodes, the asset or the person in sight); read by the mind hooks.
    /// </summary>
    public struct JobBelief
    {
        public const byte AsWorker = 1, AsEmployer = 2;
        public int Contract;
        public byte Side;
        /// <summary>The other party as this mind names it (the employer's agent for a worker; the worker for an employer).</summary>
        public int Other;
        public int Asset;
        public int AssetNode;
        public int AssetClass;
        public Ware PayWare;
        public byte PayCount;
        public PayPer Per;
        public int ShiftTicks;
        public ContractStatus Status;
        /// <summary>Who it believes suspended it (<see cref="EmploymentContract.ByWorker"/> / <see cref="EmploymentContract.ByEmployer"/>).</summary>
        public byte By;
        public int UnitsWorked, UnitsPaid, Unpaid;
        /// <summary>Its own trust in the other side, -1000..1000 (starts from its social memory's trust).</summary>
        public short Trust;
        /// <summary>How many of its offers to this person were refused (an employer raises the pay after each), and the pay it last offered.</summary>
        public byte Refusals;
        public long SinceTick, Tick;
    }

    /// <summary>
    /// What the mind meant by one of its C3 candidates (written by the mind hook at the think, read by the act it starts
    /// in the same tick, as M5's pending answers are): the act, the contract, the asset, and the pay it offers or owes.
    /// </summary>
    public struct C3Plan
    {
        public byte Verb;
        public Policy.NpcVerb Intent;
        public Policy.IntentTarget Kind;
        public int Target;
        public byte Act;
        public int Contract;
        public int Asset;
        public Ware Ware;
        public byte Count;
        public byte Units;
    }

    /// <summary>C3's part of <c>NpcMind.Civ</c>: at most <see cref="MaxJobs"/> believed contracts (ascending contract id).</summary>
    public sealed partial class CivMind
    {
        public const int MaxJobs = 6, MaxPlans = 4;
        public List<JobBelief> Jobs = new List<JobBelief>();
        /// <summary>This think's C3 plans (scratch of one think and the act it starts in the same tick: never saved or hashed).</summary>
        [Newtonsoft.Json.JsonIgnore] public C3Plan[] Plans = new C3Plan[MaxPlans];
        [Newtonsoft.Json.JsonIgnore] public int PlanCount;
        /// <summary>C3's numbers, bound by the sim at each think (the Sense seam); null: C3 has not bound them (no C3 candidate).</summary>
        [Newtonsoft.Json.JsonIgnore] public CivC3Tuning C3;

        public int IndexOfJob(int contract)
        {
            for (int i = 0; i < Jobs.Count; i++) if (Jobs[i].Contract == contract) return i;
            return -1;
        }

        partial void HashC3(ref StateHasher h)
        {
            if (Jobs.Count == 0) return; // no believed contract: the mind hashes exactly as before C3
            h.Add(3); h.Add(Jobs.Count);
            foreach (var j in Jobs)
            {
                h.Add(j.Contract); h.Add(j.Side); h.Add(j.Other); h.Add(j.Asset); h.Add(j.AssetNode); h.Add(j.AssetClass); h.Add((int)j.PayWare); h.Add(j.PayCount);
                h.Add((int)j.Per); h.Add(j.ShiftTicks); h.Add((int)j.Status); h.Add(j.By); h.Add(j.UnitsWorked); h.Add(j.UnitsPaid); h.Add(j.Unpaid); h.Add(j.Trust);
                h.Add(j.Refusals); h.Add(j.SinceTick); h.Add(j.Tick);
            }
        }
    }
}
