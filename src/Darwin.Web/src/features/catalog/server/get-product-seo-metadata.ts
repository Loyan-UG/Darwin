import "server-only";
import { getCatalogDetailRouteContext } from "@/features/catalog/server/get-catalog-route-context";
import { getProductLanguageAlternatesMap } from "@/features/catalog/server/get-product-language-alternates-map";
import { productDetailRouteObservationContext } from "@/lib/route-observation-context";
import { normalizeEntityRouteArgs } from "@/lib/route-context-normalization";
import { buildSeoMetadata, deriveSeoDescription } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getCatalogResource } from "@/localization";

export const getProductSeoMetadata = createCachedObservedSeoMetadataLoader({
  area: "catalog-seo",
  operation: "load-product-seo-metadata",
  thresholdMs: 200,
  normalizeArgs: normalizeEntityRouteArgs,
  getContext: (culture: string, slug: string) =>
    productDetailRouteObservationContext(culture, slug),
  load: async (culture: string, slug: string) => {
    const copy = getCatalogResource(culture);
    const { detailContext } = await getCatalogDetailRouteContext(culture, slug);
    const { productResult } = detailContext;
    const product = productResult.data;
    const canonicalPath = `/catalog/${encodeURIComponent(slug)}`;

    if (!product) {
      return {
        metadata: buildSeoMetadata({
          culture,
          title: copy.productUnavailableMetaTitle,
          description: copy.productFallbackMetaDescription,
          path: canonicalPath,
          noIndex: true,
        }),
        canonicalPath,
        noIndex: true,
        languageAlternates: {},
      };
    }

    const alternates = await getProductLanguageAlternatesMap();
    const languageAlternates = alternates.get(product.id) ?? {};
    const noIndex = productResult.status !== "ok";

    return {
      metadata: buildSeoMetadata({
        culture,
        title: product.metaTitle ?? product.name,
        description:
          deriveSeoDescription(
            product.metaDescription,
            product.shortDescription,
            product.fullDescriptionHtml,
          ) ?? copy.productFallbackMetaDescription,
        path: canonicalPath,
        imageUrl: product.media[0]?.url ?? product.primaryImageUrl,
        noIndex,
        languageAlternates,
      }),
      canonicalPath,
      noIndex,
      languageAlternates,
    };
  },
});
