using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaDDJ.Web.Modules.Acceso.Entities;

namespace SistemaDDJ.Web.Modules.Acceso.Data.Configurations;

public static class RolIds
{
    public const int Admin = 1;
    public const int Trabajador = 2;
}

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Correo).HasMaxLength(255).IsRequired();
        builder.HasIndex(x => x.Correo).IsUnique();
        builder.Property(x => x.NombreVisible).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Activo).HasDefaultValue(true);
        builder.Property(x => x.CreadoEn).HasDefaultValueSql("now()");
    }
}

public sealed class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nombre).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Nombre).IsUnique();

        builder.HasData(
            new Rol { Id = RolIds.Admin, Nombre = "Admin" },
            new Rol { Id = RolIds.Trabajador, Nombre = "Trabajador" });
    }
}

public sealed class UsuarioRolConfiguration : IEntityTypeConfiguration<UsuarioRol>
{
    public void Configure(EntityTypeBuilder<UsuarioRol> builder)
    {
        builder.HasKey(x => new { x.UsuarioId, x.RolId });
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId);
        builder.HasOne<Rol>().WithMany().HasForeignKey(x => x.RolId);
    }
}
