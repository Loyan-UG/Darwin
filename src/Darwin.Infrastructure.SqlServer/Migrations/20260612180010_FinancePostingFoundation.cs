using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class FinancePostingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostingStatus",
                schema: "Billing",
                table: "JournalEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<string>(
                name: "PostingKind",
                schema: "Billing",
                table: "JournalEntries",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<string>(
                name: "PostingKey",
                schema: "Billing",
                table: "JournalEntries",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceEntityType",
                schema: "Billing",
                table: "JournalEntries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceEntityId",
                schema: "Billing",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceDocumentNumber",
                schema: "Billing",
                table: "JournalEntries",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostedAtUtc",
                schema: "Billing",
                table: "JournalEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReversedAtUtc",
                schema: "Billing",
                table: "JournalEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReversalOfJournalEntryId",
                schema: "Billing",
                table: "JournalEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostingReason",
                schema: "Billing",
                table: "JournalEntries",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                schema: "Billing",
                table: "JournalEntries",
                type: "nvarchar(max)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_PostingStatus",
                schema: "Billing",
                table: "JournalEntries",
                column: "PostingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_PostingKind",
                schema: "Billing",
                table: "JournalEntries",
                column: "PostingKind");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_SourceEntity",
                schema: "Billing",
                table: "JournalEntries",
                columns: new[] { "SourceEntityType", "SourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "UX_JournalEntries_PostingKey",
                schema: "Billing",
                table: "JournalEntries",
                column: "PostingKey",
                unique: true,
                filter: "[PostingKey] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_JournalEntries_PostingStatus", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropIndex(name: "IX_JournalEntries_PostingKind", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropIndex(name: "IX_JournalEntries_SourceEntity", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropIndex(name: "UX_JournalEntries_PostingKey", schema: "Billing", table: "JournalEntries");

            migrationBuilder.DropColumn(name: "PostingStatus", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropColumn(name: "PostingKind", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropColumn(name: "PostingKey", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropColumn(name: "SourceEntityType", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropColumn(name: "SourceEntityId", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropColumn(name: "SourceDocumentNumber", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropColumn(name: "PostedAtUtc", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropColumn(name: "ReversedAtUtc", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropColumn(name: "ReversalOfJournalEntryId", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropColumn(name: "PostingReason", schema: "Billing", table: "JournalEntries");
            migrationBuilder.DropColumn(name: "MetadataJson", schema: "Billing", table: "JournalEntries");
        }
    }
}
