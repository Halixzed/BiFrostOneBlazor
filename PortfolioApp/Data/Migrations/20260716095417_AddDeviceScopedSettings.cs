using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceScopedSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the new Devices table first - needed below to migrate the old singleton
            // AppSettings row / Unit.IsActive flag into it before either is dropped.
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    ActiveUnitId = table.Column<int>(type: "INTEGER", nullable: true),
                    BackgroundColor = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            // Seed the well-known "default" device (backs "/" and "/settings") from whatever the
            // old singleton AppSettings/Unit.IsActive data was, so existing production data
            // (background color, active unit) survives the move to per-device settings instead
            // of silently resetting. This INSERT is guaranteed to produce Id=1 since Devices is a
            // brand-new, empty, autoincrementing table - later steps below rely on that Id=1.
            migrationBuilder.Sql(@"
                INSERT INTO Devices (Key, DisplayName, ActiveUnitId, BackgroundColor)
                SELECT 'default', 'Default',
                       (SELECT Id FROM Units WHERE IsActive = 1 LIMIT 1),
                       COALESCE((SELECT BackgroundColor FROM AppSettings LIMIT 1), '#333333');
            ");

            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Units");

            // Existing Watermark/EnvironmentMap rows (there's at most one of each, from before
            // per-device scoping) belonged to the app's single implicit "screen" - defaulting them
            // to the default device's Id (1, per above) preserves them instead of orphaning them.
            migrationBuilder.AddColumn<int>(
                name: "DeviceId",
                table: "Watermarks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "DeviceId",
                table: "EnvironmentMaps",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "Watermarks");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "EnvironmentMaps");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Units",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BackgroundColor = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Id);
                });
        }
    }
}
