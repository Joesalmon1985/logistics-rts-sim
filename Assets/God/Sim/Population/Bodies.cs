using System;

namespace God.Sim.Population
{
    /// <summary>Contracts 4.1: what kind of body an <see cref="Npc"/> record is. 2+ reserved (animals, brief §6 Mode C).</summary>
    public enum BodyKind : byte { Person = 0, Avatar = 1 }

    /// <summary>Contracts 4.1, 8: what hurt a body (attribution; never a new <see cref="DeathCause"/>, contracts 20.4).</summary>
    public enum DamageCause : byte { None = 0, Divine = 1, Violence = 2, Accident = 3 }

    /// <summary>Contracts 4.1, 8: what a heal restores.</summary>
    public enum HealKind : byte { None = 0, Injury = 1, Illness = 2 }

    /// <summary>
    /// F1.8 (contracts 4): body semantics. Every loop over <c>PopulationState.Npcs</c> whose meaning is "people" uses
    /// <see cref="IsLivingPerson"/> (or <see cref="IsPerson"/>); M4 ageing uses <see cref="AgeTicks"/> (biological age, which
    /// a resurrection or rejuvenation shifts by <see cref="Npc.BioAgeOffset"/>). OWNER: F1 (frozen; D5 avatar behaviour and
    /// D7 resurrection change it only by amendment).
    /// </summary>
    public static class Bodies
    {
        public static bool IsPerson(Npc n) => n.Kind == BodyKind.Person;
        public static bool IsLivingPerson(Npc n) => n.Alive && n.Kind == BodyKind.Person;

        /// <summary>Biological age in ticks: calendar age minus the offset (0 for everyone until D7).</summary>
        public static long AgeTicks(Npc n, long tick) => tick - n.BornTick - n.BioAgeOffset;

        /// <summary>
        /// Damage to a body: the divinity hooks first (D3 protection, D5 avatar bodies: an avatar is destroyed, never killed);
        /// otherwise a person takes the injury and loses that much health (micro), and dies at 0 (<c>Violence</c> for divine
        /// and violent causes, <c>Accident</c> otherwise). No caller in F1.
        /// </summary>
        public static void Damage(GodState s, EventBuffer ev, Npc body, int injuryMicro, DamageCause cause, int actor, int intervention)
        {
            if (body == null || !body.Alive || injuryMicro <= 0) return;
            bool handled = false;
            Divinity.DivinityHooks.Damage(s, ev, body, ref injuryMicro, cause, actor, intervention, ref handled);
            if (handled || injuryMicro <= 0) return;
            if (!IsPerson(body)) throw new InvalidOperationException("Bodies.Damage: a non-person body needs its package's hook");
            body.Injury = Math.Min(1000000, body.Injury + injuryMicro);
            body.Health = Math.Max(0, body.Health - injuryMicro);
            if (body.Health <= 0) PopulationModule.Kill(s, ev, body, cause == DamageCause.Accident ? DeathCause.Accident : DeathCause.Violence);
        }

        /// <summary>
        /// A heal: a person's injury is healed here (M3 fields); illness (through M4) and avatar bodies go to the divinity
        /// hooks (D3, D5). No caller in F1.
        /// </summary>
        public static void Heal(GodState s, EventBuffer ev, Npc body, HealKind what, int amount, int intervention)
        {
            if (body == null || !body.Alive || amount <= 0) return;
            if (IsPerson(body) && what == HealKind.Injury) { body.Injury = Math.Max(0, body.Injury - amount); return; }
            bool handled = false;
            Divinity.DivinityHooks.Heal(s, ev, body, what, amount, intervention, ref handled);
        }
    }
}
