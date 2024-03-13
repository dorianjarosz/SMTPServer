using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMTPServer.Migrations
{
    /// <inheritdoc />
    public partial class AddedLastUpdateColumnToSMTPLogAndMappingSMTPReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "lastUpdate",
                table: "sys_SMTPLog",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "getdate()");

            migrationBuilder.AddColumn<DateTime>(
                name: "lastUpdate",
                table: "sys_MappingSMTPReceiver",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "getdate()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lastUpdate",
                table: "sys_SMTPLog");

            migrationBuilder.DropColumn(
                name: "lastUpdate",
                table: "sys_MappingSMTPReceiver");
        }
    }
}
