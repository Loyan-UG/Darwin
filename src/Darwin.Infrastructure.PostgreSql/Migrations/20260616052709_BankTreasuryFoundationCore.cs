using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class BankTreasuryFoundationCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankAccounts",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BankName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MaskedAccountIdentifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_BankAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankAccounts_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalSchema: "Billing",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankStatementImports",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LineCount = table.Column<int>(type: "integer", nullable: false),
                    DebitTotalMinor = table.Column<long>(type: "bigint", nullable: false),
                    CreditTotalMinor = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_BankStatementImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatementImports_BankAccounts_BankAccountId",
                        column: x => x.BankAccountId,
                        principalSchema: "Billing",
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankStatementLines",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankStatementImportId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Direction = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CounterpartyName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CounterpartyReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RemittanceInformation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NormalizedIdentityKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReviewStatus = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_BankStatementLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankStatementLines_BankStatementImports_BankStatementImport~",
                        column: x => x.BankStatementImportId,
                        principalSchema: "Billing",
                        principalTable: "BankStatementImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_BusinessId",
                schema: "Billing",
                table: "BankAccounts",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_FinancialAccountId",
                schema: "Billing",
                table: "BankAccounts",
                column: "FinancialAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_IsDefault",
                schema: "Billing",
                table: "BankAccounts",
                column: "IsDefault");

            migrationBuilder.CreateIndex(
                name: "IX_BankAccounts_Status",
                schema: "Billing",
                table: "BankAccounts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_BankAccounts_Business_Code_Active",
                schema: "Billing",
                table: "BankAccounts",
                columns: new[] { "BusinessId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementImports_BankAccountId",
                schema: "Billing",
                table: "BankStatementImports",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementImports_BusinessId",
                schema: "Billing",
                table: "BankStatementImports",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementImports_BusinessId_PeriodStartUtc_PeriodEndUtc",
                schema: "Billing",
                table: "BankStatementImports",
                columns: new[] { "BusinessId", "PeriodStartUtc", "PeriodEndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementImports_ImportedAtUtc",
                schema: "Billing",
                table: "BankStatementImports",
                column: "ImportedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementImports_Status",
                schema: "Billing",
                table: "BankStatementImports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_BankStatementImports_Account_Reference_Active",
                schema: "Billing",
                table: "BankStatementImports",
                columns: new[] { "BankAccountId", "StatementReference" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_BankAccountId",
                schema: "Billing",
                table: "BankStatementLines",
                column: "BankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_BankStatementImportId",
                schema: "Billing",
                table: "BankStatementLines",
                column: "BankStatementImportId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_BusinessId",
                schema: "Billing",
                table: "BankStatementLines",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_ReviewStatus",
                schema: "Billing",
                table: "BankStatementLines",
                column: "ReviewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BankStatementLines_TransactionDateUtc",
                schema: "Billing",
                table: "BankStatementLines",
                column: "TransactionDateUtc");

            migrationBuilder.CreateIndex(
                name: "UX_BankStatementLines_Account_Identity_Active",
                schema: "Billing",
                table: "BankStatementLines",
                columns: new[] { "BankAccountId", "NormalizedIdentityKey" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankStatementLines",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "BankStatementImports",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "BankAccounts",
                schema: "Billing");
        }
    }
}
