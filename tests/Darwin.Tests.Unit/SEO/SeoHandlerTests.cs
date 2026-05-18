using System;
using System.Threading.Tasks;
using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.SEO.Commands;
using Darwin.Application.SEO.DTOs;
using Darwin.Application.SEO.Queries;
using Darwin.Domain.Entities.SEO;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;

namespace Darwin.Tests.Unit.SEO;

/// <summary>
/// Handler-level unit tests for the SEO module.
/// Covers <see cref="CreateRedirectRuleHandler"/>, <see cref="UpdateRedirectRuleHandler"/>,
/// <see cref="DeleteRedirectRuleHandler"/>, and <see cref="ResolveRedirectHandler"/>.
/// </summary>
public sealed class SeoHandlerTests
{
    // ─── Shared helpers ──────────────────────────────────────────────────────

    private static IStringLocalizer<ValidationResource> CreateLocalizer()
    {
        var mock = new Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[It.IsAny<string>()])
            .Returns<string>(name => new LocalizedString(name, name));
        mock.Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns<string, object[]>((name, _) => new LocalizedString(name, name));
        return mock.Object;
    }

    // ─── CreateRedirectRuleHandler ────────────────────────────────────────────

    [Fact]
    public async Task CreateRedirectRule_Should_Persist_New_Rule()
    {
        await using var db = SeoTestDbContext.Create();
        var handler = new CreateRedirectRuleHandler(db, CreateLocalizer());

        await handler.HandleAsync(new RedirectRuleCreateDto
        {
            FromPath = "/old-page",
            To = "/new-page",
            IsPermanent = true
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<RedirectRule>().SingleAsync(TestContext.Current.CancellationToken);
        saved.FromPath.Should().Be("/old-page");
        saved.To.Should().Be("/new-page");
        saved.IsPermanent.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRedirectRule_Should_Trim_Whitespace_From_Paths()
    {
        await using var db = SeoTestDbContext.Create();
        var handler = new CreateRedirectRuleHandler(db, CreateLocalizer());

        await handler.HandleAsync(new RedirectRuleCreateDto
        {
            FromPath = "  /spaced-path  ",
            To = "  /target  ",
            IsPermanent = false
        }, TestContext.Current.CancellationToken);

        var saved = await db.Set<RedirectRule>().SingleAsync(TestContext.Current.CancellationToken);
        saved.FromPath.Should().Be("/spaced-path");
        saved.To.Should().Be("/target");
    }

    [Fact]
    public async Task CreateRedirectRule_Should_Throw_ValidationException_When_FromPath_Is_Empty()
    {
        await using var db = SeoTestDbContext.Create();
        var handler = new CreateRedirectRuleHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(new RedirectRuleCreateDto
        {
            FromPath = "",
            To = "/target"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty FromPath violates the format validator");
    }

    [Fact]
    public async Task CreateRedirectRule_Should_Throw_ValidationException_When_FromPath_Already_Exists()
    {
        await using var db = SeoTestDbContext.Create();
        db.Set<RedirectRule>().Add(new RedirectRule
        {
            Id = Guid.NewGuid(),
            FromPath = "/existing",
            To = "/somewhere",
            IsPermanent = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateRedirectRuleHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(new RedirectRuleCreateDto
        {
            FromPath = "/existing",
            To = "/other-target"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("duplicate FromPath must be rejected by the uniqueness validator");
    }

    [Fact]
    public async Task CreateRedirectRule_Should_Allow_Same_FromPath_When_Previous_Rule_Is_Deleted()
    {
        await using var db = SeoTestDbContext.Create();
        db.Set<RedirectRule>().Add(new RedirectRule
        {
            Id = Guid.NewGuid(),
            FromPath = "/reused-path",
            To = "/old-target",
            IsPermanent = true,
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new CreateRedirectRuleHandler(db, CreateLocalizer());

        await handler.HandleAsync(new RedirectRuleCreateDto
        {
            FromPath = "/reused-path",
            To = "/new-target",
            IsPermanent = false
        }, TestContext.Current.CancellationToken);

        var active = await db.Set<RedirectRule>()
            .CountAsync(r => !r.IsDeleted, TestContext.Current.CancellationToken);
        active.Should().Be(1, "the newly created rule should be the only active entry");
    }

    // ─── UpdateRedirectRuleHandler ────────────────────────────────────────────

    [Fact]
    public async Task UpdateRedirectRule_Should_Persist_Changes()
    {
        await using var db = SeoTestDbContext.Create();
        var id = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3, 4 };

        db.Set<RedirectRule>().Add(new RedirectRule
        {
            Id = id,
            FromPath = "/original",
            To = "/original-target",
            IsPermanent = true,
            RowVersion = rowVersion
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateRedirectRuleHandler(db, CreateLocalizer());

        await handler.HandleAsync(new RedirectRuleEditDto
        {
            Id = id,
            RowVersion = rowVersion,
            FromPath = "/updated",
            To = "/updated-target",
            IsPermanent = false
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<RedirectRule>().FindAsync([id], TestContext.Current.CancellationToken);
        updated!.FromPath.Should().Be("/updated");
        updated.To.Should().Be("/updated-target");
        updated.IsPermanent.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateRedirectRule_Should_Throw_When_Rule_Not_Found()
    {
        await using var db = SeoTestDbContext.Create();
        var handler = new UpdateRedirectRuleHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(new RedirectRuleEditDto
        {
            Id = Guid.NewGuid(),
            RowVersion = new byte[] { 1 },
            FromPath = "/any",
            To = "/target"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("a non-existent rule cannot be updated");
    }

    [Fact]
    public async Task UpdateRedirectRule_Should_Throw_On_Concurrency_Conflict()
    {
        await using var db = SeoTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<RedirectRule>().Add(new RedirectRule
        {
            Id = id,
            FromPath = "/path",
            To = "/target",
            RowVersion = new byte[] { 1, 2, 3 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateRedirectRuleHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(new RedirectRuleEditDto
        {
            Id = id,
            RowVersion = new byte[] { 9, 9, 9 },  // stale version
            FromPath = "/path",
            To = "/other"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("a RowVersion mismatch must raise a concurrency conflict");
    }

    [Fact]
    public async Task UpdateRedirectRule_Should_Throw_DbUpdateConcurrencyException_When_RowVersion_Is_Empty()
    {
        await using var db = SeoTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<RedirectRule>().Add(new RedirectRule
        {
            Id = id,
            FromPath = "/path",
            To = "/target",
            RowVersion = new byte[] { 1, 2, 3 }
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateRedirectRuleHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(new RedirectRuleEditDto
        {
            Id = id,
            RowVersion = Array.Empty<byte>(), // empty RowVersion
            FromPath = "/path",
            To = "/other"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>("an empty RowVersion must be treated as a concurrency conflict");
    }

    [Fact]
    public async Task UpdateRedirectRule_Should_Throw_ValidationException_When_Dto_Invalid()
    {
        await using var db = SeoTestDbContext.Create();
        var handler = new UpdateRedirectRuleHandler(db, CreateLocalizer());

        var act = () => handler.HandleAsync(new RedirectRuleEditDto
        {
            Id = Guid.Empty,   // invalid: empty Id
            RowVersion = new byte[] { 1 },
            FromPath = "/path",
            To = "/target"
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("an empty Id violates the edit format validator");
    }

    // ─── DeleteRedirectRuleHandler ────────────────────────────────────────────

    [Fact]
    public async Task DeleteRedirectRule_Should_Soft_Delete_Existing_Rule()
    {
        await using var db = SeoTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<RedirectRule>().Add(new RedirectRule
        {
            Id = id,
            FromPath = "/to-delete",
            To = "/somewhere"
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var entity = await db.Set<RedirectRule>().FindAsync([id], TestContext.Current.CancellationToken);
        var fakeRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        entity!.RowVersion = fakeRowVersion;
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteRedirectRuleHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(id, fakeRowVersion, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue();
        entity = await db.Set<RedirectRule>().FindAsync([id], TestContext.Current.CancellationToken);
        entity!.IsDeleted.Should().BeTrue("soft delete should set the IsDeleted flag");
    }

    [Fact]
    public async Task DeleteRedirectRule_Should_Be_Idempotent_When_Rule_Not_Found()
    {
        await using var db = SeoTestDbContext.Create();
        var handler = new DeleteRedirectRuleHandler(db, CreateLocalizer());

        // Should not throw even when the rule does not exist.
        var act = () => handler.HandleAsync(Guid.NewGuid(), null, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync("deleting a non-existent rule should be silently ignored");
    }

    [Fact]
    public async Task DeleteRedirectRule_Should_Not_Delete_Already_Deleted_Rule()
    {
        await using var db = SeoTestDbContext.Create();
        var id = Guid.NewGuid();
        db.Set<RedirectRule>().Add(new RedirectRule
        {
            Id = id,
            FromPath = "/already-deleted",
            To = "/target",
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteRedirectRuleHandler(db, CreateLocalizer());
        // Should complete without error; the entity was already soft-deleted.
        await handler.HandleAsync(id, null, TestContext.Current.CancellationToken);

        var count = await db.Set<RedirectRule>().CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(1, "the entity should remain in the store");
    }

    [Fact]
    public async Task DeleteRedirectRule_Should_Fail_WhenIdIsEmpty()
    {
        await using var db = SeoTestDbContext.Create();
        var handler = new DeleteRedirectRuleHandler(db, CreateLocalizer());

        var result = await handler.HandleAsync(Guid.Empty, new byte[] { 1, 2, 3 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Guid is not a valid redirect rule identifier");
    }

    [Fact]
    public async Task DeleteRedirectRule_Should_Fail_WhenRowVersionIsNull_ForExistingRule()
    {
        await using var db = SeoTestDbContext.Create();
        var id = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };
        db.Set<RedirectRule>().Add(new RedirectRule { Id = id, FromPath = "/from", To = "/to", RowVersion = rowVersion });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteRedirectRuleHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(id, null, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("null RowVersion must be rejected");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteRedirectRule_Should_Fail_WhenRowVersionIsEmpty_ForExistingRule()
    {
        await using var db = SeoTestDbContext.Create();
        var id = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };
        db.Set<RedirectRule>().Add(new RedirectRule { Id = id, FromPath = "/from", To = "/to", RowVersion = rowVersion });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteRedirectRuleHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(id, Array.Empty<byte>(), TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must be rejected");
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteRedirectRule_Should_Fail_WhenRowVersionIsStale()
    {
        await using var db = SeoTestDbContext.Create();
        var id = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };
        db.Set<RedirectRule>().Add(new RedirectRule { Id = id, FromPath = "/from", To = "/to", RowVersion = rowVersion });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteRedirectRuleHandler(db, CreateLocalizer());
        var result = await handler.HandleAsync(id, new byte[] { 9, 9, 9 }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("stale RowVersion must trigger a concurrency conflict");
        result.Error.Should().NotBeNullOrEmpty();
    }

    // ─── ResolveRedirectHandler ───────────────────────────────────────────────

    [Fact]
    public async Task ResolveRedirect_Should_Return_Result_When_Rule_Matches()
    {
        await using var db = SeoTestDbContext.Create();
        db.Set<RedirectRule>().Add(new RedirectRule
        {
            Id = Guid.NewGuid(),
            FromPath = "/old",
            To = "/new",
            IsPermanent = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ResolveRedirectHandler(db);
        var result = await handler.HandleAsync("/old", TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.To.Should().Be("/new");
        result.IsPermanent.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveRedirect_Should_Return_Null_When_No_Rule_Matches()
    {
        await using var db = SeoTestDbContext.Create();
        var handler = new ResolveRedirectHandler(db);

        var result = await handler.HandleAsync("/non-existent", TestContext.Current.CancellationToken);

        result.Should().BeNull("no matching rule means no redirect");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ResolveRedirect_Should_Return_Null_For_Blank_Or_Null_Path(string? path)
    {
        await using var db = SeoTestDbContext.Create();
        var handler = new ResolveRedirectHandler(db);

        var result = await handler.HandleAsync(path!, TestContext.Current.CancellationToken);

        result.Should().BeNull("blank or null paths cannot match any stored rule");
    }

    [Fact]
    public async Task ResolveRedirect_Should_Not_Return_Soft_Deleted_Rules()
    {
        await using var db = SeoTestDbContext.Create();
        db.Set<RedirectRule>().Add(new RedirectRule
        {
            Id = Guid.NewGuid(),
            FromPath = "/deleted-rule",
            To = "/target",
            IsPermanent = true,
            IsDeleted = true
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ResolveRedirectHandler(db);
        var result = await handler.HandleAsync("/deleted-rule", TestContext.Current.CancellationToken);

        result.Should().BeNull("soft-deleted rules must not be resolved");
    }

    // ─── Shared DbContext ─────────────────────────────────────────────────────

    private sealed class SeoTestDbContext : DbContext, IAppDbContext
    {
        private SeoTestDbContext(DbContextOptions<SeoTestDbContext> options) : base(options) { }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static SeoTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<SeoTestDbContext>()
                .UseInMemoryDatabase($"darwin_seo_tests_{Guid.NewGuid()}")
                .Options;
            return new SeoTestDbContext(options);
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
}
