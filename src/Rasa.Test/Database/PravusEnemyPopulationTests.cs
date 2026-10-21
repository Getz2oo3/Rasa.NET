using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
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
    public class PravusEnemyPopulationTests
    {
        private const string Previous = "20261217000000_Pravus_historical_npcs_v1";
        private const string Current = "20261218000000_Pravus_enemy_population_v2";

        [TestMethod]
        public void MySqlGeneratesBothDirectionsWithoutChangingExistingCreatures()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Current);
            foreach (var table in new[] { "creature", "creature_stat", "creature_action", "spawnpool" })
                StringAssert.Contains(up, $"INSERT INTO `{table}`");
            Assert.IsFalse(up.Contains("UPDATE "));
            Assert.IsFalse(up.Contains("INSERT INTO `mission_"));
            var down = migrator.GenerateScript(Current, Previous);
            StringAssert.Contains(down, "DELETE FROM creature WHERE id = 640101;");
            StringAssert.Contains(down, "DELETE FROM creature_action WHERE id = 640104;");
            Assert.IsFalse(down.Contains("DELETE FROM creature WHERE id = 560001"));
        }

        [TestMethod]
        public void UpgradeHasCombatReadyPopulationOnReachableFloorsAndRollbackPreservesBaseline()
        {
            using var database = new OrdinaryLootTestDatabase();
            var db = database.Context;
            var migrator = db.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = Snapshot(db);
            var missions = db.Set<MissionContentDefinitionEntry>().Count();
            var baselinePools = db.Set<SpawnPoolEntry>().Count(p => p.MapContextId == 1430);

            migrator.Migrate(Current);
            migrator.Migrate(Current);
            var pools = db.Set<SpawnPoolEntry>().AsNoTracking()
                .Where(p => p.Id >= 640101 && p.Id <= 640103).OrderBy(p => p.Id).ToArray();
            Assert.AreEqual(3, pools.Length);
            Assert.AreEqual(baselinePools + 3, db.Set<SpawnPoolEntry>().Count(p => p.MapContextId == 1430));
            Assert.AreEqual(before, Snapshot(db), "Existing creatures, actions, stats and pools must remain unchanged");
            Assert.AreEqual(missions, db.Set<MissionContentDefinitionEntry>().Count());

            // Bounds describe the whole encounter, not just the primary slot.
            Assert.AreEqual(3, pools[0].Creature1MinCount);
            Assert.AreEqual(3, pools[0].Creature1MaxCount);
            Assert.AreEqual(6, pools.Skip(1).Sum(p => p.Creature1MinCount + p.Creature2MinCount));
            Assert.AreEqual(8, pools.Skip(1).Sum(p => p.Creature1MaxCount + p.Creature2MaxCount));
            CollectionAssert.AreEqual(new uint[] { 1800, 1200, 1200 }, pools.Select(p => p.RespawnTime).ToArray());

            var mesh = new NavMeshQuery(NavMeshFile.Read(Path.Combine(FindRepository(), "navmesh",
                "adv_foreas_concordia_wilderness_pravusresearch.nav")));
            foreach (var pool in pools)
            {
                Assert.AreEqual(1430U, pool.MapContextId);
                Assert.AreEqual((byte)0, pool.Mode);
                Assert.AreEqual((byte)0, pool.AnimType);
                var nearest = mesh.Nearest(pool.Position);
                Assert.IsTrue(nearest.HasValue);
                Assert.IsTrue(Vector3.Distance(nearest.Value, pool.Position) < 0.05f);
                var surveyed = pool.Id == 640101 ? new Vector3(140, 17.1992f, 98) : new Vector3(165, 41.918f, -8);
                mesh.FindPath(pool.Position, surveyed, out var complete);
                Assert.IsTrue(complete, $"Pool {pool.Id} cannot reach the surveyed passage/entrance");
                foreach (var id in new[] { pool.Creature1Id, pool.Creature2Id }.Where(id => id != 0))
                {
                    var creature = db.Set<CreatureEntry>().AsNoTracking().Single(c => c.Id == id);
                    Assert.AreEqual(10U, creature.Level);
                    Assert.AreEqual(0U, creature.Faction);
                    Assert.IsTrue(creature.RunSpeed > 0);
                }
            }

            var linker = db.Set<CreatureEntry>().AsNoTracking().Single(c => c.Id == 640101);
            var stats = db.Set<CreatureStatEntry>().AsNoTracking().Single(c => c.Id == linker.Id);
            Assert.AreEqual(linker.MaxHitPoints, (uint)stats.Health);
            var actions = db.Set<CreatureActionEntry>().AsNoTracking()
                .Where(a => a.Id >= 640101 && a.Id <= 640104).OrderBy(a => a.Id).ToArray();
            CollectionAssert.AreEqual(new uint[] { 263, 412, 264, 410 }, actions.Select(a => a.ActionId).ToArray());
            Assert.AreEqual(0U, actions[3].MaxDamage, "Channel must not deal a second direct hit");
            Assert.IsTrue(actions.Take(3).All(a => a.MinDamage > 0 && a.MaxDamage <= 45));
            Assert.IsFalse(db.Set<NpcPackageEntry>().Any(n => n.Id == linker.Id));

            migrator.Migrate(Previous);
            Assert.AreEqual(before, Snapshot(db));
            Assert.AreEqual(baselinePools, db.Set<SpawnPoolEntry>().Count(p => p.MapContextId == 1430));
            Assert.AreEqual(0, db.Set<CreatureEntry>().Count(c => c.Id == 640101));
            Assert.AreEqual(0, db.Set<CreatureStatEntry>().Count(c => c.Id == 640101));
            Assert.AreEqual(0, db.Set<CreatureActionEntry>().Count(c => c.Id >= 640101 && c.Id <= 640104));
        }

        private static string Snapshot(DbContext db) => JsonSerializer.Serialize(new {
            Pools = db.Set<SpawnPoolEntry>().AsNoTracking().Where(p => p.Id < 640101 || p.Id > 640103).OrderBy(p => p.Id).ToArray(),
            Creatures = db.Set<CreatureEntry>().AsNoTracking().Where(c => c.Id != 640101).OrderBy(c => c.Id).ToArray(),
            Stats = db.Set<CreatureStatEntry>().AsNoTracking().Where(c => c.Id != 640101).OrderBy(c => c.Id).ToArray(),
            Actions = db.Set<CreatureActionEntry>().AsNoTracking().Where(c => c.Id < 640101 || c.Id > 640104).OrderBy(c => c.Id).ToArray()
        });

        private static string FindRepository()
        {
            for (var directory = new DirectoryInfo(System.AppContext.BaseDirectory); directory != null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                    return directory.FullName;
            throw new DirectoryNotFoundException("Rasa.NET.sln");
        }
    }
}
