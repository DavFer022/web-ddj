using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaDDJ.Web.Modules.Personalizacion.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "personalizacion");

            migrationBuilder.CreateSequence<int>(
                name: "orden_servicio_numero_seq",
                schema: "personalizacion");

            migrationBuilder.CreateTable(
                name: "orden_servicio",
                schema: "personalizacion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_orden = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValueSql: "'OS-' || lpad(nextval('personalizacion.orden_servicio_numero_seq')::text, 6, '0')"),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    solicitado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    vence_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    total_cotizado = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_final = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    notas = table.Column<string>(type: "text", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_por_usuario_id = table.Column<int>(type: "integer", nullable: false),
                    asignado_a_usuario_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orden_servicio", x => x.id);
                    table.CheckConstraint("ck_orden_estado", "estado IN ('PENDIENTE', 'EN_PROCESO', 'LISTO', 'ENTREGADO', 'CANCELADO')");
                    table.CheckConstraint("ck_orden_fechas", "vence_en >= solicitado_en");
                    table.CheckConstraint("ck_orden_total_cotizado", "total_cotizado >= 0");
                    table.CheckConstraint("ck_orden_total_final", "total_final IS NULL OR total_final >= 0");
                });

            migrationBuilder.CreateTable(
                name: "tecnica_personalizacion",
                schema: "personalizacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tecnica_personalizacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "detalle_orden_servicio",
                schema: "personalizacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    orden_servicio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    insumo_id = table.Column<int>(type: "integer", nullable: true),
                    descripcion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notas = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalle_orden_servicio", x => x.id);
                    table.CheckConstraint("ck_detalle_orden_cantidad", "cantidad > 0");
                    table.CheckConstraint("ck_detalle_orden_precio", "precio_unitario >= 0");
                    table.ForeignKey(
                        name: "FK_detalle_orden_servicio_orden_servicio_orden_servicio_id",
                        column: x => x.orden_servicio_id,
                        principalSchema: "personalizacion",
                        principalTable: "orden_servicio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "linea_orden_tecnica",
                schema: "personalizacion",
                columns: table => new
                {
                    detalle_orden_servicio_id = table.Column<int>(type: "integer", nullable: false),
                    tecnica_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_linea_orden_tecnica", x => new { x.detalle_orden_servicio_id, x.tecnica_id });
                    table.ForeignKey(
                        name: "FK_linea_orden_tecnica_detalle_orden_servicio_detalle_orden_se~",
                        column: x => x.detalle_orden_servicio_id,
                        principalSchema: "personalizacion",
                        principalTable: "detalle_orden_servicio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_linea_orden_tecnica_tecnica_personalizacion_tecnica_id",
                        column: x => x.tecnica_id,
                        principalSchema: "personalizacion",
                        principalTable: "tecnica_personalizacion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_detalle_orden_servicio_orden_servicio_id",
                schema: "personalizacion",
                table: "detalle_orden_servicio",
                column: "orden_servicio_id");

            migrationBuilder.CreateIndex(
                name: "IX_linea_orden_tecnica_tecnica_id",
                schema: "personalizacion",
                table: "linea_orden_tecnica",
                column: "tecnica_id");

            migrationBuilder.CreateIndex(
                name: "IX_orden_servicio_numero_orden",
                schema: "personalizacion",
                table: "orden_servicio",
                column: "numero_orden",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tecnica_personalizacion_nombre",
                schema: "personalizacion",
                table: "tecnica_personalizacion",
                column: "nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "linea_orden_tecnica",
                schema: "personalizacion");

            migrationBuilder.DropTable(
                name: "detalle_orden_servicio",
                schema: "personalizacion");

            migrationBuilder.DropTable(
                name: "tecnica_personalizacion",
                schema: "personalizacion");

            migrationBuilder.DropTable(
                name: "orden_servicio",
                schema: "personalizacion");

            migrationBuilder.DropSequence(
                name: "orden_servicio_numero_seq",
                schema: "personalizacion");
        }
    }
}
