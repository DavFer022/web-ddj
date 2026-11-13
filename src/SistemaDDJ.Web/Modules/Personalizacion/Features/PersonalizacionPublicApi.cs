using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Personalizacion.Data;
using SistemaDDJ.Web.Modules.Personalizacion.Entities;
using SistemaDDJ.Web.Modules.Personalizacion.PublicApi;

namespace SistemaDDJ.Web.Modules.Personalizacion.Features;

public sealed class PersonalizacionPublicApi(PersonalizacionDbContext db) : IPersonalizacionPublicApi
{
    public Task<bool> ExisteAsync(Guid ordenServicioId, CancellationToken cancellationToken = default) =>
        db.OrdenesServicio.AnyAsync(o => o.Id == ordenServicioId, cancellationToken);

    public Task<bool> AdmiteConsumoAsync(Guid ordenServicioId, CancellationToken cancellationToken = default) =>
        db.OrdenesServicio.AnyAsync(
            o => o.Id == ordenServicioId
                 && (o.Estado == EstadoOrdenServicio.PENDIENTE || o.Estado == EstadoOrdenServicio.EN_PROCESO),
            cancellationToken);

    public async Task<OrdenServicioResumenDto?> ObtenerAsync(
        Guid ordenServicioId,
        CancellationToken cancellationToken = default)
    {
        var orden = await db.OrdenesServicio.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == ordenServicioId, cancellationToken);
        return orden is null ? null : Mapear(orden);
    }

    public async Task<IReadOnlyList<OrdenServicioResumenDto>> ListarAbiertasAsync(
        CancellationToken cancellationToken = default)
    {
        var ordenes = await db.OrdenesServicio.AsNoTracking()
            .Where(o => o.Estado == EstadoOrdenServicio.PENDIENTE || o.Estado == EstadoOrdenServicio.EN_PROCESO)
            .OrderBy(o => o.VenceEn)
            .ToListAsync(cancellationToken);
        return ordenes.Select(Mapear).ToList();
    }

    private static OrdenServicioResumenDto Mapear(OrdenServicio orden) =>
        new(orden.Id, orden.NumeroOrden, orden.Estado, orden.ClienteId, orden.VenceEn);
}
