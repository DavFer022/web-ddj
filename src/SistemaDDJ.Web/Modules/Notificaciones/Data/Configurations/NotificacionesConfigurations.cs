using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Modules.Notificaciones.Entities;

namespace SistemaDDJ.Web.Modules.Notificaciones.Data.Configurations;

public sealed class NotificacionConfiguration : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Titulo).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Canal).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CreadoEn).HasDefaultValueSql("now()");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_notificacion_canal",
                $"canal IN ({PersistenceExtensions.SqlValuesOf<CanalNotificacion>()})");
            t.HasCheckConstraint("ck_notificacion_estado",
                $"estado IN ({PersistenceExtensions.SqlValuesOf<EstadoNotificacion>()})");
            t.HasCheckConstraint("ck_notificacion_un_destinatario",
                "(usuario_id IS NULL) <> (cliente_id IS NULL)");
        });
    }
}
