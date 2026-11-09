using SistemaDDJ.Web.Components;
using SistemaDDJ.Web.Infrastructure.Persistence;
using SistemaDDJ.Web.Infrastructure.Persistence.Integracion;
using SistemaDDJ.Web.Modules.Acceso;
using SistemaDDJ.Web.Modules.Clientes;
using SistemaDDJ.Web.Modules.Inventario;
using SistemaDDJ.Web.Modules.Notificaciones;
using SistemaDDJ.Web.Modules.Personalizacion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'DefaultConnection'. Configúrala con .NET User Secrets; consulta el README.md.");
}

builder.Services
    .AddAccesoModule(connectionString)
    .AddClientesModule(connectionString)
    .AddInventarioModule(connectionString)
    .AddNotificacionesModule(connectionString)
    .AddPersonalizacionModule(connectionString)
    .AddModuleDbContext<IntegracionDbContext>(connectionString, IntegracionDbContext.Schema);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    using var scope = app.Services.CreateScope();
    await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
