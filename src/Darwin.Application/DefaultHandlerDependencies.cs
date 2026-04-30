using System.Globalization;
using Darwin.Application.Abstractions.Services;
using Microsoft.Extensions.Localization;

namespace Darwin.Application;

internal static class DefaultHandlerDependencies
{
    public static readonly IClock DefaultClock = new FallbackClock();

    public static readonly IStringLocalizer<ValidationResource> DefaultLocalizer = new FallbackValidationLocalizer();

    private sealed class FallbackClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    private sealed class FallbackValidationLocalizer : IStringLocalizer<ValidationResource>
    {
        public LocalizedString this[string name] => new LocalizedString(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, string.Format(CultureInfo.InvariantCulture, name, arguments), resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Array.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(CultureInfo culture) => this;
    }
}
