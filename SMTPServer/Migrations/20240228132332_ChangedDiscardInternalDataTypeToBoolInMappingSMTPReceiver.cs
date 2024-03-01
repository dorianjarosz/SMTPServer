using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMTPServer.Migrations
{
    /// <inheritdoc />
    public partial class ChangedDiscardInternalDataTypeToBoolInMappingSMTPReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "discardInternal",
                table: "sys_MappingSMTPReceiver",
                type: "bit",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "discardInternal",
                table: "sys_MappingSMTPReceiver",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);
        }
    }
}
