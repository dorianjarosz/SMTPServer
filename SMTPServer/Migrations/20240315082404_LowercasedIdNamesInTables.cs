using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMTPServer.Migrations
{
    /// <inheritdoc />
    public partial class LowercasedIdNamesInTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sys_SMTPLog",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sys_MappingSMTPReceiver",
                newName: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "id",
                table: "sys_SMTPLog",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "sys_MappingSMTPReceiver",
                newName: "Id");
        }
    }
}
