import { getSharedResource } from "@/localization";

export function getCultureDisplayName(culture: string) {
  const shared = getSharedResource(culture);
  return shared.cultureDisplayNames[culture as keyof typeof shared.cultureDisplayNames]
    ?? shared.cultureDisplayNames["de-DE"];
}

export function getCultureShortCode(culture: string) {
  return culture.split("-")[0]?.slice(0, 2).toUpperCase() || "DE";
}
