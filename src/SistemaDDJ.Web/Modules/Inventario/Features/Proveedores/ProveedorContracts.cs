namespace SistemaDDJ.Web.Modules.Inventario.Features.Proveedores;

public sealed record TelefonoProveedorDto(string Numero, string? Etiqueta = null, bool EsPrincipal = false);

public sealed record ProveedorDto(
    int Id,
    string RazonSocial,
    string? IdentificacionFiscal,
    string? Correo,
    bool Activo,
    IReadOnlyList<TelefonoProveedorDto> Telefonos);

internal static class ProveedorValidacion
{
    public static (string RazonSocial, string? IdentificacionFiscal, string? Correo, IReadOnlyList<TelefonoProveedorDto> Telefonos)
        Normalizar(string razonSocial, string? identificacionFiscal, string? correo, IReadOnlyList<TelefonoProveedorDto>? telefonos, List<string> errores)
    {
        var razon = razonSocial?.Trim() ?? string.Empty;
        if (razon.Length is 0 or > 150)
        {
            errores.Add("La razón social es obligatoria y puede tener hasta 150 caracteres.");
        }

        var fiscal = string.IsNullOrWhiteSpace(identificacionFiscal)
            ? null
            : identificacionFiscal.Trim().ToUpperInvariant();
        if (fiscal is { Length: > 30 })
        {
            errores.Add("La identificación fiscal puede tener hasta 30 caracteres.");
        }

        var correoNormalizado = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim();
        if (correoNormalizado is { Length: > 255 })
        {
            errores.Add("El correo puede tener hasta 255 caracteres.");
        }

        var normalizados = new List<TelefonoProveedorDto>();
        var numeros = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var telefono in telefonos ?? [])
        {
            var numero = telefono.Numero?.Trim() ?? string.Empty;
            if (numero.Length is 0 or > 30)
            {
                errores.Add("Cada teléfono debe tener un número de hasta 30 caracteres.");
                continue;
            }

            if (!numeros.Add(numero))
            {
                errores.Add($"El teléfono '{numero}' está repetido en el proveedor.");
                continue;
            }

            var etiqueta = string.IsNullOrWhiteSpace(telefono.Etiqueta) ? null : telefono.Etiqueta.Trim();
            if (etiqueta is { Length: > 50 })
            {
                errores.Add("La etiqueta del teléfono puede tener hasta 50 caracteres.");
            }

            normalizados.Add(new TelefonoProveedorDto(numero, etiqueta, telefono.EsPrincipal));
        }

        if (normalizados.Count(t => t.EsPrincipal) > 1)
        {
            errores.Add("Solo un teléfono puede marcarse como principal.");
        }

        return (razon, fiscal, correoNormalizado, normalizados);
    }
}
