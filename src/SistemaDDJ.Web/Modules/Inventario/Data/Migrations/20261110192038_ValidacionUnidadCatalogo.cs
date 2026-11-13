using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDDJ.Web.Modules.Inventario.Data.Migrations
{
    /// <inheritdoc />
    public partial class ValidacionUnidadCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE inventario.articulo SET unidad = UPPER(unidad);");

            migrationBuilder.AddCheckConstraint(
                name: "ck_articulo_unidad",
                schema: "inventario",
                table: "articulo",
                sql: "unidad IN ('UNIDAD', 'METRO', 'HOJA', 'ROLLO', 'ML', 'KG', 'LITRO')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_articulo_unidad",
                schema: "inventario",
                table: "articulo");
        }
    }
}
