using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Darwin.Contracts.Businesses;
using Darwin.Contracts.Common;
using Darwin.Contracts.Invoices;
using Darwin.Contracts.Identity;
using Darwin.Contracts.Orders;
using Darwin.Contracts.Meta;
using Darwin.Mobile.Consumer.Resources;
using Darwin.Mobile.Consumer.ViewModels;
using Darwin.Mobile.Shared.Services;
using Darwin.Mobile.Shared.Services.Commerce;
using Darwin.Shared.Results;
using FluentAssertions;

namespace Darwin.Mobile.Consumer.Tests;

public sealed class ConsumerLoyaltyAndPaymentSourceContractTests
{
    [Fact]
    public void LoginPage_SourceContract_Should_Prefill_ActivationViewModel_With_Current_Login_Email()
    {
        var loginPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/LoginPage.xaml.cs");

        loginPage.Should().Contain("var page = _serviceProvider.GetService<ActivationPage>()");
        loginPage.Should().Contain("BindingContext is LoginViewModel loginViewModel");
        loginPage.Should().Contain("page.BindingContext is ActivationViewModel activationViewModel");
        loginPage.Should().Contain("activationViewModel.ApplyPrefill(loginViewModel.Email);");
        loginPage.Should().Contain("await Navigation.PushAsync(page);");
    }

    [Fact]
    public void LoginPage_SourceContract_Should_Bind_Login_Form_Actions_And_Email_Activation_Handoff()
    {
        var page = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/LoginPage.xaml");
        var codeBehind = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/LoginPage.xaml.cs");

        page.Should().Contain("x:Class=\"Darwin.Mobile.Consumer.Views.LoginPage\"");
        page.Should().Contain("x:DataType=\"vm:LoginViewModel\"");
        page.Should().Contain("Command=\"{Binding LoginCommand}\"");
        page.Should().Contain("IsEnabled=\"{Binding IsLoginReady}\"");
        page.Should().Contain("Text=\"{x:Static res:AppResources.ExternalLoginGoogleButton}\"");
        page.Should().Contain("Command=\"{Binding LoginWithGoogleCommand}\"");
        page.Should().Contain("IsEnabled=\"{Binding IsExternalLoginReady}\"");
        page.Should().Contain("Text=\"{x:Static res:AppResources.ExternalLoginMicrosoftComingSoon}\"");
        page.Should().Contain("IsEnabled=\"False\"");
        page.Should().Contain("TapGestureRecognizer Tapped=\"OnRegisterTapped\"");
        page.Should().Contain("TapGestureRecognizer Tapped=\"OnForgotPasswordTapped\"");
        page.Should().Contain("IsVisible=\"{Binding ShowActivationEmailAction}\"");
        page.Should().Contain("Command=\"{Binding RequestActivationEmailCommand}\"");
        page.Should().Contain("Clicked=\"OnOpenActivationClicked\"");
        page.Should().Contain("Tapped=\"OnLegalHubTapped\"");

        codeBehind.Should().Contain("public partial class LoginPage");
        codeBehind.Should().Contain("private async void OnRegisterTapped(object? sender, TappedEventArgs e)");
        codeBehind.Should().Contain("private async void OnForgotPasswordTapped(object? sender, TappedEventArgs e)");
        codeBehind.Should().Contain("private async void OnOpenActivationClicked(object? sender, EventArgs e)");
        codeBehind.Should().Contain("private async void OnLegalHubTapped(object? sender, TappedEventArgs e)");
        codeBehind.Should().Contain("PushPageSafelyAsync<TPage>() where TPage : Page");
        codeBehind.Should().Contain("Interlocked.Exchange(ref _navigationInProgress, 1)");
        codeBehind.Should().Contain("Interlocked.Exchange(ref _navigationInProgress, 0)");
        codeBehind.Should().Contain("activationViewModel.ApplyPrefill(loginViewModel.Email)");
        codeBehind.Should().Contain("OnErrorBecameVisibleRequested");
    }

    [Fact]
    public void LoginViewModel_SourceContract_Should_Expose_Login_Readiness_And_Email_Activation_Handoff_Contracts()
    {
        var viewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/LoginViewModel.cs");

        viewModel.Should().Contain("public bool IsLoginReady => !IsBusy && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);");
        viewModel.Should().Contain("public string LoginReadinessMessage =>");
        viewModel.Should().Contain("AppResources.LoginReadinessBusy");
        viewModel.Should().Contain("AppResources.LoginReadinessEmail");
        viewModel.Should().Contain("AppResources.LoginReadinessPassword");
        viewModel.Should().Contain("AppResources.LoginReadinessReady");
        viewModel.Should().Contain("public bool HasInfo => !string.IsNullOrWhiteSpace(_infoMessage);");
        viewModel.Should().Contain("public bool ShowActivationEmailAction");
        viewModel.Should().Contain("ApplyEntryContext(string? email, string? infoMessage = null, bool showActivationEmailAction = false)");
        viewModel.Should().Contain("Email = email.Trim();");
        viewModel.Should().Contain("Password = string.Empty;");
        viewModel.Should().Contain("ShowActivationEmailAction = showActivationEmailAction;");
        viewModel.Should().Contain("private async Task LoginAsync()");
        viewModel.Should().Contain("private async Task LoginWithGoogleAsync()");
        viewModel.Should().Contain("_externalAuthService");
        viewModel.Should().Contain(".SignInWithGoogleAsync");
        viewModel.Should().Contain("_authService.LoginWithExternalProviderAsync");
        viewModel.Should().Contain("Provider = credential.Provider");
        viewModel.Should().Contain("IdToken = credential.IdToken");
        viewModel.Should().Contain("AllowAccountCreation = false");
        viewModel.Should().Contain("ShowActivationEmailAction = string.Equals(errorMessage, AppResources.LoginEmailConfirmationRequired, StringComparison.Ordinal);");
        viewModel.Should().Contain("private async Task OpenImpressumAsync()");
        viewModel.Should().Contain("private async Task OpenPrivacyPolicyAsync()");
        viewModel.Should().Contain("private async Task OpenTermsAsync()");
        viewModel.Should().Contain("private async Task RequestActivationEmailAsync()");
        viewModel.Should().Contain("_authService.RequestEmailConfirmationAsync(Email.Trim()");
    }

    [Fact]
    public void RegisterPage_SourceContract_Should_ReturnTo_Login_With_Pending_Activation_Context()
    {
        var registerPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/RegisterPage.xaml.cs");

        registerPage.Should().Contain("var pendingActivation = registerViewModel.HasPendingEmailConfirmation;");
        registerPage.Should().Contain("var infoMessage = pendingActivation ? AppResources.RegisterEmailConfirmationSent : null;");
        registerPage.Should().Contain("var existingLoginPage = Navigation.NavigationStack");
        registerPage.Should().Contain(".OfType<LoginPage>()");
        registerPage.Should().Contain(".LastOrDefault();");
        registerPage.Should().Contain("existingLoginViewModel.ApplyEntryContext(registerViewModel.Email, infoMessage, pendingActivation);");
        registerPage.Should().Contain("await Navigation.PushAsync(loginPage);");
        registerPage.Should().Contain("var loginPage = _serviceProvider.GetRequiredService<LoginPage>();");
        registerPage.Should().Contain("await Navigation.PopAsync();");
    }

    [Fact]
    public void RegisterPage_SourceContract_Should_Expose_Google_Registration_And_Disabled_Microsoft()
    {
        var page = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/RegisterPage.xaml");
        var viewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/RegisterViewModel.cs");

        page.Should().Contain("Text=\"{x:Static res:AppResources.ExternalRegisterDivider}\"");
        page.Should().Contain("Text=\"{x:Static res:AppResources.ExternalRegisterGoogleButton}\"");
        page.Should().Contain("Command=\"{Binding RegisterWithGoogleCommand}\"");
        page.Should().Contain("IsEnabled=\"{Binding IsExternalRegistrationReady}\"");
        page.Should().Contain("Text=\"{x:Static res:AppResources.ExternalLoginMicrosoftComingSoon}\"");
        page.Should().Contain("IsEnabled=\"False\"");

        viewModel.Should().Contain("public AsyncCommand RegisterWithGoogleCommand { get; }");
        viewModel.Should().Contain("RegisterWithGoogleCommand = new AsyncCommand(RegisterWithGoogleAsync, () => !IsBusy);");
        viewModel.Should().Contain("private async Task RegisterWithGoogleAsync()");
        viewModel.Should().Contain("_externalAuthService");
        viewModel.Should().Contain(".SignInWithGoogleAsync");
        viewModel.Should().Contain("_authService.LoginWithExternalProviderAsync");
        viewModel.Should().Contain("Provider = credential.Provider");
        viewModel.Should().Contain("IdToken = credential.IdToken");
        viewModel.Should().Contain("AllowAccountCreation = true");
    }

    [Fact]
    public void AndroidGoogleExternalAuthService_SourceContract_Should_Use_CredentialManager_With_WebClientId()
    {
        var source = ReadSourceFile("src/Darwin.Mobile.Consumer/Platforms/Android/AndroidGoogleExternalAuthService.cs");

        source.Should().Contain("bootstrap.GoogleExternalLoginWebClientId");
        source.Should().Contain("new GetSignInWithGoogleOption.Builder(bootstrap.GoogleExternalLoginWebClientId.Trim())");
        source.Should().Contain("CredentialManager.Create(activity)");
        source.Should().Contain("GoogleIdTokenCredential.CreateFrom");
        source.Should().Contain("GoogleIdTokenCredential.TypeGoogleIdTokenSiwgCredential");
        source.Should().NotContain("new GetGoogleIdOption.Builder()");
        source.Should().NotContain("GoogleSignIn.GetClient");
        source.Should().NotContain(".RequestIdToken(bootstrap.GoogleExternalLoginAndroidClientId.Trim())");
    }

    [Fact]
    public void ActivationPage_SourceContract_Should_Apply_Email_And_Token_Query_Aliases_With_Safe_Unescape()
    {
        var activationPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/ActivationPage.xaml.cs");

        activationPage.Should().Contain("var email = ReadQueryValue(query, \"email\");");
        activationPage.Should().Contain("var token = ReadQueryValue(query, \"token\")");
        activationPage.Should().Contain("?? ReadQueryValue(query, \"confirmationToken\")");
        activationPage.Should().Contain("?? ReadQueryValue(query, \"confirmToken\")");
        activationPage.Should().Contain("?? ReadQueryValue(query, \"code\");");
        activationPage.Should().Contain("viewModel.ApplyPrefill(email, token);");
        activationPage.Should().Contain("if (string.IsNullOrWhiteSpace(raw))");
    }

    [Fact]
    public void ActivationViewModel_SourceContract_Should_Respect_NonOverwriting_Prefill_Contract()
    {
        var activationViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/ActivationViewModel.cs");

        activationViewModel.Should().Contain("if (string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(email))");
        activationViewModel.Should().Contain("if (string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(token))");
        activationViewModel.Should().Contain("Email = email.Trim();");
        activationViewModel.Should().Contain("Token = token.Trim();");
    }

    [Fact]
    public void ActivationViewModel_SourceContract_Should_Expose_Email_Request_And_Confirm_Commands()
    {
        var viewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/ActivationViewModel.cs");

        viewModel.Should().Contain("public AsyncCommand RequestActivationEmailCommand { get; }");
        viewModel.Should().Contain("public AsyncCommand ConfirmEmailCommand { get; }");
        viewModel.Should().Contain("RequestActivationEmailCommand = new AsyncCommand(RequestActivationEmailAsync, () => !IsBusy);");
        viewModel.Should().Contain("ConfirmEmailCommand = new AsyncCommand(ConfirmEmailAsync, () => !IsBusy);");
        viewModel.Should().Contain("public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);");
        viewModel.Should().Contain("private async Task RequestActivationEmailAsync()");
        viewModel.Should().Contain("private async Task ConfirmEmailAsync()");
    }

    [Fact]
    public void ForgotPasswordPage_SourceContract_Should_Prefill_ResetPassword_Email_Before_Push()
    {
        var forgotPasswordPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/ForgotPasswordPage.xaml.cs");

        forgotPasswordPage.Should().Contain("var page = _serviceProvider.GetService<ResetPasswordPage>()");
        forgotPasswordPage.Should().Contain("if (page.BindingContext is ResetPasswordViewModel resetPasswordViewModel &&");
        forgotPasswordPage.Should().Contain("BindingContext is ForgotPasswordViewModel forgotPasswordViewModel)");
        forgotPasswordPage.Should().Contain("resetPasswordViewModel.ApplyPrefill(forgotPasswordViewModel.Email);");
        forgotPasswordPage.Should().Contain("await Navigation.PushAsync(page);");
        forgotPasswordPage.Should().Contain("Interlocked.Exchange(ref _navigationInProgress, 1)");
    }

    [Fact]
    public void ForgotPasswordViewModel_SourceContract_Should_Use_Trimmed_Email_And_SendResetLink_Readiness_Gating()
    {
        var viewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/ForgotPasswordViewModel.cs");

        viewModel.Should().Contain("var normalizedEmail = Email.Trim();");
        viewModel.Should().Contain("var requested = await _authService.RequestPasswordResetAsync(normalizedEmail");
        viewModel.Should().Contain("SendResetLinkCommand = new AsyncCommand(SendResetLinkAsync, () => IsSendReady);");
        viewModel.Should().Contain("public bool IsSendReady => !IsBusy && !string.IsNullOrWhiteSpace(Email);");
        viewModel.Should().Contain("ForgotPasswordReadinessMessage =>");
        viewModel.Should().Contain("IsSendReady");
        viewModel.Should().Contain("RaiseReadinessChanged();");
        viewModel.Should().Contain("SendResetLinkCommand.RaiseCanExecuteChanged();");
        viewModel.Should().Contain("AppResources.ForgotPasswordReadinessBusy");
        viewModel.Should().Contain("AppResources.ForgotPasswordReadinessEmail");
        viewModel.Should().Contain("AppResources.ForgotPasswordReadinessReady");
    }

    [Fact]
    public void RegisterPage_SourceContract_Should_Open_ActivationPage_With_Prefill_And_Navigation_Guard()
    {
        var registerPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/RegisterPage.xaml.cs");

        registerPage.Should().Contain("if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)");
        registerPage.Should().Contain("var activationPage = _serviceProvider.GetRequiredService<ActivationPage>();");
        registerPage.Should().Contain("if (activationPage.BindingContext is ActivationViewModel activationViewModel)");
        registerPage.Should().Contain("activationViewModel.ApplyPrefill(registerViewModel.Email);");
        registerPage.Should().Contain("await Navigation.PushAsync(activationPage);");
    }

    [Fact]
    public void ResetPasswordPage_SourceContract_Should_Apply_Email_And_Token_Query_Aliases()
    {
        var resetPasswordPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/ResetPasswordPage.xaml.cs");

        resetPasswordPage.Should().Contain("var email = ReadQueryValue(query, \"email\");");
        resetPasswordPage.Should().Contain("var token = ReadQueryValue(query, \"token\")");
        resetPasswordPage.Should().Contain("?? ReadQueryValue(query, \"resetToken\")");
        resetPasswordPage.Should().Contain("?? ReadQueryValue(query, \"code\");");
        resetPasswordPage.Should().Contain("viewModel.ApplyPrefill(email, token);");
    }

    [Fact]
    public void ResetPasswordViewModel_SourceContract_Should_Respect_NonOverwriting_Prefill_Contract()
    {
        var resetPasswordViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/ResetPasswordViewModel.cs");

        resetPasswordViewModel.Should().Contain("if (string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(email))");
        resetPasswordViewModel.Should().Contain("if (string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(token))");
        resetPasswordViewModel.Should().Contain("Email = email.Trim();");
        resetPasswordViewModel.Should().Contain("Token = token.Trim();");
    }

    [Fact]
    public void DiscoverPage_SourceContract_Should_Navigate_Joined_Business_to_QR_With_Joined_Flag()
    {
        var discoverPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/DiscoverPage.xaml.cs");

        discoverPage.Should().Contain("await NavigateSafelyAsync(() => Shell.Current.GoToAsync($\"//{Routes.Qr}\", new Dictionary<string, object?>");
        discoverPage.Should().Contain("[\"businessId\"] = businessId,");
        discoverPage.Should().Contain("[\"joined\"] = true");
    }

    [Fact]
    public void DiscoverPage_SourceContract_Should_Quick_Open_Rewards_And_Navigate_Explore_Delivery()
    {
        var discoverPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/DiscoverPage.xaml.cs");

        discoverPage.Should().Contain("await OpenRewardsAsync(businessId);");
        discoverPage.Should().Contain("if (selected.JoinedAccount is not null)");
        discoverPage.Should().Contain("await OpenBusinessDetailAsync(selected.JoinedAccount.BusinessId);");
        discoverPage.Should().Contain("if (selected.ExploreItem is not null)");
        discoverPage.Should().Contain("await NavigateFromExploreSelectionAsync(selected.ExploreItem);");
    }

    [Fact]
    public void RewardsPage_SourceContract_Should_Apply_Guid_Or_Decoded_BusinessId_Query_Values()
    {
        var rewardsPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/RewardsPage.xaml.cs");

        rewardsPage.Should().Contain("if (rawBusinessId is Guid businessId && businessId != Guid.Empty)");
        rewardsPage.Should().Contain("if (rawBusinessId is string businessIdText &&");
        rewardsPage.Should().Contain("Guid.TryParse(SafeUnescape(businessIdText), out var parsedBusinessId)");
        rewardsPage.Should().Contain("Uri.UnescapeDataString(raw).Trim();");
    }

    [Fact]
    public void BusinessDetailPage_SourceContract_Should_Apply_Query_Id_As_Guid_Or_Decoded_String()
    {
        var businessDetailPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/BusinessDetailPage.xaml.cs");

        businessDetailPage.Should().Contain("if (value is Guid businessId && businessId != Guid.Empty)");
        businessDetailPage.Should().Contain("var idString = value as string ?? value?.ToString();");
        businessDetailPage.Should().Contain("Guid.TryParse(SafeUnescape(idString), out var parsedBusinessId)");
        businessDetailPage.Should().Contain("Uri.UnescapeDataString(raw).Trim();");
    }

    [Fact]
    public void MemberCommerceViewModel_SourceContract_Should_Guard_Payment_And_InvoiceArtifact_Command_Readiness()
    {
        var memberCommerceViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/MemberCommerceViewModel.cs");

        memberCommerceViewModel.Should().Contain("public MemberCommerceViewModel(IMemberCommerceService memberCommerceService)");
        memberCommerceViewModel.Should().Contain("ViewOrderCommand = new AsyncCommand<MemberCommerceOrderItemViewModel>(LoadOrderDetailAsync, CanLoadOrderDetail);");
        memberCommerceViewModel.Should().Contain("RetryOrderPaymentCommand = new AsyncCommand(RetryOrderPaymentAsync, () => !IsBusy && SelectedOrder?.CanRetryPayment == true);");
        memberCommerceViewModel.Should().Contain("CopyOrderDocumentCommand = new AsyncCommand(CopyOrderDocumentAsync, () => !IsBusy && SelectedOrder?.HasDocument == true);");
        memberCommerceViewModel.Should().Contain("OpenOrderShipmentTrackingCommand = new AsyncCommand<MemberCommerceShipmentSummaryViewModel>(OpenOrderShipmentTrackingAsync, CanOpenOrderShipmentTracking);");
        memberCommerceViewModel.Should().Contain("RetryInvoicePaymentCommand = new AsyncCommand(RetryInvoicePaymentAsync, () => !IsBusy && SelectedInvoice?.CanRetryPayment == true);");
        memberCommerceViewModel.Should().Contain("CopyInvoiceDocumentCommand = new AsyncCommand(CopyInvoiceDocumentAsync, () => !IsBusy && SelectedInvoice?.HasDocument == true);");
        memberCommerceViewModel.Should().Contain("CopyInvoiceArchiveDocumentCommand = new AsyncCommand(CopyInvoiceArchiveDocumentAsync, () => !IsBusy && SelectedInvoice is not null);");
        memberCommerceViewModel.Should().Contain("CopyInvoiceStructuredDataCommand = new AsyncCommand(CopyInvoiceStructuredDataAsync, () => !IsBusy && SelectedInvoice is not null);");
        memberCommerceViewModel.Should().Contain("CopyInvoiceStructuredXmlCommand = new AsyncCommand(CopyInvoiceStructuredXmlAsync, () => !IsBusy && SelectedInvoice is not null);");
        memberCommerceViewModel.Should().Contain("Provider = \"HostedCheckout\"");
        memberCommerceViewModel.Should().Contain("string.Format(AppResources.MemberCommerceCheckoutLaunchedFormat, referenceNumber)");
        memberCommerceViewModel.Should().Contain("Browser.Default.OpenAsync(result.Value.CheckoutUrl, BrowserLaunchMode.SystemPreferred)");
        memberCommerceViewModel.Should().Contain("AppResources.MemberCommerceCheckoutLaunchedFormat");
        memberCommerceViewModel.Should().Contain("AppResources.MemberCommercePaymentIntentFailed");
    }

    [Fact]
    public void MemberCommercePage_SourceContract_Should_Expose_Invoice_Artifact_Action_Buttons()
    {
        var memberCommercePage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/MemberCommercePage.xaml");
        var resources = ReadSourceFile("src/Darwin.Mobile.Consumer/Resources/Strings.resx");

        memberCommercePage.Should().Contain("Text=\"{x:Static res:AppResources.MemberCommerceCopyDocumentButton}\"");
        memberCommercePage.Should().Contain("Text=\"{x:Static res:AppResources.MemberCommerceCopyArchiveDocumentButton}\"");
        memberCommercePage.Should().Contain("Text=\"{x:Static res:AppResources.MemberCommerceCopyStructuredDataButton}\"");
        memberCommercePage.Should().Contain("Text=\"{x:Static res:AppResources.MemberCommerceCopyStructuredXmlButton}\"");
        memberCommercePage.Should().Contain("Text=\"{x:Static res:AppResources.MemberCommerceStructuredInvoiceNotice}\"");
        memberCommercePage.Should().Contain("Command=\"{Binding CopyInvoiceArchiveDocumentCommand}\"");
        memberCommercePage.Should().Contain("Command=\"{Binding CopyInvoiceStructuredDataCommand}\"");
        memberCommercePage.Should().Contain("Command=\"{Binding CopyInvoiceStructuredXmlCommand}\"");
        memberCommercePage.Should().Contain("AppResources.MemberCommerceStructuredInvoiceNotice");
        memberCommercePage.Should().Contain("IsVisible=\"{Binding HasSelectedInvoice}\"");

        resources.Should().Contain("<data name=\"MemberCommerceCheckoutLaunchedFormat\" xml:space=\"preserve\">");
        resources.Should().Contain("<value>Hosted checkout launched for {0}.</value>");
    }

    [Fact]
    public void MemberCommercePage_SourceContract_Should_Expose_Order_And_Tracking_Action_Buttons()
    {
        var memberCommercePage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/MemberCommercePage.xaml");

        memberCommercePage.Should().Contain("Text=\"{x:Static res:AppResources.MemberCommerceViewOrderButton}\"");
        memberCommercePage.Should().Contain("Text=\"{x:Static res:AppResources.MemberCommerceViewInvoiceButton}\"");
        memberCommercePage.Should().Contain("Text=\"{x:Static res:AppResources.MemberCommerceOpenTrackingButton}\"");
        memberCommercePage.Should().Contain("Command=\"{Binding Source={x:Reference PageRoot}, Path=ViewOrderCommand, x:DataType=views:MemberCommercePage}\"");
        memberCommercePage.Should().Contain("Command=\"{Binding Source={x:Reference PageRoot}, Path=ViewInvoiceCommand, x:DataType=views:MemberCommercePage}\"");
        memberCommercePage.Should().Contain("Command=\"{Binding Source={x:Reference PageRoot}, Path=OpenOrderShipmentTrackingCommand, x:DataType=views:MemberCommercePage}\"");
        memberCommercePage.Should().Contain("Command=\"{Binding RetryOrderPaymentCommand}\"");
        memberCommercePage.Should().Contain("Command=\"{Binding CopyOrderDocumentCommand}\"");
    }

    [Fact]
    public void AccountDeletion_SourceContract_Should_Gate_Submit_Api_By_User_Confirmation_Checkbox()
    {
        var page = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/AccountDeletionPage.xaml");
        var viewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/AccountDeletionViewModel.cs");

        page.Should().Contain("IsEnabled=\"{Binding ConfirmIrreversibleDeletion}\"");
        page.Should().Contain("Command=\"{Binding SubmitDeletionRequestCommand}\"");
        viewModel.Should().Contain("if (!ConfirmIrreversibleDeletion)");
        viewModel.Should().Contain("ErrorMessage = AppResources.AccountDeletionConfirmationRequired");
    }

    [Fact]
    public void QrPage_SourceContract_Should_Accept_Guid_Encoded_Name_And_Joined_Query_Inputs()
    {
        var qrPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/QrPage.xaml.cs");

        qrPage.Should().Contain("if (query.TryGetValue(\"businessId\", out var rawBusinessId))");
        qrPage.Should().Contain("if (rawBusinessId is Guid businessId)");
        qrPage.Should().Contain("else if (rawBusinessId is string businessIdText && Guid.TryParse(SafeUnescape(businessIdText), out var parsedBusinessId)");
        qrPage.Should().Contain("_navigationContext.Clear();");
        qrPage.Should().Contain("TryApplyNavigationContextFallback(out var fallbackJoined)");
        qrPage.Should().Contain("_navigationContext.TryConsume(out var context)");
        qrPage.Should().Contain("if (query.TryGetValue(\"businessName\", out var rawBusinessName))");
        qrPage.Should().Contain("_viewModel.SetBusinessDisplayName(SafeUnescape(businessName));");
        qrPage.Should().Contain("query.TryGetValue(\"joined\", out var rawJoined)");
        qrPage.Should().Contain("string.Equals(joinedText, \"true\", StringComparison.OrdinalIgnoreCase)");
        qrPage.Should().Contain("string.Equals(joinedText, \"1\", StringComparison.OrdinalIgnoreCase)");
        qrPage.Should().Contain("_viewModel.SetJoinedStatus(joined);");
        qrPage.Should().Contain("_ = RunAppearingSafelyAsync();");
        qrPage.Should().Contain("Uri.UnescapeDataString(raw).Trim();");
        qrPage.Should().Contain("Interlocked.Exchange(ref _appearanceRefreshInProgress, 1) == 1");
    }

    [Fact]
    public void RewardsViewModel_SourceContract_Should_Expose_Open_Selected_Business_Qr_Command_And_Guards()
    {
        var rewardsViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/RewardsViewModel.cs");

        rewardsViewModel.Should().Contain("public AsyncCommand OpenSelectedBusinessQrCommand { get; }");
        rewardsViewModel.Should().Contain("OpenSelectedBusinessQrCommand = new AsyncCommand(OpenSelectedBusinessQrAsync, () => CanOpenSelectedBusinessQr);");
        rewardsViewModel.Should().Contain("public bool CanOpenSelectedBusinessQr => SelectedAccount is not null && SelectedAccount.BusinessId != Guid.Empty && !IsBusy;");
        rewardsViewModel.Should().Contain("public LoyaltyAccountSummary? SelectedAccount");
        rewardsViewModel.Should().Contain("if (value is null || value.BusinessId == Guid.Empty)");
        rewardsViewModel.Should().Contain("_ = SafeRefreshForSelectionChangeAsync();");
        rewardsViewModel.Should().Contain("OpenSelectedBusinessQrCommand.RaiseCanExecuteChanged();");
    }

    [Fact]
    public void RewardsViewModel_SourceContract_Should_Navigate_To_Qr_With_Selected_Business_Context()
    {
        var rewardsViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/RewardsViewModel.cs");

        rewardsViewModel.Should().Contain("if (!CanOpenSelectedBusinessQr || SelectedAccount is null || IsBusy)");
        rewardsViewModel.Should().Contain("await _navigationService.GoToAsync($\"//{Routes.Qr}\", parameters);");
        rewardsViewModel.Should().Contain("IDictionary<string, object?> parameters = new Dictionary<string, object?>");
        rewardsViewModel.Should().Contain("[\"businessId\"] = SelectedAccount.BusinessId");
        rewardsViewModel.Should().Contain("EndBusyState();");
    }

    [Fact]
    public void RewardsViewModel_SourceContract_Should_Throw_On_Empty_Business_Setting()
    {
        var rewardsViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/RewardsViewModel.cs");

        rewardsViewModel.Should().Contain("public void SetBusiness(Guid businessId)");
        rewardsViewModel.Should().Contain("if (businessId == Guid.Empty)");
        rewardsViewModel.Should().Contain("throw new ArgumentException(\"Business id must not be empty.\", nameof(businessId));");
    }

    [Fact]
    public void SettingsViewModel_SourceContract_Should_Expose_Loyalty_And_Settings_Navigation_Commands()
    {
        var settingsViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/SettingsViewModel.cs");

        settingsViewModel.Should().Contain("public AsyncCommand OpenProfileCommand { get; }");
        settingsViewModel.Should().Contain("public AsyncCommand OpenChangePasswordCommand { get; }");
        settingsViewModel.Should().Contain("public AsyncCommand OpenMemberCommerceCommand { get; }");
        settingsViewModel.Should().Contain("public AsyncCommand OpenMemberPreferencesCommand { get; }");
        settingsViewModel.Should().Contain("public AsyncCommand OpenLegalHubCommand { get; }");
        settingsViewModel.Should().Contain("public AsyncCommand OpenAccountDeletionCommand { get; }");
        settingsViewModel.Should().Contain("IProfileService profileService");
        settingsViewModel.Should().Contain("public string ProfileDisplayName");
        settingsViewModel.Should().Contain("public string? ProfileImageUrl");
        settingsViewModel.Should().Contain("public override async Task OnAppearingAsync()");
        settingsViewModel.Should().Contain("OpenMemberCommerceCommand = new AsyncCommand(OpenMemberCommerceAsync, () => !IsBusy);");
        settingsViewModel.Should().Contain("OpenAccountDeletionCommand = new AsyncCommand(OpenAccountDeletionAsync, () => !IsBusy);");
        settingsViewModel.Should().Contain("OpenMemberCommerceAsync()");
        settingsViewModel.Should().Contain("OpenAccountDeletionAsync()");
        settingsViewModel.Should().Contain("await NavigateAsync(Routes.MemberCommerce)");
        settingsViewModel.Should().Contain("await NavigateAsync(Routes.AccountDeletion)");
    }

    [Fact]
    public void SettingsPage_SourceContract_Should_Bind_All_Settings_Navigation_Commands()
    {
        var settingsPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/SettingsPage.xaml");

        settingsPage.Should().Contain("Command=\"{Binding OpenProfileCommand}\"");
        settingsPage.Should().Contain("Command=\"{Binding OpenMemberPreferencesCommand}\"");
        settingsPage.Should().Contain("Command=\"{Binding OpenMemberCommerceCommand}\"");
        settingsPage.Should().Contain("Command=\"{Binding OpenChangePasswordCommand}\"");
        settingsPage.Should().Contain("Command=\"{Binding OpenLegalHubCommand}\"");
        settingsPage.Should().Contain("Command=\"{Binding OpenAccountDeletionCommand}\"");
        settingsPage.Should().Contain("<TapGestureRecognizer Command=\"{Binding OpenProfileCommand}\" />");
        settingsPage.Should().Contain("Source=\"{Binding ProfileImageUrl}\"");
        settingsPage.Should().Contain("Text=\"{Binding ProfileDisplayName}\"");
        settingsPage.Should().Contain("Text=\"{Binding ProfileEmail}\"");
        settingsPage.Should().Contain("Title=\"{x:Static res:AppResources.SettingsTitle}\"");
        settingsPage.Should().Contain("Text=\"{x:Static res:AppResources.SettingsDeleteAccountButton}\"");
    }

    [Fact]
    public void ChangePasswordPage_SourceContract_Should_Bind_Update_Password_Form_And_Feedback()
    {
        var page = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/ChangePasswordPage.xaml");
        var viewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/ChangePasswordViewModel.cs");

        page.Should().Contain("x:Class=\"Darwin.Mobile.Consumer.Views.ChangePasswordPage\"");
        page.Should().Contain("Text=\"{Binding CurrentPassword}\"");
        page.Should().Contain("Text=\"{Binding NewPassword}\"");
        page.Should().Contain("Text=\"{Binding ConfirmNewPassword}\"");
        page.Should().Contain("Text=\"{Binding SuccessMessage}\"");
        page.Should().Contain("Text=\"{Binding ErrorMessage}\"");
        page.Should().Contain("Command=\"{Binding UpdatePasswordCommand}\"");
        page.Should().Contain("IsVisible=\"{Binding HasSuccess}\"");
        page.Should().Contain("IsVisible=\"{Binding HasError}\"");

        viewModel.Should().Contain("public AsyncCommand UpdatePasswordCommand { get; }");
        viewModel.Should().Contain("UpdatePasswordCommand = new AsyncCommand(UpdatePasswordAsync, () => !IsBusy && CanSubmit());");
        viewModel.Should().Contain("public string CurrentPassword");
        viewModel.Should().Contain("public string NewPassword");
        viewModel.Should().Contain("public string ConfirmNewPassword");
        viewModel.Should().Contain("public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);");
        viewModel.Should().Contain("if (!CanSubmit())");
        viewModel.Should().Contain("RunOnMain(() => ErrorMessage = AppResources.PasswordRequired);");
        viewModel.Should().Contain("if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))");
        viewModel.Should().Contain("if (NewPassword.Length < 8)");
    }

    [Fact]
    public void LegalHubViewModel_SourceContract_Should_Expose_Legal_Links_With_Busy_Guard_And_Command_Refresh()
    {
        var legalHubViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/LegalHubViewModel.cs");
        var legalHubPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/LegalHubPage.xaml");

        legalHubViewModel.Should().Contain("public AsyncCommand OpenImpressumCommand { get; }");
        legalHubViewModel.Should().Contain("public AsyncCommand OpenPrivacyPolicyCommand { get; }");
        legalHubViewModel.Should().Contain("public AsyncCommand OpenTermsCommand { get; }");
        legalHubViewModel.Should().Contain("OpenImpressumCommand = new AsyncCommand(() => OpenAsync(LegalLinkKind.Impressum), () => !IsBusy);");
        legalHubViewModel.Should().Contain("OpenPrivacyPolicyCommand = new AsyncCommand(() => OpenAsync(LegalLinkKind.PrivacyPolicy), () => !IsBusy);");
        legalHubViewModel.Should().Contain("OpenTermsCommand = new AsyncCommand(() => OpenAsync(LegalLinkKind.ConsumerTerms), () => !IsBusy);");
        legalHubViewModel.Should().Contain("if (IsBusy)");
        legalHubViewModel.Should().Contain("private void RaiseCommandStates()");
        legalHubViewModel.Should().Contain("OpenImpressumCommand.RaiseCanExecuteChanged();");
        legalHubViewModel.Should().Contain("OpenPrivacyPolicyCommand.RaiseCanExecuteChanged();");
        legalHubViewModel.Should().Contain("OpenTermsCommand.RaiseCanExecuteChanged();");

        legalHubPage.Should().Contain("Command=\"{Binding OpenImpressumCommand}\"");
        legalHubPage.Should().Contain("Command=\"{Binding OpenPrivacyPolicyCommand}\"");
        legalHubPage.Should().Contain("Command=\"{Binding OpenTermsCommand}\"");
        legalHubPage.Should().Contain("Clicked=\"OnAccountDeletionClicked\"");
    }

    [Fact]
    public void LegalHubPage_SourceContract_Should_Guard_AccountDeletion_Navigation_With_Atomic_Flag()
    {
        var legalHubPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/LegalHubPage.xaml.cs");

        legalHubPage.Should().Contain("if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)");
        legalHubPage.Should().Contain("var page = _serviceProvider.GetRequiredService<AccountDeletionPage>();");
        legalHubPage.Should().Contain("await Navigation.PushAsync(page);");
        legalHubPage.Should().Contain("private async void OnAccountDeletionClicked(object? sender, EventArgs e)");
        legalHubPage.Should().Contain("private readonly IServiceProvider _serviceProvider;");
        legalHubPage.Should().Contain("Interlocked.Exchange(ref _navigationInProgress, 0)");
    }

    [Fact]
    public void MemberPreferencesPage_SourceContract_Should_Bind_Refresh_Save_And_Preference_Toggles()
    {
        var memberPreferencesPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/MemberPreferencesPage.xaml");
        var memberPreferencesViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/MemberPreferencesViewModel.cs");

        memberPreferencesPage.Should().Contain("Command=\"{Binding RefreshCommand}\"");
        memberPreferencesPage.Should().Contain("Command=\"{Binding SaveCommand}\"");
        memberPreferencesPage.Should().Contain("IsToggled=\"{Binding MarketingConsent}\"");
        memberPreferencesPage.Should().Contain("IsToggled=\"{Binding AllowEmailMarketing}\"");
        memberPreferencesPage.Should().Contain("IsToggled=\"{Binding AllowSmsMarketing}\"");
        memberPreferencesPage.Should().Contain("IsToggled=\"{Binding AllowWhatsAppMarketing}\"");
        memberPreferencesPage.Should().Contain("IsToggled=\"{Binding AllowPromotionalPushNotifications}\"");
        memberPreferencesPage.Should().Contain("IsToggled=\"{Binding AllowOptionalAnalyticsTracking}\"");
        memberPreferencesPage.Should().Contain("Text=\"{Binding SuccessMessage}\"");
        memberPreferencesPage.Should().Contain("Text=\"{Binding ErrorMessage}\"");

        memberPreferencesViewModel.Should().Contain("public AsyncCommand RefreshCommand { get; }");
        memberPreferencesViewModel.Should().Contain("public AsyncCommand SaveCommand { get; }");
        memberPreferencesViewModel.Should().Contain("public bool MarketingConsent");
        memberPreferencesViewModel.Should().Contain("if (SetProperty(ref _marketingConsent, value) && !value)");
        memberPreferencesViewModel.Should().Contain("AllowEmailMarketing = false;");
        memberPreferencesViewModel.Should().Contain("AllowSmsMarketing = false;");
        memberPreferencesViewModel.Should().Contain("AllowWhatsAppMarketing = false;");
        memberPreferencesViewModel.Should().Contain("AllowPromotionalPushNotifications = false;");
        memberPreferencesViewModel.Should().Contain("public bool HasAcceptsTermsAt => !string.IsNullOrWhiteSpace(AcceptsTermsAtText);");
    }

    [Fact]
    public void MemberPreferencesViewModel_SourceContract_Should_Send_RowVersion_With_Marketing_Gating()
    {
        var memberPreferencesViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/MemberPreferencesViewModel.cs");

        memberPreferencesViewModel.Should().Contain("RowVersion = _rowVersion,");
        memberPreferencesViewModel.Should().Contain("MarketingConsent = MarketingConsent,");
        memberPreferencesViewModel.Should().Contain("AllowEmailMarketing = MarketingConsent && AllowEmailMarketing");
        memberPreferencesViewModel.Should().Contain("AllowSmsMarketing = MarketingConsent && AllowSmsMarketing");
        memberPreferencesViewModel.Should().Contain("AllowWhatsAppMarketing = MarketingConsent && AllowWhatsAppMarketing");
        memberPreferencesViewModel.Should().Contain("AllowPromotionalPushNotifications = MarketingConsent && AllowPromotionalPushNotifications");
        memberPreferencesViewModel.Should().Contain("AllowOptionalAnalyticsTracking = AllowOptionalAnalyticsTracking");
        memberPreferencesViewModel.Should().Contain("LoadPreferencesSnapshotAsync(cancellationToken)");
        memberPreferencesViewModel.Should().Contain("_rowVersion = preferences.RowVersion;");
        memberPreferencesViewModel.Should().Contain("MarketingConsent = preferences.MarketingConsent;");
    }

    [Fact]
    public void ProfilePage_SourceContract_Should_Bind_Refresh_Save_Push_And_Navigation_Guards()
    {
        var page = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/ProfilePage.xaml");
        var codeBehind = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/ProfilePage.xaml.cs");

        page.Should().Contain("x:Class=\"Darwin.Mobile.Consumer.Views.ProfilePage\"");
        page.Should().Contain("x:DataType=\"vm:ProfileViewModel\"");
        page.Should().Contain("RefreshCommand");
        page.Should().Contain("SaveProfileCommand");
        page.Should().Contain("RequestPhoneVerificationCommand");
        page.Should().Contain("ConfirmPhoneVerificationCommand");
        page.Should().Contain("OpenNotificationSettingsCommand");
        page.Should().Contain("Command=\"{Binding RefreshCommand}\"");
        page.Should().Contain("Command=\"{Binding SaveProfileCommand}\"");
        page.Should().Contain("Command=\"{Binding RequestPhoneVerificationCommand}\"");
        page.Should().Contain("Command=\"{Binding ConfirmPhoneVerificationCommand}\"");
        page.Should().Contain("Command=\"{Binding OpenNotificationSettingsCommand}\"");
        page.Should().Contain("IsVisible=\"{Binding ShouldShowPhoneVerificationRequest}\"");
        page.Should().Contain("IsVisible=\"{Binding ShouldShowPhoneVerificationCodeEntry}\"");
        page.Should().NotContain("Clicked=\"OnManageAddressesClicked\"");
        page.Should().NotContain("ProfileManageAddressesButton");
        page.Should().Contain("Clicked=\"OnViewCustomerContextClicked\"");
        page.Should().Contain("Text=\"{x:Static res:AppResources.ProfilePhoneVerificationRequestButton}\"");
        page.Should().Contain("Text=\"{x:Static res:AppResources.ProfilePhoneVerificationConfirmButton}\"");
        page.Should().Contain("Text=\"{x:Static res:AppResources.ProfilePushOpenSettingsButton}\"");
        page.Should().NotContain("Text=\"{x:Static res:AppResources.ProfilePushRegistrationSyncButton}\"");

        codeBehind.Should().Contain("private readonly ProfileViewModel _viewModel;");
        codeBehind.Should().Contain("private int _navigationInProgress;");
        codeBehind.Should().Contain("public ProfilePage(ProfileViewModel viewModel)");
        codeBehind.Should().Contain("BindingContext = _viewModel;");
        codeBehind.Should().Contain("await _viewModel.OnAppearingAsync();");
        codeBehind.Should().Contain("await _viewModel.OnDisappearingAsync();");
        codeBehind.Should().Contain("if (Interlocked.Exchange(ref _navigationInProgress, 1) == 1)");
        codeBehind.Should().NotContain("await NavigateSafelyAsync(Routes.MemberAddresses)");
        codeBehind.Should().Contain("await NavigateSafelyAsync(Routes.MemberCustomerContext)");
        codeBehind.Should().Contain("await NavigateSafelyAsync(Routes.AccountDeletion)");
        codeBehind.Should().Contain("catch");
    }

    [Fact]
    public void ProfileViewModel_SourceContract_Should_Expose_Profile_Commands_And_Dirty_Refresh_Load_Relays()
    {
        var viewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/ProfileViewModel.cs");

        viewModel.Should().Contain("public ProfileViewModel(");
        viewModel.Should().Contain("IProfileService profileService");
        viewModel.Should().Contain("IConsumerPushRegistrationCoordinator pushRegistrationCoordinator");
        viewModel.Should().Contain("IConsumerPushTokenProvider pushTokenProvider");
        viewModel.Should().Contain("IConsumerNotificationPermissionService notificationPermissionService");
        viewModel.Should().Contain("TimeProvider timeProvider");
        viewModel.Should().Contain("RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);");
        viewModel.Should().Contain("SaveProfileCommand = new AsyncCommand(SaveProfileAsync, () => !IsBusy);");
        viewModel.Should().Contain("RequestPhoneVerificationCommand = new AsyncCommand(RequestPhoneVerificationAsync, CanRunPhoneVerificationAction);");
        viewModel.Should().Contain("ConfirmPhoneVerificationCommand = new AsyncCommand(ConfirmPhoneVerificationAsync, CanRunPhoneVerificationAction);");
        viewModel.Should().Contain("SyncPushRegistrationCommand = new AsyncCommand(SyncPushRegistrationAsync, () => !IsPushSyncBusy);");
        viewModel.Should().Contain("OpenNotificationSettingsCommand = new AsyncCommand(OpenNotificationSettingsAsync, () => !_isOpeningNotificationSettings);");
        viewModel.Should().Contain("public bool ShouldShowPhoneVerificationRequest");
        viewModel.Should().Contain("public bool ShouldShowPhoneVerificationCodeEntry");
        viewModel.Should().Contain("HasRequestedPhoneVerificationCode = result.Succeeded;");
        viewModel.Should().Contain("public override async Task OnAppearingAsync()");
        viewModel.Should().Contain("SchedulePushRuntimeStateRefresh();");
        viewModel.Should().Contain("await RefreshPushRuntimeStateAsync().ConfigureAwait(false);");
        viewModel.Should().Contain("if (_isLoaded)");
        viewModel.Should().Contain("await RefreshAsync();");
        viewModel.Should().Contain("public override Task OnDisappearingAsync()");
        viewModel.Should().Contain("CancelCurrentOperation();");
        viewModel.Should().Contain("CancelCurrentPushOperation();");
        viewModel.Should().Contain("public string PhoneVerificationReadinessText =>");
        viewModel.Should().Contain("public string? SuccessMessage");
        viewModel.Should().Contain("public bool HasSuccess");
        viewModel.Should().Contain("public bool HasPhoneVerificationStatus");
        viewModel.Should().Contain("public bool IsPushSyncBusy");
        viewModel.Should().Contain("public bool HasLastPushSyncAt");
    }

    [Fact]
    public void MemberAddressesPage_SourceContract_Should_Bind_Address_List_And_Editor_Commands()
    {
        var page = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/MemberAddressesPage.xaml");
        var codeBehind = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/MemberAddressesPage.xaml.cs");

        page.Should().Contain("x:Class=\"Darwin.Mobile.Consumer.Views.MemberAddressesPage\"");
        page.Should().Contain("x:DataType=\"vm:MemberAddressesViewModel\"");
        page.Should().Contain("Command=\"{Binding RefreshCommand}\"");
        page.Should().Contain("Command=\"{Binding StartCreateCommand}\"");
        page.Should().Contain("Command=\"{Binding SaveCommand}\"");
        page.Should().Contain("Command=\"{Binding CancelEditCommand}\"");
        page.Should().Contain("Command=\"{Binding Source={x:Reference PageRoot}, Path=EditCommand, x:DataType=views:MemberAddressesPage}\"");
        page.Should().Contain("Command=\"{Binding Source={x:Reference PageRoot}, Path=DeleteCommand, x:DataType=views:MemberAddressesPage}\"");
        page.Should().Contain("Command=\"{Binding Source={x:Reference PageRoot}, Path=SetDefaultBillingCommand, x:DataType=views:MemberAddressesPage}\"");
        page.Should().Contain("Command=\"{Binding Source={x:Reference PageRoot}, Path=SetDefaultShippingCommand, x:DataType=views:MemberAddressesPage}\"");
        page.Should().Contain("IsVisible=\"{Binding HasAddresses, Converter={StaticResource InverseBoolConverter}}\"");
        page.Should().Contain("ItemsSource=\"{Binding Addresses}\"");
        page.Should().Contain("IsVisible=\"{Binding IsBusy}\"");

        codeBehind.Should().Contain("public partial class MemberAddressesPage : ContentPage");
        codeBehind.Should().Contain("private readonly MemberAddressesViewModel _viewModel;");
        codeBehind.Should().Contain("public MemberAddressesPage(MemberAddressesViewModel viewModel)");
        codeBehind.Should().Contain("BindingContext = _viewModel;");
        codeBehind.Should().Contain("await _viewModel.OnAppearingAsync();");
        codeBehind.Should().Contain("await _viewModel.OnDisappearingAsync();");
    }

    [Fact]
    public void MemberAddressesViewModel_SourceContract_Should_Expose_Address_Crud_Commands_And_RowVersion_Contracts()
    {
        var viewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/MemberAddressesViewModel.cs");

        viewModel.Should().Contain("public MemberAddressesViewModel(IProfileService profileService)");
        viewModel.Should().Contain("RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);");
        viewModel.Should().Contain("StartCreateCommand = new AsyncCommand(StartCreateAsync, () => !IsBusy);");
        viewModel.Should().Contain("SaveCommand = new AsyncCommand(SaveAsync, () => !IsBusy);");
        viewModel.Should().Contain("CancelEditCommand = new AsyncCommand(CancelEditAsync, () => !IsBusy);");
        viewModel.Should().Contain("EditCommand = new AsyncCommand<MemberAddressItemViewModel>(EditAsync, item => !IsBusy && item is not null);");
        viewModel.Should().Contain("DeleteCommand = new AsyncCommand<MemberAddressItemViewModel>(DeleteAsync, item => !IsBusy && item is not null);");
        viewModel.Should().Contain("SetDefaultBillingCommand = new AsyncCommand<MemberAddressItemViewModel>(SetDefaultBillingAsync, item => !IsBusy && item is not null);");
        viewModel.Should().Contain("SetDefaultShippingCommand = new AsyncCommand<MemberAddressItemViewModel>(SetDefaultShippingAsync, item => !IsBusy && item is not null);");
        viewModel.Should().Contain("public string EditorTitle => IsEditingExisting");
        viewModel.Should().Contain("public bool IsEditingExisting");
        viewModel.Should().Contain("public bool HasSuccess => !string.IsNullOrWhiteSpace(SuccessMessage);");
        viewModel.Should().Contain("private async Task ReloadAddressesAsync(CancellationToken cancellationToken)");
        viewModel.Should().Contain("public override async Task OnAppearingAsync()");
        viewModel.Should().Contain("public override Task OnDisappearingAsync()");
        viewModel.Should().Contain("ResetEditor");
        viewModel.Should().Contain("var update = new UpdateMemberAddressRequest");
        viewModel.Should().Contain("RowVersion = _editingRowVersion.ToArray()");
        viewModel.Should().Contain("var create = new CreateMemberAddressRequest");
    }

    [Fact]
    public void FeedPage_SourceContract_Should_Bind_Loyalty_Refresh_Promotions_And_Diagnostics()
    {
        var page = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/FeedPage.xaml");
        var codeBehind = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/FeedPage.xaml.cs");

        page.Should().Contain("x:Class=\"Darwin.Mobile.Consumer.Views.FeedPage\"");
        page.Should().Contain("x:DataType=\"vm:FeedViewModel\"");
        page.Should().Contain("RefreshCommand=\"{Binding RefreshCommand}\"");
        page.Should().Contain("Command=\"{Binding LoadMoreCommand}\"");
        page.Should().Contain("Command=\"{Binding OpenQrCommand}\"");
        page.Should().Contain("Command=\"{Binding OpenRewardsCommand}\"");
        page.Should().Contain("OpenPromotionCommand");
        page.Should().Contain("Command=\"{Binding ShowSelectedBusinessPromotionsCommand}\"");
        page.Should().Contain("Command=\"{Binding ShowAllBusinessesPromotionsCommand}\"");
        page.Should().Contain("CommandParameter=\"{Binding .}\"");
        page.Should().Contain("HasPromotions");
        page.Should().Contain("HasPromotionDiagnosticsSnapshotAt");

        codeBehind.Should().Contain("public partial class FeedPage");
        codeBehind.Should().Contain("private readonly FeedViewModel _viewModel;");
        codeBehind.Should().Contain("public FeedPage(FeedViewModel viewModel)");
        codeBehind.Should().Contain("BindingContext = _viewModel;");
        codeBehind.Should().Contain("await _viewModel.OnAppearingAsync();");
        codeBehind.Should().Contain("await _viewModel.OnDisappearingAsync();");
    }

    [Fact]
    public void FeedViewModel_SourceContract_Should_Expose_Loyalty_Navigation_Rules_And_Promotion_Commands()
    {
        var viewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/FeedViewModel.cs");

        viewModel.Should().Contain("public FeedViewModel(");
        viewModel.Should().Contain("ILoyaltyService loyaltyService");
        viewModel.Should().Contain("IConsumerLoyaltySnapshotCache loyaltySnapshotCache");
        viewModel.Should().Contain("INavigationService navigationService");
        viewModel.Should().Contain("TimeProvider timeProvider");
        viewModel.Should().Contain("RefreshCommand = new AsyncCommand(RefreshAsync, () => !IsBusy);");
        viewModel.Should().Contain("LoadMoreCommand = new AsyncCommand(LoadMoreAsync, () => HasMore && !_isLoadingMore && !IsBusy);");
        viewModel.Should().Contain("OpenQrCommand = new AsyncCommand(OpenQrAsync, () => CanNavigateWithSelection);");
        viewModel.Should().Contain("OpenRewardsCommand = new AsyncCommand(OpenRewardsAsync, () => CanNavigateWithSelection);");
        viewModel.Should().Contain("OpenPromotionCommand = new AsyncCommand<PromotionFeedItem>(OpenPromotionAsync, item => item is not null && !IsBusy);");
        viewModel.Should().Contain("ShowSelectedBusinessPromotionsCommand = new AsyncCommand(ShowSelectedBusinessPromotionsAsync, () => !IsBusy);");
        viewModel.Should().Contain("ShowAllBusinessesPromotionsCommand = new AsyncCommand(ShowAllBusinessesPromotionsAsync, () => !IsBusy);");
        viewModel.Should().Contain("CopyPromotionDiagnosticsCommand = new AsyncCommand(CopyPromotionDiagnosticsAsync, () => HasPromotionDiagnosticsSnapshotAt && !IsBusy);");
        viewModel.Should().Contain("ClearPromotionDiagnosticsStatusCommand = new AsyncCommand(ClearPromotionDiagnosticsStatusAsync, () => HasPromotionDiagnosticsCopyStatus && !IsBusy);");
        viewModel.Should().Contain("public override async Task OnAppearingAsync()");
        viewModel.Should().Contain("if (_hasLoaded)");
        viewModel.Should().Contain("await RefreshAsync();");
        viewModel.Should().Contain("public override Task OnDisappearingAsync()");
        viewModel.Should().Contain("CancelCurrentOperation();");
        viewModel.Should().Contain("EndBusyState();");
        viewModel.Should().Contain("_loyaltyService.GetMyLoyaltyTimelinePageAsync");
        viewModel.Should().Contain("var selectedBusinessId = await LoadAccountsAndResolveSelectionAsync");
        viewModel.Should().Contain("BuildContextParameters()");
        viewModel.Should().Contain("await _navigationService.GoToAsync($\"//{Routes.Qr}\", parameters);");
        viewModel.Should().Contain("await _navigationService.GoToAsync($\"//{Routes.Rewards}\", parameters);");
        viewModel.Should().Contain("HasPromotionDiagnosticsSnapshotAt");
        viewModel.Should().Contain("PromotionPolicyDiagnosticsSummaryText");
        viewModel.Should().Contain("TrackPromotionInteractionBestEffortAsync");
    }

    [Fact]
    public void MemberCustomerContextPage_SourceContract_Should_Bind_ViewModel_And_Refresh_On_Appearing()
    {
        var page = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/MemberCustomerContextPage.xaml");
        var codeBehind = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/MemberCustomerContextPage.xaml.cs");

        page.Should().Contain("x:Class=\"Darwin.Mobile.Consumer.Views.MemberCustomerContextPage\"");
        page.Should().Contain("x:DataType=\"vm:MemberCustomerContextViewModel\"");
        page.Should().Contain("Command=\"{Binding RefreshCommand}\"");
        page.Should().Contain("IsVisible=\"{Binding HasSegments}\"");
        page.Should().Contain("ItemsSource=\"{Binding Segments}\"");

        codeBehind.Should().Contain("public MemberCustomerContextPage(MemberCustomerContextViewModel viewModel)");
        codeBehind.Should().Contain("_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));");
        codeBehind.Should().Contain("BindingContext = _viewModel;");
        codeBehind.Should().Contain("await _viewModel.OnAppearingAsync();");
        codeBehind.Should().Contain("await _viewModel.OnDisappearingAsync();");
    }

    [Fact]
    public void LegalHubPage_SourceContract_Should_Preserve_Ioc_Constructor_And_BindingState()
    {
        var legalHubPage = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/LegalHubPage.xaml.cs");

        legalHubPage.Should().Contain("public LegalHubPage(LegalHubViewModel viewModel, IServiceProvider serviceProvider)");
        legalHubPage.Should().Contain("_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));");
        legalHubPage.Should().Contain("_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));");
        legalHubPage.Should().Contain("BindingContext = _viewModel;");
    }

    [Fact]
    public void AccountDeletionPage_SourceContract_Should_Bind_ViewModel_And_Cleanup_On_Disappear()
    {
        var page = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/AccountDeletionPage.xaml");
        var codeBehind = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/AccountDeletionPage.xaml.cs");

        page.Should().Contain("x:Class=\"Darwin.Mobile.Consumer.Views.AccountDeletionPage\"");
        page.Should().Contain("IsEnabled=\"{Binding ConfirmIrreversibleDeletion}\"");
        page.Should().Contain("Command=\"{Binding SubmitDeletionRequestCommand}\"");

        codeBehind.Should().Contain("public AccountDeletionPage(AccountDeletionViewModel viewModel)");
        codeBehind.Should().Contain("_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));");
        codeBehind.Should().Contain("BindingContext = _viewModel;");
        codeBehind.Should().Contain("await _viewModel.OnDisappearingAsync();");
        codeBehind.Should().Contain("base.OnDisappearing();");
    }

    [Fact]
    public void ChangePasswordPage_SourceContract_Should_Clean_Cancelable_Password_Workflow_On_Disappearing()
    {
        var codeBehind = ReadSourceFile("src/Darwin.Mobile.Consumer/Views/ChangePasswordPage.xaml.cs");

        codeBehind.Should().Contain("public ChangePasswordPage(ChangePasswordViewModel viewModel)");
        codeBehind.Should().Contain("_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));");
        codeBehind.Should().Contain("BindingContext = _viewModel;");
        codeBehind.Should().Contain("await _viewModel.OnDisappearingAsync();");
        codeBehind.Should().Contain("base.OnDisappearing();");
    }

    [Fact]
    public void SettingsViewModel_SourceContract_Should_Gate_Navigation_When_Busy_And_Reset_Command_States()
    {
        var settingsViewModel = ReadSourceFile("src/Darwin.Mobile.Consumer/ViewModels/SettingsViewModel.cs");

        settingsViewModel.Should().Contain("if (IsBusy)");
        settingsViewModel.Should().Contain("return;");
        settingsViewModel.Should().Contain("IsBusy = true;");
        settingsViewModel.Should().Contain("RaiseCommandStates();");
        settingsViewModel.Should().Contain("IsBusy = false;");
        settingsViewModel.Should().Contain("private void RaiseCommandStates()");
        settingsViewModel.Should().Contain("OpenProfileCommand.RaiseCanExecuteChanged();");
        settingsViewModel.Should().Contain("OpenMemberCommerceCommand.RaiseCanExecuteChanged();");
        settingsViewModel.Should().Contain("OpenAccountDeletionCommand.RaiseCanExecuteChanged();");
    }

    private static string ReadSourceFile(string relativePath)
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(root.Combine(relativePath));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src", "Darwin.Mobile.Consumer")))
            {
                return current;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the repository root for Consumer tests.");
    }
}

public sealed class MemberCommerceCommandGatingTests
{
    [Fact]
    public void MemberCommerce_CanExecute_For_Order_Commands_Should_Track_SelectedOrder_State()
    {
        var viewModel = new MemberCommerceViewModel(new FakeMemberCommerceService());

        viewModel.RetryOrderPaymentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyOrderDocumentCommand.CanExecute(null).Should().BeFalse();

        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            CanRetryPayment = false,
            HasDocument = false
        });

        viewModel.RetryOrderPaymentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyOrderDocumentCommand.CanExecute(null).Should().BeFalse();

        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            CanRetryPayment = true,
            HasDocument = false
        });

        viewModel.RetryOrderPaymentCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyOrderDocumentCommand.CanExecute(null).Should().BeFalse();

        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            CanRetryPayment = false,
            HasDocument = true
        });

        viewModel.RetryOrderPaymentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyOrderDocumentCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void MemberCommerce_CanExecute_For_InvoiceCommands_Should_Track_SelectedInvoice_State()
    {
        var viewModel = new MemberCommerceViewModel(new FakeMemberCommerceService());

        viewModel.RetryInvoicePaymentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyInvoiceDocumentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyInvoiceArchiveDocumentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyInvoiceStructuredDataCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyInvoiceStructuredXmlCommand.CanExecute(null).Should().BeFalse();

        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            CanRetryPayment = false,
            HasDocument = false
        });

        viewModel.RetryInvoicePaymentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyInvoiceDocumentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyInvoiceArchiveDocumentCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyInvoiceStructuredDataCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyInvoiceStructuredXmlCommand.CanExecute(null).Should().BeTrue();

        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            CanRetryPayment = true,
            HasDocument = true
        });

        viewModel.RetryInvoicePaymentCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyInvoiceDocumentCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyInvoiceArchiveDocumentCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyInvoiceStructuredDataCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyInvoiceStructuredXmlCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void MemberCommerce_CanExecute_For_ViewOrderCommand_Should_Depend_On_Parameter()
    {
        var viewModel = new MemberCommerceViewModel(new FakeMemberCommerceService());

        viewModel.ViewOrderCommand.CanExecute(null).Should().BeFalse();
        viewModel.ViewOrderCommand.CanExecute(new MemberCommerceOrderItemViewModel
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-1001"
        }).Should().BeTrue();
    }

    [Fact]
    public void MemberCommerce_CanExecute_For_ViewInvoiceCommand_Should_Depend_On_Parameter()
    {
        var viewModel = new MemberCommerceViewModel(new FakeMemberCommerceService());

        viewModel.ViewInvoiceCommand.CanExecute(null).Should().BeFalse();
        viewModel.ViewInvoiceCommand.CanExecute(new MemberCommerceInvoiceItemViewModel
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = "INV-1001"
        }).Should().BeTrue();
    }

    [Fact]
    public void MemberCommerce_CanExecute_For_OpenOrderShipmentTrackingCommand_Should_Track_TrackingLink()
    {
        var viewModel = new MemberCommerceViewModel(new FakeMemberCommerceService());

        viewModel.OpenOrderShipmentTrackingCommand.CanExecute(null).Should().BeFalse();

        viewModel.OpenOrderShipmentTrackingCommand.CanExecute(new MemberCommerceShipmentSummaryViewModel
        {
            TrackingUrl = null
        }).Should().BeFalse();

        viewModel.OpenOrderShipmentTrackingCommand.CanExecute(new MemberCommerceShipmentSummaryViewModel
        {
            TrackingUrl = "   "
        }).Should().BeFalse();

        viewModel.OpenOrderShipmentTrackingCommand.CanExecute(new MemberCommerceShipmentSummaryViewModel
        {
            TrackingUrl = "https://example.com/track"
        }).Should().BeTrue();
    }

    [Fact]
    public void MemberCommerce_CanExecute_For_PaymentCommands_Should_Be_Gated_By_BusyState()
    {
        var viewModel = new MemberCommerceViewModel(new FakeMemberCommerceService());

        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            CanRetryPayment = true,
            HasDocument = true
        });

        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            CanRetryPayment = true,
            HasDocument = true
        });

        var order = new MemberCommerceOrderItemViewModel
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-1002"
        };

        var invoice = new MemberCommerceInvoiceItemViewModel
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = "INV-1002"
        };

        var shipment = new MemberCommerceShipmentSummaryViewModel
        {
            TrackingUrl = "https://track.example.com/abc"
        };

        viewModel.ViewOrderCommand.CanExecute(order).Should().BeTrue();
        viewModel.ViewInvoiceCommand.CanExecute(invoice).Should().BeTrue();
        viewModel.OpenOrderShipmentTrackingCommand.CanExecute(shipment).Should().BeTrue();
        viewModel.RetryOrderPaymentCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyOrderDocumentCommand.CanExecute(null).Should().BeTrue();
        viewModel.RetryInvoicePaymentCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyInvoiceDocumentCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyInvoiceArchiveDocumentCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyInvoiceStructuredDataCommand.CanExecute(null).Should().BeTrue();
        viewModel.CopyInvoiceStructuredXmlCommand.CanExecute(null).Should().BeTrue();

        SetPrivateField(viewModel, "_isBusy", true);

        viewModel.ViewOrderCommand.CanExecute(order).Should().BeFalse();
        viewModel.ViewInvoiceCommand.CanExecute(invoice).Should().BeFalse();
        viewModel.OpenOrderShipmentTrackingCommand.CanExecute(shipment).Should().BeFalse();
        viewModel.RetryOrderPaymentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyOrderDocumentCommand.CanExecute(null).Should().BeFalse();
        viewModel.RetryInvoicePaymentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyInvoiceDocumentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyInvoiceArchiveDocumentCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyInvoiceStructuredDataCommand.CanExecute(null).Should().BeFalse();
        viewModel.CopyInvoiceStructuredXmlCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ActivationViewModel_CanExecute_For_EmailCommands_Should_Be_Gated_By_BusyState()
    {
        var viewModel = new ActivationViewModel(new FakeActivationAuthService());

        viewModel.RequestActivationEmailCommand.CanExecute(null).Should().BeTrue();
        viewModel.ConfirmEmailCommand.CanExecute(null).Should().BeTrue();

        SetPrivateField(viewModel, "_isBusy", true);

        viewModel.RequestActivationEmailCommand.CanExecute(null).Should().BeFalse();
        viewModel.ConfirmEmailCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task ActivationViewModel_OnDisappearingAsync_Should_Not_Throw_When_No_Operation_Is_Active()
    {
        var viewModel = new ActivationViewModel(new FakeActivationAuthService());

        var exception = await Record.ExceptionAsync(async () => await viewModel.OnDisappearingAsync());

        exception.Should().BeNull();
    }

    [Fact]
    public async Task ActivationViewModel_RequestActivationEmailAsync_Should_Not_Call_Service_When_Email_Is_Missing()
    {
        var fakeAuth = new FakeActivationAuthService();
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "   "
        };

        await InvokePrivateAsync(viewModel, "RequestActivationEmailAsync");

        fakeAuth.RequestEmailConfirmationCallCount.Should().Be(0);
        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.EmailRequired);
    }

    [Fact]
    public async Task ActivationViewModel_RequestActivationEmailAsync_Should_Trim_Email_And_Call_Service()
    {
        var fakeAuth = new FakeActivationAuthService();
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "  user@example.com  "
        };

        await InvokePrivateAsync(viewModel, "RequestActivationEmailAsync");

        fakeAuth.RequestEmailConfirmationCallCount.Should().Be(1);
        fakeAuth.LastRequestedEmail.Should().Be("user@example.com");
        (await WaitForConditionAsync(() => viewModel.SuccessMessage is not null)).Should().BeTrue();
        viewModel.SuccessMessage.Should().Be(AppResources.ActivationEmailSent);
    }

    [Fact]
    public async Task ActivationViewModel_RequestActivationEmailAsync_Should_Set_Error_When_Request_Fails()
    {
        var fakeAuth = new FakeActivationAuthService
        {
            RequestEmailConfirmationResult = false
        };
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "user@example.com"
        };

        await InvokePrivateAsync(viewModel, "RequestActivationEmailAsync");

        fakeAuth.RequestEmailConfirmationCallCount.Should().Be(1);
        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.ActivationEmailRequestFailed);
    }

    [Fact]
    public async Task ActivationViewModel_RequestActivationEmailAsync_Should_Set_Error_When_Service_Throws()
    {
        var fakeAuth = new FakeActivationAuthService
        {
            RequestEmailConfirmationException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "user@example.com"
        };

        await InvokePrivateAsync(viewModel, "RequestActivationEmailAsync");

        fakeAuth.RequestEmailConfirmationCallCount.Should().Be(1);
        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.ActivationEmailRequestFailed);
    }

    [Fact]
    public async Task ActivationViewModel_RequestActivationEmailAsync_Should_Set_Success_When_Request_Succeeds()
    {
        var fakeAuth = new FakeActivationAuthService();
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "user@example.com"
        };

        await InvokePrivateAsync(viewModel, "RequestActivationEmailAsync");

        fakeAuth.RequestEmailConfirmationCallCount.Should().Be(1);
        fakeAuth.LastRequestedEmail.Should().Be("user@example.com");
        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.SuccessMessage))).Should().BeTrue();
        viewModel.SuccessMessage.Should().Be(AppResources.ActivationEmailSent);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ActivationViewModel_ConfirmEmailAsync_Should_Not_Call_Service_When_Token_Is_Missing()
    {
        var fakeAuth = new FakeActivationAuthService();
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "user@example.com",
            Token = "   "
        };

        await InvokePrivateAsync(viewModel, "ConfirmEmailAsync");

        fakeAuth.ConfirmEmailCallCount.Should().Be(0);
        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.ActivationEmailTokenRequired);
    }

    [Fact]
    public async Task ActivationViewModel_ConfirmEmailAsync_Should_Not_Call_Service_When_Email_Is_Missing()
    {
        var fakeAuth = new FakeActivationAuthService();
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "   ",
            Token = "valid-token"
        };

        await InvokePrivateAsync(viewModel, "ConfirmEmailAsync");

        fakeAuth.ConfirmEmailCallCount.Should().Be(0);
        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.ActivationEmailTokenRequired);
    }

    [Fact]
    public async Task ActivationViewModel_ConfirmEmailAsync_Should_Trim_Email_And_Token_And_Call_Service()
    {
        var fakeAuth = new FakeActivationAuthService();
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "  user@example.com  ",
            Token = "  token-value  "
        };

        await InvokePrivateAsync(viewModel, "ConfirmEmailAsync");

        fakeAuth.ConfirmEmailCallCount.Should().Be(1);
        fakeAuth.LastConfirmEmail.Should().Be("user@example.com");
        fakeAuth.LastConfirmToken.Should().Be("token-value");
        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.SuccessMessage))).Should().BeTrue();
        viewModel.SuccessMessage.Should().Be(AppResources.ActivationConfirmSuccess);
        viewModel.Token.Should().Be(string.Empty);
    }

    [Fact]
    public async Task ActivationViewModel_ConfirmEmailAsync_Should_Set_Error_When_Confirmation_Fails()
    {
        var fakeAuth = new FakeActivationAuthService
        {
            ConfirmEmailResult = false
        };
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "user@example.com",
            Token = "token-value"
        };

        await InvokePrivateAsync(viewModel, "ConfirmEmailAsync");

        fakeAuth.ConfirmEmailCallCount.Should().Be(1);
        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.ActivationConfirmFailed);
    }

    [Fact]
    public async Task ActivationViewModel_ConfirmEmailAsync_Should_Set_Error_When_Service_Throws()
    {
        var fakeAuth = new FakeActivationAuthService
        {
            ConfirmEmailException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "user@example.com",
            Token = "token-value"
        };

        await InvokePrivateAsync(viewModel, "ConfirmEmailAsync");

        fakeAuth.ConfirmEmailCallCount.Should().Be(1);
        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.ActivationConfirmFailed);
    }

    [Fact]
    public async Task ActivationViewModel_ConfirmEmailAsync_Should_Set_Success_When_Confirmation_Succeeds()
    {
        var fakeAuth = new FakeActivationAuthService
        {
            ConfirmEmailResult = true
        };
        var viewModel = new ActivationViewModel(fakeAuth)
        {
            Email = "user@example.com",
            Token = "token-value"
        };

        await InvokePrivateAsync(viewModel, "ConfirmEmailAsync");

        fakeAuth.ConfirmEmailCallCount.Should().Be(1);
        fakeAuth.LastConfirmEmail.Should().Be("user@example.com");
        fakeAuth.LastConfirmToken.Should().Be("token-value");
        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.SuccessMessage))).Should().BeTrue();
        viewModel.SuccessMessage.Should().Be(AppResources.ActivationConfirmSuccess);
        viewModel.Token.Should().Be(string.Empty);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_RefreshAsync_Should_Load_Order_And_Invoice_Histories()
    {
        var orderId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            GetMyOrdersResult = Result<PagedResponse<MemberOrderSummary>>.Ok(
                new PagedResponse<MemberOrderSummary>
                {
                    Items = new[]
                    {
                        new MemberOrderSummary
                        {
                            Id = orderId,
                            OrderNumber = "ORD-1001",
                            CreatedAtUtc = DateTime.UtcNow
                        }
                    }
                }),
            GetMyInvoicesResult = Result<PagedResponse<MemberInvoiceSummary>>.Ok(
                new PagedResponse<MemberInvoiceSummary>
                {
                    Items = new[]
                    {
                        new MemberInvoiceSummary
                        {
                            Id = invoiceId,
                            OrderNumber = "INV-2001",
                            DueDateUtc = DateTime.UtcNow
                        }
                    }
                })
        };

        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel { ReferenceNumber = "SELECTED-ORDER" });
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel { ReferenceNumber = "SELECTED-INVOICE" });

        await InvokePrivateAsync(viewModel, "RefreshAsync");

        (await WaitForConditionAsync(() => viewModel.Orders.Count == 1 && viewModel.Invoices.Count == 1)).Should().BeTrue();
        fakeService.GetMyOrdersCallCount.Should().Be(1);
        fakeService.GetMyInvoicesCallCount.Should().Be(1);
        viewModel.HasSelectedOrder.Should().BeFalse();
        viewModel.HasSelectedInvoice.Should().BeFalse();
        viewModel.Orders.Should().HaveCount(1);
        viewModel.Invoices.Should().HaveCount(1);
    }

    [Fact]
    public async Task MemberCommerce_RefreshAsync_Should_Set_Error_When_Order_List_Fails()
    {
        var fakeService = new FakeMemberCommerceService
        {
            GetMyOrdersResult = Result<PagedResponse<MemberOrderSummary>>.Fail("orders failed"),
            GetMyInvoicesResult = Result<PagedResponse<MemberInvoiceSummary>>.Ok(
                new PagedResponse<MemberInvoiceSummary>
                {
                    Items = Array.Empty<MemberInvoiceSummary>()
                })
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel { ReferenceNumber = "OLD-ORDER" });
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel { ReferenceNumber = "OLD-INVOICE" });

        await InvokePrivateAsync(viewModel, "RefreshAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("orders failed");
        fakeService.GetMyOrdersCallCount.Should().Be(1);
        fakeService.GetMyInvoicesCallCount.Should().Be(1);
        viewModel.SelectedOrder.Should().BeNull();
        viewModel.SelectedInvoice.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_RefreshAsync_Should_Set_Error_When_Invoice_List_Fails()
    {
        var orderId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            GetMyOrdersResult = Result<PagedResponse<MemberOrderSummary>>.Ok(
                new PagedResponse<MemberOrderSummary>
                {
                    Items = new[]
                    {
                        new MemberOrderSummary
                        {
                            Id = orderId,
                            OrderNumber = "ORD-1005",
                            CreatedAtUtc = DateTime.UtcNow
                        }
                    }
                }),
            GetMyInvoicesResult = Result<PagedResponse<MemberInvoiceSummary>>.Fail("invoices failed")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel { ReferenceNumber = "OLD-ORDER" });
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel { ReferenceNumber = "OLD-INVOICE" });

        await InvokePrivateAsync(viewModel, "RefreshAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("invoices failed");
        fakeService.GetMyOrdersCallCount.Should().Be(1);
        fakeService.GetMyInvoicesCallCount.Should().Be(1);
        viewModel.SelectedOrder.Should().BeNull();
        viewModel.SelectedInvoice.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_RefreshAsync_Should_Set_Error_When_Order_Service_Throws()
    {
        var fakeService = new FakeMemberCommerceService
        {
            GetMyOrdersException = new InvalidOperationException("service unavailable"),
            GetMyInvoicesResult = Result<PagedResponse<MemberInvoiceSummary>>.Ok(
                new PagedResponse<MemberInvoiceSummary>
                {
                    Items = Array.Empty<MemberInvoiceSummary>()
                })
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel { ReferenceNumber = "OLD-ORDER" });
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel { ReferenceNumber = "OLD-INVOICE" });

        await InvokePrivateAsync(viewModel, "RefreshAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommerceLoadFailed);
        fakeService.GetMyOrdersCallCount.Should().Be(1);
        fakeService.GetMyInvoicesCallCount.Should().Be(0);
        viewModel.SelectedOrder.Should().BeNull();
        viewModel.SelectedInvoice.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_RefreshAsync_Should_Set_Error_When_Invoice_Service_Throws()
    {
        var orderId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            GetMyOrdersResult = Result<PagedResponse<MemberOrderSummary>>.Ok(
                new PagedResponse<MemberOrderSummary>
                {
                    Items = new[]
                    {
                        new MemberOrderSummary
                        {
                            Id = orderId,
                            OrderNumber = "ORD-1007",
                            CreatedAtUtc = DateTime.UtcNow
                        }
                    }
                }),
            GetMyInvoicesException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "RefreshAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommerceLoadFailed);
        fakeService.GetMyOrdersCallCount.Should().Be(1);
        fakeService.GetMyInvoicesCallCount.Should().Be(1);
        viewModel.SelectedOrder.Should().BeNull();
        viewModel.SelectedInvoice.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_LoadInvoiceDetailAsync_Should_Select_Invoice_And_Clear_Order()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            GetInvoiceResult = Result<MemberInvoiceDetail>.Ok(new MemberInvoiceDetail
            {
                Id = invoiceId,
                OrderNumber = "INV-2002",
                Status = "Draft",
                DueDateUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                TotalGrossMinor = 1000,
                Currency = "EUR",
                Actions = new MemberInvoiceActions
                {
                    CanRetryPayment = true
                }
            })
        };

        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel { ReferenceNumber = "OLD-ORDER" });

        await InvokePrivateAsync(viewModel, "LoadInvoiceDetailAsync", new MemberCommerceInvoiceItemViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-2002"
        });

        (await WaitForConditionAsync(() => viewModel.HasSelectedInvoice)).Should().BeTrue();
        fakeService.GetInvoiceCallCount.Should().Be(1);
        fakeService.LastGetInvoiceId.Should().Be(invoiceId);
        viewModel.SelectedInvoice.Should().NotBeNull();
        viewModel.SelectedInvoice!.Id.Should().Be(invoiceId);
        viewModel.SelectedOrder.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_LoadInvoiceDetailAsync_Should_Not_Call_Service_For_Null_Invoice_Parameter()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "LoadInvoiceDetailAsync", new object?[] { null });

        fakeService.GetInvoiceCallCount.Should().Be(0);
        fakeService.LastGetInvoiceId.Should().BeNull();
        viewModel.SelectedInvoice.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_LoadInvoiceDetailAsync_Should_Set_Error_When_Service_Response_Fails()
    {
        var fakeService = new FakeMemberCommerceService
        {
            GetInvoiceResult = Result<MemberInvoiceDetail>.Fail("invoice load failed")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        var invoiceId = Guid.NewGuid();

        await InvokePrivateAsync(viewModel, "LoadInvoiceDetailAsync", new MemberCommerceInvoiceItemViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-2003"
        });

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("invoice load failed");
        fakeService.GetInvoiceCallCount.Should().Be(1);
        fakeService.LastGetInvoiceId.Should().Be(invoiceId);
        viewModel.SelectedInvoice.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_LoadInvoiceDetailAsync_Should_Set_Error_When_Service_Throws()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            GetInvoiceException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "LoadInvoiceDetailAsync", new MemberCommerceInvoiceItemViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-3010"
        });

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommerceInvoiceDetailLoadFailed);
        fakeService.GetInvoiceCallCount.Should().Be(1);
        fakeService.LastGetInvoiceId.Should().Be(invoiceId);
        viewModel.SelectedInvoice.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_LoadOrderDetailAsync_Should_Set_Error_When_Service_Response_Fails()
    {
        var orderId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            GetOrderResult = Result<MemberOrderDetail>.Fail("order load failed")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "LoadOrderDetailAsync", new MemberCommerceOrderItemViewModel
        {
            Id = orderId,
            OrderNumber = "ORD-3001"
        });

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("order load failed");
        fakeService.GetOrderCallCount.Should().Be(1);
        fakeService.LastGetOrderId.Should().Be(orderId);
        viewModel.SelectedOrder.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_LoadOrderDetailAsync_Should_Set_Error_When_Service_Throws()
    {
        var orderId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            GetOrderException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "LoadOrderDetailAsync", new MemberCommerceOrderItemViewModel
        {
            Id = orderId,
            OrderNumber = "ORD-3011"
        });

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommerceOrderDetailLoadFailed);
        fakeService.GetOrderCallCount.Should().Be(1);
        fakeService.LastGetOrderId.Should().Be(orderId);
        viewModel.SelectedOrder.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_LoadOrderDetailAsync_Should_Not_Call_Service_When_Order_Parameter_Is_Null()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "LoadOrderDetailAsync", new object?[] { null });

        fakeService.GetOrderCallCount.Should().Be(0);
        fakeService.LastGetOrderId.Should().BeNull();
        viewModel.SelectedOrder.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_LoadOrderDetailAsync_Should_Select_Order_And_Clear_Invoice()
    {
        var orderId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            GetOrderResult = Result<MemberOrderDetail>.Ok(new MemberOrderDetail
            {
                Id = orderId,
                OrderNumber = "ORD-1002",
                Status = "Created",
                CreatedAtUtc = DateTime.UtcNow,
                Actions = new MemberOrderActions
                {
                    CanRetryPayment = true
                }
            })
        };

        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel { ReferenceNumber = "KEEPING-OLD-INVOICE" });

        await InvokePrivateAsync(viewModel, "LoadOrderDetailAsync", new MemberCommerceOrderItemViewModel
        {
            Id = orderId,
            OrderNumber = "ORD-1002"
        });

        (await WaitForConditionAsync(() => viewModel.HasSelectedOrder)).Should().BeTrue();
        fakeService.GetOrderCallCount.Should().Be(1);
        viewModel.SelectedOrder.Should().NotBeNull();
        viewModel.SelectedOrder!.Id.Should().Be(orderId);
        viewModel.SelectedInvoice.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceDocumentAsync_Should_Not_Call_Service_When_Invoice_Has_No_Document()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = "INV-NODOC",
            HasDocument = false
        });

        await InvokePrivateAsync(viewModel, "CopyInvoiceDocumentAsync");

        fakeService.DownloadInvoiceDocumentCallCount.Should().Be(0);
        fakeService.LastDownloadInvoiceId.Should().BeNull();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_CopyOrderDocumentAsync_Should_Not_Call_Service_When_SelectedOrder_Is_Null()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "CopyOrderDocumentAsync");

        fakeService.DownloadOrderDocumentCallCount.Should().Be(0);
        fakeService.LastDownloadOrderId.Should().BeNull();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_CopyOrderDocumentAsync_Should_Use_Service_And_Show_Error_On_Failed_Response()
    {
        var orderId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            DownloadOrderDocumentResult = Result<string>.Fail("document failed")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            Id = orderId,
            ReferenceNumber = "ORD-2004",
            HasDocument = true
        });

        await InvokePrivateAsync(viewModel, "CopyOrderDocumentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("document failed");
        fakeService.DownloadOrderDocumentCallCount.Should().Be(1);
        fakeService.LastDownloadOrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task MemberCommerce_CopyOrderDocumentAsync_Should_Set_Error_When_Service_Throws()
    {
        var orderId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            DownloadOrderDocumentException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            Id = orderId,
            ReferenceNumber = "ORD-3006",
            HasDocument = true
        });

        await InvokePrivateAsync(viewModel, "CopyOrderDocumentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommerceDocumentDownloadFailed);
        fakeService.DownloadOrderDocumentCallCount.Should().Be(1);
        fakeService.LastDownloadOrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task MemberCommerce_CopyOrderDocumentAsync_Should_Not_Call_Service_When_Order_Has_No_Document()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = "ORD-2005",
            HasDocument = false
        });

        await InvokePrivateAsync(viewModel, "CopyOrderDocumentAsync");

        fakeService.DownloadOrderDocumentCallCount.Should().Be(0);
        fakeService.LastDownloadOrderId.Should().BeNull();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceDocumentAsync_Should_Not_Call_Service_When_SelectedInvoice_Is_Null()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "CopyInvoiceDocumentAsync");

        fakeService.DownloadInvoiceDocumentCallCount.Should().Be(0);
        fakeService.LastDownloadInvoiceId.Should().BeNull();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceDocumentAsync_Should_Use_Service_And_Show_Error_On_Failed_Response()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            DownloadInvoiceDocumentResult = Result<string>.Fail("invoice document failed")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-2004",
            HasDocument = true
        });

        await InvokePrivateAsync(viewModel, "CopyInvoiceDocumentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("invoice document failed");
        fakeService.DownloadInvoiceDocumentCallCount.Should().Be(1);
        fakeService.LastDownloadInvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceDocumentAsync_Should_Set_Error_When_Service_Throws()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            DownloadInvoiceDocumentException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-2006",
            HasDocument = true
        });

        await InvokePrivateAsync(viewModel, "CopyInvoiceDocumentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommerceDocumentDownloadFailed);
        fakeService.DownloadInvoiceDocumentCallCount.Should().Be(1);
        fakeService.LastDownloadInvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceArchiveDocumentAsync_Should_Not_Call_Service_When_SelectedInvoice_Is_Null()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "CopyInvoiceArchiveDocumentAsync");

        fakeService.DownloadInvoiceArchiveDocumentCallCount.Should().Be(0);
        fakeService.LastDownloadInvoiceId.Should().BeNull();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceArchiveDocumentAsync_Should_Use_Service_And_Show_Error_On_Failed_Response()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            DownloadInvoiceArchiveDocumentResult = Result<string>.Fail("invoice archive failed")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-ARCHIVE"
        });

        await InvokePrivateAsync(viewModel, "CopyInvoiceArchiveDocumentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("invoice archive failed");
        fakeService.DownloadInvoiceArchiveDocumentCallCount.Should().Be(1);
        fakeService.LastDownloadInvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceArchiveDocumentAsync_Should_Set_Error_When_Service_Throws()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            DownloadInvoiceArchiveDocumentException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-ARCHIVE-2"
        });

        await InvokePrivateAsync(viewModel, "CopyInvoiceArchiveDocumentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommerceArchiveDocumentDownloadFailed);
        fakeService.DownloadInvoiceArchiveDocumentCallCount.Should().Be(1);
        fakeService.LastDownloadInvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceStructuredDataAsync_Should_Not_Call_Service_When_SelectedInvoice_Is_Null()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "CopyInvoiceStructuredDataAsync");

        fakeService.DownloadInvoiceStructuredDataCallCount.Should().Be(0);
        fakeService.LastDownloadInvoiceId.Should().BeNull();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceStructuredDataAsync_Should_Use_Service_And_Show_Error_On_Failed_Response()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            DownloadInvoiceStructuredDataResult = Result<string>.Fail("invoice structured failed")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-STRUCTURED"
        });

        await InvokePrivateAsync(viewModel, "CopyInvoiceStructuredDataAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("invoice structured failed");
        fakeService.DownloadInvoiceStructuredDataCallCount.Should().Be(1);
        fakeService.LastDownloadInvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceStructuredDataAsync_Should_Set_Error_When_Service_Throws()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            DownloadInvoiceStructuredDataException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-STRUCTURED-2"
        });

        await InvokePrivateAsync(viewModel, "CopyInvoiceStructuredDataAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommerceStructuredDownloadFailed);
        fakeService.DownloadInvoiceStructuredDataCallCount.Should().Be(1);
        fakeService.LastDownloadInvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceStructuredXmlAsync_Should_Not_Call_Service_When_SelectedInvoice_Is_Null()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "CopyInvoiceStructuredXmlAsync");

        fakeService.DownloadInvoiceStructuredXmlCallCount.Should().Be(0);
        fakeService.LastDownloadInvoiceId.Should().BeNull();
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceStructuredXmlAsync_Should_Use_Service_And_Show_Error_On_Failed_Response()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            DownloadInvoiceStructuredXmlResult = Result<string>.Fail("invoice xml failed")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-XML"
        });

        await InvokePrivateAsync(viewModel, "CopyInvoiceStructuredXmlAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("invoice xml failed");
        fakeService.DownloadInvoiceStructuredXmlCallCount.Should().Be(1);
        fakeService.LastDownloadInvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public async Task MemberCommerce_CopyInvoiceStructuredXmlAsync_Should_Set_Error_When_Service_Throws()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            DownloadInvoiceStructuredXmlException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-XML-2"
        });

        await InvokePrivateAsync(viewModel, "CopyInvoiceStructuredXmlAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommerceStructuredDownloadFailed);
        fakeService.DownloadInvoiceStructuredXmlCallCount.Should().Be(1);
        fakeService.LastDownloadInvoiceId.Should().Be(invoiceId);
    }

    [Fact]
    public async Task MemberCommerce_OpenOrderShipmentTrackingAsync_Should_Not_Open_When_Shipment_Is_Null()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "OpenOrderShipmentTrackingAsync", new object?[] { null });

        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_OpenOrderShipmentTrackingAsync_Should_Not_Open_When_Tracking_Link_Is_Missing()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(
            viewModel,
            "OpenOrderShipmentTrackingAsync",
            new MemberCommerceShipmentSummaryViewModel
            {
                TitleText = "Label",
                TrackingText = "Track: 123",
                TrackingUrl = string.Empty
            });

        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_OpenOrderShipmentTrackingAsync_Should_Not_Open_When_Tracking_Url_Is_Whitespace()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(
            viewModel,
            "OpenOrderShipmentTrackingAsync",
            new MemberCommerceShipmentSummaryViewModel
            {
                TitleText = "Label",
                TrackingText = "Track: 456",
                TrackingUrl = "   "
            });

        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_RetryOrderPaymentAsync_Should_Not_Call_Service_When_Order_Is_Not_Retryable()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = "ORD-3002",
            CanRetryPayment = false
        });

        await InvokePrivateAsync(viewModel, "RetryOrderPaymentAsync");

        fakeService.CreateOrderPaymentIntentCallCount.Should().Be(0);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_RetryOrderPaymentAsync_Should_Not_Call_Service_When_SelectedOrder_Is_Null()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "RetryOrderPaymentAsync");

        fakeService.CreateOrderPaymentIntentCallCount.Should().Be(0);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_RetryOrderPaymentAsync_Should_Set_Error_When_CheckoutUrl_Is_Missing()
    {
        var orderId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            CreateOrderPaymentIntentResult = Result<CreateStorefrontPaymentIntentResponse>.Ok(
                new CreateStorefrontPaymentIntentResponse
                {
                    CheckoutUrl = string.Empty
                })
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            Id = orderId,
            ReferenceNumber = "ORD-1004",
            CanRetryPayment = true
        });

        await InvokePrivateAsync(viewModel, "RetryOrderPaymentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommercePaymentIntentFailed);
        fakeService.CreateOrderPaymentIntentCallCount.Should().Be(1);
        fakeService.LastCreateOrderPaymentIntentOrderId.Should().Be(orderId);
        fakeService.LastCreateOrderPaymentIntentRequest!.Provider.Should().Be("HostedCheckout");
    }

    [Fact]
    public async Task MemberCommerce_RetryOrderPaymentAsync_Should_Invoke_Service_With_HostedCheckout_And_Show_Error_On_Failed_Response()
    {
        var orderId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            CreateOrderPaymentIntentResult = Result<CreateStorefrontPaymentIntentResponse>.Fail("payment failed")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            Id = orderId,
            ReferenceNumber = "ORD-1003",
            CanRetryPayment = true
        });

        await InvokePrivateAsync(viewModel, "RetryOrderPaymentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("payment failed");
        fakeService.CreateOrderPaymentIntentCallCount.Should().Be(1);
        fakeService.LastCreateOrderPaymentIntentOrderId.Should().Be(orderId);
        fakeService.LastCreateOrderPaymentIntentRequest!.Provider.Should().Be("HostedCheckout");
    }

    [Fact]
    public async Task MemberCommerce_RetryOrderPaymentAsync_Should_Set_Error_When_Service_Throws()
    {
        var orderId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            CreateOrderPaymentIntentException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedOrder", new MemberCommerceOrderDetailViewModel
        {
            Id = orderId,
            ReferenceNumber = "ORD-1006",
            CanRetryPayment = true
        });

        await InvokePrivateAsync(viewModel, "RetryOrderPaymentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommercePaymentIntentFailed);
        fakeService.CreateOrderPaymentIntentCallCount.Should().Be(1);
        fakeService.LastCreateOrderPaymentIntentOrderId.Should().Be(orderId);
        fakeService.LastCreateOrderPaymentIntentRequest!.Provider.Should().Be("HostedCheckout");
    }

    [Fact]
    public async Task MemberCommerce_RetryInvoicePaymentAsync_Should_Not_Call_Service_When_Invoice_Is_Not_Retryable()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = Guid.NewGuid(),
            ReferenceNumber = "INV-3003",
            CanRetryPayment = false
        });

        await InvokePrivateAsync(viewModel, "RetryInvoicePaymentAsync");

        fakeService.CreateInvoicePaymentIntentCallCount.Should().Be(0);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_RetryInvoicePaymentAsync_Should_Not_Call_Service_When_SelectedInvoice_Is_Null()
    {
        var fakeService = new FakeMemberCommerceService();
        var viewModel = new MemberCommerceViewModel(fakeService);

        await InvokePrivateAsync(viewModel, "RetryInvoicePaymentAsync");

        fakeService.CreateInvoicePaymentIntentCallCount.Should().Be(0);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task MemberCommerce_RetryInvoicePaymentAsync_Should_Set_Error_When_CheckoutUrl_Is_Missing()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            CreateInvoicePaymentIntentResult = Result<CreateStorefrontPaymentIntentResponse>.Ok(
                new CreateStorefrontPaymentIntentResponse
                {
                    CheckoutUrl = " "
                })
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-3004",
            CanRetryPayment = true
        });

        await InvokePrivateAsync(viewModel, "RetryInvoicePaymentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommercePaymentIntentFailed);
        fakeService.CreateInvoicePaymentIntentCallCount.Should().Be(1);
        fakeService.LastCreateInvoicePaymentIntentId.Should().Be(invoiceId);
        fakeService.LastCreateInvoicePaymentIntentRequest!.Provider.Should().Be("HostedCheckout");
    }

    [Fact]
    public async Task MemberCommerce_RetryInvoicePaymentAsync_Should_Invoke_Service_With_HostedCheckout_And_Show_Error_On_Failed_Response()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            CreateInvoicePaymentIntentResult = Result<CreateStorefrontPaymentIntentResponse>.Fail("payment failed")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-1003",
            CanRetryPayment = true
        });

        await InvokePrivateAsync(viewModel, "RetryInvoicePaymentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be("payment failed");
        fakeService.CreateInvoicePaymentIntentCallCount.Should().Be(1);
        fakeService.LastCreateInvoicePaymentIntentId.Should().Be(invoiceId);
        fakeService.LastCreateInvoicePaymentIntentRequest!.Provider.Should().Be("HostedCheckout");
    }

    [Fact]
    public async Task MemberCommerce_RetryInvoicePaymentAsync_Should_Set_Error_When_Service_Throws()
    {
        var invoiceId = Guid.NewGuid();
        var fakeService = new FakeMemberCommerceService
        {
            CreateInvoicePaymentIntentException = new InvalidOperationException("service unavailable")
        };
        var viewModel = new MemberCommerceViewModel(fakeService);
        SetPrivateField(viewModel, "_selectedInvoice", new MemberCommerceInvoiceDetailViewModel
        {
            Id = invoiceId,
            ReferenceNumber = "INV-2007",
            CanRetryPayment = true
        });

        await InvokePrivateAsync(viewModel, "RetryInvoicePaymentAsync");

        (await WaitForConditionAsync(() => !string.IsNullOrWhiteSpace(viewModel.ErrorMessage))).Should().BeTrue();
        viewModel.ErrorMessage.Should().Be(AppResources.MemberCommercePaymentIntentFailed);
        fakeService.CreateInvoicePaymentIntentCallCount.Should().Be(1);
        fakeService.LastCreateInvoicePaymentIntentId.Should().Be(invoiceId);
        fakeService.LastCreateInvoicePaymentIntentRequest!.Provider.Should().Be("HostedCheckout");
    }


    [Fact]
    public async Task MemberCommerce_OnAppearingAsync_Should_Refresh_Only_Once()
    {
        var fakeService = new FakeMemberCommerceService
        {
            GetMyOrdersResult = Result<PagedResponse<MemberOrderSummary>>.Ok(new PagedResponse<MemberOrderSummary>
            {
                Items = Array.Empty<MemberOrderSummary>()
            }),
            GetMyInvoicesResult = Result<PagedResponse<MemberInvoiceSummary>>.Ok(new PagedResponse<MemberInvoiceSummary>
            {
                Items = Array.Empty<MemberInvoiceSummary>()
            })
        };
        var viewModel = new MemberCommerceViewModel(fakeService);

        await viewModel.OnAppearingAsync();
        await viewModel.OnAppearingAsync();
        await viewModel.OnAppearingAsync();

        fakeService.GetMyOrdersCallCount.Should().Be(1);
        fakeService.GetMyInvoicesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task MemberCommerce_OnDisappearingAsync_Should_Not_Throw_When_No_Active_Operation()
    {
        var fakeService = new FakeMemberCommerceService
        {
            GetMyOrdersResult = Result<PagedResponse<MemberOrderSummary>>.Ok(new PagedResponse<MemberOrderSummary>()),
            GetMyInvoicesResult = Result<PagedResponse<MemberInvoiceSummary>>.Ok(new PagedResponse<MemberInvoiceSummary>())
        };
        var viewModel = new MemberCommerceViewModel(fakeService);

        var exception = await Record.ExceptionAsync(async () => await viewModel.OnDisappearingAsync());

        exception.Should().BeNull();
        fakeService.GetMyOrdersCallCount.Should().Be(0);
        fakeService.GetMyInvoicesCallCount.Should().Be(0);
    }

    private static Task InvokePrivateAsync(object target, string methodName, params object?[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return (Task)method!.Invoke(target, args)!;
    }

    private static async Task<bool> WaitForConditionAsync(Func<bool> condition, int timeoutMs = 1000, int pollDelayMs = 10)
    {
        var elapsed = 0;
        while (!condition() && elapsed < timeoutMs)
        {
            await Task.Delay(pollDelayMs);
            elapsed += pollDelayMs;
        }

        return condition();
    }

    private static void SetPrivateField(object target, string name, object? value)
    {
        var type = target.GetType();
        FieldInfo? field = null;
        while (type is not null)
        {
            field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field is not null)
            {
                break;
            }

            type = type.BaseType;
        }

        field.Should().NotBeNull();
        field!.SetValue(target, value);
    }

    private sealed class FakeActivationAuthService : IAuthService
    {
        public int RequestEmailConfirmationCallCount { get; private set; }
        public int ConfirmEmailCallCount { get; private set; }
        public string? LastRequestedEmail { get; private set; }
        public string? LastConfirmEmail { get; private set; }
        public string? LastConfirmToken { get; private set; }
        public Exception? RequestEmailConfirmationException { get; set; }
        public Exception? ConfirmEmailException { get; set; }
        public bool RequestEmailConfirmationResult { get; set; } = true;
        public bool ConfirmEmailResult { get; set; } = true;

        public Task<AppBootstrapResponse> LoginAsync(string email, string password, string? deviceId, CancellationToken ct)
            => Task.FromException<AppBootstrapResponse>(new InvalidOperationException("Not configured in this test."));

        public Task<AppBootstrapResponse> LoginWithExternalProviderAsync(ExternalLoginRequest request, CancellationToken ct)
            => Task.FromException<AppBootstrapResponse>(new InvalidOperationException("Not configured in this test."));

        public Task<bool> TryRefreshAsync(CancellationToken ct)
            => Task.FromResult(false);

        public Task<bool> EnsureAuthenticatedSessionAsync(CancellationToken ct)
            => Task.FromResult(false);

        public Task LogoutAsync(CancellationToken ct)
            => Task.CompletedTask;

        public Task<bool> LogoutAllAsync(CancellationToken ct)
            => Task.FromResult(false);

        public Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct)
            => Task.FromResult<RegisterResponse?>(null);

        public Task<bool> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct)
            => Task.FromResult(false);

        public Task<bool> RequestPasswordResetAsync(string email, CancellationToken ct)
            => Task.FromResult(false);

        public Task<bool> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken ct)
            => Task.FromResult(false);

        public Task<bool> RequestEmailConfirmationAsync(string email, CancellationToken ct)
        {
            RequestEmailConfirmationCallCount++;
            LastRequestedEmail = email;
            if (RequestEmailConfirmationException is not null)
            {
                throw RequestEmailConfirmationException;
            }

            return Task.FromResult(RequestEmailConfirmationResult);
        }

        public Task<bool> ConfirmEmailAsync(string email, string token, CancellationToken ct)
        {
            ConfirmEmailCallCount++;
            LastConfirmEmail = email;
            LastConfirmToken = token;
            if (ConfirmEmailException is not null)
            {
                throw ConfirmEmailException;
            }

            return Task.FromResult(ConfirmEmailResult);
        }

        public Task<BusinessInvitationPreviewResponse?> GetBusinessInvitationPreviewAsync(string token, CancellationToken ct)
            => Task.FromResult<BusinessInvitationPreviewResponse?>(null);

        public Task<AppBootstrapResponse> AcceptBusinessInvitationAsync(AcceptBusinessInvitationRequest request, string? deviceId, CancellationToken ct)
            => Task.FromException<AppBootstrapResponse>(new InvalidOperationException("Not configured in this test."));
    }

    private sealed class FakeMemberCommerceService : IMemberCommerceService
    {
        public int GetMyOrdersCallCount { get; private set; }
        public int GetOrderCallCount { get; private set; }
        public int CreateOrderPaymentIntentCallCount { get; private set; }
        public int DownloadOrderDocumentCallCount { get; private set; }
        public int GetMyInvoicesCallCount { get; private set; }
        public int GetInvoiceCallCount { get; private set; }
        public int CreateInvoicePaymentIntentCallCount { get; private set; }
        public int DownloadInvoiceDocumentCallCount { get; private set; }
        public int DownloadInvoiceArchiveDocumentCallCount { get; private set; }
        public int DownloadInvoiceStructuredDataCallCount { get; private set; }
        public int DownloadInvoiceStructuredXmlCallCount { get; private set; }
        public Guid? LastDownloadOrderId { get; private set; }
        public Guid? LastDownloadInvoiceId { get; private set; }
        public Guid? LastGetInvoiceId { get; private set; }
        public Guid? LastGetOrderId { get; private set; }
        public Guid? LastCreateOrderPaymentIntentOrderId { get; private set; }
        public CreateStorefrontPaymentIntentRequest? LastCreateOrderPaymentIntentRequest { get; private set; }
        public Guid? LastCreateInvoicePaymentIntentId { get; private set; }
        public CreateStorefrontPaymentIntentRequest? LastCreateInvoicePaymentIntentRequest { get; private set; }
        public Exception? GetMyOrdersException { get; set; }
        public Exception? GetMyInvoicesException { get; set; }
        public Exception? GetOrderException { get; set; }
        public Exception? GetInvoiceException { get; set; }
        public Exception? CreateOrderPaymentIntentException { get; set; }
        public Exception? CreateInvoicePaymentIntentException { get; set; }
        public Exception? DownloadOrderDocumentException { get; set; }
        public Exception? DownloadInvoiceDocumentException { get; set; }
        public Exception? DownloadInvoiceArchiveDocumentException { get; set; }
        public Exception? DownloadInvoiceStructuredDataException { get; set; }
        public Exception? DownloadInvoiceStructuredXmlException { get; set; }

        public Result<PagedResponse<MemberOrderSummary>> GetMyOrdersResult { get; set; } = FailResult<PagedResponse<MemberOrderSummary>>("not configured in unit test");
        public Result<MemberOrderDetail> GetOrderResult { get; set; } = FailResult<MemberOrderDetail>("not configured in unit test");
        public Result<CreateStorefrontPaymentIntentResponse> CreateOrderPaymentIntentResult { get; set; } = FailResult<CreateStorefrontPaymentIntentResponse>("not configured in unit test");
        public Result<string> DownloadOrderDocumentResult { get; set; } = FailResult<string>("not configured in unit test");
        public Result<PagedResponse<MemberInvoiceSummary>> GetMyInvoicesResult { get; set; } = FailResult<PagedResponse<MemberInvoiceSummary>>("not configured in unit test");
        public Result<MemberInvoiceDetail> GetInvoiceResult { get; set; } = FailResult<MemberInvoiceDetail>("not configured in unit test");
        public Result<CreateStorefrontPaymentIntentResponse> CreateInvoicePaymentIntentResult { get; set; } = FailResult<CreateStorefrontPaymentIntentResponse>("not configured in unit test");
        public Result<string> DownloadInvoiceDocumentResult { get; set; } = FailResult<string>("not configured in unit test");
        public Result<string> DownloadInvoiceArchiveDocumentResult { get; set; } = FailResult<string>("not configured in unit test");
        public Result<string> DownloadInvoiceStructuredDataResult { get; set; } = FailResult<string>("not configured in unit test");
        public Result<string> DownloadInvoiceStructuredXmlResult { get; set; } = FailResult<string>("not configured in unit test");

        public Task<Result<PagedResponse<MemberOrderSummary>>> GetMyOrdersAsync(int page, int pageSize, CancellationToken ct)
        {
            GetMyOrdersCallCount++;
            if (GetMyOrdersException is not null)
            {
                throw GetMyOrdersException;
            }

            return Task.FromResult(GetMyOrdersResult);
        }

        public Task<Result<MemberOrderDetail>> GetOrderAsync(Guid orderId, CancellationToken ct)
        {
            GetOrderCallCount++;
            LastGetOrderId = orderId;
            if (GetOrderException is not null)
            {
                throw GetOrderException;
            }

            return Task.FromResult(GetOrderResult);
        }

        public Task<Result<CreateStorefrontPaymentIntentResponse>> CreateOrderPaymentIntentAsync(
            Guid orderId,
            CreateStorefrontPaymentIntentRequest request,
            CancellationToken ct)
        {
            CreateOrderPaymentIntentCallCount++;
            LastCreateOrderPaymentIntentOrderId = orderId;
            LastCreateOrderPaymentIntentRequest = request;
            if (CreateOrderPaymentIntentException is not null)
            {
                throw CreateOrderPaymentIntentException;
            }

            return Task.FromResult(CreateOrderPaymentIntentResult);
        }

        public Task<Result<string>> DownloadOrderDocumentAsync(Guid orderId, CancellationToken ct)
        {
            DownloadOrderDocumentCallCount++;
            LastDownloadOrderId = orderId;
            if (DownloadOrderDocumentException is not null)
            {
                throw DownloadOrderDocumentException;
            }

            return Task.FromResult(DownloadOrderDocumentResult);
        }

        public Task<Result<PagedResponse<MemberInvoiceSummary>>> GetMyInvoicesAsync(int page, int pageSize, CancellationToken ct)
        {
            GetMyInvoicesCallCount++;
            if (GetMyInvoicesException is not null)
            {
                throw GetMyInvoicesException;
            }

            return Task.FromResult(GetMyInvoicesResult);
        }

        public Task<Result<MemberInvoiceDetail>> GetInvoiceAsync(Guid invoiceId, CancellationToken ct)
        {
            GetInvoiceCallCount++;
            LastGetInvoiceId = invoiceId;
            if (GetInvoiceException is not null)
            {
                throw GetInvoiceException;
            }

            return Task.FromResult(GetInvoiceResult);
        }

        public Task<Result<CreateStorefrontPaymentIntentResponse>> CreateInvoicePaymentIntentAsync(
            Guid invoiceId,
            CreateStorefrontPaymentIntentRequest request,
            CancellationToken ct)
        {
            CreateInvoicePaymentIntentCallCount++;
            LastCreateInvoicePaymentIntentId = invoiceId;
            LastCreateInvoicePaymentIntentRequest = request;
            if (CreateInvoicePaymentIntentException is not null)
            {
                throw CreateInvoicePaymentIntentException;
            }

            return Task.FromResult(CreateInvoicePaymentIntentResult);
        }

        public Task<Result<string>> DownloadInvoiceDocumentAsync(Guid invoiceId, CancellationToken ct)
        {
            DownloadInvoiceDocumentCallCount++;
            LastDownloadInvoiceId = invoiceId;
            if (DownloadInvoiceDocumentException is not null)
            {
                throw DownloadInvoiceDocumentException;
            }

            return Task.FromResult(DownloadInvoiceDocumentResult);
        }

        public Task<Result<string>> DownloadInvoiceArchiveDocumentAsync(Guid invoiceId, CancellationToken ct)
        {
            DownloadInvoiceArchiveDocumentCallCount++;
            LastDownloadInvoiceId = invoiceId;
            if (DownloadInvoiceArchiveDocumentException is not null)
            {
                throw DownloadInvoiceArchiveDocumentException;
            }

            return Task.FromResult(DownloadInvoiceArchiveDocumentResult);
        }

        public Task<Result<string>> DownloadInvoiceStructuredDataAsync(Guid invoiceId, CancellationToken ct)
        {
            DownloadInvoiceStructuredDataCallCount++;
            LastDownloadInvoiceId = invoiceId;
            if (DownloadInvoiceStructuredDataException is not null)
            {
                throw DownloadInvoiceStructuredDataException;
            }

            return Task.FromResult(DownloadInvoiceStructuredDataResult);
        }

        public Task<Result<string>> DownloadInvoiceStructuredXmlAsync(Guid invoiceId, CancellationToken ct)
        {
            DownloadInvoiceStructuredXmlCallCount++;
            LastDownloadInvoiceId = invoiceId;
            if (DownloadInvoiceStructuredXmlException is not null)
            {
                throw DownloadInvoiceStructuredXmlException;
            }

            return Task.FromResult(DownloadInvoiceStructuredXmlResult);
        }

        private static Result<T> FailResult<T>(string error)
            => Result<T>.Fail(error);
    }
}
