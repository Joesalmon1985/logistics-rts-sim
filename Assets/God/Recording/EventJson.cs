using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using God.Sim;
using Newtonsoft.Json;

namespace God.Recording
{
    /// <summary>
    /// Turns domain events and snapshot rows into JSON lines (DATA_WAREHOUSE_DESIGN 8, M0): one line per event with the run
    /// id, tick, seq, code, name, version and payload:
    /// <c>{"run":"…","t":341,"s":0,"c":14,"e":"BuildingPlaced","v":1,"p":{…}}</c>. A command event's payload carries the
    /// command's type and fields, serialised on the sim thread at drain time. Field lists are cached per instance, so two
    /// writers on two threads share nothing.
    /// </summary>
    public sealed class EventJson
    {
        private readonly Dictionary<Type, FieldInfo[]> _fields = new Dictionary<Type, FieldInfo[]>();
        private readonly JsonSerializer _serializer = JsonSerializer.Create(SaveFile.Settings);
        private readonly string _runId;

        public EventJson(string runId) { _runId = runId ?? ""; }

        public string Line(SimEvent e)
        {
            var sw = new StringWriter();
            using (var w = new JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                var info = EventCatalog.Of(e.GetType());
                w.WriteStartObject();
                w.WritePropertyName("run"); w.WriteValue(_runId);
                w.WritePropertyName("t"); w.WriteValue(e.Tick);
                w.WritePropertyName("s"); w.WriteValue(e.Seq);
                w.WritePropertyName("c"); w.WriteValue(info.Code);
                w.WritePropertyName("e"); w.WriteValue(info.Name);
                w.WritePropertyName("v"); w.WriteValue(info.Version);
                w.WritePropertyName("d"); w.WriteValue(info.Domain.ToString().ToLowerInvariant());
                w.WritePropertyName("p");
                WritePayload(w, e);
                w.WriteEndObject();
            }
            return sw.ToString();
        }

        private void WritePayload(JsonTextWriter w, SimEvent e)
        {
            w.WriteStartObject();
            foreach (var f in FieldsOf(e.GetType()))
            {
                var v = f.GetValue(e);
                w.WritePropertyName(f.Name);
                if (v is Command c) WriteCommand(w, c);
                else _serializer.Serialize(w, v);
            }
            w.WriteEndObject();
        }

        private void WriteCommand(JsonTextWriter w, Command c)
        {
            w.WriteStartObject();
            w.WritePropertyName("type"); w.WriteValue(c.GetType().FullName);
            w.WritePropertyName("args"); _serializer.Serialize(w, c);
            w.WriteEndObject();
        }

        /// <summary>Public instance fields except Tick and Seq (they are the line's own keys), in declaration order.</summary>
        private FieldInfo[] FieldsOf(Type t)
        {
            if (_fields.TryGetValue(t, out var f)) return f;
            var l = new List<FieldInfo>();
            foreach (var x in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (x.Name != nameof(SimEvent.Tick) && x.Name != nameof(SimEvent.Seq)) l.Add(x);
            f = l.ToArray();
            _fields[t] = f;
            return f;
        }

        /// <summary>A daily faction snapshot as one line: <c>{"run":…,"snap":"faction_day","day":…,"t":…,"p":{…}}</c>.</summary>
        public string SnapshotLine(FactionDayRow r)
        {
            var sw = new StringWriter();
            using (var w = new JsonTextWriter(sw) { Formatting = Formatting.None })
            {
                w.WriteStartObject();
                w.WritePropertyName("run"); w.WriteValue(_runId);
                w.WritePropertyName("snap"); w.WriteValue("faction_day");
                w.WritePropertyName("day"); w.WriteValue(r.Day);
                w.WritePropertyName("t"); w.WriteValue(r.Tick);
                w.WritePropertyName("p"); _serializer.Serialize(w, r);
                w.WriteEndObject();
            }
            return sw.ToString();
        }
    }
}
