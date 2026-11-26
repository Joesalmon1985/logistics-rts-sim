namespace God.Sim.Comms
{
    /// <summary>
    /// The act of a gesture message (COMMUNICATION 4.8 vocabulary). Layout frozen by F0; meaning and use owned by M5. The
    /// learning verb of saying an act is <c>Say(act) = 64 + act</c>, so acts stay below 32.
    /// </summary>
    public enum GestureAct : byte
    {
        None = 0, Want = 1, Offer = 2, Accept = 3, Refuse = 4, NeedFood = 5, NeedHelp = 6, Cooperate = 7, Promise = 8, Follow = 9,
        Warn = 10, Surrender = 11, Defend = 12, Accuse = 13, Apologise = 14, Forgive = 15, AskJoin = 16, Praise = 17,
    }

    /// <summary>What a topic names: a ware, a place (node), a person, an issue (M10), a posting (M6), or a task (reserved, 13.10).</summary>
    public enum TopicKind : byte { None = 0, Ware = 1, Place = 2, Npc = 3, Issue = 4, Posting = 5, Task = 6 }

    /// <summary>When a message's content applies.</summary>
    public enum MsgTime : byte { Now = 0, Soon = 1, Later = 2 }

    /// <summary>What a message is about: ware id, node, NpcId (as recognised), issue kind or posting id; Sub for issues only.</summary>
    public struct MsgTopic
    {
        public TopicKind Kind;
        public int Id;
        public int Sub;
    }

    /// <summary>
    /// One message as a listener decoded it (its belief, possibly wrong): layout frozen by F0, filled only by M5 into
    /// <c>ThinkScratch.Heard</c>. Speaker is the speaker as the listener recognised it. Never carries the speaker's intention.
    /// </summary>
    public struct DecodedMessage
    {
        public int MessageId;
        public int Speaker;
        public GestureAct Act;
        public MsgTopic Topic;
        public byte Quantity;
        public MsgTopic Counter;
        /// <summary>The addressee as recognised (0: broadcast).</summary>
        public int Target;
        public MsgTime Time;
        public int DealId;
        public int Tick;
    }
}
