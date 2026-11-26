namespace God.Sim.Comms
{
    /// <summary>
    /// <c>SimTuning.Comms</c> (F0.12): M5's gesture and decoding numbers. Written only when changed. OWNER: M5.
    /// Misunderstanding (COMMUNICATION 4.9): each decoded message is misread with a chance that rises with distance (up
    /// to <see cref="MisreadEdgePermille"/> at the edge of the listener's sight) and darkness, and falls when the speaker
    /// presents the goods it offers. A misread changes one field: the act to a similar act, the quantity by one, or the
    /// ware to a neighbouring ware. The draw is the listener's keyed stream (<c>decode</c>).
    /// </summary>
    public sealed class CommsTuning : IOmitWhenDefault
    {
        /// <summary>0: every message is decoded exactly as sent (M5.4).</summary>
        public int Misunderstanding = 1;
        public int MisreadEdgePermille = 60;
        public int MisreadDark = 40;
        /// <summary>Presenting the offered goods (carrying them) lowers the misread chance by this much.</summary>
        public int MisreadPresented = 30;
        /// <summary>A heard message stays in the inbox (readable) this many ticks; then it is dropped (an addressed one as ignored).</summary>
        public int MessageTtlTicks = 60;
        /// <summary>Inbox slots per mind.</summary>
        public int InboxSlots = 6;
        /// <summary>A listener attends a speaker that addresses it, or one among its nearest this many people in sight.</summary>
        public int AttendPeople = 8;
    }
}
