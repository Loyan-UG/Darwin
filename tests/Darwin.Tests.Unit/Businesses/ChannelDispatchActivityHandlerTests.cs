using System;
using System.Threading.Tasks;
using Darwin.Application.Abstractions.Persistence;
using Darwin.Application.Abstractions.Services;
using Darwin.Application.Businesses.DTOs;
using Darwin.Application.Businesses.Queries;
using Darwin.Domain.Common;
using Darwin.Domain.Entities.Integration;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Darwin.Tests.Unit.Businesses;

public sealed class ChannelDispatchActivityHandlerTests
{
    [Fact]
    public async Task HandlePageAsync_Should_Ignore_SoftDeleted_ChannelDispatchAudit_ForAdminTestCooldownWindow()
    {
        var nowUtc = new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var fixedClock = new FixedClock(nowUtc);

        await using var db = ChannelDispatchActivityTestDbContext.Create();
        db.Set<ChannelDispatchAudit>().AddRange(
            new ChannelDispatchAudit
            {
                Id = Guid.NewGuid(),
                Channel = "SMS",
                Provider = "Twilio",
                FlowKey = ChannelDispatchAuditVocabulary.FlowKeys.AdminCommunicationTest,
                TemplateKey = "AdminCommunicationTestSms",
                RecipientAddress = "+49123123123",
                IntendedRecipientAddress = "+49123123123",
                MessagePreview = "deleted admin test message",
                Status = "Sent",
                AttemptedAtUtc = nowUtc.AddMinutes(-1),
                IsDeleted = true,
                RowVersion = [1]
            },
            new ChannelDispatchAudit
            {
                Id = Guid.NewGuid(),
                Channel = "SMS",
                Provider = "Twilio",
                FlowKey = ChannelDispatchAuditVocabulary.FlowKeys.AdminCommunicationTest,
                TemplateKey = "AdminCommunicationTestSms",
                RecipientAddress = "+49123123123",
                IntendedRecipientAddress = "+49123123123",
                MessagePreview = "active admin test message",
                Status = "Sent",
                AttemptedAtUtc = nowUtc.AddMinutes(-10),
                RowVersion = [1]
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new GetChannelDispatchActivityHandler(db, fixedClock);
        var (items, total, summary, _, _) = await handler
            .HandlePageAsync(
                1,
                20,
                new ChannelDispatchAuditFilterDto { AdminTestOnly = true },
                TestContext.Current.CancellationToken);

        total.Should().Be(1);
        items.Should().HaveCount(1);
        items[0].CanRerunNow.Should().BeTrue();
        items[0].ActionPolicyState.Should().Be(ChannelDispatchAuditVocabulary.ActionPolicyStates.Ready);
        summary.ActionReadyCount.Should().Be(1);
        summary.ActionBlockedCount.Should().Be(0);
    }

    private sealed class FixedClock : IClock
    {
        private readonly DateTime _utcNow;

        public FixedClock(DateTime utcNow)
        {
            _utcNow = utcNow;
        }

        public DateTime UtcNow => _utcNow;
    }

    private sealed class ChannelDispatchActivityTestDbContext : DbContext, IAppDbContext
    {
        private ChannelDispatchActivityTestDbContext(DbContextOptions<ChannelDispatchActivityTestDbContext> options)
            : base(options)
        {
        }

        public new DbSet<T> Set<T>() where T : class => base.Set<T>();

        public static ChannelDispatchActivityTestDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ChannelDispatchActivityTestDbContext>()
                .UseInMemoryDatabase($"darwin_channel_dispatch_activity_tests_{Guid.NewGuid()}")
                .Options;
            return new ChannelDispatchActivityTestDbContext(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ChannelDispatchAudit>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Channel).IsRequired();
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.RecipientAddress).IsRequired();
                builder.Property(x => x.MessagePreview).IsRequired();
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });

            modelBuilder.Entity<ChannelDispatchOperation>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Channel).IsRequired();
                builder.Property(x => x.Provider).IsRequired();
                builder.Property(x => x.RecipientAddress).IsRequired();
                builder.Property(x => x.MessageText).IsRequired();
                builder.Property(x => x.Status).IsRequired();
                builder.Property(x => x.RowVersion).IsRequired();
            });
        }
    }
}
