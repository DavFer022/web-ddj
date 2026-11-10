using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.ConsultarStock;

namespace SistemaDDJ.Web.Modules.Inventario.Features.ConsultarDashboardInventario;

public enum EstadoStock
{
    AGOTADO,
    BAJO,
    DISPONIBLE
}

public static class ConsultarDashboardInventario
{
    public sealed record Consulta(
        ArticuloTipo? Tipo = null,
        string? Busqueda = null,
        bool SoloAlertas = false);

    public sealed record ResumenStock(int Total, int Agotados, int Bajos);

    public sealed record ArticuloStockDto(
        int ArticuloId,
        string Codigo,
        string Nombre,
        ArticuloTipo Tipo,
        UnidadMedida Unidad,
        string Categoria,
        decimal Stock,
        decimal StockMinimo,
        EstadoStock Estado,
        bool Activo,
        string? Sku,
        decimal? PrecioVenta,
        string? DescripcionInsumo);

    public sealed record Resultado(
        ResumenStock Productos,
        ResumenStock Insumos,
        IReadOnlyList<ArticuloStockDto> Articulos);

    public sealed class Handler(InventarioDbContext db)
    {
        public async Task<Resultado> HandleAsync(Consulta consulta, CancellationToken cancellationToken = default)
        {
            var saldos = await SaldosInventario.ObtenerSaldosAsync(db, cancellationToken: cancellationToken);
            var articulos = await db.Articulos.AsNoTracking().ToListAsync(cancellationToken);
            var categorias = await db.CategoriasArticulo.AsNoTracking()
                .ToDictionaryAsync(c => c.Id, c => c.Nombre, cancellationToken);
            var productos = await db.Productos.AsNoTracking()
                .ToDictionaryAsync(p => p.ArticuloId, cancellationToken);
            var insumos = await db.Insumos.AsNoTracking()
                .ToDictionaryAsync(i => i.ArticuloId, cancellationToken);

            var busqueda = string.IsNullOrWhiteSpace(consulta.Busqueda)
                ? null
                : consulta.Busqueda.Trim();

            var todos = articulos
                .Where(a => busqueda is null
                            || a.Codigo.Contains(busqueda, StringComparison.OrdinalIgnoreCase)
                            || a.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                .Select(a =>
                {
                    var stock = saldos.GetValueOrDefault(a.Id);
                    var estado = stock <= 0
                        ? EstadoStock.AGOTADO
                        : a.StockMinimo > 0 && stock <= a.StockMinimo
                            ? EstadoStock.BAJO
                            : EstadoStock.DISPONIBLE;
                    productos.TryGetValue(a.Id, out var producto);
                    insumos.TryGetValue(a.Id, out var insumo);
                    return new ArticuloStockDto(
                        a.Id,
                        a.Codigo,
                        a.Nombre,
                        a.Tipo,
                        a.Unidad,
                        categorias.GetValueOrDefault(a.CategoriaId, string.Empty),
                        stock,
                        a.StockMinimo,
                        estado,
                        a.Activo,
                        producto?.Sku,
                        producto?.PrecioVenta,
                        insumo?.Descripcion);
                })
                .ToList();

            var visibles = todos
                .Where(a => consulta.Tipo is null || a.Tipo == consulta.Tipo)
                .Where(a => !consulta.SoloAlertas || a.Estado != EstadoStock.DISPONIBLE)
                .OrderBy(a => a.Tipo)
                .ThenBy(a => a.Nombre)
                .ToList();

            return new Resultado(
                Resumir(todos, ArticuloTipo.PRODUCTO),
                Resumir(todos, ArticuloTipo.INSUMO),
                visibles);
        }

        private static ResumenStock Resumir(IReadOnlyCollection<ArticuloStockDto> articulos, ArticuloTipo tipo)
        {
            var delTipo = articulos.Where(a => a.Tipo == tipo).ToList();
            return new ResumenStock(
                delTipo.Count,
                delTipo.Count(a => a.Estado == EstadoStock.AGOTADO),
                delTipo.Count(a => a.Estado == EstadoStock.BAJO));
        }
    }
}
