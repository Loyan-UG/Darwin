import type {
  CatalogMediaState,
  CatalogSavingsBand,
  CatalogVisibleSort,
  CatalogVisibleState,
} from "@/features/catalog/types";
import type {
  CmsMetadataFocus,
  CmsVisibleSort,
  CmsVisibleState,
} from "@/features/cms/discovery";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { buildAppQueryPath } from "@/lib/locale-routing";

export type CmsReviewWindow = {
  visibleQuery?: string;
  visibleState?: CmsVisibleState;
  visibleSort?: CmsVisibleSort;
  metadataFocus?: CmsMetadataFocus;
};

export type CatalogReviewWindow = {
  category?: string;
  visibleQuery?: string;
  visibleState?: CatalogVisibleState;
  visibleSort?: CatalogVisibleSort;
  mediaState?: CatalogMediaState;
  savingsBand?: CatalogSavingsBand;
};

export function buildCmsReviewWindowHref(
  reviewWindow?: CmsReviewWindow,
  overrides?: Partial<CmsReviewWindow>,
) {
  const next = {
    visibleQuery: overrides?.visibleQuery ?? reviewWindow?.visibleQuery,
    visibleState: overrides?.visibleState ?? reviewWindow?.visibleState,
    visibleSort: overrides?.visibleSort ?? reviewWindow?.visibleSort,
    metadataFocus: overrides?.metadataFocus ?? reviewWindow?.metadataFocus,
  };

  return buildAppQueryPath("/cms", {
    visibleQuery: next.visibleQuery,
    visibleState:
      next.visibleState && next.visibleState !== "all"
        ? next.visibleState
        : undefined,
    visibleSort:
      next.visibleSort && next.visibleSort !== "featured"
        ? next.visibleSort
        : undefined,
    metadataFocus:
      next.metadataFocus && next.metadataFocus !== "all"
        ? next.metadataFocus
        : undefined,
  });
}

export function buildCmsReviewTargetHref(
  slug: string,
  reviewWindow?: CmsReviewWindow,
  overrides?: Partial<CmsReviewWindow>,
) {
  const next = {
    visibleQuery: overrides?.visibleQuery ?? reviewWindow?.visibleQuery,
    visibleState: overrides?.visibleState ?? reviewWindow?.visibleState,
    visibleSort: overrides?.visibleSort ?? reviewWindow?.visibleSort,
    metadataFocus: overrides?.metadataFocus ?? reviewWindow?.metadataFocus,
  };

  return buildAppQueryPath(buildCmsPagePath(slug), {
    visibleQuery: next.visibleQuery,
    visibleState:
      next.visibleState && next.visibleState !== "all"
        ? next.visibleState
        : undefined,
    visibleSort:
      next.visibleSort && next.visibleSort !== "featured"
        ? next.visibleSort
        : undefined,
    metadataFocus:
      next.metadataFocus && next.metadataFocus !== "all"
        ? next.metadataFocus
        : undefined,
  });
}

export function buildCatalogReviewWindowHref(
  reviewWindow?: CatalogReviewWindow,
  overrides?: Partial<CatalogReviewWindow>,
) {
  const next = {
    category: overrides?.category ?? reviewWindow?.category,
    visibleQuery: overrides?.visibleQuery ?? reviewWindow?.visibleQuery,
    visibleState: overrides?.visibleState ?? reviewWindow?.visibleState,
    visibleSort: overrides?.visibleSort ?? reviewWindow?.visibleSort,
    mediaState: overrides?.mediaState ?? reviewWindow?.mediaState,
    savingsBand: overrides?.savingsBand ?? reviewWindow?.savingsBand,
  };

  return buildAppQueryPath("/catalog", {
    category: next.category,
    visibleQuery: next.visibleQuery,
    visibleState:
      next.visibleState && next.visibleState !== "all"
        ? next.visibleState
        : undefined,
    visibleSort:
      next.visibleSort && next.visibleSort !== "featured"
        ? next.visibleSort
        : undefined,
    mediaState:
      next.mediaState && next.mediaState !== "all"
        ? next.mediaState
        : undefined,
    savingsBand:
      next.savingsBand && next.savingsBand !== "all"
        ? next.savingsBand
        : undefined,
  });
}

export function buildCatalogReviewTargetHref(
  slug: string,
  reviewWindow?: CatalogReviewWindow,
  overrides?: Partial<CatalogReviewWindow>,
) {
  const next = {
    category: overrides?.category ?? reviewWindow?.category,
    visibleQuery: overrides?.visibleQuery ?? reviewWindow?.visibleQuery,
    visibleState: overrides?.visibleState ?? reviewWindow?.visibleState,
    visibleSort: overrides?.visibleSort ?? reviewWindow?.visibleSort,
    mediaState: overrides?.mediaState ?? reviewWindow?.mediaState,
    savingsBand: overrides?.savingsBand ?? reviewWindow?.savingsBand,
  };

  return buildAppQueryPath(buildCatalogProductPath(slug), {
    category: next.category,
    visibleQuery: next.visibleQuery,
    visibleState:
      next.visibleState && next.visibleState !== "all"
        ? next.visibleState
        : undefined,
    visibleSort:
      next.visibleSort && next.visibleSort !== "featured"
        ? next.visibleSort
        : undefined,
    mediaState:
      next.mediaState && next.mediaState !== "all"
        ? next.mediaState
        : undefined,
    savingsBand:
      next.savingsBand && next.savingsBand !== "all"
        ? next.savingsBand
        : undefined,
  });
}
