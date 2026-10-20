using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Three documented Pravus NPC locations, without conversations or mission bindings.
    /// Ellatha records 69/70/71 give species and XYZ; client 1.16.5.0
    /// creaturenamelanguage gives name IDs 4843/205/204. X/Z are unchanged;
    /// Y is corrected to the checked-in Pravus walkable surface.
    /// See docs/pravus-population.txt for evidence and the limits of this first pass.
    /// Frozen data: future corrections belong in a new paired World migration.
    /// </summary>
    public static class PravusHistoricalNpcsV1
    {
        public static void Up(MigrationBuilder migration)
        {
            var creatureColumns = new[] {
                "id", "comment", "class_id", "faction", "level", "max_hp", "name_id",
                "run_speed", "walk_speed", "action1", "action2", "action3",
                "action4", "action5", "action6", "action7", "action8"
            };
            Insert(migration, "creature", creatureColumns, new object[,] {
                { 640001U, "Ranger Nylla - Pravus", 7034U, 1U, 10U, 1000U, 4843U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U },
                { 640002U, "Cmd. Sgt. Price - Pravus", 3846U, 1U, 10U, 1000U, 205U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U },
                { 640003U, "Field Lt. Perkins - Pravus", 3846U, 1U, 10U, 1000U, 204U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U, 0U }
            });

            // Existing stock Forean ranger / AFS officer presentation. These are not
            // claims that the original NPC equipment, colors or combat stats survived.
            Insert(migration, "creature_appearance", new[] { "id", "slot_id", "Class_id", "color" },
                new object[,] {
                    { 640001U, 13U, 10532U, 1U },
                    { 640002U, 17U, 24008U, 4286690539U },
                    { 640002U, 13U, 6271U, 1U },
                    { 640002U, 14U, 9781U, 4278655809U },
                    { 640002U, 15U, 4023U, 4294934528U },
                    { 640002U, 16U, 4022U, 4294934528U },
                    { 640002U, 2U, 4021U, 4294934528U },
                    { 640003U, 17U, 24008U, 4286690539U },
                    { 640003U, 13U, 6271U, 1U },
                    { 640003U, 14U, 9781U, 4278655809U },
                    { 640003U, 15U, 4023U, 4294934528U },
                    { 640003U, 16U, 4022U, 4294934528U },
                    { 640003U, 2U, 4021U, 4294934528U }
                });

            var poolColumns = new[] {
                "id", "mode", "anim_type", "respown_time", "pos_x", "pos_y", "pos_z", "rotation", "map_context_id",
                "creature_1_Id", "creature_1_min_count", "creature_1_max_count",
                "creature_2_Id", "creature_2_min_count", "creature_2_max_count",
                "creature_3_Id", "creature_3_min_count", "creature_3_max_count",
                "creature_4_Id", "creature_4_min_count", "creature_4_max_count",
                "creature_5_Id", "creature_5_min_count", "creature_5_max_count",
                "creature_6_Id", "creature_6_min_count", "creature_6_max_count", "radius"
            };
            Insert(migration, "spawnpool", poolColumns, new object[,] {
                { 640001U, (byte)0, (byte)0, 20U, -185.0, 5.137001, -123.0, 0.0, 1430U,
                    640001U, (byte)1, (byte)1, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0.0 },
                { 640002U, (byte)0, (byte)0, 20U, -268.0, 7.159849, -127.0, 0.0, 1430U,
                    640002U, (byte)1, (byte)1, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0.0 },
                { 640003U, (byte)0, (byte)0, 20U, -77.0, 32.020172, 176.0, 0.0, 1430U,
                    640003U, (byte)1, (byte)1, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0,
                    0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0U, (byte)0, (byte)0, 0.0 }
            });
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var id in new[] { 640001U, 640002U, 640003U })
            {
                // Appearance has no key in the EF model; delete only our allocated IDs.
                migration.Sql($"DELETE FROM creature_appearance WHERE id = {id};");
                migration.Sql($"DELETE FROM spawnpool WHERE id = {id};");
                migration.Sql($"DELETE FROM creature WHERE id = {id};");
            }
        }

        private static void Insert(MigrationBuilder migration, string table, string[] columns, object[,] values)
        {
            // A data-only migration has no generated model. Explicit store types let
            // both providers generate its INSERT statements without a target snapshot.
            var sqlite = migration.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite";
            var types = new string[columns.Length];
            for (var i = 0; i < types.Length; i++)
                types[i] = values[0, i] switch {
                    string => sqlite ? "TEXT" : "varchar(50)",
                    double => sqlite ? "REAL" : "double",
                    byte => sqlite ? "INTEGER" : "tinyint unsigned",
                    _ => sqlite ? "INTEGER" : "int unsigned"
                };
            migration.InsertData(table: table, columns: columns, columnTypes: types, values: values);
        }
    }
}
