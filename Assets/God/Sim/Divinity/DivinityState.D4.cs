namespace God.Sim.Divinity
{
    /// <summary>
    /// D4's part of <see cref="DivinityState"/> (F1.2: created empty by F1; OWNER: D4). The frozen container has no D4
    /// sub-object, so the possession spells live in the embodiment registry <see cref="DivinityState.Avatars"/> (D4 owns D5's
    /// slots, spec 1): the control entry is F1's <see cref="ControlTable"/>, the spell's bookkeeping is a
    /// <see cref="PossessionSpell"/> in <see cref="AvatarRegistry.Possessions"/>.
    /// </summary>
    public sealed partial class DivinityState
    {
    }

    /// <summary>
    /// POSSESSION_AND_AVATAR 3: one possession, keyed by its InterventionId, open while its <c>Possessed</c> control entry exists
    /// (at most <c>Control.MaxPossessionsPerGod</c> per god). Removed when its end is reported (<c>PossessionEnded</c>, CT1).
    /// Keyed by the person, never by a movement mode (spec 7: a later vehicle operator keeps the same spell).
    /// </summary>
    public sealed class PossessionSpell
    {
        public int Intervention, God, Npc;
        public long StartTick;
        /// <summary>The compulsion this possession superseded (0: none).</summary>
        public int SupersededCompulsion;
        public int InputsApplied, InputsRefused, ActionsStarted;
        /// <summary>The tick of the action input not yet resolved into a started or refused act (-1: none).</summary>
        public long PendingAction = -1;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Intervention); h.Add(God); h.Add(Npc); h.Add(StartTick); h.Add(SupersededCompulsion);
            h.Add(InputsApplied); h.Add(InputsRefused); h.Add(ActionsStarted); h.Add(PendingAction);
        }
    }
}
