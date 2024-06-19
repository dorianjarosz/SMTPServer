using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OneSourceSMTPServer.Migrations
{
    /// <inheritdoc />
    public partial class AddDestinationInstanceVersionToMappingSMTPReceiverTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "destinationInstanceVersion",
                table: "sys_MappingSMTPReceiver",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "destinationInstanceVersion",
                table: "sys_MappingSMTPReceiver");
        }
    }
}
