using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChannelDispatchOperationsQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChannelDispatchOperations",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecipientAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IntendedRecipientAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MessageText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FlowKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TemplateKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CorrelationKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelDispatchOperations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchOperations_Channel_Status_CreatedAtUtc",
                schema: "Integration",
                table: "ChannelDispatchOperations",
                columns: new[] { "Channel", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchOperations_CorrelationKey",
                schema: "Integration",
                table: "ChannelDispatchOperations",
                column: "CorrelationKey");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchOperations_IntendedRecipientAddress",
                schema: "Integration",
                table: "ChannelDispatchOperations",
                column: "IntendedRecipientAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChannelDispatchOperations",
                schema: "Integration");
        }
    }
}
