using Microsoft.EntityFrameworkCore;

namespace SistemaDDJ.Web.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options);
