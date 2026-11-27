using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace God.Sim.Tests
{
    /// <summary>
    /// M0 (HEADLESS_SIMULATION 4.6, acceptance M0.5 and G2.7): no mutable static state in the sim assembly, so sims in one
    /// process share nothing. Every static field of <c>God.Sim</c> must be <c>const</c>, or <c>readonly</c> of an immutable
    /// type (primitive, string, enum, a struct or class whose fields are all readonly and immutable, a
    /// <see cref="ReadOnlyArray{T}"/> of immutable items), or be on <see cref="Allowed"/> with a written reason.
    /// </summary>
    public class StaticAuditTests
    {
        /// <summary>The allow-list: "Type.Field" and why it is safe. It only shrinks.</summary>
        public static readonly Dictionary<string, string> Allowed = new Dictionary<string, string>
        {
            ["BoardTopology.Cache"] = "Locked cache (lock (Cache)) of a pure function of radius and shape; never part of state, never saved.",
            ["NodeGrid.Cache"] = "Locked cache (lock (Lock)) of a pure function of radius and shape; never part of state, never saved.",
            ["EconomyDefs.ByKind"] = "Private lookup over the frozen EconomyDefs.All table, filled once in the static constructor and only read after.",
            ["SaveFile.Settings"] = "Newtonsoft serializer settings: configuration built once by the initialiser and never changed; holds no rule data.",
            ["TutorialSteps.All"] = "Tutorial card texts read by the HUD and the tutorial tracker; never written after construction. TEMPORARY: TutorialStep becomes readonly in the M14 tutorial rewrite.",
            ["EventCatalog.ByType"] = "Private lookup over the frozen EventCatalog.All table, filled once in the static constructor and only read after.",
        };

        [Test]
        public void StaticAudit_NoMutableStatics()
        {
            var bad = new List<string>();
            var seen = new HashSet<string>();
            foreach (var type in typeof(GodSim).Assembly.GetTypes())
            {
                if (IsCompilerGenerated(type)) continue;
                foreach (var f in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (f.IsLiteral) continue; // const
                    string key = Name(type) + "." + FieldName(f);
                    seen.Add(key);
                    if (Allowed.ContainsKey(key)) continue;
                    if (!f.IsInitOnly) { bad.Add(key + ": mutable static field (" + f.FieldType.Name + ")"); continue; }
                    object value = f.GetValue(null);
                    var why = new List<string>();
                    if (!IsImmutable(f.FieldType, value, new HashSet<Type>(), why))
                        bad.Add(key + ": readonly but its contents can change (" + string.Join("; ", why) + ")");
                }
            }
            Assert.IsEmpty(bad, "mutable statics in God.Sim:\n" + string.Join("\n", bad));
            foreach (var kv in Allowed)
            {
                Assert.IsTrue(seen.Contains(kv.Key), "allow-list entry " + kv.Key + " names no static field any more: remove it");
                Assert.IsFalse(string.IsNullOrWhiteSpace(kv.Value), kv.Key + " has no reason");
            }
        }

        [Test]
        public void StaticAudit_CatchesAMutableStatic()
        {
            // The audit's own rule, on fixtures outside the sim assembly.
            var why = new List<string>();
            Assert.IsFalse(IsImmutable(typeof(int[]), new int[1], new HashSet<Type>(), why), "an array can be edited");
            Assert.IsFalse(IsImmutable(typeof(List<int>), new List<int>(), new HashSet<Type>(), why), "a list can be edited");
            Assert.IsFalse(IsImmutable(typeof(MilitaryTuning), new MilitaryTuning(), new HashSet<Type>(), why), "tuning has settable fields");
            Assert.IsTrue(IsImmutable(typeof(ReadOnlyArray<Ware>), new ReadOnlyArray<Ware>(Ware.Log), new HashSet<Type>(), why));
            Assert.IsTrue(IsImmutable(typeof(BuildingDef), EconomyDefs.Hq, new HashSet<Type>(), why), "frozen building defs");
            Assert.IsFalse(typeof(Holder).GetField(nameof(Holder.Mutable)).IsInitOnly, "a plain static field is flagged by IsInitOnly");
        }

        private static class Holder
        {
#pragma warning disable CS0649
            public static int Mutable;
#pragma warning restore CS0649
        }

        // ---------------- the rule ----------------

        private static bool IsCompilerGenerated(Type t)
        {
            for (var x = t; x != null; x = x.DeclaringType)
                if (x.Name.StartsWith("<") || x.GetCustomAttribute<CompilerGeneratedAttribute>() != null) return true;
            return false;
        }

        private static string Name(Type t) => t.DeclaringType != null ? Name(t.DeclaringType) + "+" + t.Name : t.Name;

        /// <summary>The property name for an auto-property's backing field, else the field name.</summary>
        private static string FieldName(FieldInfo f)
        {
            var n = f.Name;
            if (n.StartsWith("<") && n.Contains(">k__BackingField")) return n.Substring(1, n.IndexOf('>') - 1);
            return n;
        }

        /// <summary>True when nothing reachable from a value of this type can be changed after construction.</summary>
        public static bool IsImmutable(Type declared, object value, HashSet<Type> busy, List<string> why)
        {
            var t = value != null ? value.GetType() : declared;
            if (t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(object)) return true;
            if (typeof(Type).IsAssignableFrom(t)) return true; // reflection metadata: the runtime's own caches are not sim state
            if (t.IsArray) { why.Add(t.Name + " is an array"); return false; }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ReadOnlyArray<>))
            {
                var item = t.GetGenericArguments()[0];
                if (value is System.Collections.IEnumerable items)
                {
                    foreach (var x in items) if (!IsImmutable(item, x, busy, why)) return false;
                    return true;
                }
                return IsImmutable(item, null, busy, why);
            }
            if (typeof(Delegate).IsAssignableFrom(t)) { why.Add(t.Name + " is a delegate"); return false; }
            if (t.IsInterface || t.IsAbstract && value == null) { why.Add(t.Name + " is abstract with no value to inspect"); return false; }
            if (!busy.Add(t)) return true; // already being checked (a cycle)
            try
            {
                for (var x = t; x != null && x != typeof(object) && x != typeof(ValueType); x = x.BaseType)
                {
                    foreach (var f in x.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (!f.IsInitOnly) { why.Add(t.Name + "." + FieldName(f) + " is not readonly"); return false; }
                        object v = value != null ? f.GetValue(value) : null;
                        if (!IsImmutable(f.FieldType, v, busy, why)) { why.Add("in " + t.Name + "." + FieldName(f)); return false; }
                    }
                }
                return true;
            }
            finally { busy.Remove(t); }
        }
    }
}
