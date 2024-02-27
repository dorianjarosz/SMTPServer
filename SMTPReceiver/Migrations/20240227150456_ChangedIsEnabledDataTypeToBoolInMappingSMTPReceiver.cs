using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMTPReceiver.Migrations
{
    /// <inheritdoc />
    public partial class ChangedIsEnabledDataTypeToBoolInMappingSMTPReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "isEnabled",
                table: "sys_MappingSMTPReceiver",
                type: "bit",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "isEnabled",
                table: "sys_MappingSMTPReceiver",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
