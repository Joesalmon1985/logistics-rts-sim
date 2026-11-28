using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace God.Sim
{
    /// <summary>
    /// Every number of the rules that a scenario may change, owned by one sim (<see cref="GodState.Tuning"/>), saved and
    /// hashed with its state. M0 replaces the mutable statics of stages 1 to 3 (<c>MilitaryTuning</c>'s 22 fields) with this
    /// object, so two sims in one process can run different tunings side by side. Numbers that are never tuned stay
    /// <c>const</c> where they are.
    /// <para>Scenario files and <c>--set</c> address fields by dotted path (<c>Military.CritPct</c>, <c>Military.Sword.Damage</c>,
    /// <c>Decay.GrudgeBasePermille</c>): see <see cref="TrySet"/>. Values are integers (booleans as 0 or 1).</para>
    /// </summary>
    public sealed class SimTuning
    {
        /// <summary>Gear and combat (stage 3 numbers).</summary>
        public MilitaryTuning Military = new MilitaryTuning();

        /// <summary>Forgetting rates for the social memory and learning stores of later milestones (M3, M5). Unused in M0.</summary>
        public DecayTuning Decay = new DecayTuning();

        /// <summary>M3: bodies, drives, movement, work and huts of the population. Written to JSON only when changed (stage 3 hashes stay).</summary>
        public Population.PopulationTuning Population = new Population.PopulationTuning();
        /// <summary>M3: the ecological rules of wild sources (Q11). Written only when changed.</summary>
        public Population.EcologyTuning Ecology = new Population.EcologyTuning();
        /// <summary>M3: the associative learning core (NPC_LEARNING). Written only when changed.</summary>
        public Learning.LearningTuning Learning = new Learning.LearningTuning();
        /// <summary>F0.12 (M4): households, life cycle, care. Written only when changed.</summary>
        public Population.Households.HouseholdTuning Households = new Population.Households.HouseholdTuning();
        /// <summary>F0.12 (M4): biological inheritance. Written only when changed.</summary>
        public Population.Genetics.GeneticsTuning Genetics = new Population.Genetics.GeneticsTuning();
        /// <summary>F0.12 (M4): estates. Written only when changed.</summary>
        public Estate.EstateTuning Estate = new Estate.EstateTuning();
        /// <summary>F0.12 (M5): social memory, exchange and gifts. Written only when changed.</summary>
        public God.Sim.Social.SocialTuning Social = new God.Sim.Social.SocialTuning();
        /// <summary>F0.12 (M5): gestures and decoding. Written only when changed.</summary>
        public Comms.CommsTuning Comms = new Comms.CommsTuning();
        /// <summary>F1.3 (contracts 16): the divinity module (D1-D7); <c>Enabled = 0</c> by default. Written only when changed.</summary>
        public Divinity.DivinityTuning Divinity = new Divinity.DivinityTuning();
        /// <summary>F1.3: organisations (C6-C10); <c>Enabled = 0</c>, <c>Emergence = 1</c> by default. Written only when changed.</summary>
        public Orgs.OrgTuning Orgs = new Orgs.OrgTuning();
        /// <summary>F1.3: the civilisation ledger (C1-C5); <c>Enabled = 0</c> by default. Written only when changed.</summary>
        public Civ.CivTuning Civ = new Civ.CivTuning();
        /// <summary>UN0 (contracts 23.1): the unified switch; <c>Enabled = 0</c> by default. Written only when changed.</summary>
        public Unified.UnifiedTuning Unification = new Unified.UnifiedTuning();

        /// <summary>A deep, independent copy (through the save format, so every saved field is copied).</summary>
        public SimTuning Clone()
        {
            var json = JsonConvert.SerializeObject(this, SaveFile.Settings);
            return JsonConvert.DeserializeObject<SimTuning>(json, SaveFile.Settings);
        }

        /// <summary>The tuning as the save writes it (stable field order): used by the hash and by tests.</summary>
        public string ToJson() => JsonConvert.SerializeObject(this, SaveFile.Settings);

        // ---------------- dotted paths ----------------

        /// <summary>Every settable leaf path, in declaration order (for help text and the scenario parser).</summary>
        public static List<string> Paths()
        {
            var l = new List<string>();
            Collect(typeof(SimTuning), "", l);
            return l;
        }

        private static void Collect(Type t, string prefix, List<string> into)
        {
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                string p = prefix + f.Name;
                if (IsLeaf(f.FieldType)) into.Add(p);
                else Collect(f.FieldType, p + ".", into);
            }
        }

        private static bool IsLeaf(Type t) => t == typeof(int) || t == typeof(long) || t == typeof(bool) || t.IsEnum;

        /// <summary>Reads a leaf as an integer. False for an unknown path.</summary>
        public bool TryGet(string path, out long value)
        {
            value = 0;
            if (!Walk(path, out var chain, out var leaf)) return false;
            object holder = this;
            foreach (var f in chain) holder = f.GetValue(holder);
            var v = leaf.GetValue(holder);
            value = v is bool b ? (b ? 1 : 0) : Convert.ToInt64(v);
            return true;
        }

        /// <summary>
        /// Sets one leaf by dotted path. Refuses unknown paths, paths naming a seed, and values out of the field's range.
        /// Structs on the way (a <see cref="WeaponStats"/>) are copied, changed and written back.
        /// </summary>
        public bool TrySet(string path, long value, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(path)) { error = "empty tuning path"; return false; }
            if (path.IndexOf("seed", StringComparison.OrdinalIgnoreCase) >= 0) { error = "tuning may not touch seeds: " + path; return false; }
            if (!Walk(path, out var chain, out var leaf)) { error = "unknown tuning path: " + path; return false; }
            object boxed;
            var t = leaf.FieldType;
            if (t == typeof(bool)) { if (value != 0 && value != 1) { error = path + " takes 0 or 1"; return false; } boxed = value == 1; }
            else if (t == typeof(int)) { if (value < int.MinValue || value > int.MaxValue) { error = path + " is out of range"; return false; } boxed = (int)value; }
            else if (t == typeof(long)) boxed = value;
            else if (t.IsEnum) { if (!Enum.IsDefined(t, Convert.ChangeType(value, Enum.GetUnderlyingType(t)))) { error = path + ": no such value " + value; return false; } boxed = Enum.ToObject(t, value); }
            else { error = "not a settable leaf: " + path; return false; }
            Assign(this, chain, 0, leaf, boxed);
            return true;
        }

        /// <summary>Sets the leaf at the end of the chain, writing every value-type holder back on the way out.</summary>
        private static object Assign(object holder, List<FieldInfo> chain, int i, FieldInfo leaf, object value)
        {
            if (i == chain.Count) { leaf.SetValue(holder, value); return holder; }
            var f = chain[i];
            var child = f.GetValue(holder); // a boxed copy for a struct
            child = Assign(child, chain, i + 1, leaf, value);
            f.SetValue(holder, child);
            return holder;
        }

        private static bool Walk(string path, out List<FieldInfo> chain, out FieldInfo leaf)
        {
            chain = new List<FieldInfo>(); leaf = null;
            if (string.IsNullOrEmpty(path)) return false;
            var parts = path.Split('.');
            var t = typeof(SimTuning);
            for (int i = 0; i < parts.Length; i++)
            {
                var f = t.GetField(parts[i], BindingFlags.Public | BindingFlags.Instance);
                if (f == null) return false;
                if (i == parts.Length - 1) { if (!IsLeaf(f.FieldType)) return false; leaf = f; return true; }
                if (IsLeaf(f.FieldType)) return false;
                chain.Add(f);
                t = f.FieldType;
            }
            return false;
        }
    }

    /// <summary>
    /// A tuning block that the save and the hash write only when it differs from its defaults, so adding a block for a new
    /// module leaves every older game's JSON, and so its hash, exactly as it was. A default block is rebuilt on load.
    /// </summary>
    public interface IOmitWhenDefault { }

    /// <summary>
    /// Per-day forgetting rates (permille) for later milestones: social memory (grudges, M5; SOCIAL_MEMORY 4.7) and
    /// learned associations (M3; NPC_LEARNING 4.12). By default they do not follow <c>LifeScale</c>
    /// (<see cref="GameSetup.DecayFollowsLifeScale"/>; conventions, open question 1). <see cref="SimCalendar.DecayPermille"/>
    /// applies that choice.
    /// </summary>
    public sealed class DecayTuning
    {
        /// <summary>Grudge decay per day at Vengefulness 0: <c>clamp(GrudgeBase - Vengefulness / 2, GrudgeMin, GrudgeMax)</c>.</summary>
        public int GrudgeBasePermille = 60;
        public int GrudgeMinPermille = 10;
        public int GrudgeMaxPermille = 110;
        /// <summary>Association forgetting: an untouched entry loses <c>(Ltw >> AssociationForgetShift) * ForgetRate / 1000</c> a day.</summary>
        public int AssociationForgetShift = 5;
        /// <summary>The genome's <c>ForgetRate</c> before genomes exist (M4): 1000 permille, about 3% a day.</summary>
        public int AssociationForgetRatePermille = 1000;
    }
}
