using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaDDJ.Web.Modules.Notificaciones.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notificaciones");

            migrationBuilder.CreateTable(
                name: "notificacion",
                schema: "notificaciones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    mensaje = table.Column<string>(type: "text", nullable: false),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    enviado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    leido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificacion", x => x.id);
                    table.CheckConstraint("ck_notificacion_canal", "canal IN ('WHATSAPP', 'SMS', 'CORREO')");
                    table.CheckConstraint("ck_notificacion_estado", "estado IN ('PENDIENTE', 'ENVIADA', 'LEIDA', 'FALLIDA')");
                    table.CheckConstraint("ck_notificacion_un_destinatario", "(usuario_id IS NULL) <> (cliente_id IS NULL)");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notificacion",
                schema: "notificaciones");
        }
    }
}
