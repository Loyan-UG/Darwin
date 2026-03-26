using System;
using System.Collections.Generic;

namespace Darwin.WebAdmin.ViewModels.Catalog
{
    public sealed class ProductEditVm : ProductEditorVm
    {
        public Guid Id { get; set; }
        public byte[]? RowVersion { get; set; }
    }
}
