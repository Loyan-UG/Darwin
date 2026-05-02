import "server-only";
import { cookies, headers } from "next/headers";
import { REQUEST_CULTURE_HEADER } from "@/lib/locale-routing";
import { getPublicSiteRuntimeConfig } from "@/lib/public-site-runtime-config";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export function normalizeCulture(
  value: string | undefined | null,
  runtimeConfig: {
    defaultCulture: string;
    supportedCultures: string[];
  } = getSiteRuntimeConfig(),
) {
  const normalized = value?.trim();

  return normalized && runtimeConfig.supportedCultures.includes(normalized)
    ? normalized
    : runtimeConfig.defaultCulture;
}

export async function getRequestCulture() {
  const runtimeConfig = await getPublicSiteRuntimeConfig();
  const headerStore = await headers();
  const cookieStore = await cookies();
  const cookieName = getSiteRuntimeConfig().cultureCookieName;

  return normalizeCulture(
    headerStore.get(REQUEST_CULTURE_HEADER) ??
      cookieStore.get(cookieName)?.value ??
      runtimeConfig.defaultCulture,
    runtimeConfig,
  );
}

export function getSupportedCultures() {
  return getSiteRuntimeConfig().supportedCultures;
}

export async function getSupportedCulturesAsync() {
  return (await getPublicSiteRuntimeConfig()).supportedCultures;
}
