using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;

namespace Darwin.WebAdmin.Localization
{
    public sealed class ConfigureDisplayMetadataLocalization : IConfigureOptions<MvcOptions>
    {
        private readonly IDisplayMetadataProvider _displayMetadataProvider;

        public ConfigureDisplayMetadataLocalization(IDisplayMetadataProvider displayMetadataProvider)
        {
            _displayMetadataProvider = displayMetadataProvider;
        }

        public void Configure(MvcOptions options)
        {
            options.ModelMetadataDetailsProviders.Add(_displayMetadataProvider);
        }
    }
}
