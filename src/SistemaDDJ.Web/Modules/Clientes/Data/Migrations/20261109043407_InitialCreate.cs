using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaDDJ.Web.Modules.Clientes.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "clientes");

            migrationBuilder.CreateTable(
                name: "beneficio_fidelidad",
                schema: "clientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    costo_puntos = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beneficio_fidelidad", x => x.id);
                    table.CheckConstraint("ck_beneficio_costo_puntos", "costo_puntos > 0");
                });

            migrationBuilder.CreateTable(
                name: "cliente",
                schema: "clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    apellido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    correo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cliente", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_cuenta_cliente",
                schema: "clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    monto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    metodo_pago = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    notas = table.Column<string>(type: "text", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden_servicio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    movimiento_inventario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registrado_por_usuario_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_cuenta_cliente", x => x.id);
                    table.CheckConstraint("ck_cuenta_cargo_referencia", "tipo_movimiento <> 'CARGO' OR ((orden_servicio_id IS NULL) <> (movimiento_inventario_id IS NULL))");
                    table.CheckConstraint("ck_cuenta_monto", "monto > 0");
                    table.CheckConstraint("ck_cuenta_pago_sin_referencia", "tipo_movimiento <> 'PAGO' OR (orden_servicio_id IS NULL AND movimiento_inventario_id IS NULL AND metodo_pago IS NOT NULL)");
                    table.CheckConstraint("ck_cuenta_tipo", "tipo_movimiento IN ('CARGO', 'PAGO')");
                    table.ForeignKey(
                        name: "FK_movimiento_cuenta_cliente_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "clientes",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_fidelidad_cliente",
                schema: "clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    variacion_puntos = table.Column<int>(type: "integer", nullable: false),
                    motivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beneficio_fidelidad_id = table.Column<int>(type: "integer", nullable: true),
                    registrado_por_usuario_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_fidelidad_cliente", x => x.id);
                    table.CheckConstraint("ck_fidelidad_variacion", "variacion_puntos <> 0");
                    table.ForeignKey(
                        name: "FK_movimiento_fidelidad_cliente_beneficio_fidelidad_beneficio_~",
                        column: x => x.beneficio_fidelidad_id,
                        principalSchema: "clientes",
                        principalTable: "beneficio_fidelidad",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_movimiento_fidelidad_cliente_cliente_cliente_id",
                        column: x => x.cliente_id,
                        principalSchema: "clientes",
                        principalTable: "cliente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_cuenta_cliente_cliente_id",
                schema: "clientes",
                table: "movimiento_cuenta_cliente",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_fidelidad_cliente_beneficio_fidelidad_id",
                schema: "clientes",
                table: "movimiento_fidelidad_cliente",
                column: "beneficio_fidelidad_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_fidelidad_cliente_cliente_id",
                schema: "clientes",
                table: "movimiento_fidelidad_cliente",
                column: "cliente_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimiento_cuenta_cliente",
                schema: "clientes");

            migrationBuilder.DropTable(
                name: "movimiento_fidelidad_cliente",
                schema: "clientes");

            migrationBuilder.DropTable(
                name: "beneficio_fidelidad",
                schema: "clientes");

            migrationBuilder.DropTable(
                name: "cliente",
                schema: "clientes");
        }
    }
}
