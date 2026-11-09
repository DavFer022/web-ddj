using Microsoft.EntityFrameworkCore;

namespace SistemaDDJ.Web.Infrastructure.Persistence.Integracion;

// Sin entidades: solo aplica las claves foráneas que cruzan esquemas de módulos.
// Debe migrarse después de los DbContext de todos los módulos.
public sealed class IntegracionDbContext(DbContextOptions<IntegracionDbContext> options) : DbContext(options)
{
    public const string Schema = "integracion";
}
