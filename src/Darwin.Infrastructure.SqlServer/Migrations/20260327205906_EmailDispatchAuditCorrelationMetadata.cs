using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class EmailDispatchAuditCorrelationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId",
                table: "EmailDispatchAudits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlowKey",
                table: "EmailDispatchAudits",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_BusinessId",
                table: "EmailDispatchAudits",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_FlowKey",
                table: "EmailDispatchAudits",
                column: "FlowKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailDispatchAudits_BusinessId",
                table: "EmailDispatchAudits");

            migrationBuilder.DropIndex(
                name: "IX_EmailDispatchAudits_FlowKey",
                table: "EmailDispatchAudits");

            migrationBuilder.DropColumn(
                name: "BusinessId",
                table: "EmailDispatchAudits");

            migrationBuilder.DropColumn(
                name: "FlowKey",
                table: "EmailDispatchAudits");
        }
    }
}
