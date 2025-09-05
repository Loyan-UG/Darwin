using System;
using System.Collections.Generic;

namespace Darwin.Web.Areas.Admin.ViewModels.Catalog
{
    public sealed class ProductEditVm
    {
        public Guid Id { get; set; }
        public Guid? BrandId { get; set; }
        public Guid? PrimaryCategoryId { get; set; }
        public string Kind { get; set; } = "Simple";
        public byte[]? RowVersion { get; set; }

        public List<ProductTranslationVm> Translations { get; set; } = new();
        public List<ProductVariantCreateVm> Variants { get; set; } = new();
    }
}
