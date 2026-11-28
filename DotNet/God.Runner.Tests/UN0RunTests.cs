using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// UN0 (contracts 23.2, UN0 spec 13): the run-level pins of the unification foundation. U-P1 with the switch written explicitly
    /// at 0 (the F1 base digests, which F1NeutralityTests also pins with the switch omitted: the same runs, since a default block is
    /// not written); the U-P3 static guard (no title or permission read under <c>Unified/</c> or in a unified branch of a shared
    /// file); the stub unified run of ibs_families (deterministic, seeds 1-3, 10 days).
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class UN0RunTests
    {
        // ---------------------------------------------------------------- U-P1

        [Test]
        public void Unification_Off_Reproduces_Base()
        {
            var t = new God.Sim.SimTuning();
            string json = t.ToJson();
            Assert.IsTrue(t.TrySet("Unification.Enabled", 0, out var err), err);
            Assert.AreEqual(json, t.ToJson(), "Enabled = 0 writes nothing: the runs of F1NeutralityTests are exactly the switch-off runs");
            var keys = new List<string>(); var specs = new List<RunSpec>();
            foreach (var sc in F1NeutralityTests.Scenarios)
                for (ulong seed = 1; seed <= 3; seed++) { keys.Add(sc + "/" + seed); specs.Add(F1NeutralityTests.Spec(sc, seed, "tuning.Unification.Enabled=0")); }
            var res = TestUtil.RunAll(specs);
            for (int i = 0; i < res.Length; i++)
            {
                Assert.AreEqual(0, res[i].ExitCode, keys[i] + ": " + res[i].Error);
                Assert.AreEqual(F1NeutralityTests.Base[keys[i]], F1NeutralityTests.Digest(res[i]), "U-P1: " + keys[i] + " differs from the base");
            }
        }

        // ---------------------------------------------------------------- U-P3 (static)

        internal static readonly string[] Forbidden = { ".Owner", "OwnerRelation", "Permits.", "StoreAccess", "RightOn", "Truth(" };

        /// <summary>Files under Unified/ allowed to name a forbidden token, with the reason (the list only shrinks).</summary>
        internal static readonly Dictionary<string, string> Allowed = new Dictionary<string, string>
        {
            ["Invariants.cs"] = "U-C5 reads every Owner field to assert it is None; it never acts on one.",
        };

        internal static string SimRoot()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Assets", "God", "Sim"))) d = d.Parent;
            Assert.IsNotNull(d, "repository root not found above " + AppContext.BaseDirectory);
            return Path.Combine(d.FullName, "Assets", "God", "Sim");
        }

        /// <summary>The code of a C# text without comments and string literals (line structure kept).</summary>
        internal static string[] CodeLines(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", m => new string('\n', m.Value.Count(c => c == '\n')), RegexOptions.Singleline);
            var lines = text.Replace("\r\n", "\n").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var l = Regex.Replace(lines[i], "\"(\\\\.|[^\"\\\\])*\"", "\"\"");
                int c = l.IndexOf("//", StringComparison.Ordinal);
                lines[i] = c >= 0 ? l.Substring(0, c) : l;
            }
            return lines;
        }

        /// <summary>Violations in a file under Unified/: any forbidden token in code.</summary>
        internal static List<string> ScanUnified(string name, string text)
        {
            var bad = new List<string>();
            var lines = CodeLines(text);
            for (int i = 0; i < lines.Length; i++) foreach (var f in Forbidden) if (lines[i].Contains(f)) bad.Add(name + ":" + (i + 1) + " " + f);
            return bad;
        }

        /// <summary>
        /// Violations in a shared file: on a line that branches on the switch (it names <c>Unified.</c>, <c>Unification</c> or the
        /// local <c>u ?</c>), the unified branch is the text before the legacy arm (the first <c> : </c> of the ternary or the first
        /// <c> || </c> after a <c>Unification != null</c> test); a forbidden token there is a violation.
        /// </summary>
        internal static List<string> ScanShared(string name, string text)
        {
            var bad = new List<string>();
            var lines = CodeLines(text);
            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i];
                if (!(l.Contains("Unified.") || l.Contains("Unification") || Regex.IsMatch(l, @"\bu \?"))) continue;
                int cut = l.Length;
                int t = l.IndexOf(" : ", StringComparison.Ordinal); if (t >= 0) cut = Math.Min(cut, t);
                int o = l.IndexOf(" || ", StringComparison.Ordinal); if (o >= 0 && l.Contains("Unification != null")) cut = Math.Min(cut, o);
                var head = l.Substring(0, cut);
                foreach (var f in Forbidden) if (head.Contains(f)) bad.Add(name + ":" + (i + 1) + " " + f);
            }
            return bad;
        }

        [Test]
        public void Unified_NoLegacyTitle_Static()
        {
            string root = SimRoot();
            var bad = new List<string>();
            int unifiedFiles = 0, sharedFiles = 0;
            foreach (var path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
            {
                string rel = path.Substring(root.Length + 1).Replace('\\', '/');
                string text = File.ReadAllText(path);
                if (rel.StartsWith("Unified/"))
                {
                    unifiedFiles++;
                    if (Allowed.ContainsKey(Path.GetFileName(rel))) continue;
                    bad.AddRange(ScanUnified(rel, text));
                }
                else if (text.Contains("Unified.") || text.Contains("Unification"))
                {
                    sharedFiles++;
                    bad.AddRange(ScanShared(rel, text));
                }
            }
            Assert.Greater(unifiedFiles, 10, "the Unified folder was scanned");
            Assert.Greater(sharedFiles, 5, "the shared files with unified branches were scanned");
            Assert.IsEmpty(bad, "U-P3: title or permission reads on the unified path:\n" + string.Join("\n", bad));
            // the guard catches a seeded violation (and ignores comments and strings)
            Assert.IsNotEmpty(ScanUnified("x.cs", "class X { int F(Lot l) => l.Owner.Id; }"));
            Assert.IsNotEmpty(ScanUnified("x.cs", "bool b = Permits.Believed(s);"));
            Assert.IsEmpty(ScanUnified("x.cs", "// a comment about l.Owner\nvar t = \"Truth(\";"));
            Assert.IsNotEmpty(ScanShared("y.cs", "var o = Unified.Unified.On(s) ? lot.Owner : Owner.None;"));
            Assert.IsEmpty(ScanShared("y.cs", "var o = u ? Owner.None : lot.Owner;"));
        }

        // ---------------------------------------------------------------- UN0-T17 (the runner side)

        [Test]
        public void Unified_StubRun_Deterministic()
        {
            const long ticks = 12000; // 10 days at the scenario calendar
            var specs = new List<RunSpec>();
            for (ulong seed = 1; seed <= 3; seed++)
                for (int k = 0; k < 2; k++)
                {
                    var sp = TestUtil.Spec("ibs_families", seed, ticks, "tuning.Unification.Enabled=1");
                    sp.Scenario.Checks.Remove("smoke");
                    sp.CheckpointEvery = 600;
                    specs.Add(sp);
                }
            var r = TestUtil.RunAll(specs);
            for (int i = 0; i < r.Length; i += 2)
            {
                Assert.AreEqual(0, r[i].ExitCode, "seed " + (i / 2 + 1) + ": " + r[i].Error + " " + r[i].StopReason + " " + string.Join(" | ", r[i].Messages));
                Assert.AreEqual(0, r[i + 1].ExitCode, r[i + 1].Error);
                CollectionAssert.AreEqual(TestUtil.Seq(r[i]), TestUtil.Seq(r[i + 1]), "seed " + (i / 2 + 1) + ": identical at every checkpoint");
                TestContext.WriteLine("ibs_families unified seed " + (i / 2 + 1) + ": " + r[i].FinalHash.ToString("x16") + " parts " + string.Join(",", r[i].FinalParts.Select(p => p.ToString("x16"))));
            }
        }
    }
}
