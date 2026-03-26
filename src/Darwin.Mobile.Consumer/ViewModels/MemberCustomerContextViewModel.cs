using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Shared.Commands;
using Darwin.Mobile.Shared.Services.Profile;
using Darwin.Mobile.Shared.ViewModels;

namespace Darwin.Mobile.Consumer.ViewModels;

/// <summary>
/// Displays richer CRM customer context for the current authenticated member.
/// </summary>
public sealed class MemberCustomerContextViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;
    private bool _isLoaded;
    private string _displayName = string.Empty;
    private string _emailText = string.Empty;
    private string? _phoneText;
    private string? _companyText;
    private string? _notesText;
    private string _createdAtText = string.Empty;
    private string _interactionCountText = string.Empty;
    private string? _lastInteractionText;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberCustomerContextViewModel"/> class.
    /// </summary>
    public MemberCustomerContextViewModel(IProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);
    }

    /// <summary>
    /// Gets the refresh command.
    /// </summary>
    public AsyncCommand RefreshCommand { get; }

    /// <summary>
    /// Gets the linked customer display name.
    /// </summary>
    public string DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    /// <summary>
    /// Gets the formatted customer email line.
    /// </summary>
    public string EmailText
    {
        get => _emailText;
        private set => SetProperty(ref _emailText, value);
    }

    /// <summary>
    /// Gets the formatted customer phone line.
    /// </summary>
    public string? PhoneText
    {
        get => _phoneText;
        private set
        {
            if (SetProperty(ref _phoneText, value))
            {
                OnPropertyChanged(nameof(HasPhone));
            }
        }
    }

    /// <summary>
    /// Gets the formatted customer company line.
    /// </summary>
    public string? CompanyText
    {
        get => _companyText;
        private set
        {
            if (SetProperty(ref _companyText, value))
            {
                OnPropertyChanged(nameof(HasCompany));
            }
        }
    }

    /// <summary>
    /// Gets the formatted customer notes line.
    /// </summary>
    public string? NotesText
    {
        get => _notesText;
        private set
        {
            if (SetProperty(ref _notesText, value))
            {
                OnPropertyChanged(nameof(HasNotes));
            }
        }
    }

    /// <summary>
    /// Gets the formatted customer creation timestamp.
    /// </summary>
    public string CreatedAtText
    {
        get => _createdAtText;
        private set => SetProperty(ref _createdAtText, value);
    }

    /// <summary>
    /// Gets the formatted interaction-count summary.
    /// </summary>
    public string InteractionCountText
    {
        get => _interactionCountText;
        private set => SetProperty(ref _interactionCountText, value);
    }

    /// <summary>
    /// Gets the formatted latest-interaction line.
    /// </summary>
    public string? LastInteractionText
    {
        get => _lastInteractionText;
        private set
        {
            if (SetProperty(ref _lastInteractionText, value))
            {
                OnPropertyChanged(nameof(HasLastInteraction));
            }
        }
    }

    /// <summary>
    /// Gets the current segment list.
    /// </summary>
    public ObservableCollection<MemberCustomerSegmentItemViewModel> Segments { get; } = new();

    /// <summary>
    /// Gets the current consent history.
    /// </summary>
    public ObservableCollection<MemberCustomerConsentItemViewModel> Consents { get; } = new();

    /// <summary>
    /// Gets the recent interaction list.
    /// </summary>
    public ObservableCollection<MemberCustomerInteractionItemViewModel> RecentInteractions { get; } = new();

    /// <summary>
    /// Gets a value indicating whether linked customer context is available.
    /// </summary>
    public bool HasContext => !string.IsNullOrWhiteSpace(DisplayName);

    /// <summary>
    /// Gets a value indicating whether a customer phone line is available.
    /// </summary>
    public bool HasPhone => !string.IsNullOrWhiteSpace(PhoneText);

    /// <summary>
    /// Gets a value indicating whether a customer company line is available.
    /// </summary>
    public bool HasCompany => !string.IsNullOrWhiteSpace(CompanyText);

    /// <summary>
    /// Gets a value indicating whether customer notes are available.
    /// </summary>
    public bool HasNotes => !string.IsNullOrWhiteSpace(NotesText);

    /// <summary>
    /// Gets a value indicating whether a latest-interaction line is available.
    /// </summary>
    public bool HasLastInteraction => !string.IsNullOrWhiteSpace(LastInteractionText);

    /// <summary>
    /// Gets a value indicating whether the customer belongs to CRM segments.
    /// </summary>
    public bool HasSegments => Segments.Count > 0;

    /// <summary>
    /// Gets a value indicating whether consent history exists.
    /// </summary>
    public bool HasConsents => Consents.Count > 0;

    /// <summary>
    /// Gets a value indicating whether interaction history exists.
    /// </summary>
    public bool HasRecentInteractions => RecentInteractions.Count > 0;

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
        });

        try
        {
            var context = await _profileService.GetLinkedCustomerContextAsync(CancellationToken.None).ConfigureAwait(false);
            if (context is null)
            {
                ClearContext();
                return;
            }

            RunOnMain(() =>
            {
                DisplayName = context.DisplayName;
                EmailText = string.Format(AppResources.MemberCustomerContextEmailFormat, context.Email);
                PhoneText = string.IsNullOrWhiteSpace(context.Phone)
                    ? null
                    : string.Format(AppResources.MemberCustomerContextPhoneFormat, context.Phone);
                CompanyText = string.IsNullOrWhiteSpace(context.CompanyName)
                    ? null
                    : string.Format(AppResources.MemberCustomerContextCompanyFormat, context.CompanyName);
                NotesText = string.IsNullOrWhiteSpace(context.Notes)
                    ? null
                    : string.Format(AppResources.MemberCustomerContextNotesFormat, context.Notes);
                CreatedAtText = string.Format(AppResources.MemberCustomerContextCreatedAtFormat, context.CreatedAtUtc.ToLocalTime());
                InteractionCountText = string.Format(AppResources.MemberCustomerContextInteractionCountFormat, context.InteractionCount);
                LastInteractionText = context.LastInteractionAtUtc.HasValue
                    ? string.Format(AppResources.MemberCustomerContextLastInteractionFormat, context.LastInteractionAtUtc.Value.ToLocalTime())
                    : null;

                Segments.Clear();
                foreach (var segment in context.Segments)
                {
                    Segments.Add(new MemberCustomerSegmentItemViewModel
                    {
                        Name = segment.Name,
                        Description = segment.Description
                    });
                }

                Consents.Clear();
                foreach (var consent in context.Consents.OrderByDescending(static item => item.GrantedAtUtc))
                {
                    Consents.Add(new MemberCustomerConsentItemViewModel
                    {
                        Type = consent.Type,
                        StatusText = consent.Granted
                            ? AppResources.MemberCustomerContextConsentStatusGranted
                            : AppResources.MemberCustomerContextConsentStatusRevoked,
                        GrantedAtText = string.Format(AppResources.MemberCustomerContextConsentGrantedAtFormat, consent.GrantedAtUtc.ToLocalTime()),
                        RevokedAtText = consent.RevokedAtUtc.HasValue
                            ? string.Format(AppResources.MemberCustomerContextConsentRevokedAtFormat, consent.RevokedAtUtc.Value.ToLocalTime())
                            : null
                    });
                }

                RecentInteractions.Clear();
                foreach (var interaction in context.RecentInteractions.OrderByDescending(static item => item.CreatedAtUtc))
                {
                    RecentInteractions.Add(new MemberCustomerInteractionItemViewModel
                    {
                        TypeChannelText = string.Format(AppResources.MemberCustomerContextInteractionTypeChannelFormat, interaction.Type, interaction.Channel),
                        SubjectText = string.IsNullOrWhiteSpace(interaction.Subject)
                            ? null
                            : string.Format(AppResources.MemberCustomerContextInteractionSubjectFormat, interaction.Subject),
                        PreviewText = interaction.ContentPreview,
                        CreatedAtText = string.Format(AppResources.MemberCustomerContextInteractionCreatedAtFormat, interaction.CreatedAtUtc.ToLocalTime())
                    });
                }

                OnPropertyChanged(nameof(HasContext));
                OnPropertyChanged(nameof(HasSegments));
                OnPropertyChanged(nameof(HasConsents));
                OnPropertyChanged(nameof(HasRecentInteractions));
            });
        }
        catch (Exception ex)
        {
            RunOnMain(() =>
            {
                ClearContext();
                ErrorMessage = ViewModelErrorMapper.ToUserMessage(ex, AppResources.MemberCustomerContextLoadFailed);
            });
        }
        finally
        {
            RunOnMain(() =>
            {
                IsBusy = false;
                RefreshCommand.RaiseCanExecuteChanged();
            });
        }
    }

    private void ClearContext()
    {
        DisplayName = string.Empty;
        EmailText = string.Empty;
        PhoneText = null;
        CompanyText = null;
        NotesText = null;
        CreatedAtText = string.Empty;
        InteractionCountText = string.Empty;
        LastInteractionText = null;
        Segments.Clear();
        Consents.Clear();
        RecentInteractions.Clear();
        OnPropertyChanged(nameof(HasContext));
        OnPropertyChanged(nameof(HasSegments));
        OnPropertyChanged(nameof(HasConsents));
        OnPropertyChanged(nameof(HasRecentInteractions));
    }
}

/// <summary>
/// Read-only CRM segment row for member customer-context screens.
/// </summary>
public sealed class MemberCustomerSegmentItemViewModel
{
    /// <summary>Gets or sets the segment name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional segment description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets a value indicating whether a description is available.</summary>
    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
}

/// <summary>
/// Read-only CRM consent-history row for member customer-context screens.
/// </summary>
public sealed class MemberCustomerConsentItemViewModel
{
    /// <summary>Gets or sets the consent type label.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the consent-status label.</summary>
    public string StatusText { get; set; } = string.Empty;

    /// <summary>Gets or sets the granted-at line.</summary>
    public string GrantedAtText { get; set; } = string.Empty;

    /// <summary>Gets or sets the revoked-at line.</summary>
    public string? RevokedAtText { get; set; }

    /// <summary>Gets a value indicating whether a revoke timestamp is available.</summary>
    public bool HasRevokedAt => !string.IsNullOrWhiteSpace(RevokedAtText);
}

/// <summary>
/// Read-only CRM interaction-history row for member customer-context screens.
/// </summary>
public sealed class MemberCustomerInteractionItemViewModel
{
    /// <summary>Gets or sets the combined type/channel line.</summary>
    public string TypeChannelText { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional subject line.</summary>
    public string? SubjectText { get; set; }

    /// <summary>Gets or sets the optional preview content.</summary>
    public string? PreviewText { get; set; }

    /// <summary>Gets or sets the creation timestamp line.</summary>
    public string CreatedAtText { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether a subject is available.</summary>
    public bool HasSubject => !string.IsNullOrWhiteSpace(SubjectText);

    /// <summary>Gets a value indicating whether a preview is available.</summary>
    public bool HasPreview => !string.IsNullOrWhiteSpace(PreviewText);
}
