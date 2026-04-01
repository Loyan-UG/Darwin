import { getSharedResource } from "@/localization";

export function getCultureDisplayName(culture: string) {
  const shared = getSharedResource(culture);
  return shared.cultureDisplayNames[culture as keyof typeof shared.cultureDisplayNames]
    ?? shared.cultureDisplayNames["de-DE"];
}
