using AngleSharp.Dom;
using Ganss.Xss;
using System;

namespace Darwin.Application.Common.Html
{
    /// <summary>
    /// Factory that produces configured <see cref="IHtmlSanitizer"/> instances using Ganss.Xss.
    /// Centralizes allowed tags/attributes/schemes and post-processing rules.
    /// </summary>
    public static class HtmlSanitizerFactory
    {
        /// <summary>
        /// Creates a sanitizer tailored for Quill-like rich text and wraps it in an adapter
        /// that implements the app-level <see cref="IHtmlSanitizer"/> interface.
        /// </summary>
        public static IHtmlSanitizer Create()
        {
            var s = new HtmlSanitizer(); // parameterless ctor; then mutate allow-lists

            // URL schemes
            s.AllowedSchemes.Add("data");    // allow inline images if needed; remove if you don't want data URIs
            s.AllowedSchemes.Remove("file"); // disallow local files

            // Allowed tags
            s.AllowedTags.UnionWith(new[]
            {
                "p","br","hr","span","div","blockquote","pre","code",
                "strong","b","em","i","u","s","sub","sup","small","mark",
                "h1","h2","h3","h4","h5","h6",
                "ul","ol","li",
                "a","img","figure","figcaption",
                "table","thead","tbody","tfoot","tr","th","td"
            });

            // Allowed attributes
            s.AllowedAttributes.UnionWith(new[]
            {
                "class","id","style","title","dir","lang",
                "href","target","rel",
                "src","alt","width","height","data-*",
                "border","cellpadding","cellspacing","rowspan","colspan","scope","align","valign"
            });

            // Allowed inline CSS properties
            s.AllowedCssProperties.UnionWith(new[]
            {
                "text-align","margin","padding","border","border-collapse","width","height",
                "background-color","color","font-size","font-weight","font-style",
                "list-style-type","vertical-align","border-radius","max-width"
            });

            // Post-processing: harden anchors & images
            s.PostProcessNode += (_, e) =>
            {
                if (e.Node is IElement el)
                {
                    if (el.NodeName.Equals("A", StringComparison.OrdinalIgnoreCase))
                    {
                        el.SetAttribute("rel", "noopener");
                        var target = el.GetAttribute("target");
                        if (!string.Equals(target, "_blank", StringComparison.OrdinalIgnoreCase))
                            el.SetAttribute("target", "_self");
                    }
                    else if (el.NodeName.Equals("IMG", StringComparison.OrdinalIgnoreCase))
                    {
                        var src = el.GetAttribute("src") ?? string.Empty;
                        // strip data URIs if you consider them unsafe:
                        // if (src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        //     el.RemoveAttribute("src");
                    }
                }
            };

            return new GanssHtmlSanitizerAdapter(s);
        }

        /// <summary>
        /// Adapter to bridge Ganss.Xss.HtmlSanitizer to the app's <see cref="IHtmlSanitizer"/>.
        /// </summary>
        private sealed class GanssHtmlSanitizerAdapter : IHtmlSanitizer
        {
            private readonly HtmlSanitizer _inner;
            public GanssHtmlSanitizerAdapter(HtmlSanitizer inner) => _inner = inner;
            public string Sanitize(string html) => _inner.Sanitize(html);
        }
    }
}
