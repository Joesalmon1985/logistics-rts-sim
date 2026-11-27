using System;
using System.Collections.Generic;
using God.Sim;
using God.Sim.Divinity;

namespace God.Game
{
    public enum FxShape : byte { Halo, Chevron, Tether, Rim, Column, Figure, Implode, HealSpiral, Aura, Bolt, Burst, Collapse, Spark, Motes, Dissolve, Ring, Glyph, Thread, Snap, Cross }
    public enum FxColour : byte { Pale, Gold, Violet, Teal, White, Grey, Green, Blue, Red, Amber, Brown, Orange }
    /// <summary>Flash 2 s, Short 8 s, Sustained 30 s, Persistent until a closing event (view-time at 10 ticks per second).</summary>
    public enum FxPersist : byte { Flash, Short, Sustained, Persistent }

    /// <summary>One divine effect (VISUAL_WORLD 9, 11): exactly one per mapped event; plain data.</summary>
    public sealed class FxRecord
    {
        public int Id;
        public long SourceEventId;
        public ushort Code;
        public string Name;
        public FxShape Shape;
        public FxColour Colour;
        public FxPersist Persist;
        public int God, Intervention;
        /// <summary>The person it is on (0 none), the place (-1 none), the thing named by a miracle effect, and goods for create/destroy.</summary>
        public int Npc, Node = -1;
        public byte ThingKind; public int ThingId;
        public Ware Ware; public int Count, Lot;
        public long StartTick;
        public long EndTick;
        /// <summary>True when drawn in full; false when over the budget (a marker only).</summary>
        public bool Detailed = true;
        /// <summary>The persistent record this one closes (0 none).</summary>
        public int Closes;
        public Rgb GodTint;
        public int Sigil;
        public float Intensity = 0.5f;
        public MiracleKind Miracle;
    }

    /// <summary>The god's colour and sigil as a pure function of its id (VISUAL_WORLD 9.3).</summary>
    public static class GodLook
    {
        public const int Sigils = 6;
        public static Rgb Colour(int godId)
        {
            float h = (godId * 0.38196601f + 0.11f) % 1f;
            return Hsv(h, 0.55f, 0.95f);
        }
        public static int Sigil(int godId) => ((godId % Sigils) + Sigils) % Sigils;

        private static Rgb Hsv(float h, float s, float v)
        {
            float r = Math.Abs(h * 6f - 3f) - 1f, g = 2f - Math.Abs(h * 6f - 2f), b = 2f - Math.Abs(h * 6f - 4f);
            r = Math.Max(0f, Math.Min(1f, r)); g = Math.Max(0f, Math.Min(1f, g)); b = Math.Max(0f, Math.Min(1f, b));
            return new Rgb(((r - 1f) * s + 1f) * v, ((g - 1f) * s + 1f) * v, ((b - 1f) * s + 1f) * v);
        }
    }

    /// <summary>
    /// Maps divine events to effect records. Pure, data table keyed by effect name. A record exists only because an event did;
    /// events with no effect (the instruction's own issue rows, the person's response, which animates the person instead)
    /// give none. Persistent effects are closed by their ending event. At most <see cref="MaxDetailed"/> are drawn in full:
    /// the rest are markers. D4 and D6 events that have no type yet enter through <see cref="PlanCode"/>.
    /// </summary>
    public sealed class DivineFxPlanner
    {
        public const int MaxDetailed = 24;

        public struct Look { public FxShape Shape; public FxColour Colour; public FxPersist Persist; public Look(FxShape s, FxColour c, FxPersist p) { Shape = s; Colour = c; Persist = p; } }

        /// <summary>The look table: shape, colour family and persistence per effect (distinct in at least two of the three).</summary>
        public static readonly Dictionary<string, Look> Table = new Dictionary<string, Look>
        {
            ["suggestion"] = new Look(FxShape.Halo, FxColour.Pale, FxPersist.Short),
            ["command"] = new Look(FxShape.Chevron, FxColour.Gold, FxPersist.Sustained),
            ["compulsion"] = new Look(FxShape.Tether, FxColour.Violet, FxPersist.Persistent),
            ["compulsion_end"] = new Look(FxShape.Snap, FxColour.Violet, FxPersist.Flash),
            ["compulsion_blocked"] = new Look(FxShape.Cross, FxColour.Violet, FxPersist.Short),
            ["possession"] = new Look(FxShape.Rim, FxColour.Teal, FxPersist.Persistent),
            ["possession_end"] = new Look(FxShape.Glyph, FxColour.Teal, FxPersist.Flash),
            ["avatar_column"] = new Look(FxShape.Column, FxColour.White, FxPersist.Sustained),
            ["avatar"] = new Look(FxShape.Figure, FxColour.White, FxPersist.Persistent),
            ["avatar_collapse"] = new Look(FxShape.Implode, FxColour.White, FxPersist.Short),
            ["heal"] = new Look(FxShape.HealSpiral, FxColour.Green, FxPersist.Short),
            ["bless"] = new Look(FxShape.Aura, FxColour.Blue, FxPersist.Sustained),
            ["smite"] = new Look(FxShape.Bolt, FxColour.Red, FxPersist.Flash),
            ["miracle"] = new Look(FxShape.Burst, FxColour.Amber, FxPersist.Flash),
            ["collapse"] = new Look(FxShape.Collapse, FxColour.Brown, FxPersist.Short),
            ["spark"] = new Look(FxShape.Spark, FxColour.Red, FxPersist.Short),
            ["create"] = new Look(FxShape.Motes, FxColour.Gold, FxPersist.Short),
            ["destroy"] = new Look(FxShape.Dissolve, FxColour.Grey, FxPersist.Flash),
            ["consecrate"] = new Look(FxShape.Ring, FxColour.Gold, FxPersist.Persistent),
            ["commandment"] = new Look(FxShape.Glyph, FxColour.Amber, FxPersist.Sustained),
            ["commandment_learned"] = new Look(FxShape.Glyph, FxColour.Orange, FxPersist.Short),
            ["prayer"] = new Look(FxShape.Thread, FxColour.Blue, FxPersist.Short),
            ["resurrection"] = new Look(FxShape.Column, FxColour.Orange, FxPersist.Flash),
        };

        /// <summary>Codes of events whose types a later package adds (D4, D6), by effect name.</summary>
        public static readonly Dictionary<ushort, string> CodeTable = new Dictionary<ushort, string>
        {
            [845] = "possession", [846] = "possession_end", [850] = "avatar_column", [851] = "avatar", [852] = "avatar_collapse",
            [860] = "commandment", [863] = "commandment_learned", [870] = "prayer", [880] = "resurrection",
        };

        private readonly List<FxRecord> _live = new List<FxRecord>();
        private readonly Dictionary<long, FxRecord> _open = new Dictionary<long, FxRecord>();
        private readonly Dictionary<int, int> _castNode = new Dictionary<int, int>();
        private int _nextId = 1;

        public static long Key(int a, int b) => ((long)a << 32) | (uint)b;
        public int LiveDetailed(long tick) { Expire(tick); int n = 0; foreach (var r in _live) if (r.Detailed) n++; return n; }
        public void Reset() { _live.Clear(); _open.Clear(); _castNode.Clear(); _nextId = 1; }

        private static int LengthTicks(FxPersist p)
        {
            switch (p) { case FxPersist.Flash: return 20; case FxPersist.Short: return 80; case FxPersist.Sustained: return 300; default: return int.MaxValue; }
        }

        private void Expire(long tick) { _live.RemoveAll(r => r.EndTick <= tick); }

        private FxRecord Make(string name, SimEvent e, ushort code, int god, int intervention)
        {
            Expire(e.Tick);
            var look = Table[name];
            int n = 0; foreach (var l in _live) if (l.Detailed) n++;
            var r = new FxRecord
            {
                Id = _nextId++, SourceEventId = GestureAnimationPlanner.EventKey(e), Code = code, Name = name, Shape = look.Shape, Colour = look.Colour,
                Persist = look.Persist, God = god, Intervention = intervention, StartTick = e.Tick,
                EndTick = look.Persist == FxPersist.Persistent ? long.MaxValue : e.Tick + LengthTicks(look.Persist),
                Detailed = n < MaxDetailed, GodTint = GodLook.Colour(god), Sigil = GodLook.Sigil(god),
            };
            _live.Add(r);
            return r;
        }

        private FxRecord Close(long key, long tick)
        {
            if (!_open.TryGetValue(key, out var o)) return null;
            _open.Remove(key); o.EndTick = tick;
            return o;
        }

        /// <summary>The records for one event (at most one).</summary>
        public List<FxRecord> Plan(SimEvent e)
        {
            var o = new List<FxRecord>(1);
            FxRecord r = null;
            switch (e)
            {
                case CommandPerceived p:
                    if (p.Kind == InterventionKind.Suggestion) r = Make("suggestion", e, 813, p.God, p.Intervention);
                    else if (p.Kind == InterventionKind.Command) r = Make("command", e, 813, p.God, p.Intervention);
                    if (r != null) r.Npc = p.Npc;
                    break;
                case CompulsionStarted c:
                    r = Make("compulsion", e, 815, c.God, c.Intervention); r.Npc = c.Npc; r.Node = c.TargetNode;
                    _open[Key(c.Intervention, c.Npc)] = r;
                    break;
                case CompulsionEnded c:
                    r = Make("compulsion_end", e, 816, c.God, c.Intervention); r.Npc = c.Npc;
                    var closed = Close(Key(c.Intervention, c.Npc), e.Tick);
                    if (closed != null) r.Closes = closed.Id;
                    break;
                case CompulsionBlocked c:
                    r = Make("compulsion_blocked", e, 817, c.God, c.Intervention); r.Npc = c.Npc;
                    break;
                case MiracleCast m:
                    _castNode[m.Intervention] = m.TargetNode;
                    r = Make(NameOf(m.Kind), e, 825, m.God, m.Intervention);
                    r.Miracle = m.Kind; r.Node = m.TargetNode; r.ThingId = m.TargetId; r.ThingKind = (byte)m.TargetKind;
                    if (m.TargetKind == DivineTargetKind.Npc) r.Npc = m.TargetId;
                    break;
                case MiracleEffectApplied a:
                    string nm = a.ThingKind == MiracleThing.Hut || a.ThingKind == MiracleThing.Building || a.ThingKind == MiracleThing.Source ? "collapse" : "spark";
                    r = Make(nm, e, 826, 0, a.Intervention);
                    r.ThingKind = a.ThingKind; r.ThingId = a.ThingId;
                    if (a.ThingKind == MiracleThing.Person) r.Npc = a.ThingId;
                    if (_castNode.TryGetValue(a.Intervention, out int cn)) r.Node = cn;
                    break;
                case ResourceCreated rc:
                    r = Make("create", e, 827, rc.God, rc.Intervention); r.Ware = rc.Ware; r.Count = rc.Count; r.Lot = rc.Lot; r.Node = rc.Node;
                    break;
                case ResourceDestroyed rd:
                    r = Make("destroy", e, 828, rd.God, rd.Intervention); r.Ware = rd.Ware; r.Count = rd.Count; r.Lot = rd.Lot; r.Node = rd.Node;
                    break;
                case LocationConsecrated lc:
                    r = Make("consecrate", e, 829, lc.God, lc.Intervention); r.Node = lc.Node;
                    break;
            }
            if (r != null) o.Add(r);
            return o;
        }

        /// <summary>
        /// The entry for events whose types a later package adds (845/846 possession, 850-852 avatar, 860/863 commandment,
        /// 870 prayer, 880 resurrection): the same table, so one record per event.
        /// </summary>
        public List<FxRecord> PlanCode(ushort code, long tick, int seq, int god, int npc, int node)
        {
            var o = new List<FxRecord>(1);
            if (!CodeTable.TryGetValue(code, out var name)) return o;
            var e = new CodeEvent { Tick = tick, Seq = seq };
            var r = Make(name, e, code, god, 0);
            r.Npc = npc; r.Node = node;
            long key = Key(code == 846 ? 845 : code == 852 ? 851 : code, npc);
            if (name == "possession" || name == "avatar") _open[key] = r;
            else if (name == "possession_end" || name == "avatar_collapse") { var c = Close(key, tick); if (c != null) r.Closes = c.Id; }
            o.Add(r);
            return o;
        }

        private sealed class CodeEvent : SimEvent { }

        private static string NameOf(MiracleKind k)
        {
            switch (k)
            {
                case MiracleKind.HealInjury: case MiracleKind.HealDisease: case MiracleKind.Rejuvenate: return "heal";
                case MiracleKind.Bless: case MiracleKind.Protect: return "bless";
                case MiracleKind.SmitePerson: case MiracleKind.SmiteBuilding: case MiracleKind.SmiteSource: return "smite";
                default: return "miracle";
            }
        }
    }
}
