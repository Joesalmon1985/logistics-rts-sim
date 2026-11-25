using System;
using System.Collections.Generic;

namespace God.Sim
{
    public sealed partial class AiBrain
    {
        // ---------------- armies and VP ----------------

        private void LookArmies()
        {
            Array.Clear(_strength, 0, _strength.Length);
            Array.Clear(_vp, 0, _vp.Length);
            _mine.Clear(); _inf = 0; _veh = 0;
            foreach (var u in M.Units)
            {
                _strength[(int)u.Faction] += UnitStrength(u);
                if (u.Faction != Side) continue;
                _mine.Add(u);
                _inf++;
            }
            for (int i = 0; i < S.FactionCount; i++)
                if (!S.Factions.IsEliminated((Faction)i)) _vp[i] = FactionRules.VictoryPoints(S, (Faction)i);
        }

        /// <summary>Stage 3: a soldier's strength grows with its kit (one point per two defence pieces). Q4 refines this.</summary>
        private int UnitStrength(MilUnit u) => Tune.InfantryStrength + MilitaryTuning.DefencePieces(u.Kit) / 2;

        /// <summary>Army strength of a faction (sum over its units), as the AI counts it.</summary>
        public int StrengthOf(Faction f)
        {
            int n = 0;
            foreach (var u in S.Military.Units) if (u.Faction == f) n += UnitStrength(u);
            return n;
        }

        /// <summary>True when one of <paramref name="other"/>'s HQs is within <see cref="AiTuning.NeighbourCornerDistance"/> corners of one of ours.</summary>
        private bool Neighbour(Faction other) => NearestEnemyHq(other, out _) <= Tune.NeighbourCornerDistance;

        private int NearestEnemyHq(Faction other, out Hq best)
        {
            best = null; int bestD = int.MaxValue;
            foreach (var o in T.Hqs)
            {
                if (o.Faction != other || o.Site) continue;
                foreach (var h in _hqs)
                {
                    int d = Topo.CornerDistance(o.Corner, h.Corner);
                    if (d >= 0 && d < bestD) { bestD = d; best = o; }
                }
            }
            return bestD;
        }

        /// <summary>Strength of <paramref name="owner"/>'s units within the defence radius of a building.</summary>
        private int DefenceAt(BuildingRecord target, Faction owner)
        {
            long r2 = (long)Tune.DefenceRadiusMm * Tune.DefenceRadiusMm;
            int n = 0;
            foreach (var u in M.Units)
                if (u.Faction == owner && Pathing.Dist2(u.X, u.Z, target.X, target.Z) <= r2) n += UnitStrength(u);
            return n;
        }

        // ---------------- diplomacy (owner rules) ----------------

        private struct Board6
        {
            public int Top, Second, LeaderCount; public Faction Leader;
        }

        private Board6 Leaders()
        {
            var b = new Board6 { Top = -1, Second = -1 };
            for (int i = 0; i < S.FactionCount; i++)
            {
                if (S.Factions.IsEliminated((Faction)i)) continue;
                int v = _vp[i];
                if (v > b.Top) { b.Second = b.Top; b.Top = v; b.LeaderCount = 1; b.Leader = (Faction)i; }
                else if (v == b.Top) { b.LeaderCount++; b.Second = v; }
                else if (v > b.Second) b.Second = v;
            }
            return b;
        }

        /// <summary>
        /// Losing badly against <paramref name="x"/>: its units at this faction's HQs (within <see cref="AiTuning.ThreatRadiusMm"/>)
        /// are at least <see cref="AiTuning.LosingPercent"/> of this faction's whole army plus a slack. Counted at home, not
        /// over the island, so a coalition against a big leader holds while the leader's army is elsewhere.
        /// </summary>
        private bool Losing(Faction x)
        {
            long r2 = (long)Tune.ThreatRadiusMm * Tune.ThreatRadiusMm;
            int siege = 0;
            foreach (var u in M.Units)
            {
                if (u.Faction != x) continue;
                foreach (var h in _hqs)
                {
                    var rec = M.BuildingById(h.Id);
                    if (rec != null && Pathing.Dist2(u.X, u.Z, rec.X, rec.Z) <= r2) { siege += UnitStrength(u); break; }
                }
            }
            return siege > 0 && siege * 100 >= _strength[(int)Side] * Tune.LosingPercent + Tune.LosingSlack * 100;
        }

        /// <summary>Army strength of this faction plus every other faction at war with <paramref name="x"/>.</summary>
        private int Coalition(Faction x)
        {
            int n = _strength[(int)Side];
            for (int i = 0; i < S.FactionCount; i++)
                if ((Faction)i != Side && (Faction)i != x && S.Factions.AtWar((Faction)i, x)) n += _strength[i];
            return n;
        }

        /// <summary>Owner rule: a sole leader ahead of the second place by <see cref="AiTuning.LeaderMargin"/> VP gets war declared on it.</summary>
        public bool LeaderReason(Faction x, bool keep)
        {
            var b = Leaders();
            if (b.LeaderCount != 1 || b.Leader != x || x == Side) return false;
            return b.Top - b.Second >= (keep ? Tune.LeaderKeepMargin : Tune.LeaderMargin);
        }

        /// <summary>Owner rule: tied leaders fight each other (only them), once the lead is worth something.</summary>
        public bool TieReason(Faction x, bool keep)
        {
            var b = Leaders();
            if (b.Top < Tune.TieWarMinVp || x == Side) return false;
            int me = _vp[(int)Side], him = _vp[(int)x];
            if (!keep) return b.LeaderCount >= 2 && me == b.Top && him == b.Top;
            return me >= b.Top - 1 && him >= b.Top - 1 && Math.Abs(me - him) <= 1;
        }

        /// <summary>Owner rule: a weak neighbour with a capturable HQ near, when the own army is clearly stronger.</summary>
        public bool OpportunistReason(Faction x, bool keep)
        {
            if (x == Side || !Neighbour(x)) return false;
            int me = _strength[(int)Side], him = _strength[(int)x];
            if (keep) return me * 100 >= him * Tune.OpportunistKeepPercent && me * 2 >= Tune.OpportunistMinStrength;
            // The leader does not pick fights (it is the one the others gang up on); it only fights back.
            if (Tune.LeaderStaysOut) { var b = Leaders(); if (_vp[(int)Side] == b.Top) return false; }
            if (me < Tune.OpportunistMinStrength || me * 100 < him * Tune.OpportunistPercent) return false;
            foreach (var o in S.Factions.Enemies(Side)) if (o != x) return false; // one opportunistic front at a time
            NearestEnemyHq(x, out var hq);
            var rec = hq != null ? M.BuildingById(hq.Id) : null;
            if (rec == null) return false;
            return me * 100 >= DefenceAt(rec, x) * Tune.AttackPercent + Tune.AttackMinStrength * 100;
        }

        private void Diplomacy()
        {
            var fs = S.Factions;
            for (int i = 0; i < S.FactionCount; i++)
            {
                var x = (Faction)i;
                if (x == Side || fs.IsEliminated(x)) continue;
                if (S.Setup.Tutorial && S.Setup.HasHuman && x == S.Setup.HumanFaction && !fs.AtWar(Side, x)) continue; // tutorial: no war on the human
                bool losing = Losing(x);
                if (!fs.AtWar(Side, x))
                {
                    // An owner-rule war is joined only when the coalition against x (this faction and everyone already at war
                    // with x) can stand up to it; otherwise a weak faction would declare, be overrun at home and beg for peace.
                    bool ownerDeclare = (LeaderReason(x, false) || TieReason(x, false))
                        && _strength[(int)Side] >= Tune.JoinWarMinStrength
                        && Coalition(x) * 100 >= _strength[(int)x] * Tune.CoalitionPercent;
                    bool declare = ownerDeclare || OpportunistReason(x, false);
                    // One declaration per think: the next think sees the new war (one opportunistic front at a time).
                    if (declare && !losing && Send(new DeclareWar(Side, x), "war on " + FactionInfo.Name(x))) return;
                    continue;
                }
                bool owner = LeaderReason(x, true) || TieReason(x, true);
                bool keep = owner || OpportunistReason(x, true);
                // Accept an offer when it makes sense: no reason left, losing, or an opportunistic war on a second front.
                bool tooManyFronts = !owner && fs.HasOfferedPeace(x, Side) && fs.Enemies(Side).Count >= 2;
                if ((!keep || losing || tooManyFronts) && !fs.HasOfferedPeace(Side, x)
                    && Send(new OfferPeace(Side, x), "peace to " + FactionInfo.Name(x)) && tooManyFronts) return;
            }
        }

        // ---------------- army: guards, targets ----------------

        private void Army()
        {
            var fs = S.Factions; var m = M;
            var enemies = fs.Enemies(Side);

            // Guards on every HQ: inner 1, border 2, facing a war 3, threatened up to 5.
            long threat2 = (long)Tune.ThreatRadiusMm * Tune.ThreatRadiusMm;
            foreach (var h in _hqs)
            {
                var rec = m.BuildingById(h.Id);
                if (rec == null) continue;
                int guards = Tune.GuardsInner;
                foreach (var o in T.Hqs)
                {
                    if (o.Faction == Side || o.Site) continue;
                    int d = Topo.CornerDistance(o.Corner, h.Corner);
                    if (d < 0 || d > Tune.NeighbourCornerDistance) continue;
                    guards = Math.Max(guards, fs.AtWar(Side, o.Faction) ? Tune.GuardsFront : Tune.GuardsBorder);
                }
                int near = 0, nearStr = 0;
                foreach (var u in m.Units)
                    if (fs.AtWar(Side, u.Faction) && Pathing.Dist2(u.X, u.Z, rec.X, rec.Z) <= threat2) { near++; nearStr += UnitStrength(u); }
                if (near > 0) guards = Math.Min(Tune.GuardsThreatMax, Math.Max(guards, Tune.GuardsFront + near / 2));
                guards = Math.Min(guards, MilitaryTuning.MaxGuards);
                var flag = m.DefendAtBuilding(h.Id);
                if (flag == null || flag.Guards != guards) Send(SetDefend.OnBuilding(Side, h.Id, guards), "guards " + guards + " on " + h.Id);
                // Reinforcements: a second defend flag on the HQ's flag node pulls free units home while the HQ is attacked.
                var tb = T.BuildingById(h.Id);
                if (tb == null) continue;
                int reinforce = near == 0 ? 0 : Math.Min(MilitaryTuning.MaxGuards, Math.Max(0, (nearStr - guards * Tune.InfantryStrength + 1) / Tune.InfantryStrength));
                var rf = m.DefendAtNode(Side, tb.FlagNode);
                if (reinforce > 0 && (rf == null || rf.Guards != reinforce)) Send(SetDefend.OnNode(Side, tb.FlagNode, reinforce), "reinforce " + h.Id + " with " + reinforce);
                else if (reinforce == 0 && rf != null) Send(new RemoveDefend(Side, rf.Id), "stand down at " + h.Id);
            }

            if (enemies.Count == 0) return;

            // The free army (not guarding) attacks one target at a time.
            int free = 0;
            foreach (var u in _mine) if (u.GuardFlagId == 0) free += UnitStrength(u);
            var marks = m.MarkedOf(Side);
            BuildingRecord current = marks.Count > 0 ? m.BuildingById(marks[0]) : null;
            if (current != null && fs.AtWar(Side, current.Faction))
            {
                int def = DefenceAt(current, current.Faction);
                if (free < Tune.RetreatMinStrength || free * 100 < def * Tune.RetreatPercent)
                {
                    foreach (int id in new List<int>(marks)) Send(new UnmarkTarget(Side, id), "call off " + id);
                }
                return;
            }

            // Pick the nearest enemy HQ or Citadel (capture brings its VP), then its military buildings.
            BuildingRecord best = null; int bestScore = int.MaxValue, bestDef = 0;
            foreach (var o in T.Hqs)
            {
                if (o.Site || !fs.AtWar(Side, o.Faction)) continue;
                var rec = m.BuildingById(o.Id);
                if (rec == null) continue;
                int dist = int.MaxValue;
                foreach (var h in _hqs) dist = Math.Min(dist, G.Distance(h.Node, o.Node));
                int def = DefenceAt(rec, o.Faction);
                int score = dist + (o.Citadel ? Tune.CitadelTargetPenalty : 0) + def * 3 + rec.Health / 100;
                if (score < bestScore) { bestScore = score; best = rec; bestDef = def; }
            }
            if (best == null) return;
            if (free * 100 < bestDef * Tune.AttackPercent + Tune.AttackMinStrength * 100) return;
            foreach (int id in new List<int>(marks)) Send(new UnmarkTarget(Side, id), "drop mark " + id);
            Send(new MarkTarget(Side, best.Id), "attack hq " + best.Id + " of " + FactionInfo.Name(best.Faction));
            int extra = 0;
            foreach (var b in m.Buildings)
            {
                if (extra >= 2 || b.Faction != best.Faction || b.IsHq) continue;
                if (b.Kind != BuildingKinds.Barracks && b.Kind != BuildingKinds.VehicleWorkshop) continue;
                if (Pathing.Dist2(b.X, b.Z, best.X, best.Z) > 60000L * 60000L) continue;
                if (Send(new MarkTarget(Side, b.Id), "mark " + b.Kind + " " + b.Id)) extra++;
            }
        }
    }
}
