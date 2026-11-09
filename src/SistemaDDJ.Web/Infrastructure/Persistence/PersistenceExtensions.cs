using Microsoft.EntityFrameworkCore;

namespace SistemaDDJ.Web.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public static string ToSnakeCase(this string value) =>
        string.Concat(value.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? $"_{char.ToLowerInvariant(c)}" : char.ToLowerInvariant(c).ToString()));

    public static string SqlValuesOf<TEnum>() where TEnum : struct, Enum =>
        string.Join(", ", Enum.GetNames<TEnum>().Select(name => $"'{name}'"));

    public static void ApplyModuleConventions(this ModelBuilder modelBuilder, string schema)
    {
        modelBuilder.HasDefaultSchema(schema);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(entity.ClrType.Name.ToSnakeCase());
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.Name.ToSnakeCase());
            }
        }

        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
