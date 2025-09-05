using AutoMapper;
using Darwin.Application.Catalog.DTOs;
using Darwin.Domain.Entities.Catalog;
using Darwin.Domain.Enums;

namespace Darwin.Application.Catalog.Mapping
{
    public sealed class CatalogProfile : Profile
    {
        public CatalogProfile()
        {
            CreateMap<ProductTranslationDto, ProductTranslation>();
            CreateMap<ProductVariantCreateDto, ProductVariant>();

            CreateMap<ProductCreateDto, Product>()
                .ForMember(d => d.Translations, cfg => cfg.MapFrom(s => s.Translations))
                .ForMember(d => d.Variants, cfg => cfg.MapFrom(s => s.Variants))
                // Let AutoMapper map everything else, then set Kind imperatively:
                .AfterMap((src, dest) =>
                {
                    dest.Kind = ParseKind(src.Kind);
                });
        }

        private static ProductKind ParseKind(string? value)
        {
            return value?.Trim().ToLowerInvariant() switch
            {
                "simple" => ProductKind.Simple,
                "variant" => ProductKind.Variant,
                "bundle" => ProductKind.Bundle,
                "digital" => ProductKind.Digital,
                "service" => ProductKind.Service,
                _ => ProductKind.Simple
            };
        }
    }
}
