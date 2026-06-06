using System.Collections.ObjectModel;
using Darwin.Contracts.Businesses;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.ViewModels;
using Microsoft.Maui.Storage;

namespace Darwin.Mobile.Business.ViewModels;

public sealed class BusinessMediaViewModel : BaseViewModel
{
    private readonly IBusinessMediaService _mediaService;
    private string? _profileImageUrl;
    private bool _loadedOnce;

    public BusinessMediaViewModel(IBusinessMediaService mediaService)
    {
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        Gallery = new ObservableCollection<BusinessMediaItem>();
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        UploadProfileImageCommand = new AsyncCommand(UploadProfileImageAsync, () => !IsBusy);
        AddGalleryImageCommand = new AsyncCommand(AddGalleryImageAsync, () => !IsBusy && Gallery.Count < 10);
        SetPrimaryCommand = new AsyncCommand<BusinessMediaItem>(SetPrimaryAsync, item => !IsBusy && item is not null && !item.IsPrimary);
        DeleteImageCommand = new AsyncCommand<BusinessMediaItem>(DeleteImageAsync, item => !IsBusy && item is not null && Gallery.Count > 1);
    }

    public ObservableCollection<BusinessMediaItem> Gallery { get; }

    public string? ProfileImageUrl
    {
        get => _profileImageUrl;
        private set => SetProperty(ref _profileImageUrl, value);
    }

    public bool HasProfileImage => !string.IsNullOrWhiteSpace(ProfileImageUrl);
    public bool CanAddGalleryImage => Gallery.Count < 10;

    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand UploadProfileImageCommand { get; }
    public AsyncCommand AddGalleryImageCommand { get; }
    public AsyncCommand<BusinessMediaItem> SetPrimaryCommand { get; }
    public AsyncCommand<BusinessMediaItem> DeleteImageCommand { get; }

    public override async Task OnAppearingAsync()
    {
        if (_loadedOnce)
        {
            return;
        }

        _loadedOnce = true;
        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        BeginBusy();
        try
        {
            var result = await _mediaService.GetAsync(CancellationToken.None).ConfigureAwait(false);
            if (!result.Succeeded || result.Value is null)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? "Unable to load business media.");
                return;
            }

            RunOnMain(() =>
            {
                ProfileImageUrl = result.Value.ProfileImageUrl;
                Gallery.Clear();
                foreach (var item in result.Value.Gallery.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.SortOrder))
                {
                    Gallery.Add(item);
                }

                OnPropertyChanged(nameof(HasProfileImage));
                OnPropertyChanged(nameof(CanAddGalleryImage));
                RaiseCommandStates();
            });
        }
        finally
        {
            EndBusy();
        }
    }

    private async Task UploadProfileImageAsync()
    {
        var url = await PickAndUploadAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        BeginBusy();
        try
        {
            var result = await _mediaService.SetProfileImageAsync(url, CancellationToken.None).ConfigureAwait(false);
            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? "Unable to update profile image.");
                return;
            }
        }
        finally
        {
            EndBusy();
        }

        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task AddGalleryImageAsync()
    {
        if (Gallery.Count >= 10)
        {
            RunOnMain(() => ErrorMessage = "A business can have at most 10 gallery images.");
            return;
        }

        var url = await PickAndUploadAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        BeginBusy();
        try
        {
            var result = await _mediaService.CreateGalleryImageAsync(new CreateBusinessMediaRequest
            {
                Url = url,
                IsPrimary = Gallery.Count == 0
            }, CancellationToken.None).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? "Unable to add image.");
                return;
            }
        }
        finally
        {
            EndBusy();
        }

        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task SetPrimaryAsync(BusinessMediaItem? item)
    {
        if (item is null)
        {
            return;
        }

        BeginBusy();
        try
        {
            var result = await _mediaService.UpdateGalleryImageAsync(item.Id, new UpdateBusinessMediaRequest
            {
                BusinessLocationId = item.BusinessLocationId,
                Url = item.Url,
                Caption = item.Caption,
                SortOrder = item.SortOrder,
                IsPrimary = true,
                RowVersion = item.RowVersion
            }, CancellationToken.None).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? "Unable to set main image.");
                return;
            }
        }
        finally
        {
            EndBusy();
        }

        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task DeleteImageAsync(BusinessMediaItem? item)
    {
        if (item is null || Gallery.Count <= 1)
        {
            return;
        }

        BeginBusy();
        try
        {
            var result = await _mediaService.DeleteGalleryImageAsync(item.Id, new DeleteBusinessMediaRequest
            {
                RowVersion = item.RowVersion
            }, CancellationToken.None).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? "Unable to delete image.");
                return;
            }
        }
        finally
        {
            EndBusy();
        }

        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task<string?> PickAndUploadAsync()
    {
        var file = await FilePicker.PickAsync(new PickOptions
        {
            PickerTitle = "Select image",
            FileTypes = FilePickerFileType.Images
        }).ConfigureAwait(false);

        if (file is null)
        {
            return null;
        }

        BeginBusy();
        try
        {
            await using var stream = await file.OpenReadAsync().ConfigureAwait(false);
            var result = await _mediaService.UploadAsync(stream, file.FileName, file.ContentType ?? "application/octet-stream", CancellationToken.None).ConfigureAwait(false);
            if (!result.Succeeded || result.Value is null)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? "Unable to upload image.");
                return null;
            }

            return result.Value.Url;
        }
        finally
        {
            EndBusy();
        }
    }

    private void BeginBusy()
    {
        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            RaiseCommandStates();
        });
    }

    private void EndBusy()
    {
        RunOnMain(() =>
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanAddGalleryImage));
            RaiseCommandStates();
        });
    }

    private void RaiseCommandStates()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        UploadProfileImageCommand.RaiseCanExecuteChanged();
        AddGalleryImageCommand.RaiseCanExecuteChanged();
        SetPrimaryCommand.RaiseCanExecuteChanged();
        DeleteImageCommand.RaiseCanExecuteChanged();
    }
}
