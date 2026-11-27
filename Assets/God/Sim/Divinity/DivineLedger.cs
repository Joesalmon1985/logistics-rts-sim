using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// Contracts 7.2 (C7): the miracle ledger. <see cref="Book"/> is the only allocator of <c>LedgerId</c>
    /// (<see cref="DivinityState.NextLedgerId"/>) and the only writer of <see cref="MiracleLedger"/>: every miracle
    /// <c>Goods.Produce</c> or <c>Goods.Consume</c> is paired with exactly one call, which emits one
    /// <see cref="ResourceCreated"/> or <see cref="ResourceDestroyed"/> and books the god's per-ware total. The goods
    /// ledger's miracle slots (<c>ProducedBy(Miracle, w)</c>, <c>ConsumedBy(Miracle, w)</c>) must equal the sums here (C7).
    /// OWNER: D3.
    /// </summary>
    public static class DivineLedger
    {
        /// <summary>The state, created at the first miracle.</summary>
        public static MiracleState State(GodState s)
        {
            var d = s.Divinity ?? throw new System.InvalidOperationException("DivineLedger without divinity state");
            if (d.Miracles == null) d.Miracles = new MiracleState();
            if (d.Miracles.Ledger == null) d.Miracles.Ledger = new MiracleLedger(s.FactionCount);
            return d.Miracles;
        }

        /// <summary>Books goods a miracle created (<paramref name="lot"/> the new lot id, <paramref name="intoLot"/> what Produce returned) into <paramref name="holder"/>. Returns the LedgerId.</summary>
        public static int BookCreated(GodState s, EventBuffer ev, int god, int intervention, Ware ware, int count, int lot, int intoLot, Holder holder)
        {
            var m = State(s);
            int id = DivinityIds.Take(ref s.Divinity.NextLedgerId);
            m.Ledger.Created[god * Wares.PersonalCount + (int)ware] += count;
            ev.Add(new ResourceCreated
            {
                LedgerId = id, Intervention = intervention, God = god, Ware = ware, Count = count, Lot = lot, IntoLot = intoLot,
                HolderKind = holder.Kind, HolderId = holder.Id, Node = Goods.NodeOf(s, holder),
            });
            return id;
        }

        /// <summary>Books goods a miracle destroyed out of <paramref name="holder"/>. Returns the LedgerId.</summary>
        public static int BookDestroyed(GodState s, EventBuffer ev, int god, int intervention, Ware ware, int count, int lot, Holder holder, int node)
        {
            var m = State(s);
            int id = DivinityIds.Take(ref s.Divinity.NextLedgerId);
            m.Ledger.Destroyed[god * Wares.PersonalCount + (int)ware] += count;
            ev.Add(new ResourceDestroyed
            {
                LedgerId = id, Intervention = intervention, God = god, Ware = ware, Count = count, Lot = lot,
                HolderKind = holder.Kind, HolderId = holder.Id, Node = node,
            });
            return id;
        }

        /// <summary>C7: the ledger equals the goods ledger's miracle slots, per ware (called by <see cref="DivinityModule.Check"/>).</summary>
        public static void Check(GodState s, System.Collections.Generic.List<string> bad)
        {
            var p = s.Population;
            if (p == null || s.Divinity == null) return;
            var m = s.Divinity.Miracles;
            for (int w = 1; w < Wares.PersonalCount; w++)
            {
                long created = m?.Ledger?.CreatedOf((Ware)w) ?? 0, destroyed = m?.Ledger?.DestroyedOf((Ware)w) ?? 0;
                long produced = p.Ledger.ProducedBy(ProduceReason.Miracle, (Ware)w), consumed = p.Ledger.ConsumedBy(ConsumeReason.Miracle, (Ware)w);
                if (created != produced) bad.Add("C7: ware " + w + " miracle-produced " + produced + " != ResourceCreated " + created);
                if (destroyed != consumed) bad.Add("C7: ware " + w + " miracle-consumed " + consumed + " != ResourceDestroyed " + destroyed);
            }
        }
    }
}
