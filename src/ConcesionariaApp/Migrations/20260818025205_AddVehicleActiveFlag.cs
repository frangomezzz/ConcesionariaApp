using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConcesionariaApp.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleActiveFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Vehiculos",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Vehiculos");
        }
    }
}
