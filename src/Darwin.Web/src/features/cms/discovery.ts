import type { PublicPageSummary } from "@/features/cms/types";
import { readAllowedSearchParam } from "@/features/checkout/helpers";

export type CmsVisibleState = "all" | "ready" | "needs-attention";
export type CmsVisibleSort =
  | "featured"
  | "title-asc"
  | "ready-first"
  | "attention-first";

export type CmsReviewTarget = {
  page: PublicPageSummary;
  missingMetaTitle: boolean;
  missingMetaDescription: boolean;
};

export function readCmsVisibleState(value?: string): CmsVisibleState {
  return (
    readAllowedSearchParam(value, ["all", "ready", "needs-attention"] as const) ??
    "all"
  );
}

export function readCmsVisibleSort(value?: string): CmsVisibleSort {
  return (
    readAllowedSearchParam(
      value,
      ["featured", "title-asc", "ready-first", "attention-first"] as const,
    ) ?? "featured"
  );
}

export function isDiscoveryReadyPage(page: {
  metaTitle?: string | null;
  metaDescription?: string | null;
}) {
  return Boolean(page.metaTitle?.trim() && page.metaDescription?.trim());
}

export function getCmsReviewTarget(page: PublicPageSummary): CmsReviewTarget {
  return {
    page,
    missingMetaTitle: !page.metaTitle?.trim(),
    missingMetaDescription: !page.metaDescription?.trim(),
  };
}

export function isCmsReviewTargetPending(target: CmsReviewTarget) {
  return target.missingMetaTitle || target.missingMetaDescription;
}

export function sortCmsReviewTargets(targets: CmsReviewTarget[]) {
  return [...targets].sort((left, right) => {
    const leftRank =
      Number(left.missingMetaTitle) + Number(left.missingMetaDescription);
    const rightRank =
      Number(right.missingMetaTitle) + Number(right.missingMetaDescription);

    if (rightRank !== leftRank) {
      return rightRank - leftRank;
    }

    return left.page.title.localeCompare(right.page.title);
  });
}

export function getPendingCmsReviewTargets(pages: PublicPageSummary[]) {
  return sortCmsReviewTargets(
    pages.map(getCmsReviewTarget).filter(isCmsReviewTargetPending),
  );
}

export function filterVisiblePages(
  pages: PublicPageSummary[],
  visibleState: CmsVisibleState,
  visibleQuery?: string,
) {
  const normalizedQuery = visibleQuery?.trim().toLowerCase();

  return pages.filter((page) => {
    if (normalizedQuery) {
      const haystack =
        `${page.title} ${page.slug} ${page.metaTitle ?? ""} ${page.metaDescription ?? ""}`.toLowerCase();

      if (!haystack.includes(normalizedQuery)) {
        return false;
      }
    }

    if (visibleState === "ready") {
      return isDiscoveryReadyPage(page);
    }

    if (visibleState === "needs-attention") {
      return !isDiscoveryReadyPage(page);
    }

    return true;
  });
}

export function sortVisiblePages(
  pages: PublicPageSummary[],
  visibleSort: CmsVisibleSort,
) {
  const rankedPages = [...pages];

  switch (visibleSort) {
    case "title-asc":
      rankedPages.sort((left, right) => left.title.localeCompare(right.title));
      break;
    case "ready-first":
      rankedPages.sort((left, right) => {
        const readinessDelta =
          Number(isDiscoveryReadyPage(right)) - Number(isDiscoveryReadyPage(left));
        if (readinessDelta !== 0) {
          return readinessDelta;
        }

        return left.title.localeCompare(right.title);
      });
      break;
    case "attention-first":
      rankedPages.sort((left, right) => {
        const attentionDelta =
          Number(!isDiscoveryReadyPage(right)) - Number(!isDiscoveryReadyPage(left));
        if (attentionDelta !== 0) {
          return attentionDelta;
        }

        return left.title.localeCompare(right.title);
      });
      break;
    case "featured":
    default:
      break;
  }

  return rankedPages;
}
