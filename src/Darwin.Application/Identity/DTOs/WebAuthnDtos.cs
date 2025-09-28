using System;

namespace Darwin.Application.Identity.DTOs
{
    /// <summary>Begin registration inputs.</summary>
    public sealed class WebAuthnBeginRegisterDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;   // usually Email
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>Begin registration result.</summary>
    public sealed class WebAuthnBeginRegisterResult
    {
        public Guid ChallengeTokenId { get; set; }             // persisted token id to correlate finish
        public string OptionsJson { get; set; } = "{}";         // send to browser
    }

    /// <summary>Finish registration input.</summary>
    public sealed class WebAuthnFinishRegisterDto
    {
        public Guid ChallengeTokenId { get; set; }              // references the stored challenge
        public string ClientResponseJson { get; set; } = "{}";  // attestation response from browser
    }

    /// <summary>Begin login inputs.</summary>
    public sealed class WebAuthnBeginLoginDto
    {
        public Guid UserId { get; set; }
    }

    public sealed class WebAuthnBeginLoginResult
    {
        public Guid ChallengeTokenId { get; set; }
        public string OptionsJson { get; set; } = "{}";
    }

    /// <summary>Finish login input.</summary>
    public sealed class WebAuthnFinishLoginDto
    {
        public Guid ChallengeTokenId { get; set; }
        public string ClientResponseJson { get; set; } = "{}";
    }
}
