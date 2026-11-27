using God.Sim.Policy;
using Newtonsoft.Json;

namespace God.Sim.Divinity
{
    /// <summary>
    /// What one decoded instruction became at the think that read it (D2): whether the mind offered a candidate for it and
    /// which candidate shape counts as following it (<see cref="Wild"/>: any candidate of the verb, when the instruction
    /// names no object).
    /// </summary>
    public struct DivineReading
    {
        public int Intervention;
        public InterventionKind Kind;
        public bool Offered;
        public bool Wild;
        public byte Verb, Verb2;
        public IntentTarget TargetKind;
        public int TargetId, TargetNode;
        public int God;

        /// <summary>True when a candidate (or a chosen decision) of this verb and target counts as following it.</summary>
        public bool Matches(byte verb, IntentTarget kind, int id, int node)
        {
            if (!Offered || verb == 0 || verb != Verb && verb != Verb2) return false;
            if (Wild) return true;
            if (kind != TargetKind) return false;
            return kind == IntentTarget.Node ? node == TargetNode : id == TargetId;
        }
    }

    /// <summary>
    /// D2's part of <see cref="DivineMind"/> (OWNER: D2). The inbox fields are F1's; this file adds the tuning reference and
    /// the record of the last think that read the inbox. <see cref="ReadTick"/>/<see cref="Read"/> are written by the mind
    /// hooks at a think and consumed by <c>DivinitySystem</c>'s D2 slot at the next tick (which turns them into a
    /// <c>CommandResponded</c> and hashed registry state); they are saved (a save between the two ticks continues exactly)
    /// but not in the frozen mind hash. <see cref="CompelTick"/>: the last think at which the compelled candidate existed.
    /// </summary>
    public sealed partial class DivineMind
    {
        public const int ReadCap = 2;

        /// <summary>The game's instruction tuning (bound at creation and after a load; not state).</summary>
        [JsonIgnore] public InstructionTuning Tuning;

        public long ReadTick = -1;
        public int ReadCount;
        public DivineReading[] Read;
        public long CompelTick = -1;

        public DivineReading[] ReadOrCreate() => Read ?? (Read = new DivineReading[ReadCap]);

        /// <summary>Adds an entry to the inbox (cap 4, the oldest evicted). Returns the evicted entry's intervention (0: none).</summary>
        public int Deliver(in DecodedDivine d)
        {
            if (Inbox == null) Inbox = new DecodedDivine[InboxCap];
            int evicted = 0;
            if (InboxCount >= InboxCap)
            {
                int at = 0; // the oldest entry that is not a compulsion (a compulsion leaves only when its spell ends)
                while (at < InboxCount - 1 && Inbox[at].Kind == InterventionKind.Compulsion) at++;
                evicted = Inbox[at].Intervention;
                for (int i = at + 1; i < InboxCount; i++) Inbox[i - 1] = Inbox[i];
                InboxCount--;
            }
            Inbox[InboxCount++] = d;
            return evicted;
        }

        /// <summary>Removes the inbox entry of an intervention (true when one was there).</summary>
        public bool Remove(int intervention)
        {
            for (int i = 0; i < InboxCount; i++)
            {
                if (Inbox[i].Intervention != intervention) continue;
                for (int j = i + 1; j < InboxCount; j++) Inbox[j - 1] = Inbox[j];
                InboxCount--;
                Inbox[InboxCount] = default;
                return true;
            }
            return false;
        }

        public int IndexOf(int intervention)
        {
            for (int i = 0; i < InboxCount; i++) if (Inbox[i].Intervention == intervention) return i;
            return -1;
        }
    }
}
