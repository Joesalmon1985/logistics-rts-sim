namespace God.Sim.Unified
{
    /// <summary>
    /// UN2's motor fields beyond UN0's shape (contracts 23.5, UN2 spec 11), hashed in <see cref="AddToUN2"/>: the source lot, design,
    /// catalogue id and row of the action in progress, its total ticks, and the last resisted TAKE the body felt.
    /// <para><see cref="ActionOrigin"/> is saved (events after a load carry the right origin) but deliberately <b>not hashed</b>: the
    /// origin is analytics only and must never enter a physical hash (QA-01, U-P4).</para>
    /// </summary>
    public sealed partial class UnifiedNpc
    {
        /// <summary>The lot the action in progress uses (USE source, 0 = empty hand).</summary>
        public int ActionSource;
        /// <summary>The design of a USE on a placement (0 none).</summary>
        public ushort ActionDesign;
        /// <summary>The catalogue id the USE matched on and the row's index in the resolver's table (-1 none).</summary>
        public ushort ActionCat;
        public int ActionRow = -1;
        /// <summary>The ticks the action in progress takes in all (progress = total - left).</summary>
        public int ActionTotal;
        /// <summary>The person whose TAKE from this body's carry was resisted last, and when (perceived by this body).</summary>
        public int ResistedBy;
        public long ResistedTick;
        /// <summary>Who issued the action in progress (saved, not hashed: never a physical input).</summary>
        public ControlOrigin ActionOrigin;

        partial void AddToUN2(ref StateHasher h)
        {
            h.Add(ActionSource); h.Add(ActionDesign | (ActionCat << 16)); h.Add(ActionRow); h.Add(ActionTotal);
            h.Add(ResistedBy); h.Add(ResistedTick);
        }

        /// <summary>Clears the action in progress (none, no ticks).</summary>
        public void ClearAction()
        {
            ActionKind = Prim.None; ActionTicksLeft = 0; ActionTotal = 0; ActionTarget = TargetRef.None; ActionSlot = 0;
            ActionSource = 0; ActionDesign = 0; ActionCat = 0; ActionRow = -1; ActionOrigin = ControlOrigin.Policy;
        }
    }

    /// <summary>
    /// UN2's run-time (never saved, never hashed) companions of the unified state: the spatial index cache and the host's
    /// interaction tables. Private fields, so the save format and the hash never see them.
    /// </summary>
    public sealed partial class UnifiedState
    {
        private SimpleSpatialIndex _index;
        private InteractionBook _book;
        private EffectTable _effects;
        private SpatialHit[] _scratch;

        /// <summary>A reusable query buffer of the motor (never state).</summary>
        public SpatialHit[] Scratch => _scratch ?? (_scratch = new SpatialHit[64]);

        /// <summary>The spatial index of this world (a cache rebuilt from the state, contracts 23.6).</summary>
        public SimpleSpatialIndex Index => _index ?? (_index = new SimpleSpatialIndex());

        /// <summary>The interaction rows the resolver matches (default: the generated <see cref="Catalogue"/>).</summary>
        public InteractionBook Book => _book ?? InteractionBook.Default;

        /// <summary>The effect handlers the resolver calls (default: <see cref="EffectRegistry.Table"/>).</summary>
        public EffectTable Effects => _effects ?? EffectRegistry.Table;

        /// <summary>
        /// A host or test setting (not state: never saved or hashed): code-defined interaction rows and effect handlers instead of the
        /// generated catalogue and the registry (null keeps the default). Tests only; a game uses the defaults.
        /// </summary>
        public void UseTables(InteractionBook book, EffectTable effects) { _book = book; _effects = effects; }
    }
}
