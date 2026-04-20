import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

function resolveFormattingLocale(locale?: string) {
  const runtimeConfig = getSiteRuntimeConfig();
  const normalized = locale?.trim();

  if (!normalized) {
    return runtimeConfig.defaultCulture;
  }

  if (runtimeConfig.supportedCultures.includes(normalized)) {
    return normalized;
  }

  const language = normalized.split("-")[0]?.toLowerCase();
  const languageFallback = runtimeConfig.supportedCultures.find((supportedCulture) =>
    supportedCulture.toLowerCase().startsWith(`${language}-`),
  );

  return languageFallback ?? runtimeConfig.defaultCulture;
}

export function formatMoney(
  valueMinor: number,
  currency: string,
  locale?: string,
) {
  return new Intl.NumberFormat(resolveFormattingLocale(locale), {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(valueMinor / 100);
}

export function formatDateTime(
  value: string | Date,
  locale?: string,
) {
  return new Intl.DateTimeFormat(resolveFormattingLocale(locale), {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(typeof value === "string" ? new Date(value) : value);
}
