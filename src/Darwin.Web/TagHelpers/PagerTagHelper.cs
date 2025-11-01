using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Darwin.Web.TagHelpers
{
    /// <summary>
    /// Renders a Bootstrap pagination component + page-size selector.
    /// - Keeps route values via asp-route-*.
    /// - Defaults page-size options to 10/20/50/100 globally (no VM list needed).
    /// - Deep-link friendly (page/pageSize/filter are kept in the URL).
    /// </summary>
    [HtmlTargetElement("pager", TagStructure = TagStructure.NormalOrSelfClosing)]
    public sealed class PagerTagHelper : TagHelper
    {
        private readonly IUrlHelperFactory _urlHelperFactory;

        [ViewContext]
        public ViewContext? ViewContext { get; set; }

        [HtmlAttributeName("asp-area")]
        public string? Area { get; set; }

        [HtmlAttributeName("asp-controller")]
        public string? Controller { get; set; }

        [HtmlAttributeName("asp-action")]
        public string? Action { get; set; }

        /// <summary>
        /// Extra route values collected from attributes like asp-route-query="foo".
        /// Razor may leave this null if no asp-route-* is present; always null-guard.
        /// </summary>
        [HtmlAttributeName(DictionaryAttributePrefix = "asp-route-")]
        public Dictionary<string, string?> RouteValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>1-based current page.</summary>
        [HtmlAttributeName("page")]
        public int Page { get; set; } = 1;

        /// <summary>Items per page.</summary>
        [HtmlAttributeName("page-size")]
        public int PageSize { get; set; } = 20;

        /// <summary>Total item count across all pages.</summary>
        [HtmlAttributeName("total")]
        public long Total { get; set; }

        /// <summary>How many numeric buttons to show (prefer odd numbers).</summary>
        [HtmlAttributeName("window")]
        public int Window { get; set; } = 5;

        /// <summary>Global page-size candidates.</summary>
        private static readonly int[] DefaultPageSizes = { 10, 20, 50, 100 };

        public PagerTagHelper(IUrlHelperFactory urlHelperFactory) => _urlHelperFactory = urlHelperFactory;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Nothing to page? Render only page-size selector if you want; or suppress entirely.
            if (Total <= 0)
            {
                output.SuppressOutput();
                return;
            }

            // ---- Null guards for framework-injected services/props ----
            var viewContext = ViewContext ?? throw new InvalidOperationException("PagerTagHelper requires a non-null ViewContext.");
            var url = _urlHelperFactory.GetUrlHelper(viewContext);

            var routeValuesInput = RouteValues ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            // Normalize Page/PageSize and compute total pages
            var pageSize = PageSize <= 0 ? 20 : PageSize;
            var totalPages = (int)((Total + pageSize - 1) / pageSize);
            var page = Math.Max(1, Math.Min(Page, Math.Max(1, totalPages)));

            // Base route (area/controller/action + extra)
            var baseRoute = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(Area)) baseRoute["area"] = Area;
            if (!string.IsNullOrEmpty(Controller)) baseRoute["controller"] = Controller;
            baseRoute["action"] = string.IsNullOrEmpty(Action) ? "Index" : Action;

            foreach (var kv in routeValuesInput)
                baseRoute[kv.Key] = kv.Value;

            string BuildUrl(int targetPage, int? targetPageSize = null)
            {
                var rv = new Dictionary<string, object?>(baseRoute, StringComparer.OrdinalIgnoreCase)
                {
                    ["page"] = targetPage,
                    ["pageSize"] = targetPageSize ?? pageSize
                };
                var action = (baseRoute["action"] ?? "Index")!.ToString()!;
                return url.Action(action, rv) ?? "#";
            }

            // Outer wrapper
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.SetAttribute("class", "d-flex flex-column flex-md-row align-items-md-center gap-2");

            var sb = new StringBuilder();

            // === Page-size selector (left) ===
            sb.AppendLine("<form method=\"get\" class=\"d-inline-block\">");

            // Preserve existing asp-route-* as hidden inputs
            foreach (var kv in routeValuesInput)
            {
                var key = System.Net.WebUtility.HtmlEncode(kv.Key);
                var val = System.Net.WebUtility.HtmlEncode(kv.Value ?? string.Empty);
                sb.AppendLine($"  <input type=\"hidden\" name=\"{key}\" value=\"{val}\" />");
            }

            // Keep current page unless size changes (JS sets it to 1 on change)
            sb.AppendLine($"  <input type=\"hidden\" name=\"page\" value=\"{page}\" />");

            sb.AppendLine("  <div class=\"input-group input-group-sm\" style=\"width: 220px;\">");
            sb.AppendLine("    <span class=\"input-group-text\">Page size</span>");
            sb.AppendLine("    <select name=\"pageSize\" class=\"form-select\" id=\"pager-pageSize-select\">");

            // (This loop was where the exception pointed; now nothing here can be null.)
            foreach (var size in DefaultPageSizes)
            {
                var selected = size == pageSize ? " selected" : string.Empty;
                sb.AppendLine($"      <option value=\"{size}\"{selected}>{size}</option>");
            }

            sb.AppendLine("    </select>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</form>");

            // Auto-submit on size change + reset page=1
            sb.AppendLine("<script>");
            sb.AppendLine("  (function(){");
            sb.AppendLine("    const sel = document.getElementById('pager-pageSize-select');");
            sb.AppendLine("    if(!sel) return;");
            sb.AppendLine("    sel.addEventListener('change', function(){");
            sb.AppendLine("      const form = sel.closest('form');");
            sb.AppendLine("      if(!form) return;");
            sb.AppendLine("      const pageInput = form.querySelector('input[name=\"page\"]');");
            sb.AppendLine("      if(pageInput) pageInput.value = '1';");
            sb.AppendLine("      form.submit();");
            sb.AppendLine("    });");
            sb.AppendLine("  })();");
            sb.AppendLine("</script>");

            // === Pager (right) ===
            sb.AppendLine("<nav class=\"ms-md-auto\" aria-label=\"Page navigation\">");
            sb.AppendLine("  <ul class=\"pagination pagination-sm mb-0\">");

            void PageItem(string label, int target, bool disabled, bool active = false, string? aria = null)
            {
                var liClass = "page-item";
                if (disabled) liClass += " disabled";
                if (active) liClass += " active";

                var href = disabled ? "#" : BuildUrl(target);
                var ariaAttr = string.IsNullOrEmpty(aria) ? "" : $" aria-label=\"{aria}\"";
                sb.Append($"    <li class=\"{liClass}\">");
                sb.Append($"<a class=\"page-link\" href=\"{href}\"{ariaAttr}>{label}</a>");
                sb.AppendLine("</li>");
            }

            // First / Prev
            PageItem("« First", 1, page == 1, aria: "First");
            PageItem("‹ Prev", Math.Max(1, page - 1), page == 1, aria: "Previous");

            // Numeric window
            var half = Math.Max(0, (Window - 1) / 2);
            var start = Math.Max(1, page - half);
            var end = Math.Min(Math.Max(1, (int)((Total + pageSize - 1) / pageSize)), start + Window - 1);
            start = Math.Max(1, Math.Min(start, Math.Max(1, end - Window + 1)));

            for (int i = start; i <= end; i++)
                PageItem(i.ToString(), i, disabled: false, active: i == page, aria: i == page ? "Current" : null);

            // Next / Last
            var totalPagesSafe = Math.Max(1, (int)((Total + pageSize - 1) / pageSize));
            PageItem("Next ›", Math.Min(totalPagesSafe, page + 1), page == totalPagesSafe, aria: "Next");
            PageItem("Last »", totalPagesSafe, page == totalPagesSafe, aria: "Last");

            sb.AppendLine("  </ul>");
            sb.AppendLine("</nav>");

            output.Content.SetHtmlContent(sb.ToString());
        }
    }
}
