import type { PublicApiFetchResult } from "@/lib/api/fetch-public-json";

type PagedSetData<TItem> = {
  items: TItem[];
  total: number;
};

type PagedSetResult<TItem> = PublicApiFetchResult<PagedSetData<TItem>>;

export function shouldExpandPublicPagedSet<TItem>(
  result: PagedSetResult<TItem>,
) {
  const total = result.data?.total ?? 0;
  const loadedCount = result.data?.items.length ?? 0;

  return result.status === "ok" && Boolean(result.data) && total > loadedCount;
}

export function selectExpandedPublicPagedSet<TItem>(
  initialResult: PagedSetResult<TItem>,
  expandedResult: PagedSetResult<TItem>,
) {
  return expandedResult.status === "ok" && expandedResult.data
    ? expandedResult
    : initialResult;
}
