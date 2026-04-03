import "server-only";
import { buildSeoMetadata } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { homeSeoObservationContext } from "@/lib/route-observation-context";
import { getSharedResource } from "@/localization";

export const getHomeSeoMetadata = createCachedObservedSeoMetadataLoader({
  area: "home-seo",
  operation: "load-home-seo-metadata",
  thresholdMs: 150,
  getContext: homeSeoObservationContext,
  load: async (culture: string) => {
    const shared = getSharedResource(culture);
    const canonicalPath = "/";

    return {
      metadata: buildSeoMetadata({
        culture,
        title: shared.homeMetaTitle,
        description: shared.homeMetaDescription,
        path: canonicalPath,
        allowLanguageAlternates: true,
      }),
      canonicalPath,
      noIndex: false,
      languageAlternates: {},
    };
  },
});
