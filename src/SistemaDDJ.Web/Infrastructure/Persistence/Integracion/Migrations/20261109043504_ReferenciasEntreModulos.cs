using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDDJ.Web.Infrastructure.Persistence.Integracion.Migrations
{
    /// <inheritdoc />
    public partial class ReferenciasEntreModulos : Migration
    {
        private static readonly string[] Referencias =
        [
            // (tabla, columna, tabla_referenciada, columna_referenciada)
            "inventario.movimiento_inventario|cliente_id|clientes.cliente|id",
            "inventario.movimiento_inventario|orden_servicio_id|personalizacion.orden_servicio|id",
            "inventario.movimiento_inventario|registrado_por_usuario_id|acceso.usuario|id",
            "personalizacion.orden_servicio|cliente_id|clientes.cliente|id",
            "personalizacion.orden_servicio|creado_por_usuario_id|acceso.usuario|id",
            "personalizacion.orden_servicio|asignado_a_usuario_id|acceso.usuario|id",
            "personalizacion.detalle_orden_servicio|insumo_id|inventario.insumo|articulo_id",
            "clientes.movimiento_cuenta_cliente|orden_servicio_id|personalizacion.orden_servicio|id",
            "clientes.movimiento_cuenta_cliente|movimiento_inventario_id|inventario.movimiento_inventario|id",
            "clientes.movimiento_cuenta_cliente|registrado_por_usuario_id|acceso.usuario|id",
            "clientes.movimiento_fidelidad_cliente|registrado_por_usuario_id|acceso.usuario|id",
            "notificaciones.notificacion|usuario_id|acceso.usuario|id",
            "notificaciones.notificacion|cliente_id|clientes.cliente|id",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var referencia in Referencias)
            {
                var (tabla, columna, tablaReferenciada, columnaReferenciada) = Separar(referencia);
                var nombre = $"fk_{tabla.Split('.')[1]}_{columna}";

                migrationBuilder.Sql($"CREATE INDEX IF NOT EXISTS \"IX_{tabla.Split('.')[1]}_{columna}\" ON {tabla} ({columna});");
                migrationBuilder.Sql(
                    $"ALTER TABLE {tabla} ADD CONSTRAINT \"{nombre}\" FOREIGN KEY ({columna}) " +
                    $"REFERENCES {tablaReferenciada} ({columnaReferenciada}) ON DELETE RESTRICT;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var referencia in Referencias)
            {
                var (tabla, columna, _, _) = Separar(referencia);
                var nombre = $"fk_{tabla.Split('.')[1]}_{columna}";

                migrationBuilder.Sql($"ALTER TABLE {tabla} DROP CONSTRAINT IF EXISTS \"{nombre}\";");
                migrationBuilder.Sql($"DROP INDEX IF EXISTS {tabla.Split('.')[0]}.\"IX_{tabla.Split('.')[1]}_{columna}\";");
            }
        }

        private static (string Tabla, string Columna, string TablaReferenciada, string ColumnaReferenciada) Separar(string referencia)
        {
            var partes = referencia.Split('|');
            return (partes[0], partes[1], partes[2], partes[3]);
        }
    }
}
