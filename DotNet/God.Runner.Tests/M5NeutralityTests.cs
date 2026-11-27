using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// M5 switch neutrality (SHARED_CONTRACTS 7.3, task rule): with <c>Social.Enabled = 0</c> the game reproduces the post-F0
    /// behaviour exactly. The hashes below were taken at <c>6671592</c> (unity/poc with F0, before any M5 code) with the
    /// same tuning (<c>tuning.Social.Enabled=0</c>, a no-op stub there): band_6x20, seeds 1-3, 10 days, every day boundary.
    /// After the M4 + M5 merge both switches are off, and the pins are 6671592 with both off (re-taken 26 Nov 2026); with only
    /// one switch off the merge reproduces the other branch exactly (checked at the merge, DEV_LOG).
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class M5NeutralityTests
    {
        // seed -> the state hash at the end of days 1..10 (pinned at 6671592)
        private static readonly Dictionary<ulong, ulong[]> Pinned = new Dictionary<ulong, ulong[]>
        {
            [1] = new ulong[] { 0x839f380739e8d190UL, 0x7cbfc8f4af9c7bedUL, 0xa1edf799076908aaUL, 0x14ffc44f225e0a88UL, 0xe5ce23876c5eba3fUL, 0x7a4fda37aae789dfUL, 0xf6139693932f00d8UL, 0x1f2860a159741e81UL, 0xdc0f7ede36c752b5UL, 0x8b898d115d8a05a7UL },
            [2] = new ulong[] { 0x0f9b8f6c1405c902UL, 0xc92eb064468547aaUL, 0xb4f0bddcfe5b5ec4UL, 0x9de2601740ff7bd7UL, 0x04c6c3e3b9134af4UL, 0x1a915f325efd60beUL, 0x9740b0864e3d14c6UL, 0x400691c90a0d1667UL, 0x15a6786fe439663fUL, 0x5fa8991b92168385UL },
            [3] = new ulong[] { 0x948c56ee75eb8667UL, 0x4ce55a1a4a920c5aUL, 0x2ace4827000e95feUL, 0x2178a7d998c1de79UL, 0x795d134c6ee223aeUL, 0xfa5811897405d008UL, 0x852b61695b67a851UL, 0xa005eb24a6cfa46dUL, 0xe445a5172b07d0cfUL, 0x66a55d5a67f2ad4eUL },
        };

        private static RunResultSeq Run(ulong seed)
        {
            var spec = TestUtil.Spec("band_6x20", seed, 12000, "tuning.Social.Enabled=0", "tuning.Households.Enabled=0");
            spec.Scenario.Checks.Remove("smoke");
            spec.CheckpointEvery = 1200;
            var r = RunEngine.Run(spec);
            Assert.AreEqual(0, r.ExitCode, r.Error);
            return new RunResultSeq { Hashes = r.Checkpoints.Where(c => c.Tick > 0 && c.Tick % 1200 == 0).Select(c => c.Hash).ToArray() };
        }

        private sealed class RunResultSeq { public ulong[] Hashes; }

        [Test]
        public void SocialDisabled_Band6x20_Seeds1To3_TenDays_EqualsPostF0Hashes()
        {
            var sb = new StringBuilder();
            for (ulong seed = 1; seed <= 3; seed++)
            {
                var h = Run(seed).Hashes;
                sb.Append("            [" + seed + "] = new ulong[] { " + string.Join(", ", h.Select(x => "0x" + x.ToString("x16") + "UL")) + " },\n");
                if (Pinned.Count == 0) continue;
                CollectionAssert.AreEqual(Pinned[seed], h, "seed " + seed + ": with Social and Households off the day hashes equal 6671592's");
            }
            TestContext.WriteLine(sb.ToString());
            Assert.AreEqual(3, Pinned.Count, "pinned hashes present\n" + sb);
        }
    }
}
