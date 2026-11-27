using System.Collections.Generic;
using God.Sim.Population;

namespace God.Sim.Divinity
{
    /// <summary>
    /// D5's part of <see cref="DivinityState"/> (F1.2: created empty by F1; OWNER: D4, which absorbs D5). Its sub-object types are declared below; the container hashes them in the fixed order.
    /// </summary>
    public sealed partial class DivinityState
    {
    }

    /// <summary>
    /// POSSESSION_AND_AVATAR 3: one manifestation (warehouse <c>fact_avatar_manifestation</c>), keyed by its InterventionId,
    /// open while the god's <c>AvatarBody</c> control entry exists. The body is the god's one avatar record (<see cref="Body"/> =
    /// <c>AvatarBodyId</c>), reused by every manifestation.
    /// </summary>
    public sealed class AvatarSpell
    {
        public int Intervention, God, Body, Node;
        public long StartTick;
        public int DamageTaken, Blows, ActionsStarted, InputsApplied, InputsRefused;
        /// <summary>The tick of the action input not yet resolved into a started or refused act (-1: none).</summary>
        public long PendingAction = -1;
        /// <summary>Distinct people who attended an action of the avatar (ascending id, at most <c>Avatar.MaxObservers</c>).</summary>
        public List<int> Observers = new List<int>();
        /// <summary>The last blow (filled before a destruction is reported): who struck, why, with which intervention.</summary>
        public int LastBy, LastIntervention;
        public DamageCause LastCause;

        public void AddTo(ref StateHasher h)
        {
            h.Add(Intervention); h.Add(God); h.Add(Body); h.Add(Node); h.Add(StartTick);
            h.Add(DamageTaken); h.Add(Blows); h.Add(ActionsStarted); h.Add(InputsApplied); h.Add(InputsRefused); h.Add(PendingAction);
            h.Add(Observers.Count); foreach (int o in Observers) h.Add(o);
            h.Add(LastBy); h.Add(LastIntervention); h.Add((int)LastCause);
        }

        /// <summary>Adds a distinct observer (sorted); false when known or the list is full.</summary>
        public bool Observe(int npc, int cap)
        {
            int i = Observers.BinarySearch(npc);
            if (i >= 0 || Observers.Count >= cap) return false;
            Observers.Insert(~i, npc);
            return true;
        }
    }

    /// <summary>
    /// The embodiment registry <c>DivinityState.Avatars</c> (OWNER: D4, holding D5's slot): the open possession spells and the
    /// open manifestations (each ascending InterventionId), and running totals. Null until the first possession or manifestation,
    /// so a game where no god embodies itself hashes exactly as before (pin P2).
    /// </summary>
    public sealed partial class AvatarRegistry
    {
        public List<PossessionSpell> Possessions = new List<PossessionSpell>();
        public List<AvatarSpell> Manifestations = new List<AvatarSpell>();
        /// <summary>Totals: possessions begun and ended, manifestations, destructions.</summary>
        public long PossessionsBegun, PossessionsEnded, Manifested, Destroyed;

        /// <summary>A path cache for the embodied walks (give, the avatar's take); not state.</summary>
        [Newtonsoft.Json.JsonIgnore] internal NodePather Pather;

        partial void Hash(ref StateHasher h)
        {
            h.Add(Possessions.Count); foreach (var p in Possessions) p.AddTo(ref h);
            h.Add(Manifestations.Count); foreach (var a in Manifestations) a.AddTo(ref h);
            h.Add(PossessionsBegun); h.Add(PossessionsEnded); h.Add(Manifested); h.Add(Destroyed);
        }

        public PossessionSpell PossessionOf(int intervention)
        {
            foreach (var p in Possessions) if (p.Intervention == intervention) return p;
            return null;
        }

        public PossessionSpell PossessionOfGod(int god)
        {
            foreach (var p in Possessions) if (p.God == god) return p;
            return null;
        }

        public AvatarSpell ManifestationOf(int intervention)
        {
            foreach (var a in Manifestations) if (a.Intervention == intervention) return a;
            return null;
        }

        public AvatarSpell ManifestationOfGod(int god)
        {
            foreach (var a in Manifestations) if (a.God == god) return a;
            return null;
        }
    }
}
