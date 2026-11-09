namespace SistemaDDJ.Web.Modules.Personalizacion.Entities;

public enum EstadoOrdenServicio
{
    PENDIENTE,
    EN_PROCESO,
    LISTO,
    ENTREGADO,
    CANCELADO
}

public sealed class OrdenServicio
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string NumeroOrden { get; set; } = null!;
    public EstadoOrdenServicio Estado { get; set; }
    public DateTimeOffset SolicitadoEn { get; set; }
    public DateTimeOffset VenceEn { get; set; }
    public decimal TotalCotizado { get; set; }
    public decimal? TotalFinal { get; set; }
    public string? Notas { get; set; }
    public Guid ClienteId { get; set; }
    public int CreadoPorUsuarioId { get; set; }
    public int? AsignadoAUsuarioId { get; set; }
}

public sealed class DetalleOrdenServicio
{
    public int Id { get; set; }
    public Guid OrdenServicioId { get; set; }
    public int? InsumoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public string? Notas { get; set; }
}

public sealed class TecnicaPersonalizacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class LineaOrdenTecnica
{
    public int DetalleOrdenServicioId { get; set; }
    public int TecnicaId { get; set; }
}
