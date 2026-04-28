using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteAwareUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserWebAuthnCredentials_UserId_CredentialId",
                schema: "Identity",
                table: "UserWebAuthnCredentials");

            migrationBuilder.DropIndex(
                name: "IX_UserTokens_UserId_Purpose",
                schema: "Identity",
                table: "UserTokens");

            migrationBuilder.DropIndex(
                name: "UX_User_NormalizedUserName",
                schema: "Identity",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UX_UserRole_User_Role",
                schema: "Identity",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserLogins_Provider_ProviderKey",
                schema: "Identity",
                table: "UserLogins");

            migrationBuilder.DropIndex(
                name: "IX_UserLogins_UserId_Provider",
                schema: "Identity",
                table: "UserLogins");

            migrationBuilder.DropIndex(
                name: "UX_UserEngagementSnapshots_UserId",
                schema: "Identity",
                table: "UserEngagementSnapshots");

            migrationBuilder.DropIndex(
                name: "UX_UserDevices_User_DeviceId",
                schema: "Identity",
                table: "UserDevices");

            migrationBuilder.DropIndex(
                name: "UX_Role_Name",
                schema: "Identity",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UX_Role_NormalizedName",
                schema: "Identity",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UX_RolePermission_Role_Permission",
                schema: "Identity",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "UX_Permission_Key",
                schema: "Identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "UX_BusinessReviews_User_Business",
                schema: "Businesses",
                table: "BusinessReviews");

            migrationBuilder.DropIndex(
                name: "IX_BusinessMembers_BusinessId_UserId",
                schema: "Businesses",
                table: "BusinessMembers");

            migrationBuilder.DropIndex(
                name: "UX_BusinessLikes_User_Business",
                schema: "Businesses",
                table: "BusinessLikes");

            migrationBuilder.DropIndex(
                name: "UX_BusinessFavorites_User_Business",
                schema: "Businesses",
                table: "BusinessFavorites");

            migrationBuilder.DropIndex(
                name: "IX_BrandTranslations_BrandId_Culture",
                schema: "Catalog",
                table: "BrandTranslations");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Slug",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_AddOnOptionValueTranslations_AddOnOptionValueId_Culture",
                schema: "Catalog",
                table: "AddOnOptionValueTranslations");

            migrationBuilder.DropIndex(
                name: "IX_AddOnOptionTranslations_AddOnOptionId_Culture",
                schema: "Catalog",
                table: "AddOnOptionTranslations");

            migrationBuilder.DropIndex(
                name: "IX_AddOnGroupVariants_AddOnGroupId_VariantId",
                schema: "Catalog",
                table: "AddOnGroupVariants");

            migrationBuilder.DropIndex(
                name: "IX_AddOnGroupTranslations_AddOnGroupId_Culture",
                schema: "Catalog",
                table: "AddOnGroupTranslations");

            migrationBuilder.DropIndex(
                name: "IX_AddOnGroupProducts_AddOnGroupId_ProductId",
                schema: "Catalog",
                table: "AddOnGroupProducts");

            migrationBuilder.DropIndex(
                name: "IX_AddOnGroupCategories_AddOnGroupId_CategoryId",
                schema: "Catalog",
                table: "AddOnGroupCategories");

            migrationBuilder.DropIndex(
                name: "IX_AddOnGroupBrands_AddOnGroupId_BrandId",
                schema: "Catalog",
                table: "AddOnGroupBrands");

            migrationBuilder.CreateIndex(
                name: "IX_UserWebAuthnCredentials_UserId_CredentialId",
                schema: "Identity",
                table: "UserWebAuthnCredentials",
                columns: new[] { "UserId", "CredentialId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_UserId_Purpose",
                schema: "Identity",
                table: "UserTokens",
                columns: new[] { "UserId", "Purpose" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_User_NormalizedUserName",
                schema: "Identity",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_UserRole_User_Role",
                schema: "Identity",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_Provider_ProviderKey",
                schema: "Identity",
                table: "UserLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId_Provider",
                schema: "Identity",
                table: "UserLogins",
                columns: new[] { "UserId", "Provider" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_UserEngagementSnapshots_UserId",
                schema: "Identity",
                table: "UserEngagementSnapshots",
                column: "UserId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_UserDevices_User_DeviceId",
                schema: "Identity",
                table: "UserDevices",
                columns: new[] { "UserId", "DeviceId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Role_Name",
                schema: "Identity",
                table: "Roles",
                column: "Key",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Role_NormalizedName",
                schema: "Identity",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_RolePermission_Role_Permission",
                schema: "Identity",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Permission_Key",
                schema: "Identity",
                table: "Permissions",
                column: "Key",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_BusinessReviews_User_Business",
                schema: "Businesses",
                table: "BusinessReviews",
                columns: new[] { "UserId", "BusinessId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMembers_BusinessId_UserId",
                schema: "Businesses",
                table: "BusinessMembers",
                columns: new[] { "BusinessId", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_BusinessLikes_User_Business",
                schema: "Businesses",
                table: "BusinessLikes",
                columns: new[] { "UserId", "BusinessId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_BusinessFavorites_User_Business",
                schema: "Businesses",
                table: "BusinessFavorites",
                columns: new[] { "UserId", "BusinessId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BrandTranslations_BrandId_Culture",
                schema: "Catalog",
                table: "BrandTranslations",
                columns: new[] { "BrandId", "Culture" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                schema: "Catalog",
                table: "Brands",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptionValueTranslations_AddOnOptionValueId_Culture",
                schema: "Catalog",
                table: "AddOnOptionValueTranslations",
                columns: new[] { "AddOnOptionValueId", "Culture" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptionTranslations_AddOnOptionId_Culture",
                schema: "Catalog",
                table: "AddOnOptionTranslations",
                columns: new[] { "AddOnOptionId", "Culture" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupVariants_AddOnGroupId_VariantId",
                schema: "Catalog",
                table: "AddOnGroupVariants",
                columns: new[] { "AddOnGroupId", "VariantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupTranslations_AddOnGroupId_Culture",
                schema: "Catalog",
                table: "AddOnGroupTranslations",
                columns: new[] { "AddOnGroupId", "Culture" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupProducts_AddOnGroupId_ProductId",
                schema: "Catalog",
                table: "AddOnGroupProducts",
                columns: new[] { "AddOnGroupId", "ProductId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupCategories_AddOnGroupId_CategoryId",
                schema: "Catalog",
                table: "AddOnGroupCategories",
                columns: new[] { "AddOnGroupId", "CategoryId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupBrands_AddOnGroupId_BrandId",
                schema: "Catalog",
                table: "AddOnGroupBrands",
                columns: new[] { "AddOnGroupId", "BrandId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserWebAuthnCredentials_UserId_CredentialId",
                schema: "Identity",
                table: "UserWebAuthnCredentials");

            migrationBuilder.DropIndex(
                name: "IX_UserTokens_UserId_Purpose",
                schema: "Identity",
                table: "UserTokens");

            migrationBuilder.DropIndex(
                name: "UX_User_NormalizedUserName",
                schema: "Identity",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "UX_UserRole_User_Role",
                schema: "Identity",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserLogins_Provider_ProviderKey",
                schema: "Identity",
                table: "UserLogins");

            migrationBuilder.DropIndex(
                name: "IX_UserLogins_UserId_Provider",
                schema: "Identity",
                table: "UserLogins");

            migrationBuilder.DropIndex(
                name: "UX_UserEngagementSnapshots_UserId",
                schema: "Identity",
                table: "UserEngagementSnapshots");

            migrationBuilder.DropIndex(
                name: "UX_UserDevices_User_DeviceId",
                schema: "Identity",
                table: "UserDevices");

            migrationBuilder.DropIndex(
                name: "UX_Role_Name",
                schema: "Identity",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UX_Role_NormalizedName",
                schema: "Identity",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UX_RolePermission_Role_Permission",
                schema: "Identity",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "UX_Permission_Key",
                schema: "Identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "UX_BusinessReviews_User_Business",
                schema: "Businesses",
                table: "BusinessReviews");

            migrationBuilder.DropIndex(
                name: "IX_BusinessMembers_BusinessId_UserId",
                schema: "Businesses",
                table: "BusinessMembers");

            migrationBuilder.DropIndex(
                name: "UX_BusinessLikes_User_Business",
                schema: "Businesses",
                table: "BusinessLikes");

            migrationBuilder.DropIndex(
                name: "UX_BusinessFavorites_User_Business",
                schema: "Businesses",
                table: "BusinessFavorites");

            migrationBuilder.DropIndex(
                name: "IX_BrandTranslations_BrandId_Culture",
                schema: "Catalog",
                table: "BrandTranslations");

            migrationBuilder.DropIndex(
                name: "IX_Brands_Slug",
                schema: "Catalog",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_AddOnOptionValueTranslations_AddOnOptionValueId_Culture",
                schema: "Catalog",
                table: "AddOnOptionValueTranslations");

            migrationBuilder.DropIndex(
                name: "IX_AddOnOptionTranslations_AddOnOptionId_Culture",
                schema: "Catalog",
                table: "AddOnOptionTranslations");

            migrationBuilder.DropIndex(
                name: "IX_AddOnGroupVariants_AddOnGroupId_VariantId",
                schema: "Catalog",
                table: "AddOnGroupVariants");

            migrationBuilder.DropIndex(
                name: "IX_AddOnGroupTranslations_AddOnGroupId_Culture",
                schema: "Catalog",
                table: "AddOnGroupTranslations");

            migrationBuilder.DropIndex(
                name: "IX_AddOnGroupProducts_AddOnGroupId_ProductId",
                schema: "Catalog",
                table: "AddOnGroupProducts");

            migrationBuilder.DropIndex(
                name: "IX_AddOnGroupCategories_AddOnGroupId_CategoryId",
                schema: "Catalog",
                table: "AddOnGroupCategories");

            migrationBuilder.DropIndex(
                name: "IX_AddOnGroupBrands_AddOnGroupId_BrandId",
                schema: "Catalog",
                table: "AddOnGroupBrands");

            migrationBuilder.CreateIndex(
                name: "IX_UserWebAuthnCredentials_UserId_CredentialId",
                schema: "Identity",
                table: "UserWebAuthnCredentials",
                columns: new[] { "UserId", "CredentialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_UserId_Purpose",
                schema: "Identity",
                table: "UserTokens",
                columns: new[] { "UserId", "Purpose" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_User_NormalizedUserName",
                schema: "Identity",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UserRole_User_Role",
                schema: "Identity",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_Provider_ProviderKey",
                schema: "Identity",
                table: "UserLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId_Provider",
                schema: "Identity",
                table: "UserLogins",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UserEngagementSnapshots_UserId",
                schema: "Identity",
                table: "UserEngagementSnapshots",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UserDevices_User_DeviceId",
                schema: "Identity",
                table: "UserDevices",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Role_Name",
                schema: "Identity",
                table: "Roles",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Role_NormalizedName",
                schema: "Identity",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_RolePermission_Role_Permission",
                schema: "Identity",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Permission_Key",
                schema: "Identity",
                table: "Permissions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BusinessReviews_User_Business",
                schema: "Businesses",
                table: "BusinessReviews",
                columns: new[] { "UserId", "BusinessId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMembers_BusinessId_UserId",
                schema: "Businesses",
                table: "BusinessMembers",
                columns: new[] { "BusinessId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BusinessLikes_User_Business",
                schema: "Businesses",
                table: "BusinessLikes",
                columns: new[] { "UserId", "BusinessId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BusinessFavorites_User_Business",
                schema: "Businesses",
                table: "BusinessFavorites",
                columns: new[] { "UserId", "BusinessId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrandTranslations_BrandId_Culture",
                schema: "Catalog",
                table: "BrandTranslations",
                columns: new[] { "BrandId", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                schema: "Catalog",
                table: "Brands",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptionValueTranslations_AddOnOptionValueId_Culture",
                schema: "Catalog",
                table: "AddOnOptionValueTranslations",
                columns: new[] { "AddOnOptionValueId", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptionTranslations_AddOnOptionId_Culture",
                schema: "Catalog",
                table: "AddOnOptionTranslations",
                columns: new[] { "AddOnOptionId", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupVariants_AddOnGroupId_VariantId",
                schema: "Catalog",
                table: "AddOnGroupVariants",
                columns: new[] { "AddOnGroupId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupTranslations_AddOnGroupId_Culture",
                schema: "Catalog",
                table: "AddOnGroupTranslations",
                columns: new[] { "AddOnGroupId", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupProducts_AddOnGroupId_ProductId",
                schema: "Catalog",
                table: "AddOnGroupProducts",
                columns: new[] { "AddOnGroupId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupCategories_AddOnGroupId_CategoryId",
                schema: "Catalog",
                table: "AddOnGroupCategories",
                columns: new[] { "AddOnGroupId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupBrands_AddOnGroupId_BrandId",
                schema: "Catalog",
                table: "AddOnGroupBrands",
                columns: new[] { "AddOnGroupId", "BrandId" },
                unique: true);
        }
    }
}
