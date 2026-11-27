using God.Sim.Civ;

namespace God.Runner.Warehouse
{
    /// <summary>
    /// C3's warehouse mappers (OWNER: C3; migration <c>0016_employment.sql</c>, tables in <c>Tables.C3.cs</c>). Employment
    /// spells (EmploymentStarted opens, EmploymentEnded closes), their relationship rows in C1's
    /// <c>fact_economic_relationship</c> (kind 2 employs; keys from 2^40 so they never meet C1's), one job-day row per open
    /// contract per day, and one event row per wage paid, suspension, resumption and unpaid unit. All written at every
    /// level that records rows (employment events are few).
    /// </summary>
    public static class C3Mappings
    {
        /// <summary>The grain of each C3 table (written to <c>meta_table_grain</c>).</summary>
        public static readonly (string table, string grain, string events)[] Grains =
        {
            ("fact_employment", "one row per employment contract spell (to_tick null while active or suspended)", "EmploymentStarted, EmploymentEnded"),
            ("fact_job_day", "one row per contract open on a day", "JobDay"),
            ("fact_employment_event", "one row per wage paid, suspension, resumption or unpaid unit", "WagePaid, ContractSuspended, ContractResumed, WageUnpaid"),
        };

        public const long RelEmploys = 2, RelKeyBase = 1L << 40;

        public static void Register(EventMapRegistry r)
        {
            r.Register<EmploymentStarted>("fact_employment", "employment", (c, e) =>
            {
                var employer = c.Party(e.EmployerKind, e.EmployerId);
                var worker = c.Person(e.Worker).Key;
                c.Insert(Tables.FactEmploymentOpen, (long)e.Contract, employer, (long)(int)e.EmployerKind, (long)e.EmployerId, (object)c.Person(e.Agent).Key, (object)worker,
                    (long)e.Asset, (long)(int)e.PayUnit, (long)e.PayRef, (long)e.PayAmount, (long)(int)e.Per, (long)e.ShiftTicks, (long)e.Control, c.Tick, c.DateKey());
                c.Insert(Tables.FactEconRelOpen, RelKeyBase + e.Contract, employer, c.Party(God.Sim.Population.OwnerKind.Npc, e.Worker), RelEmploys, (long)e.Asset, c.Tick, c.DateKey());
            });
            r.Register<EmploymentEnded>("fact_employment", "employment", (c, e) =>
            {
                c.Insert(Tables.FactEmploymentClose, (long)e.Contract, c.Tick, c.DateKey(), (long)(int)e.Reason, (long)e.By, (long)e.ArrearsUnits, (long)e.UnitsEarned, (long)e.UnitsPaid);
                c.Insert(Tables.FactEconRelClose, RelKeyBase + e.Contract, c.Tick, c.DateKey());
            });
            r.Register<JobDay>("fact_job_day", "job_day", (c, e) =>
                c.Insert(Tables.FactJobDay, (long)e.Contract, c.DateKeyOfDay(e.Day), (object)c.Person(e.Worker).Key, (long)e.WorkTicks, (long)e.OutputCount, (long)e.PaidUnits,
                    (long)e.UnpaidUnits, (long)(int)e.Status));
            void Ev(MapContext c, int contract, long kind, int payer, int worker, God.Sim.Ware ware, int qty, int units, int arrears, int by, int reason, int obligation, int lot, int control)
            {
                c.Insert(Tables.FactEmploymentEvent, c.Tick, c.Seq, c.DateKey(), (long)contract, kind, payer > 0 ? (object)c.Person(payer).Key : null, worker > 0 ? (object)c.Person(worker).Key : null,
                    ware != God.Sim.Ware.None ? (object)(long)(int)ware : null, (long)qty, (long)units, (long)arrears, (long)by, (long)reason, obligation != 0 ? (object)(long)obligation : null,
                    lot != 0 ? (object)(long)lot : null, (long)control);
            }
            r.Register<WagePaid>("fact_employment_event", "employment_event", (c, e) => Ev(c, e.Contract, 251, e.Payer, e.Worker, e.Ware, e.Count, e.ForUnits, 0, 0, 0, 0, e.Lot, e.Control));
            r.Register<ContractSuspended>("fact_employment_event", "employment_event", (c, e) => Ev(c, e.Contract, 254, 0, 0, God.Sim.Ware.None, 0, 0, e.ArrearsUnits, e.By, e.Reason, 0, 0, 0));
            r.Register<ContractResumed>("fact_employment_event", "employment_event", (c, e) => Ev(c, e.Contract, 255, 0, 0, God.Sim.Ware.None, e.PayAmount, 0, 0, e.By, 0, 0, 0, 0));
            r.Register<WageUnpaid>("fact_employment_event", "employment_event", (c, e) => Ev(c, e.Contract, 256, 0, e.Worker, God.Sim.Ware.None, 0, e.Units, e.Arrears, 0, 0, e.Obligation, 0, 0));
        }
    }
}
