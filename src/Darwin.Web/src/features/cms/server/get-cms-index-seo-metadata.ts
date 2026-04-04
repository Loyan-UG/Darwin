import "server-only";
import {
  readCmsMetadataFocus,
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
  search?: string;
  visibleQuery?: string;
  visibleState?: string;
  visibleSort?: string;
  metadataFocus?: string;
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
    const search = readSearchTextParam(searchParams?.search, 80);
    const visibleQuery = readSearchTextParam(searchParams?.visibleQuery);
    const visibleState = readCmsVisibleState(searchParams?.visibleState);
    const visibleSort = readCmsVisibleSort(searchParams?.visibleSort);
    const metadataFocus = readCmsMetadataFocus(searchParams?.metadataFocus);
    const canonicalPath = buildAppQueryPath("/cms", {
      page: safePage > 1 ? safePage : undefined,
      search,
      visibleQuery,
      visibleState: visibleState !== "all" ? visibleState : undefined,
      visibleSort: visibleSort !== "featured" ? visibleSort : undefined,
      metadataFocus:
        metadataFocus !== "all" ? metadataFocus : undefined,
    });
    const noIndex =
      safePage > 1 ||
      Boolean(search) ||
      Boolean(visibleQuery) ||
      visibleState !== "all" ||
      visibleSort !== "featured" ||
      metadataFocus !== "all";

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
