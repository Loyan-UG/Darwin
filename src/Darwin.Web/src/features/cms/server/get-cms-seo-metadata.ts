import "server-only";
import { getCmsDetailRouteContext } from "@/features/cms/server/get-cms-route-context";
import { getCmsLanguageAlternatesMap } from "@/features/cms/server/get-cms-language-alternates-map";
import { cmsDetailRouteObservationContext } from "@/lib/route-observation-context";
import { normalizeEntityRouteArgs } from "@/lib/route-context-normalization";
import { buildSeoMetadata, deriveSeoDescription } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getSharedResource } from "@/localization";

export const getCmsSeoMetadata = createCachedObservedSeoMetadataLoader({
  area: "cms-seo",
  operation: "load-page-seo-metadata",
  thresholdMs: 200,
  normalizeArgs: normalizeEntityRouteArgs,
  getContext: (culture: string, slug: string) =>
    cmsDetailRouteObservationContext(culture, slug),
  load: async (culture: string, slug: string) => {
    const shared = getSharedResource(culture);
    const { detailContext } = await getCmsDetailRouteContext(culture, slug);
    const { pageResult } = detailContext;
    const page = pageResult.data;
    const requestedPath = `/cms/${encodeURIComponent(slug)}`;

    if (!page) {
      return {
        metadata: buildSeoMetadata({
          culture,
          title:
            pageResult.status === "not-found"
              ? shared.cmsPageNotFoundTitle
              : shared.cmsPageUnavailableTitle,
          description: shared.cmsFallbackMetaDescription,
          path: requestedPath,
          noIndex: true,
        }),
        canonicalPath: requestedPath,
        noIndex: true,
        languageAlternates: {},
      };
    }

    const alternates = await getCmsLanguageAlternatesMap();
    const languageAlternates = alternates.get(page.id) ?? {};
    const canonicalPath = `/cms/${encodeURIComponent(page.slug)}`;
    const noIndex = pageResult.status !== "ok";

    return {
      metadata: buildSeoMetadata({
        culture,
        title: page.metaTitle ?? page.title,
        description:
          deriveSeoDescription(page.metaDescription, page.contentHtml) ??
          shared.cmsFallbackMetaDescription,
        path: canonicalPath,
        noIndex,
        type: "article",
        languageAlternates,
      }),
      canonicalPath,
      noIndex,
      languageAlternates,
    };
  },
});
