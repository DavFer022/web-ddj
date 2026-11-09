using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Clientes.Entities;

namespace SistemaDDJ.Web.Modules.Clientes.Data.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Apellido).HasMaxLength(100);
        builder.Property(x => x.Correo).HasMaxLength(255);
        builder.Property(x => x.Telefono).HasMaxLength(30);
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.CreadoEn).HasDefaultValueSql("now()");
    }
}

public sealed class MovimientoCuentaClienteConfiguration : IEntityTypeConfiguration<MovimientoCuentaCliente>
{
    public void Configure(EntityTypeBuilder<MovimientoCuentaCliente> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TipoMovimiento).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Monto).HasPrecision(18, 2);
        builder.Property(x => x.MetodoPago).HasMaxLength(50);
        builder.Property(x => x.Referencia).HasMaxLength(100);
        builder.Property(x => x.OcurridoEn).HasDefaultValueSql("now()");
        builder.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_cuenta_tipo",
                $"tipo_movimiento IN ({PersistenceExtensions.SqlValuesOf<TipoMovimientoCuenta>()})");
            t.HasCheckConstraint("ck_cuenta_monto", "monto > 0");
            t.HasCheckConstraint("ck_cuenta_cargo_referencia",
                "tipo_movimiento <> 'CARGO' OR ((orden_servicio_id IS NULL) <> (movimiento_inventario_id IS NULL))");
            t.HasCheckConstraint("ck_cuenta_pago_sin_referencia",
                "tipo_movimiento <> 'PAGO' OR (orden_servicio_id IS NULL AND movimiento_inventario_id IS NULL AND metodo_pago IS NOT NULL)");
        });
    }
}

public sealed class BeneficioFidelidadConfiguration : IEntityTypeConfiguration<BeneficioFidelidad>
{
    public void Configure(EntityTypeBuilder<BeneficioFidelidad> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Activo).HasDefaultValue(true);

        builder.ToTable(t => t.HasCheckConstraint("ck_beneficio_costo_puntos", "costo_puntos > 0"));
    }
}

public sealed class MovimientoFidelidadClienteConfiguration : IEntityTypeConfiguration<MovimientoFidelidadCliente>
{
    public void Configure(EntityTypeBuilder<MovimientoFidelidadCliente> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Motivo).HasMaxLength(200).IsRequired();
        builder.Property(x => x.OcurridoEn).HasDefaultValueSql("now()");
        builder.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId);
        builder.HasOne<BeneficioFidelidad>().WithMany().HasForeignKey(x => x.BeneficioFidelidadId);

        builder.ToTable(t => t.HasCheckConstraint("ck_fidelidad_variacion", "variacion_puntos <> 0"));
    }
}
