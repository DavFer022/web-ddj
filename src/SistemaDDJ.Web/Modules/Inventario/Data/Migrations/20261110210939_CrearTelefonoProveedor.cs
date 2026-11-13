using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaDDJ.Web.Modules.Inventario.Data.Migrations
{
    /// <inheritdoc />
    public partial class CrearTelefonoProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "telefono_proveedor",
                schema: "inventario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    proveedor_id = table.Column<int>(type: "integer", nullable: false),
                    numero = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    etiqueta = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    es_principal = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telefono_proveedor", x => x.id);
                    table.CheckConstraint("ck_telefono_proveedor_numero", "btrim(numero) <> ''");
                    table.ForeignKey(
                        name: "FK_telefono_proveedor_proveedor_proveedor_id",
                        column: x => x.proveedor_id,
                        principalSchema: "inventario",
                        principalTable: "proveedor",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_telefono_proveedor_proveedor_id_numero",
                schema: "inventario",
                table: "telefono_proveedor",
                columns: new[] { "proveedor_id", "numero" },
                unique: true);

            migrationBuilder.Sql(
                "INSERT INTO inventario.telefono_proveedor (proveedor_id, numero, es_principal) " +
                "SELECT id, btrim(telefono), true FROM inventario.proveedor " +
                "WHERE telefono IS NOT NULL AND btrim(telefono) <> '';");

            migrationBuilder.DropColumn(
                name: "telefono",
                schema: "inventario",
                table: "proveedor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "telefono",
                schema: "inventario",
                table: "proveedor",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE inventario.proveedor p SET telefono = t.numero " +
                "FROM inventario.telefono_proveedor t " +
                "WHERE t.proveedor_id = p.id " +
                "AND t.id = (SELECT t2.id FROM inventario.telefono_proveedor t2 " +
                "WHERE t2.proveedor_id = p.id ORDER BY t2.es_principal DESC, t2.id LIMIT 1);");

            migrationBuilder.DropTable(
                name: "telefono_proveedor",
                schema: "inventario");
        }
    }
}
