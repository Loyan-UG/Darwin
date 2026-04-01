using System.ComponentModel.DataAnnotations;
using Darwin.WebAdmin;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Localization;

namespace Darwin.WebAdmin.Localization
{
    /// <summary>
    /// Resolves DisplayAttribute names through the shared ASP.NET Core resource pipeline
    /// so existing view-model metadata can move to resx files incrementally.
    /// </summary>
    public sealed class SharedDisplayMetadataProvider : IDisplayMetadataProvider
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public SharedDisplayMetadataProvider(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
        }

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            var displayAttribute = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
            if (displayAttribute is null || string.IsNullOrWhiteSpace(displayAttribute.Name))
            {
                return;
            }

            var resourceKey = displayAttribute.Name!;
            context.DisplayMetadata.DisplayName = () => _localizer[resourceKey];
        }
    }
}
