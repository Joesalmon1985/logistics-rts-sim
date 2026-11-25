using System.Collections.Generic;

namespace God.Sim
{
    /// <summary>
    /// The trade tick (after the military): each envoy walks overland (A* over land like a unit) to its target HQ, waits for
    /// the answer there, and walks home with the goods. War declared, a lost target or a lost home HQ change its way (see
    /// the plan's trade rules). Afterwards military units of factions at war with an envoy's owner shoot envoys in range
    /// (unless they are busy with an enemy unit); a killed envoy loses its goods.
    /// </summary>
    public sealed class TradeSystem : ISimSystem
    {
        public void Tick(GodState s, EventBuffer ev)
        {
            var t = s.Trade;
            if (t == null || s.IsGameOver) return;
            foreach (var env in new List<Envoy>(t.Envoys))
            {
                if (t.EnvoyById(env.Id) == null) continue;
                if (env.HitFlash > 0) env.HitFlash--;
                if (s.Factions.IsEliminated(env.Faction)) { TradeRules.KillEnvoy(s, env, -1, EnvoyDeathCause.SenderEliminated, ev); continue; }
                switch (env.Stage)
                {
                    case EnvoyStage.ToTarget: GoToTarget(s, env, ev); break;
                    case EnvoyStage.Waiting: Wait(s, env, ev); break;
                    default: GoHome(s, env, ev); break;
                }
            }
            Shoot(s, ev);
        }

        // ---------------- walking ----------------

        /// <summary>One step towards an HQ. True when the envoy is there. Counts ticks without progress.</summary>
        private static bool Walk(GodState s, Envoy env, EcoBuilding hq)
        {
            int gx = TradeRules.HqX(s, hq), gz = TradeRules.HqZ(s, hq);
            if (Pathing.Dist2(env.X, env.Z, gx, gz) <= (long)TradeTuning.ArriveMm * TradeTuning.ArriveMm) return true;
            int ox = env.X, oz = env.Z;
            Pathing.MoveToward(s.Territory.Grid, env.Body, gx, gz, TradeTuning.EnvoySpeedMmPerTick);
            env.Stuck = env.X == ox && env.Z == oz ? env.Stuck + 1 : 0;
            return Pathing.Dist2(env.X, env.Z, gx, gz) <= (long)TradeTuning.ArriveMm * TradeTuning.ArriveMm;
        }

        private static void GoToTarget(GodState s, Envoy env, EventBuffer ev)
        {
            var o = s.Trade.OfferById(env.Id);
            if (o == null) return;
            if (s.Factions.AtWar(env.Faction, env.Target)) { TradeRules.Resolve(s, o, env, false, RefusalCause.War, ev); return; }
            var hq = Retarget(s, env, o);
            if (hq == null) { TradeRules.Resolve(s, o, env, false, RefusalCause.TargetGone, ev); return; }
            if (Walk(s, env, hq))
            {
                Arrive(s, env, o, hq, ev);
                return;
            }
            if (env.Stuck >= TradeTuning.StuckTicks) TradeRules.Resolve(s, o, env, false, RefusalCause.TargetGone, ev);
        }

        /// <summary>The target HQ the envoy should walk to: its current one if still the target's and finished, else the nearest remaining one (null when none).</summary>
        private static EcoBuilding Retarget(GodState s, Envoy env, TradeOffer o)
        {
            if (s.Factions.IsEliminated(env.Target)) return null;
            if (TradeRules.IsLiveHq(s, env.TargetHq, env.Target)) return s.Economy.Find(env.TargetHq);
            var hq = TradeRules.NearestHq(s, env.Target, env.X, env.Z);
            if (hq == null) return null;
            env.TargetHq = hq.Id; o.TargetHq = hq.Id; env.Stuck = 0;
            env.Body.Path = null; env.Body.PathGoalNode = -1;
            return hq;
        }

        private static void Arrive(GodState s, Envoy env, TradeOffer o, EcoBuilding hq, EventBuffer ev)
        {
            env.Stage = EnvoyStage.Waiting; env.Stuck = 0;
            o.Stage = OfferStage.Pending; o.ArrivedTick = s.Tick; o.TicksLeft = TradeTuning.AnswerTicks; o.TargetHq = hq.Id;
            ev.Add(new EnvoyArrived { OfferId = o.Id, EnvoyId = env.Id, From = o.From, To = o.To, HqId = hq.Id, Give = o.Give, Want = o.Want });
            bool human = s.Setup != null && s.Setup.HasHuman && s.Setup.Human == (int)o.To;
            if (human) return;
            var verdict = (s.Trade.Judge ?? new DefaultTradeJudge()).Decide(s, o);
            if (verdict == TradeVerdict.Wait) return;
            TradeRules.Answer(s, o.Id, verdict == TradeVerdict.Accept, ev);
        }

        private static void Wait(GodState s, Envoy env, EventBuffer ev)
        {
            var o = s.Trade.OfferById(env.Id);
            if (o == null) return;
            if (s.Factions.AtWar(env.Faction, env.Target)) { TradeRules.Resolve(s, o, env, false, RefusalCause.War, ev); return; }
            if (s.Factions.IsEliminated(env.Target)) { TradeRules.Resolve(s, o, env, false, RefusalCause.TargetGone, ev); return; }
            if (!TradeRules.IsLiveHq(s, env.TargetHq, env.Target))
            {
                // The HQ fell: on to the target's next one, or home.
                var hq = Retarget(s, env, o);
                if (hq == null) { TradeRules.Resolve(s, o, env, false, RefusalCause.TargetGone, ev); return; }
                env.Stage = EnvoyStage.ToTarget; o.Stage = OfferStage.InFlight; o.TicksLeft = 0;
                return;
            }
            if (--o.TicksLeft <= 0) TradeRules.Resolve(s, o, env, false, RefusalCause.Timeout, ev);
        }

        private static void GoHome(GodState s, Envoy env, EventBuffer ev)
        {
            var home = s.Economy.Find(env.HomeHq);
            if (home == null || !home.IsHq || !home.Built || home.Faction != env.Faction)
            {
                home = TradeRules.NearestHq(s, env.Faction, env.X, env.Z);
                if (home == null) { TradeRules.KillEnvoy(s, env, -1, EnvoyDeathCause.NoHomeLeft, ev); return; }
                env.HomeHq = home.Id; env.Stuck = 0;
                env.Body.Path = null; env.Body.PathGoalNode = -1;
            }
            if (Walk(s, env, home))
            {
                var cargo = env.CargoList();
                var t = s.Trade;
                for (int w = 1; w < Wares.Count; w++)
                {
                    int n = env.Cargo[w];
                    if (n == 0) continue;
                    home.Stock[w] += n; t.Arrived[w] += n; env.Cargo[w] = 0;
                }
                t.Envoys.Remove(env);
                ev.Add(new EnvoyHome { EnvoyId = env.Id, OfferId = env.Id, Faction = env.Faction, HqId = home.Id, Cargo = cargo.ToArray(), Payment = env.CarriesPayment });
                return;
            }
            if (env.Stuck >= TradeTuning.StuckTicks) TradeRules.KillEnvoy(s, env, -1, EnvoyDeathCause.NoHomeLeft, ev);
        }

        // ---------------- units shoot envoys ----------------

        /// <summary>
        /// Every unit that is ready to fire and not engaged with an enemy unit shoots the nearest envoy of a faction it is at
        /// war with within its range (ties: lowest envoy id). Envoys rank below units. A soldier reaches an envoy from its weapon range or <see cref="MilitaryTuning.EnvoyReachMm"/>, whichever is longer, and does its weapon's damage (no rolls: an envoy does not fight back).
        /// </summary>
        private static void Shoot(GodState s, EventBuffer ev)
        {
            var t = s.Trade; var m = s.Military; var fs = s.Factions;
            if (m == null || t.Envoys.Count == 0) return;
            List<(Envoy env, Faction by)> dead = null;
            foreach (var u in m.Units)
            {
                if (u.Health <= 0 || u.Cooldown > 0 || u.TargetKind == TargetKind.Unit) continue;
                var st = MilitaryTuning.Of(u.Kit);
                long reach = System.Math.Max(st.RangeMm, MilitaryTuning.EnvoyReachMm);
                long r2 = reach * reach;
                Envoy pick = null; long bd = long.MaxValue;
                foreach (var env in t.Envoys)
                {
                    if (env.Health <= 0 || !fs.AtWar(u.Faction, env.Faction)) continue;
                    long d = Pathing.Dist2(u.X, u.Z, env.X, env.Z);
                    if (d <= r2 && d < bd) { bd = d; pick = env; }
                }
                if (pick == null) continue;
                int dmg = st.W.Damage;
                u.Cooldown = st.CooldownTicks; u.FireFlash = MilitaryTuning.FireFlashTicks; u.LastOutcome = AttackOutcome.Hit; u.LastDamage = dmg;
                if (pick.X != u.X || pick.Z != u.Z) { u.FaceX = pick.X - u.X; u.FaceZ = pick.Z - u.Z; }
                pick.Health -= dmg; pick.HitFlash = MilitaryTuning.FireFlashTicks;
                if (pick.Health <= 0)
                {
                    if (dead == null) dead = new List<(Envoy, Faction)>();
                    dead.Add((pick, u.Faction));
                }
            }
            if (dead == null) return;
            dead.Sort((a, b) => a.env.Id.CompareTo(b.env.Id));
            foreach (var (env, by) in dead) TradeRules.KillEnvoy(s, env, (int)by, EnvoyDeathCause.Shot, ev);
        }
    }

    public static class TradeModule
    {
        /// <summary>Creates the trade state.</summary>
        public static void StartGame(GodState s) { s.Trade = new TradeState(); }

        /// <summary>Registers the trade commands and the trade system.</summary>
        public static void Install(GodSim sim)
        {
            sim.RegisterHandler(new OfferTradeHandler());
            sim.RegisterHandler(new AnswerTradeHandler());
            sim.AddSystem(TradeTuning.SystemOrder, new TradeSystem());
        }

        /// <summary>Trade-state hash: envoys, offers, the ledger and the history count.</summary>
        public static ulong Hash(GodState s)
        {
            ulong h = 14695981039346656037UL;
            void Mix(long v) { unchecked { for (int i = 0; i < 8; i++) { h ^= (ulong)((v >> (8 * i)) & 0xFF); h *= 1099511628211UL; } } }
            var t = s.Trade;
            if (t == null) return h;
            Mix(t.NextId);
            foreach (var e in t.Envoys)
            {
                Mix(e.Id); Mix((int)e.Faction); Mix((int)e.Target); Mix((int)e.Stage); Mix(e.Health); Mix(e.HomeHq); Mix(e.TargetHq);
                Mix(e.CarriesPayment ? 1 : 0); Mix(e.X); Mix(e.Z); Mix(e.Body.FaceX); Mix(e.Body.FaceZ); Mix(e.Stuck);
                foreach (int c in e.Cargo) Mix(c);
            }
            Mix(-1);
            foreach (var o in t.Offers)
            {
                Mix(o.Id); Mix((int)o.Stage); Mix(o.SentTick); Mix(o.ArrivedTick); Mix(o.TicksLeft); Mix(o.TargetHq);
                foreach (var l in o.Give) { Mix((int)l.Ware); Mix(l.Count); }
                Mix(-2);
                foreach (var l in o.Want) { Mix((int)l.Ware); Mix(l.Count); }
                Mix(-3);
            }
            Mix(-4);
            foreach (int v in t.Departed) Mix(v);
            foreach (int v in t.Arrived) Mix(v);
            foreach (int v in t.Lost) Mix(v);
            Mix(t.Accepted); Mix(t.Refused); Mix(t.Killed); Mix(t.History.Count);
            foreach (var r in t.History) { Mix(r.OfferId); Mix((int)r.Outcome); Mix((int)r.Cause); Mix(r.Tick); }
            return h;
        }
    }
}
