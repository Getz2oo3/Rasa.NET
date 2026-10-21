using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Historically documented Linker passage and entrance guards, with estimated spacing,
    /// density and level-10 tuning. See docs/pravus-population.txt for provenance.
    /// Frozen migration data; corrections belong in a subsequent paired World migration.
    /// </summary>
    public static class PravusEnemyPopulationV2
    {
        public static void Up(MigrationBuilder migration)
        {
            // Stock regular Linker class/name/movement and creature 86's stat baseline.
            // Level 10 matches the existing Pravus Thrax; it is a restoration estimate.
            Insert(migration, "creature", new[] {
                "id", "comment", "class_id", "faction", "level", "max_hp", "name_id",
                "run_speed", "walk_speed", "action1", "action2", "action3", "action4",
                "action5", "action6", "action7", "action8"
            }, new object[,] {
                { 640101U, "Linker - Pravus restoration", 9751U, 0U, 10U, 750U, 419U,
                    9U, 5U, 640101U, 640102U, 640103U, 640104U, 0U, 0U, 0U, 0U }
            });
            Insert(migration, "creature_stat", new[] { "id", "body", "mind", "spirit", "health", "armor" },
                new object[,] { { 640101U, 15, 15, 15, 750, 175 } });

            // Native Linker abilities and timings used by the existing world Linkers.
            // Damage is estimated from the low-level Linker actions 37..40 (15..25),
            // not copied from level-40/42 Linkers. Channel has no direct damage.
            Insert(migration, "creature_action", new[] {
                "id", "description", "action_id", "action_arg_id", "range_min", "range_max",
                "cooldown", "windup", "min_damage", "max_damage", "damage_type"
            }, new object[,] {
                { 640101U, "Pravus Linker chest blast", 263U, 1U, 1.0, 30.0, 20000U, 7333U, 30U, 45U, 1U },
                { 640102U, "Pravus Linker hand blast", 412U, 1U, 1.0, 30.0, 5000U, 566U, 15U, 25U, 1U },
                { 640103U, "Pravus Linker ground blast", 264U, 1U, 1.0, 30.0, 5000U, 2633U, 15U, 25U, 1U },
                { 640104U, "Pravus Linker channel", 410U, 1U, 0.0, 40.0, 20000U, 5666U, 0U, 0U, 1U }
            });

            var columns = new[] {
                "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id",
                "creature_1_Id", "creature_1_min_count", "creature_1_max_count",
                "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
                "creature_3_Id", "creature_3_min_count", "creature_3_max_count",
                "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
                "creature_5_Id", "creature_5_min_count", "creature_5_max_count",
                "creature_6_Id", "creature_6_min_count", "creature_6_max_count", "radius"
            };
            // Times are tenths of a second. SquadInstances overrides them with its
            // cleared-pool policy (45 minutes by default), as for all existing pools.
            // Small connected footprints keep this population on the surveyed floor.
            Insert(migration, "spawnpool", columns, new object[,] {
                { 640101U, (byte)0, (byte)0, 1800U, 139.6989, 17.326593, 98.389595, 0.0, 1430U,
                    640101U, (byte)3, (byte)3, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 5.0 },
                { 640102U, (byte)0, (byte)0, 1200U, 155.0, 42.312, -8.0, 4.712389, 1430U,
                    560001U, (byte)3, (byte)4, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 4.0 },
                { 640103U, (byte)0, (byte)0, 1200U, 170.0, 41.937004, -8.0, 4.712389, 1430U,
                    560001U, (byte)2, (byte)3, 560002U, (byte)1, (byte)1, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 4.0 }
            });
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var id in new[] { 640101U, 640102U, 640103U })
                migration.Sql($"DELETE FROM spawnpool WHERE id = {id};");
            migration.Sql("DELETE FROM creature_stat WHERE id = 640101;");
            migration.Sql("DELETE FROM creature WHERE id = 640101;");
            foreach (var id in new[] { 640101U, 640102U, 640103U, 640104U })
                migration.Sql($"DELETE FROM creature_action WHERE id = {id};");
        }

        private static void Insert(MigrationBuilder migration, string table, string[] columns, object[,] values)
        {
            var sqlite = migration.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";
            var types = new string[columns.Length];
            for (var i = 0; i < types.Length; i++)
                types[i] = values[0, i] switch {
                    string => sqlite ? "TEXT" : "varchar(50)",
                    double => sqlite ? "REAL" : "double",
                    byte => sqlite ? "INTEGER" : "tinyint unsigned",
                    int => sqlite ? "INTEGER" : "int",
                    _ => sqlite ? "INTEGER" : "int unsigned"
                };
            migration.InsertData(table: table, columns: columns, columnTypes: types, values: values);
        }
    }
}
