using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RelationshipPrecisionCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessFavorites_Businesses_BusinessId1",
                schema: "Businesses",
                table: "BusinessFavorites");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessInvitations_Businesses_BusinessId1",
                schema: "Businesses",
                table: "BusinessInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessLikes_Businesses_BusinessId1",
                schema: "Businesses",
                table: "BusinessLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessReviews_Businesses_BusinessId1",
                schema: "Businesses",
                table: "BusinessReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_PasswordResetTokens_Users_UserId1",
                schema: "Identity",
                table: "PasswordResetTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId1",
                schema: "Identity",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Roles_RoleId1",
                schema: "Identity",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDevices_Users_UserId1",
                schema: "Identity",
                table: "UserDevices");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId1",
                schema: "Identity",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId1",
                schema: "Identity",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_RoleId1",
                schema: "Identity",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId1",
                schema: "Identity",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserDevices_UserId1",
                schema: "Identity",
                table: "UserDevices");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_PermissionId1",
                schema: "Identity",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId1",
                schema: "Identity",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_UserId1",
                schema: "Identity",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_BusinessReviews_BusinessId1",
                schema: "Businesses",
                table: "BusinessReviews");

            migrationBuilder.DropIndex(
                name: "IX_BusinessLikes_BusinessId1",
                schema: "Businesses",
                table: "BusinessLikes");

            migrationBuilder.DropIndex(
                name: "IX_BusinessInvitations_BusinessId1",
                schema: "Businesses",
                table: "BusinessInvitations");

            migrationBuilder.DropIndex(
                name: "IX_BusinessFavorites_BusinessId1",
                schema: "Businesses",
                table: "BusinessFavorites");

            migrationBuilder.DropColumn(
                name: "RoleId1",
                schema: "Identity",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "UserId1",
                schema: "Identity",
                table: "UserRoles");

            migrationBuilder.DropColumn(
                name: "UserId1",
                schema: "Identity",
                table: "UserDevices");

            migrationBuilder.DropColumn(
                name: "PermissionId1",
                schema: "Identity",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "RoleId1",
                schema: "Identity",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "UserId1",
                schema: "Identity",
                table: "PasswordResetTokens");

            migrationBuilder.DropColumn(
                name: "BusinessId1",
                schema: "Businesses",
                table: "BusinessReviews");

            migrationBuilder.DropColumn(
                name: "BusinessId1",
                schema: "Businesses",
                table: "BusinessLikes");

            migrationBuilder.DropColumn(
                name: "BusinessId1",
                schema: "Businesses",
                table: "BusinessInvitations");

            migrationBuilder.DropColumn(
                name: "BusinessId1",
                schema: "Businesses",
                table: "BusinessFavorites");

            migrationBuilder.AlterColumn<decimal>(
                name: "VatRate",
                schema: "Orders",
                table: "OrderLines",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoleId1",
                schema: "Identity",
                table: "UserRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                schema: "Identity",
                table: "UserRoles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                schema: "Identity",
                table: "UserDevices",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PermissionId1",
                schema: "Identity",
                table: "RolePermissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId1",
                schema: "Identity",
                table: "RolePermissions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                schema: "Identity",
                table: "PasswordResetTokens",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "VatRate",
                schema: "Orders",
                table: "OrderLines",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,4)",
                oldPrecision: 9,
                oldScale: 4);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId1",
                schema: "Businesses",
                table: "BusinessReviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId1",
                schema: "Businesses",
                table: "BusinessLikes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId1",
                schema: "Businesses",
                table: "BusinessInvitations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessId1",
                schema: "Businesses",
                table: "BusinessFavorites",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId1",
                schema: "Identity",
                table: "UserRoles",
                column: "RoleId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId1",
                schema: "Identity",
                table: "UserRoles",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId1",
                schema: "Identity",
                table: "UserDevices",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId1",
                schema: "Identity",
                table: "RolePermissions",
                column: "PermissionId1");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId1",
                schema: "Identity",
                table: "RolePermissions",
                column: "RoleId1");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId1",
                schema: "Identity",
                table: "PasswordResetTokens",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReviews_BusinessId1",
                schema: "Businesses",
                table: "BusinessReviews",
                column: "BusinessId1");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLikes_BusinessId1",
                schema: "Businesses",
                table: "BusinessLikes",
                column: "BusinessId1");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessInvitations_BusinessId1",
                schema: "Businesses",
                table: "BusinessInvitations",
                column: "BusinessId1");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFavorites_BusinessId1",
                schema: "Businesses",
                table: "BusinessFavorites",
                column: "BusinessId1");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessFavorites_Businesses_BusinessId1",
                schema: "Businesses",
                table: "BusinessFavorites",
                column: "BusinessId1",
                principalSchema: "Businesses",
                principalTable: "Businesses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessInvitations_Businesses_BusinessId1",
                schema: "Businesses",
                table: "BusinessInvitations",
                column: "BusinessId1",
                principalSchema: "Businesses",
                principalTable: "Businesses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessLikes_Businesses_BusinessId1",
                schema: "Businesses",
                table: "BusinessLikes",
                column: "BusinessId1",
                principalSchema: "Businesses",
                principalTable: "Businesses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessReviews_Businesses_BusinessId1",
                schema: "Businesses",
                table: "BusinessReviews",
                column: "BusinessId1",
                principalSchema: "Businesses",
                principalTable: "Businesses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetTokens_Users_UserId1",
                schema: "Identity",
                table: "PasswordResetTokens",
                column: "UserId1",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId1",
                schema: "Identity",
                table: "RolePermissions",
                column: "PermissionId1",
                principalSchema: "Identity",
                principalTable: "Permissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Roles_RoleId1",
                schema: "Identity",
                table: "RolePermissions",
                column: "RoleId1",
                principalSchema: "Identity",
                principalTable: "Roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDevices_Users_UserId1",
                schema: "Identity",
                table: "UserDevices",
                column: "UserId1",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId1",
                schema: "Identity",
                table: "UserRoles",
                column: "RoleId1",
                principalSchema: "Identity",
                principalTable: "Roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId1",
                schema: "Identity",
                table: "UserRoles",
                column: "UserId1",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
