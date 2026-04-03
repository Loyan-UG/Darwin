import {
  getCatalogReviewTargets,
  type CatalogReviewTarget,
} from "@/features/catalog/discovery";
import { getPendingCmsReviewTargets, type CmsReviewTarget } from "@/features/cms/discovery";
import {
  buildCatalogReviewWindowHref,
  buildCmsReviewWindowHref,
  type CatalogReviewWindow,
  type CmsReviewWindow,
} from "@/features/review/review-window";

type ReviewQueueOptions = {
  currentSlug?: string;
  previewCount?: number;
};

export function getPreferredCmsReviewState(
  readyCount: number,
  attentionCount: number,
) {
  return readyCount >= attentionCount ? "ready" : "needs-attention";
}

export function buildPreferredCmsReviewWindowHref(
  preferredState: "ready" | "needs-attention",
  reviewWindow?: CmsReviewWindow,
) {
  return buildCmsReviewWindowHref(reviewWindow, {
    visibleState: preferredState,
    visibleSort:
      preferredState === "ready" ? "ready-first" : "attention-first",
  });
}

export function getCmsReviewQueueState(
  targets: CmsReviewTarget[],
  options?: ReviewQueueOptions,
) {
  const queue = options?.currentSlug
    ? targets.filter((target) => target.page.slug !== options.currentSlug)
    : targets;

  return {
    queue,
    nextTarget: queue[0] ?? null,
    previewTargets: queue.slice(0, options?.previewCount ?? 3),
  };
}

export function getPendingCmsReviewQueueState(
  pages: CmsReviewTarget["page"][],
  options?: ReviewQueueOptions,
) {
  return getCmsReviewQueueState(getPendingCmsReviewTargets(pages), options);
}

export function getPreferredCatalogReviewState(
  offerCount: number,
  baseCount: number,
) {
  return offerCount >= baseCount ? "offers" : "base";
}

export function buildPreferredCatalogReviewWindowHref(
  preferredState: "offers" | "base",
  reviewWindow?: CatalogReviewWindow,
) {
  return buildCatalogReviewWindowHref(reviewWindow, {
    visibleState: preferredState,
    visibleSort: preferredState === "offers" ? "offers-first" : "base-first",
  });
}

export function getCatalogReviewQueueState(
  targets: CatalogReviewTarget[],
  options?: ReviewQueueOptions,
) {
  const queue = options?.currentSlug
    ? targets.filter((target) => target.product.slug !== options.currentSlug)
    : targets;

  return {
    queue,
    nextTarget: queue[0] ?? null,
    previewTargets: queue.slice(0, options?.previewCount ?? 3),
  };
}

export function getPendingCatalogReviewQueueState(
  products: CatalogReviewTarget["product"][],
  options?: ReviewQueueOptions,
) {
  return getCatalogReviewQueueState(getCatalogReviewTargets(products), options);
}
