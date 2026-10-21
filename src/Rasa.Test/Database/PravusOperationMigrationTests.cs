using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Structures.World;

namespace Rasa.Test.Database
{
    [TestClass]
    public class PravusOperationMigrationTests
    {
        private const string Previous = "20261220000000_Pravus_frontlines_spacing_v4";
        private const string Current = "20261221000000_Pravus_operation_v1";

        [TestMethod]
        public void UpgradeAndRollbackPreserveThePreviousWorldAndForeignKeys()
        {
            using var database = new OrdinaryLootTestDatabase();
            var db = database.Context;
            var migrator = db.GetService<IMigrator>();
            migrator.Migrate(Previous);
            var before = Snapshot(db);
            migrator.Migrate(Current);
            migrator.Migrate(Current);
            Assert.AreEqual(6, Scalar(db, "SELECT count(*) FROM mission_content_definition WHERE content_revision='pravus_operation_v1' AND enabled=1"));
            Assert.AreEqual(14, Scalar(db, "SELECT count(*) FROM spawnpool WHERE id BETWEEN 640401 AND 640414"));
            Assert.AreEqual(0, Scalar(db, "SELECT count(*) FROM spawnpool WHERE id IN (560013,560014,560015,560016,640101,640102,640103,640201,640202,640203,640204) AND mode<>2"));
            Assert.AreEqual(0, Scalar(db, "SELECT count(*) FROM pragma_foreign_key_check"));
            migrator.Migrate(Previous);
            Assert.AreEqual(before, Snapshot(db));
            Assert.AreEqual(0, Scalar(db, "SELECT count(*) FROM pragma_foreign_key_check"));
        }

        [TestMethod]
        public void MySqlUpgradeAndRollbackGenerateWithoutAChangingModelDependency()
        {
            using var db = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = db.GetService<IMigrator>();
            var up = migrator.GenerateScript(Previous, Current);
            var down = migrator.GenerateScript(Current, Previous);
            StringAssert.Contains(up, "pravus_operation_v1");
            StringAssert.Contains(up, "11505");
            StringAssert.Contains(up, "45439");
            StringAssert.Contains(down, "DELETE FROM mission_scene_binding");
            StringAssert.Contains(down, "SET mode=0");
        }

        private static long Scalar(DbContext db, string sql)
        {
            db.Database.OpenConnection();
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt64(command.ExecuteScalar());
        }

        private static string Snapshot(DbContext db)
        {
            db.Database.OpenConnection();
            var rows = new List<string>();
            foreach (var table in new[] { "spawnpool", "creature", "creature_stat", "creature_appearance", "creature_action", "npc_package",
                "creature_loot_binding", "loot_map_policy", "mission_content_definition", "mission_scene_binding", "mission_prerequisite",
                "mission_objective_definition", "mission_objective_transition", "mission_action", "mission_trigger", "mission_reward_definition",
                "mission_reward_item", "mission_area", "mission_indicator", "mission_evidence" })
            {
                using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = $"SELECT * FROM {table}";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var values = new object[reader.FieldCount];
                    reader.GetValues(values);
                    rows.Add(table + ":" + JsonSerializer.Serialize(values));
                }
            }
            return string.Join('\n', rows.OrderBy(row => row, StringComparer.Ordinal));
        }
    }
}
