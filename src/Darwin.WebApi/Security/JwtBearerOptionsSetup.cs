using System;
using Darwin.WebApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Darwin.WebApi.Security
{
    /// <summary>
    /// Configures JwtBearerOptions using parameters sourced from SiteSetting.
    /// This avoids relying on appsettings for issuer/audience/keys and keeps
    /// validation consistent with issuing (JwtTokenService).
    /// </summary>
    public sealed class JwtBearerOptionsSetup : IConfigureNamedOptions<JwtBearerOptions>
    {
        private readonly JwtSigningParametersProvider _provider;

        /// <summary>
        /// Initializes a new instance of <see cref="JwtBearerOptionsSetup"/>.
        /// </summary>
        /// <param name="provider">Provider that reads JWT parameters from SiteSetting.</param>
        public JwtBearerOptionsSetup(JwtSigningParametersProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Configures options for the default JwtBearer scheme.
        /// </summary>
        public void Configure(JwtBearerOptions options)
        {
            Configure(Options.DefaultName, options);
        }

        /// <summary>
        /// Configures options for a named JwtBearer scheme.
        /// </summary>
        public void Configure(string? name, JwtBearerOptions options)
        {
            if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
            {
                return;
            }

            var p = _provider.GetParameters();

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = p.Issuer,

                ValidateAudience = true,
                ValidAudience = p.Audience,

                ValidateIssuerSigningKey = true,

                // Use resolver so both current and previous keys are accepted (rolling rotation).
                IssuerSigningKeyResolver = (_, _, _, _) => p.SigningKeys,

                RequireExpirationTime = true,
                ValidateLifetime = true,

                ClockSkew = p.ClockSkew
            };
        }
    }
}
