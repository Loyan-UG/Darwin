using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Renders a navigation link that automatically adds the Bootstrap “active” class when the
/// current request matches the target (area/controller/action).  Instead of relying on the
/// built‑in AnchorTagHelper to build the href, this TagHelper uses IUrlHelper to generate
/// the link so that no asp-* attributes are emitted in the final HTML.
/// Example usage:
///   <active-nav class="list-group-item" asp-area="Admin" asp-controller="Products" asp-action="Index">
///     <i class="fa-solid fa-box-open me-2"></i> Products
///   </active-nav>
/// </summary>
[HtmlTargetElement("active-nav", Attributes = "asp-controller", TagStructure = TagStructure.NormalOrSelfClosing)]
public sealed class ActiveNavLinkTagHelper : TagHelper
{
    private readonly IUrlHelperFactory _urlHelperFactory;

    public ActiveNavLinkTagHelper(IUrlHelperFactory urlHelperFactory)
    {
        _urlHelperFactory = urlHelperFactory;
    }

    /// <summary>
    /// Provides contextual information about the current HTTP request.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    /// <summary>
    /// Optional area segment for the route.
    /// </summary>
    [HtmlAttributeName("asp-area")]
    public string? Area { get; set; }

    /// <summary>
    /// Required controller segment for the route.
    /// </summary>
    [HtmlAttributeName("asp-controller")]
    public string Controller { get; set; } = string.Empty;

    /// <summary>
    /// Optional action segment for the route; defaults to “Index” when omitted.
    /// </summary>
    [HtmlAttributeName("asp-action")]
    public string? Action { get; set; }

    /// <summary>
    /// Additional CSS classes for the anchor element.
    /// </summary>
    [HtmlAttributeName("class")]
    public string? Class { get; set; }

    /// <summary>
    /// Generates the anchor element, its href, and adds an “active” class when appropriate.
    /// </summary>
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Always render as a standard <a> tag.
        output.TagName = "a";
        output.TagMode = TagMode.StartTagAndEndTag;

        // Use IUrlHelper to build the href.
        var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
        var routeValues = new RouteValueDictionary();
        if (!string.IsNullOrEmpty(Area))
            routeValues["area"] = Area;

        var actionName = string.IsNullOrEmpty(Action) ? "Index" : Action;
        var href = urlHelper.Action(actionName, Controller, routeValues);
        if (!string.IsNullOrEmpty(href))
            output.Attributes.SetAttribute("href", href);

        // Build the CSS class string and append "active" if the link matches the current route.
        var css = Class ?? string.Empty;
        if (IsActiveRoute())
            css = string.IsNullOrWhiteSpace(css) ? "active" : $"{css} active";
        if (!string.IsNullOrEmpty(css))
            output.Attributes.SetAttribute("class", css);

        // Remove any leftover asp-* attributes to avoid leaking them into the rendered HTML.
        output.Attributes.RemoveAll("asp-area");
        output.Attributes.RemoveAll("asp-controller");
        output.Attributes.RemoveAll("asp-action");
    }

    /// <summary>
    /// Determines whether the current request's area, controller and action match the target.
    /// </summary>
    private bool IsActiveRoute()
    {
        var currentArea = (string?)ViewContext.RouteData.Values["area"];
        var currentController = (string?)ViewContext.RouteData.Values["controller"];
        var currentAction = (string?)ViewContext.RouteData.Values["action"];

        static string Normalize(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();

        var wantArea = Normalize(Area);
        var wantController = Normalize(Controller);
        var wantAction = Normalize(Action ?? "Index");

        var curArea = Normalize(currentArea);
        var curController = Normalize(currentController);
        var curAction = Normalize(currentAction);

        // Exact match on controller/action (+ area when provided)
        if (curController == wantController && curAction == wantAction)
        {
            if (!string.IsNullOrEmpty(wantArea))
                return curArea == wantArea;
            return true;
        }

        // When asp-action is omitted, a match on controller is sufficient.
        if (string.IsNullOrEmpty(Action) && curController == wantController)
        {
            if (!string.IsNullOrEmpty(wantArea))
                return curArea == wantArea;
            return true;
        }

        return false;
    }
}
