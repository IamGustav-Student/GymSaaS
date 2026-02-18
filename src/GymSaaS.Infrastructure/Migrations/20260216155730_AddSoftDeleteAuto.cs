using Microsoft.EntityFrameworkCore.Migrations;

// ============================================================
// ¿QUÉ ES ESTE ARCHIVO?
// Es una "migración" de Entity Framework Core.
// Una migración es básicamente un script que le dice a EF Core
// qué cambios hacer en la base de datos (agregar tablas, columnas, etc.).
//
// ¿POR QUÉ ESTABA ROTO?
// Esta migración fue generada por EF Core pero quedó con los métodos
// Up() y Down() VACÍOS. Eso significa que EF Core la registraba como
// "aplicada" en la tabla __EFMigrationsHistory, pero NUNCA ejecutaba
// ningún SQL. La columna IsDeleted nunca se creaba en la base de datos,
// y al hacer cualquier query a TiposMembresia explotaba con:
// SqlException: "Invalid column name 'IsDeleted'"
//
// ¿QUÉ HACE ESTE FIX?
// Implementamos el Up() con el SQL real para agregar la columna IsDeleted
// a la tabla TiposMembresia. También implementamos Down() para poder
// revertir el cambio si fuera necesario.
// ============================================================

#nullable disable

namespace GymSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteAuto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ¿QUÉ HACE ESTO?
            // Agrega la columna "IsDeleted" (tipo BIT = boolean en SQL Server)
            // a la tabla TiposMembresia.
            //
            // defaultValue: false → todos los registros existentes quedan con
            // IsDeleted = 0 (false), es decir, como NO eliminados.
            // Esto es crítico: si pusieramos defaultValue: true, todos los
            // tipos de membresía existentes quedarían marcados como borrados.
            //
            // IMPORTANTE: Este método se ejecuta UNA SOLA VEZ cuando la app
            // detecta que esta migración no fue aplicada aún en la base de datos.
            // Si la columna ya existe (porque la creaste manualmente con ALTER TABLE),
            // esta migración NO vuelve a ejecutarse gracias al registro en __EFMigrationsHistory.
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
            // ¿QUÉ HACE ESTO?
            // Es el "deshacer" del Up(). Si alguna vez necesitás volver atrás
            // (hacer rollback a la migración anterior), EF Core ejecuta este método.
            // En este caso, simplemente elimina la columna IsDeleted.
            //
            // Para hacer rollback se usa:
            // dotnet ef database update 20260213010930_AddReinicioDeDatos
            // (nombre de la migración ANTERIOR a esta)
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TiposMembresia");
        }
    }
}