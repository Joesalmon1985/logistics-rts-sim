using System.IO;
using System.Linq;
using God.Runner.Warehouse;
using NUnit.Framework;

namespace God.Runner.Tests
{
    /// <summary>
    /// LEAD_RULINGS R64 (Checkpoint B): migrations merge out of number order (0009 after 0012/0015/0026), so the runner
    /// applies every migration whose version is missing from <c>meta_schema_version</c>, not only those above the maximum.
    /// </summary>
    [TestFixture]
    public sealed class MigrationSetMembershipTests
    {
        [Test]
        public void MissingLowerMigration_IsApplied_R64()
        {
            var all = WarehouseDb.Migrations();
            Assert.IsTrue(all.Any(m => m.version == 9) && all.Max(m => m.version) > 9, "the fixture needs 0009 below a later migration");
            string path = Path.Combine(Path.GetTempPath(), "r64_" + System.Guid.NewGuid().ToString("N") + ".sqlite");
            try
            {
                // a warehouse made by a runner that did not yet have 0009 (but had 0012, 0015, 0026)
                using (var db = WarehouseDb.OpenMigrate(path, all.Where(m => m.version != 9)))
                {
                    Assert.IsFalse(db.AppliedVersions().Contains(9));
                    Assert.AreEqual(all.Max(m => m.version), db.SchemaVersion(), "the maximum is already the current version");
                    Assert.IsFalse(db.TableNames().Contains("fact_divine_command"));
                }
                using (var db = WarehouseDb.OpenMigrate(path))
                {
                    CollectionAssert.AreEqual(new[] { 9 }, db.LastApplied, "only the missing migration is applied");
                    CollectionAssert.AreEquivalent(all.Select(m => m.version).ToArray(), db.AppliedVersions().ToArray());
                    Assert.IsTrue(db.TableNames().Contains("fact_divine_command"), "0009's tables exist");
                }
                using (var db = WarehouseDb.OpenMigrate(path)) Assert.IsEmpty(db.LastApplied, "nothing left to apply");
            }
            finally
            {
                foreach (var f in new[] { path, path + "-wal", path + "-shm" }) if (File.Exists(f)) File.Delete(f);
            }
        }
    }
}
