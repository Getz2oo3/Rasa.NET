using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261218000000_Pravus_enemy_population_v2")]
    public class Pravus_enemy_population_v2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => PravusEnemyPopulationV2.Up(migrationBuilder);
        protected override void Down(MigrationBuilder migrationBuilder) => PravusEnemyPopulationV2.Down(migrationBuilder);
    }
}
