import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type {
  CatalogVisibleSort,
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import { formatMoney } from "@/lib/formatting";
import { localizeHref } from "@/lib/locale-routing";
import { formatResource, getCatalogResource } from "@/localization";

type CatalogPageProps = {
  culture: string;
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
  activeCategorySlug?: string;
  totalProducts: number;
  currentPage: number;
  pageSize: number;
  visibleQuery?: string;
  visibleSort?: CatalogVisibleSort;
  loadedProductsCount: number;
  dataStatus?: {
    categories: string;
    products: string;
  };
};

function buildCatalogHref(
  categorySlug?: string,
  page = 1,
  visibleQuery?: string,
  visibleSort?: CatalogVisibleSort,
) {
  const params = new URLSearchParams();
  if (categorySlug) {
    params.set("category", categorySlug);
  }
  if (page > 1) {
    params.set("page", String(page));
  }
  if (visibleQuery) {
    params.set("visibleQuery", visibleQuery);
  }
  if (visibleSort && visibleSort !== "featured") {
    params.set("visibleSort", visibleSort);
  }

  const query = params.toString();
  return query ? `/catalog?${query}` : "/catalog";
}

export function CatalogPage({
  culture,
  categories,
  products,
  activeCategorySlug,
  totalProducts,
  currentPage,
  pageSize,
  visibleQuery,
  visibleSort = "featured",
  loadedProductsCount,
  dataStatus,
}: CatalogPageProps) {
  const copy = getCatalogResource(culture);
  const hasProducts = products.length > 0;
  const totalPages = Math.max(1, Math.ceil(totalProducts / pageSize));
  const pageStart = totalProducts === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const pageEnd =
    totalProducts === 0
      ? 0
      : Math.min(totalProducts, pageStart + loadedProductsCount - 1);
  const activeCategory =
    categories.find((category) => category.slug === activeCategorySlug) ?? null;
  const offerProducts = products.filter(
    (product) =>
      typeof product.compareAtPriceMinor === "number" &&
      product.compareAtPriceMinor > product.priceMinor,
  );
  const hasVisibleLens = Boolean(visibleQuery) || visibleSort !== "featured";

  function getSavingsPercent(product: PublicProductSummary) {
    if (
      typeof product.compareAtPriceMinor !== "number" ||
      product.compareAtPriceMinor <= product.priceMinor
    ) {
      return null;
    }

    return Math.round(
      ((product.compareAtPriceMinor - product.priceMinor) /
        product.compareAtPriceMinor) *
        100,
    );
  }

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--color-brand)]">
            {copy.heroEyebrow}
          </p>
          <div className="mt-4 flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <h1 className="font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {copy.heroTitle}
              </h1>
              <p className="mt-4 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {copy.heroDescription}
              </p>
              {activeCategory ? (
                <div className="mt-5 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-accent)]">
                    {copy.activeCategoryEyebrow}
                  </p>
                  <p className="mt-2 text-xl font-semibold text-[var(--color-text-primary)]">
                    {activeCategory.name}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {activeCategory.description ??
                      copy.categoryFallbackDescription}
                  </p>
                </div>
              ) : null}
            </div>
            <div className="grid gap-4 sm:grid-cols-3 lg:w-[24rem]">
              {[
                {
                  label: hasVisibleLens
                    ? copy.visibleResultsLabel
                    : copy.currentResultsLabel,
                  value: String(hasVisibleLens ? products.length : totalProducts),
                  note: hasVisibleLens
                    ? copy.visibleResultsNote
                    : copy.currentResultsNote,
                },
                {
                  label: copy.visibleOffersLabel,
                  value: String(offerProducts.length),
                  note: copy.visibleOffersNote,
                },
                {
                  label: copy.categoriesLabel,
                  value: String(categories.length),
                  note: copy.categoriesNote,
                },
              ].map((item) => (
                <div
                  key={item.label}
                  className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-4"
                >
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
                    {item.label}
                  </p>
                  <p className="mt-2 text-2xl font-semibold text-[var(--color-text-primary)]">
                    {item.value}
                  </p>
                  <p className="text-sm text-[var(--color-text-secondary)]">
                    {item.note}
                  </p>
                </div>
              ))}
            </div>
          </div>
        </div>

        {(dataStatus?.categories !== "ok" || dataStatus?.products !== "ok") && (
          <StatusBanner
            tone="warning"
            title={copy.degradedTitle}
            message={formatResource(copy.degradedMessage, {
              categoriesStatus: dataStatus?.categories ?? "unknown",
              productsStatus: dataStatus?.products ?? "unknown",
            })}
          />
        )}

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.catalogRouteSummaryTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.catalogRouteSummaryMessage, {
              categoriesStatus: dataStatus?.categories ?? "unknown",
              productsStatus: dataStatus?.products ?? "unknown",
              visibleCount: products.length,
              totalProducts,
              currentPage,
              totalPages,
            })}
          </p>
        </div>

        {hasVisibleLens && (
          <StatusBanner
            title={copy.visibleLensActiveTitle}
            message={formatResource(copy.visibleLensActiveMessage, {
              count: products.length,
              loadedCount: loadedProductsCount,
              serverTotal: totalProducts,
            })}
          />
        )}

        <div className="grid gap-5 lg:grid-cols-[minmax(0,1.35fr)_minmax(0,0.65fr)]">
          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.resultSummaryTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {hasVisibleLens
                ? formatResource(copy.resultSummaryFilteredMessage, {
                    visibleCount: products.length,
                    loadedCount: loadedProductsCount,
                    totalProducts,
                    currentPage,
                  })
                : formatResource(copy.resultSummaryMessage, {
                    pageStart,
                    pageEnd,
                    totalProducts,
                    currentPage,
                    totalPages,
                  })}
            </p>
          </div>
          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.resultSetTitle}
            </p>
            <div className="mt-4 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.resultSetLoadedLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.resultSetLoadedValue, {
                    count: loadedProductsCount,
                  })}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.resultSetVisibleLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.resultSetVisibleValue, {
                    count: products.length,
                  })}
                </p>
              </div>
              <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.resultSetTotalLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.resultSetTotalValue, {
                    count: totalProducts,
                  })}
                </p>
              </div>
            </div>
          </div>
        </div>

        <div className="grid gap-8 lg:grid-cols-[280px_minmax(0,1fr)]">
          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-5 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.categoriesTitle}
            </p>

            <form
              action={localizeHref("/catalog", culture)}
              method="get"
              className="mt-5 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
            >
              {activeCategorySlug ? (
                <input type="hidden" name="category" value={activeCategorySlug} />
              ) : null}
              <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-accent)]">
                {copy.visibleToolsTitle}
              </p>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.visibleToolsDescription}
              </p>
              <label className="mt-4 flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.visibleSearchLabel}
                <input
                  name="visibleQuery"
                  defaultValue={visibleQuery}
                  placeholder={copy.visibleSearchPlaceholder}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 text-sm font-normal outline-none"
                />
              </label>
              <label className="mt-4 flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.sortLabel}
                <select
                  name="visibleSort"
                  defaultValue={visibleSort}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 text-sm font-normal outline-none"
                >
                  <option value="featured">{copy.sortDefaultOption}</option>
                  <option value="name-asc">{copy.sortNameAscendingOption}</option>
                  <option value="price-asc">{copy.sortPriceAscendingOption}</option>
                  <option value="price-desc">{copy.sortPriceDescendingOption}</option>
                  <option value="savings-desc">{copy.sortSavingsDescendingOption}</option>
                </select>
              </label>
              <div className="mt-4 flex flex-wrap gap-3">
                <button
                  type="submit"
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.applyVisibleToolsCta}
                </button>
                <Link
                  href={localizeHref(buildCatalogHref(activeCategorySlug), culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-white/70"
                >
                  {copy.resetVisibleToolsCta}
                </Link>
              </div>
            </form>

            {activeCategory ? (
              <div className="mt-4 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                <p className="font-semibold text-[var(--color-text-primary)]">
                  {copy.browsingCategoryPrefix} {activeCategory.name}
                </p>
                <p>
                  {activeCategory.description ??
                    copy.categoryFallbackDescription}
                </p>
                <Link
                  href={localizeHref("/catalog", culture)}
                  className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-white/70"
                >
                  {copy.clearCategory}
                </Link>
              </div>
            ) : null}
            <div className="mt-5 flex flex-col gap-2">
              <Link
                href={localizeHref(
                  buildCatalogHref(undefined, 1, visibleQuery, visibleSort),
                  culture,
                )}
                className={
                  !activeCategorySlug
                    ? "rounded-2xl bg-[var(--color-brand)] px-4 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
                    : "rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-secondary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                }
              >
                {copy.allProducts}
              </Link>
              {categories.map((category) => (
                <Link
                  key={category.id}
                  href={localizeHref(
                    buildCatalogHref(category.slug, 1, visibleQuery, visibleSort),
                    culture,
                  )}
                  className={
                    activeCategorySlug === category.slug
                      ? "rounded-2xl bg-[var(--color-brand)] px-4 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
                      : "rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-secondary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  }
                >
                  <span className="block text-[var(--color-text-primary)]">
                    {category.name}
                  </span>
                  {category.description && (
                    <span className="mt-1 block text-xs font-normal text-inherit">
                      {category.description}
                    </span>
                  )}
                </Link>
              ))}
            </div>
          </aside>

          <div className="flex flex-col gap-6">
            {!hasProducts ? (
              <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
                  {copy.noResultsEyebrow}
                </p>
                <h2 className="mt-4 font-[family-name:var(--font-display)] text-3xl text-[var(--color-text-primary)]">
                  {copy.noResultsTitle}
                </h2>
                <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)]">
                  {copy.noResultsDescription}
                </p>
                <div className="mt-6 flex flex-wrap justify-center gap-3">
                  <Link
                    href={localizeHref("/", culture)}
                    className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                  >
                    {copy.noResultsHomeCta}
                  </Link>
                  <Link
                    href={localizeHref("/cms", culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.noResultsCmsCta}
                  </Link>
                </div>
              </div>
            ) : (
              <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
                {products.map((product) => (
                  <article
                    key={product.id}
                    className="flex h-full flex-col rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-5 shadow-[var(--shadow-panel)]"
                  >
                    <div className="flex min-h-48 items-center justify-center rounded-[1.5rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-6">
                      {product.primaryImageUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img
                          src={product.primaryImageUrl}
                          alt={product.name}
                          className="max-h-36 w-auto object-contain"
                        />
                      ) : (
                        <span className="text-sm font-semibold uppercase tracking-[0.2em] text-[var(--color-text-muted)]">
                          {copy.noMedia}
                        </span>
                      )}
                    </div>
                    <div className="mt-5 flex flex-1 flex-col">
                      <div className="flex flex-wrap items-center gap-2">
                        <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-accent)]">
                          {copy.productEyebrow}
                        </p>
                        {getSavingsPercent(product) ? (
                          <span className="rounded-full bg-[var(--color-brand)] px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--color-brand-contrast)]">
                            {copy.savePrefix} {getSavingsPercent(product)}%
                          </span>
                        ) : null}
                      </div>
                      <h2 className="mt-3 text-xl font-semibold text-[var(--color-text-primary)]">
                        <Link
                          href={localizeHref(`/catalog/${product.slug}`, culture)}
                          className="transition hover:text-[var(--color-brand)]"
                        >
                          {product.name}
                        </Link>
                      </h2>
                      <p className="mt-3 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {product.shortDescription ??
                          copy.productDescriptionFallback}
                      </p>
                      <div className="mt-5 flex items-end justify-between gap-4">
                        <div>
                          <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                            {formatMoney(product.priceMinor, product.currency, culture)}
                          </p>
                          {product.compareAtPriceMinor ? (
                            <div className="mt-1">
                              <p className="text-sm text-[var(--color-text-muted)] line-through">
                                {formatMoney(
                                  product.compareAtPriceMinor,
                                  product.currency,
                                  culture,
                                )}
                              </p>
                              <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[var(--color-accent)]">
                                {copy.merchandisingPriceDrop}
                              </p>
                            </div>
                          ) : null}
                        </div>
                        <Link
                          href={localizeHref(`/catalog/${product.slug}`, culture)}
                          className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          {copy.viewDetails}
                        </Link>
                      </div>
                      <div className="mt-4 flex flex-wrap gap-3">
                        <Link
                          href={localizeHref("/", culture)}
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          {copy.productCardHomeCta}
                        </Link>
                        <Link
                          href={localizeHref("/account", culture)}
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          {copy.productCardAccountCta}
                        </Link>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            )}

            {totalPages > 1 && (
              <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                  {copy.paginationTitle}
                </p>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.paginationMessage, {
                    currentPage,
                    totalPages,
                  })}
                </p>
                <div className="mt-5 flex flex-wrap items-center gap-3">
                  <Link
                    aria-disabled={currentPage <= 1}
                    href={localizeHref(
                      buildCatalogHref(
                        activeCategorySlug,
                        1,
                        visibleQuery,
                        visibleSort,
                      ),
                      culture,
                    )}
                    className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
                  >
                    {copy.firstPageCta}
                  </Link>
                  <Link
                    aria-disabled={currentPage <= 1}
                    href={localizeHref(
                      buildCatalogHref(
                        activeCategorySlug,
                        Math.max(1, currentPage - 1),
                        visibleQuery,
                        visibleSort,
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
                      buildCatalogHref(
                        activeCategorySlug,
                        Math.min(totalPages, currentPage + 1),
                        visibleQuery,
                        visibleSort,
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
                      buildCatalogHref(
                        activeCategorySlug,
                        totalPages,
                        visibleQuery,
                        visibleSort,
                      ),
                      culture,
                    )}
                    className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
                  >
                    {copy.lastPageCta}
                  </Link>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </section>
  );
}
