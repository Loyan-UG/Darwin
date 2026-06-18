using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class BankReconciliationCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankReconciliationMatches",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MatchDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    BankTotalMinor = table.Column<long>(type: "bigint", nullable: false),
                    FinanceTotalMinor = table.Column<long>(type: "bigint", nullable: false),
                    DifferenceMinor = table.Column<long>(type: "bigint", nullable: false),
                    MatchedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankReconciliationMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankReconciliationMatches_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalSchema: "Billing",
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankReconciliationMatchLines",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BankReconciliationMatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankStatementLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SourceEntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Direction = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Memo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankReconciliationMatchLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankReconciliationMatchLines_BankReconciliationMatches_Bank~",
                        column: x => x.BankReconciliationMatchId,
                        principalSchema: "Billing",
                        principalTable: "BankReconciliationMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BankReconciliationMatchLines_BankStatementLines_BankStateme~",
                        column: x => x.BankStatementLineId,
                        principalSchema: "Billing",
                        principalTable: "BankStatementLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BankReconciliationMatchLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "Billing",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_BankAccountId",
                schema: "Billing",
                table: "BankReconciliationMatches",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_BusinessId",
                schema: "Billing",
                table: "BankReconciliationMatches",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_MatchDateUtc",
                schema: "Billing",
                table: "BankReconciliationMatches",
                column: "MatchDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_MatchedAtUtc",
                schema: "Billing",
                table: "BankReconciliationMatches",
                column: "MatchedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatches_Status",
                schema: "Billing",
                table: "BankReconciliationMatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_BankReconciliationMatches_Business_Number_Active",
                schema: "Billing",
                table: "BankReconciliationMatches",
                columns: new[] { "BusinessId", "MatchNumber" },
                unique: true,
                filter: "\"MatchNumber\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatchLines_BankReconciliationMatchId",
                schema: "Billing",
                table: "BankReconciliationMatchLines",
                column: "BankReconciliationMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatchLines_JournalEntryId",
                schema: "Billing",
                table: "BankReconciliationMatchLines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_BankReconciliationMatchLines_SourceEntity",
                schema: "Billing",
                table: "BankReconciliationMatchLines",
                columns: new[] { "SourceEntityType", "SourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "UX_BankReconciliationMatchLines_StatementLine_Active",
                schema: "Billing",
                table: "BankReconciliationMatchLines",
                column: "BankStatementLineId",
                unique: true,
                filter: "\"IsActive\" = TRUE AND \"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankReconciliationMatchLines",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "BankReconciliationMatches",
                schema: "Billing");
        }
    }
}
