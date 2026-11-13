using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Acceso.PublicApi;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.ConsultarStock;
using SistemaDDJ.Web.Modules.Personalizacion.PublicApi;

namespace SistemaDDJ.Web.Modules.Inventario.Features.RegistrarAjusteOMerma;

public static class RegistrarAjusteOMerma
{
    public sealed record Linea(int ArticuloId, decimal Cantidad);

    public sealed record Command(
        TipoMovimientoInventario TipoMovimiento,
        MotivoMovimientoInventario Motivo,
        string? Notas,
        int RegistradoPorUsuarioId,
        Guid? OrdenServicioId,
        string? Referencia,
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
            var notas = comando.Notas?.Trim();
            if (string.IsNullOrWhiteSpace(notas))
            {
                errores.Add("Las notas son obligatorias para ajustes y mermas.");
            }

            if (comando.Motivo is not (MotivoMovimientoInventario.AJUSTE or MotivoMovimientoInventario.MERMA))
            {
                errores.Add("El motivo debe ser AJUSTE o MERMA.");
            }

            if (comando.Lineas is null || comando.Lineas.Count == 0)
            {
                return new Resultado(null, ["El movimiento debe tener al menos una línea."]);
            }

            foreach (var linea in comando.Lineas)
            {
                if (linea.Cantidad <= 0) errores.Add("La cantidad de cada línea debe ser mayor que 0.");
            }

            if (!await acceso.TieneAlgunRolAsync(
                    comando.RegistradoPorUsuarioId,
                    [RolesAcceso.Admin, RolesAcceso.Trabajador],
                    cancellationToken))
            {
                errores.Add("El usuario no está activo o no tiene un rol autorizado.");
            }

            if (comando.Motivo == MotivoMovimientoInventario.MERMA)
            {
                if (comando.TipoMovimiento != TipoMovimientoInventario.SALIDA)
                {
                    errores.Add("Una merma debe registrarse como salida de inventario.");
                }

                if (comando.OrdenServicioId is not null
                    && !await personalizacion.ExisteAsync(comando.OrdenServicioId.Value, cancellationToken))
                {
                    errores.Add("La orden de servicio indicada no existe.");
                }
            }

            if (comando.Motivo == MotivoMovimientoInventario.AJUSTE && comando.OrdenServicioId is not null)
            {
                errores.Add("Un ajuste de inventario no puede vincularse a una orden de servicio.");
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

                if (comando.Motivo == MotivoMovimientoInventario.MERMA && articulo.Tipo != ArticuloTipo.INSUMO)
                {
                    errores.Add($"El artículo '{articulo.Nombre}' no es un insumo; una merma solo aplica a insumos.");
                    continue;
                }

                if (comando.TipoMovimiento == TipoMovimientoInventario.SALIDA)
                {
                    var disponible = saldos.GetValueOrDefault(articulo.Id);
                    if (linea.Cantidad > disponible)
                    {
                        errores.Add(
                            $"Stock insuficiente para '{articulo.Nombre}': solicitado {linea.Cantidad:0.###}, disponible {disponible:0.###}.");
                    }
                }
            }

            if (errores.Count > 0) return new Resultado(null, errores);

            await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

            var movimiento = new MovimientoInventario
            {
                TipoMovimiento = comando.TipoMovimiento,
                Motivo = comando.Motivo,
                OcurridoEn = DateTimeOffset.UtcNow,
                Referencia = string.IsNullOrWhiteSpace(comando.Referencia) ? null : comando.Referencia.Trim(),
                Notas = notas,
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
