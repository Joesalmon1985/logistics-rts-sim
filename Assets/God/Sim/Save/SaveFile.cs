using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace God.Sim
{
    /// <summary>The small header of a save file, enough for a slot list: what, when and how far along.</summary>
    public sealed class SaveHeader
    {
        public ulong Seed;
        public long Tick;
        /// <summary>Human faction id, or <see cref="GameSetup.NoHuman"/> in watch mode.</summary>
        public int Human;
        public int FactionCount;
        /// <summary>When it was saved (ISO 8601, UTC). Filled by the caller: the sim never reads a clock.</summary>
        public string SavedAt;
        /// <summary>Short victory-point line, for example "Amber 3 - Azure 2 - Crimson 1".</summary>
        public string VpLine;
        public bool GameOver;
    }

    /// <summary>Thrown when a save cannot be read: not JSON, wrong version, or missing parts of the state.</summary>
    public sealed class SaveException : Exception
    {
        public SaveException(string message) : base(message) { }
        public SaveException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary>
    /// Anything outside the sim that must survive a save (bots, views' own tables): it names itself with a key and
    /// turns its state into a string and back. The bootstrap collects them into <see cref="SaveFile.Extras"/>.
    /// </summary>
    public interface ISaveExtra
    {
        string SaveKey { get; }
        string SaveState();
        /// <summary>Called after a load with what <see cref="SaveState"/> returned.</summary>
        void LoadState(string data);
    }

    /// <summary>
    /// A whole game on disk: the full <see cref="GodState"/>, the commands queued but not yet applied, and
    /// <see cref="Extras"/> (named strings for systems that live outside the sim, see <see cref="ISaveExtra"/>).
    /// Loading rebuilds a working <see cref="GodSim"/> without dealing again.
    /// </summary>
    public sealed class SaveFile
    {
        /// <summary>
        /// 3 since stage 4 M0: the state carries its <see cref="SimTuning"/>, the setup its calendar (TicksPerDay, DaysPerYear,
        /// LifeScale), disease setting and victory list, and the random streams are keyed. Older saves (2: stage 3; 1: stage 2)
        /// are refused (stage 3 save compatibility is not kept).
        /// </summary>
        public const int CurrentVersion = 3;

        public int Version = CurrentVersion;
        public SaveHeader Header;
        public GodState State;
        /// <summary>Commands accepted but not yet applied (they apply on the next tick, as they would have).</summary>
        public List<PendingCommand> Pending = new List<PendingCommand>();
        public Dictionary<string, string> Extras = new Dictionary<string, string>();

        /// <summary>One queued command: its type name and its public fields as JSON.</summary>
        public sealed class PendingCommand
        {
            public string Type;
            public string Json;
        }

        // ---------------- build ----------------

        /// <summary>Snapshots a sim. <paramref name="savedAt"/> is the caller's clock text (may be null).</summary>
        public static SaveFile From(GodSim sim, string savedAt = null, IEnumerable<ISaveExtra> extras = null)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            var f = new SaveFile { State = sim.State };
            f.Header = MakeHeader(sim.State, savedAt);
            foreach (var c in sim.PendingCommands)
                f.Pending.Add(new PendingCommand { Type = c.GetType().FullName, Json = JsonConvert.SerializeObject(c, Settings) });
            if (extras != null)
                foreach (var x in extras) f.Extras[x.SaveKey] = x.SaveState();
            return f;
        }

        public static SaveHeader MakeHeader(GodState s, string savedAt)
        {
            var h = new SaveHeader
            {
                Seed = s.Seed, Tick = s.Tick, Human = s.Setup?.Human ?? 0, FactionCount = s.FactionCount,
                SavedAt = savedAt, GameOver = s.IsGameOver,
            };
            var parts = new List<string>();
            for (int i = 0; i < s.FactionCount; i++) parts.Add(FactionInfo.Name((Faction)i) + " " + FactionRules.VictoryPoints(s, (Faction)i));
            h.VpLine = string.Join(" - ", parts);
            return h;
        }

        // ---------------- JSON ----------------

        public string ToJson() => JsonConvert.SerializeObject(this, Settings);

        /// <summary>Reads a save. Throws <see cref="SaveException"/> for anything that is not a whole, current save.</summary>
        public static SaveFile FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new SaveException("The save file is empty.");
            SaveFile f;
            try { f = JsonConvert.DeserializeObject<SaveFile>(json, Settings); }
            catch (Exception e) { throw new SaveException("The save file is damaged.", e); }
            if (f == null) throw new SaveException("The save file is empty.");
            if (f.Version < CurrentVersion)
                throw new SaveException("This save is from an older version of the game (save version " + f.Version + ", before stage 4) and cannot be loaded. Start a new game.");
            if (f.Version != CurrentVersion) throw new SaveException("The save file is from another version (" + f.Version + ").");
            var s = f.State;
            if (s == null || s.Board == null || s.Territory == null || s.Economy == null || s.Military == null || s.Factions == null || s.Setup == null || s.Rng == null
                || s.Tuning == null || s.Tuning.Military == null || s.Tuning.Decay == null || s.Setup.Victory == null)
                throw new SaveException("The save file is incomplete.");
            if (s.Setup.TicksPerDay <= 0 || s.Setup.DaysPerYear <= 0 || s.Setup.LifeScale <= 0)
                throw new SaveException("The save file has an impossible calendar.");
            if (s.Board.HexTerrain == null || s.Board.HexNumber == null || s.Territory.NodeBuilding == null || s.Territory.NodeFlag == null
                || s.Territory.NodeRoad == null || s.Territory.Piles == null || s.Economy.PilesReserved == null)
                throw new SaveException("The save file is incomplete.");
            if (s.Territory.NodeBuilding.Length != s.Territory.Grid.Count || s.Board.HexTerrain.Length != s.Board.Topology.HexCount)
                throw new SaveException("The save file does not match the board.");
            if (f.Header == null) f.Header = MakeHeader(s, null);
            if (f.Pending == null) f.Pending = new List<PendingCommand>();
            if (f.Extras == null) f.Extras = new Dictionary<string, string>();
            return f;
        }

        /// <summary>Reads only the header (cheap enough for a slot list). Null when the file is not a save.</summary>
        public static SaveHeader TryReadHeader(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return null;
                var h = JsonConvert.DeserializeObject<HeaderOnly>(json, Settings);
                return h != null && h.Version == CurrentVersion ? h.Header : null;
            }
            catch { return null; }
        }

#pragma warning disable CS0649
        private sealed class HeaderOnly { public int Version; public SaveHeader Header; }
#pragma warning restore CS0649

        /// <summary>Builds the working sim: handlers and systems installed, nothing dealt, queued commands put back.</summary>
        public GodSim ToSim()
        {
            var sim = GodSim.FromState(State);
            foreach (var p in Pending)
            {
                var type = FindCommandType(p.Type);
                if (type == null) throw new SaveException("The save names an unknown command: " + p.Type);
                Command c;
                try { c = (Command)JsonConvert.DeserializeObject(p.Json, type, Settings); }
                catch (Exception e) { throw new SaveException("A queued command is damaged.", e); }
                sim.RestorePending(c);
            }
            return sim;
        }

        private static Type FindCommandType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var t = typeof(Command).Assembly.GetType(name, false);
            return t != null && typeof(Command).IsAssignableFrom(t) && !t.IsAbstract ? t : null;
        }

        // ---------------- settings ----------------

        /// <summary>Public fields only (state is plain data in public fields), arrays and lists replaced, not appended to.</summary>
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new FieldsOnlyResolver(),
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            TypeNameHandling = TypeNameHandling.None,
            Formatting = Formatting.None,
            MissingMemberHandling = MissingMemberHandling.Ignore,
        };

        private sealed class FieldsOnlyResolver : DefaultContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                var l = new List<MemberInfo>();
                foreach (var f in objectType.GetFields(BindingFlags.Public | BindingFlags.Instance)) l.Add(f);
                return l;
            }

            protected override JsonObjectContract CreateObjectContract(Type objectType)
            {
                var c = base.CreateObjectContract(objectType);
                // Commands have constructors that take faction arguments; build them empty and fill the fields.
                if (typeof(Command).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                {
                    c.OverrideCreator = null;
                    c.CreatorParameters.Clear();
                    c.DefaultCreator = () => RuntimeHelpers.GetUninitializedObject(objectType);
                }
                return c;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var p = base.CreateProperty(member, memberSerialization);
                p.Readable = true; p.Writable = true;
                if (member is FieldInfo fi && typeof(IOmitWhenDefault).IsAssignableFrom(fi.FieldType))
                {
                    var type = fi.FieldType;
                    p.ShouldSerialize = holder =>
                    {
                        var v = fi.GetValue(holder);
                        if (v == null) return false;
                        var fresh = Activator.CreateInstance(type);
                        return JsonConvert.SerializeObject(v, Settings) != JsonConvert.SerializeObject(fresh, Settings);
                    };
                }
                return p;
            }
        }
    }
}
