using System;
using God.Sim.Learning;

namespace God.Sim.Population
{
    /// <summary>Why a lot moved (10-31 reserved).</summary>
    public enum MoveReason : byte { PickUp = 1, Drop = 2, Deposit = 3, Withdraw = 4, HandOver = 5, EstateIn = 6, EstateOut = 7, Claim = 8, DeathSpill = 9 }

    /// <summary>Who owns a moved lot at its destination. A lot on the ground is always ownerless, whatever the rule.</summary>
    public enum OwnerRule : byte { Keep = 0, ToReceiver = 1, ToBinOwner = 2, Set = 3 }

    /// <summary>Whether a moved lot may merge with an equal lot at its destination (<see cref="Never"/> for estates).</summary>
    public enum MergeRule : byte { Merge = 0, Never = 1 }

    /// <summary><see cref="OutOfReach"/> (D3, A1): the acting person is not within the move's <see cref="Reach"/> of a holder.</summary>
    public enum MoveStatus : byte { Ok = 0, NoSuchLot = 1, NotEnough = 2, NoRoom = 3, BadHolder = 4, SameHolder = 5, OutOfReach = 6 }

    /// <summary>
    /// Why an owner changed (SHARED_CONTRACTS 3): 0-7 are ECONOMY's <c>ClaimBasis</c>; 8-13 are F0's; 14-31 reserved.
    /// <see cref="Auto"/> (a parameter value only) derives it from the <see cref="MoveReason"/>.
    /// </summary>
    public enum OwnershipBasis : byte
    {
        FirstArrival = 0, Built = 1, Exchanged = 2, Gift = 3, Inherited = 4, StateGrant = 5, Seized = 6, Founded = 7,
        Produced = 8, Deposited = 9, Abandoned = 10, Spilled = 11, Escheat = 12, Divided = 13,
        Auto = 255,
    }

    /// <summary>
    /// The result of <see cref="Goods.Move"/>. <see cref="Lot"/>: the id the moved goods had when they left (after a split, the
    /// new id); <see cref="IntoLot"/>: the id they have at the destination (the surviving, lower id when they merged);
    /// <see cref="EndedLot"/>: the id that a merge ended (0 when nothing merged); <see cref="ParentLot"/>: the lot split from (0
    /// when the whole lot moved).
    /// </summary>
    public struct MoveResult
    {
        public MoveStatus Status;
        public int Lot, IntoLot, EndedLot, ParentLot;
        public Owner Before, After;
        public bool Ok => Status == MoveStatus.Ok;
    }

    /// <summary>A hand-over between two living people, owner changed or not (the input of <c>SocialHooks.OnHandOver</c>, A3).</summary>
    public struct HandOver
    {
        public int Giver, Receiver;
        public Ware Ware;
        public int Count;
        public int Lot;
        public int PurposeRef;
        public MoveReason Reason;
        public Owner FromOwner, ToOwner;
        /// <summary>The owner changed (a gift, an exchange step); false for household goods fed to a member or a loan (owner kept).</summary>
        public bool OwnerChanged;
    }

    /// <summary>
    /// The one sanctioned goods API (SHARED_CONTRACTS 2.2, F0). Only this class (and <see cref="PopulationModule.NewLot"/>,
    /// which it calls) adds, removes, splits, merges or re-owns a lot, or changes <c>NextLotId</c>; a static test checks it.
    /// Every operation is atomic (on any status but Ok nothing changes and nothing is emitted), immediate (inside the actor's
    /// own act, in tick order) and checks physical facts only (presence, count, room); reach, consent and rights are the
    /// caller's. It keeps the goods ledger and pays the goods reward (2.3), the only goods channel into learning.
    /// <para>Merge rules (the M3 rules, plus the owner): carry merges the same ware and owner (tools never), keeping the lower
    /// id and the earlier spoil day; ground merges the same node, ware and spoil day (tools never); a bin merges the same ware,
    /// spoil day and owner (tools never) unless <see cref="MergeRule.Never"/>. Ground lots are always ownerless.</para>
    /// </summary>
    public static class Goods
    {
        /// <summary>A hand-over reaches at most this many nodes (alias of <see cref="Reach.HandOver"/>; enforced by <see cref="Move"/>).</summary>
        public const int HandOverReach = Reach.HandOver;

        // ---------------- produce, consume ----------------

        /// <summary>
        /// Goods enter the world into a holder as a new lot (a new id, always). Returns the id the goods have there (the
        /// surviving lower id when they merged), or 0 when there is no room (nothing produced). Emits <see cref="Produced"/>
        /// (<paramref name="ev"/> may be null at game start) and books the ledger; pays the production reward (2.3).
        /// </summary>
        public static int Produce(GodState s, EventBuffer ev, Holder to, Ware w, int count, int spoilDay, int uses,
            Owner owner, ProduceReason why, int actor, int source, bool withTool = false)
        {
            var p = s.Population;
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (to.Kind == HolderKind.Ground) owner = Owner.None;
            if (!Fits(s, to, w, count, owner, MergeRule.Merge)) return 0;
            var lot = PopulationModule.NewLot(s, w, count, spoilDay, uses);
            lot.Owner = owner;
            Insert(s, to, lot, MergeRule.Merge, out int into, out _);
            p.Ledger.Produce(why, w, count);
            ev?.Add(new Produced
            {
                Npc = actor, Source = source, Ware = w, Count = count, Reason = why, WithTool = withTool,
                Lot = lot.Id, IntoLot = into, OwnerKind = owner.Kind, OwnerId = owner.Id,
            });
            PayProduce(s, ev, owner, w, count, why);
            return into;
        }

        /// <summary>
        /// Goods taken from the wild and used at once without becoming a lot (eating at the source): books the ledger and
        /// emits <see cref="Produced"/> with <c>Lot = 0</c>. Pays nothing. Pair it with <see cref="ConsumeInHand"/>.
        /// </summary>
        public static void ProduceInHand(GodState s, EventBuffer ev, Ware w, int count, ProduceReason why, int actor, int source)
        {
            s.Population.Ledger.Produce(why, w, count);
            ev?.Add(new Produced { Npc = actor, Source = source, Ware = w, Count = count, Reason = why });
        }

        /// <summary>The other half of <see cref="ProduceInHand"/>: goods used up that were never a lot (<c>Lot = 0</c>).</summary>
        public static void ConsumeInHand(GodState s, EventBuffer ev, Ware w, int count, ConsumeReason why, int actor, int refId = 0)
        {
            s.Population.Ledger.Consume(why, w, count);
            ev?.Add(new Consumed { Npc = actor, Hut = why == ConsumeReason.Built ? refId : 0, Ware = w, Count = count, Reason = why, RefId = refId });
        }

        /// <summary>
        /// Goods leave the world from a holder (eaten, spoiled, built into a hut, worn out, buried). False when the lot is
        /// not there or holds fewer. Emits <see cref="Consumed"/>, books the ledger, and books spoilage against its owner (2.3).
        /// </summary>
        public static bool Consume(GodState s, EventBuffer ev, Holder from, int lotId, int count, ConsumeReason why, int actor, int refId = 0)
        {
            if (count <= 0 || !Find(s, from, lotId, out var at)) return false;
            if (!InReach(s, actor, from, Reach.Touch)) return false;
            var lot = Read(s, at);
            if (lot.Count < count) return false;
            if (lot.Count == count) Remove(s, at);
            else { lot.Count -= count; Write(s, at, lot); }
            s.Population.Ledger.Consume(why, lot.Ware, count);
            ev?.Add(new Consumed
            {
                Npc = actor, Hut = why == ConsumeReason.Built ? refId : 0, Ware = lot.Ware, Count = count, Reason = why, RefId = refId,
                Lot = lotId, OwnerKind = lot.Owner.Kind, OwnerId = lot.Owner.Id,
            });
            if (why == ConsumeReason.Spoiled) PaySpoiled(s, ev, lot.Owner, lotId, lot.Ware, count);
            return true;
        }

        /// <summary>One use of a tool lot: its uses fall by one; at zero it is consumed (<see cref="ConsumeReason.Wear"/>). Returns true when it wore out.</summary>
        public static bool Wear(GodState s, EventBuffer ev, Holder at, int lotId, int actor)
        {
            if (!Find(s, at, lotId, out var loc) || !InReach(s, actor, at, Reach.Touch)) return false;
            var lot = Read(s, loc);
            lot.Uses--;
            Write(s, loc, lot);
            if (lot.Uses > 0) return false;
            Consume(s, ev, at, lotId, lot.Count, ConsumeReason.Wear, actor);
            return true;
        }

        // ---------------- move, own ----------------

        /// <summary>
        /// Moves <paramref name="count"/> of a lot from one holder to another (SHARED_CONTRACTS 2.2). A partial move splits:
        /// the moved part gets a new id. Emits exactly one <see cref="ItemMoved"/>, and one <see cref="OwnershipChanged"/>
        /// when the owner changed; pays the ownership reward (2.3); a hand-over from one person (or its household) to another
        /// person calls <c>SocialHooks.OnHandOver</c>.
        /// </summary>
        public static MoveResult Move(GodState s, EventBuffer ev, Holder from, int lotId, int count, Holder to,
            MoveReason why, int actor, OwnerRule rule = OwnerRule.Keep, Owner set = default,
            MergeRule merge = MergeRule.Merge, int purposeRef = 0, OwnershipBasis basis = OwnershipBasis.Auto)
        {
            var r = new MoveResult();
            if (from.Same(to)) { r.Status = MoveStatus.SameHolder; return r; }
            if (!Valid(s, from) || !Valid(s, to)) { r.Status = MoveStatus.BadHolder; return r; }
            if (!Find(s, from, lotId, out var at)) { r.Status = MoveStatus.NoSuchLot; return r; }
            int reach = Reach.Of(why);
            if (reach >= 0 && !InReach(s, actor, from, reach) | !InReach(s, actor, to, reach)) { r.Status = MoveStatus.OutOfReach; return r; }
            var lot = Read(s, at);
            if (count <= 0 || count > lot.Count) { r.Status = MoveStatus.NotEnough; return r; }
            var after = OwnerAt(s, to, rule, set, lot.Owner, actor);
            if (!Fits(s, to, lot.Ware, count, after, merge)) { r.Status = MoveStatus.NoRoom; return r; }
            var p = s.Population;
            int actorNode = p.Find(actor)?.Node ?? -1, fromNode = NodeOf(s, from), toNode = NodeOf(s, to);
            r.Before = lot.Owner; r.After = after;
            var moved = lot;
            if (count < lot.Count)
            {
                lot.Count -= count;
                Write(s, at, lot);
                moved.Count = count;
                moved.Id = p.NextLotId++;
                r.ParentLot = lot.Id;
            }
            else Remove(s, at);
            moved.Owner = after;
            r.Lot = moved.Id;
            Insert(s, to, moved, merge, out r.IntoLot, out r.EndedLot);
            ev?.Add(new ItemMoved
            {
                Lot = r.Lot, IntoLot = r.IntoLot, EndedLot = r.EndedLot, ParentLot = r.ParentLot, Ware = moved.Ware, Count = count,
                FromKind = from.Kind, FromId = from.Id, ToKind = to.Kind, ToId = to.Id, Reason = why, Actor = actor, ActorNode = actorNode,
                FromNode = fromNode, ToNode = toNode,
                OwnerBeforeKind = r.Before.Kind, OwnerBeforeId = r.Before.Id, OwnerAfterKind = after.Kind, OwnerAfterId = after.Id, PurposeRef = purposeRef,
            });
            if (!r.Before.Same(after))
            {
                var b = basis != OwnershipBasis.Auto ? basis : BasisOf(why);
                ev?.Add(new OwnershipChanged
                {
                    AssetKind = OwnershipChanged.AssetLot, Lot = r.IntoLot, Ware = moved.Ware, Count = count,
                    FromKind = r.Before.Kind, FromId = r.Before.Id, ToKind = after.Kind, ToId = after.Id, Basis = b, Actor = actor,
                });
                if (why != MoveReason.Drop && why != MoveReason.DeathSpill) PayOwnerChange(s, ev, r.Before, after, moved.Ware, count);
            }
            // A3: every hand-over between two living people is observed by both (M5 filters on OwnerChanged)
            if (why == MoveReason.HandOver && from.Kind == HolderKind.Carry && to.Kind == HolderKind.Carry)
            {
                var giver = p.Find(from.Id); var receiver = p.Find(to.Id);
                if (giver != null && giver.Alive && receiver != null && receiver.Alive)
                {
                    var h = new HandOver
                    {
                        Giver = from.Id, Receiver = to.Id, Ware = moved.Ware, Count = count, Lot = r.IntoLot, PurposeRef = purposeRef, Reason = why,
                        FromOwner = r.Before, ToOwner = after, OwnerChanged = !r.Before.Same(after),
                    };
                    Social.SocialHooks.OnHandOver(s, ev, in h);
                }
            }
            return r;
        }

        /// <summary>Changes a lot's owner where it lies (no movement). Ground lots stay ownerless (false). Emits <see cref="OwnershipChanged"/>; pays the ownership reward.</summary>
        public static bool SetOwner(GodState s, EventBuffer ev, Holder at, int lotId, Owner to, OwnershipBasis basis, int actor)
        {
            if (at.Kind == HolderKind.Ground && !to.IsNone) return false;
            if (!Find(s, at, lotId, out var loc)) return false;
            var lot = Read(s, loc);
            if (lot.Owner.Same(to)) return true;
            var before = lot.Owner;
            lot.Owner = to;
            Write(s, loc, lot);
            ev?.Add(new OwnershipChanged
            {
                AssetKind = OwnershipChanged.AssetLot, Lot = lotId, Ware = lot.Ware, Count = lot.Count,
                FromKind = before.Kind, FromId = before.Id, ToKind = to.Kind, ToId = to.Id, Basis = basis == OwnershipBasis.Auto ? OwnershipBasis.Gift : basis, Actor = actor,
            });
            PayOwnerChange(s, ev, before, to, lot.Ware, lot.Count);
            return true;
        }

        // ---------------- bins ----------------

        /// <summary>A new, empty bin (household store or estate). Returns its id.</summary>
        public static int NewBin(GodState s, BinKind kind, Owner owner, int node, int weightCap)
        {
            var p = s.Population;
            var b = new LotBin { Id = p.NextBinId++, Kind = kind, Owner = owner, Node = node, WeightCap = weightCap };
            p.Bins.Add(b); // ids ascend, so the list stays sorted
            return b.Id;
        }

        /// <summary>Removes an empty bin. Throws unless it exists and is empty.</summary>
        public static void RemoveBin(GodState s, int binId)
        {
            var b = BinById(s.Population, binId) ?? throw new InvalidOperationException("no bin " + binId);
            if (b.Lots.Count > 0) throw new InvalidOperationException("bin " + binId + " is not empty");
            s.Population.Bins.Remove(b);
        }

        public static LotBin BinById(PopulationState p, int id)
        {
            foreach (var b in p.Bins) if (b.Id == id) return b;
            return null;
        }

        // ---------------- the goods reward (2.3) ----------------

        /// <summary>
        /// True when the realised-value rule (M3b, <see cref="GoodsCredit.Realised"/>) credits these goods later through lot
        /// episodes (food when eaten or spoiled, a tool by the work it adds), so taking them pays nothing now.
        /// </summary>
        public static bool CreditedLater(GodState s, Ware w) => s.Tuning.Learning.Goods == GoodsCredit.Realised && (Wares.IsFood(w) || w == Ware.Axe);

        /// <summary>Production pays its owner for the goods a decision produced (foraged, gathered), unless credited later. Founder packs and placed goods pay nothing.</summary>
        private static void PayProduce(GodState s, EventBuffer ev, Owner owner, Ware w, int count, ProduceReason why)
        {
            if (owner.Kind != OwnerKind.Npc || why != ProduceReason.Foraged && why != ProduceReason.HandGathered || CreditedLater(s, w)) return;
            var npc = s.Population.Find(owner.Id);
            if (npc?.Mind == null) return;
            Learner.AddReward(npc.Mind.Store, GoodsWorth.Of(s, npc, w) * count);
        }

        /// <summary>An owner change: the NPC losing ownership loses the worth, the NPC gaining it gains it; nothing between an NPC and its own household, nothing for goods credited later.</summary>
        private static void PayOwnerChange(GodState s, EventBuffer ev, Owner before, Owner after, Ware w, int count)
        {
            if (CreditedLater(s, w)) return;
            var p = s.Population;
            if (before.Kind == OwnerKind.Npc)
            {
                var a = p.Find(before.Id);
                bool ownHousehold = a != null && after.Kind == OwnerKind.Household && a.Household != 0 && a.Household == after.Id;
                if (a?.Mind != null && !ownHousehold) Learner.AddReward(a.Mind.Store, -GoodsWorth.Of(s, a, w) * count);
            }
            if (after.Kind == OwnerKind.Npc)
            {
                var b = p.Find(after.Id);
                bool ownHousehold = b != null && before.Kind == OwnerKind.Household && b.Household != 0 && b.Household == before.Id;
                if (b?.Mind != null && !ownHousehold) Learner.AddReward(b.Mind.Store, GoodsWorth.Of(s, b, w) * count);
            }
        }

        /// <summary>
        /// Spoilage costs the owning NPC the goods' worth: booked against the decisions that gained the lot (M3b lot episodes,
        /// shared), or as a plain reward when no decision waits for it (or under the M3 rule).
        /// </summary>
        private static void PaySpoiled(GodState s, EventBuffer ev, Owner owner, int lotId, Ware w, int count)
        {
            if (owner.Kind != OwnerKind.Npc) return;
            var npc = s.Population.Find(owner.Id);
            if (npc?.Mind == null) return;
            var lt = s.Tuning.Learning;
            int day = (int)SimCalendar.Of(s).Day(s.Tick);
            int loss = -GoodsWorth.Of(s, npc, w) * count;
            bool lotCredit = lt.Goods != GoodsCredit.Immediate;
            var sink = s.Population.GoodsSink;
            if (lotCredit) sink.Bind(ev, npc.Id, EventLearningSink.IsSampledDay(npc.Id, day, lt.SampleEvery));
            if (!lotCredit || Learner.Outcome(npc.Mind.Store, lt, PopulationSystem.TraitsOf(s, npc), Learner.LotEpisodeKey(lotId), loss, day, sink, true) == 0)
                Learner.AddReward(npc.Mind.Store, loss);
            else if (loss > 0) npc.Mind.Store.RewardSumDay += loss;
            else npc.Mind.Store.PunishSumDay -= loss;
        }

        private static OwnershipBasis BasisOf(MoveReason why)
        {
            switch (why)
            {
                case MoveReason.PickUp: case MoveReason.Claim: return OwnershipBasis.FirstArrival;
                case MoveReason.Drop: return OwnershipBasis.Abandoned;
                case MoveReason.DeathSpill: return OwnershipBasis.Spilled;
                case MoveReason.Deposit: case MoveReason.Withdraw: return OwnershipBasis.Deposited;
                case MoveReason.EstateIn: case MoveReason.EstateOut: return OwnershipBasis.Inherited;
                default: return OwnershipBasis.Gift;
            }
        }

        /// <summary>
        /// The "equipped" state (D3, WORLD_ONTOLOGY 4): a tool lot in a person's carry is equipped (M3 has no separate flag: every carried tool is
        /// in hand and works, e.g. the axe for gathering). There is no Equipped holder kind.
        /// </summary>
        public static bool IsEquipped(in ItemLot l) => !l.Empty && (l.Ware == Ware.Axe || Wares.IsGear(l.Ware));

        /// <summary>
        /// The interaction-range check (D3, A1): a person (<paramref name="actor"/> a living or just-dead NPC; 0 = the sim) reaches its
        /// own carry always, and another holder only within <paramref name="reach"/> nodes of its own node.
        /// </summary>
        public static bool InReach(GodState s, int actor, Holder h, int reach)
        {
            if (actor <= 0) return true;
            var a = s.Population.Find(actor);
            if (a == null) return true;
            if (h.Kind == HolderKind.Carry && h.Id == actor) return true;
            int node;
            switch (h.Kind)
            {
                case HolderKind.Carry: { var o = s.Population.Find(h.Id); if (o == null) return false; node = o.Node; break; }
                case HolderKind.Ground: node = h.Id; break;
                case HolderKind.Bin: { var b = BinById(s.Population, h.Id); if (b == null) return false; node = b.Node; break; }
                default: return false;
            }
            return s.Territory.Grid.Distance(a.Node, node) <= reach;
        }

        // ---------------- holders ----------------

        /// <summary>The node a holder is at (a carry: its carrier's node), -1 when unknown.</summary>
        public static int NodeOf(GodState s, Holder h)
        {
            switch (h.Kind)
            {
                case HolderKind.Carry: return s.Population.Find(h.Id)?.Node ?? -1;
                case HolderKind.Ground: return h.Id;
                case HolderKind.Bin: return BinById(s.Population, h.Id)?.Node ?? -1;
                default: return -1;
            }
        }

        /// <summary>Where a lot sits: the holder, and the slot (carry) or list index (ground, bin).</summary>
        private struct Loc
        {
            public Holder H;
            public Npc Npc;
            public LotBin Bin;
            public int Index;
        }

        private static bool Valid(GodState s, Holder h)
        {
            var p = s.Population;
            switch (h.Kind)
            {
                case HolderKind.Carry: return p.Find(h.Id) != null; // a dead carrier may still be emptied (estate, spill); receiving needs a living one (Fits)
                case HolderKind.Ground: return h.Id >= 0 && h.Id < s.Territory.Grid.Count;
                case HolderKind.Bin: return BinById(p, h.Id) != null;
                default: return false;
            }
        }

        private static bool Find(GodState s, Holder h, int lotId, out Loc at)
        {
            at = new Loc { H = h, Index = -1 };
            if (lotId <= 0) return false;
            var p = s.Population;
            switch (h.Kind)
            {
                case HolderKind.Carry:
                {
                    var n = p.Find(h.Id);
                    if (n == null) return false;
                    at.Npc = n;
                    for (int i = 0; i < n.Carry.Length; i++) if (!n.Carry[i].Empty && n.Carry[i].Id == lotId) { at.Index = i; return true; }
                    return false;
                }
                case HolderKind.Ground:
                    for (int i = 0; i < p.Lots.Count; i++) if (p.Lots[i].Lot.Id == lotId && p.Lots[i].Node == h.Id) { at.Index = i; return true; }
                    return false;
                case HolderKind.Bin:
                {
                    var b = BinById(p, h.Id);
                    if (b == null) return false;
                    at.Bin = b;
                    for (int i = 0; i < b.Lots.Count; i++) if (b.Lots[i].Id == lotId) { at.Index = i; return true; }
                    return false;
                }
            }
            return false;
        }

        private static ItemLot Read(GodState s, in Loc at)
        {
            switch (at.H.Kind)
            {
                case HolderKind.Carry: return at.Npc.Carry[at.Index];
                case HolderKind.Ground: return s.Population.Lots[at.Index].Lot;
                default: return at.Bin.Lots[at.Index];
            }
        }

        private static void Write(GodState s, in Loc at, ItemLot lot)
        {
            switch (at.H.Kind)
            {
                case HolderKind.Carry: at.Npc.Carry[at.Index] = lot; break;
                case HolderKind.Ground: s.Population.Lots[at.Index].Lot = lot; break;
                default: at.Bin.Lots[at.Index] = lot; break;
            }
        }

        private static void Remove(GodState s, in Loc at)
        {
            switch (at.H.Kind)
            {
                case HolderKind.Carry: at.Npc.Carry[at.Index] = default; break;
                case HolderKind.Ground: s.Population.Lots.RemoveAt(at.Index); s.Population.IndexDirty = true; break;
                default: at.Bin.Lots.RemoveAt(at.Index); break;
            }
        }

        /// <summary>The owner a lot gets at its destination.</summary>
        private static Owner OwnerAt(GodState s, Holder to, OwnerRule rule, Owner set, Owner before, int actor)
        {
            if (to.Kind == HolderKind.Ground) return Owner.None;
            switch (rule)
            {
                case OwnerRule.ToReceiver:
                    if (to.Kind == HolderKind.Carry) return Owner.Npc(to.Id);
                    return BinById(s.Population, to.Id)?.Owner ?? before;
                case OwnerRule.ToBinOwner: return to.Kind == HolderKind.Bin ? BinById(s.Population, to.Id)?.Owner ?? before : before;
                case OwnerRule.Set: return set;
                default: return before;
            }
        }

        private static bool Mergeable(in ItemLot a, Ware w, Owner owner) => !a.Empty && a.Ware == w && w != Ware.Axe && a.Owner.Same(owner);

        /// <summary>Room for <paramref name="count"/> units at a holder: weight and, for a carry, a matching or free slot.</summary>
        private static bool Fits(GodState s, Holder to, Ware w, int count, Owner owner, MergeRule merge)
        {
            var p = s.Population;
            switch (to.Kind)
            {
                case HolderKind.Carry:
                {
                    var npc = p.Find(to.Id);
                    if (npc == null || !npc.Alive) return false;
                    int free = s.Tuning.Population.CarryWeight - PopulationModule.CarryWeight(npc);
                    if (free / PopulationModule.Weight(w) < count) return false;
                    foreach (var l in npc.Carry) if (l.Empty || merge == MergeRule.Merge && Mergeable(l, w, owner)) return true;
                    return false;
                }
                case HolderKind.Ground: return to.Id >= 0 && to.Id < s.Territory.Grid.Count;
                case HolderKind.Bin:
                {
                    var b = BinById(p, to.Id);
                    if (b == null) return false;
                    return b.WeightCap == 0 || b.Weight + PopulationModule.Weight(w) * count <= b.WeightCap;
                }
            }
            return false;
        }

        /// <summary>Puts a lot into a holder (room checked before). <paramref name="into"/>: the id it has there; <paramref name="ended"/>: the id a merge ended (0 none).</summary>
        private static void Insert(GodState s, Holder to, ItemLot lot, MergeRule merge, out int into, out int ended)
        {
            var p = s.Population;
            into = lot.Id;
            ended = 0;
            switch (to.Kind)
            {
                case HolderKind.Carry:
                {
                    var npc = p.Find(to.Id);
                    // the same ware merges into one lot; a mixed lot rots with its oldest part (the earlier spoil day)
                    if (merge == MergeRule.Merge)
                        for (int i = 0; i < npc.Carry.Length; i++)
                            if (Mergeable(npc.Carry[i], lot.Ware, lot.Owner))
                            {
                                ref var c = ref npc.Carry[i];
                                c.Count += lot.Count;
                                if (lot.Id < c.Id) { ended = c.Id; c.Id = lot.Id; } else ended = lot.Id;
                                if (lot.SpoilDay >= 0 && (c.SpoilDay < 0 || lot.SpoilDay < c.SpoilDay)) c.SpoilDay = lot.SpoilDay;
                                into = c.Id;
                                return;
                            }
                    for (int i = 0; i < npc.Carry.Length; i++)
                        if (npc.Carry[i].Empty) { npc.Carry[i] = lot; return; }
                    throw new InvalidOperationException("goods: no carry slot after the room check");
                }
                case HolderKind.Ground:
                {
                    lot.Owner = Owner.None;
                    if (merge == MergeRule.Merge)
                        foreach (var g in p.Lots)
                            if (g.Node == to.Id && g.Lot.Ware == lot.Ware && g.Lot.SpoilDay == lot.SpoilDay && lot.Ware != Ware.Axe)
                            {
                                g.Lot.Count += lot.Count;
                                if (lot.Id < g.Lot.Id) { ended = g.Lot.Id; g.Lot.Id = lot.Id; } else ended = lot.Id;
                                into = g.Lot.Id;
                                return;
                            }
                    p.Lots.Add(new GroundLot { Node = to.Id, Lot = lot });
                    p.Lots.Sort((a, b) => a.Lot.Id.CompareTo(b.Lot.Id));
                    p.IndexDirty = true;
                    return;
                }
                default:
                {
                    var b = BinById(p, to.Id);
                    if (merge == MergeRule.Merge)
                        for (int i = 0; i < b.Lots.Count; i++)
                        {
                            var l = b.Lots[i];
                            if (Mergeable(l, lot.Ware, lot.Owner) && l.SpoilDay == lot.SpoilDay)
                            {
                                l.Count += lot.Count;
                                if (lot.Id < l.Id) { ended = l.Id; l.Id = lot.Id; } else ended = lot.Id;
                                b.Lots[i] = l;
                                b.Lots.Sort((x, y) => x.Id.CompareTo(y.Id));
                                into = l.Id;
                                return;
                            }
                        }
                    int at = b.Lots.Count;
                    while (at > 0 && b.Lots[at - 1].Id > lot.Id) at--;
                    b.Lots.Insert(at, lot);
                    return;
                }
            }
        }
    }

    /// <summary>The goods channel's worth of a ware to one NPC (moved unchanged from <c>PopulationSystem.Worth</c>, M3).</summary>
    public static class GoodsWorth
    {
        /// <summary>The NPC's own worth of a ware gained or lost: food a little, logs a lot while it needs a hut, tools some.</summary>
        public static int Of(GodState s, Npc npc, Ware w)
        {
            var tu = s.Tuning.Population;
            if (Wares.IsFood(w)) return tu.FoodWorth;
            if (w == Ware.Log) return npc.HomeHut == 0 ? tu.LogWorthNeeded : tu.LogWorthHoused;
            if (w == Ware.Axe) return tu.ToolWorth;
            return 0;
        }
    }
}
