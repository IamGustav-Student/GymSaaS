using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GymSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Instructor = table.Column<string>(type: "text", nullable: true),
                    FechaHoraInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DuracionMinutos = table.Column<int>(type: "integer", nullable: false),
                    CupoMaximo = table.Column<int>(type: "integer", nullable: false),
                    CupoReservado = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesPagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    ModoSandbox = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesPagos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ejercicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    GrupoMuscular = table.Column<string>(type: "text", nullable: true),
                    VideoUrl = table.Column<string>(type: "text", nullable: true),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ejercicios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Socios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Apellido = table.Column<string>(type: "text", nullable: false),
                    Dni = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CodigoAcceso = table.Column<string>(type: "text", nullable: false),
                    FotoUrl = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    FechaAlta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaNacimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Socios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposMembresia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric", nullable: false),
                    DuracionDias = table.Column<int>(type: "integer", nullable: false),
                    CantidadClases = table.Column<int>(type: "integer", nullable: true),
                    AccesoLunes = table.Column<bool>(type: "boolean", nullable: false),
                    AccesoMartes = table.Column<bool>(type: "boolean", nullable: false),
                    AccesoMiercoles = table.Column<bool>(type: "boolean", nullable: false),
                    AccesoJueves = table.Column<bool>(type: "boolean", nullable: false),
                    AccesoViernes = table.Column<bool>(type: "boolean", nullable: false),
                    AccesoSabado = table.Column<bool>(type: "boolean", nullable: false),
                    AccesoDomingo = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposMembresia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    SubscriptionPlan = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    HasUsedTrial = table.Column<bool>(type: "boolean", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    WebSiteUrl = table.Column<string>(type: "text", nullable: true),
                    ColorPrimario = table.Column<string>(type: "text", nullable: true),
                    GymNombreDisplay = table.Column<string>(type: "text", nullable: true),
                    MercadoPagoAccessToken = table.Column<string>(type: "text", nullable: true),
                    Latitud = table.Column<double>(type: "double precision", nullable: true),
                    Longitud = table.Column<double>(type: "double precision", nullable: true),
                    RadioPermitidoMetros = table.Column<int>(type: "integer", nullable: false),
                    CodigoQrGym = table.Column<string>(type: "text", nullable: false),
                    TimeZoneId = table.Column<string>(type: "text", nullable: false),
                    Plan = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    MaxSocios = table.Column<int>(type: "integer", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubscriptionEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MercadoPagoSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    MercadoPagoPublicKey = table.Column<string>(type: "text", nullable: true),
                    MercadoPagoUserId = table.Column<string>(type: "text", nullable: true),
                    EmpresaId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenants_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Asistencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SocioId = table.Column<int>(type: "integer", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Permitido = table.Column<bool>(type: "boolean", nullable: false),
                    Detalle = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asistencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asistencias_Socios_SocioId",
                        column: x => x.SocioId,
                        principalTable: "Socios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ListasEspera",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClaseId = table.Column<int>(type: "integer", nullable: false),
                    SocioId = table.Column<int>(type: "integer", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListasEspera", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListasEspera_Clases_ClaseId",
                        column: x => x.ClaseId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ListasEspera_Socios_SocioId",
                        column: x => x.SocioId,
                        principalTable: "Socios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClaseId = table.Column<int>(type: "integer", nullable: false),
                    SocioId = table.Column<int>(type: "integer", nullable: false),
                    FechaReserva = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Asistio = table.Column<bool>(type: "boolean", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentId = table.Column<string>(type: "text", nullable: true),
                    Pagado = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_Clases_ClaseId",
                        column: x => x.ClaseId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservas_Socios_SocioId",
                        column: x => x.SocioId,
                        principalTable: "Socios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rutinas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    SocioId = table.Column<int>(type: "integer", nullable: false),
                    FechaAsignacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rutinas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rutinas_Socios_SocioId",
                        column: x => x.SocioId,
                        principalTable: "Socios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MembresiasSocios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SocioId = table.Column<int>(type: "integer", nullable: false),
                    TipoMembresiaId = table.Column<int>(type: "integer", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClasesRestantes = table.Column<int>(type: "integer", nullable: true),
                    Estado = table.Column<string>(type: "text", nullable: false),
                    PrecioPagado = table.Column<decimal>(type: "numeric", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembresiasSocios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembresiasSocios_Socios_SocioId",
                        column: x => x.SocioId,
                        principalTable: "Socios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MembresiasSocios_TiposMembresia_TipoMembresiaId",
                        column: x => x.TipoMembresiaId,
                        principalTable: "TiposMembresia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    UsuarioId = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RutinaEjercicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RutinaId = table.Column<int>(type: "integer", nullable: false),
                    EjercicioId = table.Column<int>(type: "integer", nullable: false),
                    Series = table.Column<int>(type: "integer", nullable: false),
                    Repeticiones = table.Column<int>(type: "integer", nullable: false),
                    PesoSugerido = table.Column<string>(type: "text", nullable: true),
                    Notas = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutinaEjercicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RutinaEjercicios_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RutinaEjercicios_Rutinas_RutinaId",
                        column: x => x.RutinaId,
                        principalTable: "Rutinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Monto = table.Column<decimal>(type: "numeric", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MetodoPago = table.Column<string>(type: "text", nullable: false),
                    Pagado = table.Column<bool>(type: "boolean", nullable: false),
                    EstadoTransaccion = table.Column<string>(type: "text", nullable: true),
                    IdTransaccionExterna = table.Column<string>(type: "text", nullable: true),
                    EsReintento = table.Column<bool>(type: "boolean", nullable: false),
                    IntentosFallidos = table.Column<int>(type: "integer", nullable: false),
                    ProximoReintento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TokenTarjeta = table.Column<string>(type: "text", nullable: true),
                    SocioId = table.Column<int>(type: "integer", nullable: false),
                    MembresiaSocioId = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_MembresiasSocios_MembresiaSocioId",
                        column: x => x.MembresiaSocioId,
                        principalTable: "MembresiasSocios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Pagos_Socios_SocioId",
                        column: x => x.SocioId,
                        principalTable: "Socios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_SocioId",
                table: "Asistencias",
                column: "SocioId");

            migrationBuilder.CreateIndex(
                name: "IX_ListasEspera_ClaseId",
                table: "ListasEspera",
                column: "ClaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ListasEspera_SocioId",
                table: "ListasEspera",
                column: "SocioId");

            migrationBuilder.CreateIndex(
                name: "IX_MembresiasSocios_SocioId",
                table: "MembresiasSocios",
                column: "SocioId");

            migrationBuilder.CreateIndex(
                name: "IX_MembresiasSocios_TipoMembresiaId",
                table: "MembresiasSocios",
                column: "TipoMembresiaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_MembresiaSocioId",
                table: "Pagos",
                column: "MembresiaSocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_SocioId",
                table: "Pagos",
                column: "SocioId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UsuarioId",
                table: "RefreshTokens",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_ClaseId",
                table: "Reservas",
                column: "ClaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_SocioId",
                table: "Reservas",
                column: "SocioId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaEjercicios_EjercicioId",
                table: "RutinaEjercicios",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaEjercicios_RutinaId",
                table: "RutinaEjercicios",
                column: "RutinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rutinas_SocioId",
                table: "Rutinas",
                column: "SocioId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_EmpresaId",
                table: "Tenants",
                column: "EmpresaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Asistencias");

            migrationBuilder.DropTable(
                name: "ConfiguracionesPagos");

            migrationBuilder.DropTable(
                name: "ListasEspera");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "RutinaEjercicios");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "MembresiasSocios");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Clases");

            migrationBuilder.DropTable(
                name: "Ejercicios");

            migrationBuilder.DropTable(
                name: "Rutinas");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "TiposMembresia");

            migrationBuilder.DropTable(
                name: "Socios");
        }
    }
}
