using God.Sim.Learning;

namespace God.Sim.Unified
{
    /// <summary>What the inspection panel and every report may say about the brain of a game (UN10a draws it; no view work here).</summary>
    public struct BrainBadge
    {
        public int Kind;
        /// <summary>"UNTRAINED", "TRAINED", "FALLBACK" or "ARTIFACT vN STATUS".</summary>
        public string Label;
        public int ArtifactVersion;
        public string WeightsSha256;
        /// <summary>True only for Kind 1 over a verified artifact whose status is "TRAINED". A fallback or untrained run can never set it.</summary>
        public bool Trained;
    }

    /// <summary>
    /// UN6's public API (frozen at UN6 merge): claims (contracts 23.9), knowledge acquisition (§D9.2 d), the brain badge (TR-01 honesty).
    /// Claims and knowledge are a person's beliefs: no function here returns a canonical owner or the truth of a claim.
    /// </summary>
    public static class Brain
    {
        // ---------------------------------------------------------------- claims (23.9)

        /// <summary>
        /// Adds a claim to <paramref name="npc"/>'s ring of <see cref="BrainState.ClaimSlots"/>. The same claimant, subject and source
        /// replace their older claim (refreshed); otherwise a free or expired slot is used, else the slot with the lowest confidence, then
        /// the oldest, then the lowest claimant id, then the lowest slot is evicted. Contradictory claims (different claimants of one
        /// subject, or different confidences from different sources) coexist. False when the person is unknown or the claim names no
        /// subject.
        /// </summary>
        public static bool AddClaim(GodState s, int npc, in Claim c)
        {
            var u = s?.Unification?.PersonOf(npc);
            if (u == null || c.SubjectKind == TargetKind.None) return false;
            var r = u.Brain.Claims;
            long now = s.Tick;
            int at = -1;
            for (int i = 0; i < r.Length; i++)
                if (Used(r[i]) && r[i].Claimant == c.Claimant && r[i].SubjectKind == c.SubjectKind && r[i].SubjectId == c.SubjectId && r[i].Source == c.Source) { at = i; break; }
            if (at < 0) for (int i = 0; i < r.Length; i++) if (!Live(r[i], now)) { at = i; break; }
            if (at < 0)
            {
                at = 0;
                for (int i = 1; i < r.Length; i++) if (Evicts(r[i], r[at])) at = i;
            }
            r[at] = c;
            u.Brain.ClaimHead = (byte)((at + 1) % r.Length);
            return true;
        }

        private static bool Used(in Claim c) => c.SubjectKind != TargetKind.None;
        private static bool Live(in Claim c, long now) => Used(c) && (c.Expiry <= 0 || c.Expiry > now);

        /// <summary>True when <paramref name="a"/> is evicted before <paramref name="b"/>.</summary>
        private static bool Evicts(in Claim a, in Claim b)
        {
            if (a.Confidence != b.Confidence) return a.Confidence < b.Confidence;
            if (a.Tick != b.Tick) return a.Tick < b.Tick;
            return a.Claimant < b.Claimant;
        }

        /// <summary>The person's live claims (a copy, in slot order); empty for an unknown person. For readers (UN7, UN11a), not hot paths.</summary>
        public static Claim[] Claims(GodState s, int npc)
        {
            var into = new Claim[BrainState.ClaimSlots];
            int n = CopyClaims(s, npc, into);
            var r = new Claim[n];
            for (int i = 0; i < n; i++) r[i] = into[i];
            return r;
        }

        /// <summary>The person's live claims into <paramref name="into"/> (slot order); returns how many (no allocation).</summary>
        public static int CopyClaims(GodState s, int npc, Claim[] into)
        {
            var u = s?.Unification?.PersonOf(npc);
            if (u == null) return 0;
            int n = 0;
            foreach (var c in u.Brain.Claims) if (Live(c, s.Tick) && n < into.Length) into[n++] = c;
            return n;
        }

        // ---------------------------------------------------------------- knowledge (§D9.2 d)

        /// <summary>
        /// <paramref name="npc"/> comes to know <paramref name="what"/>/<paramref name="index"/> (own success, observation, teaching, or
        /// the start configuration: UN5). Emits <c>KnowledgeAcquired</c> (1321) when the bit was new. Knowledge gates action only through
        /// the legality mask (a design not known is <see cref="MotorStatus.NotKnown"/>).
        /// </summary>
        public static bool Learn(GodState s, EventBuffer ev, int npc, KnowledgeKind what, int index, KnowledgeSource source, int from, ControlOrigin origin)
        {
            var u = s?.Unification?.PersonOf(npc);
            if (u == null || Knowledge.Knows(u.Brain, what, index)) return false;
            if (!Knowledge.Learn(u.Brain, what, index)) return false;
            ev?.Add(new KnowledgeAcquired { Npc = npc, What = what, Index = index, Source = source, From = from, Origin = origin });
            return true;
        }

        /// <summary>
        /// <paramref name="actor"/> just completed <paramref name="what"/>/<paramref name="index"/> (an interaction kind, a recipe, a
        /// building design; UN3b and UN4 may call this for recipes and buildings). Every awake person who perceives the actor (within
        /// <c>UN6.ObserveRange</c>, through the body's spatial query) and does not know it may learn it: chance (permille) =
        /// <c>ObserveChance</c> x (range + 1 - distance) / (range + 1) x attention / 1000, attention = the observer's curiosity
        /// (500-1500), times <c>TeachBoost</c> / 1000 while the actor's teaching window is open; drawn from a stream keyed by observer,
        /// tick and actor. A taught observer's overlay also gets an imitation bias on (target class, USE) with origin Imitated (learned)
        /// or Vicarious (not learned). Nothing is copied from the actor's memory (R83). Returns how many learned it.
        /// </summary>
        public static int Demonstrated(GodState s, EventBuffer ev, int actor, KnowledgeKind what, int index, int targetClass, ControlOrigin origin)
        {
            var rt = s?.Unification?.BrainRuntime;
            var ua = s?.Unification?.PersonOf(actor);
            if (rt == null || ua == null || !rt.Body.Available(s)) return 0;
            var t = s.Tuning.Unification.UN6;
            bool taught = ua.Brain.TeachUntil >= s.Tick;
            int n = rt.Body.PersonsNear(s, actor, t.ObserveRange, rt.Near, rt.NearDist), learned = 0;
            for (int i = 0; i < n; i++)
            {
                int o = rt.Near[i], d = rt.NearDist[i];
                if (o == actor || d > t.ObserveRange) continue;
                var on = s.Population.Find(o);
                var uo = s.Unification.PersonOf(o);
                if (on == null || uo == null || !on.Alive || on.Action.Asleep) continue;
                if (!uo.Brain.Started) BrainDecider.Init(s, on, uo);
                if (Knowledge.Knows(uo.Brain, what, index)) continue;
                long chance = (long)t.ObserveChance * (t.ObserveRange + 1 - d) / (t.ObserveRange + 1) * on.Curiosity / 1000;
                if (taught) chance = chance * t.TeachBoost / 1000;
                if (chance > 1000) chance = 1000;
                rt.Draw.SetToFork(s.Rng, "un6.observe", o, s.Tick * 4096 + (actor & 4095));
                bool ok = rt.Draw.NextInt(1000) < chance;
                ev?.Add(new DemonstrationObserved { Observer = o, Actor = actor, What = what, Index = index, Distance = d, Taught = taught, Chance = (int)chance, Learned = ok, Origin = origin });
                if (ok && Learn(s, ev, o, what, index, taught ? KnowledgeSource.Taught : KnowledgeSource.Observed, actor, origin)) learned++;
                if (taught && targetClass > 0) Imitate(s, uo, targetClass, ok ? Origin.Imitated : Origin.Vicarious, t);
            }
            return learned;
        }

        /// <summary>Adds the imitation bias to (class key, USE) in the observer's own overlay store (bounded; evicts when full).</summary>
        private static void Imitate(GodState s, UnifiedNpc uo, int targetClass, Origin origin, UN6Tuning t)
        {
            var st = uo.Brain.Overlay != 0 ? s.Unification.OverlayOf(uo.Brain.Overlay) : null;
            if (st == null) return;
            uint key = Feature.Class(targetClass);
            byte verb = (byte)Prim.Use;
            ushort day = (ushort)SimCalendar.Of(s).Day(s.Tick);
            int at = st.Find(key, verb);
            if (at < 0)
            {
                if (st.Count >= st.EntryCap) { Learner.EvictOne(st, day); at = st.Find(key, verb); }
                if (at < 0) at = st.InsertAt(~at, key, verb, day, origin);
            }
            ref var e = ref st.Entries[at];
            e.Stw = Fx.Clamp16(e.Stw + t.ImitationBias);
            e.LastDay = day;
            if (e.Origin == Origin.Own) e.Flags = (byte)((e.Flags & ~3) | (int)origin);
        }

        /// <summary>Injects a policy into this game's decision site (see <see cref="BrainRuntime.Inject"/>); false when the game is not unified.</summary>
        public static bool InjectPolicy(GodSim sim, IPrimitivePolicy policy, ControlOrigin origin)
        {
            var rt = sim?.State?.Unification?.BrainRuntime;
            if (rt == null) return false;
            rt.Inject(policy, origin);
            return true;
        }

        // ---------------------------------------------------------------- the badge (TR-01 honesty)

        /// <summary>The brain of this game, as every report and the HUD must name it.</summary>
        public static BrainBadge Badge(GodState s)
        {
            var rec = s?.Unification?.Brain;
            if (rec == null) return new BrainBadge { Kind = -1, Label = "NONE", WeightsSha256 = "" };
            var rt = s.Unification.BrainRuntime;
            bool trained = rec.Kind == BrainKind.Trained && rt != null && rt.Artifact != null && rt.Artifact.StatusIsTrained
                && rt.Policy is TrainedPolicy && !rt.Injected && ArtifactLoader.Verify(rt.Artifact, ArtifactLoader.CurrentCatalogueHash()) == null;
            return new BrainBadge { Kind = rec.Kind, Label = rec.Label, ArtifactVersion = rec.ArtifactVersion, WeightsSha256 = rec.WeightsSha256, Trained = trained };
        }

        /// <summary>The one-line report string ("Brain: FALLBACK (scripted safety controller; not trained)").</summary>
        public static string Report(GodState s)
        {
            var rt = s?.Unification?.BrainRuntime;
            string inj = rt != null && rt.Injected ? "; decisions by an injected policy (origin " + rt.Origin + "), not the game's brain" : "";
            return ReportOf(Badge(s)) + inj;
        }

        private static string ReportOf(BrainBadge b)
        {
            switch (b.Kind)
            {
                case BrainKind.Untrained: return "Brain: UNTRAINED (random initial weights, seed-fixed; not trained) weights " + b.WeightsSha256;
                case BrainKind.Fallback: return "Brain: FALLBACK (scripted safety controller; not trained)";
                case BrainKind.Trained: return "Brain: " + b.Label + " (artifact v" + b.ArtifactVersion + ", weights " + b.WeightsSha256 + ")" + (b.Trained ? "" : "; not counted as trained");
                default: return "Brain: none (not a unified game)";
            }
        }
    }
}
