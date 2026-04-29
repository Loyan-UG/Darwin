using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DarwinDbContext))]
    [Migration("20260429114500_EnforceNotificationAuditIdempotency")]
    public partial class EnforceNotificationAuditIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_EmailDispatchAudits_ActiveCorrelation",
                schema: "Integration",
                table: "EmailDispatchAudits",
                column: "CorrelationKey",
                unique: true,
                filter: "[CorrelationKey] IS NOT NULL AND [IsDeleted] = 0 AND [Status] IN (N'Pending', N'Sent')");

            migrationBuilder.CreateIndex(
                name: "UX_ChannelDispatchAudits_ActiveChannelCorrelation",
                schema: "Integration",
                table: "ChannelDispatchAudits",
                columns: new[] { "Channel", "CorrelationKey" },
                unique: true,
                filter: "[CorrelationKey] IS NOT NULL AND [IsDeleted] = 0 AND [Status] IN (N'Pending', N'Sent')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_EmailDispatchAudits_ActiveCorrelation",
                schema: "Integration",
                table: "EmailDispatchAudits");

            migrationBuilder.DropIndex(
                name: "UX_ChannelDispatchAudits_ActiveChannelCorrelation",
                schema: "Integration",
                table: "ChannelDispatchAudits");
        }
    }
}
