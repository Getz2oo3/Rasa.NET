using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{
    /// <summary>Estimated Frontlines spacing, revised after the October 20 playtest. Frozen migration data.</summary>
    public static class PravusFrontlinesSpacingV4
    {
        public static void Up(MigrationBuilder migration)
        {
            Move(migration, 640202, -80, 25.213156, 10, 10);
            Move(migration, 640203, -90, 36.395542, -100, 12);
            Move(migration, 640204, -60, 25.937, 115, 12);
        }

        public static void Down(MigrationBuilder migration)
        {
            Move(migration, 640202, -110, 27.184092, 10, 7);
            Move(migration, 640203, -75, 25.768618, -20, 9);
            Move(migration, 640204, -35, 21.57058, 20, 9);
        }

        private static void Move(MigrationBuilder migration, uint id, double x, double y, double z, double radius)
        {
            // Explicit invariant SQL supports both SQLite and MySQL without changing counts,
            // enemy definitions, instance respawn policy, or the separate interior room pool.
            migration.Sql(FormattableString.Invariant(
                $"UPDATE spawnpool SET pos_x = {x}, pos_y = {y}, pos_z = {z}, radius = {radius} WHERE id = {id} AND map_context_id = 1430;"));
        }
    }
}
