using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BusinessOperationalLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAtUtc",
                schema: "Businesses",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "OperationalStatus",
                schema: "Businesses",
                table: "Businesses",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedAtUtc",
                schema: "Businesses",
                table: "Businesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuspensionReason",
                schema: "Businesses",
                table: "Businesses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_OperationalStatus",
                schema: "Businesses",
                table: "Businesses",
                column: "OperationalStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_OperationalStatus",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "ApprovedAtUtc",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "OperationalStatus",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SuspendedAtUtc",
                schema: "Businesses",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "SuspensionReason",
                schema: "Businesses",
                table: "Businesses");
        }
    }
}
