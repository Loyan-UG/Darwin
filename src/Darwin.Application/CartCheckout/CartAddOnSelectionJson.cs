using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Darwin.Application.CartCheckout
{
    /// <summary>
    /// Canonicalizes cart add-on selection JSON so line identity does not depend on client ordering.
    /// </summary>
    public static class CartAddOnSelectionJson
    {
        public static string NormalizeIds(IEnumerable<Guid>? selectedValueIds)
        {
            var normalized = (selectedValueIds ?? Enumerable.Empty<Guid>())
                .Distinct()
                .OrderBy(id => id)
                .ToArray();

            return JsonSerializer.Serialize(normalized);
        }

        public static string? NormalizeJsonOrNull(string? selectedValueIdsJson)
        {
            if (string.IsNullOrWhiteSpace(selectedValueIdsJson))
            {
                return null;
            }

            try
            {
                var selectedValueIds = JsonSerializer.Deserialize<List<Guid>>(selectedValueIdsJson);
                return NormalizeIds(selectedValueIds);
            }
            catch (JsonException)
            {
                return selectedValueIdsJson.Trim();
            }
        }
    }
}
