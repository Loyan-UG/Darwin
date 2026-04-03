import "server-only";
import type { MetadataRoute } from "next";
import { getPublicSitemapContext } from "@/features/storefront/server/get-public-sitemap-context";

export async function buildPublicSitemapEntries(): Promise<MetadataRoute.Sitemap> {
  const { entries } = await getPublicSitemapContext();
  return entries;
}
