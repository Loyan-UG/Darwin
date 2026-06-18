using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignPushDispatchSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PushToken",
                schema: "Identity",
                table: "UserDevices",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignDeliveries_Channel_Status_Created",
                schema: "Marketing",
                table: "CampaignDeliveries",
                columns: new[] { "Channel", "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CampaignDeliveries_Channel_Status_Created",
                schema: "Marketing",
                table: "CampaignDeliveries");

            migrationBuilder.AlterColumn<string>(
                name: "PushToken",
                schema: "Identity",
                table: "UserDevices",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true);
        }
    }
}
