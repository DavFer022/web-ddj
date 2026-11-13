namespace SistemaDDJ.Web.Modules.Inventario.Entities;

public enum ArticuloTipo
{
    PRODUCTO,
    INSUMO
}

public sealed class CategoriaArticulo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

public sealed class Articulo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public UnidadMedida Unidad { get; set; }
    public ArticuloTipo Tipo { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; } = true;
    public int CategoriaId { get; set; }
}

public sealed class Producto
{
    public int ArticuloId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public decimal PrecioVenta { get; set; }
}

public sealed class Insumo
{
    public int ArticuloId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}
