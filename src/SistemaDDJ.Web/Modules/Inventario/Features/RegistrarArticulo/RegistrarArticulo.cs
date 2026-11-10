using Microsoft.EntityFrameworkCore;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;

namespace SistemaDDJ.Web.Modules.Inventario.Features.RegistrarArticulo;

public static class RegistrarArticulo
{
    public sealed record Command(
        string Codigo,
        string Nombre,
        UnidadMedida Unidad,
        decimal CostoUnitario,
        int CategoriaId,
        ArticuloTipo Tipo,
        decimal? PrecioVenta = null,
        string? Sku = null,
        string? Descripcion = null);

    public sealed record Resultado(int? ArticuloId, IReadOnlyList<string> Errores)
    {
        public bool Exitoso => Errores.Count == 0;
    }

    public sealed class Handler(InventarioDbContext db)
    {
        public async Task<Resultado> HandleAsync(Command comando, CancellationToken cancellationToken = default)
        {
            var errores = new List<string>();
            var codigo = comando.Codigo.Trim().ToUpperInvariant();
            var nombre = comando.Nombre.Trim();
            var sku = comando.Sku?.Trim().ToUpperInvariant();

            if (codigo.Length is 0 or > 30) errores.Add("El código es obligatorio y puede tener hasta 30 caracteres.");
            if (nombre.Length is 0) errores.Add("El nombre es obligatorio.");
            if (comando.CostoUnitario < 0) errores.Add("El costo unitario no puede ser negativo.");

            if (!await db.CategoriasArticulo.AnyAsync(c => c.Id == comando.CategoriaId && c.Activo, cancellationToken))
            {
                errores.Add("La categoría no existe o está inactiva.");
            }

            if (codigo.Length > 0 && await db.Articulos.AnyAsync(a => a.Codigo == codigo, cancellationToken))
            {
                errores.Add("Ya existe un artículo con ese código.");
            }

            switch (comando.Tipo)
            {
                case ArticuloTipo.PRODUCTO:
                    if (string.IsNullOrEmpty(sku)) errores.Add("El SKU es obligatorio para un producto.");
                    if (comando.PrecioVenta is not > 0) errores.Add("El precio de venta debe ser mayor que 0.");
                    if (!string.IsNullOrEmpty(sku) && await db.Productos.AnyAsync(p => p.Sku == sku, cancellationToken))
                    {
                        errores.Add("Ya existe un producto con ese SKU.");
                    }
                    break;

                case ArticuloTipo.INSUMO:
                    if (string.IsNullOrWhiteSpace(comando.Descripcion)) errores.Add("La descripción del insumo es obligatoria.");
                    break;
            }

            if (errores.Count > 0) return new Resultado(null, errores);

            await using var transaccion = await db.Database.BeginTransactionAsync(cancellationToken);

            var articulo = new Articulo
            {
                Codigo = codigo,
                Nombre = nombre,
                Unidad = comando.Unidad,
                Tipo = comando.Tipo,
                CostoUnitario = comando.CostoUnitario,
                CategoriaId = comando.CategoriaId
            };
            db.Articulos.Add(articulo);
            await db.SaveChangesAsync(cancellationToken);

            if (comando.Tipo == ArticuloTipo.PRODUCTO)
            {
                db.Productos.Add(new Producto { ArticuloId = articulo.Id, Sku = sku!, PrecioVenta = comando.PrecioVenta!.Value });
            }
            else
            {
                db.Insumos.Add(new Insumo { ArticuloId = articulo.Id, Descripcion = comando.Descripcion!.Trim() });
            }
            await db.SaveChangesAsync(cancellationToken);

            await transaccion.CommitAsync(cancellationToken);
            return new Resultado(articulo.Id, []);
        }
    }
}
