using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Acceso.PublicApi;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;

namespace SistemaDDJ.Web.Modules.Inventario.Features.RegistrarCompraProveedor;

public static class RegistrarCompraProveedor
{
    public sealed record Linea(
        int ArticuloId,
        decimal Cantidad,
        decimal CostoUnitario,
        bool ActualizarCostoReferencia);

    public sealed record Command(
        int ProveedorId,
        int RegistradoPorUsuarioId,
        string? Referencia,
        string? Notas,
        IReadOnlyList<Linea> Lineas);

    public sealed record Resultado(Guid? MovimientoId, IReadOnlyList<string> Errores)
    {
        public bool Exitoso => Errores.Count == 0;
    }

    public sealed class Handler(InventarioDbContext db, IAccesoPublicApi acceso)
    {
        public async Task<Resultado> HandleAsync(Command comando, CancellationToken cancellationToken = default)
        {
            var errores = new List<string>();
            if (comando.Lineas is null || comando.Lineas.Count == 0)
            {
                return new Resultado(null, ["La compra debe tener al menos una línea."]);
            }

            foreach (var linea in comando.Lineas)
            {
                if (linea.Cantidad <= 0) errores.Add("La cantidad de cada línea debe ser mayor que 0.");
                if (linea.CostoUnitario < 0) errores.Add("El costo unitario no puede ser negativo.");
            }

            var proveedor = await db.Proveedores
                .FirstOrDefaultAsync(p => p.Id == comando.ProveedorId, cancellationToken);
            if (proveedor is null || !proveedor.Activo)
            {
                errores.Add("El proveedor no existe o está inactivo.");
            }

            if (!await acceso.EstaActivoAsync(comando.RegistradoPorUsuarioId, cancellationToken))
            {
                errores.Add("El usuario que registra la compra no existe o está inactivo.");
            }

            var consolidado = comando.Lineas
                .GroupBy(l => l.ArticuloId)
                .Select(g => new
                {
                    ArticuloId = g.Key,
                    Cantidad = g.Sum(l => l.Cantidad),
                    CostoUnitario = g.Last().CostoUnitario,
                    ActualizarCostoReferencia = g.Any(l => l.ActualizarCostoReferencia)
                })
                .ToList();

            var articuloIds = consolidado.Select(l => l.ArticuloId).ToList();
            var articulos = await db.Articulos
                .Where(a => articuloIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id, cancellationToken);

            foreach (var linea in consolidado)
            {
                if (!articulos.TryGetValue(linea.ArticuloId, out var articulo) || !articulo.Activo)
                {
                    errores.Add($"El artículo {linea.ArticuloId} no existe o está inactivo.");
                }
            }

            if (errores.Count > 0) return new Resultado(null, errores);

            await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

            var movimiento = new MovimientoInventario
            {
                TipoMovimiento = TipoMovimientoInventario.ENTRADA,
                Motivo = MotivoMovimientoInventario.COMPRA,
                OcurridoEn = DateTimeOffset.UtcNow,
                Referencia = string.IsNullOrWhiteSpace(comando.Referencia) ? null : comando.Referencia.Trim(),
                Notas = string.IsNullOrWhiteSpace(comando.Notas) ? null : comando.Notas.Trim(),
                ProveedorId = comando.ProveedorId,
                ClienteId = null,
                OrdenServicioId = null,
                RegistradoPorUsuarioId = comando.RegistradoPorUsuarioId,
                Detalles = consolidado.Select(l => new DetalleMovimientoInventario
                {
                    ArticuloId = l.ArticuloId,
                    Cantidad = l.Cantidad,
                    CostoUnitario = l.CostoUnitario,
                    PrecioUnitario = null
                }).ToList()
            };
            db.MovimientosInventario.Add(movimiento);

            foreach (var linea in consolidado.Where(l => l.ActualizarCostoReferencia))
            {
                articulos[linea.ArticuloId].CostoUnitario = linea.CostoUnitario;
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);

            return new Resultado(movimiento.Id, []);
        }
    }
}
