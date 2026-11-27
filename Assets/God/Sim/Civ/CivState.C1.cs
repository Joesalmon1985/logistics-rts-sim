using System.Collections.Generic;
using God.Sim.Population;
using Newtonsoft.Json;

namespace God.Sim.Civ
{
    /// <summary>
    /// C1's part of <see cref="CivState"/> (F1.2: created empty by F1; OWNER: C1). Its sub-object type is declared below; the container hashes it in the fixed order.
    /// </summary>
    public sealed partial class CivState
    {
    }

    /// <summary>
    /// WORLD_ONTOLOGY O7 (the truth overlay of a dropped owned lot): <see cref="Goods"/> keeps every ground lot ownerless (frozen
    /// rule C4), so C1 records here who still owns the dropped units until <see cref="Day"/> + <c>Civ.C1.LostPropertyDays</c>.
    /// </summary>
    public struct LostLot
    {
        /// <summary>The ground lot id (follows merges on the ground).</summary>
        public int Lot;
        public int Node;
        public Ware Ware;
        /// <summary>How many units of that lot are still the owner's.</summary>
        public int Count;
        public Owner Owner;
        /// <summary>The sim day it was dropped.</summary>
        public int Day;
    }

    /// <summary>A first-arrival claim on a source (ECONOMY 12 "Land access"): recorded only (spec 5.11). The claim API is general (<see cref="Property.Claim"/>).</summary>
    public struct SourceClaim
    {
        /// <summary>What is claimed: 1 a resource source (its id); later kinds (sites, deposits) append.</summary>
        public byte ThingKind;
        public int Thing;
        public Owner Claimant;
        public OwnershipBasis Basis;
        public long Tick;
    }

    /// <summary>
    /// C1's sub-object (OWNER: C1): productive assets (ascending id), the AssetId counter (R20: the one allocator), the O7
    /// lost-property overlay (by drop order) and source claims (by thing id).
    /// </summary>
    public sealed partial class OwnershipState
    {
        /// <summary>Contracts 22.4 <c>CivState.C1.NextAssetId</c>: the AssetId counter, never reused.</summary>
        public int NextAssetId = 1;
        public List<ProductiveAsset> Assets = new List<ProductiveAsset>();
        public List<LostLot> Lost = new List<LostLot>();
        public List<SourceClaim> Claims = new List<SourceClaim>();
        /// <summary>Counters (analytics and the card): work units, thefts classified, lost lots abandoned.</summary>
        public long WorkUnits, Thefts, Abandoned;
        /// <summary>The last day the day-start bookkeeping ran.</summary>
        public int LastDay = -1;

        /// <summary>A path cache for the C1 acts (rebuilt on demand, never saved or hashed).</summary>
        [JsonIgnore] internal NodePather Pather;

        partial void Hash(ref StateHasher h)
        {
            h.Add(NextAssetId);
            h.Add(Assets.Count);
            foreach (var a in Assets) a.AddTo(ref h);
            h.Add(Lost.Count);
            foreach (var l in Lost) { h.Add(l.Lot); h.Add(l.Node); h.Add((int)l.Ware); h.Add(l.Count); h.Add((int)l.Owner.Kind); h.Add(l.Owner.Id); h.Add(l.Day); }
            h.Add(Claims.Count);
            foreach (var c in Claims) { h.Add(c.ThingKind); h.Add(c.Thing); h.Add((int)c.Claimant.Kind); h.Add(c.Claimant.Id); h.Add((int)c.Basis); h.Add(c.Tick); }
            h.Add(WorkUnits); h.Add(Thefts); h.Add(Abandoned); h.Add(LastDay);
        }
    }
}
