using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendSoftDeleteAwareUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warehouses_BusinessId_Name",
                schema: "Inventory",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionInvoices_Provider_ProviderInvoiceId",
                schema: "Billing",
                table: "SubscriptionInvoices");

            migrationBuilder.DropIndex(
                name: "IX_StockLevels_WarehouseId_ProductVariantId",
                schema: "Inventory",
                table: "StockLevels");

            migrationBuilder.DropIndex(
                name: "IX_ShippingRates_ShippingMethodId_SortOrder",
                table: "ShippingRates");

            migrationBuilder.DropIndex(
                name: "IX_ShippingMethods_Name_Carrier_Service",
                schema: "Shipping",
                table: "ShippingMethods");

            migrationBuilder.DropIndex(
                name: "UX_QrCodeTokens_Token",
                schema: "Loyalty",
                table: "QrCodeTokens");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_BusinessId_OrderNumber",
                schema: "Inventory",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PromotionRedemptions_OrderId",
                schema: "Pricing",
                table: "PromotionRedemptions");

            migrationBuilder.DropIndex(
                name: "UX_ResetToken_User_Token",
                schema: "Identity",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_PageTranslations_PageId_Culture_Slug",
                schema: "CMS",
                table: "PageTranslations");

            migrationBuilder.DropIndex(
                name: "IX_Menus_Name",
                schema: "CMS",
                table: "Menus");

            migrationBuilder.DropIndex(
                name: "IX_MenuItemTranslations_MenuItemId_Culture",
                schema: "CMS",
                table: "MenuItemTranslations");

            migrationBuilder.DropIndex(
                name: "UX_LoyaltyRewardTiers_Program_Points",
                schema: "Loyalty",
                table: "LoyaltyRewardTiers");

            migrationBuilder.DropIndex(
                name: "UX_LoyaltyPrograms_Business",
                schema: "Loyalty",
                table: "LoyaltyPrograms");

            migrationBuilder.DropIndex(
                name: "UX_LoyaltyAccounts_Business_User",
                schema: "Loyalty",
                table: "LoyaltyAccounts");

            migrationBuilder.DropIndex(
                name: "IX_FinancialAccounts_BusinessId_Code",
                schema: "Billing",
                table: "FinancialAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSegments_Name",
                schema: "CRM",
                table: "CustomerSegments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSegmentMemberships_CustomerId_CustomerSegmentId",
                schema: "CRM",
                table: "CustomerSegmentMemberships");

            migrationBuilder.DropIndex(
                name: "IX_Customers_UserId",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_VariantId",
                schema: "CartCheckout",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "UX_CampaignDeliveries_IdempotencyKey",
                schema: "Marketing",
                table: "CampaignDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_BusinessSubscriptions_BusinessId",
                schema: "Billing",
                table: "BusinessSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_BusinessSubscriptions_Provider_ProviderSubscriptionId",
                schema: "Billing",
                table: "BusinessSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_BusinessStaffQrCodes_Token",
                schema: "Businesses",
                table: "BusinessStaffQrCodes");

            migrationBuilder.DropIndex(
                name: "UX_AnalyticsExportJobs_Business_IdempotencyKey",
                schema: "Integration",
                table: "AnalyticsExportJobs");

            migrationBuilder.DropIndex(
                name: "UX_AnalyticsExportFiles_Job_StorageKey",
                schema: "Integration",
                table: "AnalyticsExportFiles");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BusinessId_Name",
                schema: "Inventory",
                table: "Warehouses",
                columns: new[] { "BusinessId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionInvoices_Provider_ProviderInvoiceId",
                schema: "Billing",
                table: "SubscriptionInvoices",
                columns: new[] { "Provider", "ProviderInvoiceId" },
                unique: true,
                filter: "[ProviderInvoiceId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_WarehouseId_ProductVariantId",
                schema: "Inventory",
                table: "StockLevels",
                columns: new[] { "WarehouseId", "ProductVariantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRates_ShippingMethodId_SortOrder",
                table: "ShippingRates",
                columns: new[] { "ShippingMethodId", "SortOrder" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingMethods_Name_Carrier_Service",
                schema: "Shipping",
                table: "ShippingMethods",
                columns: new[] { "Name", "Carrier", "Service" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_QrCodeTokens_Token",
                schema: "Loyalty",
                table: "QrCodeTokens",
                column: "Token",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BusinessId_OrderNumber",
                schema: "Inventory",
                table: "PurchaseOrders",
                columns: new[] { "BusinessId", "OrderNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_OrderId",
                schema: "Pricing",
                table: "PromotionRedemptions",
                column: "OrderId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ResetToken_User_Token",
                schema: "Identity",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "Token" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PageTranslations_PageId_Culture_Slug",
                schema: "CMS",
                table: "PageTranslations",
                columns: new[] { "PageId", "Culture", "Slug" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Name",
                schema: "CMS",
                table: "Menus",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemTranslations_MenuItemId_Culture",
                schema: "CMS",
                table: "MenuItemTranslations",
                columns: new[] { "MenuItemId", "Culture" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyRewardTiers_Program_Points",
                schema: "Loyalty",
                table: "LoyaltyRewardTiers",
                columns: new[] { "LoyaltyProgramId", "PointsRequired" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyPrograms_Business",
                schema: "Loyalty",
                table: "LoyaltyPrograms",
                column: "BusinessId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyAccounts_Business_User",
                schema: "Loyalty",
                table: "LoyaltyAccounts",
                columns: new[] { "BusinessId", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_BusinessId_Code",
                schema: "Billing",
                table: "FinancialAccounts",
                columns: new[] { "BusinessId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegments_Name",
                schema: "CRM",
                table: "CustomerSegments",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegmentMemberships_CustomerId_CustomerSegmentId",
                schema: "CRM",
                table: "CustomerSegmentMemberships",
                columns: new[] { "CustomerId", "CustomerSegmentId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserId",
                schema: "CRM",
                table: "Customers",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_VariantId",
                schema: "CartCheckout",
                table: "CartItems",
                columns: new[] { "CartId", "VariantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_CampaignDeliveries_IdempotencyKey",
                schema: "Marketing",
                table: "CampaignDeliveries",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_BusinessId",
                schema: "Billing",
                table: "BusinessSubscriptions",
                column: "BusinessId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_Provider_ProviderSubscriptionId",
                schema: "Billing",
                table: "BusinessSubscriptions",
                columns: new[] { "Provider", "ProviderSubscriptionId" },
                unique: true,
                filter: "[ProviderSubscriptionId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaffQrCodes_Token",
                schema: "Businesses",
                table: "BusinessStaffQrCodes",
                column: "Token",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_AnalyticsExportJobs_Business_IdempotencyKey",
                schema: "Integration",
                table: "AnalyticsExportJobs",
                columns: new[] { "BusinessId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_AnalyticsExportFiles_Job_StorageKey",
                schema: "Integration",
                table: "AnalyticsExportFiles",
                columns: new[] { "AnalyticsExportJobId", "StorageKey" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Warehouses_BusinessId_Name",
                schema: "Inventory",
                table: "Warehouses");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionInvoices_Provider_ProviderInvoiceId",
                schema: "Billing",
                table: "SubscriptionInvoices");

            migrationBuilder.DropIndex(
                name: "IX_StockLevels_WarehouseId_ProductVariantId",
                schema: "Inventory",
                table: "StockLevels");

            migrationBuilder.DropIndex(
                name: "IX_ShippingRates_ShippingMethodId_SortOrder",
                table: "ShippingRates");

            migrationBuilder.DropIndex(
                name: "IX_ShippingMethods_Name_Carrier_Service",
                schema: "Shipping",
                table: "ShippingMethods");

            migrationBuilder.DropIndex(
                name: "UX_QrCodeTokens_Token",
                schema: "Loyalty",
                table: "QrCodeTokens");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_BusinessId_OrderNumber",
                schema: "Inventory",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PromotionRedemptions_OrderId",
                schema: "Pricing",
                table: "PromotionRedemptions");

            migrationBuilder.DropIndex(
                name: "UX_ResetToken_User_Token",
                schema: "Identity",
                table: "PasswordResetTokens");

            migrationBuilder.DropIndex(
                name: "IX_PageTranslations_PageId_Culture_Slug",
                schema: "CMS",
                table: "PageTranslations");

            migrationBuilder.DropIndex(
                name: "IX_Menus_Name",
                schema: "CMS",
                table: "Menus");

            migrationBuilder.DropIndex(
                name: "IX_MenuItemTranslations_MenuItemId_Culture",
                schema: "CMS",
                table: "MenuItemTranslations");

            migrationBuilder.DropIndex(
                name: "UX_LoyaltyRewardTiers_Program_Points",
                schema: "Loyalty",
                table: "LoyaltyRewardTiers");

            migrationBuilder.DropIndex(
                name: "UX_LoyaltyPrograms_Business",
                schema: "Loyalty",
                table: "LoyaltyPrograms");

            migrationBuilder.DropIndex(
                name: "UX_LoyaltyAccounts_Business_User",
                schema: "Loyalty",
                table: "LoyaltyAccounts");

            migrationBuilder.DropIndex(
                name: "IX_FinancialAccounts_BusinessId_Code",
                schema: "Billing",
                table: "FinancialAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSegments_Name",
                schema: "CRM",
                table: "CustomerSegments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerSegmentMemberships_CustomerId_CustomerSegmentId",
                schema: "CRM",
                table: "CustomerSegmentMemberships");

            migrationBuilder.DropIndex(
                name: "IX_Customers_UserId",
                schema: "CRM",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_VariantId",
                schema: "CartCheckout",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "UX_CampaignDeliveries_IdempotencyKey",
                schema: "Marketing",
                table: "CampaignDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_BusinessSubscriptions_BusinessId",
                schema: "Billing",
                table: "BusinessSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_BusinessSubscriptions_Provider_ProviderSubscriptionId",
                schema: "Billing",
                table: "BusinessSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_BusinessStaffQrCodes_Token",
                schema: "Businesses",
                table: "BusinessStaffQrCodes");

            migrationBuilder.DropIndex(
                name: "UX_AnalyticsExportJobs_Business_IdempotencyKey",
                schema: "Integration",
                table: "AnalyticsExportJobs");

            migrationBuilder.DropIndex(
                name: "UX_AnalyticsExportFiles_Job_StorageKey",
                schema: "Integration",
                table: "AnalyticsExportFiles");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BusinessId_Name",
                schema: "Inventory",
                table: "Warehouses",
                columns: new[] { "BusinessId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionInvoices_Provider_ProviderInvoiceId",
                schema: "Billing",
                table: "SubscriptionInvoices",
                columns: new[] { "Provider", "ProviderInvoiceId" },
                unique: true,
                filter: "[ProviderInvoiceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_WarehouseId_ProductVariantId",
                schema: "Inventory",
                table: "StockLevels",
                columns: new[] { "WarehouseId", "ProductVariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRates_ShippingMethodId_SortOrder",
                table: "ShippingRates",
                columns: new[] { "ShippingMethodId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingMethods_Name_Carrier_Service",
                schema: "Shipping",
                table: "ShippingMethods",
                columns: new[] { "Name", "Carrier", "Service" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_QrCodeTokens_Token",
                schema: "Loyalty",
                table: "QrCodeTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BusinessId_OrderNumber",
                schema: "Inventory",
                table: "PurchaseOrders",
                columns: new[] { "BusinessId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_OrderId",
                schema: "Pricing",
                table: "PromotionRedemptions",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ResetToken_User_Token",
                schema: "Identity",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "Token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageTranslations_PageId_Culture_Slug",
                schema: "CMS",
                table: "PageTranslations",
                columns: new[] { "PageId", "Culture", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Name",
                schema: "CMS",
                table: "Menus",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemTranslations_MenuItemId_Culture",
                schema: "CMS",
                table: "MenuItemTranslations",
                columns: new[] { "MenuItemId", "Culture" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyRewardTiers_Program_Points",
                schema: "Loyalty",
                table: "LoyaltyRewardTiers",
                columns: new[] { "LoyaltyProgramId", "PointsRequired" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyPrograms_Business",
                schema: "Loyalty",
                table: "LoyaltyPrograms",
                column: "BusinessId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyAccounts_Business_User",
                schema: "Loyalty",
                table: "LoyaltyAccounts",
                columns: new[] { "BusinessId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_BusinessId_Code",
                schema: "Billing",
                table: "FinancialAccounts",
                columns: new[] { "BusinessId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegments_Name",
                schema: "CRM",
                table: "CustomerSegments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegmentMemberships_CustomerId_CustomerSegmentId",
                schema: "CRM",
                table: "CustomerSegmentMemberships",
                columns: new[] { "CustomerId", "CustomerSegmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserId",
                schema: "CRM",
                table: "Customers",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_VariantId",
                schema: "CartCheckout",
                table: "CartItems",
                columns: new[] { "CartId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CampaignDeliveries_IdempotencyKey",
                schema: "Marketing",
                table: "CampaignDeliveries",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_BusinessId",
                schema: "Billing",
                table: "BusinessSubscriptions",
                column: "BusinessId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_Provider_ProviderSubscriptionId",
                schema: "Billing",
                table: "BusinessSubscriptions",
                columns: new[] { "Provider", "ProviderSubscriptionId" },
                unique: true,
                filter: "[ProviderSubscriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaffQrCodes_Token",
                schema: "Businesses",
                table: "BusinessStaffQrCodes",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AnalyticsExportJobs_Business_IdempotencyKey",
                schema: "Integration",
                table: "AnalyticsExportJobs",
                columns: new[] { "BusinessId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AnalyticsExportFiles_Job_StorageKey",
                schema: "Integration",
                table: "AnalyticsExportFiles",
                columns: new[] { "AnalyticsExportJobId", "StorageKey" },
                unique: true);
        }
    }
}
