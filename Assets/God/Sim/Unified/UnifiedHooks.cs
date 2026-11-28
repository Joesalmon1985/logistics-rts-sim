using System.Collections.Generic;
using God.Sim.Divinity;
using God.Sim.Population;

namespace God.Sim.Unified
{
    /// <summary>
    /// UN0 (contracts 23.0, 23.3, 23.5): the unified dispatch sites, frozen. Each public entry calls the packages' slots in the
    /// fixed order written here; a package implements its slot in its own file <c>UnifiedHooks.UNn.cs</c> (a partial method with no
    /// implementation compiles to nothing, so an empty foundation does nothing). Nothing here is reachable from a legacy game.
    /// </summary>
    public static partial class UnifiedHooks
    {
        /// <summary>
        /// One person's tick (from <c>PopulationSystem.Tick</c>, ascending NpcId, after <c>Body()</c> and the death check): needs
        /// (UN1), action progress (UN2), compulsion (UN9), the decision (UN6). Replaces the legacy sleep, think and act.
        /// </summary>
        public static void PersonTick(GodState s, EventBuffer ev, Npc npc)
        {
            PersonTickUN1(s, ev, npc);
            PersonTickUN2(s, ev, npc);
            PersonTickUN8(s, ev, npc);
            PersonTickUN9(s, ev, npc);
            PersonTickUN6(s, ev, npc);
        }

        /// <summary>
        /// The world's tick (<see cref="UnifiedSystem"/>, order 251), slots in this fixed order: <c>TickWorld</c> (UN1), <c>TickMotor</c>
        /// (UN2), <c>TickInteractions</c> (UN3b), <c>TickSites</c> (UN4), <c>TickExchange</c> (UN7), <c>TickConflict</c> (UN8a),
        /// <c>TickSettlements</c> (UB).
        /// </summary>
        public static void Tick(GodState s, EventBuffer ev)
        {
            TickWorld(s, ev); TickMotor(s, ev); TickInteractions(s, ev); TickSites(s, ev); TickExchange(s, ev); TickConflict(s, ev); TickSettlements(s, ev);
        }

        // ---------------- start seams ----------------

        /// <summary>
        /// True when the world package (UN1) places the unified world itself, so <c>PopulationModule.StartGame</c> skips the legacy
        /// wild sources. False until UN1 fills its slot (the stub unified game keeps the legacy sources).
        /// </summary>
        public static bool ReplacesSources(GodState s) { bool r = false; ReplacesSourcesUN1(s, ref r); return r; }

        /// <summary>
        /// True when the start package (UN5) generates the founders itself, so <c>PopulationModule.StartGame</c> places no legacy
        /// founders (M4's <c>Founders.StartGame</c> then finds none and only creates its state). False until UN5 fills its slot.
        /// </summary>
        public static bool ReplacesFounders(GodState s) { bool r = false; ReplacesFoundersUN5(s, ref r); return r; }

        /// <summary>The unified start (from <c>GodSim.Create</c> after every legacy module started): <see cref="UnifiedModule.StartGame"/>.</summary>
        public static void StartGame(GodState s) => UnifiedModule.StartGame(s);

        /// <summary>A new person (founder, birth, immigrant): its unified record, then the packages (needs UN1, brain UN6).</summary>
        public static void NewPerson(GodState s, Npc npc)
        {
            if (s.Unification == null || !Bodies.IsPerson(npc)) return;
            s.Unification.PersonOrNew(npc.Id);
            NewPersonUN1(s, npc); NewPersonUN6(s, npc);
        }

        /// <summary>
        /// A person died (from <c>PopulationExtensions.OnDeath</c>, before its carry spills to the ground by <c>DeathSpill</c>): the
        /// packages end what the body was doing (UN2), held-out lots (UN7), occupancy (UN3). No succession, no title (D9.13).
        /// </summary>
        public static void OnDeath(GodState s, EventBuffer ev, Npc npc)
        {
            if (s.Unification == null) return;
            OnDeathUN2(s, ev, npc); OnDeathUN3(s, ev, npc); OnDeathUN7(s, ev, npc); OnDeathUN8(s, ev, npc);
        }

        // ---------------- possession seam (contracts 23.5, R84) ----------------

        /// <summary>
        /// Validation of a possessed or avatar body's <see cref="ControlInputKind.Primitive"/> input (from <c>Control.cs</c>'s
        /// handler): the same physical checks <c>Motor.Submit</c> makes (UN2 fills the slot); <see cref="MotorStatus.Disabled"/> while
        /// no motor exists.
        /// </summary>
        public static MotorStatus CheckPossessed(GodState s, int body, in PrimitiveControl c)
        {
            if (!Unified.On(s)) return MotorStatus.Disabled;
            if (c.Control == Prim.None || c.Actor != body) return MotorStatus.Invalid;
            var st = MotorStatus.Disabled;
            CheckPossessedUN2(s, body, in c, ref st);
            return st;
        }

        /// <summary>
        /// Submits an accepted possession input to the motor with origin <see cref="ControlOrigin.Possessed"/> (or
        /// <see cref="ControlOrigin.Avatar"/> for an avatar body): the result is the body's proprioceptive input, as for any origin.
        /// </summary>
        public static MotorStatus SubmitPossessed(GodState s, EventBuffer ev, int body, ControlKind kind, in PrimitiveControl c)
        {
            var st = MotorStatus.Disabled;
            var origin = kind == ControlKind.AvatarBody ? ControlOrigin.Avatar : ControlOrigin.Possessed;
            SubmitPossessedUN2(s, ev, body, origin, in c, ref st);
            return st;
        }

        // ---------------- cross-package seams (R85): the caller is one package, the owner of the slot another ----------------

        /// <summary>UN4's placement rule for a design at a node and facing (<see cref="MotorStatus.Accepted"/> or <c>BadSite</c>); <c>Disabled</c> until UN4.</summary>
        public static MotorStatus LegalPlacement(GodState s, int actor, ushort design, int node, byte facing)
        {
            var st = MotorStatus.Disabled;
            LegalPlacementUN4(s, actor, design, node, facing, ref st);
            return st;
        }

        /// <summary>UN5's development area: true when <paramref name="node"/> lies inside a settlement's area (false until UN5).</summary>
        public static bool DevelopmentArea(GodState s, int node) { bool r = false; DevelopmentAreaUN5(s, node, ref r); return r; }

        /// <summary>UN7's hold-out record of a lot (false when the lot is not held out), for UN2's TAKE rule.</summary>
        public static bool HeldOutLookup(GodState s, int lot, out HoldOut h) { var x = default(HoldOut); bool r = false; HeldOutLookupUN7(s, lot, ref x, ref r); h = x; return r; }

        /// <summary>UN2's TAKE from an awake holder's ordinary carry was resisted: UN7 emits <c>TakeResisted</c> (1283) and the holder perceives it.</summary>
        public static void OnTakeResisted(GodState s, EventBuffer ev, int taker, int holder, int lot, ControlOrigin origin) => OnTakeResistedUN7(s, ev, taker, holder, lot, origin);

        /// <summary>UN2's TAKE from an asleep, unconscious or dead holder (<c>MoveReason.Grab</c>) succeeded: UN7 emits <c>Grabbed</c> (1284).</summary>
        public static void OnGrabbed(GodState s, EventBuffer ev, int taker, int holder, int lot, int count, ControlOrigin origin) => OnGrabbedUN7(s, ev, taker, holder, lot, count, origin);

        /// <summary>UN2's resolver started an interaction: UN3b emits <c>InteractionStarted</c> (1260).</summary>
        public static void OnInteractionStarted(GodState s, EventBuffer ev, in EffectCall c) => OnInteractionStartedUN3b(s, ev, in c);

        /// <summary>UN2's resolver completed an interaction (after the effects applied): UN3b emits <c>InteractionCompleted</c> (1261).</summary>
        public static void OnInteractionCompleted(GodState s, EventBuffer ev, in EffectCall c, MotorStatus result) => OnInteractionCompletedUN3b(s, ev, in c, result);

        /// <summary>Package invariants (from <see cref="UnifiedInvariants.Check"/>): UN1, UN3, UN4, UN6, UN7, UN8.</summary>
        internal static void Check(GodState s, List<string> bad)
        {
            CheckUN1(s, bad); CheckUN3(s, bad); CheckUN4(s, bad); CheckUN6(s, bad); CheckUN7(s, bad); CheckUN8(s, bad);
        }

        // ---------------- slots ----------------
        static partial void PersonTickUN1(GodState s, EventBuffer ev, Npc npc);
        static partial void PersonTickUN2(GodState s, EventBuffer ev, Npc npc);
        static partial void PersonTickUN6(GodState s, EventBuffer ev, Npc npc);
        static partial void PersonTickUN8(GodState s, EventBuffer ev, Npc npc);
        static partial void PersonTickUN9(GodState s, EventBuffer ev, Npc npc);
        static partial void TickWorld(GodState s, EventBuffer ev);         // UN1
        static partial void TickMotor(GodState s, EventBuffer ev);         // UN2
        static partial void TickInteractions(GodState s, EventBuffer ev);  // UN3b
        static partial void TickSites(GodState s, EventBuffer ev);         // UN4
        static partial void TickExchange(GodState s, EventBuffer ev);      // UN7
        static partial void TickConflict(GodState s, EventBuffer ev);      // UN8a
        static partial void TickSettlements(GodState s, EventBuffer ev);   // UB
        static partial void ReplacesSourcesUN1(GodState s, ref bool replaces);
        static partial void ReplacesFoundersUN5(GodState s, ref bool replaces);
        static partial void LegalPlacementUN4(GodState s, int actor, ushort design, int node, byte facing, ref MotorStatus status);
        static partial void DevelopmentAreaUN5(GodState s, int node, ref bool inside);
        static partial void HeldOutLookupUN7(GodState s, int lot, ref HoldOut h, ref bool found);
        static partial void OnTakeResistedUN7(GodState s, EventBuffer ev, int taker, int holder, int lot, ControlOrigin origin);
        static partial void OnGrabbedUN7(GodState s, EventBuffer ev, int taker, int holder, int lot, int count, ControlOrigin origin);
        static partial void OnInteractionStartedUN3b(GodState s, EventBuffer ev, in EffectCall c);
        static partial void OnInteractionCompletedUN3b(GodState s, EventBuffer ev, in EffectCall c, MotorStatus result);
        static partial void NewPersonUN1(GodState s, Npc npc);
        static partial void NewPersonUN6(GodState s, Npc npc);
        static partial void OnDeathUN2(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathUN3(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathUN7(GodState s, EventBuffer ev, Npc npc);
        static partial void OnDeathUN8(GodState s, EventBuffer ev, Npc npc);
        static partial void CheckPossessedUN2(GodState s, int body, in PrimitiveControl c, ref MotorStatus status);
        static partial void SubmitPossessedUN2(GodState s, EventBuffer ev, int body, ControlOrigin origin, in PrimitiveControl c, ref MotorStatus status);
        static partial void CheckUN1(GodState s, List<string> bad);
        static partial void CheckUN3(GodState s, List<string> bad);
        static partial void CheckUN4(GodState s, List<string> bad);
        static partial void CheckUN6(GodState s, List<string> bad);
        static partial void CheckUN7(GodState s, List<string> bad);
        static partial void CheckUN8(GodState s, List<string> bad);
    }
}
