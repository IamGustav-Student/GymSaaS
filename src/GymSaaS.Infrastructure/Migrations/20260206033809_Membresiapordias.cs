using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Membresiapordias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AccesoDomingo",
                table: "TiposMembresia",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoJueves",
                table: "TiposMembresia",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoLunes",
                table: "TiposMembresia",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoMartes",
                table: "TiposMembresia",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoMiercoles",
                table: "TiposMembresia",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoSabado",
                table: "TiposMembresia",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AccesoViernes",
                table: "TiposMembresia",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccesoDomingo",
                table: "TiposMembresia");

            migrationBuilder.DropColumn(
                name: "AccesoJueves",
                table: "TiposMembresia");

            migrationBuilder.DropColumn(
                name: "AccesoLunes",
                table: "TiposMembresia");

            migrationBuilder.DropColumn(
                name: "AccesoMartes",
                table: "TiposMembresia");

            migrationBuilder.DropColumn(
                name: "AccesoMiercoles",
                table: "TiposMembresia");

            migrationBuilder.DropColumn(
                name: "AccesoSabado",
                table: "TiposMembresia");

            migrationBuilder.DropColumn(
                name: "AccesoViernes",
                table: "TiposMembresia");
        }
    }
}
