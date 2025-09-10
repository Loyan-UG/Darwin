using AngleSharp.Dom;
using Ganss.Xss;

namespace Darwin.Application.Common.Html
{
    /// <summary>
    ///     Factory for creating preconfigured HTML sanitizers used to clean user-supplied rich text content
    ///     (e.g., CMS pages, product descriptions) before persistence or rendering.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Security:
    ///         <list type="bullet">
    ///             <item>Mitigates XSS by allowing only a curated set of tags, attributes, and URI schemes.</item>
    ///             <item>Optionally rewrites or filters <c>&lt;a&gt;</c> and <c>&lt;img&gt;</c> attributes (nofollow, target, data-uris).</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Extensibility:
    ///         <list type="bullet">
    ///             <item>Expose a single place to adjust allowed elements across the app.</item>
    ///             <item>Consider environment-specific policies (e.g., stricter in Public, more relaxed for Admin previews).</item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Usage:
    ///         Instantiate the sanitizer via this factory in Application handlers right before saving HTML to the database.
    ///     </para>
    /// </remarks>
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
