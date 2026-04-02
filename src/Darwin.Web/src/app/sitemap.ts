import type { MetadataRoute } from "next";
import { buildPublicSitemapEntries } from "@/lib/sitemap";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  return buildPublicSitemapEntries();
}
