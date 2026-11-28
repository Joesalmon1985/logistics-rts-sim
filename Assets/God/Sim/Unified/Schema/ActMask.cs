namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.6): the physical legality mask over the <see cref="ActSchema"/> heads, one bit per option (UN2 fills it in
    /// <c>Motor.Legal</c>: exists, perceived, in reach, room, passable, not busy, a known design). Never a social, moral or advisable mask.
    /// </summary>
    public partial struct ActMask
    {
        public ulong Control;
        public ulong Target;
        public ulong Source;
        public ulong Slot;
        public ulong Design;
        public ulong Gesture;
        public ulong Referent1;
        public ulong Referent2;
        public ulong Quantity;

        public static bool Allows(ulong head, int option) => option >= 0 && option < 64 && (head & (1UL << option)) != 0;
        public static ulong With(ulong head, int option) => option >= 0 && option < 64 ? head | (1UL << option) : head;
    }
}
