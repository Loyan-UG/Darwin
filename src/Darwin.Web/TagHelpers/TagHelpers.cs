using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Darwin.Web.TagHelpers
{
    /// <summary>
    /// Renders a small help icon that shows a Bootstrap popover with field help text.
    /// Usage:
    ///   <field-help title="Slug" content="URL-friendly slug. Use lowercase letters, digits, and hyphens." />
    /// Optional attributes: placement="right|left|top|bottom" (default: right)
    /// </summary>
    [HtmlTargetElement("field-help", TagStructure = TagStructure.NormalOrSelfClosing)]
    public sealed class FieldHelpTagHelper : TagHelper
    {
        /// <summary>Popover title.</summary>
        public string? Title { get; set; }

        /// <summary>Popover body content (plain text or basic HTML; keep it safe).</summary>
        public string? Content { get; set; }

        /// <summary>Bootstrap popover placement: top, right, bottom, left.</summary>
        public string Placement { get; set; } = "right";

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "button";
            output.TagMode = TagMode.StartTagAndEndTag;

            // Render a minimalistic round "i" button (no dependency on icon packs).
            output.Attributes.SetAttribute("type", "button");
            output.Attributes.SetAttribute("class", "btn btn-sm btn-outline-secondary ms-2 rounded-circle lh-1");
            output.Attributes.SetAttribute("style", "width:1.75rem;height:1.75rem;padding:0;");

            // Bootstrap popover data attributes
            output.Attributes.SetAttribute("data-bs-toggle", "popover");
            output.Attributes.SetAttribute("data-bs-trigger", "focus");
            output.Attributes.SetAttribute("data-bs-placement", Placement);
            if (!string.IsNullOrWhiteSpace(Title))
                output.Attributes.SetAttribute("title", Title);
            if (!string.IsNullOrWhiteSpace(Content))
            {
                output.Attributes.SetAttribute("data-bs-content", Content);
                output.Attributes.SetAttribute("data-bs-html", "true");
            }

            // Button inner text: "i"
            output.Content.SetHtmlContent("<span aria-hidden=\"true\" style=\"font-weight:600;\">i</span><span class=\"visually-hidden\">Help</span>");
        }
    }
}
