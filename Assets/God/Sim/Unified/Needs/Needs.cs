using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN1 (contracts 23.6, spec 5.7, G15): the seven extension need channels (Thirst, Hygiene, Bladder, Social, Fun, Comfort, Room;
    /// pressures 0-1000, higher = more urgent). They rise at configured daily rates (<see cref="UN1Tuning.NeedRatesPerDay"/>) with a
    /// bounded per-person variation (hash and temperament halves of <c>NeedVariationPermille</c>, plus a child or elder factor on Thirst and
    /// Bladder) and fall only through <see cref="Apply"/> (UN3b relief objects, UN1's drink and wash). An inactive channel
    /// (<c>Needs.ActiveMask</c>) is held at 0 and affects nothing. Health coupling (D9.4 d): only Thirst of these seven reaches health.
    /// </summary>
    public static class Needs
    {
        const int Channels = NeedChannels.Extensions;

        /// <summary>
        /// Adds <paramref name="delta"/> pressure points (negative relieves) to one channel of a person and clamps to 0-1000. Only the named
        /// channel changes. The five drives are accepted too (Hunger, Energy, Warmth, Safety; Pain is derived and refused). False when the
        /// person or channel is unknown or the channel is inactive.
        /// </summary>
        public static bool Apply(GodState s, Npc npc, NeedChannel channel, int delta)
        {
            if (npc == null || !Unified.NeedActive(s, channel)) return false;
            int c = (int)channel;
            if (c >= NeedChannels.FirstExtension)
            {
                var p = s.Unification.PersonOf(npc.Id);
                if (p == null) return false;
                int i = c - NeedChannels.FirstExtension;
                int v = p.Needs[i] + delta;
                p.Needs[i] = (short)(v < 0 ? 0 : v > 1000 ? 1000 : v);
                return true;
            }
            int d;
            switch (channel)
            {
                case NeedChannel.Hunger: d = Drive.Hunger; break;
                case NeedChannel.Energy: d = Drive.Fatigue; break;
                case NeedChannel.Warmth: d = Drive.Cold; break;
                case NeedChannel.Safety: d = Drive.Fear; break;
                default: return false;
            }
            long m = (long)npc.Drives[d] + (long)delta * 1000;
            npc.Drives[d] = (int)(m < 0 ? 0 : m > 1000000 ? 1000000 : m);
            return true;
        }

        public static bool Apply(GodState s, int npc, NeedChannel channel, int delta) => Apply(s, s.Population.Find(npc), channel, delta);

        /// <summary>The pressure of an extension channel (0 for an unknown person).</summary>
        public static int Pressure(GodState s, int npc, NeedChannel channel)
        {
            var p = s.Unification?.PersonOf(npc);
            return p == null || (int)channel < NeedChannels.FirstExtension ? 0 : p.NeedOf(channel);
        }

        /// <summary>The rate multiplier (permille; 1000 = the configured rate) of a person on one extension channel: bounded and deterministic.</summary>
        public static int Multiplier(GodState s, Npc npc, int i)
        {
            var t = s.Tuning.Unification.UN1;
            int half = t.NeedVariationPermille / 2;
            // a stable hash part in [-half, +half]
            uint h = unchecked((uint)(npc.Id * 31 + i * 7 + 1) * 2654435761u);
            int m = 1000 + (int)((h >> 8) % (uint)(2 * half + 1)) - half;
            // a temperament part in [-half, +half] on the social channels (traits are 0-1000, centre 500)
            int trait = -1;
            switch (i + NeedChannels.FirstExtension)
            {
                case (int)NeedChannel.Social: trait = npc.Traits[Trait.Boldness]; break;
                case (int)NeedChannel.Fun: trait = 1000 - npc.Traits[Trait.Industry]; break;
                case (int)NeedChannel.Comfort: trait = 1000 - npc.Traits[Trait.Patience]; break;
            }
            if (trait >= 0 && half > 0) { int tv = (trait - 500) * half / 500; m += tv < -half ? -half : tv > half ? half : tv; }
            // age: children and elders need to drink and relieve themselves more often
            if ((i + NeedChannels.FirstExtension == (int)NeedChannel.Thirst || i + NeedChannels.FirstExtension == (int)NeedChannel.Bladder))
            {
                if (npc.Stage == LifeStage.Child) m += t.ChildExtraPermille;
                else if (npc.Stage == LifeStage.Elder) m += t.ElderExtraPermille;
            }
            return m;
        }

        /// <summary>One tick of the seven channels for a person (called from <c>PersonTick</c>, after the body).</summary>
        internal static void Tick(GodState s, EventBuffer ev, Npc npc)
        {
            var u = s.Unification; var t = s.Tuning.Unification.UN1;
            var p = u.PersonOf(npc.Id);
            if (p == null) return;
            long tpd = SimCalendar.Of(s).TicksPerDay;
            long den = tpd * 1000;
            if (p.NeedMul[0] == 0 || s.Tick % tpd == 0) for (int i = 0; i < Channels; i++) p.NeedMul[i] = (short)Multiplier(s, npc, i);
            for (int i = 0; i < Channels; i++)
            {
                var ch = (NeedChannel)(i + NeedChannels.FirstExtension);
                if (!Unified.NeedActive(s, ch)) { p.Needs[i] = 0; p.NeedFrac[i] = 0; continue; }
                long rate = (long)t.NeedRatesPerDay[i] * p.NeedMul[i];
                if (npc.Action.Asleep && ch != NeedChannel.Comfort && ch != NeedChannel.Room) rate = rate * t.NeedSleepPermille / 1000;
                long acc = p.NeedFrac[i] + rate;
                long whole = acc / den;
                p.NeedFrac[i] = (int)(acc - whole * den);
                int v = p.Needs[i] + (int)whole;
                p.Needs[i] = (short)(v > 1000 ? 1000 : v);
            }
            // health: thirst at or above the damage line wears the body down (the existing Health field, as hunger does in Body())
            if (Unified.NeedActive(s, NeedChannel.Thirst) && p.NeedOf(NeedChannel.Thirst) >= t.ThirstDamageAt)
            {
                npc.Health -= (int)(t.ThirstLossPerDay / tpd);
                if (npc.Health <= 0) npc.DeathCause = DeathCause.Starvation; // no thirst cause in the frozen enum: recorded as an amendment request
            }
        }
    }
}
