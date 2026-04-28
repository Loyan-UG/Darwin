import "server-only";
import { getPublicBusinessDetail } from "@/features/businesses/api/public-businesses";
import { buildLoyaltyBusinessPath } from "@/lib/entity-paths";
import { memberRouteObservationContext } from "@/lib/route-observation-context";
import { normalizeEntityRouteArgs } from "@/lib/route-context-normalization";
import { buildNoIndexMetadata, deriveSeoDescription } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getMemberResource } from "@/localization";

type LoyaltySeoRoute = "/loyalty" | "/loyalty/[businessId]";

function normalizeLoyaltySeoArgs(
  culture: string,
  route: LoyaltySeoRoute,
  canonicalPath: string,
): [string, LoyaltySeoRoute, string] {
  const [normalizedCulture, normalizedCanonicalPath] = normalizeEntityRouteArgs(
    culture,
    canonicalPath,
  );

  return [normalizedCulture, route.trim() as LoyaltySeoRoute, normalizedCanonicalPath];
}

const getCachedLoyaltySeoMetadata = createCachedObservedSeoMetadataLoader({
  area: "member-loyalty-seo",
  operation: "load-route-seo-metadata",
  thresholdMs: 150,
  normalizeArgs: normalizeLoyaltySeoArgs,
  getContext: (
    culture: string,
    route: LoyaltySeoRoute,
    canonicalPath: string,
  ) => memberRouteObservationContext(culture, route, { canonicalPath }),
  load: async (culture: string, route: LoyaltySeoRoute, canonicalPath: string) => {
    const copy = getMemberResource(culture);

    if (route === "/loyalty") {
      return {
        metadata: buildNoIndexMetadata(
          culture,
          copy.loyaltyMetaTitle,
          copy.loyaltyMetaDescription,
          canonicalPath,
        ),
        canonicalPath,
        noIndex: true,
        languageAlternates: {},
      };
    }

    const businessId = canonicalPath.split("/").filter(Boolean).at(-1) ?? "";
    const businessResult = await getPublicBusinessDetail(businessId, culture);
    const business = businessResult.data;

    return {
      metadata: buildNoIndexMetadata(
        culture,
        business?.name ?? copy.loyaltyBusinessFallback,
        deriveSeoDescription(
          business?.shortDescription,
          business?.description,
          copy.loyaltyMetaDescription,
        ),
        canonicalPath,
      ),
      canonicalPath,
      noIndex: true,
      languageAlternates: {},
    };
  },
});

export const getLoyaltyOverviewSeoMetadata = (culture: string) =>
  getCachedLoyaltySeoMetadata(culture, "/loyalty", "/loyalty");

export const getLoyaltyBusinessSeoMetadata = (
  culture: string,
  businessId: string,
) =>
  getCachedLoyaltySeoMetadata(
    culture,
    "/loyalty/[businessId]",
    buildLoyaltyBusinessPath(businessId),
  );
