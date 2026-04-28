using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class BusinessOwnerOverrideAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessOwnerOverrideAudits",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AffectedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionKind = table.Column<short>(type: "smallint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessOwnerOverrideAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOwnerOverrideAudits_AffectedUserId",
                schema: "Businesses",
                table: "BusinessOwnerOverrideAudits",
                column: "AffectedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOwnerOverrideAudits_BusinessId",
                schema: "Businesses",
                table: "BusinessOwnerOverrideAudits",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOwnerOverrideAudits_BusinessMemberId",
                schema: "Businesses",
                table: "BusinessOwnerOverrideAudits",
                column: "BusinessMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessOwnerOverrideAudits_CreatedAtUtc",
                schema: "Businesses",
                table: "BusinessOwnerOverrideAudits",
                column: "CreatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessOwnerOverrideAudits",
                schema: "Businesses");
        }
    }
}
