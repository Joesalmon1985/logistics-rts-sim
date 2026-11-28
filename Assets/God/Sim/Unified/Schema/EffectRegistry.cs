using System;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.7, UN0 spec 5.8): what an effect handler is given: the actor, the control, its origin, the matched interaction
    /// row and effect, the resolved target and the source lot (0 = empty hand), and the work done so far.
    /// </summary>
    public struct EffectCall
    {
        public int Actor;
        public PrimitiveControl Control;
        public ControlOrigin Origin;
        public InteractionDef Row;
        public EffectDef Effect;
        public TargetRef Target;
        public int SourceLot;
        public int Progress;
    }

    /// <summary>
    /// UN0 (LEAD_RULINGS R85, UN0 spec 5.8): one effect kind's handler. <see cref="Validate"/> runs when the action starts (a failure is
    /// the submission's result); <see cref="Apply"/> runs when it completes. A handler is stateless (all state is in <c>GodState</c>):
    /// it must have no mutable fields (the static audit checks the registered instances).
    /// </summary>
    public interface IEffectHandler
    {
        MotorStatus Validate(GodState s, in EffectCall c);
        MotorStatus Apply(GodState s, EventBuffer ev, in EffectCall c);
    }

    /// <summary>The handler of an unregistered kind: refuses with <see cref="MotorStatus.Disabled"/>.</summary>
    public sealed class NoEffectHandler : IEffectHandler
    {
        public MotorStatus Validate(GodState s, in EffectCall c) => MotorStatus.Disabled;
        public MotorStatus Apply(GodState s, EventBuffer ev, in EffectCall c) => MotorStatus.Disabled;
    }

    /// <summary>Collects registrations (in call order) for an <see cref="EffectTable"/>; a second registration of a kind is refused.</summary>
    public sealed class EffectRegistrar
    {
        public const int Kinds = 64;
        private readonly IEffectHandler[] _slots = new IEffectHandler[Kinds];

        public void Register(EffectKind kind, IEffectHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            int k = (int)kind;
            if (k <= 0 || k >= Kinds) throw new ArgumentOutOfRangeException(nameof(kind));
            if (_slots[k] != null) throw new InvalidOperationException("effect kind " + kind + " is already registered");
            _slots[k] = handler;
        }

        public bool IsRegistered(EffectKind kind) => (int)kind > 0 && (int)kind < Kinds && _slots[(int)kind] != null;

        /// <summary>The frozen table of what was registered (unregistered kinds answer <see cref="MotorStatus.Disabled"/>).</summary>
        public EffectTable Freeze()
        {
            var none = new NoEffectHandler();
            var a = new IEffectHandler[Kinds];
            for (int i = 0; i < Kinds; i++) a[i] = _slots[i] ?? none;
            var registered = new bool[Kinds];
            for (int i = 0; i < Kinds; i++) registered[i] = _slots[i] != null;
            return new EffectTable(a, registered);
        }
    }

    /// <summary>
    /// An immutable kind-to-handler table with the dispatch rule: a kind whose behaviour sub-switch is off answers
    /// <see cref="MotorStatus.Disabled"/> before any handler runs; an unregistered kind answers <see cref="MotorStatus.Disabled"/>.
    /// </summary>
    public sealed class EffectTable
    {
        private readonly ReadOnlyArray<IEffectHandler> _h;
        private readonly ReadOnlyArray<bool> _registered;

        internal EffectTable(IEffectHandler[] h, bool[] registered) { _h = new ReadOnlyArray<IEffectHandler>(h); _registered = new ReadOnlyArray<bool>(registered); }

        public bool IsRegistered(EffectKind k) => (int)k > 0 && (int)k < _h.Count && _registered[(int)k];

        public MotorStatus Validate(GodState s, in EffectCall c)
        {
            var k = c.Effect.Kind;
            if (!EffectRegistry.Enabled(s, k) || !IsRegistered(k)) return MotorStatus.Disabled;
            return _h[(int)k].Validate(s, in c);
        }

        public MotorStatus Apply(GodState s, EventBuffer ev, in EffectCall c)
        {
            var k = c.Effect.Kind;
            if (!EffectRegistry.Enabled(s, k) || !IsRegistered(k)) return MotorStatus.Disabled;
            return _h[(int)k].Apply(s, ev, in c);
        }
    }

    /// <summary>
    /// UN0 (contracts 23.7): the dispatch of interaction effects. The owners register their handlers in their slots
    /// (<c>RegisterUN1</c> ... in <c>EffectHandlers.UNn.cs</c>), called once in the fixed order UN1, UN3b, UN4, UN7, UN8a by the static
    /// initialiser, through <see cref="EffectRegistrar.Register"/>; the result is frozen in <see cref="Table"/> (immutable: no mutable
    /// static state, M0 audit). A package registers only its own kinds (<see cref="OwnerOf"/>).
    /// </summary>
    public static partial class EffectRegistry
    {
        /// <summary>The game's handler table (empty at UN0: every kind answers <see cref="MotorStatus.Disabled"/>).</summary>
        public static readonly EffectTable Table = Build();

        private static EffectTable Build()
        {
            var r = new EffectRegistrar();
            RegisterUN1(r); RegisterUN3b(r); RegisterUN4(r); RegisterUN7(r); RegisterUN8a(r);
            return r.Freeze();
        }

        public static MotorStatus Validate(GodState s, in EffectCall c) => Table.Validate(s, in c);
        public static MotorStatus Apply(GodState s, EventBuffer ev, in EffectCall c) => Table.Apply(s, ev, in c);

        /// <summary>The package that owns an effect kind (contracts 23.7), or null for a reserved kind.</summary>
        public static string OwnerOf(EffectKind k)
        {
            switch (k)
            {
                case EffectKind.Forage: case EffectKind.DrinkWash: return "UN1";
                case EffectKind.Eat: case EffectKind.NeedRelief: case EffectKind.CollectRaw: case EffectKind.Deposit: case EffectKind.Install:
                case EffectKind.Occupy: case EffectKind.WorkInput: case EffectKind.RecipeWork: case EffectKind.Fuel: case EffectKind.Relieve:
                case EffectKind.Play: return "UN3b";
                case EffectKind.PlaceSite: case EffectKind.Contribute: case EffectKind.Labour: return "UN4";
                case EffectKind.HoldOut: return "UN7";
                case EffectKind.Strike: case EffectKind.Equip: case EffectKind.Treat: return "UN8a";
                default: return null;
            }
        }

        /// <summary>True when the behaviour sub-switch of the kind's owner is on (contracts 23.1); false outside a unified game.</summary>
        public static bool Enabled(GodState s, EffectKind k)
        {
            switch (OwnerOf(k))
            {
                case "UN1": return Unified.WorldOn(s);
                case "UN3b": return Unified.InteractionsOn(s);
                case "UN4": return Unified.ConstructionOn(s);
                case "UN7": return Unified.ExchangeOn(s);
                case "UN8a": return Unified.ConflictOn(s);
                default: return false;
            }
        }

        static partial void RegisterUN1(EffectRegistrar r);
        static partial void RegisterUN3b(EffectRegistrar r);
        static partial void RegisterUN4(EffectRegistrar r);
        static partial void RegisterUN7(EffectRegistrar r);
        static partial void RegisterUN8a(EffectRegistrar r);
    }
}
