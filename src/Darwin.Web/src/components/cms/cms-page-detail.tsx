import Link from "next/link";
import { CmsCommerceCampaignWindow } from "@/components/cms/cms-commerce-campaign-window";
import { CmsContentCompositionWindow } from "@/components/cms/cms-content-composition-window";
import { CmsContinuationRail } from "@/components/cms/cms-continuation-rail";
import { CmsStorefrontSupportWindow } from "@/components/cms/cms-storefront-support-window";
import { StatusBanner } from "@/components/feedback/status-banner";
import { SurfaceSectionNav } from "@/components/layout/surface-section-nav";
import type { PublicCategorySummary, PublicProductSummary } from "@/features/catalog/types";
import { summarizeCmsContent } from "@/features/cms/content-summary";
import {
  getCmsReviewTarget,
  isCmsReviewTargetPending,
} from "@/features/cms/discovery";
import {
  buildCmsReviewTargetHref,
} from "@/features/review/review-window";
import {
  buildPreferredCmsReviewWindowHref,
  getPendingCmsReviewQueueState,
} from "@/features/review/review-workflow";
import type { PublicPageDetail, PublicPageSummary } from "@/features/cms/types";
import { buildCmsPagePath } from "@/lib/entity-paths";
import { sanitizeHtmlFragment } from "@/lib/html-fragment";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import {
  formatResource,
  getSharedResource,
  resolveApiStatusLabel,
  resolveLocalizedQueryMessage,
} from "@/localization";

type CmsPageDetailProps = {
  culture: string;
  page: PublicPageDetail | null;
  status: string;
  message?: string;
  reviewWindow?: {
    visibleQuery?: string;
    visibleState?: "all" | "ready" | "needs-attention";
    visibleSort?: "featured" | "title-asc" | "ready-first" | "attention-first";
    metadataFocus?: "all" | "missing-title" | "missing-description" | "missing-both";
  };
  relatedPages: PublicPageSummary[];
  relatedStatus: string;
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

export function CmsPageDetail({
  culture,
  page,
  status,
  message,
  reviewWindow,
  relatedPages,
  relatedStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  cartSummary,
}: CmsPageDetailProps) {
  const copy = getSharedResource(culture);
  const resolvedMessage = resolveLocalizedQueryMessage(message, copy);
  const statusLabel = resolveApiStatusLabel(status, copy) ?? status;
  const relatedStatusLabel = resolveApiStatusLabel(relatedStatus, copy) ?? relatedStatus;
  const pageReference = page ? localizeHref(buildCmsPagePath(page.slug), culture) : null;
  const pagePath = page ? buildCmsPagePath(page.slug) : "/cms";
  const contentSummary = page
    ? summarizeCmsContent(page.contentHtml)
    : null;
  const sanitizedContentHtml = page
    ? contentSummary?.html ?? sanitizeHtmlFragment(page.contentHtml)
    : "";
  const currentPageIndex = page
    ? relatedPages.findIndex((entry) => entry.slug === page.slug)
    : -1;
  const currentPagePosition = currentPageIndex >= 0 ? currentPageIndex + 1 : null;
  const previousPage =
    currentPageIndex > 0 ? relatedPages[currentPageIndex - 1] : null;
  const nextPage =
    currentPageIndex >= 0 && currentPageIndex < relatedPages.length - 1
      ? relatedPages[currentPageIndex + 1]
      : null;
  const hasMetaTitle = Boolean(page?.metaTitle?.trim());
  const hasMetaDescription = Boolean(page?.metaDescription?.trim());
  const hasStructuredSections = Boolean(contentSummary && contentSummary.headings.length > 0);
  const hasReadingDepth = Boolean(contentSummary && contentSummary.wordCount >= 120);
  const discoveryReadySignals = [
    hasMetaTitle,
    hasMetaDescription,
    hasStructuredSections,
    hasReadingDepth,
  ].filter(Boolean).length;
  const discoveryReadinessKey =
    discoveryReadySignals >= 4
      ? copy.cmsReadinessStateReady
      : discoveryReadySignals >= 2
        ? copy.cmsReadinessStatePartial
        : copy.cmsReadinessStateAttention;
  const navigationCoverageKey =
    relatedPages.length > 0
      ? copy.cmsReadinessStateReady
      : copy.cmsReadinessStateAttention;
  const cmsReviewPrimaryHref = buildPreferredCmsReviewWindowHref(
    discoveryReadySignals >= 4 ? "ready" : "needs-attention",
    reviewWindow,
  );
  const cmsReviewPrimaryLabel =
    discoveryReadySignals >= 4
      ? copy.cmsReviewWindowReadyCta
      : copy.cmsReviewWindowAttentionCta;
  const detailRouteSummaryMessage = formatResource(copy.cmsDetailRouteSummaryMessage, {
    status: statusLabel,
    relatedStatus: relatedStatusLabel,
    relatedCount: relatedPages.length,
  });
  if (!page) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.cmsPageUnavailableTitle}
            message={
              resolvedMessage ??
              formatResource(copy.cmsDetailWarningsMessage, {
                status: statusLabel,
              })
            }
          />
          <div className="mt-6 rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.cmsDetailRouteSummaryTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {detailRouteSummaryMessage}
            </p>
          </div>
          <div className="mt-8">
            <CmsContinuationRail
              culture={culture}
              title={copy.cmsPageUnavailableTitle}
              description={detailRouteSummaryMessage}
              items={[
                {
                  id: "cms-page-unavailable-index",
                  label: copy.cmsBreadcrumbIndex,
                  title: copy.cmsBreadcrumbIndex,
                  description: copy.cmsFollowUpDescription,
                  href: "/cms",
                  ctaLabel: copy.cmsFollowUpHomeCta,
                },
              ]}
            />
          </div>
        </div>
      </section>
    );
  }

  const currentReviewTarget = getCmsReviewTarget(page);
  const currentNeedsReview = isCmsReviewTargetPending(currentReviewTarget);
  const reviewQueueState = getPendingCmsReviewQueueState(relatedPages, {
    currentSlug: page.slug,
  });
  const reviewQueue = reviewQueueState.queue;
  const nextReviewPage = reviewQueueState.nextTarget;
  const reviewQueuePreview = reviewQueueState.previewTargets;
  const sectionLinks = [
    { href: "#cms-detail-content", label: copy.cmsPageEyebrow },
    { href: "#cms-detail-readiness", label: copy.cmsReadinessTitle },
    { href: "#cms-detail-review", label: copy.cmsReviewQueueTitle },
    { href: "#cms-detail-composition", label: copy.cmsDetailRouteSummaryTitle },
    { href: "#cms-detail-support", label: copy.cmsContentNavigationTitle },
  ];

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-6">
        <SurfaceSectionNav items={sectionLinks} />

      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <article
          id="cms-detail-content"
          className="scroll-mt-28 rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8 lg:px-12"
        >
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
            <Link
              href={localizeHref("/cms", culture)}
              className="transition hover:text-[var(--color-brand)]"
            >
              {copy.cmsBreadcrumbIndex}
            </Link>
            <span>/</span>
            <span className="font-medium text-[var(--color-text-primary)]">
              {page.title}
            </span>
          </nav>

          <div className="max-w-3xl">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {copy.cmsPageEyebrow}
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {page.title}
            </h1>
            {page.metaDescription ? (
              <p className="mt-5 text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {page.metaDescription}
              </p>
            ) : null}
          </div>

          {status !== "ok" && (
            <div className="mt-8">
              <StatusBanner
                tone="warning"
                title={copy.cmsDetailWarningsTitle}
                message={resolvedMessage ?? formatResource(copy.cmsDetailWarningsMessage, { status: statusLabel })}
              />
            </div>
          )}

          <div
            className="cms-content mt-8 max-w-none"
            dangerouslySetInnerHTML={{ __html: sanitizedContentHtml }}
          />

          {(previousPage || nextPage) && (
            <div className="mt-10 grid gap-4 border-t border-[var(--color-border-soft)] pt-8 md:grid-cols-2">
              {previousPage ? (
                <Link
                  href={localizeHref(
                    buildCmsReviewTargetHref(previousPage.slug, reviewWindow),
                    culture,
                  )}
                  className="rounded-[1.75rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5 transition hover:bg-[var(--color-surface-panel)]"
                >
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.cmsPreviousPageLabel}
                  </p>
                  <p className="mt-3 text-lg font-semibold text-[var(--color-text-primary)]">
                    {previousPage.title}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {previousPage.metaDescription ?? copy.cmsAdjacentPageFallback}
                  </p>
                </Link>
              ) : (
                <div className="rounded-[1.75rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-5 py-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.cmsPreviousPageEmptyMessage}
                </div>
              )}

              {nextPage ? (
                <Link
                  href={localizeHref(
                    buildCmsReviewTargetHref(nextPage.slug, reviewWindow),
                    culture,
                  )}
                  className="rounded-[1.75rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5 transition hover:bg-[var(--color-surface-panel)]"
                >
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.cmsNextPageLabel}
                  </p>
                  <p className="mt-3 text-lg font-semibold text-[var(--color-text-primary)]">
                    {nextPage.title}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {nextPage.metaDescription ?? copy.cmsAdjacentPageFallback}
                  </p>
                </Link>
              ) : (
                <div className="rounded-[1.75rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-5 py-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.cmsNextPageEmptyMessage}
                </div>
              )}
            </div>
          )}
        </article>

        <aside className="flex flex-col gap-5">
          {contentSummary && contentSummary.headings.length > 0 && (
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.cmsOnThisPageTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.cmsOnThisPageDescription}
              </p>
              <div className="mt-5 flex flex-col gap-2">
                {contentSummary.headings.map((heading) => (
                  <a
                    key={heading.id}
                    href={`#${heading.id}`}
                    className={
                      heading.level === 3
                        ? "ml-4 rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-medium text-[var(--color-text-secondary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                        : "rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    }
                  >
                    {heading.text}
                  </a>
                ))}
              </div>
            </div>
          )}

          {contentSummary && (
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.cmsReadingStatsTitle}
              </p>
              <div className="mt-4 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.cmsReadingMinutesLabel}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {formatResource(copy.cmsReadingMinutesValue, {
                      minutes: contentSummary.readingMinutes,
                    })}
                  </p>
                </div>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.cmsWordCountLabel}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {formatResource(copy.cmsWordCountValue, {
                      count: contentSummary.wordCount,
                    })}
                  </p>
                </div>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.cmsParagraphCountLabel}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {formatResource(copy.cmsParagraphCountValue, {
                      count: contentSummary.paragraphCount,
                    })}
                  </p>
                </div>
              </div>
            </div>
          )}

          <div
            id="cms-detail-readiness"
            className="scroll-mt-28 rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.cmsReadinessTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cmsReadinessDescription, {
                status: discoveryReadinessKey,
              })}
            </p>
            <div className="mt-4 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsReadinessDiscoveryLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {discoveryReadinessKey}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsReadinessMetadataLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.cmsReadinessMetadataValue, {
                    current: Number(hasMetaTitle) + Number(hasMetaDescription),
                    total: 2,
                  })}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsReadinessStructureLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.cmsReadinessStructureValue, {
                    headings: contentSummary?.headings.length ?? 0,
                    paragraphs: contentSummary?.paragraphCount ?? 0,
                  })}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsReadinessNavigationLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {navigationCoverageKey}
                </p>
              </div>
            </div>
          </div>

          <div
            id="cms-detail-review"
            className="scroll-mt-28 rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.cmsReviewWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cmsReviewWindowMessage, {
                status: discoveryReadinessKey,
              })}
            </p>
            <div className="mt-5 flex flex-col gap-3">
              <Link
                href={localizeHref(cmsReviewPrimaryHref, culture)}
                className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {cmsReviewPrimaryLabel}
              </Link>
              <Link
                href={localizeHref(
                  buildAppQueryPath("/cms", {
                    visibleSort: "title-asc",
                    visibleQuery: reviewWindow?.visibleQuery,
                    visibleState:
                      reviewWindow?.visibleState !== "all"
                        ? reviewWindow?.visibleState
                        : undefined,
                    metadataFocus:
                      reviewWindow?.metadataFocus &&
                      reviewWindow.metadataFocus !== "all"
                        ? reviewWindow.metadataFocus
                        : undefined,
                  }),
                  culture,
                )}
                className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.cmsReviewWindowBrowseAllCta}
              </Link>
            </div>
          </div>

          <div
            id="cms-detail-support"
            className="scroll-mt-28 rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.cmsNextReviewTargetTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {nextReviewPage
                ? nextReviewPage.missingMetaTitle && nextReviewPage.missingMetaDescription
                  ? copy.cmsNextReviewTargetBothMessage
                  : nextReviewPage.missingMetaTitle
                    ? copy.cmsNextReviewTargetMetaTitleMessage
                    : copy.cmsNextReviewTargetMetaDescriptionMessage
                : copy.cmsNextReviewTargetFallback}
            </p>
            {nextReviewPage ? (
              <div className="mt-4 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="font-semibold text-[var(--color-text-primary)]">
                  {nextReviewPage.page.title}
                </p>
                <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {nextReviewPage.page.metaDescription ?? copy.cmsAdjacentPageFallback}
                </p>
                <div className="mt-4">
                  <Link
                    href={localizeHref(
                      buildCmsReviewTargetHref(nextReviewPage.page.slug, reviewWindow),
                      culture,
                    )}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                  >
                    {copy.cmsNextReviewTargetCta}
                  </Link>
                </div>
              </div>
            ) : null}
          </div>

          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.cmsReviewQueueTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cmsReviewQueueMessage, {
                currentStatus: currentNeedsReview
                  ? copy.cmsReviewQueueCurrentPending
                  : copy.cmsReviewQueueCurrentReady,
                remainingCount: reviewQueue.length,
              })}
            </p>
            <div className="mt-4 grid gap-3">
              {reviewQueuePreview.length > 0 ? (
                reviewQueuePreview.map((target) => (
                  <Link
                    key={target.page.id}
                    href={localizeHref(
                      buildCmsReviewTargetHref(target.page.slug, reviewWindow),
                      culture,
                    )}
                    className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)] transition hover:bg-[var(--color-surface-panel)]"
                  >
                    <p className="font-semibold text-[var(--color-text-primary)]">
                      {target.page.title}
                    </p>
                    <p className="mt-2">
                      {target.missingMetaTitle && target.missingMetaDescription
                        ? copy.cmsReviewQueueBothMessage
                        : target.missingMetaTitle
                          ? copy.cmsReviewQueueMetaTitleMessage
                          : copy.cmsReviewQueueMetaDescriptionMessage}
                    </p>
                  </Link>
                ))
              ) : (
                <div className="rounded-[1.5rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.cmsReviewQueueFallback}
                </div>
              )}
            </div>
          </div>

          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.cmsContentNavigationTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.cmsContentNavigationDescription}
            </p>
            <div className="mt-5 flex flex-col gap-2">
              <Link
                href={localizeHref(
                  buildAppQueryPath("/cms", {
                    visibleQuery: reviewWindow?.visibleQuery,
                    visibleState:
                      reviewWindow?.visibleState !== "all"
                        ? reviewWindow?.visibleState
                        : undefined,
                    visibleSort:
                      reviewWindow?.visibleSort &&
                      reviewWindow.visibleSort !== "featured"
                        ? reviewWindow.visibleSort
                        : undefined,
                    metadataFocus:
                      reviewWindow?.metadataFocus &&
                      reviewWindow.metadataFocus !== "all"
                        ? reviewWindow.metadataFocus
                        : undefined,
                  }),
                  culture,
                )}
                className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.cmsAllPublishedPagesCta}
              </Link>
              {relatedPages.length > 0 ? (
                relatedPages.map((relatedPage) => (
                  <Link
                    key={relatedPage.id}
                    href={localizeHref(
                      buildCmsReviewTargetHref(relatedPage.slug, reviewWindow),
                      culture,
                    )}
                    className={
                      relatedPage.slug === page.slug
                        ? "rounded-2xl bg-[var(--color-brand)] px-4 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
                        : "rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    }
                  >
                    {relatedPage.title}
                  </Link>
                ))
              ) : (
                <div className="rounded-[1.5rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.cmsRelatedPagesEmptyMessage}
                </div>
              )}
            </div>
          </div>

          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.cmsPageReferenceTitle}
            </p>
            <div className="mt-4 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsReferenceSlugLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">{page.slug}</p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsReferenceMetaTitleLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {page.metaTitle ?? copy.cmsReferenceMetaFallback}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsReferencePathLabel}
                </p>
                <p className="mt-2 break-all font-semibold text-[var(--color-text-primary)]">
                  {pageReference}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.cmsReferencePositionLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {currentPagePosition
                    ? formatResource(copy.cmsReferencePositionValue, {
                        current: currentPagePosition,
                        total: relatedPages.length,
                      })
                    : copy.cmsReferencePositionFallback}
                </p>
              </div>
            </div>
          </div>

          <div id="cms-detail-composition" className="scroll-mt-28">
            <CmsContentCompositionWindow
              culture={culture}
              page={page}
              pagePath={pagePath}
              headings={contentSummary?.headings ?? []}
              readingMinutes={contentSummary?.readingMinutes ?? 1}
              relatedPages={relatedPages}
              categories={categories}
              products={products}
              cartSummary={cartSummary}
              reviewWindow={reviewWindow}
              reviewPrimaryHref={cmsReviewPrimaryHref}
              reviewPrimaryLabel={cmsReviewPrimaryLabel}
              reviewNextPage={nextReviewPage?.page ?? null}
            />
          </div>

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

          <CmsContinuationRail culture={culture} description={copy.cmsFollowUpDescription} />

          {relatedStatus !== "ok" && (
              <StatusBanner
                tone="warning"
                title={copy.cmsRelatedPagesDegradedTitle}
                message={formatResource(copy.cmsRelatedPagesDegradedMessage, {
                status: relatedStatusLabel,
                })}
              />
          )}

          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.cmsDetailRouteSummaryTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {detailRouteSummaryMessage}
            </p>
          </div>
        </aside>
      </div>
      </div>
    </section>
  );
}

