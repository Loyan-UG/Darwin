import type { PublicPageSummary } from "@/features/cms/types";
import { readAllowedSearchParam } from "@/features/checkout/helpers";

export type CmsVisibleState = "all" | "ready" | "needs-attention";
export type CmsVisibleSort =
  | "featured"
  | "title-asc"
  | "ready-first"
  | "attention-first";
export type CmsMetadataFocus =
  | "all"
  | "missing-title"
  | "missing-description"
  | "missing-both";

export type CmsContentQualityTarget = {
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

export function readCmsMetadataFocus(value?: string): CmsMetadataFocus {
  return (
    readAllowedSearchParam(
      value,
      [
        "all",
        "missing-title",
        "missing-description",
        "missing-both",
      ] as const,
    ) ?? "all"
  );
}

export function isDiscoveryReadyPage(page: {
  metaTitle?: string | null;
  metaDescription?: string | null;
}) {
  return Boolean(page.metaTitle?.trim() && page.metaDescription?.trim());
}

export function hasMetaTitle(page: {
  metaTitle?: string | null;
}) {
  return Boolean(page.metaTitle?.trim());
}

export function hasMetaDescription(page: {
  metaDescription?: string | null;
}) {
  return Boolean(page.metaDescription?.trim());
}

export function getCmsContentQualityTarget(page: PublicPageSummary): CmsContentQualityTarget {
  return {
    page,
    missingMetaTitle: !page.metaTitle?.trim(),
    missingMetaDescription: !page.metaDescription?.trim(),
  };
}

export function isCmsContentQualityTargetPending(target: CmsContentQualityTarget) {
  return target.missingMetaTitle || target.missingMetaDescription;
}

export function sortCmsContentQualityTargets(targets: CmsContentQualityTarget[]) {
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

export function getPendingCmsContentQualityTargets(pages: PublicPageSummary[]) {
  return sortCmsContentQualityTargets(
    pages.map(getCmsContentQualityTarget).filter(isCmsContentQualityTargetPending),
  );
}

export function filterVisiblePages(
  pages: PublicPageSummary[],
  visibleState: CmsVisibleState,
  visibleQuery?: string,
  metadataFocus: CmsMetadataFocus = "all",
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

    if (metadataFocus === "missing-title" && hasMetaTitle(page)) {
      return false;
    }

    if (metadataFocus === "missing-description" && hasMetaDescription(page)) {
      return false;
    }

    if (
      metadataFocus === "missing-both" &&
      (hasMetaTitle(page) || hasMetaDescription(page))
    ) {
      return false;
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

export function buildCmsVisibleWindow(
  pages: PublicPageSummary[],
  input: {
    page: number;
    pageSize: number;
    visibleState: CmsVisibleState;
    visibleSort: CmsVisibleSort;
    metadataFocus?: CmsMetadataFocus;
  },
) {
  const filteredPages = sortVisiblePages(
    filterVisiblePages(pages, input.visibleState, undefined, input.metadataFocus),
    input.visibleSort,
  );
  const total = filteredPages.length;
  const totalPages = Math.max(1, Math.ceil(total / input.pageSize));
  const currentPage = Math.min(Math.max(input.page, 1), totalPages);
  const start = (currentPage - 1) * input.pageSize;

  return {
    items: filteredPages.slice(start, start + input.pageSize),
    total,
    totalPages,
    currentPage,
  };
}

export function summarizeCmsMetadataDebt(pages: PublicPageSummary[]) {
  const missingMetaTitleCount = pages.filter((page) => !hasMetaTitle(page)).length;
  const missingMetaDescriptionCount = pages.filter(
    (page) => !hasMetaDescription(page),
  ).length;
  const missingBothCount = pages.filter(
    (page) => !hasMetaTitle(page) && !hasMetaDescription(page),
  ).length;

  return {
    totalCount: pages.length,
    readyCount: pages.filter(isDiscoveryReadyPage).length,
    attentionCount: pages.filter((page) => !isDiscoveryReadyPage(page)).length,
    missingMetaTitleCount,
    missingMetaDescriptionCount,
    missingBothCount,
  };
}
