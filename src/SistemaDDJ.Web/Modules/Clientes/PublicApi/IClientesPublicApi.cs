namespace SistemaDDJ.Web.Modules.Clientes.PublicApi;

public sealed record ClienteResumenDto(
    Guid Id,
    string Nombre,
    string? Apellido,
    string? Correo,
    string? Telefono,
    bool Activo);

public interface IClientesPublicApi
{
    Task<ClienteResumenDto?> ObtenerActivoAsync(Guid clienteId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClienteResumenDto>> ListarActivosAsync(CancellationToken cancellationToken = default);
}
