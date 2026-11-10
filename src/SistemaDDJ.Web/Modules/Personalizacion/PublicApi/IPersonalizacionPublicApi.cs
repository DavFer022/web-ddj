using SistemaDDJ.Web.Modules.Personalizacion.Entities;

namespace SistemaDDJ.Web.Modules.Personalizacion.PublicApi;

public sealed record OrdenServicioResumenDto(
    Guid Id,
    string NumeroOrden,
    EstadoOrdenServicio Estado,
    Guid ClienteId,
    DateTimeOffset VenceEn);

public interface IPersonalizacionPublicApi
{
    Task<bool> ExisteAsync(Guid ordenServicioId, CancellationToken cancellationToken = default);

    Task<bool> AdmiteConsumoAsync(Guid ordenServicioId, CancellationToken cancellationToken = default);

    Task<OrdenServicioResumenDto?> ObtenerAsync(Guid ordenServicioId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrdenServicioResumenDto>> ListarAbiertasAsync(CancellationToken cancellationToken = default);
}
