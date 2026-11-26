using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace God.Sim.Tests
{
    /// <summary>
    /// Lists every type a type refers to (G4.1, G4.5): field types, method and constructor signatures, local variables,
    /// and the IL operands of its bodies (call and field targets with their signatures, type tokens), with generic arguments
    /// and element types unwrapped, over the type and all its nested types (lambdas, closures and iterator classes are
    /// nested types). Works under .NET and Unity's Mono. Used by the reflection tests of M1 and later milestones.
    /// </summary>
    public static class TypeReferenceScanner
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static Dictionary<short, OpCode> _ops;

        private static Dictionary<short, OpCode> Ops()
        {
            if (_ops != null) return _ops;
            var d = new Dictionary<short, OpCode>();
            foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (f.GetValue(null) is OpCode op) d[op.Value] = op;
            _ops = d;
            return d;
        }

        /// <summary>One reference: the type referred to and where (for messages).</summary>
        public readonly struct Reference
        {
            public readonly Type Type; public readonly string Where;
            public Reference(Type type, string where) { Type = type; Where = where; }
            public override string ToString() => Where + " -> " + Type.FullName;
        }

        /// <summary>Every reference from <paramref name="type"/> and its nested types.</summary>
        public static List<Reference> ReferencesOf(Type type)
        {
            var l = new List<Reference>();
            Scan(type, l);
            return l;
        }

        /// <summary>The references of <paramref name="type"/> whose type is in <paramref name="forbidden"/>.</summary>
        public static List<Reference> Forbidden(Type type, ICollection<Type> forbidden) =>
            ReferencesOf(type).Where(r => forbidden.Contains(r.Type)).ToList();

        /// <summary>The distinct types referred to.</summary>
        public static HashSet<Type> TypesOf(Type type) => new HashSet<Type>(ReferencesOf(type).Select(r => r.Type));

        private static void Scan(Type t, List<Reference> into)
        {
            string tn = t.FullName ?? t.Name;
            Add(t.BaseType, tn + " base", into);
            foreach (var i in t.GetInterfaces()) Add(i, tn + " implements", into);
            foreach (var f in t.GetFields(All)) Add(f.FieldType, tn + "." + f.Name, into);
            foreach (var p in t.GetProperties(All)) Add(p.PropertyType, tn + "." + p.Name, into);
            foreach (var m in t.GetMethods(All)) ScanMethod(t, m, tn + "." + m.Name, into);
            foreach (var c in t.GetConstructors(All)) ScanMethod(t, c, tn + ".ctor", into);
            foreach (var n in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) Scan(n, into);
        }

        private static void ScanMethod(Type owner, MethodBase m, string where, List<Reference> into)
        {
            if (m is MethodInfo mi) Add(mi.ReturnType, where + " returns", into);
            foreach (var p in m.GetParameters()) Add(p.ParameterType, where + "(" + p.Name + ")", into);
            MethodBody body;
            try { body = m.GetMethodBody(); } catch { body = null; }
            if (body == null) return;
            foreach (var lv in body.LocalVariables) Add(lv.LocalType, where + " local", into);
            var il = body.GetILAsByteArray();
            if (il == null) return;
            var module = m.Module;
            Type[] typeArgs = owner.IsGenericType ? owner.GetGenericArguments() : null;
            Type[] methodArgs = m.IsGenericMethod ? m.GetGenericArguments() : null;
            var ops = Ops();
            int pos = 0;
            while (pos < il.Length)
            {
                short code = il[pos++];
                if (code == 0xFE && pos < il.Length) code = (short)(0xFE00 | il[pos++]);
                if (!ops.TryGetValue(code, out var op)) break; // unknown opcode: stop rather than misread
                int size = OperandSize(op.OperandType, il, pos);
                if (op.OperandType == OperandType.InlineMethod || op.OperandType == OperandType.InlineField
                    || op.OperandType == OperandType.InlineType || op.OperandType == OperandType.InlineTok)
                {
                    int token = BitConverter.ToInt32(il, pos);
                    MemberInfo member = null;
                    try { member = module.ResolveMember(token, typeArgs, methodArgs); } catch { }
                    switch (member)
                    {
                        case MethodBase target:
                            Add(target.DeclaringType, where + " calls " + target.Name, into);
                            if (target is MethodInfo tm) Add(tm.ReturnType, where + " calls " + target.Name + " returning", into);
                            try { foreach (var p in target.GetParameters()) Add(p.ParameterType, where + " calls " + target.Name + " with", into); } catch { }
                            if (target.IsGenericMethod) foreach (var g in target.GetGenericArguments()) Add(g, where + " calls " + target.Name + "<>", into);
                            break;
                        case FieldInfo field:
                            Add(field.DeclaringType, where + " uses field " + field.Name + " of", into);
                            Add(field.FieldType, where + " uses field " + field.Name, into);
                            break;
                        case Type type:
                            Add(type, where + " type token", into);
                            break;
                    }
                }
                pos += size;
            }
        }

        private static int OperandSize(OperandType t, byte[] il, int pos)
        {
            switch (t)
            {
                case OperandType.InlineNone: return 0;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: return 1;
                case OperandType.InlineVar: return 2;
                case OperandType.InlineI8: case OperandType.InlineR: return 8;
                case OperandType.InlineSwitch: return 4 + 4 * BitConverter.ToInt32(il, pos);
                default: return 4;
            }
        }

        private static void Add(Type t, string where, List<Reference> into)
        {
            if (t == null) return;
            if (t.HasElementType) { Add(t.GetElementType(), where, into); return; }
            if (t.IsGenericType && !t.IsGenericTypeDefinition)
            {
                into.Add(new Reference(t.GetGenericTypeDefinition(), where));
                foreach (var a in t.GetGenericArguments()) Add(a, where + "<>", into);
                return;
            }
            if (t.IsGenericParameter) return;
            into.Add(new Reference(t, where));
        }
    }
}
