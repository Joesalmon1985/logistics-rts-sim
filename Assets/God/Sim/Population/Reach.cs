namespace God.Sim.Population
{
    /// <summary>
    /// Physical interaction ranges in nodes (WORLD_ONTOLOGY 2, SHARED_CONTRACTS A1; frozen by F0). A range r means
    /// <c>NodeGrid.Distance &lt;= r</c> from the actor's own node. Nothing is acted on at a distance because it is seen or
    /// remembered: every intent walks until within its reach first, and <see cref="Goods"/> refuses a person-initiated move
    /// or use outside it (<see cref="MoveStatus.OutOfReach"/>). Sight, hearing and gesture ranges are perception, not reach.
    /// </summary>
    public static class Reach
    {
        /// <summary>On the node itself: sleeping in a hut, the end of a walk, setting goods down.</summary>
        public const int Enter = 0;
        /// <summary>Take a lot, eat or forage at a source, build, use up goods, inspect.</summary>
        public const int Touch = 1;
        /// <summary>Gather logs from a wood stand or a hex pile.</summary>
        public const int Gather = 2;
        /// <summary>Hand goods to another person (both carries).</summary>
        public const int HandOver = 1;
        /// <summary>Deposit into or withdraw from a bin, from the bin's node.</summary>
        public const int Store = 1;
        /// <summary>No person acts (the sim moves the goods: estate in and out).</summary>
        public const int NoActor = -1;

        /// <summary>The reach a goods move needs, by its reason.</summary>
        public static int Of(MoveReason why)
        {
            switch (why)
            {
                case MoveReason.PickUp: case MoveReason.Claim: return Touch;
                case MoveReason.Drop: case MoveReason.DeathSpill: return Enter;
                case MoveReason.HandOver: return HandOver;
                case MoveReason.Deposit: case MoveReason.Withdraw: return Store;
                default: return NoActor;
            }
        }
    }
}
