import type { MetadataRoute } from "next";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export default function robots(): MetadataRoute.Robots {
  const { siteUrl } = getSiteRuntimeConfig();

  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: [
        "/checkout/orders/*/confirmation/finalize",
      ],
    },
    sitemap: `${siteUrl}/sitemap.xml`,
  };
}
