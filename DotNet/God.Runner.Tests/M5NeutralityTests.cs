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
    /// </summary>
    [Parallelizable(ParallelScope.None)]
    public class M5NeutralityTests
    {
        // seed -> the state hash at the end of days 1..10 (pinned at 6671592)
        private static readonly Dictionary<ulong, ulong[]> Pinned = new Dictionary<ulong, ulong[]>
        {
            [1] = new ulong[] { 0xf35a5d8f16122634UL, 0x2cc1c8ae6e903285UL, 0xa3c5ddcc593396aaUL, 0xef055ae07bf911a0UL, 0x31ee51eca1fb7be7UL, 0x07b2bb530d951cbdUL, 0x09f518e7a67ccce3UL, 0x59364bb177608cc0UL, 0x05d06b7d3ffb281dUL, 0x658d9ff8f1b5d344UL },
            [2] = new ulong[] { 0x3566394ebb79db14UL, 0x075fc69c9df93d04UL, 0x49c8be501904d2bfUL, 0x6f10b93f33237a82UL, 0xab274c43cb6f1ed1UL, 0x8df0da0a59183ee9UL, 0xbde291b9a9647781UL, 0xe4664cbde6e09986UL, 0x2cd5d1543aa9e2aaUL, 0x42ddc5266d094075UL },
            [3] = new ulong[] { 0xfcb7476cf523ae04UL, 0x3971287850663dddUL, 0xb8fe79f1f958cdecUL, 0xaaf48e938f022aeeUL, 0x8a44bb9fb1a88d06UL, 0x37ddfe51be43b420UL, 0x3337e007eaf57d40UL, 0xc2e18b2f061a5af1UL, 0x46348f965af89c42UL, 0x1c94dfbc622a385fUL },
        };

        private static RunResultSeq Run(ulong seed)
        {
            var spec = TestUtil.Spec("band_6x20", seed, 12000, "tuning.Social.Enabled=0");
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
                CollectionAssert.AreEqual(Pinned[seed], h, "seed " + seed + ": with Social.Enabled = 0 the day hashes equal 6671592's");
            }
            TestContext.WriteLine(sb.ToString());
            Assert.AreEqual(3, Pinned.Count, "pinned hashes present\n" + sb);
        }
    }
}
