using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Navigation;
using Rasa.Structures.World;

namespace Rasa.Test.Database
{
    [TestClass]
    public class PravusPopulationTests
    {
        private const string Previous = "20261216000000_Bind_control_point_turret_loot";
        private const string Current = "20261217000000_Pravus_historical_npcs_v1";

        [TestMethod]
        public void MySqlCanGenerateUpgradeAndRollbackWithoutConnectingToAServer()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var upgrade = migrator.GenerateScript(Previous, Current);
            StringAssert.Contains(upgrade, "INSERT INTO `creature`");
            StringAssert.Contains(upgrade, "INSERT INTO `creature_appearance`");
            StringAssert.Contains(upgrade, "INSERT INTO `spawnpool`");
            StringAssert.Contains(upgrade, "4286690539");
            Assert.IsFalse(upgrade.Contains("INSERT INTO `mission_"));
            var rollback = migrator.GenerateScript(Current, Previous);
            StringAssert.Contains(rollback, "DELETE FROM creature WHERE id = 640003;");
            StringAssert.Contains(rollback, "DELETE FROM creature_appearance WHERE id = 640001;");
        }

        [TestMethod]
        public void UpgradePlacesDocumentedNpcsWithoutChangingExistingPopulationOrMissionsAndCanRollBack()
        {
            using var database = new OrdinaryLootTestDatabase();
            var db = database.Context;
            var migrator = db.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = db.Set<SpawnPoolEntry>().AsNoTracking().Where(p => p.MapContextId == 1430).OrderBy(p => p.Id).ToArray();
            var missionCount = db.Set<MissionContentDefinitionEntry>().Count();

            migrator.Migrate(Current);
            // A second update does not duplicate the installed NPCs.
            migrator.Migrate(Current);
            var added = db.Set<SpawnPoolEntry>().AsNoTracking().Where(p => p.Id >= 640001 && p.Id <= 640003).OrderBy(p => p.Id).ToArray();
            Assert.AreEqual(3, added.Length);
            CollectionAssert.AreEqual(new[] { -185.0, -268.0, -77.0 }, added.Select(p => p.PosX).ToArray());
            CollectionAssert.AreEqual(new[] { -123.0, -127.0, 176.0 }, added.Select(p => p.PosZ).ToArray());
            var repo = FindRepository();
            var mesh = new NavMeshQuery(NavMeshFile.Read(Path.Combine(repo, "navmesh",
                "adv_foreas_concordia_wilderness_pravusresearch.nav")));
            foreach (var pool in added)
            {
                Assert.AreEqual(1430U, pool.MapContextId);
                var ground = mesh.GroundHeight(pool.Position);
                Assert.IsTrue(ground.HasValue, $"No walkable floor for {pool.Id}");
                Assert.AreEqual(ground.Value, (float)pool.PosY, 0.01f, $"NPC {pool.Id} would stand above/below its floor");
                var creature = db.Set<CreatureEntry>().AsNoTracking().Single(c => c.Id == pool.Creature1Id);
                Assert.AreEqual(0U, creature.WalkSpeed);
                Assert.AreEqual(0U, creature.RunSpeed);
                Assert.IsTrue(db.Set<CreatureAppearanceEntry>().Any(a => a.Id == creature.Id));
                Assert.IsFalse(db.Set<NpcPackageEntry>().Any(n => n.Id == creature.Id), "No quest packages in this population pass");
            }
            Assert.AreEqual(missionCount, db.Set<MissionContentDefinitionEntry>().Count());
            Assert.AreEqual(before.Length + 3, db.Set<SpawnPoolEntry>().Count(p => p.MapContextId == 1430));

            migrator.Migrate(Previous);
            Assert.AreEqual(0, db.Set<CreatureEntry>().Count(c => c.Id >= 640001 && c.Id <= 640003));
            Assert.AreEqual(0, db.Set<CreatureAppearanceEntry>().Count(c => c.Id >= 640001 && c.Id <= 640003));
            var after = db.Set<SpawnPoolEntry>().AsNoTracking().Where(p => p.MapContextId == 1430).OrderBy(p => p.Id).ToArray();
            CollectionAssert.AreEqual(before.Select(p => (p.Id, p.Position, p.Creature1Id)).ToArray(),
                after.Select(p => (p.Id, p.Position, p.Creature1Id)).ToArray());
            Assert.AreEqual(missionCount, db.Set<MissionContentDefinitionEntry>().Count());
        }

        private static string FindRepository()
        {
            for (var directory = new DirectoryInfo(System.AppContext.BaseDirectory); directory != null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                    return directory.FullName;
            throw new DirectoryNotFoundException("Rasa.NET.sln");
        }
    }
}
