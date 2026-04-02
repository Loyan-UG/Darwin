import "server-only";
import { cookies, headers } from "next/headers";
import { REQUEST_CULTURE_HEADER } from "@/lib/locale-routing";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

export function normalizeCulture(value: string | undefined | null) {
  const runtimeConfig = getSiteRuntimeConfig();
  const normalized = value?.trim();

  return normalized && runtimeConfig.supportedCultures.includes(normalized)
    ? normalized
    : runtimeConfig.defaultCulture;
}

export async function getRequestCulture() {
  const runtimeConfig = getSiteRuntimeConfig();
  const headerStore = await headers();
  const cookieStore = await cookies();

  return normalizeCulture(
    headerStore.get(REQUEST_CULTURE_HEADER) ??
      cookieStore.get(runtimeConfig.cultureCookieName)?.value ??
      runtimeConfig.defaultCulture,
  );
}

export function getSupportedCultures() {
  return getSiteRuntimeConfig().supportedCultures;
}
