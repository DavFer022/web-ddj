using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.Proveedores;

namespace SistemaDDJ.Web.Modules.Inventario.Features.ActualizarProveedor;

public static class ActualizarProveedor
{
    public sealed record Command(
        int ProveedorId,
        string RazonSocial,
        string? IdentificacionFiscal,
        string? Correo,
        IReadOnlyList<TelefonoProveedorDto> Telefonos);

    public sealed record Resultado(IReadOnlyList<string> Errores)
    {
        public bool Exitoso => Errores.Count == 0;
    }

    public sealed class Handler(InventarioDbContext db)
    {
        public async Task<Resultado> HandleAsync(Command comando, CancellationToken cancellationToken = default)
        {
            var errores = new List<string>();
            var proveedor = await db.Proveedores
                .Include(p => p.Telefonos)
                .FirstOrDefaultAsync(p => p.Id == comando.ProveedorId, cancellationToken);
            if (proveedor is null)
            {
                return new Resultado(["El proveedor no existe."]);
            }

            var (razonSocial, fiscal, correo, telefonos) = ProveedorValidacion.Normalizar(
                comando.RazonSocial, comando.IdentificacionFiscal, comando.Correo, comando.Telefonos, errores);

            if (fiscal is not null && await db.Proveedores.AnyAsync(
                    p => p.Id != comando.ProveedorId
                         && p.IdentificacionFiscal != null
                         && p.IdentificacionFiscal.ToLower() == fiscal.ToLower(),
                    cancellationToken))
            {
                errores.Add("Ya existe un proveedor con esa identificación fiscal.");
            }

            if (errores.Count > 0) return new Resultado(errores);

            await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

            proveedor.RazonSocial = razonSocial;
            proveedor.IdentificacionFiscal = fiscal;
            proveedor.Correo = correo;

            var existentes = proveedor.Telefonos.ToList();
            foreach (var telefono in telefonos)
            {
                var existente = existentes.FirstOrDefault(t =>
                    string.Equals(t.Numero, telefono.Numero, StringComparison.OrdinalIgnoreCase));
                if (existente is null)
                {
                    proveedor.Telefonos.Add(new TelefonoProveedor
                    {
                        Numero = telefono.Numero,
                        Etiqueta = telefono.Etiqueta,
                        EsPrincipal = telefono.EsPrincipal
                    });
                    continue;
                }

                existente.Numero = telefono.Numero;
                existente.Etiqueta = telefono.Etiqueta;
                existente.EsPrincipal = telefono.EsPrincipal;
                existentes.Remove(existente);
            }

            db.TelefonosProveedor.RemoveRange(existentes);
            proveedor.Telefonos.RemoveAll(t => existentes.Contains(t));

            await db.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return new Resultado([]);
        }
    }
}
