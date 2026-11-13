using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Inventario.Data;

namespace SistemaDDJ.Web.Modules.Inventario.Features.CambiarEstadoProveedor;

public static class CambiarEstadoProveedor
{
    public sealed record Command(int ProveedorId, bool Activo);

    public sealed record Resultado(IReadOnlyList<string> Errores)
    {
        public bool Exitoso => Errores.Count == 0;
    }

    public sealed class Handler(InventarioDbContext db)
    {
        public async Task<Resultado> HandleAsync(Command comando, CancellationToken cancellationToken = default)
        {
            var proveedor = await db.Proveedores.FirstOrDefaultAsync(p => p.Id == comando.ProveedorId, cancellationToken);
            if (proveedor is null) return new Resultado(["El proveedor no existe."]);

            proveedor.Activo = comando.Activo;
            await db.SaveChangesAsync(cancellationToken);
            return new Resultado([]);
        }
    }
}
