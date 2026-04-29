using Darwin.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DarwinDbContext))]
    [Migration("20260429103000_AddPostgreSqlDirectLikeSearchIndexes")]
    public partial class AddPostgreSqlDirectLikeSearchIndexes : Migration
    {
        private static readonly SearchIndex[] SearchIndexes =
        [
            new("Catalog", "ProductTranslations", "Name", "IX_PG_ProductTranslations_Name_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Catalog", "ProductTranslations", "Slug", "IX_PG_ProductTranslations_Slug_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Catalog", "CategoryTranslations", "Name", "IX_PG_CategoryTranslations_Name_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Catalog", "CategoryTranslations", "Slug", "IX_PG_CategoryTranslations_Slug_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Catalog", "BrandTranslations", "Name", "IX_PG_BrandTranslations_Name_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Catalog", "ProductVariants", "Sku", "IX_PG_ProductVariants_Sku_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Catalog", "AddOnGroups", "Name", "IX_PG_AddOnGroups_Name_Like_Trgm", "\"IsDeleted\" = FALSE"),

            new("CMS", "PageTranslations", "Title", "IX_PG_PageTranslations_Title_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("CMS", "PageTranslations", "Slug", "IX_PG_PageTranslations_Slug_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("CMS", "MediaAssets", "Title", "IX_PG_MediaAssets_Title_Like_Trgm", "\"IsDeleted\" = FALSE AND \"Title\" IS NOT NULL"),
            new("CMS", "MediaAssets", "OriginalFileName", "IX_PG_MediaAssets_OriginalFileName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"OriginalFileName\" IS NOT NULL"),
            new("CMS", "MediaAssets", "Alt", "IX_PG_MediaAssets_Alt_Like_Trgm", "\"IsDeleted\" = FALSE AND \"Alt\" IS NOT NULL"),
            new("CMS", "MediaAssets", "Url", "IX_PG_MediaAssets_Url_Like_Trgm", "\"IsDeleted\" = FALSE AND \"Url\" IS NOT NULL"),

            new("Businesses", "Businesses", "Name", "IX_PG_Businesses_Name_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Businesses", "Businesses", "LegalName", "IX_PG_Businesses_LegalName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"LegalName\" IS NOT NULL"),
            new("Businesses", "Businesses", "ShortDescription", "IX_PG_Businesses_ShortDescription_Like_Trgm", "\"IsDeleted\" = FALSE AND \"ShortDescription\" IS NOT NULL"),
            new("Businesses", "Businesses", "BrandDisplayName", "IX_PG_Businesses_BrandDisplayName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"BrandDisplayName\" IS NOT NULL"),
            new("Businesses", "Businesses", "SupportEmail", "IX_PG_Businesses_SupportEmail_Like_Trgm", "\"IsDeleted\" = FALSE AND \"SupportEmail\" IS NOT NULL"),
            new("Businesses", "Businesses", "CommunicationSenderName", "IX_PG_Businesses_CommunicationSenderName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"CommunicationSenderName\" IS NOT NULL"),
            new("Businesses", "Businesses", "CommunicationReplyToEmail", "IX_PG_Businesses_CommunicationReplyToEmail_Like_Trgm", "\"IsDeleted\" = FALSE AND \"CommunicationReplyToEmail\" IS NOT NULL"),
            new("Businesses", "BusinessLocations", "Name", "IX_PG_BusinessLocations_Name_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Businesses", "BusinessLocations", "AddressLine1", "IX_PG_BusinessLocations_AddressLine1_Like_Trgm", "\"IsDeleted\" = FALSE AND \"AddressLine1\" IS NOT NULL"),
            new("Businesses", "BusinessLocations", "City", "IX_PG_BusinessLocations_City_Like_Trgm", "\"IsDeleted\" = FALSE AND \"City\" IS NOT NULL"),
            new("Businesses", "BusinessLocations", "PostalCode", "IX_PG_BusinessLocations_PostalCode_Like_Trgm", "\"IsDeleted\" = FALSE AND \"PostalCode\" IS NOT NULL"),
            new("Businesses", "BusinessInvitations", "Email", "IX_PG_BusinessInvitations_Email_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Businesses", "BusinessOwnerOverrideAudits", "Reason", "IX_PG_BusinessOwnerOverrideAudits_Reason_Like_Trgm", "\"IsDeleted\" = FALSE AND \"Reason\" IS NOT NULL"),
            new("Businesses", "BusinessOwnerOverrideAudits", "ActorDisplayName", "IX_PG_BusinessOwnerOverrideAudits_ActorDisplayName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"ActorDisplayName\" IS NOT NULL"),

            new("Identity", "Users", "Email", "IX_PG_Users_Email_Like_Trgm", "\"IsDeleted\" = FALSE AND \"Email\" IS NOT NULL"),
            new("Identity", "Users", "FirstName", "IX_PG_Users_FirstName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"FirstName\" IS NOT NULL"),
            new("Identity", "Users", "LastName", "IX_PG_Users_LastName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"LastName\" IS NOT NULL"),
            new("Identity", "Permissions", "Key", "IX_PG_Permissions_Key_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Identity", "Permissions", "DisplayName", "IX_PG_Permissions_DisplayName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"DisplayName\" IS NOT NULL"),
            new("Identity", "Roles", "Key", "IX_PG_Roles_Key_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Identity", "Roles", "DisplayName", "IX_PG_Roles_DisplayName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"DisplayName\" IS NOT NULL"),
            new("Identity", "UserDevices", "DeviceId", "IX_PG_UserDevices_DeviceId_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Identity", "UserDevices", "DeviceModel", "IX_PG_UserDevices_DeviceModel_Like_Trgm", "\"IsDeleted\" = FALSE AND \"DeviceModel\" IS NOT NULL"),
            new("Identity", "UserDevices", "AppVersion", "IX_PG_UserDevices_AppVersion_Like_Trgm", "\"IsDeleted\" = FALSE AND \"AppVersion\" IS NOT NULL"),

            new("Integration", "ChannelDispatchAudits", "RecipientAddress", "IX_PG_ChannelDispatchAudits_RecipientAddress_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "ChannelDispatchAudits", "IntendedRecipientAddress", "IX_PG_ChannelDispatchAudits_IntendedRecipientAddress_Like_Trgm", "\"IsDeleted\" = FALSE AND \"IntendedRecipientAddress\" IS NOT NULL"),
            new("Integration", "ChannelDispatchAudits", "MessagePreview", "IX_PG_ChannelDispatchAudits_MessagePreview_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "ChannelDispatchAudits", "Provider", "IX_PG_ChannelDispatchAudits_Provider_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "ChannelDispatchOperations", "RecipientAddress", "IX_PG_ChannelDispatchOperations_RecipientAddress_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "ChannelDispatchOperations", "IntendedRecipientAddress", "IX_PG_ChannelDispatchOperations_IntendedRecipientAddress_Like_Trgm", "\"IsDeleted\" = FALSE AND \"IntendedRecipientAddress\" IS NOT NULL"),
            new("Integration", "ChannelDispatchOperations", "MessageText", "IX_PG_ChannelDispatchOperations_MessageText_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "EmailDispatchAudits", "RecipientEmail", "IX_PG_EmailDispatchAudits_RecipientEmail_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "EmailDispatchAudits", "IntendedRecipientEmail", "IX_PG_EmailDispatchAudits_IntendedRecipientEmail_Like_Trgm", "\"IsDeleted\" = FALSE AND \"IntendedRecipientEmail\" IS NOT NULL"),
            new("Integration", "EmailDispatchAudits", "Subject", "IX_PG_EmailDispatchAudits_Subject_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "EmailDispatchOperations", "RecipientEmail", "IX_PG_EmailDispatchOperations_RecipientEmail_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "EmailDispatchOperations", "IntendedRecipientEmail", "IX_PG_EmailDispatchOperations_IntendedRecipientEmail_Like_Trgm", "\"IsDeleted\" = FALSE AND \"IntendedRecipientEmail\" IS NOT NULL"),
            new("Integration", "EmailDispatchOperations", "Subject", "IX_PG_EmailDispatchOperations_Subject_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "ProviderCallbackInboxMessages", "Provider", "IX_PG_ProviderCallbackInboxMessages_Provider_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "ProviderCallbackInboxMessages", "CallbackType", "IX_PG_ProviderCallbackInboxMessages_CallbackType_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "ProviderCallbackInboxMessages", "IdempotencyKey", "IX_PG_ProviderCallbackInboxMessages_IdempotencyKey_Like_Trgm", "\"IsDeleted\" = FALSE AND \"IdempotencyKey\" IS NOT NULL"),
            new("Integration", "ProviderCallbackInboxMessages", "FailureReason", "IX_PG_ProviderCallbackInboxMessages_FailureReason_Like_Trgm", "\"IsDeleted\" = FALSE AND \"FailureReason\" IS NOT NULL"),
            new("Integration", "ShipmentProviderOperations", "Provider", "IX_PG_ShipmentProviderOperations_Provider_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "ShipmentProviderOperations", "OperationType", "IX_PG_ShipmentProviderOperations_OperationType_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Integration", "ShipmentProviderOperations", "FailureReason", "IX_PG_ShipmentProviderOperations_FailureReason_Like_Trgm", "\"IsDeleted\" = FALSE AND \"FailureReason\" IS NOT NULL"),

            new("CRM", "Customers", "FirstName", "IX_PG_Customers_FirstName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"FirstName\" IS NOT NULL"),
            new("CRM", "Customers", "LastName", "IX_PG_Customers_LastName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"LastName\" IS NOT NULL"),
            new("CRM", "Customers", "Email", "IX_PG_Customers_Email_Like_Trgm", "\"IsDeleted\" = FALSE AND \"Email\" IS NOT NULL"),
            new("CRM", "Customers", "CompanyName", "IX_PG_Customers_CompanyName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"CompanyName\" IS NOT NULL"),
            new("CRM", "Leads", "FirstName", "IX_PG_Leads_FirstName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"FirstName\" IS NOT NULL"),
            new("CRM", "Leads", "LastName", "IX_PG_Leads_LastName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"LastName\" IS NOT NULL"),
            new("CRM", "Leads", "Email", "IX_PG_Leads_Email_Like_Trgm", "\"IsDeleted\" = FALSE AND \"Email\" IS NOT NULL"),
            new("CRM", "Leads", "CompanyName", "IX_PG_Leads_CompanyName_Like_Trgm", "\"IsDeleted\" = FALSE AND \"CompanyName\" IS NOT NULL"),
            new("CRM", "Opportunities", "Title", "IX_PG_Opportunities_Title_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("CRM", "CustomerSegments", "Name", "IX_PG_CustomerSegments_Name_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("CRM", "CustomerSegments", "Description", "IX_PG_CustomerSegments_Description_Like_Trgm", "\"IsDeleted\" = FALSE AND \"Description\" IS NOT NULL"),

            new("Orders", "Orders", "OrderNumber", "IX_PG_Orders_OrderNumber_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Orders", "Shipments", "TrackingNumber", "IX_PG_Shipments_TrackingNumber_Like_Trgm", "\"IsDeleted\" = FALSE AND \"TrackingNumber\" IS NOT NULL"),
            new("Orders", "Shipments", "ProviderShipmentReference", "IX_PG_Shipments_ProviderShipmentReference_Like_Trgm", "\"IsDeleted\" = FALSE AND \"ProviderShipmentReference\" IS NOT NULL"),
            new("Orders", "Shipments", "Carrier", "IX_PG_Shipments_Carrier_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Orders", "Shipments", "Service", "IX_PG_Shipments_Service_Like_Trgm", "\"IsDeleted\" = FALSE"),

            new("Shipping", "ShippingMethods", "Name", "IX_PG_ShippingMethods_Name_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Shipping", "ShippingMethods", "Carrier", "IX_PG_ShippingMethods_Carrier_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Shipping", "ShippingMethods", "Service", "IX_PG_ShippingMethods_Service_Like_Trgm", "\"IsDeleted\" = FALSE"),
            new("Shipping", "ShippingMethods", "CountriesCsv", "IX_PG_ShippingMethods_CountriesCsv_Like_Trgm", "\"IsDeleted\" = FALSE AND \"CountriesCsv\" IS NOT NULL"),

            new("Loyalty", "ScanSessions", "CreatedByDeviceId", "IX_PG_ScanSessions_CreatedByDeviceId_Like_Trgm", "\"IsDeleted\" = FALSE AND \"CreatedByDeviceId\" IS NOT NULL"),
            new("Loyalty", "ScanSessions", "FailureReason", "IX_PG_ScanSessions_FailureReason_Like_Trgm", "\"IsDeleted\" = FALSE AND \"FailureReason\" IS NOT NULL"),
            new("Loyalty", "LoyaltyRewardTiers", "Description", "IX_PG_LoyaltyRewardTiers_Description_Like_Trgm", "\"IsDeleted\" = FALSE AND \"Description\" IS NOT NULL"),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE EXTENSION IF NOT EXISTS pg_trgm;""");

            foreach (var searchIndex in SearchIndexes)
            {
                migrationBuilder.Sql($"""
                    CREATE INDEX IF NOT EXISTS "{searchIndex.Name}"
                    ON "{searchIndex.Schema}"."{searchIndex.Table}"
                    USING GIN ("{searchIndex.Column}" gin_trgm_ops)
                    WHERE {searchIndex.Filter};
                    """);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            for (var index = SearchIndexes.Length - 1; index >= 0; index--)
            {
                var searchIndex = SearchIndexes[index];
                migrationBuilder.Sql($"""DROP INDEX IF EXISTS "{searchIndex.Schema}"."{searchIndex.Name}";""");
            }
        }

        private readonly record struct SearchIndex(
            string Schema,
            string Table,
            string Column,
            string Name,
            string Filter);
    }
}
