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
            // Esta migración quedó duplicada: la columna "IsDeleted" en TiposMembresia
            // ya la agrega la migración anterior 20260216155730_AddSoftDeleteAuto.
            // Un intento previo de "arreglar" esta migración (que originalmente tenía
            // Up()/Down() vacíos) le agregó el mismo AddColumn, lo que rompe
            // cualquier base de datos nueva con "Column names in each table must be
            // unique" al aplicar migraciones desde cero. Se deja como no-op:
            // en bases ya migradas esta migración ya figura como aplicada en
            // __EFMigrationsHistory y no se re-ejecuta, así que no hay nada que hacer.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op — ver comentario en Up(). El DropColumn real vive en el Down()
            // de 20260216155730_AddSoftDeleteAuto.
        }
    }
}