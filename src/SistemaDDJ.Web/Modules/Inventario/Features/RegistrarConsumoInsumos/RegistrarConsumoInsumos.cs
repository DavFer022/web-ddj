using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Acceso.PublicApi;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.ConsultarStock;
using SistemaDDJ.Web.Modules.Personalizacion.PublicApi;

namespace SistemaDDJ.Web.Modules.Inventario.Features.RegistrarConsumoInsumos;

public static class RegistrarConsumoInsumos
{
    public sealed record Linea(int ArticuloId, decimal Cantidad);

    public sealed record Command(
        Guid OrdenServicioId,
        int RegistradoPorUsuarioId,
        string? Referencia,
        string? Notas,
        IReadOnlyList<Linea> Lineas);

    public sealed record Resultado(Guid? MovimientoId, IReadOnlyList<string> Errores)
    {
        public bool Exitoso => Errores.Count == 0;
    }

    public sealed class Handler(
        InventarioDbContext db,
        IAccesoPublicApi acceso,
        IPersonalizacionPublicApi personalizacion)
    {
        public async Task<Resultado> HandleAsync(Command comando, CancellationToken cancellationToken = default)
        {
            var errores = new List<string>();
            if (comando.Lineas is null || comando.Lineas.Count == 0)
            {
                return new Resultado(null, ["El consumo debe tener al menos una línea."]);
            }

            foreach (var linea in comando.Lineas)
            {
                if (linea.Cantidad <= 0) errores.Add("La cantidad de cada línea debe ser mayor que 0.");
            }

            if (comando.OrdenServicioId == Guid.Empty
                || !await personalizacion.AdmiteConsumoAsync(comando.OrdenServicioId, cancellationToken))
            {
                errores.Add("La orden de servicio no existe o no admite consumo (solo PENDIENTE o EN_PROCESO).");
            }

            if (!await acceso.EstaActivoAsync(comando.RegistradoPorUsuarioId, cancellationToken))
            {
                errores.Add("El usuario que registra el consumo no existe o está inactivo.");
            }

            var consolidado = comando.Lineas
                .GroupBy(l => l.ArticuloId)
                .Select(g => new { ArticuloId = g.Key, Cantidad = g.Sum(l => l.Cantidad) })
                .ToList();
            var articuloIds = consolidado.Select(l => l.ArticuloId).ToList();

            var articulos = await db.Articulos
                .Where(a => articuloIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken);
            var saldos = await SaldosInventario.ObtenerSaldosAsync(db, articuloIds, cancellationToken);

            foreach (var linea in consolidado)
            {
                if (!articulos.TryGetValue(linea.ArticuloId, out var articulo) || !articulo.Activo)
                {
                    errores.Add($"El artículo {linea.ArticuloId} no existe o está inactivo.");
                    continue;
                }

                if (articulo.Tipo != ArticuloTipo.INSUMO)
                {
                    errores.Add($"El artículo '{articulo.Nombre}' no es un insumo de personalización.");
                    continue;
                }

                var disponible = saldos.GetValueOrDefault(articulo.Id);
                if (linea.Cantidad > disponible)
                {
                    errores.Add(
                        $"Stock insuficiente para '{articulo.Nombre}': solicitado {linea.Cantidad:0.###}, disponible {disponible:0.###}.");
                }
            }

            if (errores.Count > 0) return new Resultado(null, errores);

            await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

            var movimiento = new MovimientoInventario
            {
                TipoMovimiento = TipoMovimientoInventario.SALIDA,
                Motivo = MotivoMovimientoInventario.CONSUMO,
                OcurridoEn = DateTimeOffset.UtcNow,
                Referencia = string.IsNullOrWhiteSpace(comando.Referencia) ? null : comando.Referencia.Trim(),
                Notas = string.IsNullOrWhiteSpace(comando.Notas) ? null : comando.Notas.Trim(),
                ProveedorId = null,
                ClienteId = null,
                OrdenServicioId = comando.OrdenServicioId,
                RegistradoPorUsuarioId = comando.RegistradoPorUsuarioId,
                Detalles = consolidado.Select(l => new DetalleMovimientoInventario
                {
                    ArticuloId = l.ArticuloId,
                    Cantidad = l.Cantidad,
                    CostoUnitario = articulos[l.ArticuloId].CostoUnitario,
                    PrecioUnitario = null
                }).ToList()
            };
            db.MovimientosInventario.Add(movimiento);
            await db.SaveChangesAsync(cancellationToken);

            await transaccion.CommitAsync(cancellationToken);
            return new Resultado(movimiento.Id, []);
        }
    }
}
