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
    public class PravusFrontlinesSpacingTests
    {
        private const string Previous = "20261219000000_Pravus_frontlines_population_v3";
        private const string Current = "20261220000000_Pravus_frontlines_spacing_v4";

        [TestMethod]
        public void MigrationMovesOnlyTheThreeOutdoorPoolsAndRollsBackExactly()
        {
            using var database = new OrdinaryLootTestDatabase();
            var db = database.Context;
            var migrator = db.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = Snapshot(db);
            var oldPools = JsonSerializer.Serialize(db.Set<SpawnPoolEntry>().AsNoTracking().OrderBy(p => p.Id).ToArray());
            migrator.Migrate(Current);
            migrator.Migrate(Current);
            Assert.AreEqual(before, Snapshot(db));
            var pools = db.Set<SpawnPoolEntry>().AsNoTracking().Where(p => p.Id >= 640202 && p.Id <= 640204).ToArray();
            Assert.AreEqual(3, pools.Length);
            Assert.IsTrue(pools.All(p => p.Creature1Id == 640201 && p.Creature1MinCount == 4 && p.Creature1MaxCount == 6 && p.RespawnTime == 1200));
            foreach (var a in pools)
                foreach (var b in pools.Where(p => p.Id > a.Id))
                    Assert.IsTrue(Vector2.Distance(new((float)a.PosX, (float)a.PosZ), new((float)b.PosX, (float)b.PosZ))
                        - a.Radius - b.Radius > BehaviorManager.MaxChaseDistance + 18,
                        "Leave room between one group's chase and the next group's detection area.");
            var turrets = db.Set<SpawnPoolEntry>().AsNoTracking().Where(p => p.Id >= 550085 && p.Id <= 550090).ToArray();
            foreach (var pool in pools)
                foreach (var turret in turrets)
                    Assert.IsTrue(Vector2.Distance(new((float)pool.PosX, (float)pool.PosZ), new((float)turret.PosX, (float)turret.PosZ))
                        - pool.Radius > SpawnPoolManager.TurretScan);
            migrator.Migrate(Previous);
            Assert.AreEqual(oldPools, JsonSerializer.Serialize(db.Set<SpawnPoolEntry>().AsNoTracking().OrderBy(p => p.Id).ToArray()));
            Assert.AreEqual(before, Snapshot(db));
        }

        [TestMethod]
        public void MySqlScriptsOnlyUpdateTheThreeScopedPools()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            foreach (var sql in new[] { migrator.GenerateScript(Previous, Current), migrator.GenerateScript(Current, Previous) })
            {
                Assert.AreEqual(3, sql.Split("UPDATE spawnpool SET").Length - 1);
                foreach (var id in new[] { 640202, 640203, 640204 })
                    StringAssert.Contains(sql, $"WHERE id = {id} AND map_context_id = 1430;");
            }
        }

        [TestMethod]
        [DataRow(-80f, 25.213156f, 10f, 10f)]
        [DataRow(-90f, 36.395542f, -100f, 12f)]
        [DataRow(-60f, 25.937f, 115f, 12f)]
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
            Pools = db.Set<SpawnPoolEntry>().AsNoTracking().Where(p => p.Id < 640202 || p.Id > 640204).OrderBy(p => p.Id).ToArray(),
            Creatures = db.Set<CreatureEntry>().AsNoTracking().OrderBy(c => c.Id).ToArray(),
            Stats = db.Set<CreatureStatEntry>().AsNoTracking().OrderBy(c => c.Id).ToArray(),
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
