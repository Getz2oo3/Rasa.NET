using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261220000000_Pravus_frontlines_spacing_v4")]
    public class Pravus_frontlines_spacing_v4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) => PravusFrontlinesSpacingV4.Up(migrationBuilder);
        protected override void Down(MigrationBuilder migrationBuilder) => PravusFrontlinesSpacingV4.Down(migrationBuilder);
    }
}
