namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.6, §D9.2 a): the action schema <c>Act v1</c>, frozen: a factorised choice, one head per field, each a
    /// categorical of the given size: the control (13: <see cref="Prim"/>, 0 = none), the target (16 perceived entity slots + self +
    /// facing cell + none = 19), the source (4 carry slots + empty hand = 5), the interaction slot (4), the design (32 known-design
    /// indices + none), the gesture act (32: <c>GestureAct</c>), two referents (16 entity slots + 4 carry slots + self + none = 22 each)
    /// and the quantity bucket (16). Masks are physical legality only (UN2's <c>Motor.Legal</c>).
    /// </summary>
    public static class ActSchema
    {
        public const string Name = "Act";
        public const int Version = 1;
        public const int Scale = 1000;

        public static readonly ReadOnlyArray<SchemaField> Heads = new ReadOnlyArray<SchemaField>(
            new SchemaField("control", 13),
            new SchemaField("target", ObsSchema.Entities + 3),
            new SchemaField("source", ObsSchema.CarrySlots + 1),
            new SchemaField("slot", 4),
            new SchemaField("design", 33),
            new SchemaField("gesture", 32),
            new SchemaField("referent1", ObsSchema.Entities + ObsSchema.CarrySlots + 2),
            new SchemaField("referent2", ObsSchema.Entities + ObsSchema.CarrySlots + 2),
            new SchemaField("quantity", 16));

        /// <summary>The total logits of all heads.</summary>
        public static readonly int Length = SchemaText.Total(Heads);

        public static readonly string Canonical = SchemaText.Canonical(Name, Version, Scale, Heads);

        public static readonly string SchemaHash = SchemaText.Sha256(Canonical);

        public static int OffsetOf(string head) => SchemaText.OffsetOf(Heads, head);
    }
}
