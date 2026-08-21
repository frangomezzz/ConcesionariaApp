using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConcesionariaApp.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditQueryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Fecha_UsuarioId_Accion",
                table: "RegistrosAuditoria",
                columns: new[] { "Fecha", "UsuarioId", "Accion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegistrosAuditoria_Fecha_UsuarioId_Accion",
                table: "RegistrosAuditoria");
        }
    }
}
