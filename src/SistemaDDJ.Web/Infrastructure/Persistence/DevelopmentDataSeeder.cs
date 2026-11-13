using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using SistemaDDJ.Web.Infrastructure.Persistence.Integracion;
using SistemaDDJ.Web.Modules.Acceso.Data;
using SistemaDDJ.Web.Modules.Acceso.Data.Configurations;
using SistemaDDJ.Web.Modules.Acceso.Entities;
using SistemaDDJ.Web.Modules.Clientes.Data;
using SistemaDDJ.Web.Modules.Clientes.Entities;
using SistemaDDJ.Web.Modules.Inventario.Data;
using SistemaDDJ.Web.Modules.Inventario.Entities;
using SistemaDDJ.Web.Modules.Notificaciones.Data;
using SistemaDDJ.Web.Modules.Notificaciones.Entities;
using SistemaDDJ.Web.Modules.Personalizacion.Data;
using SistemaDDJ.Web.Modules.Personalizacion.Entities;

namespace SistemaDDJ.Web.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var acceso = services.GetRequiredService<AccesoDbContext>();
        var inventario = services.GetRequiredService<InventarioDbContext>();
        var clientes = services.GetRequiredService<ClientesDbContext>();
        var personalizacion = services.GetRequiredService<PersonalizacionDbContext>();
        var notificaciones = services.GetRequiredService<NotificacionesDbContext>();
        var integracion = services.GetRequiredService<IntegracionDbContext>();
        DbContext[] contextos = [acceso, inventario, clientes, personalizacion, notificaciones, integracion];

        if (!await acceso.Database.CanConnectAsync(cancellationToken)) return;

        // Todos los contextos comparten una conexión para participar en una misma transacción.
        var conexion = acceso.Database.GetDbConnection();
        foreach (var contexto in contextos.Skip(1))
        {
            contexto.Database.SetDbConnection(conexion, contextOwnsConnection: false);
        }

        foreach (var contexto in contextos)
        {
            if ((await contexto.Database.GetPendingMigrationsAsync(cancellationToken)).Any()) return;
        }
        if (await acceso.Usuarios.AnyAsync(cancellationToken)) return;

        await using var transaccion = await acceso.Database.BeginTransactionAsync(cancellationToken);
        foreach (var contexto in contextos.Skip(1))
        {
            await contexto.Database.UseTransactionAsync(transaccion.GetDbTransaction(), cancellationToken);
        }

        var ahora = DateTimeOffset.UtcNow;

        // 1. Acceso: usuarios y roles (IDs int generados al guardar).
        var admin = new Usuario { Correo = "admin@ddj.local", NombreVisible = "Administrador DD&J", CreadoEn = ahora };
        var trabajador = new Usuario { Correo = "trabajador@ddj.local", NombreVisible = "Trabajador de prueba", CreadoEn = ahora };
        var usuarioInactivo = new Usuario { Correo = "inactivo@ddj.local", NombreVisible = "Usuario inactivo", Activo = false, CreadoEn = ahora };
        acceso.Usuarios.AddRange(admin, trabajador, usuarioInactivo);
        await acceso.SaveChangesAsync(cancellationToken);
        acceso.UsuarioRoles.AddRange(
            new UsuarioRol { UsuarioId = admin.Id, RolId = RolIds.Admin },
            new UsuarioRol { UsuarioId = trabajador.Id, RolId = RolIds.Trabajador },
            new UsuarioRol { UsuarioId = usuarioInactivo.Id, RolId = RolIds.Trabajador });
        await acceso.SaveChangesAsync(cancellationToken);

        // 2. Inventario: catálogo (categorías, artículos, proveedores).
        var ropa = new CategoriaArticulo { Nombre = "Ropa", Descripcion = "Prendas base para personalizar" };
        var tazas = new CategoriaArticulo { Nombre = "Tazas y accesorios", Descripcion = "Artículos sublimables" };
        var insumosSublimacion = new CategoriaArticulo { Nombre = "Insumos de sublimación", Descripcion = "Papeles y tintas de transferencia" };
        var insumosDtfVinil = new CategoriaArticulo { Nombre = "Insumos DTF y vinil", Descripcion = "Películas DTF y vinil textil" };
        inventario.CategoriasArticulo.AddRange(ropa, tazas, insumosSublimacion, insumosDtfVinil);
        await inventario.SaveChangesAsync(cancellationToken);

        var franelaM = NuevoArticulo("PRD-001", "Franela blanca M", UnidadMedida.UNIDAD, ArticuloTipo.PRODUCTO, 4.50m, ropa.Id, stockMinimo: 5);
        var franelaL = NuevoArticulo("PRD-002", "Franela blanca L", UnidadMedida.UNIDAD, ArticuloTipo.PRODUCTO, 4.50m, ropa.Id, stockMinimo: 5);
        var taza = NuevoArticulo("PRD-003", "Taza blanca 11 oz", UnidadMedida.UNIDAD, ArticuloTipo.PRODUCTO, 1.20m, tazas.Id, stockMinimo: 10);
        var papelSublimacion = NuevoArticulo("INS-001", "Papel de sublimación A4", UnidadMedida.HOJA, ArticuloTipo.INSUMO, 0.30m, insumosSublimacion.Id, stockMinimo: 50);
        var tintaSublimacion = NuevoArticulo("INS-002", "Tinta de sublimación cian", UnidadMedida.ML, ArticuloTipo.INSUMO, 0.08m, insumosSublimacion.Id, stockMinimo: 100);
        var vinilNegro = NuevoArticulo("INS-003", "Vinil textil negro", UnidadMedida.METRO, ArticuloTipo.INSUMO, 3.50m, insumosDtfVinil.Id, stockMinimo: 3);
        var peliculaDtf = NuevoArticulo("INS-004", "Película DTF A3", UnidadMedida.HOJA, ArticuloTipo.INSUMO, 1.20m, insumosDtfVinil.Id, stockMinimo: 100);
        inventario.Articulos.AddRange(franelaM, franelaL, taza, papelSublimacion, tintaSublimacion, vinilNegro, peliculaDtf);
        await inventario.SaveChangesAsync(cancellationToken);

        inventario.Productos.AddRange(
            new Producto { ArticuloId = franelaM.Id, Sku = "FRN-BLA-M", PrecioVenta = 12.00m },
            new Producto { ArticuloId = franelaL.Id, Sku = "FRN-BLA-L", PrecioVenta = 12.00m },
            new Producto { ArticuloId = taza.Id, Sku = "TAZ-BLA-11", PrecioVenta = 4.00m });
        inventario.Insumos.AddRange(
            new Insumo { ArticuloId = papelSublimacion.Id, Descripcion = "Papel para transferencia por sublimación" },
            new Insumo { ArticuloId = tintaSublimacion.Id, Descripcion = "Tinta para impresora de sublimación" },
            new Insumo { ArticuloId = vinilNegro.Id, Descripcion = "Vinil textil para corte y prensado" },
            new Insumo { ArticuloId = peliculaDtf.Id, Descripcion = "Película para transferencia DTF" });

        var textiles = new Proveedor
        {
            RazonSocial = "Textiles del Centro C.A.",
            IdentificacionFiscal = "J-30111222-1",
            Correo = "ventas@textilescentro.example",
            Telefonos =
            [
                new TelefonoProveedor { Numero = "0412-5550101", Etiqueta = "Ventas", EsPrincipal = true },
                new TelefonoProveedor { Numero = "0414-5550102", Etiqueta = "Administración" }
            ]
        };
        var sublimacionExpress = new Proveedor
        {
            RazonSocial = "Sublimación Express",
            Telefonos = [new TelefonoProveedor { Numero = "0414-5550202", Etiqueta = "Pedidos", EsPrincipal = true }]
        };
        var proveedorInactivo = new Proveedor { RazonSocial = "Proveedor descontinuado", IdentificacionFiscal = "J-40000000-0", Activo = false };
        inventario.Proveedores.AddRange(textiles, sublimacionExpress, proveedorInactivo);
        await inventario.SaveChangesAsync(cancellationToken);

        // 3. Clientes: clientes y beneficios de fidelidad.
        var maria = new Cliente { Nombre = "María", Apellido = "Pérez", Correo = "maria@example.com", Telefono = "0412-1110001", CreadoEn = ahora };
        var luis = new Cliente { Nombre = "Luis", Apellido = "Gómez", Telefono = "0424-2220002", CreadoEn = ahora };
        clientes.Clientes.AddRange(maria, luis);
        var beneficioTaza = new BeneficioFidelidad { Nombre = "Taza gratis", Descripcion = "Una taza blanca 11 oz sin costo", CostoPuntos = 50 };
        clientes.BeneficiosFidelidad.AddRange(
            beneficioTaza,
            new BeneficioFidelidad { Nombre = "10% de descuento en próxima compra", Descripcion = "Aplica sobre el total de la compra", CostoPuntos = 100 },
            new BeneficioFidelidad { Nombre = "Beneficio descontinuado", CostoPuntos = 30, Activo = false });
        await clientes.SaveChangesAsync(cancellationToken);

        // 4. Personalización: técnicas y órdenes de servicio.
        var sublimacion = new TecnicaPersonalizacion { Nombre = "Sublimación", Descripcion = "Transferencia de tinta sobre poliéster" };
        var dtf = new TecnicaPersonalizacion { Nombre = "DTF", Descripcion = "Impresión directa a película para transferir" };
        var vinil = new TecnicaPersonalizacion { Nombre = "Vinil textil", Descripcion = "Corte y prensado de vinil" };
        personalizacion.TecnicasPersonalizacion.AddRange(sublimacion, dtf, vinil);

        var orden1 = new OrdenServicio
        {
            Estado = EstadoOrdenServicio.EN_PROCESO, SolicitadoEn = ahora.AddDays(-5), VenceEn = ahora.AddDays(3),
            TotalCotizado = 60.00m, Notas = "Franelas personalizadas para evento", ClienteId = maria.Id,
            CreadoPorUsuarioId = admin.Id, AsignadoAUsuarioId = trabajador.Id
        };
        var orden2 = new OrdenServicio
        {
            Estado = EstadoOrdenServicio.PENDIENTE, SolicitadoEn = ahora.AddDays(-1), VenceEn = ahora.AddDays(7),
            TotalCotizado = 25.00m, ClienteId = luis.Id, CreadoPorUsuarioId = trabajador.Id
        };
        personalizacion.OrdenesServicio.AddRange(orden1, orden2);
        await personalizacion.SaveChangesAsync(cancellationToken);

        var detalleVinil = new DetalleOrdenServicio { OrdenServicioId = orden1.Id, InsumoId = vinilNegro.Id, Descripcion = "Estampado vinil en pecho", Cantidad = 2, PrecioUnitario = 15.00m };
        var detalleDtf = new DetalleOrdenServicio { OrdenServicioId = orden1.Id, InsumoId = peliculaDtf.Id, Descripcion = "Estampado DTF en espalda", Cantidad = 2, PrecioUnitario = 15.00m, Notas = "Prenda recibida del cliente" };
        var detalleTaza = new DetalleOrdenServicio { OrdenServicioId = orden2.Id, InsumoId = papelSublimacion.Id, Descripcion = "Sublimación en taza 11 oz", Cantidad = 5, PrecioUnitario = 5.00m };
        personalizacion.DetallesOrdenServicio.AddRange(detalleVinil, detalleDtf, detalleTaza);
        await personalizacion.SaveChangesAsync(cancellationToken);
        personalizacion.LineasOrdenTecnica.AddRange(
            new LineaOrdenTecnica { DetalleOrdenServicioId = detalleVinil.Id, TecnicaId = vinil.Id },
            new LineaOrdenTecnica { DetalleOrdenServicioId = detalleDtf.Id, TecnicaId = dtf.Id },
            new LineaOrdenTecnica { DetalleOrdenServicioId = detalleTaza.Id, TecnicaId = sublimacion.Id });
        await personalizacion.SaveChangesAsync(cancellationToken);

        // 5. Inventario: movimientos (las salidas por consumo referencian las órdenes ya guardadas).
        var compraTextiles = Movimiento(TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.COMPRA, ahora.AddDays(-15), admin.Id, "FAC-0001", proveedorId: textiles.Id);
        var compraSublimacion = Movimiento(TipoMovimientoInventario.ENTRADA, MotivoMovimientoInventario.COMPRA, ahora.AddDays(-14), admin.Id, "FAC-0002", proveedorId: sublimacionExpress.Id);
        var venta = Movimiento(TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.VENTA, ahora.AddDays(-10), trabajador.Id, "VTA-0001", clienteId: maria.Id);
        var ventaCredito = Movimiento(TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.VENTA, ahora.AddDays(-2), trabajador.Id, "VTA-0002", clienteId: luis.Id);
        var ajuste = Movimiento(TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.AJUSTE, ahora.AddDays(-1), admin.Id, notas: "Conteo físico: 2 tazas con defecto de fábrica");
        var consumoVinil = Movimiento(TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.CONSUMO, ahora.AddDays(-4), trabajador.Id, ordenServicioId: orden1.Id);
        var consumoDtf = Movimiento(TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.CONSUMO, ahora.AddDays(-4), trabajador.Id, ordenServicioId: orden1.Id);
        var mermaDtf = Movimiento(TipoMovimientoInventario.SALIDA, MotivoMovimientoInventario.MERMA, ahora.AddDays(-4), trabajador.Id, ordenServicioId: orden1.Id, notas: "Prueba de estampado dañada");
        inventario.MovimientosInventario.AddRange(compraTextiles, compraSublimacion, venta, ventaCredito, ajuste, consumoVinil, consumoDtf, mermaDtf);
        inventario.DetallesMovimientoInventario.AddRange(
            Detalle(compraTextiles.Id, franelaM.Id, 20, costo: 4.50m),
            Detalle(compraTextiles.Id, franelaL.Id, 20, costo: 4.50m),
            Detalle(compraTextiles.Id, vinilNegro.Id, 10, costo: 3.50m),
            Detalle(compraSublimacion.Id, papelSublimacion.Id, 200, costo: 0.30m),
            Detalle(compraSublimacion.Id, tintaSublimacion.Id, 500, costo: 0.08m),
            Detalle(compraSublimacion.Id, peliculaDtf.Id, 100, costo: 1.20m),
            Detalle(compraSublimacion.Id, taza.Id, 50, costo: 1.20m),
            Detalle(venta.Id, franelaM.Id, 3, precio: 12.00m),
            Detalle(ventaCredito.Id, taza.Id, 4, precio: 4.00m),
            Detalle(ajuste.Id, taza.Id, 2, costo: 1.20m),
            Detalle(consumoVinil.Id, vinilNegro.Id, 1.2m, costo: 3.50m),
            Detalle(consumoDtf.Id, peliculaDtf.Id, 2, costo: 1.20m),
            Detalle(mermaDtf.Id, peliculaDtf.Id, 1, costo: 1.20m));
        await inventario.SaveChangesAsync(cancellationToken);

        // 6. Clientes: cuenta corriente y fidelidad (los cargos referencian órdenes y movimientos ya guardados).
        clientes.MovimientosCuentaCliente.AddRange(
            new MovimientoCuentaCliente { TipoMovimiento = TipoMovimientoCuenta.CARGO, Monto = 60.00m, OcurridoEn = ahora.AddDays(-5), ClienteId = maria.Id, OrdenServicioId = orden1.Id, RegistradoPorUsuarioId = admin.Id },
            new MovimientoCuentaCliente { TipoMovimiento = TipoMovimientoCuenta.PAGO, Monto = 30.00m, MetodoPago = "efectivo", Notas = "Pago inicial 50%", OcurridoEn = ahora.AddDays(-5), ClienteId = maria.Id, RegistradoPorUsuarioId = trabajador.Id },
            new MovimientoCuentaCliente { TipoMovimiento = TipoMovimientoCuenta.PAGO, Monto = 10.00m, MetodoPago = "pago móvil", Referencia = "PM-4455", OcurridoEn = ahora.AddDays(-3), ClienteId = maria.Id, RegistradoPorUsuarioId = trabajador.Id },
            new MovimientoCuentaCliente { TipoMovimiento = TipoMovimientoCuenta.CARGO, Monto = 25.00m, OcurridoEn = ahora.AddDays(-1), ClienteId = luis.Id, OrdenServicioId = orden2.Id, RegistradoPorUsuarioId = trabajador.Id },
            new MovimientoCuentaCliente { TipoMovimiento = TipoMovimientoCuenta.PAGO, Monto = 13.00m, MetodoPago = "transferencia", Referencia = "TRX-998877", Notas = "Pago inicial 52%", OcurridoEn = ahora.AddDays(-1), ClienteId = luis.Id, RegistradoPorUsuarioId = trabajador.Id },
            new MovimientoCuentaCliente { TipoMovimiento = TipoMovimientoCuenta.CARGO, Monto = 16.00m, OcurridoEn = ahora.AddDays(-2), ClienteId = luis.Id, MovimientoInventarioId = ventaCredito.Id, RegistradoPorUsuarioId = trabajador.Id });
        clientes.MovimientosFidelidadCliente.AddRange(
            new MovimientoFidelidadCliente { VariacionPuntos = 120, Motivo = "Acumulación de saldo inicial de prueba", OcurridoEn = ahora.AddDays(-20), ClienteId = maria.Id, RegistradoPorUsuarioId = admin.Id },
            new MovimientoFidelidadCliente { VariacionPuntos = -50, Motivo = "Canje de taza gratis", OcurridoEn = ahora.AddDays(-6), ClienteId = maria.Id, BeneficioFidelidadId = beneficioTaza.Id, RegistradoPorUsuarioId = trabajador.Id });
        await clientes.SaveChangesAsync(cancellationToken);

        // 7. Notificaciones.
        notificaciones.Notificaciones.AddRange(
            new Notificacion { Titulo = "Orden por vencer", Mensaje = "Una orden de servicio vence en 3 días.", Canal = CanalNotificacion.CORREO, Estado = EstadoNotificacion.ENVIADA, CreadoEn = ahora.AddHours(-2), EnviadoEn = ahora.AddHours(-1), UsuarioId = admin.Id },
            new Notificacion { Titulo = "Pedido en proceso", Mensaje = "Tu pedido está en proceso de personalización.", Canal = CanalNotificacion.WHATSAPP, Estado = EstadoNotificacion.PENDIENTE, CreadoEn = ahora, ClienteId = maria.Id },
            new Notificacion { Titulo = "Orden asignada", Mensaje = "Se te asignó la orden de María Pérez.", Canal = CanalNotificacion.CORREO, Estado = EstadoNotificacion.LEIDA, CreadoEn = ahora.AddDays(-5), EnviadoEn = ahora.AddDays(-5), LeidoEn = ahora.AddDays(-5), UsuarioId = trabajador.Id });
        await notificaciones.SaveChangesAsync(cancellationToken);

        await transaccion.CommitAsync(cancellationToken);
    }

    private static Articulo NuevoArticulo(
        string codigo,
        string nombre,
        UnidadMedida unidad,
        ArticuloTipo tipo,
        decimal costo,
        int categoriaId,
        decimal stockMinimo = 0) =>
        new()
        {
            Codigo = codigo, Nombre = nombre, Unidad = unidad, Tipo = tipo, CostoUnitario = costo,
            StockMinimo = stockMinimo, CategoriaId = categoriaId
        };

    private static MovimientoInventario Movimiento(
        TipoMovimientoInventario tipo, MotivoMovimientoInventario motivo, DateTimeOffset ocurridoEn, int registradoPor,
        string? referencia = null, string? notas = null, int? proveedorId = null, Guid? clienteId = null, Guid? ordenServicioId = null) =>
        new()
        {
            TipoMovimiento = tipo, Motivo = motivo, OcurridoEn = ocurridoEn, Referencia = referencia, Notas = notas,
            ProveedorId = proveedorId, ClienteId = clienteId, OrdenServicioId = ordenServicioId, RegistradoPorUsuarioId = registradoPor
        };

    private static DetalleMovimientoInventario Detalle(Guid movimientoId, int articuloId, decimal cantidad, decimal? costo = null, decimal? precio = null) =>
        new() { MovimientoId = movimientoId, ArticuloId = articuloId, Cantidad = cantidad, CostoUnitario = costo, PrecioUnitario = precio };
}
