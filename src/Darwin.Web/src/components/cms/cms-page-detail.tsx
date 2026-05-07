import Link from "next/link";
import { CmsContinuationRail } from "@/components/cms/cms-continuation-rail";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCategorySummary, PublicProductSummary } from "@/features/catalog/types";
import { summarizeCmsContent } from "@/features/cms/content-summary";
import type { PublicPageDetail, PublicPageSummary } from "@/features/cms/types";
import { buildCmsPagePath } from "@/lib/entity-paths";
import { resolveRelativeHtmlMediaUrls, sanitizeHtmlFragment } from "@/lib/html-fragment";
import { localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";
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
  relatedPages,
  relatedStatus,
}: CmsPageDetailProps) {
  const copy = getSharedResource(culture);
  const resolvedMessage = resolveLocalizedQueryMessage(message, copy);
  const statusLabel = resolveApiStatusLabel(status, copy) ?? status;
  const relatedStatusLabel = resolveApiStatusLabel(relatedStatus, copy) ?? relatedStatus;
  const contentSummary = page
    ? summarizeCmsContent(page.contentHtml)
    : null;
  const sanitizedContentHtml = page
    ? resolveRelativeHtmlMediaUrls(
        contentSummary?.html ?? sanitizeHtmlFragment(page.contentHtml),
        toWebApiUrl,
      )
    : "";
  const detailRouteSummaryMessage = formatResource(copy.cmsDetailRouteSummaryMessage, {
    status: statusLabel,
    relatedStatus: relatedStatusLabel,
    relatedCount: relatedPages.length,
  });
  if (!page) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
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
          <div className="mt-6 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5">
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

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <article
          id="cms-detail-content"
          className="w-full scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8 lg:px-12"
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
        </article>
    </section>
  );
}

