using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Darwin.Web.TagHelpers
{
    /// <summary>
    /// Adds the Bootstrap "active" class to a navigation link when the current request
    /// matches the target route (area/controller/action) of the link.
    /// Usage:
    ///   <active-nav class="list-group-item list-group-item-action"
    ///               asp-area="Admin" asp-controller="Products" asp-action="Index">
    ///       <i class="fa-solid fa-box-open me-2"></i> Products
    ///   </active-nav>
    /// Notes:
    /// - Falls back to controller-only match when action is not specified.
    /// - Comparisons are case-insensitive.
    /// </summary>
    [HtmlTargetElement("active-nav", Attributes = "asp-controller", TagStructure = TagStructure.NormalOrSelfClosing)]
    public sealed class ActiveNavLinkTagHelper : TagHelper
    {
        private const string ActiveClass = "active";

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext? ViewContext { get; set; }

        /// <summary>Area segment to link to (optional).</summary>
        [HtmlAttributeName("asp-area")]
        public string? Area { get; set; }

        /// <summary>Controller segment to link to (required).</summary>
        [HtmlAttributeName("asp-controller")]
        public string Controller { get; set; } = string.Empty;

        /// <summary>Action segment to link to (optional; defaults to Index when omitted).</summary>
        [HtmlAttributeName("asp-action")]
        public string? Action { get; set; }

        /// <summary>Additional CSS classes to render on the anchor tag.</summary>
        [HtmlAttributeName("class")]
        public string? Class { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Render as a standard <a> element so we keep natural semantics/tab order.
            output.TagName = "a";
            output.TagMode = TagMode.StartTagAndEndTag;

            // Compute href via MVC anchor helpers (leave to AnchorTagHelper pipeline)
            output.Attributes.SetAttribute("asp-area", Area);
            output.Attributes.SetAttribute("asp-controller", Controller);
            if (!string.IsNullOrWhiteSpace(Action))
                output.Attributes.SetAttribute("asp-action", Action);

            // Compose CSS classes (append "active" if route matches).
            var css = string.IsNullOrWhiteSpace(Class) ? string.Empty : Class!;
            if (IsActiveRoute())
                css = string.IsNullOrWhiteSpace(css) ? ActiveClass : $"{css} {ActiveClass}";
            output.Attributes.SetAttribute("class", css);
        }

        /// <summary>
        /// Determines whether the current request route matches the link's route.
        /// </summary>
        private bool IsActiveRoute()
        {
            if (ViewContext is null) return false;

            var currentArea = (string?)ViewContext.RouteData.Values["area"];
            var currentController = (string?)ViewContext.RouteData.Values["controller"];
            var currentAction = (string?)ViewContext.RouteData.Values["action"];

            // Normalize for comparison
            string norm(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();

            var wantArea = norm(Area);
            var wantController = norm(Controller);
            var wantAction = norm(Action ?? "index");

            var curArea = norm(currentArea);
            var curController = norm(currentController);
            var curAction = norm(currentAction);

            // Exact match (area/controller/action)
            if (curController == wantController && curAction == wantAction)
            {
                // When an area is specified, ensure area also matches.
                if (!string.IsNullOrEmpty(wantArea))
                    return curArea == wantArea;
                return true;
            }

            // Fallback: match only controller (common for index pages)
            if (string.IsNullOrEmpty(Action) && curController == wantController)
            {
                if (!string.IsNullOrEmpty(wantArea))
                    return curArea == wantArea;
                return true;
            }

            return false;
        }
    }
}
