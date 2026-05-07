import Link from "next/link";
import { CmsCommerceCampaignWindow } from "@/components/cms/cms-commerce-campaign-window";
import { CmsIndexCompositionWindow } from "@/components/cms/cms-index-composition-window";
import { CmsContinuationRail } from "@/components/cms/cms-continuation-rail";
import { CmsStorefrontSupportWindow } from "@/components/cms/cms-storefront-support-window";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCategorySummary, PublicProductSummary } from "@/features/catalog/types";
import type { CmsMetadataFocus } from "@/features/cms/discovery";
import { isDiscoveryReadyPage } from "@/features/cms/discovery";
import { buildCmsReviewTargetHref } from "@/features/review/review-window";
import {
  buildPreferredCmsReviewWindowHref,
  getPendingCmsReviewQueueState,
  getPreferredCmsReviewState,
} from "@/features/review/review-workflow";
import type { PublicPageSummary } from "@/features/cms/types";
import { buildCmsPagePath } from "@/lib/entity-paths";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import {
  formatResource,
  getSharedResource,
  resolveApiStatusLabel,
} from "@/localization";

type CmsPagesIndexProps = {
  culture: string;
  pages: PublicPageSummary[];
  loadedPageCount: number;
  totalItems: number;
  matchingItemsTotal: number;
  pageSize: number;
  totalPages: number;
  currentPage: number;
  status: string;
  searchQuery?: string;
  visibleState?: "all" | "ready" | "needs-attention";
  visibleSort?: "featured" | "title-asc" | "ready-first" | "attention-first";
  metadataFocus?: CmsMetadataFocus;
  metadataSummary: {
    totalCount: number;
    readyCount: number;
    attentionCount: number;
    missingMetaTitleCount: number;
    missingMetaDescriptionCount: number;
    missingBothCount: number;
  };
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  cartSummary: {
    status: string;
    itemCount: number;
    currency: string;
    grandTotalGrossMinor: number;
  } | null;
};

function buildCmsHref(
  page = 1,
  searchQuery?: string,
  visibleState?: "all" | "ready" | "needs-attention",
  visibleSort?: "featured" | "title-asc" | "ready-first" | "attention-first",
  metadataFocus?: CmsMetadataFocus,
) {
  return buildAppQueryPath("/cms", {
    page: page > 1 ? page : undefined,
    search: searchQuery,
    visibleState: visibleState && visibleState !== "all" ? visibleState : undefined,
    visibleSort: visibleSort && visibleSort !== "featured" ? visibleSort : undefined,
    metadataFocus:
      metadataFocus && metadataFocus !== "all" ? metadataFocus : undefined,
  });
}

function buildCmsGroupAnchor(key: string) {
  return `cms-group-${encodeURIComponent(key.toLowerCase())}`;
}

export function CmsPagesIndex({
  culture,
  pages,
  loadedPageCount,
  totalItems,
  matchingItemsTotal,
  pageSize,
  totalPages,
  currentPage,
  status,
  searchQuery,
  visibleState = "all",
  visibleSort = "featured",
  metadataFocus = "all",
  metadataSummary,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  cartSummary,
}: CmsPagesIndexProps) {
  const copy = getSharedResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy) ?? status;
  const pageStart = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const pageEnd =
    totalItems === 0 ? 0 : Math.min(totalItems, pageStart + loadedPageCount - 1);
  const groupedPages = pages.reduce<
    Array<{
      key: string;
      items: PublicPageSummary[];
    }>
  >((groups, page) => {
    const normalizedTitle = page.title.trim();
    const firstCharacter = normalizedTitle.charAt(0).toUpperCase();
    const key =
      firstCharacter && /[A-Z0-9]/.test(firstCharacter)
        ? firstCharacter
        : "#";
    const existingGroup = groups.find((group) => group.key === key);

    if (existingGroup) {
      existingGroup.items.push(page);
      return groups;
    }

    groups.push({
      key,
      items: [page],
    });
    return groups;
  }, []);
  const largestGroup =
    groupedPages.length > 0
      ? groupedPages.reduce((largest, group) =>
          group.items.length > largest.items.length ? group : largest,
        )
      : null;
  const spotlightPage = pages[0] ?? null;
  const followUpPages = pages.slice(1, 4);
  const readyPagesCount = metadataSummary.readyCount;
  const attentionPagesCount = metadataSummary.attentionCount;
  const attentionQueue = getPendingCmsReviewQueueState(pages).previewTargets.map(
    (target) => ({
      page: target.page,
      reason:
        target.missingMetaTitle && target.missingMetaDescription
          ? copy.cmsIndexReviewTargetBothMessage
          : target.missingMetaTitle
            ? copy.cmsIndexReviewTargetMetaTitleMessage
            : copy.cmsIndexReviewTargetMetaDescriptionMessage,
    }),
  );
  const missingMetaTitleCount = metadataSummary.missingMetaTitleCount;
  const missingMetaDescriptionCount = metadataSummary.missingMetaDescriptionCount;
  const missingBothCount = metadataSummary.missingBothCount;
  const readinessSignals = [
    readyPagesCount > 0,
    attentionPagesCount > 0,
    groupedPages.length > 0,
    followUpPages.length > 0,
  ].filter(Boolean).length;
  const readinessState =
    readinessSignals >= 4
      ? copy.cmsIndexReadinessStateReady
      : readinessSignals >= 2
        ? copy.cmsIndexReadinessStatePartial
        : copy.cmsIndexReadinessStateAttention;
  const preferredCmsReviewState = getPreferredCmsReviewState(
    readyPagesCount,
    attentionPagesCount,
  );
  const cmsReviewPrimaryHref = buildPreferredCmsReviewWindowHref(
    preferredCmsReviewState,
    {
      visibleQuery: searchQuery,
      visibleState,
      visibleSort,
      metadataFocus,
    },
  );
  const cmsReviewPrimaryLabel =
    preferredCmsReviewState === "ready"
      ? copy.cmsIndexReviewReadyCta
      : copy.cmsIndexReviewAttentionCta;
  const sectionLinks = [
    { id: "cms-overview", label: copy.cmsResultSummaryTitle },
    { id: "cms-composition", label: copy.cmsIndexCompositionJourneyTitle },
    { id: "cms-readiness", label: copy.cmsIndexReadinessTitle },
    { id: "cms-review", label: copy.cmsIndexReviewTitle },
    ...(groupedPages.length > 0
      ? [{ id: "cms-groups", label: copy.cmsGroupingTitle }]
      : []),
    { id: "cms-results", label: copy.cmsPageEyebrow },
    ...(totalPages > 1
      ? [{ id: "cms-pagination", label: copy.cmsPaginationTitle }]
      : []),
  ];

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <nav
          aria-label={copy.cmsBreadcrumbLabel}
          className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
        >
          <Link
            href={localizeHref("/", culture)}
            className="transition hover:text-[var(--color-brand)]"
          >
            {copy.cmsBreadcrumbHome}
          </Link>
          <span>/</span>
          <span className="font-medium text-[var(--color-text-primary)]">
            {copy.cmsBreadcrumbIndex}
          </span>
        </nav>

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.cmsIndexEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.cmsIndexTitle}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.cmsIndexDescription}
          </p>
        </div>

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title={copy.cmsIndexDegradedTitle}
            message={formatResource(copy.cmsIndexDegradedMessage, {
              status: statusLabel,
            })}
          />
        )}

        <div
          id="cms-review"
          className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
        >
          <form action={localizeHref("/cms", culture)} className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,0.7fr)_minmax(0,0.7fr)_minmax(0,0.7fr)_auto_auto] lg:items-end">
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.cmsVisibleSearchLabel}
              <input
                type="search"
                name="search"
                defaultValue={searchQuery ?? ""}
                maxLength={80}
                autoComplete="off"
                placeholder={copy.cmsVisibleSearchPlaceholder}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none"
              />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.cmsVisibleStateLabel}
              <select
                name="visibleState"
                defaultValue={visibleState}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none"
              >
                <option value="all">{copy.cmsVisibleStateAllOption}</option>
                <option value="ready">{copy.cmsVisibleStateReadyOption}</option>
                <option value="needs-attention">{copy.cmsVisibleStateNeedsAttentionOption}</option>
              </select>
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.cmsMetadataFocusLabel}
              <select
                name="metadataFocus"
                defaultValue={metadataFocus}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none"
              >
                <option value="all">{copy.cmsMetadataFocusAllOption}</option>
                <option value="missing-title">{copy.cmsMetadataFocusMissingTitleOption}</option>
                <option value="missing-description">{copy.cmsMetadataFocusMissingDescriptionOption}</option>
                <option value="missing-both">{copy.cmsMetadataFocusMissingBothOption}</option>
              </select>
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.cmsVisibleSortLabel}
              <select
                name="visibleSort"
                defaultValue={visibleSort}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none"
              >
                <option value="featured">{copy.cmsVisibleSortFeaturedOption}</option>
                <option value="title-asc">{copy.cmsVisibleSortTitleAscendingOption}</option>
                <option value="ready-first">{copy.cmsVisibleSortReadyFirstOption}</option>
                <option value="attention-first">{copy.cmsVisibleSortAttentionFirstOption}</option>
              </select>
            </label>
            <button
              type="submit"
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.cmsVisibleSearchSubmitCta}
            </button>
            {searchQuery ? (
              <Link
                href={localizeHref(buildCmsHref(1, undefined, "all", "featured", "all"), culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.cmsVisibleSearchClearCta}
              </Link>
            ) : null}
          </form>
          <div className="mt-4">
            <StatusBanner
              title={copy.cmsVisibleSearchInfoTitle}
              message={searchQuery
                ? formatResource(copy.cmsVisibleSearchFilteredMessage, {
                    query: searchQuery,
                  })
                : visibleSort === "ready-first"
                  ? copy.cmsVisibleSortReadyFirstMessage
                  : visibleSort === "attention-first"
                    ? copy.cmsVisibleSortAttentionFirstMessage
                    : visibleSort === "title-asc"
                      ? copy.cmsVisibleSortTitleAscendingMessage
                : metadataFocus === "missing-title"
                  ? copy.cmsMetadataFocusMissingTitleMessage
                  : metadataFocus === "missing-description"
                    ? copy.cmsMetadataFocusMissingDescriptionMessage
                    : metadataFocus === "missing-both"
                      ? copy.cmsMetadataFocusMissingBothMessage
                : visibleState === "ready"
                  ? copy.cmsVisibleStateReadyMessage
                  : visibleState === "needs-attention"
                    ? copy.cmsVisibleStateNeedsAttentionMessage
                    : copy.cmsVisibleSearchInfoMessage}
            />
          </div>
        </div>

        <section className="sticky top-4 z-10 rounded-[1rem] border border-[var(--color-border-soft)] bg-[color:color-mix(in_srgb,var(--color-surface-panel)_92%,white_8%)] px-6 py-5 shadow-[var(--shadow-panel)] backdrop-blur">
          <div className="flex flex-wrap gap-2">
            {sectionLinks.map((section) => (
              <a
                key={section.id}
                href={`#${section.id}`}
                className="inline-flex items-center rounded-full border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {section.label}
              </a>
            ))}
          </div>
        </section>

        <div
          id="cms-overview"
          className="scroll-mt-28 grid gap-5 lg:grid-cols-[minmax(0,1.35fr)_minmax(0,0.65fr)]"
        >
          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.cmsResultSummaryTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {searchQuery
                ? formatResource(copy.cmsResultSummaryFilteredMessage, {
                    visibleCount: pages.length,
                    loadedCount: totalItems,
                    totalItems: matchingItemsTotal,
                    currentPage,
                  })
                : formatResource(copy.cmsResultSummaryMessage, {
                    pageStart,
                    pageEnd,
                    totalItems,
                    currentPage,
                    totalPages,
                  })}
            </p>
          </div>
          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.cmsResultSetTitle}
            </p>
            <div className="mt-4 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsResultSetLoadedLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.cmsResultSetLoadedValue, {
                    count: loadedPageCount,
                  })}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsResultSetVisibleLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.cmsResultSetVisibleValue, {
                    count: totalItems,
                  })}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsResultSetTotalLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.cmsResultSetTotalValue, {
                    count: matchingItemsTotal,
                  })}
                </p>
              </div>
            </div>
          </div>
        </div>

        <div id="cms-composition" className="scroll-mt-28">
          <CmsIndexCompositionWindow
            culture={culture}
            searchQuery={searchQuery}
            visibleState={visibleState}
            visibleSort={visibleSort}
            metadataFocus={metadataFocus}
            currentPage={currentPage}
            totalItems={totalItems}
            pages={pages}
            categories={categories}
            products={products}
            cartSummary={cartSummary}
            reviewHref={cmsReviewPrimaryHref}
            reviewLabel={cmsReviewPrimaryLabel}
          />
        </div>

        <div
          id="cms-readiness"
          className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.cmsIndexReadinessTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.cmsIndexReadinessMessage, {
              status: readinessState,
            })}
          </p>
          <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.cmsIndexReadinessDiscoveryLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {readinessState}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.cmsIndexReadinessReadyLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.cmsIndexReadinessReadyValue, {
                  count: readyPagesCount,
                })}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.cmsIndexReadinessAttentionLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.cmsIndexReadinessAttentionValue, {
                  count: attentionPagesCount,
                })}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.cmsIndexReadinessSupportLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.cmsIndexReadinessSupportValue, {
                  groupCount: groupedPages.length,
                  followUpCount: followUpPages.length,
                })}
              </p>
            </div>
          </div>
        </div>

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.cmsIndexReviewTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.cmsIndexReviewMessage, {
              readyCount: readyPagesCount,
              attentionCount: attentionPagesCount,
            })}
          </p>
          <div className="mt-5 flex flex-wrap gap-3">
            <Link
              href={localizeHref(cmsReviewPrimaryHref, culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {cmsReviewPrimaryLabel}
            </Link>
            <Link
              href={localizeHref(
                buildCmsHref(1, searchQuery, "all", "title-asc", metadataFocus),
                culture,
              )}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.cmsIndexReviewTitleSortCta}
            </Link>
            <Link
              href={localizeHref(
                buildCmsHref(1, undefined, "all", "featured", "all"),
                culture,
              )}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.cmsIndexReviewResetCta}
            </Link>
          </div>
          <div className="mt-4 grid gap-3 md:grid-cols-3">
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.cmsIndexReviewMetaTitleLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.cmsIndexReviewMetaTitleValue, {
                  count: missingMetaTitleCount,
                })}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.cmsIndexReviewMetaDescriptionLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.cmsIndexReviewMetaDescriptionValue, {
                  count: missingMetaDescriptionCount,
                })}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.cmsIndexReviewMissingBothLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.cmsIndexReviewMissingBothValue, {
                  count: missingBothCount,
                })}
              </p>
            </div>
          </div>
          <div className="mt-4 grid gap-3 md:grid-cols-3">
            <Link
              href={localizeHref(
                buildCmsHref(
                  1,
                  searchQuery,
                  visibleState,
                  visibleSort,
                  "missing-title",
                ),
                culture,
              )}
              className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm transition hover:bg-[var(--color-surface-panel)]"
            >
              <p className="font-semibold text-[var(--color-text-primary)]">
                {copy.cmsIndexReviewMissingTitleCta}
              </p>
              <p className="mt-2 text-[var(--color-text-secondary)]">
                {formatResource(copy.cmsIndexReviewMissingTitleMessage, {
                  count: missingMetaTitleCount,
                })}
              </p>
            </Link>
            <Link
              href={localizeHref(
                buildCmsHref(
                  1,
                  searchQuery,
                  visibleState,
                  visibleSort,
                  "missing-description",
                ),
                culture,
              )}
              className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm transition hover:bg-[var(--color-surface-panel)]"
            >
              <p className="font-semibold text-[var(--color-text-primary)]">
                {copy.cmsIndexReviewMissingDescriptionCta}
              </p>
              <p className="mt-2 text-[var(--color-text-secondary)]">
                {formatResource(copy.cmsIndexReviewMissingDescriptionMessage, {
                  count: missingMetaDescriptionCount,
                })}
              </p>
            </Link>
            <Link
              href={localizeHref(
                buildCmsHref(
                  1,
                  searchQuery,
                  visibleState,
                  visibleSort,
                  "missing-both",
                ),
                culture,
              )}
              className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm transition hover:bg-[var(--color-surface-panel)]"
            >
              <p className="font-semibold text-[var(--color-text-primary)]">
                {copy.cmsIndexReviewMissingBothCta}
              </p>
              <p className="mt-2 text-[var(--color-text-secondary)]">
                {formatResource(copy.cmsIndexReviewMissingBothMessage, {
                  count: missingBothCount,
                })}
              </p>
            </Link>
          </div>
          <div className="mt-4">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {copy.cmsIndexReviewTargetsTitle}
            </p>
            <div className="mt-3 grid gap-3">
              {attentionQueue.length > 0 ? (
                attentionQueue.map(({ page, reason }) => (
                  <Link
                    key={page.id}
                    href={localizeHref(
                      buildCmsReviewTargetHref(page.slug, {
                        visibleQuery: searchQuery,
                        visibleState,
                        visibleSort,
                        metadataFocus,
                      }),
                      culture,
                    )}
                    className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 transition hover:bg-[var(--color-surface-panel)]"
                  >
                    <p className="font-semibold text-[var(--color-text-primary)]">
                      {page.title}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {reason}
                    </p>
                  </Link>
                ))
              ) : (
                <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.cmsIndexReviewTargetsFallback}
                </div>
              )}
            </div>
          </div>
        </div>

        {groupedPages.length > 0 && (
          <div
            id="cms-groups"
            className="scroll-mt-28 grid gap-5 lg:grid-cols-[minmax(0,1.35fr)_minmax(0,0.65fr)]"
          >
            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.cmsGroupingTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {searchQuery
                  ? copy.cmsGroupingFilteredMessage
                  : copy.cmsGroupingMessage}
              </p>
              <div className="mt-5 flex flex-wrap gap-2">
                {groupedPages.map((group) => (
                  <a
                    key={group.key}
                    href={`#${buildCmsGroupAnchor(group.key)}`}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {formatResource(copy.cmsGroupingChip, {
                      key: group.key,
                      count: group.items.length,
                    })}
                  </a>
                ))}
              </div>
            </div>
            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.cmsGroupingLargestSetTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {largestGroup
                  ? formatResource(copy.cmsGroupingLargestSetMessage, {
                      key: largestGroup.key,
                      count: largestGroup.items.length,
                    })
                  : copy.cmsGroupingLargestSetFallback}
              </p>
            </div>
          </div>
        )}

        {spotlightPage && (
          <div className="grid gap-5 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
            <article className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-7 shadow-[var(--shadow-panel)] sm:px-8">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.cmsSpotlightEyebrow}
              </p>
              <h2 className="mt-4 text-3xl font-semibold leading-tight text-[var(--color-text-primary)]">
                <Link
                  href={localizeHref(buildCmsPagePath(spotlightPage.slug), culture)}
                  className="transition hover:text-[var(--color-brand)]"
                >
                  {spotlightPage.title}
                </Link>
              </h2>
              <p className="mt-4 max-w-2xl text-sm leading-7 text-[var(--color-text-secondary)]">
                {spotlightPage.metaDescription ?? copy.cmsCardDescriptionFallback}
              </p>
              <div className="mt-6 flex flex-wrap gap-3">
                <Link
                  href={localizeHref(buildCmsPagePath(spotlightPage.slug), culture)}
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.cmsSpotlightPrimaryCta}
                </Link>
                <Link
                  href={localizeHref("/catalog", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.cmsSpotlightSecondaryCta}
                </Link>
              </div>
            </article>

            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-7 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.cmsFollowUpRailTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {followUpPages.length > 0
                  ? copy.cmsFollowUpRailMessage
                  : copy.cmsFollowUpRailFallback}
              </p>
              <div className="mt-5 flex flex-col gap-3">
                {followUpPages.length > 0 ? (
                  followUpPages.map((page, index) => (
                    <Link
                      key={page.id}
                      href={localizeHref(
                        buildCmsReviewTargetHref(page.slug, {
                          visibleQuery: searchQuery,
                          visibleState,
                          visibleSort,
                          metadataFocus,
                        }),
                        culture,
                      )}
                      className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 transition hover:bg-[var(--color-surface-panel)]"
                    >
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                        {formatResource(copy.cmsFollowUpRailItemLabel, {
                          index: index + 1,
                        })}
                      </p>
                      <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                        {page.title}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {page.metaDescription ?? copy.cmsCardDescriptionFallback}
                      </p>
                    </Link>
                  ))
                ) : (
                  <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.cmsFollowUpRailFallback}
                  </div>
                )}
              </div>
            </div>
          </div>
        )}

        <CmsStorefrontSupportWindow
          culture={culture}
          categories={categories}
          categoriesStatus={categoriesStatus}
          products={products}
          productsStatus={productsStatus}
          cartSummary={cartSummary}
        />

        <CmsCommerceCampaignWindow
          culture={culture}
          categories={categories}
          categoriesStatus={categoriesStatus}
          products={products}
          productsStatus={productsStatus}
        />

        <CmsContinuationRail culture={culture} description={copy.cmsCrossSurfaceMessage} />

        <div id="cms-results" className="scroll-mt-28 flex flex-col gap-6">
          {groupedPages.map((group) => (
            <section key={group.key} id={buildCmsGroupAnchor(group.key)} className="flex flex-col gap-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {copy.cmsGroupingSectionEyebrow}
                  </p>
                  <h2 className="mt-2 text-2xl font-semibold text-[var(--color-text-primary)]">
                    {formatResource(copy.cmsGroupingSectionTitle, {
                      key: group.key,
                    })}
                  </h2>
                </div>
                <p className="text-sm text-[var(--color-text-secondary)]">
                  {formatResource(copy.cmsGroupingSectionCount, {
                    count: group.items.length,
                  })}
                </p>
              </div>

              <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
                {group.items.map((page) => (
                  (() => {
                    const missingMetaTitle = !page.metaTitle?.trim();
                    const missingMetaDescription = !page.metaDescription?.trim();

                    return (
                  <article
                    key={page.id}
                    className="flex h-full flex-col rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
                  >
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                      {copy.cmsPageEyebrow}
                    </p>
                    <h3 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
                      <Link
                        href={localizeHref(
                          buildCmsReviewTargetHref(page.slug, {
                            visibleQuery: searchQuery,
                            visibleState,
                            visibleSort,
                            metadataFocus,
                          }),
                          culture,
                        )}
                        className="transition hover:text-[var(--color-brand)]"
                        >
                          {page.title}
                        </Link>
                      </h3>
                      <div className="mt-4 flex flex-wrap gap-2">
                        <span className="rounded-full bg-[var(--color-surface-panel-strong)] px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--color-text-primary)]">
                          {isDiscoveryReadyPage(page)
                            ? copy.cmsCardReadyLabel
                            : copy.cmsCardAttentionLabel}
                        </span>
                        {missingMetaTitle ? (
                          <span className="rounded-full bg-[rgba(217,111,50,0.12)] px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--color-accent)]">
                            {copy.cmsCardMissingMetaTitleLabel}
                          </span>
                        ) : null}
                        {missingMetaDescription ? (
                          <span className="rounded-full bg-[rgba(217,111,50,0.12)] px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--color-accent)]">
                            {copy.cmsCardMissingMetaDescriptionLabel}
                          </span>
                        ) : null}
                      </div>
                    <p className="mt-4 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {page.metaDescription ?? copy.cmsCardDescriptionFallback}
                    </p>
                    <div className="mt-6">
                      <Link
                        href={localizeHref(
                          buildCmsReviewTargetHref(page.slug, {
                            visibleQuery: searchQuery,
                            visibleState,
                            visibleSort,
                            metadataFocus,
                          }),
                          culture,
                        )}
                        className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                      >
                        {copy.cmsOpenPageCta}
                      </Link>
                    </div>
                  </article>
                    );
                  })()
                ))}
              </div>
            </section>
          ))}
        </div>

        {pages.length === 0 && (
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center shadow-[var(--shadow-panel)]">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.cmsNoPagesMessage}
            </p>
            <div className="mt-8 text-left">
              <CmsContinuationRail
                culture={culture}
                title={copy.cmsNoPagesRailTitle}
                description={copy.cmsCrossSurfaceMessage}
              />
            </div>
          </div>
        )}

        {totalPages > 1 && (
          <div
            id="cms-pagination"
            className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.cmsPaginationTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cmsPaginationMessage, {
                currentPage,
                totalPages,
              })}
            </p>
            <div className="mt-5 flex flex-wrap items-center gap-3">
              <Link
                aria-disabled={currentPage <= 1}
                href={localizeHref(
                  buildCmsHref(
                    1,
                    searchQuery,
                    visibleState,
                    visibleSort,
                    metadataFocus,
                  ),
                  culture,
                )}
                className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
              >
                {copy.cmsFirstPageCta}
              </Link>
              <Link
                aria-disabled={currentPage <= 1}
                href={localizeHref(
                  buildCmsHref(
                    Math.max(1, currentPage - 1),
                    searchQuery,
                    visibleState,
                    visibleSort,
                    metadataFocus,
                  ),
                  culture,
                )}
                className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
              >
                {copy.previous}
              </Link>
              <p className="text-sm text-[var(--color-text-secondary)]">
                {formatResource(copy.pageLabel, { currentPage, totalPages })}
              </p>
              <Link
                aria-disabled={currentPage >= totalPages}
                href={localizeHref(
                  buildCmsHref(
                    Math.min(totalPages, currentPage + 1),
                    searchQuery,
                    visibleState,
                    visibleSort,
                    metadataFocus,
                  ),
                  culture,
                )}
                className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
              >
                {copy.next}
              </Link>
              <Link
                aria-disabled={currentPage >= totalPages}
                href={localizeHref(
                  buildCmsHref(
                    totalPages,
                    searchQuery,
                    visibleState,
                    visibleSort,
                    metadataFocus,
                  ),
                  culture,
                )}
                className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
              >
                {copy.cmsLastPageCta}
              </Link>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
