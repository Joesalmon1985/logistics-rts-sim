using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using God.Recording;
using God.Sim;
using Newtonsoft.Json;

namespace God.Runner.Warehouse
{
    /// <summary>How one event type reaches the warehouse: its primary target table, a mapper name for <c>meta_event_map</c> and the code that writes the rows.</summary>
    public sealed class EventMapping
    {
        public Type EventType;
        public string Table;
        public string Mapper;
        public string Note;
        public Action<MapContext, SimEvent> Map;
    }

    /// <summary>
    /// The registration pattern for event to table mapping (DATA_WAREHOUSE_DESIGN 4.5, <c>meta_event_map</c>). A milestone that
    /// adds person, household, transaction, employment or ownership events registers a mapper per event type next to its own
    /// <see cref="TableDef"/>s and migration; nothing else changes. An event with no registered mapper lands in
    /// <c>fact_event</c> (the generic table). One registry per recorder, never shared state.
    /// </summary>
    public sealed class EventMapRegistry
    {
        public const string GenericTable = "fact_event";
        private readonly Dictionary<Type, EventMapping> _explicit = new Dictionary<Type, EventMapping>();
        private static readonly EventMapping GenericMapping = new EventMapping
        {
            Table = GenericTable, Mapper = "generic", Map = (c, e) => c.Generic(e),
        };

        public IEnumerable<EventMapping> Explicit => _explicit.Values.OrderBy(m => m.EventType.Name, StringComparer.Ordinal);

        public EventMapRegistry Register<T>(string table, string mapper, Action<MapContext, T> map, string note = null) where T : SimEvent
        {
            if (_explicit.ContainsKey(typeof(T))) throw new InvalidOperationException("Event type " + typeof(T).Name + " is already registered.");
            _explicit[typeof(T)] = new EventMapping { EventType = typeof(T), Table = table, Mapper = mapper, Note = note, Map = (c, e) => map(c, (T)e) };
            return this;
        }

        /// <summary>The mapping of an event type (the generic one when none is registered).</summary>
        public EventMapping Resolve(Type t) => _explicit.TryGetValue(t, out var m) ? m : GenericMapping;

        public bool IsGeneric(Type t) => !_explicit.ContainsKey(t);

        /// <summary>The mappers of the M13a core, for the events that exist today.</summary>
        public static EventMapRegistry CreateDefault()
        {
            var r = new EventMapRegistry();
            // commands
            r.Register<CommandApplied>("fact_command", "command", (c, e) => c.CommandRow(e.Command, true, null, null));
            r.Register<CommandRejected>("fact_command", "command", (c, e) => c.CommandRow(e.Command, false, e.Result.Reason, e.Result.Message));
            // production
            r.Register<WaresProduced>("fact_production", "production", (c, e) =>
            {
                var b = c.Building(e.BuildingId);
                if (b.Kind == null) b.Kind = e.Kind;
                long date = c.DateKey();
                c.Aggregate(Tables.FactProductionDay, new object[] { date, b.Key, (long)(int)e.Ware, c.FactionKey((int)e.Faction) ?? 0L }, null, e.Count, 1);
                if (c.RowWanted)
                    c.Insert(Tables.FactProduction, c.Tick, c.Seq, date, c.FactionKey((int)e.Faction), b.Key, (long)(int)e.Ware, (long)e.Count, e.Kind, (long)c.Weight);
            });
            // territory and economy events that also fill the building dimension (each also writes its generic fact_event row)
            r.Register<HqPlaced>("fact_event", "dim_building+generic", (c, e) =>
            {
                var b = c.Building(e.HqId); b.IsHq = true; b.Kind = "hq"; b.SizeCode = (int)BuildingSize.Hq; b.SetFaction(c.FactionKey((int)e.Faction), true);
                b.PlacedTick = e.Tick; b.Dirty = true; c.Generic(e);
            }, "also sets dim_building (an HQ)");
            r.Register<BuildingPlaced>("fact_event", "dim_building+generic", (c, e) =>
            {
                var b = c.Building(e.BuildingId); b.SizeCode = (int)e.Size; b.SetFaction(c.FactionKey((int)e.Faction), true);
                b.NodeLocation = c.NodeLocation(e.Node); b.HexLocation = c.HexLocationOfNode(e.Node); b.PlacedTick = e.Tick; b.Dirty = true; c.Generic(e);
            }, "also sets dim_building");
            r.Register<BuildingRemoved>("fact_event", "dim_building+generic", (c, e) => { var b = c.Building(e.BuildingId); b.RemovedTick = e.Tick; b.Dirty = true; c.Generic(e); }, "also closes dim_building");
            r.Register<BuildingCompleted>("fact_event", "dim_building+generic", (c, e) =>
            {
                var b = c.Building(e.BuildingId); b.Kind = e.Kind; b.CompletedTick = e.Tick; b.SetFaction(c.FactionKey((int)e.Faction), false); b.Dirty = true; c.Generic(e);
            }, "also sets dim_building.completed_tick");
            r.Register<BuildingUpgraded>("fact_event", "dim_building+generic", (c, e) => { var b = c.Building(e.BuildingId); b.Kind = e.Kind; b.Level = e.Level; b.Dirty = true; c.Generic(e); }, "also sets dim_building.level");
            r.Register<HqLost>("fact_event", "dim_building+generic", (c, e) => { var b = c.Building(e.HqId); b.DestroyedTick = e.Tick; b.Dirty = true; c.Generic(e); }, "also closes dim_building");
            r.Register<HqCaptured>("fact_event", "dim_building+generic", (c, e) =>
            {
                long? to = c.FactionKey((int)e.To);
                var hq = c.Building(e.HqId); hq.SetFaction(to, false); hq.Dirty = true;
                if (e.BuildingIds != null) foreach (int id in e.BuildingIds) { var b = c.Building(id); b.SetFaction(to, false); b.Dirty = true; }
                c.Generic(e);
            }, "also moves dim_building.faction_key");
            r.Register<FactionEliminated>("fact_event", "dim_faction_history+generic", (c, e) => { c.FactionEliminated((int)e.Faction); c.Generic(e); }, "also closes and opens dim_faction_history");
            // combat
            r.Register<UnitFired>("fact_combat", "combat", (c, e) =>
            {
                long date = c.DateKey(); long? f = c.FactionKey((int)e.Faction);
                c.Aggregate(Tables.FactCombatDay, new object[] { date, f ?? 0L, 1L, (long)(int)e.Outcome }, null, 1, e.Damage);
                if (c.RowWanted)
                    c.Insert(Tables.FactCombat, c.Tick, c.Seq, date, 1L, f, (long)e.AttackerId, (long)(int)e.Kind, (long)(int)e.Weapon, (long)(int)e.TargetKind, (long)e.TargetId,
                        (long)(int)e.Outcome, (long)e.Damage, null, null, null, (long)e.FromX, (long)e.FromZ, (long)c.Weight);
            });
            r.Register<UnitDied>("fact_combat", "combat", (c, e) =>
            {
                long date = c.DateKey(); long? f = c.FactionKey((int)e.Faction); long disb = e.Disbanded ? 1 : 0;
                c.Aggregate(Tables.FactCombatDay, new object[] { date, f ?? 0L, 2L, disb }, null, 1, 0);
                if (c.RowWanted)
                    c.Insert(Tables.FactCombat, c.Tick, c.Seq, date, 2L, f, (long)e.UnitId, (long)(int)e.Kind, null, null, null,
                        disb, null, (long)e.KillerId, disb, null, (long)e.X, (long)e.Z, (long)c.Weight);
            });
            r.Register<BuildingDestroyed>("fact_combat", "combat", (c, e) =>
            {
                long date = c.DateKey(); long? f = c.FactionKey((int)e.Faction); var b = c.Building(e.BuildingId); b.DestroyedTick = e.Tick; b.Dirty = true;
                c.Aggregate(Tables.FactCombatDay, new object[] { date, f ?? 0L, 3L, 0L }, null, 1, 0);
                if (c.RowWanted)
                    c.Insert(Tables.FactCombat, c.Tick, c.Seq, date, 3L, f, null, null, null, null, null, null, null, null, null, b.Key, null, null, (long)c.Weight);
            }, "also closes dim_building");
            PopulationMapping.Register(r);   // M3: person, death, person-day, personal goods, learning; F0: ownership
            HouseholdMappings.Register(r);   // M4 (F0.14 stub)
            SocialMappings.Register(r);      // M5 (F0.14 stub)
            CivMappings.Register(r);         // F1.15: C1-C5, then C6-C10, then D1-D7 (stubs)
            OrgMappings.Register(r);
            DivinityMappings.Register(r);
            return r;
        }
    }

    /// <summary>One in-memory row of <c>dim_building</c> (type 1); written as an upsert on the day it changed.</summary>
    public sealed class BuildingDim
    {
        public long Key; public int Id;
        public long? FactionKey, InitialFactionKey;
        public string Kind; public int? SizeCode; public bool IsHq;
        public long? NodeLocation, HexLocation;
        public long? PlacedTick, CompletedTick, DestroyedTick, RemovedTick; public int? Level;
        public bool Dirty;

        public void SetFaction(long? faction, bool initial)
        {
            FactionKey = faction;
            if (InitialFactionKey == null || initial) InitialFactionKey = faction;
        }
    }

    /// <summary>
    /// What a mapper may do while it handles one event: look up or create dimension rows, insert fact rows, add to daily
    /// aggregates. A mapper reads the event and the context only; it never touches the sim. Rows go into the current day's
    /// batch, which the writer commits as one transaction.
    /// </summary>
    public sealed class MapContext
    {
        private readonly WarehouseRecorder _r;
        internal MapContext(WarehouseRecorder r) { _r = r; }

        public SimEvent Event { get; internal set; }
        public God.Sim.EventInfo Info { get; internal set; }
        public bool RowWanted { get; internal set; }
        /// <summary>The event's domain records at Full (for mappers whose table is written at Full only).</summary>
        public bool Full { get; internal set; }
        public int Weight { get; internal set; }
        public long Tick => Event.Tick;
        public int Seq => Event.Seq;
        public long RunKey => 1;
        public SimCalendar Calendar => _r.Calendar;

        /// <summary>The sim day of the event (day 0 first); a row for it is in <c>dim_date</c> once this has been called.</summary>
        public long DateKey() => _r.EnsureDate(_r.DayOf(Event.Tick));
        public long DateKeyOfTick(long tick) => _r.EnsureDate(_r.DayOf(tick));
        /// <summary>The key of an explicit sim day (NpcDay names the day it describes).</summary>
        public long DateKeyOfDay(long day) => _r.EnsureDate(day);

        /// <summary>The <c>dim_person</c> row of a sim Npc id (created on first sight, so a recorder that starts late still has the person).</summary>
        public PersonDim Person(int npcId) => _r.PersonOf(npcId);

        /// <summary>
        /// F1.15: the <c>dim_god</c> key of a GodId (null for 0). The key is the GodId itself (one run per database, gods fixed at
        /// start and never removed), so a mapper needs no lookup; D1 writes the dim_god rows.
        /// </summary>
        public long? God(int godId) => godId > 0 ? (long?)godId : null;

        /// <summary>F1.15: the <c>dim_org</c> key of an OrgId (null for 0); the key is the OrgId (ids are never reused). C6 writes the rows.</summary>
        public long? Org(int orgId) => orgId > 0 ? (long?)orgId : null;

        /// <summary>The <c>dim_faction</c> key of a sim faction id, or null for an id outside the game.</summary>
        public long? FactionKey(int factionId) => _r.FactionKeyOf(factionId);

        public BuildingDim Building(int buildingId) => _r.BuildingOf(buildingId);

        /// <summary>The <c>dim_party</c> key of an owner (created on first sight), or null for "nobody" (ownerless).</summary>
        public long? Party(God.Sim.Population.OwnerKind kind, int id) => _r.PartyOf(kind, id);
        public long NodeLocation(int node) => _r.NodeLocation(node);
        public long? HexLocationOfNode(int node) => _r.HexLocationOfNode(node);
        public long? HexLocation(int hex) => _r.HexLocation(hex);

        /// <summary>Inserts a fact row. The values are the table's columns after <c>run_key</c>, which is added here.</summary>
        public void Insert(TableDef t, params object[] values)
        {
            var v = new object[values.Length + 1];
            v[0] = 1L; Array.Copy(values, 0, v, 1, values.Length);
            _r.AddRow(t, v);
        }

        /// <summary>
        /// Adds to a daily aggregate row. The row's columns are run_key, then <paramref name="key"/> (identifies the row),
        /// then <paramref name="attrs"/> (last value wins), then <paramref name="sums"/> (added up). Aggregates are taken
        /// before sampling, so they equal the full event stream at every level that writes the table.
        /// </summary>
        public void Aggregate(TableDef t, object[] key, object[] attrs, params long[] sums) => _r.AddAggregate(t, key, attrs, sums);

        public void FactionEliminated(int factionId) => _r.EliminateFaction(factionId, Event.Tick);

        public void CommandRow(Command cmd, bool applied, string reason, string message)
        {
            long date = DateKey();
            long? faction = cmd != null ? FactionKey(cmd.PlayerId) : null;
            Insert(Tables.FactCommand, Tick, Seq, date, applied ? 1L : 0L, cmd?.GetType().Name ?? "", cmd != null ? (object)(long)cmd.PlayerId : null, faction, reason, message, _r.Json.CommandArgs(cmd));
        }

        /// <summary>Writes the event as a <c>fact_event</c> row (when this event's row is wanted): faction, up to four ids, and the whole payload as JSON.</summary>
        public void Generic(SimEvent e)
        {
            if (!RowWanted) return;
            var (faction, ids) = _r.Json.Ids(e, _r);
            Insert(Tables.FactEvent, Tick, Seq, DateKey(), (long)Info.Code, (long)Info.Version, faction, ids[0], ids[1], ids[2], ids[3], _r.Json.Payload(e), (long)Weight);
        }
    }

    /// <summary>Turns an event into the generic row's id columns and JSON payload. Field lists are cached per type; one instance per recorder.</summary>
    internal sealed class PayloadJson
    {
        private readonly Dictionary<Type, FieldInfo[]> _fields = new Dictionary<Type, FieldInfo[]>();
        private readonly JsonSerializer _serializer = JsonSerializer.Create(SaveFile.Settings);

        private FieldInfo[] FieldsOf(Type t)
        {
            if (_fields.TryGetValue(t, out var f)) return f;
            f = t.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(x => x.Name != nameof(SimEvent.Tick) && x.Name != nameof(SimEvent.Seq)).ToArray();
            _fields[t] = f;
            return f;
        }

        public string Payload(SimEvent e)
        {
            var sw = new StringWriter(CultureInfo.InvariantCulture);
            using (var w = new JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                w.WriteStartObject();
                foreach (var f in FieldsOf(e.GetType()))
                {
                    w.WritePropertyName(f.Name);
                    var v = f.GetValue(e);
                    if (v is Command c) WriteCommand(w, c); else _serializer.Serialize(w, v);
                }
                w.WriteEndObject();
            }
            return sw.ToString();
        }

        public string CommandArgs(Command c)
        {
            if (c == null) return null;
            var sw = new StringWriter(CultureInfo.InvariantCulture);
            using (var w = new JsonTextWriter(sw) { Formatting = Formatting.None }) _serializer.Serialize(w, c);
            return sw.ToString();
        }

        private void WriteCommand(JsonTextWriter w, Command c)
        {
            w.WriteStartObject();
            w.WritePropertyName("type"); w.WriteValue(c.GetType().FullName);
            w.WritePropertyName("args"); _serializer.Serialize(w, c);
            w.WriteEndObject();
        }

        /// <summary>The first Faction-typed field as the faction key; the first four other integer-like fields (int, long, enum, bool) as ids.</summary>
        public (object faction, object[] ids) Ids(SimEvent e, WarehouseRecorder r)
        {
            object faction = null; var ids = new object[4]; int n = 0; bool haveFaction = false;
            foreach (var f in FieldsOf(e.GetType()))
            {
                var t = f.FieldType;
                if (t == typeof(Faction) && !haveFaction) { haveFaction = true; faction = r.FactionKeyOf((int)(Faction)f.GetValue(e)); continue; }
                if (n >= 4) continue;
                if (t == typeof(int)) ids[n++] = (long)(int)f.GetValue(e);
                else if (t == typeof(long)) ids[n++] = (long)f.GetValue(e);
                else if (t == typeof(bool)) ids[n++] = (bool)f.GetValue(e) ? 1L : 0L;
                else if (t.IsEnum) ids[n++] = Convert.ToInt64(f.GetValue(e), CultureInfo.InvariantCulture);
            }
            return (faction, ids);
        }
    }
}
