using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;
using Rasa.Services.Preloader.Missions.Pravus;

namespace Rasa.Migrations.MySqlWorld
{
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261221000000_Pravus_operation_v1")]
    public class Pravus_operation_v1 : Migration
    {
        protected override void Up(MigrationBuilder migration) => PravusOperationV1.Up(migration);
        protected override void Down(MigrationBuilder migration) => PravusOperationV1.Down(migration);
    }
}
