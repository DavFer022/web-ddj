using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Features.Proveedores;

namespace SistemaDDJ.Web.Modules.Inventario.Features.ListarProveedores;

public static class ListarProveedores
{
    public sealed record Consulta(bool SoloActivos = false);

    public sealed class Handler(InventarioDbContext db)
    {
        public async Task<IReadOnlyList<ProveedorDto>> HandleAsync(
            Consulta consulta,
            CancellationToken cancellationToken = default)
        {
            var proveedores = await db.Proveedores
                .AsNoTracking()
                .Include(p => p.Telefonos)
                .Where(p => !consulta.SoloActivos || p.Activo)
                .OrderBy(p => p.RazonSocial)
                .ToListAsync(cancellationToken);

            return proveedores.Select(p => new ProveedorDto(
                p.Id,
                p.RazonSocial,
                p.IdentificacionFiscal,
                p.Correo,
                p.Activo,
                p.Telefonos
                    .OrderByDescending(t => t.EsPrincipal)
                    .ThenBy(t => t.Id)
                    .Select(t => new TelefonoProveedorDto(t.Numero, t.Etiqueta, t.EsPrincipal))
                    .ToList()))
                .ToList();
        }
    }
}
