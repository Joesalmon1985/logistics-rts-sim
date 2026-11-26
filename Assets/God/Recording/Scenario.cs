using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using God.Sim;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace God.Recording
{
    /// <summary>Thrown for an invalid scenario file or override (runner exit code 3).</summary>
    public sealed class ScenarioException : Exception
    {
        public ScenarioException(string message) : base(message) { }
    }

    /// <summary>A command scheduled by a scenario: applied as if a player sent it before the given tick.</summary>
    public sealed class ScheduledCommand
    {
        public long Tick;
        public string Type;
        public JObject Args;
    }

    /// <summary>A stop or fail condition on a metric.</summary>
    public sealed class MetricCondition
    {
        public string Metric;
        public long? Above;
        public long? Below;
        public bool Holds(long v) => (Above.HasValue && v > Above.Value) || (Below.HasValue && v < Below.Value);
    }

    /// <summary>
    /// A scenario file, version 1 (HEADLESS_SIMULATION 3.2): everything except the seed that decides a run. Unknown keys
    /// are refused. M0 implements setup (faction count, human, calendar, life scale, victory list, disease, information),
    /// tuning overrides, the command schedule, record, metrics, checkpoints, periodic saves, stop conditions and checks.
    /// Blocks of later milestones (policies other than the stage 3 AI, population) are refused with a clear message.
    /// </summary>
    public sealed class Scenario
    {
        public const int CurrentVersion = 1;

        public string Name = "adhoc";
        public string Description = "";
        // setup
        public int FactionCount = FactionInfo.Max;
        public int Human = GameSetup.NoHuman;
        public bool Tutorial;
        public int LifeScale = SimCalendar.DefaultLifeScale;
        public int TicksPerDay = SimCalendar.DefaultTicksPerDay;
        public int DaysPerYear = SimCalendar.DefaultDaysPerYear;
        public bool DecayFollowsLifeScale;
        public DiseaseSetting Disease = DiseaseSetting.On;
        public bool FullInformation = true;
        public List<VictorySpec> Victory = VictorySpec.Stage3Default();
        /// <summary>The faction policy of every non-human faction: "AiBrain" (the stage 3 AI) or "none" (idle).</summary>
        public string FactionPolicy = SimBuilder.StageThreeAi;
        // the rest
        public SortedDictionary<string, long> Tuning = new SortedDictionary<string, long>(StringComparer.Ordinal);
        public List<ScheduledCommand> Commands = new List<ScheduledCommand>();
        public RecordingLevel RecordLevel = RecordingLevel.Off;
        public SortedDictionary<string, RecordingLevel> RecordDomains = new SortedDictionary<string, RecordingLevel>(StringComparer.Ordinal);
        public int SampleEvery = 16;
        public List<string> Metrics = new List<string>();
        /// <summary>Ticks between hash checkpoints; 0 means one sim day.</summary>
        public long CheckpointEvery;
        public int SaveEveryDays;
        public long MaxTicks;
        public long MaxDays;
        public long MaxYears;
        public bool StopOnGameOver = true;
        public long WallSeconds;
        public List<MetricCondition> StopWhen = new List<MetricCondition>();
        public List<MetricCondition> Fail = new List<MetricCondition>();
        public List<string> Checks = new List<string>();

        public long EffectiveCheckpointEvery => CheckpointEvery > 0 ? CheckpointEvery : TicksPerDay;

        /// <summary>The length limit in ticks (the smallest of the limits given; 0 = none).</summary>
        public long LimitTicks
        {
            get
            {
                long best = 0;
                void Take(long t) { if (t > 0 && (best == 0 || t < best)) best = t; }
                Take(MaxTicks);
                Take(MaxDays * TicksPerDay);
                Take(MaxYears * TicksPerDay * DaysPerYear);
                return best;
            }
        }

        public GameSetup ToSetup(ulong seed) => new GameSetup(seed, Human, FactionCount)
        {
            Tutorial = Tutorial, LifeScale = LifeScale, TicksPerDay = TicksPerDay, DaysPerYear = DaysPerYear,
            DecayFollowsLifeScale = DecayFollowsLifeScale, Disease = Disease, FullInformation = FullInformation,
            Victory = Victory.Select(v => new VictorySpec(v.Kind, v.Target)).ToList(),
        };

        public SimTuning ToTuning()
        {
            var t = new SimTuning();
            foreach (var kv in Tuning)
                if (!t.TrySet(kv.Key, kv.Value, out var err)) throw new ScenarioException(err);
            return t;
        }

        /// <summary>The scheduled commands as sim commands, in file order (validated: known type, known fields).</summary>
        public List<(long tick, Command cmd)> BuildCommands()
        {
            var l = new List<(long, Command)>();
            foreach (var c in Commands) l.Add((c.Tick, MakeCommand(c)));
            return l;
        }

        public static Command MakeCommand(ScheduledCommand c)
        {
            var type = FindCommandType(c.Type) ?? throw new ScenarioException("commands: unknown command type '" + c.Type + "'");
            var fields = new HashSet<string>(type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name), StringComparer.Ordinal);
            var args = c.Args ?? new JObject();
            foreach (var p in args.Properties())
                if (!fields.Contains(p.Name)) throw new ScenarioException("commands: " + type.Name + " has no field '" + p.Name + "' (fields: " + string.Join(", ", fields.OrderBy(x => x)) + ")");
            try { return (Command)JsonConvert.DeserializeObject(args.ToString(Formatting.None), type, SaveFile.Settings); }
            catch (Exception e) { throw new ScenarioException("commands: bad arguments for " + type.Name + ": " + e.Message); }
        }

        public static Type FindCommandType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            foreach (var t in typeof(Command).Assembly.GetTypes())
                if (typeof(Command).IsAssignableFrom(t) && !t.IsAbstract && (t.Name == name || t.FullName == name)) return t;
            return null;
        }

        // ---------------- parsing ----------------

        private static readonly string[] TopKeys = { "scenarioVersion", "name", "description", "setup", "tuning", "policies", "commands", "record", "metrics", "checkpointEvery", "saveEveryDays", "stop", "checks" };
        private static readonly string[] SetupKeys = { "factionCount", "human", "tutorial", "population", "startNpcsPerFaction", "lifeScale", "ticksPerDay", "daysPerYear", "decayFollowsLifeScale", "disease", "fullInformation", "victory" };
        private static readonly string[] StopKeys = { "maxTicks", "maxDays", "maxYears", "onGameOver", "populationBelow", "wallSeconds", "when", "fail" };
        private static readonly string[] RecordKeys = { "level", "domains", "sampleEvery" };
        public static readonly string[] KnownChecks = { "invariants", "no-exceptions", "smoke" };

        public static Scenario Parse(string json, IEnumerable<string> overrides = null)
        {
            JObject o;
            try { o = JObject.Parse(json); }
            catch (Exception e) { throw new ScenarioException("not a JSON object: " + e.Message); }
            if (overrides != null) foreach (var s in overrides) ApplyOverride(o, s);
            return FromJson(o);
        }

        /// <summary>Applies <c>path=value</c> (the <c>--set</c> form): <c>tuning.Military.CritPct=12</c>, <c>setup.lifeScale=50</c>, <c>stop.maxYears=2</c>.</summary>
        public static void ApplyOverride(JObject o, string assignment)
        {
            int eq = assignment?.IndexOf('=') ?? -1;
            if (eq <= 0) throw new ScenarioException("--set needs path=value: " + assignment);
            string path = assignment.Substring(0, eq).Trim(), raw = assignment.Substring(eq + 1).Trim();
            if (path.IndexOf("seed", StringComparison.OrdinalIgnoreCase) >= 0) throw new ScenarioException("--set may not touch seeds: " + path);
            JToken value;
            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long n)) value = n;
            else if (raw == "true" || raw == "false") value = raw == "true";
            else if (raw.StartsWith("[") || raw.StartsWith("{")) { try { value = JToken.Parse(raw); } catch (Exception e) { throw new ScenarioException("--set " + path + ": " + e.Message); } }
            else value = raw;
            if (path.StartsWith("tuning.", StringComparison.Ordinal))
            {
                if (!(o["tuning"] is JObject t)) o["tuning"] = t = new JObject();
                t[path.Substring("tuning.".Length)] = value;
                return;
            }
            var parts = path.Split('.');
            JObject cur = o;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!(cur[parts[i]] is JObject next)) cur[parts[i]] = next = new JObject();
                cur = next;
            }
            cur[parts[parts.Length - 1]] = value;
        }

        public static Scenario FromJson(JObject o)
        {
            var sc = new Scenario();
            Only(o, TopKeys, "");
            long version = Int(o, "scenarioVersion", "", 0);
            if (version != CurrentVersion) throw new ScenarioException("scenarioVersion must be " + CurrentVersion + " (got " + version + ")");
            sc.Name = Str(o, "name", "", "adhoc");
            if (string.IsNullOrWhiteSpace(sc.Name) || sc.Name.IndexOfAny(new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' }) >= 0) throw new ScenarioException("name must be a plain file name");
            sc.Description = Str(o, "description", "", "");

            if (o["setup"] != null)
            {
                var s = Obj(o, "setup", "");
                Only(s, SetupKeys, "setup.");
                sc.FactionCount = (int)Int(s, "factionCount", "setup.", sc.FactionCount);
                if (sc.FactionCount < 2 || sc.FactionCount > FactionInfo.Max) throw new ScenarioException("setup.factionCount must be 2 to " + FactionInfo.Max);
                sc.Human = (int)Int(s, "human", "setup.", sc.Human);
                if (sc.Human != GameSetup.NoHuman && (sc.Human < 0 || sc.Human >= sc.FactionCount)) throw new ScenarioException("setup.human must be -1 or a faction of the game");
                sc.Tutorial = Bool(s, "tutorial", "setup.", false);
                if (Bool(s, "population", "setup.", false)) throw new ScenarioException("setup.population is not implemented until M3");
                if (Int(s, "startNpcsPerFaction", "setup.", 0) != 0) throw new ScenarioException("setup.startNpcsPerFaction is not implemented until M3");
                sc.LifeScale = (int)Int(s, "lifeScale", "setup.", sc.LifeScale);
                sc.TicksPerDay = (int)Int(s, "ticksPerDay", "setup.", sc.TicksPerDay);
                sc.DaysPerYear = (int)Int(s, "daysPerYear", "setup.", sc.DaysPerYear);
                if (sc.LifeScale <= 0 || sc.TicksPerDay <= 0 || sc.DaysPerYear <= 0) throw new ScenarioException("setup.lifeScale, ticksPerDay and daysPerYear must be positive");
                sc.DecayFollowsLifeScale = Bool(s, "decayFollowsLifeScale", "setup.", false);
                sc.FullInformation = Bool(s, "fullInformation", "setup.", true);
                string dis = Str(s, "disease", "setup.", "on").ToLowerInvariant();
                if (dis == "on") sc.Disease = DiseaseSetting.On; else if (dis == "mild") sc.Disease = DiseaseSetting.Mild; else if (dis == "off") sc.Disease = DiseaseSetting.Off;
                else throw new ScenarioException("setup.disease must be on, mild or off");
                if (s["victory"] != null)
                {
                    if (!(s["victory"] is JArray va) || va.Count == 0) throw new ScenarioException("setup.victory must be a non-empty list");
                    sc.Victory = new List<VictorySpec>();
                    foreach (var item in va)
                    {
                        if (!(item is JObject vo)) throw new ScenarioException("setup.victory entries are objects");
                        Only(vo, new[] { "kind", "target" }, "setup.victory[].");
                        string kind = Str(vo, "kind", "setup.victory[].", null) ?? throw new ScenarioException("setup.victory[].kind is required");
                        switch (kind)
                        {
                            case "vp": case "victoryPoints":
                                int target = (int)Int(vo, "target", "setup.victory[].", FactionTuning.VictoryPoints);
                                if (target <= 0) throw new ScenarioException("setup.victory[].target must be positive");
                                sc.Victory.Add(VictorySpec.VictoryPoints(target)); break;
                            case "lastStanding": NoTarget(vo); sc.Victory.Add(VictorySpec.LastStanding()); break;
                            case "sandbox": NoTarget(vo); sc.Victory.Add(VictorySpec.Sandbox()); break;
                            default: throw new ScenarioException("setup.victory[].kind '" + kind + "' is unknown (vp, lastStanding, sandbox)");
                        }
                    }
                }
            }

            if (o["tuning"] != null)
            {
                var t = Obj(o, "tuning", "");
                var probe = new SimTuning();
                foreach (var p in t.Properties())
                {
                    if (p.Value.Type != JTokenType.Integer && p.Value.Type != JTokenType.Boolean) throw new ScenarioException("tuning." + p.Name + " must be an integer");
                    long v = p.Value.Type == JTokenType.Boolean ? ((bool)p.Value ? 1 : 0) : (long)p.Value;
                    if (!probe.TrySet(p.Name, v, out var err)) throw new ScenarioException("tuning." + p.Name + ": " + err);
                    sc.Tuning[p.Name] = v;
                }
            }

            if (o["policies"] != null)
            {
                var pol = Obj(o, "policies", "");
                foreach (var p in pol.Properties())
                {
                    if (p.Name != "faction") throw new ScenarioException("policies." + p.Name + " is not implemented until M1");
                    if (!(p.Value is JObject fo)) throw new ScenarioException("policies.faction must be an object");
                    Only(fo, new[] { "default" }, "policies.faction.");
                    string d = Str(fo, "default", "policies.faction.", SimBuilder.StageThreeAi);
                    if (d != SimBuilder.StageThreeAi && d != "none") throw new ScenarioException("policies.faction.default '" + d + "': only AiBrain or none before M1's policy registry");
                    sc.FactionPolicy = d;
                }
            }

            if (o["commands"] != null)
            {
                if (!(o["commands"] is JArray ca)) throw new ScenarioException("commands must be a list");
                foreach (var item in ca)
                {
                    if (!(item is JObject co)) throw new ScenarioException("commands entries are objects");
                    Only(co, new[] { "tick", "type", "args" }, "commands[].");
                    var c = new ScheduledCommand { Tick = Int(co, "tick", "commands[].", -1), Type = Str(co, "type", "commands[].", null), Args = co["args"] as JObject };
                    if (c.Tick < 1) throw new ScenarioException("commands[].tick must be 1 or more");
                    if (co["args"] != null && c.Args == null) throw new ScenarioException("commands[].args must be an object");
                    MakeCommand(c); // validates type and fields now
                    sc.Commands.Add(c);
                }
                for (int i = 1; i < sc.Commands.Count; i++)
                    if (sc.Commands[i].Tick < sc.Commands[i - 1].Tick) throw new ScenarioException("commands must be in tick order");
            }

            if (o["record"] != null)
            {
                var r = Obj(o, "record", "");
                Only(r, RecordKeys, "record.");
                sc.RecordLevel = Level(Str(r, "level", "record.", "off"), "record.level");
                sc.SampleEvery = (int)Int(r, "sampleEvery", "record.", 16);
                if (sc.SampleEvery < 1) throw new ScenarioException("record.sampleEvery must be 1 or more");
                if (r["domains"] != null)
                {
                    var d = Obj(r, "domains", "record.");
                    var names = new HashSet<string>(Enum.GetNames(typeof(TelemetryDomain)).Select(x => x.ToLowerInvariant()));
                    foreach (var p in d.Properties())
                    {
                        if (!names.Contains(p.Name)) throw new ScenarioException("record.domains." + p.Name + " is not a telemetry domain");
                        sc.RecordDomains[p.Name] = Level((string)p.Value, "record.domains." + p.Name);
                    }
                }
            }

            if (o["metrics"] != null)
            {
                if (!(o["metrics"] is JArray ma)) throw new ScenarioException("metrics must be a list of names");
                var cat = new HashSet<string>(MetricsRecorder.Catalogue);
                foreach (var m in ma)
                {
                    string name = (string)m;
                    if (!cat.Contains(name)) throw new ScenarioException("metrics: unknown metric '" + name + "' (M0 metrics: " + string.Join(", ", MetricsRecorder.Catalogue) + ")");
                    sc.Metrics.Add(name);
                }
            }

            sc.CheckpointEvery = Int(o, "checkpointEvery", "", 0);
            if (sc.CheckpointEvery < 0) throw new ScenarioException("checkpointEvery must not be negative");
            sc.SaveEveryDays = (int)Int(o, "saveEveryDays", "", 0);
            if (sc.SaveEveryDays < 0) throw new ScenarioException("saveEveryDays must not be negative");

            if (o["stop"] != null)
            {
                var s = Obj(o, "stop", "");
                Only(s, StopKeys, "stop.");
                sc.MaxTicks = Int(s, "maxTicks", "stop.", 0); sc.MaxDays = Int(s, "maxDays", "stop.", 0); sc.MaxYears = Int(s, "maxYears", "stop.", 0);
                if (sc.MaxTicks < 0 || sc.MaxDays < 0 || sc.MaxYears < 0) throw new ScenarioException("stop limits must not be negative");
                sc.StopOnGameOver = Bool(s, "onGameOver", "stop.", true);
                if (Int(s, "populationBelow", "stop.", 0) != 0) throw new ScenarioException("stop.populationBelow is not implemented until M3");
                sc.WallSeconds = Int(s, "wallSeconds", "stop.", 0);
                sc.StopWhen = Conditions(s, "when"); sc.Fail = Conditions(s, "fail");
            }

            if (o["checks"] != null)
            {
                if (!(o["checks"] is JArray ch)) throw new ScenarioException("checks must be a list");
                foreach (var c in ch)
                {
                    string name = (string)c;
                    if (Array.IndexOf(KnownChecks, name) < 0) throw new ScenarioException("checks: unknown check '" + name + "' (" + string.Join(", ", KnownChecks) + ")");
                    if (!sc.Checks.Contains(name)) sc.Checks.Add(name);
                }
            }
            return sc;
        }

        private static List<MetricCondition> Conditions(JObject s, string key)
        {
            var l = new List<MetricCondition>();
            if (s[key] == null) return l;
            if (!(s[key] is JArray a)) throw new ScenarioException("stop." + key + " must be a list");
            var cat = new HashSet<string>(MetricsRecorder.Catalogue);
            foreach (var item in a)
            {
                if (!(item is JObject c)) throw new ScenarioException("stop." + key + " entries are objects");
                Only(c, new[] { "metric", "above", "below" }, "stop." + key + "[].");
                var m = new MetricCondition { Metric = Str(c, "metric", "stop." + key + "[].", null) };
                if (m.Metric == null || !cat.Contains(m.Metric)) throw new ScenarioException("stop." + key + ": unknown metric '" + m.Metric + "'");
                if (c["above"] != null) m.Above = Int(c, "above", "stop." + key + "[].", 0);
                if (c["below"] != null) m.Below = Int(c, "below", "stop." + key + "[].", 0);
                if (!m.Above.HasValue && !m.Below.HasValue) throw new ScenarioException("stop." + key + ": give above or below");
                l.Add(m);
            }
            return l;
        }

        private static void NoTarget(JObject vo) { if (vo["target"] != null) throw new ScenarioException("setup.victory[]: only vp takes a target"); }

        private static RecordingLevel Level(string s, string where)
        {
            if (s == "experiment") throw new ScenarioException(where + ": the experiment level is not implemented until M13");
            if (!RecordingLevels.TryParse(s, out var l)) throw new ScenarioException(where + " must be off, metrics, summary or full");
            return l;
        }

        private static void Only(JObject o, string[] allowed, string prefix)
        {
            foreach (var p in o.Properties())
                if (Array.IndexOf(allowed, p.Name) < 0)
                {
                    string near = allowed.FirstOrDefault(a => string.Equals(a, p.Name, StringComparison.OrdinalIgnoreCase));
                    throw new ScenarioException("unknown key '" + prefix + p.Name + "'" + (near != null ? " (did you mean '" + near + "'?)" : ""));
                }
        }

        private static JObject Obj(JObject o, string key, string prefix) =>
            o[key] as JObject ?? throw new ScenarioException(prefix + key + " must be an object");

        private static long Int(JObject o, string key, string prefix, long def)
        {
            var t = o[key];
            if (t == null) return def;
            if (t.Type != JTokenType.Integer) throw new ScenarioException(prefix + key + " must be an integer");
            return (long)t;
        }

        private static bool Bool(JObject o, string key, string prefix, bool def)
        {
            var t = o[key];
            if (t == null) return def;
            if (t.Type != JTokenType.Boolean) throw new ScenarioException(prefix + key + " must be true or false");
            return (bool)t;
        }

        private static string Str(JObject o, string key, string prefix, string def)
        {
            var t = o[key];
            if (t == null) return def;
            if (t.Type != JTokenType.String) throw new ScenarioException(prefix + key + " must be a string");
            return (string)t;
        }

        // ---------------- the resolved form ----------------

        /// <summary>The resolved scenario: every key with its value, defaults filled in, in a fixed order.</summary>
        public JObject ToJson()
        {
            var victory = new JArray();
            foreach (var v in Victory)
            {
                var vo = new JObject { ["kind"] = v.Kind == VictoryKind.VictoryPoints ? "vp" : v.Kind == VictoryKind.LastStanding ? "lastStanding" : "sandbox" };
                if (v.Kind == VictoryKind.VictoryPoints) vo["target"] = v.Target;
                victory.Add(vo);
            }
            var tuning = new JObject(); foreach (var kv in Tuning) tuning[kv.Key] = kv.Value;
            var commands = new JArray();
            foreach (var c in Commands) { var co = new JObject { ["tick"] = c.Tick, ["type"] = c.Type }; if (c.Args != null) co["args"] = c.Args.DeepClone(); commands.Add(co); }
            var domains = new JObject(); foreach (var kv in RecordDomains) domains[kv.Key] = RecordingLevels.Name(kv.Value);
            JArray Conds(List<MetricCondition> l)
            {
                var a = new JArray();
                foreach (var m in l) { var mo = new JObject { ["metric"] = m.Metric }; if (m.Above.HasValue) mo["above"] = m.Above.Value; if (m.Below.HasValue) mo["below"] = m.Below.Value; a.Add(mo); }
                return a;
            }
            return new JObject
            {
                ["scenarioVersion"] = CurrentVersion,
                ["name"] = Name,
                ["description"] = Description,
                ["setup"] = new JObject
                {
                    ["factionCount"] = FactionCount, ["human"] = Human, ["tutorial"] = Tutorial, ["population"] = false, ["startNpcsPerFaction"] = 0,
                    ["lifeScale"] = LifeScale, ["ticksPerDay"] = TicksPerDay, ["daysPerYear"] = DaysPerYear,
                    ["decayFollowsLifeScale"] = DecayFollowsLifeScale, ["disease"] = Disease.ToString().ToLowerInvariant(),
                    ["fullInformation"] = FullInformation, ["victory"] = victory,
                },
                ["tuning"] = tuning,
                ["policies"] = new JObject { ["faction"] = new JObject { ["default"] = FactionPolicy } },
                ["commands"] = commands,
                ["record"] = new JObject { ["level"] = RecordingLevels.Name(RecordLevel), ["domains"] = domains, ["sampleEvery"] = SampleEvery },
                ["metrics"] = new JArray(Metrics),
                ["checkpointEvery"] = EffectiveCheckpointEvery,
                ["saveEveryDays"] = SaveEveryDays,
                ["stop"] = new JObject
                {
                    ["maxTicks"] = MaxTicks, ["maxDays"] = MaxDays, ["maxYears"] = MaxYears, ["onGameOver"] = StopOnGameOver,
                    ["populationBelow"] = 0, ["wallSeconds"] = WallSeconds, ["when"] = Conds(StopWhen), ["fail"] = Conds(Fail),
                },
                ["checks"] = new JArray(Checks),
            };
        }

        /// <summary>FNV-1a over the resolved JSON: equal scenarios have equal hashes.</summary>
        public ulong Hash() => Hex.Fnv(ToJson().ToString(Formatting.None));

        /// <summary>A copy (through the resolved form).</summary>
        public Scenario Clone() => FromJson(ToJson());

        /// <summary>The stage 3 smoke game (<c>-godsmoke N -godseed S</c> in the player): six AIs, watch mode, 10 VP, check smoke.</summary>
        public static Scenario Watch6(long ticks = 40000) => new Scenario
        {
            Name = "watch6", Description = "The stage 3 smoke game: six AI factions, watch mode, 10 VP then last standing.",
            MaxTicks = ticks, Checks = new List<string> { "invariants", "no-exceptions", "smoke" },
        };
    }
}
