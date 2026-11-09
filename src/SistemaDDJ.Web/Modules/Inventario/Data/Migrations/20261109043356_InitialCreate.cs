using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaDDJ.Web.Modules.Inventario.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventario");

            migrationBuilder.CreateTable(
                name: "categoria_articulo",
                schema: "inventario",
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
                    table.PrimaryKey("PK_categoria_articulo", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "proveedor",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    razon_social = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    identificacion_fiscal = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    correo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proveedor", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "articulo",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    unidad = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    categoria_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articulo", x => x.id);
                    table.CheckConstraint("ck_articulo_costo_unitario", "costo_unitario >= 0");
                    table.CheckConstraint("ck_articulo_tipo", "tipo IN ('PRODUCTO', 'INSUMO')");
                    table.ForeignKey(
                        name: "FK_articulo_categoria_articulo_categoria_id",
                        column: x => x.categoria_id,
                        principalSchema: "inventario",
                        principalTable: "categoria_articulo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "movimiento_inventario",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_movimiento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    motivo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notas = table.Column<string>(type: "text", nullable: true),
                    proveedor_id = table.Column<int>(type: "integer", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    orden_servicio_id = table.Column<Guid>(type: "uuid", nullable: true),
                    registrado_por_usuario_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimiento_inventario", x => x.id);
                    table.CheckConstraint("ck_movimiento_cliente_venta", "cliente_id IS NULL OR motivo = 'VENTA'");
                    table.CheckConstraint("ck_movimiento_compra_entrada", "motivo <> 'COMPRA' OR tipo_movimiento = 'ENTRADA'");
                    table.CheckConstraint("ck_movimiento_compra_proveedor", "(motivo = 'COMPRA') = (proveedor_id IS NOT NULL)");
                    table.CheckConstraint("ck_movimiento_consumo_orden", "motivo <> 'CONSUMO' OR orden_servicio_id IS NOT NULL");
                    table.CheckConstraint("ck_movimiento_motivo", "motivo IN ('COMPRA', 'VENTA', 'CONSUMO', 'MERMA', 'AJUSTE')");
                    table.CheckConstraint("ck_movimiento_notas_ajuste_merma", "motivo NOT IN ('AJUSTE', 'MERMA') OR (notas IS NOT NULL AND btrim(notas) <> '')");
                    table.CheckConstraint("ck_movimiento_orden_motivo", "orden_servicio_id IS NULL OR motivo IN ('CONSUMO', 'MERMA')");
                    table.CheckConstraint("ck_movimiento_tipo", "tipo_movimiento IN ('ENTRADA', 'SALIDA')");
                    table.CheckConstraint("ck_movimiento_venta_consumo_salida", "motivo NOT IN ('VENTA', 'CONSUMO') OR tipo_movimiento = 'SALIDA'");
                    table.ForeignKey(
                        name: "FK_movimiento_inventario_proveedor_proveedor_id",
                        column: x => x.proveedor_id,
                        principalSchema: "inventario",
                        principalTable: "proveedor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "insumo",
                schema: "inventario",
                columns: table => new
                {
                    articulo_id = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_insumo", x => x.articulo_id);
                    table.ForeignKey(
                        name: "FK_insumo_articulo_articulo_id",
                        column: x => x.articulo_id,
                        principalSchema: "inventario",
                        principalTable: "articulo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "producto",
                schema: "inventario",
                columns: table => new
                {
                    articulo_id = table.Column<int>(type: "integer", nullable: false),
                    sku = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    precio_venta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producto", x => x.articulo_id);
                    table.CheckConstraint("ck_producto_precio_venta", "precio_venta > 0");
                    table.ForeignKey(
                        name: "FK_producto_articulo_articulo_id",
                        column: x => x.articulo_id,
                        principalSchema: "inventario",
                        principalTable: "articulo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "detalle_movimiento_inventario",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    movimiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    articulo_id = table.Column<int>(type: "integer", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    costo_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    precio_unitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detalle_movimiento_inventario", x => x.id);
                    table.CheckConstraint("ck_detalle_movimiento_cantidad", "cantidad > 0");
                    table.CheckConstraint("ck_detalle_movimiento_costo", "costo_unitario IS NULL OR costo_unitario >= 0");
                    table.CheckConstraint("ck_detalle_movimiento_precio", "precio_unitario IS NULL OR precio_unitario >= 0");
                    table.ForeignKey(
                        name: "FK_detalle_movimiento_inventario_articulo_articulo_id",
                        column: x => x.articulo_id,
                        principalSchema: "inventario",
                        principalTable: "articulo",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_detalle_movimiento_inventario_movimiento_inventario_movimie~",
                        column: x => x.movimiento_id,
                        principalSchema: "inventario",
                        principalTable: "movimiento_inventario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_articulo_categoria_id",
                schema: "inventario",
                table: "articulo",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "IX_articulo_codigo",
                schema: "inventario",
                table: "articulo",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categoria_articulo_nombre",
                schema: "inventario",
                table: "categoria_articulo",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_detalle_movimiento_inventario_articulo_id",
                schema: "inventario",
                table: "detalle_movimiento_inventario",
                column: "articulo_id");

            migrationBuilder.CreateIndex(
                name: "IX_detalle_movimiento_inventario_movimiento_id",
                schema: "inventario",
                table: "detalle_movimiento_inventario",
                column: "movimiento_id");

            migrationBuilder.CreateIndex(
                name: "IX_movimiento_inventario_proveedor_id",
                schema: "inventario",
                table: "movimiento_inventario",
                column: "proveedor_id");

            migrationBuilder.CreateIndex(
                name: "IX_producto_sku",
                schema: "inventario",
                table: "producto",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_proveedor_identificacion_fiscal",
                schema: "inventario",
                table: "proveedor",
                column: "identificacion_fiscal",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detalle_movimiento_inventario",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "insumo",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "producto",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "movimiento_inventario",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "articulo",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "proveedor",
                schema: "inventario");

            migrationBuilder.DropTable(
                name: "categoria_articulo",
                schema: "inventario");
        }
    }
}
