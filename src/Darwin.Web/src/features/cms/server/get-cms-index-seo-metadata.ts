import "server-only";
import {
  readCmsVisibleSort,
  readCmsVisibleState,
} from "@/features/cms/discovery";
import {
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { cmsIndexRouteObservationContext } from "@/lib/route-observation-context";
import { buildSeoMetadata } from "@/lib/seo";
import { createCachedObservedSeoMetadataLoader } from "@/lib/seo-loader";
import { getSharedResource } from "@/localization";

type CmsSeoSearchParams = {
  page?: string;
  visibleQuery?: string;
  visibleState?: string;
  visibleSort?: string;
};

export const getCmsIndexSeoMetadata = createCachedObservedSeoMetadataLoader({
  area: "cms-seo",
  operation: "load-index-seo-metadata",
  thresholdMs: 175,
  getContext: (culture: string, searchParams?: CmsSeoSearchParams) =>
    cmsIndexRouteObservationContext(
      culture,
      readPositiveIntegerSearchParam(searchParams?.page),
    ),
  load: async (culture: string, searchParams?: CmsSeoSearchParams) => {
    const shared = getSharedResource(culture);
    const safePage = readPositiveIntegerSearchParam(searchParams?.page);
    const visibleQuery = readSearchTextParam(searchParams?.visibleQuery);
    const visibleState = readCmsVisibleState(searchParams?.visibleState);
    const visibleSort = readCmsVisibleSort(searchParams?.visibleSort);
    const canonicalPath = buildAppQueryPath("/cms", {
      page: safePage > 1 ? safePage : undefined,
      visibleQuery,
      visibleState: visibleState !== "all" ? visibleState : undefined,
      visibleSort: visibleSort !== "featured" ? visibleSort : undefined,
    });
    const noIndex =
      safePage > 1 ||
      Boolean(visibleQuery) ||
      visibleState !== "all" ||
      visibleSort !== "featured";

    return {
      metadata: buildSeoMetadata({
        culture,
        title: shared.cmsIndexMetaTitle,
        description: shared.cmsIndexMetaDescription,
        path: canonicalPath,
        noIndex,
        allowLanguageAlternates: !noIndex,
      }),
      canonicalPath,
      noIndex,
      languageAlternates: !noIndex ? {} : undefined,
    };
  },
});
