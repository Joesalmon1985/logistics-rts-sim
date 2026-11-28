using System;
using System.IO;
using God.Runner.Tests.Support;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>UN6: the committed test artifacts are exactly the generator's output (set UN6_WRITE_ARTIFACTS=1 to regenerate).</summary>
    public class UN6ArtifactFilesTests
    {
        [Test]
        public void Artifact_TestFilesReproducible()
        {
            var d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !Directory.Exists(Path.Combine(d.FullName, "DotNet", "God.Runner.Tests"))) d = d.Parent;
            string dir = Path.Combine(d.FullName, "DotNet", "God.Runner.Tests", "Support");
            bool write = Environment.GetEnvironmentVariable("UN6_WRITE_ARTIFACTS") == "1";
            foreach (int v in UN6TestArtifacts.Versions)
            {
                string path = Path.Combine(dir, UN6TestArtifacts.FileName(v)), text = UN6TestArtifacts.Text(v);
                if (write) File.WriteAllText(path, text, new System.Text.UTF8Encoding(false));
                Assert.IsTrue(File.Exists(path), path);
                Assert.AreEqual(text, File.ReadAllText(path).Replace("\r\n", "\n"), Path.GetFileName(path));
            }
        }
    }
}
