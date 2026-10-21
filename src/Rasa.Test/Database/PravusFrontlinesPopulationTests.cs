using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Managers;
using Rasa.Navigation;
using Rasa.Structures.World;

namespace Rasa.Test.Database
{
    [TestClass]
    public class PravusFrontlinesPopulationTests
    {
        private const string Previous = "20261218000000_Pravus_enemy_population_v2";
        private const string Current = "20261219000000_Pravus_frontlines_population_v3";

        [TestMethod]
        public void MySqlGeneratesUpgradeAndRollbackWithoutChangingSharedMachina()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Current);
            foreach (var table in new[] { "creature", "creature_stat", "spawnpool" })
                StringAssert.Contains(up, $"INSERT INTO `{table}`");
            Assert.IsFalse(up.Contains("UPDATE "));
            Assert.IsFalse(up.Contains("INSERT INTO `mission_"));
            var down = migrator.GenerateScript(Current, Previous);
            StringAssert.Contains(down, "DELETE FROM spawnpool WHERE id = 640204;");
            StringAssert.Contains(down, "DELETE FROM creature WHERE id = 640201;");
            Assert.IsFalse(down.Contains("DELETE FROM creature_action"));
        }

        [TestMethod]
        public void UpgradeAddsHostileMachinaAndRoomGuardsAndPreservesExistingContentOnRollback()
        {
            using var database = new OrdinaryLootTestDatabase();
            var db = database.Context;
            var migrator = db.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = Snapshot(db);
            var missionCount = db.Set<MissionContentDefinitionEntry>().Count();
            var count = db.Set<SpawnPoolEntry>().Count(p => p.MapContextId == 1430);
            migrator.Migrate(Current);
            migrator.Migrate(Current);
            Assert.AreEqual(before, Snapshot(db));
            Assert.AreEqual(missionCount, db.Set<MissionContentDefinitionEntry>().Count());
            Assert.AreEqual(count + 4, db.Set<SpawnPoolEntry>().Count(p => p.MapContextId == 1430));
            var pools = db.Set<SpawnPoolEntry>().AsNoTracking()
                .Where(p => p.Id >= 640201 && p.Id <= 640204).OrderBy(p => p.Id).ToArray();
            Assert.AreEqual(15, pools.Sum(p => p.Creature1MinCount + p.Creature2MinCount));
            Assert.AreEqual(22, pools.Sum(p => p.Creature1MaxCount + p.Creature2MaxCount));
            Assert.IsTrue(pools.All(p => p.MapContextId == 1430 && p.Mode == 0 && p.RespawnTime == 1200));
            var room = pools[0];
            var landing = new Vector3(204.8359f, 9.6133f, -7.7148f);
            Assert.IsTrue(Vector3.Distance(room.Position, landing) - room.Radius > 13,
                "The player reported the entrance first; mobs belong in the room beyond it");
            var turrets = db.Set<SpawnPoolEntry>().AsNoTracking().Where(p => p.Id >= 550085 && p.Id <= 550090).ToArray();
            foreach (var pool in pools.Skip(1))
                foreach (var turret in turrets)
                    Assert.IsTrue(Vector2.Distance(new((float)pool.PosX, (float)pool.PosZ),
                        new((float)turret.PosX, (float)turret.PosZ)) - pool.Radius >= SpawnPoolManager.TurretScan,
                        $"Pool {pool.Id} can be farmed by turret {turret.Id}");
            var machina = db.Set<CreatureEntry>().AsNoTracking().Single(c => c.Id == 640201);
            Assert.AreEqual(0U, machina.Faction, "Legacy Machina 8 is friendly; do not reuse it as a hostile spawn");
            Assert.AreEqual(10U, machina.Level);
            Assert.AreEqual(3868U, machina.ClassId);
            Assert.AreEqual(machina.MaxHitPoints, (uint)db.Set<CreatureStatEntry>().Single(s => s.Id == machina.Id).Health);
            var weapon = db.Set<CreatureActionEntry>().Single(a => a.Id == machina.Action1);
            Assert.AreEqual(1U, weapon.ActionId);
            Assert.AreEqual(97U, weapon.ActionArgId);
            Assert.AreEqual(429U, db.Set<CreatureActionEntry>().Single(a => a.Id == machina.Action2).ActionId);
            migrator.Migrate(Previous);
            Assert.AreEqual(before, Snapshot(db));
            Assert.AreEqual(count, db.Set<SpawnPoolEntry>().Count(p => p.MapContextId == 1430));
            Assert.IsFalse(db.Set<CreatureEntry>().Any(c => c.Id == 640201));
            Assert.IsFalse(db.Set<CreatureStatEntry>().Any(c => c.Id == 640201));
        }

        [TestMethod]
        [DataRow(223.9844f, 9.703304f, -7.7422f, 6f)]
        [DataRow(-110f, 27.184092f, 10f, 7f)]
        [DataRow(-75f, 25.768618f, -20f, 9f)]
        [DataRow(-35f, 21.57058f, 20f, 9f)]
        public void ScatterStaysWithinFootprintOnReachableGround(float x, float y, float z, float radius)
        {
            var mesh = TestNavMeshes.Query(Path.Combine(FindRepository(), "navmesh",
                "adv_foreas_concordia_wilderness_pravusresearch.nav"));
            var center = new Vector3(x, y, z);
            Assert.IsTrue(Vector3.Distance(center, mesh.Nearest(center).Value) < 0.05f);
            for (var i = 0; i < 200; i++)
            {
                var sample = mesh.RandomPointAround(center, radius);
                Assert.IsTrue(sample.HasValue);
                Assert.IsTrue(Vector2.Distance(new(x, z), new(sample.Value.X, sample.Value.Z)) <= radius + 0.01f,
                    $"Sample {sample} is outside the authored footprint");
                mesh.FindPath(center, sample.Value, out var complete);
                Assert.IsTrue(complete);
                if (x > 200)
                {
                    Assert.IsTrue(sample.Value.Y > 9 && sample.Value.Y < 11, "Wrong floor in the stacked interior");
                    Assert.IsTrue(Vector3.Distance(sample.Value, new(204.8359f, 9.6133f, -7.7148f)) > 13);
                }
            }
        }

        private static string Snapshot(DbContext db) => JsonSerializer.Serialize(new {
            Pools = db.Set<SpawnPoolEntry>().AsNoTracking().Where(p => p.Id < 640201 || p.Id > 640204).OrderBy(p => p.Id).ToArray(),
            Creatures = db.Set<CreatureEntry>().AsNoTracking().Where(c => c.Id != 640201).OrderBy(c => c.Id).ToArray(),
            Stats = db.Set<CreatureStatEntry>().AsNoTracking().Where(c => c.Id != 640201).OrderBy(c => c.Id).ToArray(),
            Actions = db.Set<CreatureActionEntry>().AsNoTracking().OrderBy(c => c.Id).ToArray()
        });

        private static string FindRepository()
        {
            for (var directory = new DirectoryInfo(System.AppContext.BaseDirectory); directory != null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln"))) return directory.FullName;
            throw new DirectoryNotFoundException("Rasa.NET.sln");
        }
    }
}
