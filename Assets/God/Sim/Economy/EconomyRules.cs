namespace God.Sim
{
    public static class EconomyRules
    {
        /// <summary>Placement rule of the typed building kinds: the kind must exist, match the size, and a harvester must stand on the land it takes from.</summary>
        public static CommandResult CheckKind(GodState s, string kind, BuildingSize size, int node)
        {
            if (string.IsNullOrEmpty(kind) || kind == BuildingKinds.Placeholder) return CommandResult.Accept();
            var d = EconomyDefs.Get(kind);
            if (d == null || d.Size != size) return CommandResult.RejectDefault(Reasons.InvalidSite);
            if (d.Role == BuildingRole.Harvester)
            {
                int hex = s.Territory.Grid.HexOf(node);
                if (hex < 0 || RawKinds.OfTerrain(s.Board.HexTerrain[hex]) != d.Harvests) return CommandResult.RejectDefault(Reasons.WrongTerrain);
            }
            return CommandResult.Accept();
        }
    }
}

namespace God.Sim
{
    /// <summary>
    /// How the barracks dresses a soldier (stage 3). It needs one bread and one weapon; it adds one shield (never with a
    /// bow), one helmet and one body item when it holds them. The weapon is the one it holds most of, ties in the order
    /// sword, spear, axe, bow, sling; a shield or body item likewise by the order of <see cref="Gear.Defence"/>.
    /// The input priorities steer what it holds, and so the mix.
    /// </summary>
    public static class BarracksRules
    {
        /// <summary>True when the held wares make a soldier: a bread and any weapon.</summary>
        public static bool CanAssemble(int[] held)
        {
            if (held[(int)Ware.Bread] < 1) return false;
            foreach (var w in Gear.Attack) if (held[(int)w] > 0) return true;
            return false;
        }

        /// <summary>The kit the held wares give (Weapon is None when no soldier can be made). Takes nothing.</summary>
        public static Kit Choose(int[] held)
        {
            var k = new Kit();
            if (!CanAssemble(held)) return k;
            k.Weapon = Most(held, GearSlot.Weapon);
            if (Gear.AllowsShield(k.Weapon)) k.Shield = Most(held, GearSlot.Shield);
            k.Head = Most(held, GearSlot.Head);
            k.Body = Most(held, GearSlot.Body);
            return k;
        }

        /// <summary>Chooses the kit and takes its wares and the bread from <paramref name="held"/>. Weapon None (and nothing taken) when it cannot.</summary>
        public static Kit Assemble(int[] held)
        {
            var k = Choose(held);
            if (k.Weapon == Ware.None) return k;
            held[(int)Ware.Bread]--;
            held[(int)k.Weapon]--;
            if (k.Shield != Ware.None) held[(int)k.Shield]--;
            if (k.Head != Ware.None) held[(int)k.Head]--;
            if (k.Body != Ware.None) held[(int)k.Body]--;
            return k;
        }

        /// <summary>The ware of a slot held most, first in list order on a tie; None when none is held.</summary>
        private static Ware Most(int[] held, GearSlot slot)
        {
            Ware best = Ware.None; int n = 0;
            var list = slot == GearSlot.Weapon ? Gear.Attack : Gear.Defence;
            foreach (var w in list)
                if (Gear.SlotOf(w) == slot && held[(int)w] > n) { best = w; n = held[(int)w]; }
            return best;
        }
    }
}
