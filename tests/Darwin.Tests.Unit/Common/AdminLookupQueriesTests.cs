using System;
using System.Linq;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Common.Queries;
using Darwin.Domain.Entities.Billing;
using Darwin.Domain.Entities.Businesses;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Entities.CMS;
using Darwin.Domain.Entities.CRM;
using Darwin.Domain.Entities.Identity;
using Darwin.Domain.Entities.Inventory;
using Darwin.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Common;

/// <summary>
/// Unit tests for the admin lookup query handlers in <see cref="Darwin.Application.Common.Queries"/>.
/// Covers <see cref="GetBusinessLookupHandler"/>, <see cref="GetUserLookupHandler"/>,
/// <see cref="GetCustomerLookupHandler"/>, <see cref="GetCustomerSegmentLookupHandler"/>,
/// <see cref="GetProductVariantLookupHandler"/>, <see cref="GetSupplierLookupHandler"/>,
/// <see cref="GetFinancialAccountLookupHandler"/>, and <see cref="GetPaymentLookupHandler"/>.
/// </summary>
public sealed class AdminLookupQueriesTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // GetBusinessLookupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetBusinessLookup_Should_Return_Empty_When_No_Businesses()
    {
        await using var db = LookupDbContext.Create();
        var handler = new GetBusinessLookupHandler(db);

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBusinessLookup_Should_Exclude_Deleted_Businesses()
    {
        await using var db = LookupDbContext.Create();
        db.Set<Business>().Add(new Business { Name = "Deleted Co", IsActive = true, IsDeleted = true, DefaultCurrency = "EUR" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBusinessLookup_Should_Exclude_Inactive_Businesses()
    {
        await using var db = LookupDbContext.Create();
        db.Set<Business>().Add(new Business { Name = "Inactive Co", IsActive = false, IsDeleted = false, DefaultCurrency = "EUR" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBusinessLookup_Should_Return_Active_Businesses_Ordered_By_Name()
    {
        await using var db = LookupDbContext.Create();
        db.Set<Business>().AddRange(
            new Business { Name = "Zeta Shop", IsActive = true, DefaultCurrency = "USD" },
            new Business { Name = "Alpha Store", IsActive = true, DefaultCurrency = "EUR" },
            new Business { Name = "Beta Market", IsActive = true, DefaultCurrency = "GBP" }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Select(r => r.Label).Should().ContainInOrder("Alpha Store", "Beta Market", "Zeta Shop");
    }

    [Fact]
    public async Task GetBusinessLookup_Should_Map_Fields_Correctly()
    {
        await using var db = LookupDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<Business>().Add(new Business { Id = id, Name = "Café Aurora", IsActive = true, DefaultCurrency = "EUR" });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetBusinessLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        var item = result.Single();
        item.Id.Should().Be(id);
        item.Label.Should().Be("Café Aurora");
        item.SecondaryLabel.Should().Be("EUR");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetUserLookupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUserLookup_Should_Return_Empty_When_No_Users()
    {
        await using var db = LookupDbContext.Create();
        var handler = new GetUserLookupHandler(db);

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserLookup_Should_Exclude_Deleted_Users()
    {
        await using var db = LookupDbContext.Create();
        db.Set<User>().Add(new User("deleted@example.com", "hash", "stamp") { IsActive = true, IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserLookup_Should_Exclude_Inactive_Users()
    {
        await using var db = LookupDbContext.Create();
        db.Set<User>().Add(new User("inactive@example.com", "hash", "stamp") { IsActive = false, IsDeleted = false });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserLookup_Should_Use_Full_Name_As_Label_When_Available()
    {
        await using var db = LookupDbContext.Create();
        db.Set<User>().Add(new User("john@example.com", "hash", "stamp")
        {
            IsActive = true,
            FirstName = "John",
            LastName = "Doe"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Single().Label.Should().Be("John Doe");
        result.Single().SecondaryLabel.Should().Be("john@example.com");
    }

    [Fact]
    public async Task GetUserLookup_Should_Fall_Back_To_Email_When_Name_Is_Blank()
    {
        await using var db = LookupDbContext.Create();
        db.Set<User>().Add(new User("noname@example.com", "hash", "stamp") { IsActive = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Single().Label.Should().Be("noname@example.com");
    }

    [Fact]
    public async Task GetUserLookup_Should_Return_Users_Ordered_By_Email()
    {
        await using var db = LookupDbContext.Create();
        db.Set<User>().AddRange(
            new User("zulu@example.com", "hash", "stamp") { IsActive = true },
            new User("alpha@example.com", "hash", "stamp") { IsActive = true }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetUserLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Select(r => r.SecondaryLabel).Should().ContainInOrder("alpha@example.com", "zulu@example.com");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetCustomerLookupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCustomerLookup_Should_Return_Empty_When_No_Customers()
    {
        await using var db = LookupDbContext.Create();
        var handler = new GetCustomerLookupHandler(db);

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCustomerLookup_Should_Exclude_Deleted_Customers()
    {
        await using var db = LookupDbContext.Create();
        db.Set<Customer>().Add(new Customer { FirstName = "Jane", LastName = "Deleted", Email = "jane@del.com", IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomerLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCustomerLookup_Should_Use_Customer_Name_When_No_UserId()
    {
        await using var db = LookupDbContext.Create();
        db.Set<Customer>().Add(new Customer
        {
            FirstName = "Maria",
            LastName = "Müller",
            Email = "maria@example.com",
            UserId = null
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomerLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Single().Label.Should().Be("Maria Müller");
        result.Single().SecondaryLabel.Should().Be("maria@example.com");
    }

    [Fact]
    public async Task GetCustomerLookup_Should_Use_User_Identity_Name_When_UserId_Is_Linked()
    {
        await using var db = LookupDbContext.Create();
        var userId = Guid.NewGuid();
        var user = new User("linked@example.com", "hash", "stamp")
        {
            Id = userId,
            IsActive = true,
            FirstName = "Linked",
            LastName = "User"
        };
        db.Set<User>().Add(user);
        db.Set<Customer>().Add(new Customer
        {
            FirstName = "Customer",
            LastName = "Name",
            Email = "customer@old.com",
            UserId = userId
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomerLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Single().Label.Should().Be("Linked User");
        result.Single().SecondaryLabel.Should().Be("linked@example.com");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetCustomerSegmentLookupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCustomerSegmentLookup_Should_Return_Empty_When_No_Segments()
    {
        await using var db = LookupDbContext.Create();
        var handler = new GetCustomerSegmentLookupHandler(db);

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCustomerSegmentLookup_Should_Exclude_Deleted_Segments()
    {
        await using var db = LookupDbContext.Create();
        db.Set<CustomerSegment>().Add(new CustomerSegment { Name = "VIP", IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomerSegmentLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCustomerSegmentLookup_Should_Map_Name_And_Description()
    {
        await using var db = LookupDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<CustomerSegment>().Add(new CustomerSegment
        {
            Id = id,
            Name = "Premium Members",
            Description = "High-value customers",
            IsDeleted = false
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomerSegmentLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        var item = result.Single();
        item.Id.Should().Be(id);
        item.Label.Should().Be("Premium Members");
        item.SecondaryLabel.Should().Be("High-value customers");
    }

    [Fact]
    public async Task GetCustomerSegmentLookup_Should_Return_Segments_Ordered_By_Name()
    {
        await using var db = LookupDbContext.Create();
        db.Set<CustomerSegment>().AddRange(
            new CustomerSegment { Name = "Zeta Tier" },
            new CustomerSegment { Name = "Alpha Tier" }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetCustomerSegmentLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Select(r => r.Label).Should().ContainInOrder("Alpha Tier", "Zeta Tier");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetProductVariantLookupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductVariantLookup_Should_Return_Empty_When_No_Variants()
    {
        await using var db = LookupDbContext.Create();
        var handler = new GetProductVariantLookupHandler(db);

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductVariantLookup_Should_Exclude_Deleted_Variants()
    {
        await using var db = LookupDbContext.Create();
        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Sku = "DEL-001",
            Currency = "EUR",
            IsDeleted = true,
            ProductId = Guid.NewGuid()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductVariantLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductVariantLookup_Should_Include_Sku_And_Product_Name_In_Label()
    {
        await using var db = LookupDbContext.Create();
        var productId = Guid.NewGuid();
        db.Set<ProductTranslation>().Add(new ProductTranslation
        {
            ProductId = productId,
            Name = "Super Widget",
            Culture = "de-DE",
            IsDeleted = false
        });
        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Sku = "SW-001",
            Currency = "EUR",
            IsDeleted = false,
            ProductId = productId
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductVariantLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Single().Label.Should().Be("SW-001 - Super Widget");
        result.Single().SecondaryLabel.Should().Be("EUR");
    }

    [Fact]
    public async Task GetProductVariantLookup_Should_Fall_Back_To_Unnamed_Product_When_No_Translation()
    {
        await using var db = LookupDbContext.Create();
        db.Set<ProductVariant>().Add(new ProductVariant
        {
            Sku = "NOPROD-001",
            Currency = "USD",
            IsDeleted = false,
            ProductId = Guid.NewGuid()
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductVariantLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Single().Label.Should().Be("NOPROD-001 - Unnamed product");
    }

    [Fact]
    public async Task GetProductVariantLookup_Should_Return_Variants_Ordered_By_Sku()
    {
        await using var db = LookupDbContext.Create();
        db.Set<ProductVariant>().AddRange(
            new ProductVariant { Sku = "Z-999", Currency = "EUR", ProductId = Guid.NewGuid() },
            new ProductVariant { Sku = "A-001", Currency = "EUR", ProductId = Guid.NewGuid() }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetProductVariantLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Select(r => r.Label).Should().ContainInOrder("A-001 - Unnamed product", "Z-999 - Unnamed product");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetSupplierLookupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSupplierLookup_Should_Return_Empty_When_No_Suppliers()
    {
        await using var db = LookupDbContext.Create();
        var handler = new GetSupplierLookupHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSupplierLookup_Should_Exclude_Deleted_Suppliers()
    {
        await using var db = LookupDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().Add(new Supplier { Name = "Gone Corp", Email = "gone@corp.com", BusinessId = businessId, IsDeleted = true });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSupplierLookupHandler(db);
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSupplierLookup_Should_Scope_By_BusinessId()
    {
        await using var db = LookupDbContext.Create();
        var myBusiness = Guid.NewGuid();
        var otherBusiness = Guid.NewGuid();
        db.Set<Supplier>().AddRange(
            new Supplier { Name = "My Supplier", Email = "my@sup.com", BusinessId = myBusiness },
            new Supplier { Name = "Other Supplier", Email = "other@sup.com", BusinessId = otherBusiness }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSupplierLookupHandler(db);
        var result = await handler.HandleAsync(myBusiness, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        result.Single().Label.Should().Be("My Supplier");
        result.Single().SecondaryLabel.Should().Be("my@sup.com");
    }

    [Fact]
    public async Task GetSupplierLookup_Should_Return_Suppliers_Ordered_By_Name()
    {
        await using var db = LookupDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<Supplier>().AddRange(
            new Supplier { Name = "Zeta Supply", Email = "z@z.com", BusinessId = businessId },
            new Supplier { Name = "Alpha Supply", Email = "a@a.com", BusinessId = businessId }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetSupplierLookupHandler(db);
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Select(r => r.Label).Should().ContainInOrder("Alpha Supply", "Zeta Supply");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetFinancialAccountLookupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFinancialAccountLookup_Should_Return_Empty_When_No_Accounts()
    {
        await using var db = LookupDbContext.Create();
        var handler = new GetFinancialAccountLookupHandler(db);

        var result = await handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFinancialAccountLookup_Should_Exclude_Deleted_Accounts()
    {
        await using var db = LookupDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<FinancialAccount>().Add(new FinancialAccount
        {
            Name = "Petty Cash",
            Code = "1000",
            BusinessId = businessId,
            Type = AccountType.Asset,
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetFinancialAccountLookupHandler(db);
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFinancialAccountLookup_Should_Scope_By_BusinessId()
    {
        await using var db = LookupDbContext.Create();
        var myBusiness = Guid.NewGuid();
        var otherBusiness = Guid.NewGuid();
        db.Set<FinancialAccount>().AddRange(
            new FinancialAccount { Name = "My Account", Code = "100", BusinessId = myBusiness, Type = AccountType.Asset },
            new FinancialAccount { Name = "Other Account", Code = "200", BusinessId = otherBusiness, Type = AccountType.Liability }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetFinancialAccountLookupHandler(db);
        var result = await handler.HandleAsync(myBusiness, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        result.Single().Label.Should().Be("100 - My Account");
    }

    [Fact]
    public async Task GetFinancialAccountLookup_Should_Use_Just_Name_When_Code_Is_Null()
    {
        await using var db = LookupDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<FinancialAccount>().Add(new FinancialAccount
        {
            Name = "General Revenue",
            Code = null,
            BusinessId = businessId,
            Type = AccountType.Revenue
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetFinancialAccountLookupHandler(db);
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Single().Label.Should().Be("General Revenue");
    }

    [Fact]
    public async Task GetFinancialAccountLookup_Should_Map_AccountType_As_SecondaryLabel()
    {
        await using var db = LookupDbContext.Create();
        var businessId = Guid.NewGuid();
        db.Set<FinancialAccount>().Add(new FinancialAccount
        {
            Name = "Cash Account",
            Code = "101",
            BusinessId = businessId,
            Type = AccountType.Asset
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetFinancialAccountLookupHandler(db);
        var result = await handler.HandleAsync(businessId, TestContext.Current.CancellationToken);

        result.Single().SecondaryLabel.Should().Be("Asset");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GetPaymentLookupHandler
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaymentLookup_Should_Return_Empty_When_No_Payments()
    {
        await using var db = LookupDbContext.Create();
        var handler = new GetPaymentLookupHandler(db);

        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaymentLookup_Should_Exclude_Deleted_Payments()
    {
        await using var db = LookupDbContext.Create();
        db.Set<Payment>().Add(new Payment
        {
            Provider = "Stripe",
            Currency = "EUR",
            AmountMinor = 1000,
            Status = PaymentStatus.Completed,
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPaymentLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaymentLookup_Should_Format_Label_With_Provider_And_Amount()
    {
        await using var db = LookupDbContext.Create();
        db.Set<Payment>().Add(new Payment
        {
            Provider = "Stripe",
            Currency = "EUR",
            AmountMinor = 4999,
            Status = PaymentStatus.Captured,
            IsDeleted = false
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPaymentLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Single().Label.Should().Be("Stripe - EUR 49.99");
        result.Single().SecondaryLabel.Should().Be("Captured");
    }

    [Fact]
    public async Task GetPaymentLookup_Should_Return_All_Non_Deleted_Payments()
    {
        await using var db = LookupDbContext.Create();
        db.Set<Payment>().AddRange(
            new Payment { Provider = "Stripe", Currency = "EUR", AmountMinor = 1000, Status = PaymentStatus.Completed },
            new Payment { Provider = "PayPal", Currency = "USD", AmountMinor = 2000, Status = PaymentStatus.Pending },
            new Payment { Provider = "Deleted", Currency = "GBP", AmountMinor = 500, Status = PaymentStatus.Failed, IsDeleted = true }
        );
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetPaymentLookupHandler(db);
        var result = await handler.HandleAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Test DbContext
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class LookupDbContext : DbContext, IAppDbContext
    {
        private LookupDbContext(DbContextOptions<LookupDbContext> options) : base(options) { }

        public static LookupDbContext Create()
        {
            var options = new DbContextOptionsBuilder<LookupDbContext>()
                .UseInMemoryDatabase($"lookup_test_{Guid.NewGuid()}")
                .Options;
            var ctx = new LookupDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public override Task<int> SaveChangesAsync(System.Threading.CancellationToken ct = default) =>
            base.SaveChangesAsync(ct);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Business ─────────────────────────────────────────────────────
            modelBuilder.Entity<Business>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.DefaultCurrency).IsRequired();
                b.Property(x => x.IsActive);
                b.Property(x => x.IsDeleted);
                // Ignore navigation collections — not needed by the lookup handler
                b.Ignore(x => x.Members);
                b.Ignore(x => x.Locations);
                b.Ignore(x => x.Favorites);
                b.Ignore(x => x.Likes);
                b.Ignore(x => x.Reviews);
                b.Ignore(x => x.Invitations);
                b.Ignore(x => x.StaffQrCodes);
                b.Ignore(x => x.Subscriptions);
                b.Ignore(x => x.AnalyticsExportJobs);
            });

            // ── User ─────────────────────────────────────────────────────────
            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.IsActive);
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.UserRoles);
                b.Ignore(x => x.Logins);
                b.Ignore(x => x.Tokens);
                b.Ignore(x => x.TwoFactorSecrets);
                b.Ignore(x => x.Devices);
                b.Ignore(x => x.BusinessFavorites);
                b.Ignore(x => x.BusinessLikes);
                b.Ignore(x => x.BusinessReviews);
            });

            // ── CRM ───────────────────────────────────────────────────────────
            modelBuilder.Entity<Customer>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FirstName).IsRequired();
                b.Property(x => x.LastName).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.CustomerSegments);
                b.Ignore(x => x.Addresses);
                b.Ignore(x => x.Interactions);
                b.Ignore(x => x.Consents);
                b.Ignore(x => x.Opportunities);
                b.Ignore(x => x.Invoices);
            });

            modelBuilder.Entity<CustomerSegment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.IsDeleted);
                b.Ignore(x => x.Memberships);
            });

            // ── Catalog ───────────────────────────────────────────────────────
            modelBuilder.Entity<ProductVariant>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Sku).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<ProductTranslation>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.ProductId).IsRequired();
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.Culture).IsRequired();
                b.Property(x => x.IsDeleted);
            });

            // ── Inventory ─────────────────────────────────────────────────────
            modelBuilder.Entity<Supplier>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.Email).IsRequired();
                b.Property(x => x.BusinessId).IsRequired();
                b.Property(x => x.IsDeleted);
            });

            // ── Billing ───────────────────────────────────────────────────────
            modelBuilder.Entity<FinancialAccount>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.BusinessId).IsRequired();
                b.Property(x => x.IsDeleted);
            });

            modelBuilder.Entity<Payment>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Provider).IsRequired();
                b.Property(x => x.Currency).IsRequired();
                b.Property(x => x.IsDeleted);
            });
        }
    }
}
