using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261217000000_Pravus_historical_npcs_v1")]
    public class Pravus_historical_npcs_v1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => PravusHistoricalNpcsV1.Up(migrationBuilder);
        protected override void Down(MigrationBuilder migrationBuilder) => PravusHistoricalNpcsV1.Down(migrationBuilder);
    }
}
