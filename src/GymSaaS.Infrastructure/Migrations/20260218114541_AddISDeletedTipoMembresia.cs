using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddISDeletedTipoMembresia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PROBLEMA ORIGINAL: Este método estaba vacío.
            // EF Core registraba la migración como "aplicada" en la tabla __EFMigrationsHistory
            // pero NUNCA ejecutaba ningún SQL, por lo que la columna IsDeleted
            // jamás se creó en la tabla TiposMembresia.
            // Resultado: SqlException "Invalid column name 'IsDeleted'" al hacer cualquier query.

            // SOLUCIÓN: Agregamos la columna con valor por defecto false (0),
            // para que todos los registros existentes queden como "no eliminados".
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TiposMembresia",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revierte el cambio: elimina la columna si se hace rollback de esta migración.
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TiposMembresia");
        }
    }
}