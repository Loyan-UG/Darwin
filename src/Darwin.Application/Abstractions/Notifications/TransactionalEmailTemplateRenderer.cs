using System;
using System.Collections.Generic;

namespace Darwin.Application.Abstractions.Notifications;

internal static class TransactionalEmailTemplateRenderer
{
    public static string Render(string? template, string fallback, IReadOnlyDictionary<string, string?> placeholders)
    {
        var output = string.IsNullOrWhiteSpace(template) ? fallback : template;
        foreach (var pair in placeholders)
        {
            output = output.Replace("{" + pair.Key + "}", pair.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return output;
    }
}
