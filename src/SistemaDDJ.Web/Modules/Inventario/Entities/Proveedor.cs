namespace SistemaDDJ.Web.Modules.Inventario.Entities;

public sealed class Proveedor
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? IdentificacionFiscal { get; set; }
    public string? Correo { get; set; }
    public bool Activo { get; set; } = true;
    public List<TelefonoProveedor> Telefonos { get; set; } = [];
}

public sealed class TelefonoProveedor
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string? Etiqueta { get; set; }
    public bool EsPrincipal { get; set; }
}
