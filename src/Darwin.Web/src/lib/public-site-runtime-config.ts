import "server-only";
import { cache } from "react";
import { fetchPublicJson } from "@/lib/api/fetch-public-json";
import {
  getFallbackPublicSiteRuntimeConfig,
  normalizePublicSiteRuntimeConfig,
  type PublicSiteRuntimeConfig,
} from "@/lib/public-site-runtime-config-shared";

const getCachedPublicSiteRuntimeConfig = cache(async () => {
  const result = await fetchPublicJson<PublicSiteRuntimeConfig>(
    "/api/v1/public/site/runtime-config",
    "site-runtime-config",
  );

  return result.status === "ok" && result.data
    ? normalizePublicSiteRuntimeConfig(result.data)
    : getFallbackPublicSiteRuntimeConfig();
});

export function getPublicSiteRuntimeConfig() {
  return getCachedPublicSiteRuntimeConfig();
}
