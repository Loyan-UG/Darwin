namespace Darwin.Infrastructure.Notifications.Smtp
{
    /// <summary>
    ///     Options for SMTP mail delivery. Bind from configuration section "Email:Smtp".
    /// </summary>
    public sealed class SmtpEmailOptions
    {
        /// <summary>SMTP server host name (e.g., "smtp.contoso.com").</summary>
        public string Host { get; set; } = "localhost";

        /// <summary>SMTP server port (e.g., 587 for STARTTLS, 465 for implicit TLS).</summary>
        public int Port { get; set; } = 25;

        /// <summary>Whether to enable SSL/TLS for the connection.</summary>
        public bool EnableSsl { get; set; } = true;

        /// <summary>Optional username; if empty, no credentials are used.</summary>
        public string? Username { get; set; }

        /// <summary>Password for <see cref="Username"/>; leave empty when using anonymous relay.</summary>
        public string? Password { get; set; }

        /// <summary>Default From address to appear in outgoing emails.</summary>
        public string FromAddress { get; set; } = "no-reply@darwin.com";

        /// <summary>Default From display name (human-friendly).</summary>
        public string FromDisplayName { get; set; } = "Darwin";
    }
}
