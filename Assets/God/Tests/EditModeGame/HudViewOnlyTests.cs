using System;
using System.Collections.Generic;
using System.Linq;
using God.Sim;
using God.Sim.Policy;
using God.Sim.Tests;
using NUnit.Framework;

namespace God.Game.Tests
{
    /// <summary>
    /// M1.2 / M1.3 / G4.5 for the HUD (Unity EditMode: the HUD assembly needs UnityEngine, so this half cannot run under
    /// <c>dotnet test</c>). The HUD types read the world only through <c>FactionKnowledgeView</c> and act only through
    /// <c>ICommandGate</c>: no field, parameter, local, generic argument, call or field target of <see cref="GodState"/>,
    /// a module state or <see cref="GodSim"/> (whose State is the back door). The command types the HUD can send equal the
    /// set an <see cref="IFactionPolicy"/> can return.
    /// </summary>
    public class HudViewOnlyTests
    {
        /// <summary>Every type of Assets/God/Game/Hud (all partial files of GodHud included).</summary>
        public static readonly Type[] HudTypes =
        {
            typeof(GodHud), typeof(HudOverlay), typeof(HudStyle), typeof(GodCameraRig), typeof(WatchCamera),
            typeof(StandingRow), typeof(BuildTool), typeof(BuildTab), typeof(HudMode),
        };

        /// <summary>Temporary exceptions (type, reason, removal milestone). Empty: the HUD has none.</summary>
        public static readonly Dictionary<string, string> Allowed = new Dictionary<string, string>();

        [Test]
        public void Hud_ReadsOnlyTheView()
        {
            var forbidden = new HashSet<Type>(PolicyTests.StateTypes);
            var bad = new List<string>();
            foreach (var t in HudTypes)
                foreach (var r in TypeReferenceScanner.Forbidden(t, forbidden))
                    if (!Allowed.ContainsKey(r.Where)) bad.Add(r.ToString());
            Assert.IsEmpty(bad, "the HUD reads true state directly:\n" + string.Join("\n", bad.Distinct()));
            Assert.That(TypeReferenceScanner.TypesOf(typeof(GodHud)).Select(x => x.FullName), Has.Member("God.Sim.Information.FactionKnowledgeView"), "it reads the view");
        }

        [Test]
        public void Hud_CommandTypes_EqualWhatAFactionPolicyCanReturn()
        {
            var commands = new HashSet<Type>();
            foreach (var t in HudTypes)
                foreach (var x in TypeReferenceScanner.TypesOf(t))
                    if (typeof(Command).IsAssignableFrom(x) && !x.IsAbstract) commands.Add(x);
            CollectionAssert.AreEquivalent(FactionCommandSet.Types().Select(x => x.Name), commands.Select(x => x.Name),
                "S-CMD-1: the HUD sends exactly the faction command set (no developer commands, none missing)");
        }

        /// <summary>
        /// The other God.Game types that still read the sim directly (world views, menu, tutorial cards, the bootstrap and
        /// smoke host). Not part of the M1 gate (it covers the HUD); listed for the record. The test fails only if a HUD type
        /// appears here, which the gate above would also catch.
        /// </summary>
        [Test]
        public void Report_OtherGameTypesThatReadTheSim()
        {
            var forbidden = new HashSet<Type>(PolicyTests.StateTypes);
            var hud = new HashSet<Type>(HudTypes);
            var names = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var t in typeof(GodHud).Assembly.GetTypes())
            {
                if (t.IsNested || hud.Contains(t)) continue;
                if (TypeReferenceScanner.Forbidden(t, forbidden).Count > 0) names.Add(t.FullName);
            }
            TestContext.WriteLine("God.Game types reading GodSim/GodState directly (" + names.Count + "): " + string.Join(", ", names));
            Assert.IsFalse(names.Any(n => n.StartsWith("God.Game.GodHud", StringComparison.Ordinal)));
        }

        [Test]
        public void TheScan_SeesTheOldDirectReads()
        {
            // The scanner would have caught M0's HUD: a type that calls GodSim.State is reported.
            Assert.IsNotEmpty(TypeReferenceScanner.Forbidden(typeof(OldStyleReader), new HashSet<Type>(PolicyTests.StateTypes)));
        }

        private sealed class OldStyleReader
        {
            public long Tick(GodBootstrap b) => b.Sim.State.Tick;
        }
    }
}
