import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCategorySummary, PublicProductSummary } from "@/features/catalog/types";
import { formatResource, getCatalogResource } from "@/localization";
import { formatMoney } from "@/lib/formatting";

type CatalogPageProps = {
  culture: string;
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
  activeCategorySlug?: string;
  totalProducts: number;
  currentPage: number;
  pageSize: number;
  dataStatus?: {
    categories: string;
    products: string;
  };
};

function buildCatalogHref(categorySlug?: string, page = 1) {
  const params = new URLSearchParams();
  if (categorySlug) {
    params.set("category", categorySlug);
  }
  if (page > 1) {
    params.set("page", String(page));
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
  dataStatus,
}: CatalogPageProps) {
  const copy = getCatalogResource(culture);
  const hasProducts = products.length > 0;
  const totalPages = Math.max(1, Math.ceil(totalProducts / pageSize));
  const activeCategory =
    categories.find((category) => category.slug === activeCategorySlug) ?? null;
  const offerProducts = products.filter(
    (product) =>
      typeof product.compareAtPriceMinor === "number" &&
      product.compareAtPriceMinor > product.priceMinor,
  );

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
                  label: copy.currentResultsLabel,
                  value: String(totalProducts),
                  note: copy.currentResultsNote,
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

        <div className="grid gap-8 lg:grid-cols-[280px_minmax(0,1fr)]">
          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-5 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.categoriesTitle}
            </p>
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
                  href="/catalog"
                  className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-white/70"
                >
                  {copy.clearCategory}
                </Link>
              </div>
            ) : null}
            <div className="mt-5 flex flex-col gap-2">
              <Link
                href={buildCatalogHref(undefined)}
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
                  href={buildCatalogHref(category.slug)}
                  className={
                    activeCategorySlug === category.slug
                      ? "rounded-2xl bg-[var(--color-brand)] px-4 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
                      : "rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-secondary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  }
                >
                  <span className="block text-[var(--color-text-primary)]">{category.name}</span>
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
                        <Link href={`/catalog/${product.slug}`} className="transition hover:text-[var(--color-brand)]">
                          {product.name}
                        </Link>
                      </h2>
                      <p className="mt-3 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {product.shortDescription ?? copy.productDescriptionFallback}
                      </p>
                      <div className="mt-5 flex items-end justify-between gap-4">
                        <div>
                          <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                            {formatMoney(product.priceMinor, product.currency, culture)}
                          </p>
                          {product.compareAtPriceMinor ? (
                            <div className="mt-1">
                              <p className="text-sm text-[var(--color-text-muted)] line-through">
                                {formatMoney(product.compareAtPriceMinor, product.currency, culture)}
                              </p>
                              <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[var(--color-accent)]">
                                {copy.merchandisingPriceDrop}
                              </p>
                            </div>
                          ) : null}
                        </div>
                        <Link
                          href={`/catalog/${product.slug}`}
                          className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          {copy.viewDetails}
                        </Link>
                      </div>
                    </div>
                  </article>
                ))}
              </div>
            )}

            {totalPages > 1 && (
              <div className="flex flex-wrap items-center gap-3">
                <Link
                  aria-disabled={currentPage <= 1}
                  href={buildCatalogHref(activeCategorySlug, Math.max(1, currentPage - 1))}
                  className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
                >
                  {copy.previous}
                </Link>
                <p className="text-sm text-[var(--color-text-secondary)]">
                  {formatResource(copy.pageLabel, { currentPage, totalPages })}
                </p>
                <Link
                  aria-disabled={currentPage >= totalPages}
                  href={buildCatalogHref(activeCategorySlug, Math.min(totalPages, currentPage + 1))}
                  className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
                >
                  {copy.next}
                </Link>
              </div>
            )}
          </div>
        </div>
      </div>
    </section>
  );
}
