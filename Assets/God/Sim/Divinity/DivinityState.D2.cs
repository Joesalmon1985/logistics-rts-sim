using System.Collections.Generic;
using Newtonsoft.Json;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D2's part of <see cref="DivinityState"/> (F1.2: created empty by F1; OWNER: D2). Its sub-object types are declared below; the container hashes them in the fixed order.
    /// </summary>
    public sealed partial class DivinityState
    {
    }

    /// <summary>Where one recipient of an instruction stands (D2 bookkeeping, contracts 10.3).</summary>
    public enum RecipientState : byte
    {
        /// <summary>Asleep at delivery: perceives (draw) on waking (<c>Instruction.WakeDelivery</c>).</summary>
        AwaitWake = 0,
        /// <summary>Perceived and decoded into its inbox; its response is pending.</summary>
        Perceived = 1,
        /// <summary>The perception draw failed (no event, no inbox entry).</summary>
        Unperceived = 2,
        /// <summary>Its one <c>CommandResponded</c> was emitted.</summary>
        Responded = 3,
        /// <summary>In the scope but not reachable (dead, not a person, possessed): reported, never delivered.</summary>
        NotReached = 4,
    }

    /// <summary>Contracts 10.3 step 5: the one response of a perceiver (<c>CommandResponded</c>).</summary>
    public enum CommandResponse : byte { None = 0, Followed = 1, Ignored = 2, Refused = 3, Lapsed = 4, Misread = 5 }

    /// <summary>One recipient of an instruction (ascending NpcId in <see cref="Instruction.Recipients"/>).</summary>
    public struct InstructionRecipient
    {
        public int Npc;
        public RecipientState State;
        public CommandResponse Response;
        /// <summary>Analytics truth only (never read by a rule): the decoded act or topic differs from the god's.</summary>
        public bool Mismatch;
        /// <summary>The act as this recipient decoded it.</summary>
        public DivineAct DecodedAct;
        public long PerceivedTick;
        /// <summary>For <see cref="RecipientState.NotReached"/>: why (a <see cref="DivineReasons"/> code index, see <see cref="Instructions.NotReachedReason"/>).</summary>
        public byte Why;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Npc); h.Add((int)State | ((int)Response << 8) | (Mismatch ? 1 << 16 : 0) | ((int)DecodedAct << 17) | (Why << 25)); h.Add(PerceivedTick);
        }
    }

    /// <summary>
    /// Contracts 10.2: one divine instruction (a suggestion, a command or a compulsion), keyed by its InterventionId. Open
    /// until it expires (suggestion, command) or its control spell ends (compulsion); then removed.
    /// </summary>
    public sealed class Instruction
    {
        public int Id;
        public int God;
        public InterventionKind Kind;
        public DivineAct Act;
        public DivineTarget Target;
        public DivineScope Scope;
        public long Tick, Expires;
        /// <summary>Ascending NpcId.</summary>
        public List<InstructionRecipient> Recipients = new List<InstructionRecipient>();

        public void AddTo(ref StateHasher h)
        {
            h.Add(Id); h.Add(God); h.Add((int)Kind | ((int)Act << 8) | ((int)Scope << 16)); Target.AddTo(ref h); h.Add(Tick); h.Add(Expires);
            h.Add(Recipients.Count);
            foreach (var r in Recipients) r.AddTo(ref h);
        }

        public int IndexOf(int npc)
        {
            for (int i = 0; i < Recipients.Count; i++) if (Recipients[i].Npc == npc) return i;
            return -1;
        }
    }

    /// <summary>
    /// D2's sub-object <c>DivinityState.Messages</c> (OWNER: D2): the open instructions in ascending id. Null until the first
    /// instruction is applied (so a game with idle gods hashes exactly as D1's: pin P2).
    /// </summary>
    public sealed partial class InstructionRegistry
    {
        public List<Instruction> Items = new List<Instruction>();
        /// <summary>Counters (analytics and the card): instructions issued, perceptions, responses by kind (index = <see cref="CommandResponse"/>).</summary>
        public long Issued, Perceived;
        public long[] Responses = new long[6];

        /// <summary>A path cache for D2's own walks (Approach, Give); not state.</summary>
        [JsonIgnore] internal Population.NodePather Pather;

        public Instruction Find(int id)
        {
            foreach (var i in Items) if (i.Id == id) return i;
            return null;
        }

        partial void Hash(ref StateHasher h)
        {
            h.Add(Items.Count);
            foreach (var i in Items) i.AddTo(ref h);
            h.Add(Issued); h.Add(Perceived);
            for (int i = 0; i < Responses.Length; i++) h.Add(Responses[i]);
        }
    }
}
