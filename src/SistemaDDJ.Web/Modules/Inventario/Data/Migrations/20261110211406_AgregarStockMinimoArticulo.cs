using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDDJ.Web.Modules.Inventario.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarStockMinimoArticulo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "stock_minimo",
                schema: "inventario",
                table: "articulo",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_articulo_stock_minimo",
                schema: "inventario",
                table: "articulo",
                sql: "stock_minimo >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_articulo_stock_minimo",
                schema: "inventario",
                table: "articulo");

            migrationBuilder.DropColumn(
                name: "stock_minimo",
                schema: "inventario",
                table: "articulo");
        }
    }
}
