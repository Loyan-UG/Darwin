using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Darwin.Web.TagHelpers
{
    /// <summary>
    /// Renders a Bootstrap pagination component based on page, page size, and total item count.
    /// Preserves route values (area/controller/action) and accepts additional route values via asp-route-*.
    /// Usage example:
    ///   <pager page="Model.Page"
    ///          page-size="Model.PageSize"
    ///          total="Model.Total"
    ///          asp-area="Admin"
    ///          asp-controller="Users"
    ///          asp-action="Index"
    ///          asp-route-query="@Model.Query" />
    /// 
    /// Notes:
    /// - The TagHelper calculates total pages and emits a compact Bootstrap pager.
    /// - It keeps the current page highlighted and disables prev/next on edges.
    /// - Deep-link friendly: values are encoded into the URL, so browser back/forward works as expected.
    /// </summary>
    [HtmlTargetElement("pager", TagStructure = TagStructure.NormalOrSelfClosing)]
    public sealed class PagerTagHelper : TagHelper
    {
        private readonly IUrlHelperFactory _urlHelperFactory;

        /// <summary>Current MVC view context (injected by Razor).</summary>
        [ViewContext]
        public ViewContext ViewContext { get; set; } = default!;

        /// <summary>Zero or one area name. When omitted, default area is used.</summary>
        [HtmlAttributeName("asp-area")]
        public string? Area { get; set; }

        /// <summary>Target controller name.</summary>
        [HtmlAttributeName("asp-controller")]
        public string? Controller { get; set; }

        /// <summary>Target action name (defaults to "Index").</summary>
        [HtmlAttributeName("asp-action")]
        public string? Action { get; set; }

        /// <summary>Additional custom route values via attribute prefix "asp-route-".</summary>
        [HtmlAttributeName(DictionaryAttributePrefix = "asp-route-")]
        public Dictionary<string, string?> RouteValues { get; set; } = new();

        /// <summary>Current 1-based page number.</summary>
        [HtmlAttributeName("page")]
        public int Page { get; set; } = 1;

        /// <summary>Number of items per page.</summary>
        [HtmlAttributeName("page-size")]
        public int PageSize { get; set; } = 20;

        /// <summary>Total number of items across all pages.</summary>
        [HtmlAttributeName("total")]
        public long Total { get; set; }

        /// <summary>How many numeric buttons to show around the current page (odd number recommended).</summary>
        [HtmlAttributeName("window")]
        public int Window { get; set; } = 5;

        public PagerTagHelper(IUrlHelperFactory urlHelperFactory)
        {
            _urlHelperFactory = urlHelperFactory;
        }

        /// <summary>
        /// Builds a Bootstrap pager with First/Prev/numbered/Next/Last links.
        /// All links preserve query-like route values (deep-link friendly).
        /// </summary>
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var url = _urlHelperFactory.GetUrlHelper(ViewContext);

            output.TagName = "nav";
            output.TagMode = TagMode.StartTagAndEndTag;

            var totalPages = Math.Max(1, (int)((Total + PageSize - 1) / PageSize));
            var current = Math.Clamp(Page, 1, totalPages);
            var win = Math.Max(3, Window | 1); // ensure odd number, min 3
            var half = win / 2;

            var start = Math.Max(1, current - half);
            var end = Math.Min(totalPages, start + win - 1);
            if (end - start + 1 < win)
                start = Math.Max(1, end - win + 1);

            var ul = new StringBuilder();
            ul.Append("<ul class=\"pagination justify-content-center\">");

            // helper to build a li>a
            string PageLink(string text, int targetPage, bool disabled = false, bool active = false)
            {
                var liClass = "page-item";
                if (disabled) liClass += " disabled";
                if (active) liClass += " active";

                var rv = new RouteValueDictionary(RouteValues);
                if (!string.IsNullOrWhiteSpace(Area)) rv["area"] = Area!;
                rv["page"] = targetPage;
                rv["pageSize"] = PageSize;

                var href = url.Action(Action ?? "Index", Controller, rv);

                return $"<li class=\"{liClass}\"><a class=\"page-link\" href=\"{(disabled ? "#" : href)}\">{text}</a></li>";
            }

            // First & Prev
            ul.Append(PageLink("« First", 1, disabled: current == 1));
            ul.Append(PageLink("‹ Prev", Math.Max(1, current - 1), disabled: current == 1));

            // Numbered window
            for (int i = start; i <= end; i++)
                ul.Append(PageLink(i.ToString(), i, active: i == current));

            // Next & Last
            ul.Append(PageLink("Next ›", Math.Min(totalPages, current + 1), disabled: current == totalPages));
            ul.Append(PageLink("Last »", totalPages, disabled: current == totalPages));

            ul.Append("</ul>");
            output.Content.SetHtmlContent(ul.ToString());
        }
    }
}
