using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Profile;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Profile;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Provides member self-service address-book management for the consumer app.
/// </summary>
public sealed class MemberAddressesViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;
    private bool _isLoaded;
    private Guid _editingAddressId;
    private byte[] _editingRowVersion = Array.Empty<byte>();
    private bool _isEditingExisting;
    private string? _successMessage;
    private string _fullName = string.Empty;
    private string? _company;
    private string _street1 = string.Empty;
    private string? _street2;
    private string _postalCode = string.Empty;
    private string _city = string.Empty;
    private string? _state;
    private string _countryCode = ProfileContractDefaults.DefaultCountryCode;
    private string? _phoneE164;
    private bool _isDefaultBilling;
    private bool _isDefaultShipping;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberAddressesViewModel"/> class.
    /// </summary>
    public MemberAddressesViewModel(IProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
        StartCreateCommand = new AsyncCommand(StartCreateAsync, () => !IsBusy);
        SaveCommand = new AsyncCommand(SaveAsync, () => !IsBusy);
        CancelEditCommand = new AsyncCommand(CancelEditAsync, () => !IsBusy);
        EditCommand = new AsyncCommand<MemberAddressItemViewModel>(EditAsync, item => !IsBusy && item is not null);
        DeleteCommand = new AsyncCommand<MemberAddressItemViewModel>(DeleteAsync, item => !IsBusy && item is not null);
        SetDefaultBillingCommand = new AsyncCommand<MemberAddressItemViewModel>(SetDefaultBillingAsync, item => !IsBusy && item is not null);
        SetDefaultShippingCommand = new AsyncCommand<MemberAddressItemViewModel>(SetDefaultShippingAsync, item => !IsBusy && item is not null);
    }

    /// <summary>Gets the current address items.</summary>
    public ObservableCollection<MemberAddressItemViewModel> Addresses { get; } = new();

    /// <summary>Gets the refresh command.</summary>
    public AsyncCommand RefreshCommand { get; }

    /// <summary>Gets the command that clears the editor for address creation.</summary>
    public AsyncCommand StartCreateCommand { get; }

    /// <summary>Gets the save command.</summary>
    public AsyncCommand SaveCommand { get; }

    /// <summary>Gets the cancel-edit command.</summary>
    public AsyncCommand CancelEditCommand { get; }

    /// <summary>Gets the edit command.</summary>
    public AsyncCommand<MemberAddressItemViewModel> EditCommand { get; }

    /// <summary>Gets the delete command.</summary>
    public AsyncCommand<MemberAddressItemViewModel> DeleteCommand { get; }

    /// <summary>Gets the default-billing command.</summary>
    public AsyncCommand<MemberAddressItemViewModel> SetDefaultBillingCommand { get; }

    /// <summary>Gets the default-shipping command.</summary>
    public AsyncCommand<MemberAddressItemViewModel> SetDefaultShippingCommand { get; }

    /// <summary>Gets the editor header text.</summary>
    public string EditorTitle => IsEditingExisting
        ? AppResources.MemberAddressesEditorTitleEdit
        : AppResources.MemberAddressesEditorTitleCreate;

    /// <summary>Gets a value indicating whether any saved addresses exist.</summary>
    public bool HasAddresses => Addresses.Count > 0;

    /// <summary>Gets a value indicating whether a success message is present.</summary>
    public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);

    /// <summary>Gets or sets the success message.</summary>
    public string? SuccessMessage
    {
        get => _successMessage;
        private set
        {
            if (SetProperty(ref _successMessage, value))
            {
                OnPropertyChanged(nameof(HasSuccess));
            }
        }
    }

    /// <summary>Gets or sets a value indicating whether the editor targets an existing address.</summary>
    public bool IsEditingExisting
    {
        get => _isEditingExisting;
        private set
        {
            if (SetProperty(ref _isEditingExisting, value))
            {
                OnPropertyChanged(nameof(EditorTitle));
            }
        }
    }

    /// <summary>Gets or sets the recipient full name.</summary>
    public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
    /// <summary>Gets or sets the optional company name.</summary>
    public string? Company { get => _company; set => SetProperty(ref _company, value); }
    /// <summary>Gets or sets street line one.</summary>
    public string Street1 { get => _street1; set => SetProperty(ref _street1, value); }
    /// <summary>Gets or sets street line two.</summary>
    public string? Street2 { get => _street2; set => SetProperty(ref _street2, value); }
    /// <summary>Gets or sets the postal code.</summary>
    public string PostalCode { get => _postalCode; set => SetProperty(ref _postalCode, value); }
    /// <summary>Gets or sets the city.</summary>
    public string City { get => _city; set => SetProperty(ref _city, value); }
    /// <summary>Gets or sets the optional state.</summary>
    public string? State { get => _state; set => SetProperty(ref _state, value); }
    /// <summary>Gets or sets the ISO country code.</summary>
    public string CountryCode { get => _countryCode; set => SetProperty(ref _countryCode, value); }
    /// <summary>Gets or sets the optional phone number.</summary>
    public string? PhoneE164 { get => _phoneE164; set => SetProperty(ref _phoneE164, value); }
    /// <summary>Gets or sets a value indicating whether this address should be default billing.</summary>
    public bool IsDefaultBilling { get => _isDefaultBilling; set => SetProperty(ref _isDefaultBilling, value); }
    /// <summary>Gets or sets a value indicating whether this address should be default shipping.</summary>
    public bool IsDefaultShipping { get => _isDefaultShipping; set => SetProperty(ref _isDefaultShipping, value); }

    /// <inheritdoc />
    public override async Task OnAppearingAsync()
    {
        if (_isLoaded)
        {
            return;
        }

        await RefreshAsync().ConfigureAwait(false);
        _isLoaded = true;
    }

    private async Task RefreshAsync()
    {
        if (IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        try
        {
            var addresses = await _profileService.GetAddressesAsync(CancellationToken.None).ConfigureAwait(false);
            RunOnMain(() =>
            {
                Addresses.Clear();
                foreach (var address in addresses)
                {
                    Addresses.Add(Map(address));
                }

                OnPropertyChanged(nameof(HasAddresses));
            });
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberAddressesLoadFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCanExecuteChanged();
            });
        }
    }

    private Task StartCreateAsync()
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        ResetEditor();
        return Task.CompletedTask;
    }

    private Task CancelEditAsync()
    {
        if (IsBusy)
        {
            return Task.CompletedTask;
        }

        ResetEditor();
        return Task.CompletedTask;
    }

    private Task EditAsync(MemberAddressItemViewModel? item)
    {
        if (item is null || IsBusy)
        {
            return Task.CompletedTask;
        }

        _editingAddressId = item.Id;
        _editingRowVersion = item.RowVersion;
        IsEditingExisting = true;
        SuccessMessage = null;
        ErrorMessage = null;
        FullName = item.FullName;
        Company = item.Company;
        Street1 = item.Street1;
        Street2 = item.Street2;
        PostalCode = item.PostalCode;
        City = item.City;
        State = item.State;
        CountryCode = item.CountryCode;
        PhoneE164 = item.PhoneE164;
        IsDefaultBilling = item.IsDefaultBilling;
        IsDefaultShipping = item.IsDefaultShipping;
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (!ValidateForm())
        {
            RunOnMain(() => ErrorMessage = AppResources.MemberAddressesValidationFailed);
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        try
        {
            if (IsEditingExisting)
            {
                var update = new UpdateMemberAddressRequest
                {
                    FullName = FullName.Trim(),
                    Company = Normalize(Company),
                    Street1 = Street1.Trim(),
                    Street2 = Normalize(Street2),
                    PostalCode = PostalCode.Trim(),
                    City = City.Trim(),
                    State = Normalize(State),
                    CountryCode = CountryCode.Trim().ToUpperInvariant(),
                    PhoneE164 = Normalize(PhoneE164),
                    IsDefaultBilling = IsDefaultBilling,
                    IsDefaultShipping = IsDefaultShipping,
                    RowVersion = _editingRowVersion
                };

                var result = await _profileService.UpdateAddressAsync(_editingAddressId, update, CancellationToken.None).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    RunOnMain(() => ErrorMessage = result.Error ?? AppResources.MemberAddressesSaveFailed);
                    return;
                }

                RunOnMain(() => SuccessMessage = AppResources.MemberAddressesUpdated);
            }
            else
            {
                var create = new CreateMemberAddressRequest
                {
                    FullName = FullName.Trim(),
                    Company = Normalize(Company),
                    Street1 = Street1.Trim(),
                    Street2 = Normalize(Street2),
                    PostalCode = PostalCode.Trim(),
                    City = City.Trim(),
                    State = Normalize(State),
                    CountryCode = CountryCode.Trim().ToUpperInvariant(),
                    PhoneE164 = Normalize(PhoneE164),
                    IsDefaultBilling = IsDefaultBilling,
                    IsDefaultShipping = IsDefaultShipping
                };

                var result = await _profileService.CreateAddressAsync(create, CancellationToken.None).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    RunOnMain(() => ErrorMessage = result.Error ?? AppResources.MemberAddressesSaveFailed);
                    return;
                }

                RunOnMain(() => SuccessMessage = AppResources.MemberAddressesCreated);
            }

            await ReloadAndResetAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberAddressesSaveFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCanExecuteChanged();
            });
        }
    }

    private async Task DeleteAsync(MemberAddressItemViewModel? item)
    {
        if (item is null || IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        try
        {
            var result = await _profileService.DeleteAddressAsync(
                item.Id,
                new DeleteMemberAddressRequest { RowVersion = item.RowVersion },
                CancellationToken.None).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? AppResources.MemberAddressesDeleteFailed);
                return;
            }

            RunOnMain(() => SuccessMessage = AppResources.MemberAddressesDeleted);
            await ReloadAndResetAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberAddressesDeleteFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCanExecuteChanged();
            });
        }
    }

    private Task SetDefaultBillingAsync(MemberAddressItemViewModel? item)
        => SetDefaultAsync(item, asBilling: true, asShipping: false, AppResources.MemberAddressesDefaultUpdated);

    private Task SetDefaultShippingAsync(MemberAddressItemViewModel? item)
        => SetDefaultAsync(item, asBilling: false, asShipping: true, AppResources.MemberAddressesDefaultUpdated);

    private async Task SetDefaultAsync(MemberAddressItemViewModel? item, bool asBilling, bool asShipping, string successMessage)
    {
        if (item is null || IsBusy)
        {
            return;
        }

        RunOnMain(() =>
        {
            IsBusy = true;
            ErrorMessage = null;
            SuccessMessage = null;
        });

        try
        {
            var result = await _profileService.SetDefaultAddressAsync(
                item.Id,
                new SetMemberDefaultAddressRequest
                {
                    AsBilling = asBilling,
                    AsShipping = asShipping
                },
                CancellationToken.None).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                RunOnMain(() => ErrorMessage = result.Error ?? AppResources.MemberAddressesDefaultFailed);
                return;
            }

            RunOnMain(() => SuccessMessage = successMessage);
            await ReloadAddressesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            RunOnMain(() => ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberAddressesDefaultFailed));
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RaiseCanExecuteChanged();
            });
        }
    }

    private async Task ReloadAndResetAsync()
    {
        await ReloadAddressesAsync().ConfigureAwait(false);
        RunOnMain(ResetEditor);
    }

    private async Task ReloadAddressesAsync()
    {
        var addresses = await _profileService.GetAddressesAsync(CancellationToken.None).ConfigureAwait(false);
        RunOnMain(() =>
        {
            Addresses.Clear();
            foreach (var address in addresses)
            {
                Addresses.Add(Map(address));
            }

            OnPropertyChanged(nameof(HasAddresses));
        });
    }

    private void ResetEditor()
    {
        _editingAddressId = Guid.Empty;
        _editingRowVersion = Array.Empty<byte>();
        IsEditingExisting = false;
        FullName = string.Empty;
        Company = null;
        Street1 = string.Empty;
        Street2 = null;
        PostalCode = string.Empty;
        City = string.Empty;
        State = null;
        CountryCode = ProfileContractDefaults.DefaultCountryCode;
        PhoneE164 = null;
        IsDefaultBilling = false;
        IsDefaultShipping = false;
    }

    private void RaiseCanExecuteChanged()
    {
        RefreshCommand.RaiseCanExecuteChanged();
        StartCreateCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        CancelEditCommand.RaiseCanExecuteChanged();
        EditCommand.RaiseCanExecuteChanged();
        DeleteCommand.RaiseCanExecuteChanged();
        SetDefaultBillingCommand.RaiseCanExecuteChanged();
        SetDefaultShippingCommand.RaiseCanExecuteChanged();
    }

    private bool ValidateForm()
    {
        return !string.IsNullOrWhiteSpace(FullName) &&
               !string.IsNullOrWhiteSpace(Street1) &&
               !string.IsNullOrWhiteSpace(PostalCode) &&
               !string.IsNullOrWhiteSpace(City) &&
               !string.IsNullOrWhiteSpace(CountryCode);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static MemberAddressItemViewModel Map(MemberAddress address)
    {
        var parts = new[] { address.Street1, address.PostalCode, address.City }
            .Where(static part => !string.IsNullOrWhiteSpace(part));

        return new MemberAddressItemViewModel
        {
            Id = address.Id,
            RowVersion = address.RowVersion,
            FullName = address.FullName,
            Company = address.Company,
            Street1 = address.Street1,
            Street2 = address.Street2,
            PostalCode = address.PostalCode,
            City = address.City,
            State = address.State,
            CountryCode = address.CountryCode,
            PhoneE164 = address.PhoneE164,
            IsDefaultBilling = address.IsDefaultBilling,
            IsDefaultShipping = address.IsDefaultShipping,
            Summary = string.Join(", ", parts)
        };
    }
}

/// <summary>
/// Read-only address item for the member address-book screen.
/// </summary>
public sealed class MemberAddressItemViewModel
{
    /// <summary>Gets or sets the address identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the row version.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the full name.</summary>
    public string FullName { get; set; } = string.Empty;
    /// <summary>Gets or sets the company.</summary>
    public string? Company { get; set; }
    /// <summary>Gets or sets street line one.</summary>
    public string Street1 { get; set; } = string.Empty;
    /// <summary>Gets or sets street line two.</summary>
    public string? Street2 { get; set; }
    /// <summary>Gets or sets the postal code.</summary>
    public string PostalCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;
    /// <summary>Gets or sets the state.</summary>
    public string? State { get; set; }
    /// <summary>Gets or sets the country code.</summary>
    public string CountryCode { get; set; } = ProfileContractDefaults.DefaultCountryCode;
    /// <summary>Gets or sets the phone number.</summary>
    public string? PhoneE164 { get; set; }
    /// <summary>Gets or sets a value indicating whether this item is default billing.</summary>
    public bool IsDefaultBilling { get; set; }
    /// <summary>Gets or sets a value indicating whether this item is default shipping.</summary>
    public bool IsDefaultShipping { get; set; }
    /// <summary>Gets or sets the condensed summary line.</summary>
    public string Summary { get; set; } = string.Empty;
    /// <summary>Gets a value indicating whether the company label should be displayed.</summary>
    public bool HasCompany => !string.IsNullOrWhiteSpace(Company);
}
