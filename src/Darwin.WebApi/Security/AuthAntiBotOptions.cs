using System;

namespace Darwin.WebApi.Security
{
    public sealed class AuthAntiBotOptions
    {
        public bool Enabled { get; set; } = true;
        public bool RequireChallengeToken { get; set; } = true;
        public int MinimumFormSeconds { get; set; } = 2;
        public int ChallengeTokenMaxAgeSeconds { get; set; } = 900;

        public TimeSpan MinimumFormAge => TimeSpan.FromSeconds(Math.Clamp(MinimumFormSeconds, 0, 60));
        public TimeSpan ChallengeTokenMaxAge => TimeSpan.FromSeconds(Math.Clamp(ChallengeTokenMaxAgeSeconds, 60, 3600));
    }
}
