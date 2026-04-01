import "server-only";
import { cookies } from "next/headers";
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
  const cookieStore = await cookies();

  return normalizeCulture(
    cookieStore.get(runtimeConfig.cultureCookieName)?.value ??
      runtimeConfig.defaultCulture,
  );
}

export function getSupportedCultures() {
  return getSiteRuntimeConfig().supportedCultures;
}
