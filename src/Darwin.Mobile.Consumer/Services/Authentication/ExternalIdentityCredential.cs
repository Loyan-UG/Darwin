namespace Darwin.Mobile.Consumer.Services.Authentication;

/// <summary>
/// Represents a short-lived provider credential that can be exchanged for Darwin tokens.
/// </summary>
/// <param name="Provider">Provider key understood by the WebApi, for example Google.</param>
/// <param name="IdToken">Provider-issued ID token. This value must never be logged or persisted.</param>
public sealed record ExternalIdentityCredential(string Provider, string IdToken);
