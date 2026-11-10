using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Clientes.Data;
using SistemaDDJ.Web.Modules.Clientes.PublicApi;

namespace SistemaDDJ.Web.Modules.Clientes.Features;

public sealed class ClientesPublicApi(ClientesDbContext db) : IClientesPublicApi
{
    public async Task<ClienteResumenDto?> ObtenerActivoAsync(
        Guid clienteId,
        CancellationToken cancellationToken = default)
    {
        var cliente = await db.Clientes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == clienteId && c.Activo, cancellationToken);
        return cliente is null ? null : Mapear(cliente);
    }

    public async Task<IReadOnlyList<ClienteResumenDto>> ListarActivosAsync(
        CancellationToken cancellationToken = default)
    {
        var clientes = await db.Clientes.AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .ThenBy(c => c.Apellido)
            .ToListAsync(cancellationToken);
        return clientes.Select(Mapear).ToList();
    }

    private static ClienteResumenDto Mapear(Entities.Cliente cliente) =>
        new(cliente.Id, cliente.Nombre, cliente.Apellido, cliente.Correo, cliente.Telefono, cliente.Activo);
}
