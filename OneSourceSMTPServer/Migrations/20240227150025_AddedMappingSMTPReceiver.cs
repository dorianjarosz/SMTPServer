using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSourceSMTPServer.Migrations
{
    /// <inheritdoc />
    public partial class AddedMappingSMTPReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sys_MappingSMTPReceiver",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuEntryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    section = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    dataAccess = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    toEmail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    destinationInstance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    containsString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    discardInternal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    isEnabled = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    mode = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_MappingSMTPReceiver", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_MappingSMTPReceiver");
        }
    }
}
