using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Notifications;
using Darwin.Mobile.Consumer.Constants;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Collections;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Notifications;
using Darwin.Mobile.Shared.ViewModels;
using Microsoft.Maui.Controls;

namespace Darwin.Mobile.Consumer.ViewModels;

public sealed class NotificationsViewModel : BaseViewModel
{
    private readonly INotificationInboxService _notificationInboxService;
    private NotificationCategoryFilter? _selectedCategory;
    private int _unreadCount;

    public NotificationsViewModel(INotificationInboxService notificationInboxService)
    {
        _notificationInboxService = notificationInboxService ?? throw new ArgumentNullException(nameof(notificationInboxService));

        Items = new RangeObservableCollection<NotificationDisplayItem>();
        Categories = new ObservableCollection<NotificationCategoryFilter>(CreateCategories());
        _selectedCategory = Categories.FirstOrDefault();

        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        MarkAllReadCommand = new AsyncCommand(MarkAllReadAsync, () => !IsBusy && UnreadCount > 0);
        MarkReadCommand = new AsyncCommand<NotificationDisplayItem>(MarkReadAsync, item => !IsBusy && item?.IsUnread == true);
        OpenNotificationCommand = new AsyncCommand<NotificationDisplayItem>(OpenNotificationAsync, item => item is not null);
    }

    public RangeObservableCollection<NotificationDisplayItem> Items { get; }

    public ObservableCollection<NotificationCategoryFilter> Categories { get; }

    public NotificationCategoryFilter? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    public int UnreadCount
    {
        get => _unreadCount;
        private set
        {
            if (SetProperty(ref _unreadCount, Math.Max(0, value)))
            {
                OnPropertyChanged(nameof(HasUnread));
                MarkAllReadCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool HasUnread => UnreadCount > 0;

    public bool HasItems => Items.Count > 0;

    public bool HasNoItems => !HasItems && !IsBusy;

    public AsyncCommand RefreshCommand { get; }

    public AsyncCommand MarkAllReadCommand { get; }

    public AsyncCommand<NotificationDisplayItem> MarkReadCommand { get; }

    public AsyncCommand<NotificationDisplayItem> OpenNotificationCommand { get; }

    public override Task OnAppearingAsync() => RefreshAsync();

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var category = SelectedCategory?.Category;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var response = await _notificationInboxService
                .GetAsync(NotificationInboxTargetApp.Consumer, category, 1, 80, cts.Token)
                .ConfigureAwait(false);

            if (!response.Succeeded || response.Value is null)
            {
                RunOnMain(() => ErrorMessage = AppResources.NotificationsLoadFailed);
                return;
            }

            var items = response.Value.Items
                .Select(NotificationDisplayItem.FromContract)
                .ToList();
            var unreadResult = await _notificationInboxService
                .GetUnreadCountAsync(NotificationInboxTargetApp.Consumer, cts.Token)
                .ConfigureAwait(false);

            RunOnMain(() =>
            {
                Items.ReplaceRange(items);
                UnreadCount = unreadResult.Succeeded ? unreadResult.Value : items.Count(x => x.IsUnread);
                OnPropertyChanged(nameof(HasItems));
                OnPropertyChanged(nameof(HasNoItems));
            });
        }
        catch
        {
            RunOnMain(() => ErrorMessage = AppResources.NotificationsLoadFailed);
        }
        finally
        {
            IsBusy = false;
            RefreshCommand.RaiseCanExecuteChanged();
            MarkReadCommand.RaiseCanExecuteChanged();
            MarkAllReadCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(HasNoItems));
        }
    }

    private async Task MarkReadAsync(NotificationDisplayItem? item)
    {
        if (item is null || !item.IsUnread)
        {
            return;
        }

        var result = await _notificationInboxService
            .MarkReadAsync(item.Id, NotificationInboxTargetApp.Consumer, CancellationToken.None)
            .ConfigureAwait(false);
        if (!result.Succeeded)
        {
            RunOnMain(() => ErrorMessage = AppResources.NotificationsLoadFailed);
            return;
        }

        item.MarkRead(DateTime.UtcNow);
        UnreadCount = result.Value;
    }

    private async Task OpenNotificationAsync(NotificationDisplayItem? item)
    {
        if (item is null)
        {
            return;
        }

        if (item.IsUnread)
        {
            await MarkReadAsync(item).ConfigureAwait(false);
        }

        var route = ResolveRoute(item.DeepLink);
        if (route is null || Shell.Current is null)
        {
            return;
        }

        await Shell.Current.GoToAsync(route);
    }

    private static string? ResolveRoute(string? deepLink)
    {
        if (string.IsNullOrWhiteSpace(deepLink) ||
            !Uri.TryCreate(deepLink.Trim(), UriKind.Absolute, out var uri) ||
            !uri.Scheme.Equals("loyan", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var host = uri.Host.ToLowerInvariant();
        var firstSegment = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return host switch
        {
            "business" when Guid.TryParse(firstSegment, out var businessId) => $"{Routes.BusinessDetail}/{businessId:D}",
            "rewards" => $"//{Routes.Rewards}",
            "qr" => $"//{Routes.Qr}",
            "feed" or "campaign" => $"//{Routes.Feed}",
            _ => null
        };
    }

    private async Task MarkAllReadAsync()
    {
        var result = await _notificationInboxService
            .MarkAllReadAsync(NotificationInboxTargetApp.Consumer, CancellationToken.None)
            .ConfigureAwait(false);
        if (!result.Succeeded)
        {
            ErrorMessage = AppResources.NotificationsLoadFailed;
            return;
        }

        foreach (var item in Items)
        {
            item.MarkRead(DateTime.UtcNow);
        }

        UnreadCount = result.Value;
    }

    private static NotificationCategoryFilter[] CreateCategories() =>
    [
        new(null, AppResources.NotificationsCategoryAll),
        new(NotificationInboxCategory.Campaign, AppResources.NotificationsCategoryCampaign),
        new(NotificationInboxCategory.Reward, AppResources.NotificationsCategoryReward),
        new(NotificationInboxCategory.System, AppResources.NotificationsCategorySystem),
        new(NotificationInboxCategory.Account, AppResources.NotificationsCategoryAccount)
    ];
}

public sealed record NotificationCategoryFilter(NotificationInboxCategory? Category, string Label);

public sealed class NotificationDisplayItem : BaseViewModel
{
    private DateTime? _readAtUtc;

    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? DeepLink { get; init; }
    public string CategoryLabel { get; init; } = string.Empty;
    public string CreatedAtText { get; init; } = string.Empty;

    public bool IsUnread => !_readAtUtc.HasValue;

    public bool IsRead => !IsUnread;

    public static NotificationDisplayItem FromContract(NotificationInboxItem item)
    {
        return new NotificationDisplayItem
        {
            Id = item.Id,
            Title = item.Title,
            Body = item.Body ?? string.Empty,
            DeepLink = item.DeepLink,
            CategoryLabel = GetCategoryLabel(item.Category),
            CreatedAtText = item.CreatedAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
            _readAtUtc = item.ReadAtUtc
        };
    }

    public void MarkRead(DateTime readAtUtc)
    {
        if (_readAtUtc.HasValue)
        {
            return;
        }

        _readAtUtc = readAtUtc;
        OnPropertyChanged(nameof(IsUnread));
        OnPropertyChanged(nameof(IsRead));
    }

    private static string GetCategoryLabel(NotificationInboxCategory category)
        => category switch
        {
            NotificationInboxCategory.Campaign => AppResources.NotificationsCategoryCampaign,
            NotificationInboxCategory.Reward => AppResources.NotificationsCategoryReward,
            NotificationInboxCategory.Billing => AppResources.NotificationsCategoryBilling,
            NotificationInboxCategory.ScannerSession => AppResources.NotificationsCategoryScannerSession,
            NotificationInboxCategory.Account => AppResources.NotificationsCategoryAccount,
            _ => AppResources.NotificationsCategorySystem
        };
}
