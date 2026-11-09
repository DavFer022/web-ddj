namespace SistemaDDJ.Web.Modules.Acceso.Entities;

public sealed class Usuario
{
    public int Id { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string NombreVisible { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTimeOffset CreadoEn { get; set; }
}

public sealed class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class UsuarioRol
{
    public int UsuarioId { get; set; }
    public int RolId { get; set; }
}
