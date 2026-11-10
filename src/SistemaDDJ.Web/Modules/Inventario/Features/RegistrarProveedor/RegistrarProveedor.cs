using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Inventario.Features.Proveedores;

namespace SistemaDDJ.Web.Modules.Inventario.Features.RegistrarProveedor;

public static class RegistrarProveedor
{
    public sealed record Command(
        string RazonSocial,
        string? IdentificacionFiscal,
        string? Correo,
        IReadOnlyList<TelefonoProveedorDto> Telefonos);

    public sealed record Resultado(int? ProveedorId, IReadOnlyList<string> Errores)
    {
        public bool Exitoso => Errores.Count == 0;
    }

    public sealed class Handler(InventarioDbContext db)
    {
        public async Task<Resultado> HandleAsync(Command comando, CancellationToken cancellationToken = default)
        {
            var errores = new List<string>();
            var (razonSocial, fiscal, correo, telefonos) = ProveedorValidacion.Normalizar(
                comando.RazonSocial, comando.IdentificacionFiscal, comando.Correo, comando.Telefonos, errores);

            if (fiscal is not null && await db.Proveedores.AnyAsync(
                    p => p.IdentificacionFiscal != null && p.IdentificacionFiscal.ToLower() == fiscal.ToLower(),
                    cancellationToken))
            {
                errores.Add("Ya existe un proveedor con esa identificación fiscal.");
            }

            if (errores.Count > 0) return new Resultado(null, errores);

            await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

            var proveedor = new Proveedor
            {
                RazonSocial = razonSocial,
                IdentificacionFiscal = fiscal,
                Correo = correo,
                Activo = true,
                Telefonos = telefonos.Select(t => new TelefonoProveedor
                {
                    Numero = t.Numero,
                    Etiqueta = t.Etiqueta,
                    EsPrincipal = t.EsPrincipal
                }).ToList()
            };
            db.Proveedores.Add(proveedor);
            await db.SaveChangesAsync(cancellationToken);

            await transaccion.CommitAsync(cancellationToken);
            return new Resultado(proveedor.Id, []);
        }
    }
}
