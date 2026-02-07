using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NotificacionReintentoDePago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsReintento",
                table: "Pagos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EstadoTransaccion",
                table: "Pagos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdTransaccionExterna",
                table: "Pagos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntentosFallidos",
                table: "Pagos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Pagado",
                table: "Pagos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProximoReintento",
                table: "Pagos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenTarjeta",
                table: "Pagos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsReintento",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "EstadoTransaccion",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "IdTransaccionExterna",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "IntentosFallidos",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "Pagado",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "ProximoReintento",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "TokenTarjeta",
                table: "Pagos");
        }
    }
}
