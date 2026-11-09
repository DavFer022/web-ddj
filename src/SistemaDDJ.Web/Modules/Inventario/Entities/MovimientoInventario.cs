namespace SistemaDDJ.Web.Modules.Inventario.Entities;

public enum TipoMovimientoInventario
{
    ENTRADA,
    SALIDA
}

public enum MotivoMovimientoInventario
{
    COMPRA,
    VENTA,
    CONSUMO,
    MERMA,
    AJUSTE
}

public sealed class Proveedor
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string? IdentificacionFiscal { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class MovimientoInventario
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public TipoMovimientoInventario TipoMovimiento { get; set; }
    public MotivoMovimientoInventario Motivo { get; set; }
    public DateTimeOffset OcurridoEn { get; set; }
    public string? Referencia { get; set; }
    public string? Notas { get; set; }
    public int? ProveedorId { get; set; }
    public Guid? ClienteId { get; set; }
    public Guid? OrdenServicioId { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
}

public sealed class DetalleMovimientoInventario
{
    public int Id { get; set; }
    public Guid MovimientoId { get; set; }
    public int ArticuloId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal? CostoUnitario { get; set; }
    public decimal? PrecioUnitario { get; set; }
}
