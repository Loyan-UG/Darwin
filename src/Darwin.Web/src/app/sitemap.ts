import type { MetadataRoute } from "next";
import { getPublicSitemapContext } from "@/features/storefront/server/get-public-sitemap-context";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const { entries } = await getPublicSitemapContext();
  return entries;
}
