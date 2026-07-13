using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveColorPositionScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Position_X",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Position_Y",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Position_Z",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "Scale",
                table: "Units");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Units",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Position_X",
                table: "Units",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Position_Y",
                table: "Units",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Position_Z",
                table: "Units",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Scale",
                table: "Units",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
