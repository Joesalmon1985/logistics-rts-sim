namespace God.Sim.Civ
{
    /// <summary>
    /// The civilisation system (order <see cref="Order"/> = 257: after the population 250 and the estates 255, before the
    /// military 300; installed only while <c>GodState.Civ</c> exists). Every tick: C1's classification of the tick's goods
    /// moves (O7 overlay, theft, source claims), then the per-tick slots of the later packages. At each day start: C1's
    /// lost-property expiry and use-right expiry, then the day slots. OWNER: C1; C2, C3 and C5 (C10 through C5's slot)
    /// implement their slots in their own partial files (<c>CivSystem.C2.cs</c>, ...), called in package order.
    /// </summary>
    public sealed partial class CivSystem : ISimSystem
    {
        public const int Order = 257;

        public void Tick(GodState s, EventBuffer ev)
        {
            if (s.Civ == null || s.Population == null) return;
            if (s.Civ.Ownership != null) Ownership.Classify(s, ev);
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            var o = s.Civ.Ownership;
            if (o != null && o.LastDay != day)
            {
                o.LastDay = day;
                Ownership.DayStart(s, ev, day);
                ExpireRights(s, ev, o, day);
                DayStartC2(s, ev, day);
                DayStartC3(s, ev, day);
                DayStartC5(s, ev, day);
            }
            TickC2(s, ev);
            TickC3(s, ev);
            TickC5(s, ev);
        }

        private static void ExpireRights(GodState s, EventBuffer ev, OwnershipState o, int day)
        {
            foreach (var a in o.Assets)
                for (int i = a.Rights.Count - 1; i >= 0; i--)
                    if (a.Rights[i].UntilDay >= 0 && a.Rights[i].UntilDay < day) Assets.EndAt(s, ev, a, i, UseRightEnded.Expired);
        }

        partial void DayStartC2(GodState s, EventBuffer ev, int day);
        partial void DayStartC3(GodState s, EventBuffer ev, int day);
        partial void DayStartC5(GodState s, EventBuffer ev, int day);
        partial void TickC2(GodState s, EventBuffer ev);
        partial void TickC3(GodState s, EventBuffer ev);
        partial void TickC5(GodState s, EventBuffer ev);
    }
}
