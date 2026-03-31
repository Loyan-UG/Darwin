using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChannelDispatchAuditBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelDispatchAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FlowKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MessagePreview = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttemptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelDispatchAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_AttemptedAtUtc",
                table: "ChannelDispatchAudits",
                column: "AttemptedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_BusinessId",
                table: "ChannelDispatchAudits",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_Channel",
                table: "ChannelDispatchAudits",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_FlowKey",
                table: "ChannelDispatchAudits",
                column: "FlowKey");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_RecipientAddress",
                table: "ChannelDispatchAudits",
                column: "RecipientAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_Status",
                table: "ChannelDispatchAudits",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelDispatchAudits");
        }
    }
}
