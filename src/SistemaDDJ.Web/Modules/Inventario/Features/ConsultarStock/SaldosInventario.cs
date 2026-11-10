using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;

namespace SistemaDDJ.Web.Modules.Inventario.Features.ConsultarStock;

// Saldo calculado desde los movimientos: las entradas suman y las salidas restan,
// sin importar si provienen de compras, ventas, consumos, mermas o ajustes.
public static class SaldosInventario
{
    public static async Task<Dictionary<int, decimal>> ObtenerSaldosAsync(
        InventarioDbContext db,
        IReadOnlyCollection<int>? articuloIds = null,
        CancellationToken cancellationToken = default)
    {
        var consulta = from detalle in db.DetallesMovimientoInventario.AsNoTracking()
                       join movimiento in db.MovimientosInventario.AsNoTracking()
                           on detalle.MovimientoId equals movimiento.Id
                       select new { detalle, movimiento };

        if (articuloIds is not null)
        {
            consulta = consulta.Where(x => articuloIds.Contains(x.detalle.ArticuloId));
        }

        return await consulta
            .GroupBy(x => x.detalle.ArticuloId)
            .Select(g => new
            {
                ArticuloId = g.Key,
                Saldo = g.Sum(x => x.movimiento.TipoMovimiento == TipoMovimientoInventario.ENTRADA
                    ? x.detalle.Cantidad
                    : -x.detalle.Cantidad)
            })
            .ToDictionaryAsync(x => x.ArticuloId, x => x.Saldo, cancellationToken);
    }
}
