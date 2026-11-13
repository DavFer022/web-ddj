using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;

namespace SistemaDDJ.Web.Modules.Inventario.Features.RegistrarCategoria;

public static class RegistrarCategoria
{
    public sealed record Command(string Nombre, string? Descripcion);

    public sealed record Resultado(int? CategoriaId, IReadOnlyList<string> Errores)
    {
        public bool Exitoso => Errores.Count == 0;
    }

    public sealed class Handler(InventarioDbContext db)
    {
        public async Task<Resultado> HandleAsync(Command comando, CancellationToken cancellationToken = default)
        {
            var nombre = comando.Nombre.Trim();
            var errores = new List<string>();

            if (nombre.Length is 0) errores.Add("El nombre es obligatorio.");
            else if (await db.CategoriasArticulo.AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower(), cancellationToken))
            {
                errores.Add("Ya existe una categoría con ese nombre.");
            }

            if (errores.Count > 0) return new Resultado(null, errores);

            var categoria = new CategoriaArticulo
            {
                Nombre = nombre,
                Descripcion = string.IsNullOrWhiteSpace(comando.Descripcion) ? null : comando.Descripcion.Trim(),
                Activo = true
            };
            db.CategoriasArticulo.Add(categoria);
            await db.SaveChangesAsync(cancellationToken);

            return new Resultado(categoria.Id, []);
        }
    }
}
