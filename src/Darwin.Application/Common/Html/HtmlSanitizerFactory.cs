using AngleSharp.Dom;
using Ganss.Xss;

namespace Darwin.Application.Common.Html
{
    /// <summary>
    /// Factory for a preconfigured HtmlSanitizer compatible with Quill output.
    /// </summary>
    public static class HtmlSanitizerFactory
    {
        public static HtmlSanitizer Create()
        {
            var s = new HtmlSanitizer();

            // Schemes
            s.AllowedSchemes.Add("data");      // allow data: for small inline images (remove if unwanted)
            s.AllowedSchemes.Remove("file");

            // Tags
            s.AllowedTags.UnionWith(new[]
            {
                "p","br","hr","span","div","blockquote","pre","code",
                "strong","b","em","i","u","s","sub","sup","small","mark",
                "h1","h2","h3","h4","h5","h6",
                "ul","ol","li",
                "a","img","figure","figcaption",
                "table","thead","tbody","tfoot","tr","th","td",
                "video","audio","source","track",
                "iframe"
            });

            // Attributes
            s.AllowedAttributes.UnionWith(new[]
            {
                "class","id","style","title","dir","lang",
                "href","target","rel",
                "src","alt","width","height","data-*",
                "border","cellpadding","cellspacing","rowspan","colspan","scope","align","valign",
                "controls","autoplay","loop","muted","poster","preload",
                "frameborder","allow","allowfullscreen","referrerpolicy"
            });

            // (Optional) Restrict inline CSS further if you want
            s.AllowedCssProperties.UnionWith(new[]
            {
                "text-align","margin","padding","border","border-collapse","width","height",
                "background-color","color","font-size","font-weight","font-style",
                "list-style-type","vertical-align"
            });

            // Limit <iframe> to safe providers
            s.PostProcessNode += (sender, e) =>
            {
                if (e.Node is IElement el && el.NodeName.Equals("IFRAME", System.StringComparison.OrdinalIgnoreCase))
                {
                    var src = el.GetAttribute("src") ?? string.Empty;

                    // allow-list of embed providers (extend as needed)
                    var allowed =
                        src.Contains("youtube.com/embed/", System.StringComparison.OrdinalIgnoreCase) ||
                        src.Contains("player.vimeo.com/video/", System.StringComparison.OrdinalIgnoreCase);

                    if (!allowed)
                    {
                        // Remove the iframe if not in allow-list
                        el.Remove();
                    }
                }
            };

            return s;
        }
    }
}
