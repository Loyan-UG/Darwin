using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.SEO.DTOs;
using Darwin.Application.SEO.Queries;
using Darwin.Domain.Entities.SEO;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.SEO;

/// <summary>
/// Tests for <see cref="GetRedirectRulesPageHandler"/> covering paging,
/// ordering, exclusion of soft-deleted rules, and boundary clamping.
/// </summary>
public sealed class GetRedirectRulesPageHandlerTests
{
    // ─── Shared DbContext ─────────────────────────────────────────────────────

    private sealed class SeoPageDbContext : DbContext, IAppDbContext
    {
        private SeoPageDbContext(DbContextOptions<SeoPageDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static SeoPageDbContext Create()
        {
            var options = new DbContextOptionsBuilder<SeoPageDbContext>()
                .UseInMemoryDatabase($"darwin_seo_page_tests_{Guid.NewGuid()}")
                .Options;
            return new SeoPageDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RedirectRule>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.FromPath).HasMaxLength(2048).IsRequired();
                b.Property(x => x.To).HasMaxLength(2048).IsRequired();
                b.Property(x => x.IsPermanent);
                b.Property(x => x.IsDeleted);
                b.Property(x => x.RowVersion).IsRowVersion();
            });
        }
    }

    private static RedirectRule MakeRule(string from, string to, bool isDeleted = false, DateTime? modifiedAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            FromPath = from,
            To = to,
            IsPermanent = false,
            IsDeleted = isDeleted,
            ModifiedAtUtc = modifiedAt
        };

    // ─── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPage_Should_Return_Empty_When_No_Rules()
    {
        await using var db = SeoPageDbContext.Create();
        var handler = new GetRedirectRulesPageHandler(db);

        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        items.Should().BeEmpty();
        total.Should().Be(0);
    }

    [Fact]
    public async Task GetPage_Should_Return_All_Non_Deleted_Rules()
    {
        await using var db = SeoPageDbContext.Create();
        db.Set<RedirectRule>().AddRange(
            MakeRule("/a", "/b"),
            MakeRule("/c", "/d"),
            MakeRule("/e", "/f", isDeleted: true));  // soft-deleted, should be excluded
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRedirectRulesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        total.Should().Be(2, "soft-deleted rules must not be counted");
        items.Should().HaveCount(2);
        items.Should().NotContain(r => r.FromPath == "/e");
    }

    [Fact]
    public async Task GetPage_Should_Exclude_Soft_Deleted_Rules()
    {
        await using var db = SeoPageDbContext.Create();
        db.Set<RedirectRule>().Add(MakeRule("/deleted", "/target", isDeleted: true));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRedirectRulesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        total.Should().Be(0, "soft-deleted rules must be hidden from the listing");
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPage_Should_Respect_PageSize_Limit()
    {
        await using var db = SeoPageDbContext.Create();
        for (var i = 1; i <= 10; i++)
            db.Set<RedirectRule>().Add(MakeRule($"/path-{i}", $"/target-{i}"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRedirectRulesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 3, TestContext.Current.CancellationToken);

        total.Should().Be(10);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPage_Should_Return_Second_Page_Correctly()
    {
        await using var db = SeoPageDbContext.Create();
        for (var i = 1; i <= 5; i++)
            db.Set<RedirectRule>().Add(MakeRule($"/path-{i}", $"/target-{i}",
                modifiedAt: new DateTime(2024, 1, i, 0, 0, 0, DateTimeKind.Utc)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRedirectRulesPageHandler(db);
        var (page1Items, _) = await handler.HandleAsync(1, 2, TestContext.Current.CancellationToken);
        var (page2Items, _) = await handler.HandleAsync(2, 2, TestContext.Current.CancellationToken);

        page1Items.Should().HaveCount(2);
        page2Items.Should().HaveCount(2);
        page1Items.Select(i => i.FromPath).Should().NotIntersectWith(page2Items.Select(i => i.FromPath),
            "page 1 and page 2 should have different items");
    }

    [Fact]
    public async Task GetPage_Should_Clamp_Invalid_Page_To_One()
    {
        await using var db = SeoPageDbContext.Create();
        db.Set<RedirectRule>().Add(MakeRule("/x", "/y"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRedirectRulesPageHandler(db);
        var (items, total) = await handler.HandleAsync(-5, 20, TestContext.Current.CancellationToken);

        total.Should().Be(1, "invalid page number should be clamped to 1");
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPage_Should_Clamp_Invalid_PageSize_To_Default()
    {
        await using var db = SeoPageDbContext.Create();
        db.Set<RedirectRule>().Add(MakeRule("/x", "/y"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRedirectRulesPageHandler(db);
        var (items, total) = await handler.HandleAsync(1, 0, TestContext.Current.CancellationToken);

        total.Should().Be(1, "invalid pageSize should be clamped to default");
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPage_Should_Map_All_Dto_Fields()
    {
        await using var db = SeoPageDbContext.Create();
        var modifiedAt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        db.Set<RedirectRule>().Add(new RedirectRule
        {
            Id = Guid.NewGuid(),
            FromPath = "/from",
            To = "/to",
            IsPermanent = true,
            IsDeleted = false,
            ModifiedAtUtc = modifiedAt
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetRedirectRulesPageHandler(db);
        var (items, _) = await handler.HandleAsync(1, 20, TestContext.Current.CancellationToken);

        var item = items.Single();
        item.FromPath.Should().Be("/from");
        item.To.Should().Be("/to");
        item.IsPermanent.Should().BeTrue();
        item.ModifiedAtUtc.Should().Be(modifiedAt);
    }
}
