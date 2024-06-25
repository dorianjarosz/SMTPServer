using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSourceSMTPServer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnessessaryColumnsFromMappingsSMTPReceiverTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MenuEntryName",
                table: "sys_MappingSMTPReceiver");

            migrationBuilder.DropColumn(
                name: "category",
                table: "sys_MappingSMTPReceiver");

            migrationBuilder.DropColumn(
                name: "containsString",
                table: "sys_MappingSMTPReceiver");

            migrationBuilder.DropColumn(
                name: "dataAccess",
                table: "sys_MappingSMTPReceiver");

            migrationBuilder.DropColumn(
                name: "discardInternal",
                table: "sys_MappingSMTPReceiver");

            migrationBuilder.DropColumn(
                name: "section",
                table: "sys_MappingSMTPReceiver");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MenuEntryName",
                table: "sys_MappingSMTPReceiver",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "sys_MappingSMTPReceiver",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "containsString",
                table: "sys_MappingSMTPReceiver",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "dataAccess",
                table: "sys_MappingSMTPReceiver",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "discardInternal",
                table: "sys_MappingSMTPReceiver",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "section",
                table: "sys_MappingSMTPReceiver",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
