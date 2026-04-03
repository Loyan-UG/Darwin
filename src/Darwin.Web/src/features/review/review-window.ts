import type {
  CatalogVisibleSort,
  CatalogVisibleState,
} from "@/features/catalog/types";
import type {
  CmsVisibleSort,
  CmsVisibleState,
} from "@/features/cms/discovery";
import { buildAppQueryPath } from "@/lib/locale-routing";

export type CmsReviewWindow = {
  visibleQuery?: string;
  visibleState?: CmsVisibleState;
  visibleSort?: CmsVisibleSort;
};

export type CatalogReviewWindow = {
  category?: string;
  visibleQuery?: string;
  visibleState?: CatalogVisibleState;
  visibleSort?: CatalogVisibleSort;
};

export function buildCmsReviewWindowHref(
  reviewWindow?: CmsReviewWindow,
  overrides?: Partial<CmsReviewWindow>,
) {
  const next = {
    visibleQuery: overrides?.visibleQuery ?? reviewWindow?.visibleQuery,
    visibleState: overrides?.visibleState ?? reviewWindow?.visibleState,
    visibleSort: overrides?.visibleSort ?? reviewWindow?.visibleSort,
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
  };

  return buildAppQueryPath(`/cms/${slug}`, {
    visibleQuery: next.visibleQuery,
    visibleState:
      next.visibleState && next.visibleState !== "all"
        ? next.visibleState
        : undefined,
    visibleSort:
      next.visibleSort && next.visibleSort !== "featured"
        ? next.visibleSort
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
  };

  return buildAppQueryPath(`/catalog/${slug}`, {
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
  });
}
