using System;
using System.Collections.Generic;

namespace God.Sim.Policy
{
    /// <summary>What a factory is told when it builds a policy: its level, name and scope (a faction id, or -1).</summary>
    public readonly struct PolicyContext
    {
        public readonly PolicyLevel Level;
        public readonly string Name;
        public readonly int Scope;
        public PolicyContext(PolicyLevel level, string name, int scope) { Level = level; Name = name; Scope = scope; }
    }

    /// <summary>
    /// Policies by name, per level (AI_TRAINING_DESIGN §3.1, HEADLESS_SIMULATION "policies"). One registry per sim:
    /// factories may close over that sim's host objects (the stage 3 AI does), never over another sim's. Names are
    /// looked up ordinally and listed in ordinal order; construction is a pure function of (level, name, scope), so two
    /// sims built from the same scenario get the same policies in the same order.
    /// <para>Reserved names: <see cref="None"/> (no policy: the slot decides nothing) and <see cref="External"/> (a served
    /// agent, M15). A test-only policy (a truth-reading benchmark, say) can be registered but is refused unless the caller
    /// allows test policies.</para>
    /// </summary>
    public sealed class PolicyRegistry
    {
        public const string None = "none";
        public const string External = "external";

        private sealed class Entry
        {
            public Func<PolicyContext, IPolicy> Factory;
            public string Description;
            public bool TestOnly;
        }

        private readonly SortedDictionary<string, Entry>[] _levels =
        {
            new SortedDictionary<string, Entry>(StringComparer.Ordinal), new SortedDictionary<string, Entry>(StringComparer.Ordinal),
            new SortedDictionary<string, Entry>(StringComparer.Ordinal), new SortedDictionary<string, Entry>(StringComparer.Ordinal),
        };

        /// <summary>Adds a policy. Names are unique per level; <see cref="None"/> and <see cref="External"/> are reserved.</summary>
        public void Register(PolicyLevel level, string name, Func<PolicyContext, IPolicy> factory, string description, bool testOnly = false)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A policy needs a name.", nameof(name));
            if (name == None || name == External) throw new ArgumentException("'" + name + "' is reserved.", nameof(name));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var d = _levels[(int)level];
            if (d.ContainsKey(name)) throw new ArgumentException("A " + Lower(level) + " policy '" + name + "' is already registered.", nameof(name));
            d.Add(name, new Entry { Factory = factory, Description = description ?? "", TestOnly = testOnly });
        }

        public bool Has(PolicyLevel level, string name) => name == None || (name != null && _levels[(int)level].ContainsKey(name));

        /// <summary>The registered names of a level (ordinal order), <see cref="None"/> first.</summary>
        public List<string> Names(PolicyLevel level, bool includeTestOnly = false)
        {
            var l = new List<string> { None };
            foreach (var kv in _levels[(int)level]) if (includeTestOnly || !kv.Value.TestOnly) l.Add(kv.Key);
            return l;
        }

        public string Describe(PolicyLevel level, string name) =>
            name == None ? "no policy: decides nothing" : _levels[(int)level].TryGetValue(name ?? "", out var e) ? e.Description : null;

        /// <summary>Throws <see cref="PolicyException"/> unless the name can be used at this level.</summary>
        public void Validate(PolicyLevel level, string name, bool allowTestOnly = false)
        {
            if (name == None) return;
            if (name == External) throw new PolicyException("policies." + Lower(level) + ": 'external' (served agents) is not implemented until M15");
            if (name == null || !_levels[(int)level].TryGetValue(name, out var e))
                throw new PolicyException("policies." + Lower(level) + ": unknown policy '" + name + "' (known: " + string.Join(", ", Names(level)) + ")");
            if (e.TestOnly && !allowTestOnly)
                throw new PolicyException("policies." + Lower(level) + ": '" + name + "' is a test-only policy and cannot be selected in a normal game");
        }

        /// <summary>Builds a policy, or returns null for <see cref="None"/>. Throws <see cref="PolicyException"/> for anything else unusable.</summary>
        public IPolicy Create(PolicyLevel level, string name, int scope, bool allowTestOnly = false)
        {
            Validate(level, name, allowTestOnly);
            if (name == None) return null;
            var p = _levels[(int)level][name].Factory(new PolicyContext(level, name, scope));
            if (p == null || p.Level != level) throw new PolicyException("the factory of '" + name + "' did not build a " + Lower(level) + " policy");
            return p;
        }

        public IFactionPolicy CreateFaction(string name, Faction faction, bool allowTestOnly = false) =>
            (IFactionPolicy)Create(PolicyLevel.Faction, name, (int)faction, allowTestOnly);

        public IMilitaryPolicy CreateMilitary(string name, int scope, bool allowTestOnly = false) =>
            (IMilitaryPolicy)Create(PolicyLevel.Military, name, scope, allowTestOnly);

        public static string Lower(PolicyLevel level) => level.ToString().ToLowerInvariant();

        public static bool TryParseLevel(string s, out PolicyLevel level)
        {
            switch (s)
            {
                case "civilian": level = PolicyLevel.Civilian; return true;
                case "military": level = PolicyLevel.Military; return true;
                case "institution": level = PolicyLevel.Institution; return true;
                case "faction": level = PolicyLevel.Faction; return true;
                default: level = PolicyLevel.Civilian; return false;
            }
        }
    }

    /// <summary>
    /// Which policy plays each slot: per level a default name and per-scope overrides (scope = faction id for the faction
    /// and military levels). The resolved form of a scenario's <c>policies</c> block. Plain data; the names are checked
    /// against a <see cref="PolicyRegistry"/> when a sim is built.
    /// </summary>
    public sealed class PolicyAssignment
    {
        /// <summary>The stage 3 faction AI (<c>AiBrain</c> behind <c>AiBrainFactionPolicy</c>), the default faction policy.</summary>
        public const string StageThreeFaction = "AiBrain";
        /// <summary>The stage 3 soldier rules (<c>Stage3MilitaryPolicy</c>), the default military policy until M9.</summary>
        public const string StageThreeMilitary = "Stage3MilitaryPolicy";

        public readonly string[] Defaults = { PolicyRegistry.None, StageThreeMilitary, PolicyRegistry.None, StageThreeFaction };
        public readonly SortedDictionary<int, string>[] Scoped =
        {
            new SortedDictionary<int, string>(), new SortedDictionary<int, string>(), new SortedDictionary<int, string>(), new SortedDictionary<int, string>(),
        };

        /// <summary>The stage 3 game: the AI brain for every faction, the stage 3 soldier rules, nothing else.</summary>
        public static PolicyAssignment Stage3Default() => new PolicyAssignment();

        public string DefaultOf(PolicyLevel level) => Defaults[(int)level];

        public void SetDefault(PolicyLevel level, string name) => Defaults[(int)level] = name;

        public void Set(PolicyLevel level, int scope, string name) => Scoped[(int)level][scope] = name;

        /// <summary>The name for one slot: its own entry, else the level's default.</summary>
        public string NameFor(PolicyLevel level, int scope) => Scoped[(int)level].TryGetValue(scope, out var n) ? n : Defaults[(int)level];

        /// <summary>True when this is exactly <see cref="Stage3Default"/>.</summary>
        public bool IsStage3Default
        {
            get
            {
                var d = new PolicyAssignment();
                for (int i = 0; i < 4; i++) if (Defaults[i] != d.Defaults[i] || Scoped[i].Count > 0) return false;
                return true;
            }
        }

        /// <summary>Checks every name against a registry (throws <see cref="PolicyException"/>).</summary>
        public void Validate(PolicyRegistry registry, bool allowTestOnly = false)
        {
            for (int i = 0; i < 4; i++)
            {
                registry.Validate((PolicyLevel)i, Defaults[i], allowTestOnly);
                foreach (var kv in Scoped[i]) registry.Validate((PolicyLevel)i, kv.Value, allowTestOnly);
            }
        }

        public PolicyAssignment Clone()
        {
            var c = new PolicyAssignment();
            for (int i = 0; i < 4; i++) { c.Defaults[i] = Defaults[i]; foreach (var kv in Scoped[i]) c.Scoped[i][kv.Key] = kv.Value; }
            return c;
        }

        /// <summary>A stable one-line form for logs and manifests: <c>faction=AiBrain,2:none; military=Stage3MilitaryPolicy; ...</c>.</summary>
        public override string ToString()
        {
            var parts = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                var s = PolicyRegistry.Lower((PolicyLevel)i) + "=" + Defaults[i];
                foreach (var kv in Scoped[i]) s += "," + kv.Key + ":" + kv.Value;
                parts.Add(s);
            }
            return string.Join("; ", parts);
        }
    }
}
