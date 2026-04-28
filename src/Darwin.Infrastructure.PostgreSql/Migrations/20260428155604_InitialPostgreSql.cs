using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Darwin.Infrastructure.PostgreSql.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgreSql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Catalog");

            migrationBuilder.EnsureSchema(
                name: "Identity");

            migrationBuilder.EnsureSchema(
                name: "Integration");

            migrationBuilder.EnsureSchema(
                name: "Billing");

            migrationBuilder.EnsureSchema(
                name: "Businesses");

            migrationBuilder.EnsureSchema(
                name: "Marketing");

            migrationBuilder.EnsureSchema(
                name: "CartCheckout");

            migrationBuilder.EnsureSchema(
                name: "CRM");

            migrationBuilder.EnsureSchema(
                name: "Inventory");

            migrationBuilder.EnsureSchema(
                name: "Loyalty");

            migrationBuilder.EnsureSchema(
                name: "CMS");

            migrationBuilder.EnsureSchema(
                name: "Orders");

            migrationBuilder.EnsureSchema(
                name: "Pricing");

            migrationBuilder.EnsureSchema(
                name: "SEO");

            migrationBuilder.EnsureSchema(
                name: "Shipping");

            migrationBuilder.EnsureSchema(
                name: "Settings");

            migrationBuilder.CreateTable(
                name: "AddOnGroupBrands",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddOnGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnGroupBrands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AddOnGroupCategories",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddOnGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnGroupCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AddOnGroupProducts",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddOnGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnGroupProducts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AddOnGroups",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    SelectionMode = table.Column<int>(type: "integer", nullable: false),
                    MinSelections = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MaxSelections = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AddOnGroupVariants",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddOnGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnGroupVariants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingPlans",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Interval = table.Column<short>(type: "smallint", nullable: false),
                    IntervalCount = table.Column<int>(type: "integer", nullable: false),
                    TrialDays = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    FeaturesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    LogoMediaId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Businesses",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    TaxId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ShortDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContactPhoneE164 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Category = table.Column<short>(type: "smallint", nullable: false),
                    DefaultCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DefaultCulture = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    DefaultTimeZoneId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AdminTextOverridesJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    BrandDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BrandLogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BrandPrimaryColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    BrandSecondaryColorHex = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    SupportEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CommunicationSenderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CommunicationReplyToEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CustomerEmailNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CustomerMarketingEmailsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    OperationalAlertEmailsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OperationalStatus = table.Column<short>(type: "smallint", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspendedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspensionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Businesses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMedias",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessMedias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessOwnerOverrideAudits",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    AffectedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionKind = table.Column<short>(type: "smallint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ActorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessOwnerOverrideAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                schema: "Marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Subtitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MediaUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    LandingUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Channels = table.Column<short>(type: "smallint", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TargetingJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    PayloadJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Carts",
                schema: "CartCheckout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousId = table.Column<string>(type: "text", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CouponCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelDispatchAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FlowKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TemplateKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CorrelationKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IntendedRecipientAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    MessagePreview = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelDispatchAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelDispatchOperations",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RecipientAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IntendedRecipientAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MessageText = table.Column<string>(type: "text", nullable: false),
                    FlowKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TemplateKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CorrelationKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelDispatchOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSegments",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSegments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailDispatchAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FlowKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TemplateKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CorrelationKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    IntendedRecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDispatchAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailDispatchOperations",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    IntendedRecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Subject = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: false),
                    FlowKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TemplateKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CorrelationKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDispatchOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventLogs",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnonymousId = table.Column<string>(type: "text", nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PropertiesJson = table.Column<string>(type: "text", nullable: false),
                    UtmSnapshotJson = table.Column<string>(type: "text", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    ExpenseDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityDelta = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyAccounts",
                schema: "Loyalty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PointsBalance = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LifetimePoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastAccrualAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPrograms",
                schema: "Loyalty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AccrualMode = table.Column<int>(type: "integer", nullable: false),
                    PointsPerCurrencyUnit = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RulesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                schema: "CMS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Alt = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    Role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Menus",
                schema: "CMS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Culture = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                schema: "CMS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    ContentHtml = table.Column<string>(type: "text", nullable: false),
                    MetaTitle = table.Column<string>(type: "text", nullable: false),
                    MetaDescription = table.Column<string>(type: "text", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PublishStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    PublishStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    RelatedProductIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionRedemptions",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionRedemptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: true),
                    Percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MinSubtotalNetMinor = table.Column<long>(type: "bigint", nullable: true),
                    ConditionsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    PerCustomerLimit = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderCallbackInboxMessages",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CallbackType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderCallbackInboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QrCodeTokens",
                schema: "Loyalty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoyaltyAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Purpose = table.Column<int>(type: "integer", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssuedDeviceId = table.Column<string>(type: "text", nullable: true),
                    ConsumedByBusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConsumedByBusinessLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QrCodeTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RedirectRules",
                schema: "SEO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPath = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    To = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    IsPermanent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedirectRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScanSessions",
                schema: "Loyalty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QrCodeTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoyaltyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SelectedRewardsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByDeviceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResultingTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentProviderOperations",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OperationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentProviderOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingMethods",
                schema: "Shipping",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Carrier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Service = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CountriesCsv = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                schema: "Settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    HomeSlug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DefaultCulture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SupportedCulturesCsv = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DefaultCountry = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DefaultCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DateFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TimeFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AdminTextOverridesJson = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: true),
                    JwtEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    JwtIssuer = table.Column<string>(type: "text", nullable: true),
                    JwtAudience = table.Column<string>(type: "text", nullable: true),
                    JwtAccessTokenMinutes = table.Column<int>(type: "integer", nullable: false),
                    JwtRefreshTokenDays = table.Column<int>(type: "integer", nullable: false),
                    JwtSigningKey = table.Column<string>(type: "text", nullable: true),
                    JwtPreviousSigningKey = table.Column<string>(type: "text", nullable: true),
                    JwtEmitScopes = table.Column<bool>(type: "boolean", nullable: false),
                    JwtSingleDeviceOnly = table.Column<bool>(type: "boolean", nullable: false),
                    JwtRequireDeviceBinding = table.Column<bool>(type: "boolean", nullable: false),
                    JwtClockSkewSeconds = table.Column<int>(type: "integer", nullable: false),
                    MobileQrTokenRefreshSeconds = table.Column<int>(type: "integer", nullable: false),
                    MobileMaxOutboxItems = table.Column<int>(type: "integer", nullable: false),
                    BusinessManagementWebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImpressumUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrivacyPolicyUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BusinessTermsUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AccountDeletionUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    StripeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    StripePublishableKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StripeSecretKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StripeWebhookSecret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    StripeMerchantDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VatEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultVatRatePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    PricesIncludeVat = table.Column<bool>(type: "boolean", nullable: false),
                    AllowReverseCharge = table.Column<bool>(type: "boolean", nullable: false),
                    InvoiceIssuerLegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InvoiceIssuerTaxId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    InvoiceIssuerAddressLine1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    InvoiceIssuerPostalCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    InvoiceIssuerCity = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    InvoiceIssuerCountry = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    DhlEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DhlEnvironment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DhlApiBaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DhlApiKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DhlApiSecret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DhlAccountNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DhlShipperName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DhlShipperEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DhlShipperPhoneE164 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DhlShipperStreet = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DhlShipperPostalCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    DhlShipperCity = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DhlShipperCountry = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    ShipmentAttentionDelayHours = table.Column<int>(type: "integer", nullable: false),
                    ShipmentTrackingGraceHours = table.Column<int>(type: "integer", nullable: false),
                    SoftDeleteCleanupEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SoftDeleteRetentionDays = table.Column<int>(type: "integer", nullable: false),
                    SoftDeleteCleanupBatchSize = table.Column<int>(type: "integer", nullable: false),
                    MeasurementSystem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisplayWeightUnit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DisplayLengthUnit = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    MeasurementSettingsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NumberFormattingOverridesJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SeoTitleTemplate = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SeoMetaDescriptionTemplate = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OpenGraphDefaultsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EnableCanonical = table.Column<bool>(type: "boolean", nullable: false),
                    HreflangEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    GoogleAnalyticsId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GoogleTagManagerId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GoogleSearchConsoleVerification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FeatureFlagsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WhatsAppEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppBusinessPhoneId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WhatsAppAccessToken = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    WhatsAppFromPhoneE164 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    WhatsAppAdminRecipientsCsv = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    WebAuthnRelyingPartyId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    WebAuthnRelyingPartyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    WebAuthnAllowedOriginsCsv = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    WebAuthnRequireUserVerification = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpHost = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SmtpPort = table.Column<int>(type: "integer", nullable: true),
                    SmtpEnableSsl = table.Column<bool>(type: "boolean", nullable: false),
                    SmtpUsername = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SmtpPassword = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SmtpFromAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SmtpFromDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SmsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    SmsProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SmsFromPhoneE164 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SmsApiKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SmsApiSecret = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SmsExtraSettingsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AdminAlertEmailsCsv = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AdminAlertSmsRecipientsCsv = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TransactionalEmailSubjectPrefix = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CommunicationTestInboxEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CommunicationTestSmsRecipientE164 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CommunicationTestWhatsAppRecipientE164 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    CommunicationTestEmailSubjectTemplate = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CommunicationTestEmailBodyTemplate = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CommunicationTestSmsTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CommunicationTestWhatsAppTemplate = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BusinessInvitationEmailSubjectTemplate = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    BusinessInvitationEmailBodyTemplate = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    AccountActivationEmailSubjectTemplate = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    AccountActivationEmailBodyTemplate = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    PasswordResetEmailSubjectTemplate = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PasswordResetEmailBodyTemplate = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    PhoneVerificationSmsTemplate = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PhoneVerificationWhatsAppTemplate = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PhoneVerificationPreferredChannel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PhoneVerificationAllowFallback = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionInvoices",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessSubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderInvoiceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    TotalMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    HostedInvoiceUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PdfUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LinesJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionInvoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxCategories",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SecurityStamp = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PhoneE164 = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Company = table.Column<string>(type: "text", nullable: true),
                    VatId = table.Column<string>(type: "text", nullable: true),
                    DefaultBillingAddressId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultShippingAddressId = table.Column<Guid>(type: "uuid", nullable: true),
                    MarketingConsent = table.Column<bool>(type: "boolean", nullable: false),
                    ChannelsOptInJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AcceptsTermsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    AnonymousId = table.Column<string>(type: "text", nullable: true),
                    FirstTouchUtmJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    LastTouchUtmJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    ExternalIdsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserWebAuthnCredentials",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "bytea", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    AaGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    CredentialType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AttestationFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    UserHandle = table.Column<byte[]>(type: "bytea", nullable: true),
                    LastUsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSyncedPasskey = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWebAuthnCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveries",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventRefId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponseCode = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PayloadHash = table.Column<string>(type: "text", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CallbackUrl = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Secret = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AddOnGroupTranslations",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddOnGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Culture = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnGroupTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddOnGroupTranslations_AddOnGroups_AddOnGroupId",
                        column: x => x.AddOnGroupId,
                        principalSchema: "Catalog",
                        principalTable: "AddOnGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AddOnOptions",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddOnGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddOnOptions_AddOnGroups_AddOnGroupId",
                        column: x => x.AddOnGroupId,
                        principalSchema: "Catalog",
                        principalTable: "AddOnGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BrandTranslations",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Culture = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DescriptionHtml = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandTranslations_Brands_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "Catalog",
                        principalTable: "Brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticsExportJobs",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportType = table.Column<short>(type: "smallint", nullable: false),
                    Format = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    ParametersJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    RetainUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsExportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticsExportJobs_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessEngagementStats",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false),
                    RatingSum = table.Column<int>(type: "integer", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    FavoriteCount = table.Column<int>(type: "integer", nullable: false),
                    LastCalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessEngagementStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessEngagementStats_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessInvitations",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Role = table.Column<short>(type: "smallint", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessInvitations_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessLocations",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    AddressLine2 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    AltitudeMeters = table.Column<double>(type: "double precision", nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    OpeningHoursJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    InternalNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessLocations_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessMembers",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<short>(type: "smallint", nullable: false),
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
                    table.PrimaryKey("PK_BusinessMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessMembers_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessStaffQrCodes",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<short>(type: "smallint", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IssuedDeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ConsumedDeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessStaffQrCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessStaffQrCodes_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessSubscriptions",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderCustomerId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProviderSubscriptionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelAtPeriodEnd = table.Column<bool>(type: "boolean", nullable: false),
                    CanceledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UnitPriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessSubscriptions_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignDeliveries",
                schema: "Marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    Channel = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Destination = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    FirstAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastResponseCode = table.Column<int>(type: "integer", nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignDeliveries_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "Marketing",
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                schema: "CartCheckout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SelectedAddOnValueIdsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AddOnPriceDeltaMinor = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalSchema: "CartCheckout",
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryTranslations",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    MetaTitle = table.Column<string>(type: "text", nullable: true),
                    MetaDescription = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false),
                    Culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryTranslations_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "Catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    DebitMinor = table.Column<long>(type: "bigint", nullable: false),
                    CreditMinor = table.Column<long>(type: "bigint", nullable: false),
                    Memo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalSchema: "Billing",
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPointsTransactions",
                schema: "Loyalty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoyaltyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    PointsDelta = table.Column<int>(type: "integer", nullable: false),
                    RewardRedemptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    BusinessLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPointsTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyPointsTransactions_LoyaltyAccounts_LoyaltyAccountId",
                        column: x => x.LoyaltyAccountId,
                        principalSchema: "Loyalty",
                        principalTable: "LoyaltyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyRewardRedemptions",
                schema: "Loyalty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoyaltyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoyaltyRewardTierId = table.Column<Guid>(type: "uuid", nullable: false),
                    PointsSpent = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BusinessLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RedeemedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRewardRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyRewardRedemptions_LoyaltyAccounts_LoyaltyAccountId",
                        column: x => x.LoyaltyAccountId,
                        principalSchema: "Loyalty",
                        principalTable: "LoyaltyAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyRewardTiers",
                schema: "Loyalty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoyaltyProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    PointsRequired = table.Column<int>(type: "integer", nullable: false),
                    RewardType = table.Column<int>(type: "integer", nullable: false),
                    RewardValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AllowSelfRedemption = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRewardTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyRewardTiers_LoyaltyPrograms_LoyaltyProgramId",
                        column: x => x.LoyaltyProgramId,
                        principalSchema: "Loyalty",
                        principalTable: "LoyaltyPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                schema: "CMS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalSchema: "CMS",
                        principalTable: "MenuItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuItems_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "CMS",
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItems_Menus_MenuId",
                        column: x => x.MenuId,
                        principalSchema: "CMS",
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PageTranslations",
                schema: "CMS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MetaTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    MetaDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContentHtml = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false),
                    Culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageTranslations_Pages_PageId",
                        column: x => x.PageId,
                        principalSchema: "CMS",
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductMedia_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptions",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOptions_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductTranslations",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShortDescription = table.Column<string>(type: "text", nullable: true),
                    FullDescriptionHtml = table.Column<string>(type: "text", nullable: true),
                    MetaTitle = table.Column<string>(type: "text", nullable: true),
                    MetaDescription = table.Column<string>(type: "text", nullable: true),
                    SearchKeywords = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false),
                    Culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductTranslations_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Gtin = table.Column<string>(type: "text", nullable: true),
                    ManufacturerPartNumber = table.Column<string>(type: "text", nullable: true),
                    BasePriceNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    CompareAtPriceNetMinor = table.Column<long>(type: "bigint", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    TaxCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockOnHand = table.Column<int>(type: "integer", nullable: false),
                    StockReserved = table.Column<int>(type: "integer", nullable: false),
                    ReorderPoint = table.Column<int>(type: "integer", nullable: true),
                    BackorderAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    MinOrderQty = table.Column<int>(type: "integer", nullable: true),
                    MaxOrderQty = table.Column<int>(type: "integer", nullable: true),
                    StepOrderQty = table.Column<int>(type: "integer", nullable: true),
                    PackageWeight = table.Column<int>(type: "integer", nullable: true),
                    PackageLength = table.Column<int>(type: "integer", nullable: true),
                    PackageWidth = table.Column<int>(type: "integer", nullable: true),
                    PackageHeight = table.Column<int>(type: "integer", nullable: true),
                    IsDigital = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Catalog",
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "Identity",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    PricesIncludeTax = table.Column<bool>(type: "boolean", nullable: false),
                    SubtotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TaxTotalMinor = table.Column<long>(type: "bigint", nullable: false),
                    ShippingTotalMinor = table.Column<long>(type: "bigint", nullable: false),
                    DiscountTotalMinor = table.Column<long>(type: "bigint", nullable: false),
                    GrandTotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    ShippingMethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShippingMethodName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ShippingCarrier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ShippingService = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BillingAddressJson = table.Column<string>(type: "text", nullable: false),
                    ShippingAddressJson = table.Column<string>(type: "text", nullable: false),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_ShippingMethods_ShippingMethodId",
                        column: x => x.ShippingMethodId,
                        principalSchema: "Shipping",
                        principalTable: "ShippingMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ShippingRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShippingMethodId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaxShipmentMass = table.Column<int>(type: "integer", nullable: true),
                    MaxSubtotalNetMinor = table.Column<long>(type: "bigint", nullable: true),
                    PriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingRates_ShippingMethods_ShippingMethodId",
                        column: x => x.ShippingMethodId,
                        principalSchema: "Shipping",
                        principalTable: "ShippingMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransferLines",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StockTransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransferLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransferLines_StockTransfers_StockTransferId",
                        column: x => x.StockTransferId,
                        principalSchema: "Inventory",
                        principalTable: "StockTransfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrderedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "Inventory",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Street1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Street2 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    City = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    State = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    PhoneE164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsDefaultBilling = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefaultShipping = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessFavorites",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessFavorites_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessFavorites_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessLikes",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessLikes_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessLikes_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessReviews",
                schema: "Businesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<byte>(type: "smallint", nullable: false),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    HiddenReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessReviews_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalSchema: "Businesses",
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessReviews_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    LastName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TaxProfileType = table.Column<int>(type: "integer", nullable: false),
                    VatId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestedByMeta = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDevices",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Platform = table.Column<short>(type: "smallint", nullable: false),
                    PushToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PushTokenUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeviceModel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
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
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDevices_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserEngagementSnapshots",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastActivityAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoyaltyActivityAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastOrderAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EventCount = table.Column<long>(type: "bigint", nullable: false),
                    EngagementScore30d = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SnapshotJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEngagementSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEngagementSnapshots_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTwoFactorSecrets",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecretBase32 = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Label = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTwoFactorSecrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTwoFactorSecrets_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockLevels",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReorderPoint = table.Column<int>(type: "integer", nullable: false),
                    ReorderQuantity = table.Column<int>(type: "integer", nullable: false),
                    InTransitQuantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockLevels_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AddOnOptionTranslations",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddOnOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Culture = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnOptionTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddOnOptionTranslations_AddOnOptions_AddOnOptionId",
                        column: x => x.AddOnOptionId,
                        principalSchema: "Catalog",
                        principalTable: "AddOnOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AddOnOptionValues",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddOnOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PriceDeltaMinor = table.Column<long>(type: "bigint", nullable: false),
                    Hint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnOptionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddOnOptionValues_AddOnOptions_AddOnOptionId",
                        column: x => x.AddOnOptionId,
                        principalSchema: "Catalog",
                        principalTable: "AddOnOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticsExportFiles",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalyticsExportJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsExportFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticsExportFiles_AnalyticsExportJobs_AnalyticsExportJob~",
                        column: x => x.AnalyticsExportJobId,
                        principalSchema: "Integration",
                        principalTable: "AnalyticsExportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemTranslations",
                schema: "CMS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Culture = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Label = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemTranslations_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalSchema: "CMS",
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductOptionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    ColorHex = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductOptionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductOptionValues_ProductOptions_ProductOptionId",
                        column: x => x.ProductOptionId,
                        principalSchema: "Catalog",
                        principalTable: "ProductOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariantOptionValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductOptionValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantOptionValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariantOptionValues_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "Catalog",
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderLines",
                schema: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    UnitPriceGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    LineTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    LineGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    AddOnValueIdsJson = table.Column<string>(type: "text", nullable: false),
                    AddOnPriceDeltaMinor = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderLines_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "Inventory",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "Billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderTransactionRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderPaymentIntentRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderCheckoutSessionRef = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MethodId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Carrier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Service = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderShipmentReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LabelUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    TotalWeight = table.Column<int>(type: "integer", nullable: true),
                    ShippedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastCarrierEventKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Orders",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderLines",
                schema: "Inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCostMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalCostMinor = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "Inventory",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consents",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Granted = table.Column<bool>(type: "boolean", nullable: false),
                    GrantedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consents_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "CRM",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerAddresses",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddressId = table.Column<Guid>(type: "uuid", nullable: true),
                    Line1 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Line2 = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    State = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    IsDefaultShipping = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefaultBilling = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_Addresses_AddressId",
                        column: x => x.AddressId,
                        principalSchema: "Identity",
                        principalTable: "Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerAddresses_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "CRM",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerSegmentMemberships",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerSegmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerSegmentMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerSegmentMemberships_CustomerSegments_CustomerSegment~",
                        column: x => x.CustomerSegmentId,
                        principalSchema: "CRM",
                        principalTable: "CustomerSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerSegmentMemberships_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "CRM",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    LastName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Leads_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "CRM",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Leads_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Opportunities",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    EstimatedValueMinor = table.Column<long>(type: "bigint", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    ExpectedCloseDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Opportunities_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "CRM",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Opportunities_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AddOnOptionValueTranslations",
                schema: "Catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AddOnOptionValueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Culture = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AddOnOptionValueTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AddOnOptionValueTranslations_AddOnOptionValues_AddOnOptionV~",
                        column: x => x.AddOnOptionValueId,
                        principalSchema: "Catalog",
                        principalTable: "AddOnOptionValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalTaxMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "CRM",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "Billing",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentCarrierEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Carrier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProviderShipmentReference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CarrierEventKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderStatus = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExceptionCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExceptionMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LabelUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Service = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentCarrierEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentCarrierEvents_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentLines_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Interactions",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    LeadId = table.Column<Guid>(type: "uuid", nullable: true),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Subject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interactions_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "CRM",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Interactions_Leads_LeadId",
                        column: x => x.LeadId,
                        principalSchema: "CRM",
                        principalTable: "Leads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Interactions_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalSchema: "CRM",
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Interactions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityItems",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceMinor = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityItems_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalSchema: "CRM",
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                schema: "CRM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalNetMinor = table.Column<long>(type: "bigint", nullable: false),
                    TotalGrossMinor = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "CRM",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupBrands_AddOnGroupId_BrandId",
                schema: "Catalog",
                table: "AddOnGroupBrands",
                columns: new[] { "AddOnGroupId", "BrandId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupCategories_AddOnGroupId_CategoryId",
                schema: "Catalog",
                table: "AddOnGroupCategories",
                columns: new[] { "AddOnGroupId", "CategoryId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupProducts_AddOnGroupId_ProductId",
                schema: "Catalog",
                table: "AddOnGroupProducts",
                columns: new[] { "AddOnGroupId", "ProductId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroups_IsActive",
                schema: "Catalog",
                table: "AddOnGroups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroups_IsGlobal",
                schema: "Catalog",
                table: "AddOnGroups",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupTranslations_AddOnGroupId_Culture",
                schema: "Catalog",
                table: "AddOnGroupTranslations",
                columns: new[] { "AddOnGroupId", "Culture" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnGroupVariants_AddOnGroupId_VariantId",
                schema: "Catalog",
                table: "AddOnGroupVariants",
                columns: new[] { "AddOnGroupId", "VariantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptions_AddOnGroupId_SortOrder",
                schema: "Catalog",
                table: "AddOnOptions",
                columns: new[] { "AddOnGroupId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptionTranslations_AddOnOptionId_Culture",
                schema: "Catalog",
                table: "AddOnOptionTranslations",
                columns: new[] { "AddOnOptionId", "Culture" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptionValues_AddOnOptionId_SortOrder",
                schema: "Catalog",
                table: "AddOnOptionValues",
                columns: new[] { "AddOnOptionId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptionValues_IsActive",
                schema: "Catalog",
                table: "AddOnOptionValues",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AddOnOptionValueTranslations_AddOnOptionValueId_Culture",
                schema: "Catalog",
                table: "AddOnOptionValueTranslations",
                columns: new[] { "AddOnOptionValueId", "Culture" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId",
                schema: "Identity",
                table: "Addresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId_IsDefaultBilling",
                schema: "Identity",
                table: "Addresses",
                columns: new[] { "UserId", "IsDefaultBilling" });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId_IsDefaultShipping",
                schema: "Identity",
                table: "Addresses",
                columns: new[] { "UserId", "IsDefaultShipping" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsExportFiles_ExpiresAtUtc",
                schema: "Integration",
                table: "AnalyticsExportFiles",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsExportFiles_JobId",
                schema: "Integration",
                table: "AnalyticsExportFiles",
                column: "AnalyticsExportJobId");

            migrationBuilder.CreateIndex(
                name: "UX_AnalyticsExportFiles_Job_StorageKey",
                schema: "Integration",
                table: "AnalyticsExportFiles",
                columns: new[] { "AnalyticsExportJobId", "StorageKey" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsExportJobs_BusinessId",
                schema: "Integration",
                table: "AnalyticsExportJobs",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsExportJobs_RequestedByUserId",
                schema: "Integration",
                table: "AnalyticsExportJobs",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsExportJobs_RetainUntilUtc",
                schema: "Integration",
                table: "AnalyticsExportJobs",
                column: "RetainUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsExportJobs_Status",
                schema: "Integration",
                table: "AnalyticsExportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_AnalyticsExportJobs_Business_IdempotencyKey",
                schema: "Integration",
                table: "AnalyticsExportJobs",
                columns: new[] { "BusinessId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlans_Code",
                schema: "Billing",
                table: "BillingPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlans_IsActive",
                schema: "Billing",
                table: "BillingPlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_Slug",
                schema: "Catalog",
                table: "Brands",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BrandTranslations_BrandId_Culture",
                schema: "Catalog",
                table: "BrandTranslations",
                columns: new[] { "BrandId", "Culture" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "UX_BusinessEngagementStats_BusinessId",
                schema: "Businesses",
                table: "BusinessEngagementStats",
                column: "BusinessId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Category",
                schema: "Businesses",
                table: "Businesses",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_IsActive",
                schema: "Businesses",
                table: "Businesses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_Name",
                schema: "Businesses",
                table: "Businesses",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_OperationalStatus",
                schema: "Businesses",
                table: "Businesses",
                column: "OperationalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFavorites_BusinessId",
                schema: "Businesses",
                table: "BusinessFavorites",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessFavorites_UserId",
                schema: "Businesses",
                table: "BusinessFavorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_BusinessFavorites_User_Business",
                schema: "Businesses",
                table: "BusinessFavorites",
                columns: new[] { "UserId", "BusinessId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessInvitations_Business_NormalizedEmail",
                schema: "Businesses",
                table: "BusinessInvitations",
                columns: new[] { "BusinessId", "NormalizedEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessInvitations_ExpiresAtUtc",
                schema: "Businesses",
                table: "BusinessInvitations",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessInvitations_Status",
                schema: "Businesses",
                table: "BusinessInvitations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_BusinessInvitations_Token",
                schema: "Businesses",
                table: "BusinessInvitations",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLikes_BusinessId",
                schema: "Businesses",
                table: "BusinessLikes",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLikes_UserId",
                schema: "Businesses",
                table: "BusinessLikes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_BusinessLikes_User_Business",
                schema: "Businesses",
                table: "BusinessLikes",
                columns: new[] { "UserId", "BusinessId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLocations_BusinessId",
                schema: "Businesses",
                table: "BusinessLocations",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLocations_BusinessId_IsPrimary",
                schema: "Businesses",
                table: "BusinessLocations",
                columns: new[] { "BusinessId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMedias_BusinessId",
                schema: "Businesses",
                table: "BusinessMedias",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMedias_BusinessId_SortOrder",
                schema: "Businesses",
                table: "BusinessMedias",
                columns: new[] { "BusinessId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMedias_BusinessLocationId",
                schema: "Businesses",
                table: "BusinessMedias",
                column: "BusinessLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMembers_BusinessId_UserId",
                schema: "Businesses",
                table: "BusinessMembers",
                columns: new[] { "BusinessId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessMembers_UserId",
                schema: "Businesses",
                table: "BusinessMembers",
                column: "UserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_BusinessReviews_Business_Visibility",
                schema: "Businesses",
                table: "BusinessReviews",
                columns: new[] { "BusinessId", "IsHidden", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "UX_BusinessReviews_User_Business",
                schema: "Businesses",
                table: "BusinessReviews",
                columns: new[] { "UserId", "BusinessId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaffQrCodes_BusinessId",
                schema: "Businesses",
                table: "BusinessStaffQrCodes",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaffQrCodes_BusinessMemberId",
                schema: "Businesses",
                table: "BusinessStaffQrCodes",
                column: "BusinessMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaffQrCodes_ExpiresAtUtc",
                schema: "Businesses",
                table: "BusinessStaffQrCodes",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaffQrCodes_Token",
                schema: "Businesses",
                table: "BusinessStaffQrCodes",
                column: "Token",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_BillingPlanId",
                schema: "Billing",
                table: "BusinessSubscriptions",
                column: "BillingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_BusinessId",
                schema: "Billing",
                table: "BusinessSubscriptions",
                column: "BusinessId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessSubscriptions_Provider_ProviderSubscriptionId",
                schema: "Billing",
                table: "BusinessSubscriptions",
                columns: new[] { "Provider", "ProviderSubscriptionId" },
                unique: true,
                filter: "\"ProviderSubscriptionId\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignDeliveries_BusinessId",
                schema: "Marketing",
                table: "CampaignDeliveries",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignDeliveries_CampaignId",
                schema: "Marketing",
                table: "CampaignDeliveries",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignDeliveries_Channel",
                schema: "Marketing",
                table: "CampaignDeliveries",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignDeliveries_LastAttemptAtUtc",
                schema: "Marketing",
                table: "CampaignDeliveries",
                column: "LastAttemptAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignDeliveries_RecipientUserId",
                schema: "Marketing",
                table: "CampaignDeliveries",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignDeliveries_Status",
                schema: "Marketing",
                table: "CampaignDeliveries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_CampaignDeliveries_IdempotencyKey",
                schema: "Marketing",
                table: "CampaignDeliveries",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_BusinessId",
                schema: "Marketing",
                table: "Campaigns",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_EndsAtUtc",
                schema: "Marketing",
                table: "Campaigns",
                column: "EndsAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_IsActive",
                schema: "Marketing",
                table: "Campaigns",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_StartsAtUtc",
                schema: "Marketing",
                table: "Campaigns",
                column: "StartsAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_VariantId",
                schema: "CartCheckout",
                table: "CartItems",
                columns: new[] { "CartId", "VariantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_AnonymousId",
                schema: "CartCheckout",
                table: "Carts",
                column: "AnonymousId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_UserId",
                schema: "CartCheckout",
                table: "Carts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTranslations_CategoryId",
                schema: "Catalog",
                table: "CategoryTranslations",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryTranslations_Culture_Slug",
                schema: "Catalog",
                table: "CategoryTranslations",
                columns: new[] { "Culture", "Slug" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_AttemptedAtUtc",
                table: "ChannelDispatchAudits",
                column: "AttemptedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_BusinessId",
                table: "ChannelDispatchAudits",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_Channel",
                table: "ChannelDispatchAudits",
                column: "Channel");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_CorrelationKey",
                table: "ChannelDispatchAudits",
                column: "CorrelationKey");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_FlowKey",
                table: "ChannelDispatchAudits",
                column: "FlowKey");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_IntendedRecipientAddress",
                table: "ChannelDispatchAudits",
                column: "IntendedRecipientAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_RecipientAddress",
                table: "ChannelDispatchAudits",
                column: "RecipientAddress");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchAudits_Status",
                table: "ChannelDispatchAudits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchOperations_Channel_Status_CreatedAtUtc",
                schema: "Integration",
                table: "ChannelDispatchOperations",
                columns: new[] { "Channel", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchOperations_CorrelationKey",
                schema: "Integration",
                table: "ChannelDispatchOperations",
                column: "CorrelationKey");

            migrationBuilder.CreateIndex(
                name: "IX_ChannelDispatchOperations_IntendedRecipientAddress",
                schema: "Integration",
                table: "ChannelDispatchOperations",
                column: "IntendedRecipientAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Consents_CustomerId_Type",
                schema: "CRM",
                table: "Consents",
                columns: new[] { "CustomerId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_AddressId",
                schema: "CRM",
                table: "CustomerAddresses",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_CustomerId",
                schema: "CRM",
                table: "CustomerAddresses",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                schema: "CRM",
                table: "Customers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_UserId",
                schema: "CRM",
                table: "Customers",
                column: "UserId",
                unique: true,
                filter: "\"UserId\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegmentMemberships_CustomerId_CustomerSegmentId",
                schema: "CRM",
                table: "CustomerSegmentMemberships",
                columns: new[] { "CustomerId", "CustomerSegmentId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegmentMemberships_CustomerSegmentId",
                schema: "CRM",
                table: "CustomerSegmentMemberships",
                column: "CustomerSegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerSegments_Name",
                schema: "CRM",
                table: "CustomerSegments",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_AttemptedAtUtc",
                table: "EmailDispatchAudits",
                column: "AttemptedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_BusinessId",
                table: "EmailDispatchAudits",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_CorrelationKey",
                table: "EmailDispatchAudits",
                column: "CorrelationKey");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_FlowKey",
                table: "EmailDispatchAudits",
                column: "FlowKey");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_IntendedRecipientEmail",
                table: "EmailDispatchAudits",
                column: "IntendedRecipientEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_RecipientEmail",
                table: "EmailDispatchAudits",
                column: "RecipientEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchAudits_Status",
                table: "EmailDispatchAudits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchOperations_Provider_Status_CreatedAtUtc",
                schema: "Integration",
                table: "EmailDispatchOperations",
                columns: new[] { "Provider", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDispatchOperations_RecipientEmail_FlowKey_BusinessId_C~",
                schema: "Integration",
                table: "EmailDispatchOperations",
                columns: new[] { "RecipientEmail", "FlowKey", "BusinessId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_Type_OccurredAtUtc",
                schema: "Integration",
                table: "EventLogs",
                columns: new[] { "Type", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BusinessId",
                schema: "Billing",
                table: "Expenses",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDateUtc",
                schema: "Billing",
                table: "Expenses",
                column: "ExpenseDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SupplierId",
                schema: "Billing",
                table: "Expenses",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_BusinessId",
                schema: "Billing",
                table: "FinancialAccounts",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_BusinessId_Code",
                schema: "Billing",
                table: "FinancialAccounts",
                columns: new[] { "BusinessId", "Code" },
                unique: true,
                filter: "\"Code\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Interactions_CustomerId",
                schema: "CRM",
                table: "Interactions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Interactions_LeadId",
                schema: "CRM",
                table: "Interactions",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Interactions_OpportunityId",
                schema: "CRM",
                table: "Interactions",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_Interactions_UserId",
                schema: "CRM",
                table: "Interactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductVariantId",
                schema: "Inventory",
                table: "InventoryTransactions",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ReferenceId",
                schema: "Inventory",
                table: "InventoryTransactions",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_WarehouseId",
                schema: "Inventory",
                table: "InventoryTransactions",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                schema: "CRM",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BusinessId",
                schema: "CRM",
                table: "Invoices",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                schema: "CRM",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderId",
                schema: "CRM",
                table: "Invoices",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PaymentId",
                schema: "CRM",
                table: "Invoices",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_BusinessId",
                schema: "Billing",
                table: "JournalEntries",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_EntryDateUtc",
                schema: "Billing",
                table: "JournalEntries",
                column: "EntryDateUtc");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_AccountId",
                schema: "Billing",
                table: "JournalEntryLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_JournalEntryId",
                schema: "Billing",
                table: "JournalEntryLines",
                column: "JournalEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_AssignedToUserId",
                schema: "CRM",
                table: "Leads",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_CustomerId",
                schema: "CRM",
                table: "Leads",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Email",
                schema: "CRM",
                table: "Leads",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Leads_Status",
                schema: "CRM",
                table: "Leads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyAccounts_Business_User",
                schema: "Loyalty",
                table: "LoyaltyAccounts",
                columns: new[] { "BusinessId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointsTransactions_BusinessId",
                schema: "Loyalty",
                table: "LoyaltyPointsTransactions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointsTransactions_BusinessLocationId",
                schema: "Loyalty",
                table: "LoyaltyPointsTransactions",
                column: "BusinessLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointsTransactions_LoyaltyAccountId",
                schema: "Loyalty",
                table: "LoyaltyPointsTransactions",
                column: "LoyaltyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointsTransactions_RewardRedemptionId",
                schema: "Loyalty",
                table: "LoyaltyPointsTransactions",
                column: "RewardRedemptionId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPrograms_IsActive",
                schema: "Loyalty",
                table: "LoyaltyPrograms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyPrograms_Business",
                schema: "Loyalty",
                table: "LoyaltyPrograms",
                column: "BusinessId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRewardRedemptions_BusinessId",
                schema: "Loyalty",
                table: "LoyaltyRewardRedemptions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRewardRedemptions_BusinessLocationId",
                schema: "Loyalty",
                table: "LoyaltyRewardRedemptions",
                column: "BusinessLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRewardRedemptions_LoyaltyAccountId",
                schema: "Loyalty",
                table: "LoyaltyRewardRedemptions",
                column: "LoyaltyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRewardRedemptions_LoyaltyRewardTierId",
                schema: "Loyalty",
                table: "LoyaltyRewardRedemptions",
                column: "LoyaltyRewardTierId");

            migrationBuilder.CreateIndex(
                name: "UX_LoyaltyRewardTiers_Program_Points",
                schema: "Loyalty",
                table: "LoyaltyRewardTiers",
                columns: new[] { "LoyaltyProgramId", "PointsRequired" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ContentHash",
                schema: "CMS",
                table: "MediaAssets",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Url",
                schema: "CMS",
                table: "MediaAssets",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_IsActive",
                schema: "CMS",
                table: "MenuItems",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuId",
                schema: "CMS",
                table: "MenuItems",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuId_ParentId_SortOrder",
                schema: "CMS",
                table: "MenuItems",
                columns: new[] { "MenuId", "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuItemId",
                schema: "CMS",
                table: "MenuItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_ParentId",
                schema: "CMS",
                table: "MenuItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemTranslations_MenuItemId_Culture",
                schema: "CMS",
                table: "MenuItemTranslations",
                columns: new[] { "MenuItemId", "Culture" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Name",
                schema: "CMS",
                table: "Menus",
                column: "Name",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_AssignedToUserId",
                schema: "CRM",
                table: "Opportunities",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_CustomerId",
                schema: "CRM",
                table: "Opportunities",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_Stage",
                schema: "CRM",
                table: "Opportunities",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityItems_OpportunityId",
                schema: "CRM",
                table: "OpportunityItems",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityItems_ProductVariantId",
                schema: "CRM",
                table: "OpportunityItems",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_OrderId",
                schema: "Orders",
                table: "OrderLines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_WarehouseId",
                schema: "Orders",
                table: "OrderLines",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                schema: "Orders",
                table: "Orders",
                column: "OrderNumber",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingMethodId",
                schema: "Orders",
                table: "Orders",
                column: "ShippingMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PageTranslations_PageId_Culture_Slug",
                schema: "CMS",
                table: "PageTranslations",
                columns: new[] { "PageId", "Culture", "Slug" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "UX_ResetToken_User_Token",
                schema: "Identity",
                table: "PasswordResetTokens",
                columns: new[] { "UserId", "Token" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BusinessId",
                schema: "Billing",
                table: "Payments",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CustomerId",
                schema: "Billing",
                table: "Payments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceId",
                schema: "Billing",
                table: "Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                schema: "Billing",
                table: "Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderCheckoutSessionRef",
                schema: "Billing",
                table: "Payments",
                column: "ProviderCheckoutSessionRef");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderPaymentIntentRef",
                schema: "Billing",
                table: "Payments",
                column: "ProviderPaymentIntentRef");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                schema: "Billing",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_Permission_Key",
                schema: "Identity",
                table: "Permissions",
                column: "Key",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMedia_ProductId",
                table: "ProductMedia",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_ProductId",
                schema: "Catalog",
                table: "ProductOptions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_ProductOptionId",
                table: "ProductOptionValues",
                column: "ProductOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTranslations_Culture_Slug",
                schema: "Catalog",
                table: "ProductTranslations",
                columns: new[] { "Culture", "Slug" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ProductTranslations_ProductId",
                schema: "Catalog",
                table: "ProductTranslations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId",
                schema: "Catalog",
                table: "ProductVariants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Sku",
                schema: "Catalog",
                table: "ProductVariants",
                column: "Sku",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_OrderId",
                schema: "Pricing",
                table: "PromotionRedemptions",
                column: "OrderId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_PromotionId",
                schema: "Pricing",
                table: "PromotionRedemptions",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionRedemptions_PromotionId_UserId",
                schema: "Pricing",
                table: "PromotionRedemptions",
                columns: new[] { "PromotionId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_IsActive",
                schema: "Pricing",
                table: "Promotions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_StartsAtUtc_EndsAtUtc",
                schema: "Pricing",
                table: "Promotions",
                columns: new[] { "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_Promotions_Code_Active",
                schema: "Pricing",
                table: "Promotions",
                column: "Code",
                filter: "\"Code\" IS NOT NULL AND \"IsActive\" = TRUE AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCallbackInboxMessages_IdempotencyKey",
                schema: "Integration",
                table: "ProviderCallbackInboxMessages",
                column: "IdempotencyKey");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderCallbackInboxMessages_Provider_Status_CreatedAtUtc",
                schema: "Integration",
                table: "ProviderCallbackInboxMessages",
                columns: new[] { "Provider", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_ProductVariantId",
                schema: "Inventory",
                table: "PurchaseOrderLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderLines_PurchaseOrderId",
                schema: "Inventory",
                table: "PurchaseOrderLines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BusinessId",
                schema: "Inventory",
                table: "PurchaseOrders",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BusinessId_OrderNumber",
                schema: "Inventory",
                table: "PurchaseOrders",
                columns: new[] { "BusinessId", "OrderNumber" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "Inventory",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodeTokens_ExpiresAtUtc",
                schema: "Loyalty",
                table: "QrCodeTokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodeTokens_LoyaltyAccountId",
                schema: "Loyalty",
                table: "QrCodeTokens",
                column: "LoyaltyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodeTokens_UserId",
                schema: "Loyalty",
                table: "QrCodeTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_QrCodeTokens_Token",
                schema: "Loyalty",
                table: "QrCodeTokens",
                column: "Token",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RedirectRules_FromPath",
                schema: "SEO",
                table: "RedirectRules",
                column: "FromPath",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "Identity",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "UX_RolePermission_Role_Permission",
                schema: "Identity",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "UX_Role_Name",
                schema: "Identity",
                table: "Roles",
                column: "Key",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "UX_Role_NormalizedName",
                schema: "Identity",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ScanSessions_BusinessId",
                schema: "Loyalty",
                table: "ScanSessions",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanSessions_BusinessLocationId",
                schema: "Loyalty",
                table: "ScanSessions",
                column: "BusinessLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanSessions_ExpiresAtUtc",
                schema: "Loyalty",
                table: "ScanSessions",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ScanSessions_LoyaltyAccountId",
                schema: "Loyalty",
                table: "ScanSessions",
                column: "LoyaltyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanSessions_QrCodeTokenId",
                schema: "Loyalty",
                table: "ScanSessions",
                column: "QrCodeTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanSessions_ResultingTransactionId",
                schema: "Loyalty",
                table: "ScanSessions",
                column: "ResultingTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCarrierEvents_ProviderShipmentReference_Carrier_Occ~",
                table: "ShipmentCarrierEvents",
                columns: new[] { "ProviderShipmentReference", "Carrier", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentCarrierEvents_ShipmentId_OccurredAtUtc",
                table: "ShipmentCarrierEvents",
                columns: new[] { "ShipmentId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentLines_ShipmentId",
                table: "ShipmentLines",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentProviderOperations_ShipmentId_Provider_OperationTyp~",
                schema: "Integration",
                table: "ShipmentProviderOperations",
                columns: new[] { "ShipmentId", "Provider", "OperationType", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OrderId",
                table: "Shipments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ProviderShipmentReference",
                table: "Shipments",
                column: "ProviderShipmentReference");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingMethods_Name_Carrier_Service",
                schema: "Shipping",
                table: "ShippingMethods",
                columns: new[] { "Name", "Carrier", "Service" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRates_ShippingMethodId_SortOrder",
                table: "ShippingRates",
                columns: new[] { "ShippingMethodId", "SortOrder" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_ContactEmail",
                schema: "Settings",
                table: "SiteSettings",
                column: "ContactEmail");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_DefaultCulture",
                schema: "Settings",
                table: "SiteSettings",
                column: "DefaultCulture");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_DefaultCurrency",
                schema: "Settings",
                table: "SiteSettings",
                column: "DefaultCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_Id",
                schema: "Settings",
                table: "SiteSettings",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_ProductVariantId",
                schema: "Inventory",
                table: "StockLevels",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_WarehouseId",
                schema: "Inventory",
                table: "StockLevels",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockLevels_WarehouseId_ProductVariantId",
                schema: "Inventory",
                table: "StockLevels",
                columns: new[] { "WarehouseId", "ProductVariantId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_ProductVariantId",
                schema: "Inventory",
                table: "StockTransferLines",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransferLines_StockTransferId",
                schema: "Inventory",
                table: "StockTransferLines",
                column: "StockTransferId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromWarehouseId",
                schema: "Inventory",
                table: "StockTransfers",
                column: "FromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_Status",
                schema: "Inventory",
                table: "StockTransfers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToWarehouseId",
                schema: "Inventory",
                table: "StockTransfers",
                column: "ToWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionInvoices_BusinessId",
                schema: "Billing",
                table: "SubscriptionInvoices",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionInvoices_BusinessSubscriptionId",
                schema: "Billing",
                table: "SubscriptionInvoices",
                column: "BusinessSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionInvoices_IssuedAtUtc",
                schema: "Billing",
                table: "SubscriptionInvoices",
                column: "IssuedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionInvoices_Provider_ProviderInvoiceId",
                schema: "Billing",
                table: "SubscriptionInvoices",
                columns: new[] { "Provider", "ProviderInvoiceId" },
                unique: true,
                filter: "\"ProviderInvoiceId\" IS NOT NULL AND \"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_BusinessId",
                schema: "Inventory",
                table: "Suppliers",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_BusinessId_Name",
                schema: "Inventory",
                table: "Suppliers",
                columns: new[] { "BusinessId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxCategories_EffectiveFromUtc",
                schema: "Pricing",
                table: "TaxCategories",
                column: "EffectiveFromUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TaxCategories_Name",
                schema: "Pricing",
                table: "TaxCategories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_IsActive",
                schema: "Identity",
                table: "UserDevices",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_LastSeenAtUtc",
                schema: "Identity",
                table: "UserDevices",
                column: "LastSeenAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_UserId",
                schema: "Identity",
                table: "UserDevices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_UserDevices_User_DeviceId",
                schema: "Identity",
                table: "UserDevices",
                columns: new[] { "UserId", "DeviceId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_UserEngagementSnapshots_CalculatedAtUtc",
                schema: "Identity",
                table: "UserEngagementSnapshots",
                column: "CalculatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserEngagementSnapshots_LastActivityAtUtc",
                schema: "Identity",
                table: "UserEngagementSnapshots",
                column: "LastActivityAtUtc");

            migrationBuilder.CreateIndex(
                name: "UX_UserEngagementSnapshots_UserId",
                schema: "Identity",
                table: "UserEngagementSnapshots",
                column: "UserId",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_Provider_ProviderKey",
                schema: "Identity",
                table: "UserLogins",
                columns: new[] { "Provider", "ProviderKey" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                schema: "Identity",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId_Provider",
                schema: "Identity",
                table: "UserLogins",
                columns: new[] { "UserId", "Provider" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "Identity",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UX_UserRole_User_Role",
                schema: "Identity",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_User_NormalizedEmail",
                schema: "Identity",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UX_User_NormalizedUserName",
                schema: "Identity",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_ExpiresAtUtc",
                schema: "Identity",
                table: "UserTokens",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_UserTokens_UserId_Purpose",
                schema: "Identity",
                table: "UserTokens",
                columns: new[] { "UserId", "Purpose" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_UserTwoFactorSecrets_UserId",
                schema: "Identity",
                table: "UserTwoFactorSecrets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTwoFactorSecrets_UserId_ActivatedAtUtc",
                schema: "Identity",
                table: "UserTwoFactorSecrets",
                columns: new[] { "UserId", "ActivatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserWebAuthnCredentials_CredentialId",
                schema: "Identity",
                table: "UserWebAuthnCredentials",
                column: "CredentialId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWebAuthnCredentials_UserId_CredentialId",
                schema: "Identity",
                table: "UserWebAuthnCredentials",
                columns: new[] { "UserId", "CredentialId" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_VariantOptionValues_VariantId",
                table: "VariantOptionValues",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BusinessId",
                schema: "Inventory",
                table: "Warehouses",
                column: "BusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BusinessId_IsDefault",
                schema: "Inventory",
                table: "Warehouses",
                columns: new[] { "BusinessId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BusinessId_Name",
                schema: "Inventory",
                table: "Warehouses",
                columns: new[] { "BusinessId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookSubscriptions_EventType_CallbackUrl",
                schema: "Integration",
                table: "WebhookSubscriptions",
                columns: new[] { "EventType", "CallbackUrl" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AddOnGroupBrands",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AddOnGroupCategories",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AddOnGroupProducts",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AddOnGroupTranslations",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AddOnGroupVariants",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AddOnOptionTranslations",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AddOnOptionValueTranslations",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AnalyticsExportFiles",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "BillingPlans",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "BrandTranslations",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "BusinessEngagementStats",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessFavorites",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessInvitations",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessLikes",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessLocations",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessMedias",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessMembers",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessOwnerOverrideAudits",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessReviews",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessStaffQrCodes",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "BusinessSubscriptions",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "CampaignDeliveries",
                schema: "Marketing");

            migrationBuilder.DropTable(
                name: "CartItems",
                schema: "CartCheckout");

            migrationBuilder.DropTable(
                name: "CategoryTranslations",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "ChannelDispatchAudits");

            migrationBuilder.DropTable(
                name: "ChannelDispatchOperations",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "Consents",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "CustomerAddresses",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "CustomerSegmentMemberships",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "EmailDispatchAudits");

            migrationBuilder.DropTable(
                name: "EmailDispatchOperations",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "EventLogs",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "Expenses",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "FinancialAccounts",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "Interactions",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "InventoryTransactions",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "InvoiceLines",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "JournalEntryLines",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "LoyaltyPointsTransactions",
                schema: "Loyalty");

            migrationBuilder.DropTable(
                name: "LoyaltyRewardRedemptions",
                schema: "Loyalty");

            migrationBuilder.DropTable(
                name: "LoyaltyRewardTiers",
                schema: "Loyalty");

            migrationBuilder.DropTable(
                name: "MediaAssets",
                schema: "CMS");

            migrationBuilder.DropTable(
                name: "MenuItemTranslations",
                schema: "CMS");

            migrationBuilder.DropTable(
                name: "OpportunityItems",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "OrderLines",
                schema: "Orders");

            migrationBuilder.DropTable(
                name: "PageTranslations",
                schema: "CMS");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "ProductMedia");

            migrationBuilder.DropTable(
                name: "ProductOptionValues");

            migrationBuilder.DropTable(
                name: "ProductTranslations",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "PromotionRedemptions",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "Promotions",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ProviderCallbackInboxMessages",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "PurchaseOrderLines",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "QrCodeTokens",
                schema: "Loyalty");

            migrationBuilder.DropTable(
                name: "RedirectRules",
                schema: "SEO");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "ScanSessions",
                schema: "Loyalty");

            migrationBuilder.DropTable(
                name: "ShipmentCarrierEvents");

            migrationBuilder.DropTable(
                name: "ShipmentLines");

            migrationBuilder.DropTable(
                name: "ShipmentProviderOperations",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "ShippingRates");

            migrationBuilder.DropTable(
                name: "SiteSettings",
                schema: "Settings");

            migrationBuilder.DropTable(
                name: "StockLevels",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "StockTransferLines",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "SubscriptionInvoices",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "TaxCategories",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "UserDevices",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserEngagementSnapshots",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserLogins",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserTokens",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserTwoFactorSecrets",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserWebAuthnCredentials",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "VariantOptionValues");

            migrationBuilder.DropTable(
                name: "WebhookDeliveries",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "AddOnOptionValues",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AnalyticsExportJobs",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "Brands",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "Campaigns",
                schema: "Marketing");

            migrationBuilder.DropTable(
                name: "Carts",
                schema: "CartCheckout");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "Addresses",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "CustomerSegments",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "Leads",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "JournalEntries",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "LoyaltyAccounts",
                schema: "Loyalty");

            migrationBuilder.DropTable(
                name: "LoyaltyPrograms",
                schema: "Loyalty");

            migrationBuilder.DropTable(
                name: "MenuItems",
                schema: "CMS");

            migrationBuilder.DropTable(
                name: "Opportunities",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "Pages",
                schema: "CMS");

            migrationBuilder.DropTable(
                name: "ProductOptions",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Shipments");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "StockTransfers",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "ProductVariants",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AddOnOptions",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "Businesses",
                schema: "Businesses");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "Menus",
                schema: "CMS");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "CRM");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "Inventory");

            migrationBuilder.DropTable(
                name: "Products",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "AddOnGroups",
                schema: "Catalog");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "Orders");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "ShippingMethods",
                schema: "Shipping");
        }
    }
}
