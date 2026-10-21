using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Frontlines Machina and the room beyond the spiral stairs. Estimated population;
    /// the user supplied the room center, and historical sources identify Frontlines Machina.
    /// Frozen migration data; see docs/pravus-population.txt for sources and tuning.
    /// </summary>
    public static class PravusFrontlinesPopulationV3
    {
        public static void Up(MigrationBuilder migration)
        {
            // Regular Machina 8 supplies the class, movement, weapon and self-revive.
            // It is friendly in the legacy seed: use a separate hostile definition here.
            // Level 10 matches existing Pravus enemies; 555 HP uses its max_hp baseline,
            // resolving the legacy definition's inconsistent 120 creature_stat.health.
            Insert(migration, "creature", new[] {
                "id", "comment", "class_id", "faction", "level", "max_hp", "name_id",
                "run_speed", "walk_speed", "action1", "action2", "action3", "action4",
                "action5", "action6", "action7", "action8"
            }, new object[,] {
                { 640201U, "Hominis Machina - Pravus Frontlines", 3868U, 0U, 10U, 555U, 0U,
                    9U, 5U, 72002U, 62059U, 0U, 0U, 0U, 0U, 0U, 0U }
            });
            Insert(migration, "creature_stat", new[] { "id", "body", "mind", "spirit", "health", "armor" },
                new object[,] { { 640201U, 15, 15, 15, 555, 70 } });

            var columns = new[] {
                "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id",
                "creature_1_Id", "creature_1_min_count", "creature_1_max_count",
                "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
                "creature_3_Id", "creature_3_min_count", "creature_3_max_count",
                "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
                "creature_5_Id", "creature_5_min_count", "creature_5_max_count",
                "creature_6_Id", "creature_6_min_count", "creature_6_max_count", "radius"
            };
            // Respawn values are tenths of a second; squad-instance policy still overrides
            // these (45 minutes after clearing a pool by default). No patrols or waves.
            Insert(migration, "spawnpool", columns, new object[,] {
                { 640201U, (byte)0, (byte)0, 1200U, 223.9844, 9.703304, -7.7422, 1.570796, 1430U,
                    560001U, (byte)2, (byte)3, 560002U, (byte)1, (byte)1, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 6.0 },
                { 640202U, (byte)0, (byte)0, 1200U, -110.0, 27.184092, 10.0, 1.570796, 1430U,
                    640201U, (byte)4, (byte)6, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 7.0 },
                { 640203U, (byte)0, (byte)0, 1200U, -75.0, 25.768618, -20.0, 1.570796, 1430U,
                    640201U, (byte)4, (byte)6, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 9.0 },
                { 640204U, (byte)0, (byte)0, 1200U, -35.0, 21.57058, 20.0, 1.570796, 1430U,
                    640201U, (byte)4, (byte)6, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 9.0 }
            });
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var id in new[] { 640201U, 640202U, 640203U, 640204U })
                migration.Sql($"DELETE FROM spawnpool WHERE id = {id};");
            migration.Sql("DELETE FROM creature_stat WHERE id = 640201;");
            migration.Sql("DELETE FROM creature WHERE id = 640201;");
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
