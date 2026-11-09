using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Personalizacion.Entities;

namespace SistemaDDJ.Web.Modules.Personalizacion.Data.Configurations;

public sealed class OrdenServicioConfiguration : IEntityTypeConfiguration<OrdenServicio>
{
    public void Configure(EntityTypeBuilder<OrdenServicio> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NumeroOrden)
            .HasMaxLength(20)
            .HasDefaultValueSql($"'OS-' || lpad(nextval('{PersonalizacionDbContext.Schema}.{PersonalizacionDbContext.NumeroOrdenSequence}')::text, 6, '0')")
            .ValueGeneratedOnAdd();
        builder.HasIndex(x => x.NumeroOrden).IsUnique();
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.TotalCotizado).HasPrecision(18, 2);
        builder.Property(x => x.TotalFinal).HasPrecision(18, 2);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_orden_estado",
                $"estado IN ({PersistenceExtensions.SqlValuesOf<EstadoOrdenServicio>()})");
            t.HasCheckConstraint("ck_orden_total_cotizado", "total_cotizado >= 0");
            t.HasCheckConstraint("ck_orden_total_final", "total_final IS NULL OR total_final >= 0");
            t.HasCheckConstraint("ck_orden_fechas", "vence_en >= solicitado_en");
        });
    }
}

public sealed class DetalleOrdenServicioConfiguration : IEntityTypeConfiguration<DetalleOrdenServicio>
{
    public void Configure(EntityTypeBuilder<DetalleOrdenServicio> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Descripcion).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Cantidad).HasPrecision(18, 3);
        builder.Property(x => x.PrecioUnitario).HasPrecision(18, 2);
        builder.HasOne<OrdenServicio>().WithMany().HasForeignKey(x => x.OrdenServicioId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_detalle_orden_cantidad", "cantidad > 0");
            t.HasCheckConstraint("ck_detalle_orden_precio", "precio_unitario >= 0");
        });
    }
}

public sealed class TecnicaPersonalizacionConfiguration : IEntityTypeConfiguration<TecnicaPersonalizacion>
{
    public void Configure(EntityTypeBuilder<TecnicaPersonalizacion> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.Nombre).IsUnique();
        builder.Property(x => x.Activo).HasDefaultValue(true);
    }
}

public sealed class LineaOrdenTecnicaConfiguration : IEntityTypeConfiguration<LineaOrdenTecnica>
{
    public void Configure(EntityTypeBuilder<LineaOrdenTecnica> builder)
    {
        builder.HasKey(x => new { x.DetalleOrdenServicioId, x.TecnicaId });
        builder.HasOne<DetalleOrdenServicio>().WithMany().HasForeignKey(x => x.DetalleOrdenServicioId);
        builder.HasOne<TecnicaPersonalizacion>().WithMany().HasForeignKey(x => x.TecnicaId);
    }
}
