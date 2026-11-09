namespace SistemaDDJ.Web.Modules.Clientes.Entities;

public enum TipoMovimientoCuenta
{
    CARGO,
    PAGO
}

public sealed class Cliente
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Nombre { get; set; } = string.Empty;
    public string? Apellido { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public bool Activo { get; set; } = true;
    public DateTimeOffset CreadoEn { get; set; }
}

public sealed class MovimientoCuentaCliente
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public TipoMovimientoCuenta TipoMovimiento { get; set; }
    public decimal Monto { get; set; }
    public string? MetodoPago { get; set; }
    public string? Referencia { get; set; }
    public DateTimeOffset OcurridoEn { get; set; }
    public string? Notas { get; set; }
    public Guid ClienteId { get; set; }
    public Guid? OrdenServicioId { get; set; }
    public Guid? MovimientoInventarioId { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
}

public sealed class BeneficioFidelidad
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CostoPuntos { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class MovimientoFidelidadCliente
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public int VariacionPuntos { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTimeOffset OcurridoEn { get; set; }
    public Guid ClienteId { get; set; }
    public int? BeneficioFidelidadId { get; set; }
    public int RegistradoPorUsuarioId { get; set; }
}
