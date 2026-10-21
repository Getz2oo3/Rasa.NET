using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261219000000_Pravus_frontlines_population_v3")]
    public class Pravus_frontlines_population_v3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => PravusFrontlinesPopulationV3.Up(migrationBuilder);
        protected override void Down(MigrationBuilder migrationBuilder) => PravusFrontlinesPopulationV3.Down(migrationBuilder);
    }
}
