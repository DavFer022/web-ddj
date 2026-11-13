using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SistemaDDJ.Web.Components;

namespace SistemaDDJ.Tests;

public sealed class AppSmokeTests
{
    [Fact]
    public async Task HomePage_ReturnsApplicationShell()
    {
        await using var factory = new WebApplicationFactory<App>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Port=5432;Database=unused;Username=test;Password=test"
                })));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Sistema DD", html);
    }

    [Fact]
    public async Task InventarioPage_ReturnsCatalogForm()
    {
        await using var factory = new WebApplicationFactory<App>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Port=5432;Database=unused;Username=test;Password=test"
                })));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/inventario");

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("Resumen de existencias y catálogo", html);
    }

    [Theory]
    [InlineData("/inventario/catalogo", "Catálogo de inventario")]
    [InlineData("/inventario/proveedores", "Proveedores")]
    [InlineData("/inventario/compras", "Compra a proveedor")]
    [InlineData("/inventario/ventas", "Venta de productos")]
    [InlineData("/inventario/consumos", "Consumo de insumos")]
    [InlineData("/inventario/ajustes", "Ajustes y mermas")]
    [InlineData("/login", "Acceso al sistema")]
    public async Task MudPages_ReturnApplicationShell(string path, string marker)
    {
        await using var factory = new WebApplicationFactory<App>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=localhost;Port=5432;Database=unused;Username=test;Password=test"
                })));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);

        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();

        Assert.Contains("mud-layout", html);
        Assert.Contains(marker, html);
    }
}
