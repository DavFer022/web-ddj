using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Inventario.Entities;

namespace SistemaDDJ.Web.Modules.Inventario.Data.Configurations;

public sealed class CategoriaArticuloConfiguration : IEntityTypeConfiguration<CategoriaArticulo>
{
    public void Configure(EntityTypeBuilder<CategoriaArticulo> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Nombre).IsUnique();
        builder.Property(x => x.Activo).HasDefaultValue(true);
    }
}

public sealed class ArticuloConfiguration : IEntityTypeConfiguration<Articulo>
{
    public void Configure(EntityTypeBuilder<Articulo> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.Codigo).IsUnique();
        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Unidad).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CostoUnitario).HasPrecision(18, 2);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.HasOne<CategoriaArticulo>().WithMany().HasForeignKey(x => x.CategoriaId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_articulo_tipo",
                $"tipo IN ({PersistenceExtensions.SqlValuesOf<ArticuloTipo>()})");
            t.HasCheckConstraint("ck_articulo_costo_unitario", "costo_unitario >= 0");
        });
    }
}

public sealed class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.HasKey(x => x.ArticuloId);
        builder.Property(x => x.Sku).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.Property(x => x.PrecioVenta).HasPrecision(18, 2);
        builder.HasOne<Articulo>().WithOne().HasForeignKey<Producto>(x => x.ArticuloId);

        builder.ToTable(t => t.HasCheckConstraint("ck_producto_precio_venta", "precio_venta > 0"));
    }
}

public sealed class InsumoConfiguration : IEntityTypeConfiguration<Insumo>
{
    public void Configure(EntityTypeBuilder<Insumo> builder)
    {
        builder.HasKey(x => x.ArticuloId);
        builder.Property(x => x.Descripcion).IsRequired();
        builder.HasOne<Articulo>().WithOne().HasForeignKey<Insumo>(x => x.ArticuloId);
    }
}

public sealed class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RazonSocial).HasMaxLength(150).IsRequired();
        builder.Property(x => x.IdentificacionFiscal).HasMaxLength(30);
        builder.HasIndex(x => x.IdentificacionFiscal).IsUnique();
        builder.Property(x => x.Correo).HasMaxLength(255);
        builder.Property(x => x.Telefono).HasMaxLength(30);
        builder.Property(x => x.Activo).HasDefaultValue(true);
    }
}

public sealed class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TipoMovimiento).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Motivo).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.OcurridoEn).HasDefaultValueSql("now()");
        builder.Property(x => x.Referencia).HasMaxLength(100);
        builder.HasOne<Proveedor>().WithMany().HasForeignKey(x => x.ProveedorId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_movimiento_tipo",
                $"tipo_movimiento IN ({PersistenceExtensions.SqlValuesOf<TipoMovimientoInventario>()})");
            t.HasCheckConstraint("ck_movimiento_motivo",
                $"motivo IN ({PersistenceExtensions.SqlValuesOf<MotivoMovimientoInventario>()})");
            t.HasCheckConstraint("ck_movimiento_compra_proveedor", "(motivo = 'COMPRA') = (proveedor_id IS NOT NULL)");
            t.HasCheckConstraint("ck_movimiento_compra_entrada", "motivo <> 'COMPRA' OR tipo_movimiento = 'ENTRADA'");
            t.HasCheckConstraint("ck_movimiento_venta_consumo_salida",
                "motivo NOT IN ('VENTA', 'CONSUMO') OR tipo_movimiento = 'SALIDA'");
            t.HasCheckConstraint("ck_movimiento_consumo_orden",
                "motivo <> 'CONSUMO' OR orden_servicio_id IS NOT NULL");
            t.HasCheckConstraint("ck_movimiento_orden_motivo",
                "orden_servicio_id IS NULL OR motivo IN ('CONSUMO', 'MERMA')");
            t.HasCheckConstraint("ck_movimiento_cliente_venta",
                "cliente_id IS NULL OR motivo = 'VENTA'");
            t.HasCheckConstraint("ck_movimiento_notas_ajuste_merma",
                "motivo NOT IN ('AJUSTE', 'MERMA') OR (notas IS NOT NULL AND btrim(notas) <> '')");
        });
    }
}

public sealed class DetalleMovimientoInventarioConfiguration : IEntityTypeConfiguration<DetalleMovimientoInventario>
{
    public void Configure(EntityTypeBuilder<DetalleMovimientoInventario> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Cantidad).HasPrecision(18, 3);
        builder.Property(x => x.CostoUnitario).HasPrecision(18, 2);
        builder.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
        builder.HasOne<MovimientoInventario>().WithMany().HasForeignKey(x => x.MovimientoId);
        builder.HasOne<Articulo>().WithMany().HasForeignKey(x => x.ArticuloId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_detalle_movimiento_cantidad", "cantidad > 0");
            t.HasCheckConstraint("ck_detalle_movimiento_costo", "costo_unitario IS NULL OR costo_unitario >= 0");
            t.HasCheckConstraint("ck_detalle_movimiento_precio", "precio_unitario IS NULL OR precio_unitario >= 0");
        });
    }
}
