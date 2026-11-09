namespace SistemaDDJ.Web.Modules.Notificaciones.Entities;

public enum CanalNotificacion
{
    WHATSAPP,
    SMS,
    CORREO
}

public enum EstadoNotificacion
{
    PENDIENTE,
    ENVIADA,
    LEIDA,
    FALLIDA
}

public sealed class Notificacion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Titulo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public CanalNotificacion Canal { get; set; }
    public EstadoNotificacion Estado { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset? EnviadoEn { get; set; }
    public DateTimeOffset? LeidoEn { get; set; }
    public int? UsuarioId { get; set; }
    public Guid? ClienteId { get; set; }
}
