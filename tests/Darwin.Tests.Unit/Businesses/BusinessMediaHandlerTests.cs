using Darwin.Application;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Businesses.Commands;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Validators;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Businesses;
using Darwin.Shared.Results;
using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Darwin.Tests.Unit.Businesses;

/// <summary>
/// Unit tests for <see cref="UpdateBusinessMediaHandler"/> and <see cref="DeleteBusinessMediaHandler"/>,
/// covering RowVersion guard, not-found, normalization, and delete-path behaviors.
/// </summary>
public sealed class BusinessMediaHandlerTests
{
    private static IStringLocalizer<ValidationResource> Loc()
    {
        var mock = new Moq.Mock<IStringLocalizer<ValidationResource>>();
        mock.Setup(l => l[Moq.It.IsAny<string>()])
            .Returns<string>(n => new LocalizedString(n, n));
        mock.Setup(l => l[Moq.It.IsAny<string>(), Moq.It.IsAny<object[]>()])
            .Returns<string, object[]>((n, _) => new LocalizedString(n, n));
        return mock.Object;
    }

    // ─── UpdateBusinessMediaHandler ───────────────────────────────────────────

    [Fact]
    public async Task UpdateBusinessMedia_Should_Throw_WhenIdIsEmpty()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var handler = new UpdateBusinessMediaHandler(db, new BusinessMediaEditDtoValidator(), Loc());

        var act = () => handler.HandleAsync(new BusinessMediaEditDto
        {
            Id = Guid.Empty,
            BusinessId = Guid.NewGuid(),
            Url = "https://cdn.test/img.jpg",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty Id must fail validation");
    }

    [Fact]
    public async Task UpdateBusinessMedia_Should_Throw_WhenRowVersionIsEmpty()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var handler = new UpdateBusinessMediaHandler(db, new BusinessMediaEditDtoValidator(), Loc());

        var act = () => handler.HandleAsync(new BusinessMediaEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Url = "https://cdn.test/img.jpg",
            RowVersion = []   // empty
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("empty RowVersion must fail validation");
    }

    [Fact]
    public async Task UpdateBusinessMedia_Should_Throw_WhenNotFound()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var handler = new UpdateBusinessMediaHandler(db, new BusinessMediaEditDtoValidator(), Loc());

        var act = () => handler.HandleAsync(new BusinessMediaEditDto
        {
            Id = Guid.NewGuid(),
            BusinessId = Guid.NewGuid(),
            Url = "https://cdn.test/img.jpg",
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>("non-existent media item must raise not-found");
    }

    [Fact]
    public async Task UpdateBusinessMedia_Should_Throw_WhenRowVersionIsStale()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var mediaId = Guid.NewGuid();
        db.Set<BusinessMedia>().Add(new BusinessMedia
        {
            Id = mediaId,
            BusinessId = Guid.NewGuid(),
            Url = "https://cdn.test/old.jpg",
            SortOrder = 0,
            RowVersion = [1, 2, 3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessMediaHandler(db, new BusinessMediaEditDtoValidator(), Loc());

        var act = () => handler.HandleAsync(new BusinessMediaEditDto
        {
            Id = mediaId,
            BusinessId = Guid.NewGuid(),
            Url = "https://cdn.test/new.jpg",
            RowVersion = [9, 9, 9]   // stale
        }, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>("stale RowVersion must be rejected as a concurrency conflict");
    }

    [Fact]
    public async Task UpdateBusinessMedia_Should_PersistChanges_WhenRowVersionMatches()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var mediaId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<BusinessMedia>().Add(new BusinessMedia
        {
            Id = mediaId,
            BusinessId = businessId,
            Url = "https://cdn.test/old.jpg",
            Caption = "Old caption",
            SortOrder = 0,
            IsPrimary = false,
            RowVersion = [5]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessMediaHandler(db, new BusinessMediaEditDtoValidator(), Loc());

        await handler.HandleAsync(new BusinessMediaEditDto
        {
            Id = mediaId,
            BusinessId = businessId,
            Url = "  https://cdn.test/new.jpg  ",
            Caption = "  Updated caption  ",
            SortOrder = 3,
            IsPrimary = true,
            RowVersion = [5]
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<BusinessMedia>().SingleAsync(x => x.Id == mediaId, TestContext.Current.CancellationToken);
        updated.Url.Should().Be("https://cdn.test/new.jpg", "Url should be trimmed");
        updated.Caption.Should().Be("Updated caption", "Caption should be trimmed");
        updated.SortOrder.Should().Be(3);
        updated.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBusinessMedia_Should_NullifyCaption_WhenWhitespaceProvided()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var mediaId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        db.Set<BusinessMedia>().Add(new BusinessMedia
        {
            Id = mediaId,
            BusinessId = businessId,
            Url = "https://cdn.test/img.jpg",
            Caption = "Old caption",
            RowVersion = [3]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new UpdateBusinessMediaHandler(db, new BusinessMediaEditDtoValidator(), Loc());

        await handler.HandleAsync(new BusinessMediaEditDto
        {
            Id = mediaId,
            BusinessId = businessId,
            Url = "https://cdn.test/img.jpg",
            Caption = "   ",   // whitespace only
            RowVersion = [3]
        }, TestContext.Current.CancellationToken);

        var updated = await db.Set<BusinessMedia>().SingleAsync(x => x.Id == mediaId, TestContext.Current.CancellationToken);
        updated.Caption.Should().BeNull("whitespace-only caption must be normalized to null");
    }

    // ─── DeleteBusinessMediaHandler ───────────────────────────────────────────

    [Fact]
    public async Task DeleteBusinessMedia_Should_ReturnFail_WhenIdIsEmpty()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var handler = new DeleteBusinessMediaHandler(db, new BusinessMediaDeleteDtoValidator(), Loc());

        var result = await handler.HandleAsync(new BusinessMediaDeleteDto
        {
            Id = Guid.Empty,
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty Id must return failure result");
    }

    [Fact]
    public async Task DeleteBusinessMedia_Should_ReturnFail_WhenRowVersionIsEmpty()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var handler = new DeleteBusinessMediaHandler(db, new BusinessMediaDeleteDtoValidator(), Loc());

        var result = await handler.HandleAsync(new BusinessMediaDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = []   // empty
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("empty RowVersion must return failure result");
    }

    [Fact]
    public async Task DeleteBusinessMedia_Should_ReturnFail_WhenMediaNotFound()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var handler = new DeleteBusinessMediaHandler(db, new BusinessMediaDeleteDtoValidator(), Loc());

        var result = await handler.HandleAsync(new BusinessMediaDeleteDto
        {
            Id = Guid.NewGuid(),
            RowVersion = [1]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeFalse("non-existent media item must return failure result");
    }

    [Fact]
    public async Task DeleteBusinessMedia_Should_RemoveEntity_WhenFound()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var mediaId = Guid.NewGuid();
        db.Set<BusinessMedia>().Add(new BusinessMedia
        {
            Id = mediaId,
            BusinessId = Guid.NewGuid(),
            Url = "https://cdn.test/remove.jpg",
            RowVersion = [2]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteBusinessMediaHandler(db, new BusinessMediaDeleteDtoValidator(), Loc());

        var result = await handler.HandleAsync(new BusinessMediaDeleteDto
        {
            Id = mediaId,
            RowVersion = [2]
        }, TestContext.Current.CancellationToken);

        result.Succeeded.Should().BeTrue("delete with valid RowVersion must succeed");
        var exists = await db.Set<BusinessMedia>().AnyAsync(x => x.Id == mediaId, TestContext.Current.CancellationToken);
        exists.Should().BeFalse("deleted entity must no longer exist in the database");
    }

    [Fact]
    public async Task DeleteBusinessMedia_ByIdOverload_Should_RemoveEntity_WhenFound()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var mediaId = Guid.NewGuid();
        db.Set<BusinessMedia>().Add(new BusinessMedia
        {
            Id = mediaId,
            BusinessId = Guid.NewGuid(),
            Url = "https://cdn.test/remove2.jpg",
            RowVersion = [1]
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new DeleteBusinessMediaHandler(db, new BusinessMediaDeleteDtoValidator(), Loc());

        await handler.HandleAsync(mediaId, TestContext.Current.CancellationToken);

        var exists = await db.Set<BusinessMedia>().AnyAsync(x => x.Id == mediaId, TestContext.Current.CancellationToken);
        exists.Should().BeFalse("id-only delete must remove the entity");
    }

    [Fact]
    public async Task DeleteBusinessMedia_ByIdOverload_Should_BeNoOp_WhenNotFound()
    {
        await using var db = BusinessMediaTestDbContext.Create();
        var handler = new DeleteBusinessMediaHandler(db, new BusinessMediaDeleteDtoValidator(), Loc());

        // Should not throw
        var act = () => handler.HandleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync("id-only delete of non-existent entity must be a silent no-op");
    }

    // ─── Test DbContext ───────────────────────────────────────────────────────

    private sealed class BusinessMediaTestDbContext : DbContext, IAppDbContext
    {
        private BusinessMediaTestDbContext(DbContextOptions<BusinessMediaTestDbContext> opts)
            : base(opts)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static BusinessMediaTestDbContext Create()
        {
            var opts = new DbContextOptionsBuilder<BusinessMediaTestDbContext>()
                .UseInMemoryDatabase($"darwin_business_media_tests_{Guid.NewGuid()}")
                .Options;
            return new BusinessMediaTestDbContext(opts);
        }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
            mb.Ignore<GeoCoordinate>();

            mb.Entity<BusinessMedia>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Url).IsRequired();
                b.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
