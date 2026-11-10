namespace SistemaDDJ.Web.Modules.Acceso.PublicApi;

public static class RolesAcceso
{
    public const string Admin = "Admin";
    public const string Trabajador = "Trabajador";
}

public sealed record UsuarioDto(
    int Id,
    string Correo,
    string NombreVisible,
    bool Activo,
    IReadOnlyList<string> Roles);

public interface IAccesoPublicApi
{
    Task<bool> EstaActivoAsync(int usuarioId, CancellationToken cancellationToken = default);

    Task<bool> TieneAlgunRolAsync(
        int usuarioId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);

    Task<UsuarioDto?> ObtenerUsuarioActualAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UsuarioDto>> ListarUsuariosActivosAsync(CancellationToken cancellationToken = default);
}
