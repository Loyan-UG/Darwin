using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class CrmCoreModelExpansion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloseReason",
                schema: "CRM",
                table: "Opportunities",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAtUtc",
                schema: "CRM",
                table: "Opportunities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "CRM",
                table: "Opportunities",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<int>(
                name: "ForecastCategory",
                schema: "CRM",
                table: "Opportunities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProbabilityPercent",
                schema: "CRM",
                table: "Opportunities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "CRM",
                table: "Opportunities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedReason",
                schema: "CRM",
                table: "Leads",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConvertedAtUtc",
                schema: "CRM",
                table: "Leads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DisqualifiedAtUtc",
                schema: "CRM",
                table: "Leads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                schema: "CRM",
                table: "Leads",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "QualifiedAtUtc",
                schema: "CRM",
                table: "Leads",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "CRM",
                table: "CustomerSegments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "CRM",
                table: "CustomerSegments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "RuleJson",
                schema: "CRM",
                table: "CustomerSegments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AcquisitionSource",
                schema: "CRM",
                table: "Customers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastContactedAtUtc",
                schema: "CRM",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LifecycleStatus",
                schema: "CRM",
                table: "Customers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextFollowUpAtUtc",
                schema: "CRM",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                schema: "CRM",
                table: "Customers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreferredContactChannel",
                schema: "CRM",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceJson",
                schema: "CRM",
                table: "Consents",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PolicyVersion",
                schema: "CRM",
                table: "Consents",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "CRM",
                table: "Consents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [CRM].[CustomerSegments]
                SET [Code] = CONCAT(N'segment-', CONVERT(nvarchar(36), [Id]))
                WHERE [Code] IS NULL OR LTRIM(RTRIM([Code])) = N'';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                schema: "CRM",
                table: "CustomerSegments",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_ClosedAtUtc",
                schema: "CRM",
                table: "Opportunities",
                column: "ClosedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_ExpectedCloseDateUtc",
                schema: "CRM",
                table: "Opportunities",
                column: "ExpectedCloseDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_ForecastCategory",
                schema: "CRM",
                table: "Opportunities",
                column: "ForecastCategory");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_ConvertedAtUtc",
                schema: "CRM",
                table: "Leads",
                column: "ConvertedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Priority",
                schema: "CRM",
                table: "Leads",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_QualifiedAtUtc",
                schema: "CRM",
                table: "Leads",
                column: "QualifiedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegments_Code",
                schema: "CRM",
                table: "CustomerSegments",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegments_IsActive",
                schema: "CRM",
                table: "CustomerSegments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_LifecycleStatus",
                schema: "CRM",
                table: "Customers",
                column: "LifecycleStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_NextFollowUpAtUtc",
                schema: "CRM",
                table: "Customers",
                column: "NextFollowUpAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_OwnerUserId",
                schema: "CRM",
                table: "Customers",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Consents_PolicyVersion",
                schema: "CRM",
                table: "Consents",
                column: "PolicyVersion");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Users_OwnerUserId",
                schema: "CRM",
                table: "Customers",
                column: "OwnerUserId",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Users_OwnerUserId",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_ClosedAtUtc",
                schema: "CRM",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_ExpectedCloseDateUtc",
                schema: "CRM",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_ForecastCategory",
                schema: "CRM",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Leads_ConvertedAtUtc",
                schema: "CRM",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_Priority",
                schema: "CRM",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_Leads_QualifiedAtUtc",
                schema: "CRM",
                table: "Leads");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSegments_Code",
                schema: "CRM",
                table: "CustomerSegments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSegments_IsActive",
                schema: "CRM",
                table: "CustomerSegments");

            migrationBuilder.DropIndex(
                name: "IX_Customers_LifecycleStatus",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_NextFollowUpAtUtc",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_OwnerUserId",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Consents_PolicyVersion",
                schema: "CRM",
                table: "Consents");

            migrationBuilder.DropColumn(
                name: "CloseReason",
                schema: "CRM",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "ClosedAtUtc",
                schema: "CRM",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "CRM",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "ForecastCategory",
                schema: "CRM",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "ProbabilityPercent",
                schema: "CRM",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "CRM",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "ClosedReason",
                schema: "CRM",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "ConvertedAtUtc",
                schema: "CRM",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "DisqualifiedAtUtc",
                schema: "CRM",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "CRM",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "QualifiedAtUtc",
                schema: "CRM",
                table: "Leads");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "CRM",
                table: "CustomerSegments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "CRM",
                table: "CustomerSegments");

            migrationBuilder.DropColumn(
                name: "RuleJson",
                schema: "CRM",
                table: "CustomerSegments");

            migrationBuilder.DropColumn(
                name: "AcquisitionSource",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastContactedAtUtc",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LifecycleStatus",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "NextFollowUpAtUtc",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "PreferredContactChannel",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "EvidenceJson",
                schema: "CRM",
                table: "Consents");

            migrationBuilder.DropColumn(
                name: "PolicyVersion",
                schema: "CRM",
                table: "Consents");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "CRM",
                table: "Consents");
        }
    }
}
