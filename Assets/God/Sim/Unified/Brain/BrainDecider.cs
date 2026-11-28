using God.Sim.Comms;
using God.Sim.Learning;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN6 (§D9.2 a, d; spec 5.5-5.7): the decision of one person, taken only when its primitive completed, failed or was interrupted
    /// (alive, awake, not busy, not possessed). In order: sense (local perception, physical legality); judge the previous submission
    /// (an own success becomes known and is offered to the persons who perceive it); remember what is perceived (places, people);
    /// credit the overlay with the change of the person's own need pressures since the last decision (never goods, holders or title);
    /// bias the control head by the overlay; optional bounded planning (an intention record only); the policy decides; the motor
    /// receives the control with its origin; the overlay opens a trace for it. No allocation; O(1) for a person with no decision due.
    /// </summary>
    public static class BrainDecider
    {
        /// <summary>True when the person decides this tick (the schedule; reads sim state only, never a view).</summary>
        public static bool Due(GodState s, Npc npc, UnifiedNpc u)
        {
            if (u == null || !npc.Alive || !Bodies.IsPerson(npc)) return false;
            if (npc.Action.Asleep || u.Busy) return false;
            if (s.Divinity != null && Divinity.Control.SkipsPolicy(s, npc)) return false;
            int stagger = s.Tuning.Unification.UN6.Stagger;
            if (stagger > 1 && u.Brain.LastDecisionTick < 0 && s.Tick < npc.Id % stagger) return false;
            return true;
        }

        /// <summary>A person's brain made ready (innate knowledge, its overlay store): once, at its first tick or at birth (R83).</summary>
        public static void Init(GodState s, Npc npc, UnifiedNpc u)
        {
            var b = u.Brain;
            if (b.Started) return;
            int innate = s.Tuning.Unification.UN6.InnateInteractions;
            for (int k = 0; k < 32; k++) if ((innate & (1 << k)) != 0) Knowledge.Learn(b, KnowledgeKind.Interaction, k);
            b.Started = true;
            if (s.Tuning.Unification.UN6.Overlay != 0)
            {
                var rt = s.Unification.BrainRuntime;
                var caps = rt != null ? rt.Caps : new LearningCaps(s.Tuning.Unification.UN6.OverlayEntries, 0, s.Tuning.Unification.UN6.OverlayTraces, 0, 0);
                s.Unification.OverlayOrNew(npc.Id, caps);
                b.Overlay = npc.Id;
            }
        }

        public static void Decide(GodState s, EventBuffer ev, Npc npc, UnifiedNpc u, BrainRuntime rt)
        {
            var b = u.Brain;
            if (!b.Started) Init(s, npc, u);
            var t = s.Tuning.Unification.UN6;
            long tick = s.Tick;
            int today = (int)rt.Calendar.Day(tick);
            var obs = rt.Obs;

            rt.Mask = default;
            rt.Body.Sense(s, npc, ref rt.P, obs, ref rt.Mask);

            JudgePrevious(s, ev, npc, u, rt, tick);
            Remember(b, rt, tick);

            for (int c = 0; c < NeedChannels.Count; c++) rt.Now[c] = obs[rt.NeedsOff + c];

            // ---- overlay: credit from need change, then the control biases ----
            var store = b.Overlay != 0 ? s.Unification.OverlayOf(b.Overlay) : null;
            var tr = npc.LearnTraits;
            int nk = 0;
            for (int c = 0; c < rt.Bias.Length; c++) rt.Bias[c] = 0;
            if (store != null)
            {
                if (b.LastDecisionTick >= 0) Learner.AddReward(store, NeedReward(s, b.PrevNeeds, rt.Now, t.CreditComfort));
                Learner.BeginThink(store, rt.OverlayTune, in tr, today, null);
                nk = Keys(obs, rt);
                for (int c = 1; c < rt.Bias.Length; c++) rt.Bias[c] = BiasOf(store, rt.Keys, nk, (byte)c, t);
            }

            if (s.Tuning.Unification.Brain.Planning != 0) rt.PlanExpansions += Planner.Plan(s, b, obs, rt, t);
            else { b.IntentSlot = -1; b.IntentNeed = 0; }

            // ---- decide ----
            var bp = rt.Policy as BrainPolicy;
            if (bp != null)
            {
                bp.Body = rt.Body; bp.State = s; bp.Bias = store != null ? rt.Bias : null;
                bp.Temperature = Temperature(npc, t); bp.Sample = t.Sample != 0;
            }
            if (npc.RngPolicy == null) npc.RngPolicy = s.Rng.Fork("npc", npc.Id);
            var rng = npc.RngPolicy;
            var ctl = rt.Policy.Decide(in rt.P, obs, ref b, in rt.Mask, ref rng);
            ctl.Actor = npc.Id;
            int macs = bp != null ? bp.LastMacs : 0;

            var st = rt.Body.Submit(s, ev, in ctl, rt.Origin);
            rt.Submitted++;
            if (st == MotorStatus.Accepted) rt.Accepted++;

            // ---- overlay: the trace of this decision ----
            if (store != null)
            {
                byte verb = (byte)ctl.Control;
                int q = store.SumStw(rt.Keys, 0, nk, verb, out _);
                Learner.Step(store, rt.OverlayTune, in tr, q, today, null);
                Learner.OpenTrace(store, rt.OverlayTune, in tr, (int)tick, verb, rt.Keys, 0, nk, q, 0, today, null);
                Learner.DecayTraces(store, Learner.TraceShiftOf(rt.OverlayTune, in tr));
            }

            // ---- bookkeeping ----
            b.PrevControl = (byte)ctl.Control;
            b.PrevStatus = (byte)st;
            b.PrevInteraction = 0; b.PrevTargetClass = 0;
            if (ctl.Control == Prim.Use)
            {
                int slot = SlotOf(rt, in ctl.Target);
                int cls = slot >= 0 && slot < ObsSchema.Entities ? obs[rt.EntOff + slot * ObsSchema.EntityFields] : 0;
                b.PrevTargetClass = (ushort)(cls < 0 ? 0 : cls);
                if (st == MotorStatus.Accepted) b.PrevInteraction = (byte)InteractionKindOf(cls, ctl.Slot);
            }
            if (ctl.Control == Prim.Gesture && st == MotorStatus.Accepted && (ctl.Gesture == (byte)GestureAct.Teach || ctl.Gesture == (byte)GestureAct.Demonstrate))
            {
                b.TeachUntil = tick + t.TeachWindow;
                ev?.Add(new TeachingGesture { Actor = npc.Id, Act = ctl.Gesture, Until = b.TeachUntil, Origin = rt.Origin });
            }
            for (int c = 0; c < NeedChannels.Count; c++) b.PrevNeeds[c] = rt.Now[c];
            b.LastDecisionTick = tick;
            b.Decisions++;
            rt.Count(tick, macs);
        }

        /// <summary>The perceived entity slot a target names (-1 when it is not one of the perceived entities).</summary>
        private static int SlotOf(BrainRuntime rt, in TargetRef target)
        {
            for (int e = 0; e < ObsSchema.Entities; e++)
            {
                if (!rt.Body.Entity(in rt.P, e, out var kind, out int id, out _)) break;
                if (kind == target.Kind && id == target.Id) return e;
            }
            return -1;
        }

        /// <summary>Temperature (permille) from temperament: 1000 + (Boldness - 500) x spread / 500, at least 100.</summary>
        public static int Temperature(Npc npc, UN6Tuning t)
        {
            if (t.TemperatureSpread == 0) return 1000;
            int v = 1000 + (npc.Traits[Trait.Boldness] - 500) * t.TemperatureSpread / 500;
            return v < 100 ? 100 : v;
        }

        /// <summary>
        /// The overlay's reward: the change of the twelve need pressures the person feels (its own vector), per active channel the
        /// Creatures drive rule (<see cref="Learner.DriveReward"/>, weight 1000, comfort <paramref name="comfort"/>). Nothing else pays:
        /// no holder, lot, title or transfer term exists (D9.1 d).
        /// </summary>
        public static int NeedReward(GodState s, short[] prev, short[] now, int comfort)
        {
            int r = 0;
            for (int c = 0; c < NeedChannels.Count; c++)
                if (Unified.NeedActive(s, (NeedChannel)c)) r += Learner.DriveReward(1000, comfort, prev[c], now[c]);
            return r;
        }

        /// <summary>The bias of control <paramref name="verb"/> in this context: the store's fast weights, scaled and clamped.</summary>
        public static int BiasOf(AssociationStore st, uint[] keys, int n, byte verb, UN6Tuning t)
        {
            long q = (long)st.SumStw(keys, 0, n, verb, out _) * t.OverlayGain >> 10;
            int cap = t.OverlayBiasCap;
            return (int)(q > cap ? cap : q < -cap ? -cap : q);
        }

        /// <summary>
        /// The overlay's hashed observation features (sorted, at most 5): the band of the two most pressing needs, the last result, the
        /// nearest perceived class, whether something is carried.
        /// </summary>
        public static int Keys(short[] obs, BrainRuntime rt)
        {
            int c1 = -1, p1 = -1, c2 = -1, p2 = -1;
            for (int c = 0; c < NeedChannels.Count; c++)
            {
                int v = obs[rt.NeedsOff + c];
                if (v > p1) { c2 = c1; p2 = p1; c1 = c; p1 = v; }
                else if (v > p2) { c2 = c; p2 = v; }
            }
            var k = rt.Keys;
            int n = 0;
            k[n++] = Feature.DriveBand(c1, Band(p1));
            if (c2 >= 0) k[n++] = Feature.DriveBand(c2, Band(p2));
            k[n++] = Feature.Situation(64 + (obs[rt.LastResultOff] & 63));
            int cls = obs[rt.EntOff];
            if (cls > 0) k[n++] = Feature.Class(cls);
            k[n++] = Feature.Self(obs[rt.CarryOff] != 0 ? 101 : 100);
            for (int i = 1; i < n; i++) { uint x = k[i]; int j = i - 1; while (j >= 0 && k[j] > x) { k[j + 1] = k[j]; j--; } k[j + 1] = x; }
            return n;
        }

        private static int Band(int p) => p < 250 ? 0 : p < 500 ? 1 : p < 750 ? 2 : 3;

        /// <summary>
        /// The interaction kind a USE on class <paramref name="cls"/> with <paramref name="slot"/> names (its first effect, the knowledge
        /// index of <see cref="KnowledgeKind.Interaction"/>); 0 when the catalogue has no such row. Fresh water is DrinkWash.
        /// </summary>
        public static int InteractionKindOf(int cls, int slot)
        {
            if (cls == 2) return (int)EffectKind.DrinkWash;
            if (cls <= 0 || cls > ushort.MaxValue) return 0;
            ReadOnlyArray<InteractionDef> defs = null;
            var o = Catalogue.Object((ushort)cls);
            if (o != null) defs = o.Interactions;
            else { var bd = Catalogue.Building((ushort)cls); if (bd != null) defs = bd.Interactions; }
            if (defs == null) return 0;
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                if (slot != 0 && d.Slot != slot) continue;
                return d.Effects.Count > 0 ? (int)d.Effects[0].Kind : 0;
            }
            return 0;
        }

        /// <summary>The skill group (0-7) of an interaction kind: food, water and relief, raw, construction, craft, combat, care, rest and play.</summary>
        public static int SkillOf(int kind)
        {
            switch ((EffectKind)kind)
            {
                case EffectKind.Forage: case EffectKind.Eat: return 0;
                case EffectKind.DrinkWash: case EffectKind.NeedRelief: case EffectKind.Relieve: return 1;
                case EffectKind.CollectRaw: case EffectKind.Deposit: return 2;
                case EffectKind.PlaceSite: case EffectKind.Contribute: case EffectKind.Labour: case EffectKind.Install: return 3;
                case EffectKind.WorkInput: case EffectKind.RecipeWork: case EffectKind.Fuel: return 4;
                case EffectKind.Strike: case EffectKind.Equip: return 5;
                case EffectKind.Treat: return 6;
                default: return 7;
            }
        }

        /// <summary>
        /// The outcome of the previous submission: a USE accepted at submission whose result is still Accepted now (it completed; a failed
        /// completion overwrites the result) is an own success: its interaction kind becomes known, practice rises, and every person
        /// perceiving the actor may learn it by observation (taught when the actor's teaching window is open).
        /// </summary>
        private static void JudgePrevious(GodState s, EventBuffer ev, Npc npc, UnifiedNpc u, BrainRuntime rt, long tick)
        {
            var b = u.Brain;
            if (b.LastDecisionTick < 0 || b.PrevControl != (byte)Prim.Use || b.PrevStatus != (byte)MotorStatus.Accepted || u.LastResult != MotorStatus.Accepted) return;
            int kind = b.PrevInteraction;
            if (kind <= 0) return;
            var t = s.Tuning.Unification.UN6;
            int g = SkillOf(kind);
            int sk = b.Skill[g] + t.SkillGain;
            b.Skill[g] = (short)(sk > 1000 ? 1000 : sk);
            Brain.Learn(s, ev, npc.Id, KnowledgeKind.Interaction, kind, KnowledgeSource.Own, 0, rt.Origin);
            Brain.Demonstrated(s, ev, npc.Id, KnowledgeKind.Interaction, kind, b.PrevTargetClass, rt.Origin);
        }

        /// <summary>Slot a is a better slot to overwrite than slot b: empty first, then the older (ties: the lower index, kept by the caller).</summary>
        private static bool Older(int aUsed, long aTick, int bUsed, long bTick)
        {
            if ((aUsed == 0) != (bUsed == 0)) return aUsed == 0;
            return aTick < bTick;
        }

        /// <summary>Place and person memory from the perceived entities (nearest first): the nearest of each class, the oldest slot replaced.</summary>
        private static void Remember(BrainState b, BrainRuntime rt, long tick)
        {
            var obs = rt.Obs;
            for (int e = 0; e < ObsSchema.Entities; e++)
            {
                int cls = obs[rt.EntOff + e * ObsSchema.EntityFields];
                if (cls == 0) break;
                if (!rt.Body.Entity(in rt.P, e, out var kind, out int id, out int node)) break;
                if (kind == TargetKind.Npc)
                {
                    int at = -1, old = 0;
                    for (int i = 0; i < b.People.Length; i++)
                    {
                        if (b.People[i].Npc == id) { at = i; break; }
                        if (Older(b.People[i].Npc, b.People[i].Tick, b.People[old].Npc, b.People[old].Tick)) old = i;
                    }
                    if (at < 0) { at = old; b.People[at] = new MemPerson { Npc = id }; }
                    b.People[at].Tick = tick;
                }
                else
                {
                    int at = -1, old = 0;
                    for (int i = 0; i < b.Places.Length; i++)
                    {
                        if (b.Places[i].Class == cls) { at = i; break; }
                        if (Older(b.Places[i].Class, b.Places[i].Tick, b.Places[old].Class, b.Places[old].Tick)) old = i;
                    }
                    if (at >= 0 && b.Places[at].Tick == tick) continue; // a nearer one of this class was kept this decision
                    if (at < 0) at = old;
                    b.Places[at] = new MemPlace { Node = node, Class = (ushort)cls, Tick = tick };
                }
            }
        }
    }
}
