import Link from "next/link";
import { CmsContinuationRail } from "@/components/cms/cms-continuation-rail";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCategorySummary, PublicProductSummary } from "@/features/catalog/types";
import { summarizeCmsContent } from "@/features/cms/content-summary";
import type { PublicPageDetail, PublicPageSummary } from "@/features/cms/types";
import { formatMoney } from "@/lib/formatting";
import { sanitizeHtmlFragment } from "@/lib/html-fragment";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";
import {
  formatResource,
  getSharedResource,
  resolveLocalizedQueryMessage,
} from "@/localization";

type CmsPageDetailProps = {
  culture: string;
  page: PublicPageDetail | null;
  status: string;
  message?: string;
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
  categories,
  categoriesStatus,
  products,
  productsStatus,
  cartSummary,
}: CmsPageDetailProps) {
  const copy = getSharedResource(culture);
  const resolvedMessage = resolveLocalizedQueryMessage(message, copy);
  const pageReference = page ? localizeHref(`/cms/${page.slug}`, culture) : null;
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
  if (!page) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.cmsPageUnavailableTitle}
            message={resolvedMessage ?? formatResource(copy.cmsDetailWarningsMessage, { status })}
          />
          <div className="mt-8">
            <CmsContinuationRail
              culture={culture}
              title={copy.cmsPageUnavailableTitle}
              description={copy.cmsFollowUpDescription}
            />
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <article className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8 lg:px-12">
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
                message={resolvedMessage ?? formatResource(copy.cmsDetailWarningsMessage, { status })}
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
                  href={localizeHref(`/cms/${previousPage.slug}`, culture)}
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
                  href={localizeHref(`/cms/${nextPage.slug}`, culture)}
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

          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.cmsContentNavigationTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.cmsContentNavigationDescription}
            </p>
            <div className="mt-5 flex flex-col gap-2">
              <Link
                href={localizeHref("/cms", culture)}
                className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.cmsAllPublishedPagesCta}
              </Link>
              {relatedPages.length > 0 ? (
                relatedPages.map((relatedPage) => (
                  <Link
                    key={relatedPage.id}
                    href={localizeHref(`/cms/${relatedPage.slug}`, culture)}
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

          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                  {copy.cmsCatalogWindowTitle}
                </p>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.cmsCatalogWindowMessage, {
                    categoriesStatus,
                    categoryCount: categories.length,
                  })}
                </p>
              </div>
              <Link
                href={localizeHref("/catalog", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.cmsCatalogWindowCta}
              </Link>
            </div>
            {categories.length > 0 ? (
              <div className="mt-5 grid gap-3">
                {categories.map((category) => (
                  <Link
                    key={category.id}
                    href={localizeHref(
                      buildAppQueryPath("/catalog", { category: category.slug }),
                      culture,
                    )}
                    className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 transition hover:bg-[var(--color-surface-panel)]"
                  >
                    <p className="font-semibold text-[var(--color-text-primary)]">
                      {category.name}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {category.description ??
                        copy.cmsCatalogWindowFallbackDescription}
                    </p>
                  </Link>
                ))}
              </div>
            ) : (
              <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.cmsCatalogWindowEmptyMessage, {
                  status: categoriesStatus,
                })}
              </p>
            )}
          </div>

          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                  {copy.cmsProductsWindowTitle}
                </p>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.cmsProductsWindowMessage, {
                    productsStatus,
                    productCount: products.length,
                  })}
                </p>
              </div>
              <Link
                href={localizeHref("/catalog", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.cmsProductsWindowCta}
              </Link>
            </div>
            {products.length > 0 ? (
              <div className="mt-5 grid gap-3">
                {products.map((product) => {
                  const productImageUrl = toWebApiUrl(product.primaryImageUrl ?? "");
                  return (
                    <Link
                      key={product.id}
                      href={localizeHref(`/catalog/${product.slug}`, culture)}
                      className="grid gap-3 rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 transition hover:bg-[var(--color-surface-panel)] md:grid-cols-[72px_minmax(0,1fr)]"
                    >
                      <div className="flex h-[72px] items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-2">
                        {productImageUrl ? (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img
                            src={productImageUrl}
                            alt={product.name}
                            className="max-h-14 w-auto object-contain"
                          />
                        ) : (
                          <span className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                            {copy.noImage}
                          </span>
                        )}
                      </div>
                      <div>
                        <p className="font-semibold text-[var(--color-text-primary)]">
                          {product.name}
                        </p>
                        <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {product.shortDescription ??
                            copy.cmsProductsWindowFallbackDescription}
                        </p>
                        <p className="mt-2 text-sm font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(product.priceMinor, product.currency, culture)}
                        </p>
                      </div>
                    </Link>
                  );
                })}
              </div>
            ) : (
              <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.cmsProductsWindowEmptyMessage, {
                  status: productsStatus,
                })}
              </p>
            )}
          </div>

          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.cmsCartWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {cartSummary
                ? formatResource(copy.cmsCartWindowMessage, {
                    itemCount: cartSummary.itemCount,
                    total: formatMoney(
                      cartSummary.grandTotalGrossMinor,
                      cartSummary.currency,
                      culture,
                    ),
                  })
                : copy.cmsCartWindowFallback}
            </p>
            <div className="mt-5 flex flex-wrap gap-3">
              <Link
                href={localizeHref("/cart", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.cmsCartWindowCartCta}
              </Link>
              <Link
                href={localizeHref("/checkout", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.cmsCartWindowCheckoutCta}
              </Link>
            </div>
          </div>

          <CmsContinuationRail culture={culture} description={copy.cmsFollowUpDescription} />

          {relatedStatus !== "ok" && (
            <StatusBanner
              tone="warning"
              title={copy.cmsRelatedPagesDegradedTitle}
              message={formatResource(copy.cmsRelatedPagesDegradedMessage, {
                status: relatedStatus,
              })}
            />
          )}

          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.cmsDetailRouteSummaryTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cmsDetailRouteSummaryMessage, {
                status,
                relatedStatus,
                relatedCount: relatedPages.length,
              })}
            </p>
          </div>
        </aside>
      </div>
    </section>
  );
}
