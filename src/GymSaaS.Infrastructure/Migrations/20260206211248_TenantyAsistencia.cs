using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TenantyAsistencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoQrGym",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Latitud",
                table: "Tenants",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitud",
                table: "Tenants",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RadioPermitidoMetros",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Asistencias",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoQrGym",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Latitud",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Longitud",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RadioPermitidoMetros",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Asistencias");
        }
    }
}
