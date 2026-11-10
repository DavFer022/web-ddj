using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Acceso.Data;
using SistemaDDJ.Web.Modules.Acceso.PublicApi;

namespace SistemaDDJ.Web.Modules.Acceso.Features;

// Implementación temporal del usuario actual mientras HU-01 (autenticación) no está implementada:
// devuelve el primer usuario activo, que en desarrollo es el administrador sembrado.
public sealed class AccesoPublicApi(AccesoDbContext db) : IAccesoPublicApi
{
    public Task<bool> EstaActivoAsync(int usuarioId, CancellationToken cancellationToken = default) =>
        db.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Activo, cancellationToken);

    public Task<bool> TieneAlgunRolAsync(
        int usuarioId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        var nombres = roles.ToArray();
        return (from usuario in db.Usuarios
                join usuarioRol in db.UsuarioRoles on usuario.Id equals usuarioRol.UsuarioId
                join rol in db.Roles on usuarioRol.RolId equals rol.Id
                where usuario.Id == usuarioId && usuario.Activo && nombres.Contains(rol.Nombre)
                select usuario.Id)
            .AnyAsync(cancellationToken);
    }

    public async Task<UsuarioDto?> ObtenerUsuarioActualAsync(CancellationToken cancellationToken = default)
    {
        var usuario = await db.Usuarios.AsNoTracking()
            .Where(u => u.Activo)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (usuario is null) return null;

        var roles = await ObtenerRolesPorUsuarioAsync(cancellationToken);
        return new UsuarioDto(
            usuario.Id,
            usuario.Correo,
            usuario.NombreVisible,
            usuario.Activo,
            roles.GetValueOrDefault(usuario.Id, []));
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarUsuariosActivosAsync(
        CancellationToken cancellationToken = default)
    {
        var usuarios = await db.Usuarios.AsNoTracking()
            .Where(u => u.Activo)
            .OrderBy(u => u.NombreVisible)
            .ToListAsync(cancellationToken);
        var roles = await ObtenerRolesPorUsuarioAsync(cancellationToken);

        return usuarios.Select(u => new UsuarioDto(
            u.Id,
            u.Correo,
            u.NombreVisible,
            u.Activo,
            roles.GetValueOrDefault(u.Id, [])))
            .ToList();
    }

    private async Task<Dictionary<int, IReadOnlyList<string>>> ObtenerRolesPorUsuarioAsync(
        CancellationToken cancellationToken)
    {
        var asignaciones = await (from usuarioRol in db.UsuarioRoles.AsNoTracking()
                                  join rol in db.Roles.AsNoTracking() on usuarioRol.RolId equals rol.Id
                                  select new { usuarioRol.UsuarioId, rol.Nombre })
            .ToListAsync(cancellationToken);

        return asignaciones
            .GroupBy(a => a.UsuarioId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.Nombre).ToList());
    }
}
