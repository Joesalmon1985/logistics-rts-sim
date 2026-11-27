using System.Collections.Generic;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 11.4 (order 230; MYTHIC 4.1): the divinity module's system. Installed only when <c>GodState.Divinity</c>
    /// exists. Per tick, in this fixed order:
    /// <list type="number">
    /// <item>G4: <c>GodCreated</c> and <c>GodAssociated</c> for every god on the first divinity tick (once, state flag).</item>
    /// <item>Day start (every day after day 0): per god ascending, the finished day's <c>MythicUpkeepCharged</c> (per spell
    /// kind) and <c>MythicBalanceDay</c>, then the regeneration <c>MythicCredited</c> (capped at max, excess reported); then
    /// the day slot <c>D7Day</c>.</item>
    /// <item>Upkeep of every open control spell in ascending InterventionId; a shortfall ends the spell this tick
    /// (<c>MythicExhausted</c>).</item>
    /// <item><c>Control.Sweep</c> (F1), <c>Control.ResolveMovement</c> (F1), then the slots <c>D2Tick</c>, <c>D4Tick</c>,
    /// <c>D5Tick</c> (empty in D1; D2/D4/D5 implement them in their own partial files).</item>
    /// </list>
    /// O(gods + open spells) per tick, no allocation per tick (reused buffer). OWNER: D1.
    /// </summary>
    public sealed partial class DivinitySystem : ISimSystem
    {
        public const int SystemOrder = 230;

        private readonly List<ControlEntry> _spells = new List<ControlEntry>();
        private readonly System.Comparison<ControlEntry> ByIntervention = (a, b) => a.Intervention != b.Intervention ? a.Intervention.CompareTo(b.Intervention) : a.Body.CompareTo(b.Body);

        public void Tick(GodState s, EventBuffer ev)
        {
            var d = s.Divinity;
            var reg = d?.Gods;
            if (reg == null) return;
            if (!reg.Announced)
            {
                foreach (var g in reg.Gods)
                    ev.Add(new GodCreated { God = g.Id, Name = g.Name, OriginFaction = g.OriginFaction, Seat = g.Seat, Policy = g.PolicyName, Mythic = g.Mythic, MythicMax = g.MythicMax, Mode = reg.Mode });
                foreach (var g in reg.Gods) ev.Add(new GodAssociated { God = g.Id, Faction = g.OriginFaction });
                reg.Announced = true;
            }
            long day = SimCalendar.Of(s).Day(s.Tick);
            if (day > reg.DayDone)
            {
                DayStart(s, ev, reg, day);
                reg.DayDone = day;
                D7Day(s, ev, day);
            }
            Upkeep(s, ev);
            Control.Sweep(s, ev);
            Control.ResolveMovement(s);
            D2Tick(s, ev);
            D4Tick(s, ev);
            D5Tick(s, ev);
        }

        partial void D2Tick(GodState s, EventBuffer ev);
        partial void D4Tick(GodState s, EventBuffer ev);
        partial void D5Tick(GodState s, EventBuffer ev);
        partial void D7Day(GodState s, EventBuffer ev, long day);

        private static void DayStart(GodState s, EventBuffer ev, GodRegistry reg, long day)
        {
            long regen = s.Tuning.Divinity.Mythic.RegenPerDay;
            for (int i = 0; i < reg.Gods.Count; i++)
            {
                var g = reg.Gods[i];
                var t = reg.Days[i];
                long upkeep = 0;
                for (int k = 0; k < Gods.KindCount; k++)
                {
                    if (t.UpkeepTicks[k] == 0) continue;
                    upkeep += t.Upkeep[k];
                    ev.Add(new MythicUpkeepCharged { God = g.Id, Kind = (InterventionKind)k, Day = day - 1, Ticks = t.UpkeepTicks[k], Amount = t.Upkeep[k], Waived = t.UpkeepWaived[k] });
                }
                ev.Add(new MythicBalanceDay
                {
                    God = g.Id, Day = day - 1, Balance = g.Mythic, Max = g.MythicMax, Credited = t.Credited, Capped = t.Capped, Spent = t.Spent,
                    Waived = t.Waived, Upkeep = upkeep, Actions = t.Actions, Mode = reg.Mode,
                });
                t.Clear();
                // MYTHIC 4.2: fixed regeneration, no worship term; a god without people regenerates like any other (D5.5)
                if (regen > 0 && reg.Mode != DivinityMode.Disabled) Mythic.Credit(s, ev, g, regen, day, 0);
            }
        }

        /// <summary>Charges every open control spell's upkeep (ascending InterventionId) and ends those whose god cannot pay.</summary>
        private void Upkeep(GodState s, EventBuffer ev)
        {
            var table = s.Divinity.Control;
            if (table == null || table.Entries.Count == 0) return;
            _spells.Clear();
            foreach (var e in table.Entries) if (e.Kind != ControlKind.Self) _spells.Add(e);
            _spells.Sort(ByIntervention);
            foreach (var e in _spells)
            {
                long rate = Mythic.UpkeepOf(s, e.Kind);
                if (rate <= 0) continue;
                if (!Mythic.ChargeUpkeep(s, e.God, Mythic.KindOf(e.Kind), rate))
                    Control.Release(s, ev, e.Body, ControlEndReason.MythicExhausted);
            }
            _spells.Clear();
        }
    }
}
