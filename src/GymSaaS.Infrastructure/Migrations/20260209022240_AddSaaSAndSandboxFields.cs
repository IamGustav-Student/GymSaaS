using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaaSAndSandboxFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxSocios",
                table: "Tenants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MercadoPagoSubscriptionId",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Plan",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionEndsAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndsAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAlta",
                table: "Socios",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaNacimiento",
                table: "Socios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ModoSandbox",
                table: "ConfiguracionesPagos",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxSocios",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MercadoPagoSubscriptionId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Plan",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SubscriptionEndsAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TrialEndsAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FechaAlta",
                table: "Socios");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "Socios");

            migrationBuilder.DropColumn(
                name: "ModoSandbox",
                table: "ConfiguracionesPagos");
        }
    }
}
