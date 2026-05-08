import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type {
  CatalogMediaState,
  CatalogSavingsBand,
  CatalogVisibleState,
  CatalogVisibleSort,
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import { getCatalogSavingsPercent } from "@/features/catalog/discovery";
import { buildCatalogProductPath } from "@/lib/entity-paths";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";
import { formatResource, getCatalogResource } from "@/localization";

type CatalogPageProps = {
  culture: string;
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
  activeCategorySlug?: string;
  totalProducts: number;
  currentPage: number;
  pageSize: number;
  searchQuery?: string;
  visibleState?: CatalogVisibleState;
  visibleSort?: CatalogVisibleSort;
  mediaState?: CatalogMediaState;
  savingsBand?: CatalogSavingsBand;
  dataStatus?: {
    categories: string;
    products: string;
  };
};

function buildCatalogHref(
  categorySlug?: string,
  page = 1,
  searchQuery?: string,
  visibleState?: CatalogVisibleState,
  visibleSort?: CatalogVisibleSort,
  mediaState?: CatalogMediaState,
  savingsBand?: CatalogSavingsBand,
) {
  return buildAppQueryPath("/catalog", {
    category: categorySlug,
    page: page > 1 ? page : undefined,
    search: searchQuery,
    visibleState: visibleState && visibleState !== "all" ? visibleState : undefined,
    visibleSort: visibleSort && visibleSort !== "featured" ? visibleSort : undefined,
    mediaState: mediaState && mediaState !== "all" ? mediaState : undefined,
    savingsBand: savingsBand && savingsBand !== "all" ? savingsBand : undefined,
  });
}

function getCleanCatalogCopy(culture: string) {
  const english = culture.toLowerCase().startsWith("en");

  return {
    title: english ? "Shop groceries and everyday picks" : "Lebensmittel und Alltagsprodukte einkaufen",
    description: english
      ? "Search the catalog, choose a category, and open any product for details and cart actions."
      : "Suchen Sie im Katalog, waehlen Sie eine Kategorie und oeffnen Sie Produkte fuer Details und Warenkorb.",
    search: english ? "Search products" : "Produkte suchen",
    searchPlaceholder: english ? "Search by name or keyword" : "Nach Name oder Stichwort suchen",
    category: english ? "Category" : "Kategorie",
    allCategories: english ? "All categories" : "Alle Kategorien",
    sort: english ? "Sort" : "Sortierung",
    featured: english ? "Featured" : "Empfohlen",
    nameAsc: english ? "Name A-Z" : "Name A-Z",
    priceAsc: english ? "Lowest price" : "Niedrigster Preis",
    priceDesc: english ? "Highest price" : "Hoechster Preis",
    offersFirst: english ? "Offers first" : "Angebote zuerst",
    availability: english ? "Availability" : "Verfuegbarkeit",
    allProducts: english ? "All products" : "Alle Produkte",
    offersOnly: english ? "Offers only" : "Nur Angebote",
    apply: english ? "Apply" : "Anwenden",
    clear: english ? "Clear filters" : "Filter loeschen",
    view: english ? "View product" : "Produkt ansehen",
    noMedia: english ? "Image coming soon" : "Bild folgt",
    emptyTitle: english ? "No products found" : "Keine Produkte gefunden",
    emptyMessage: english
      ? "Try another search term or clear the selected filters."
      : "Versuchen Sie einen anderen Suchbegriff oder setzen Sie die Filter zurueck.",
    unavailableTitle: english ? "Products are temporarily unavailable." : "Produkte sind voruebergehend nicht verfuegbar.",
    unavailableMessage: english ? "Please try again later." : "Bitte versuchen Sie es spaeter erneut.",
    pageLabel: english ? "Page {currentPage} of {totalPages}" : "Seite {currentPage} von {totalPages}",
    previous: english ? "Previous" : "Zurueck",
    next: english ? "Next" : "Weiter",
    save: english ? "Save" : "Sparen",
  };
}

export function CatalogPage({
  culture,
  categories,
  products,
  activeCategorySlug,
  totalProducts,
  currentPage,
  pageSize,
  searchQuery,
  visibleState = "all",
  visibleSort = "featured",
  mediaState = "all",
  savingsBand = "all",
  dataStatus,
}: CatalogPageProps) {
  const resourceCopy = getCatalogResource(culture);
  const copy = getCleanCatalogCopy(culture);
  const totalPages = Math.max(1, Math.ceil(totalProducts / pageSize));
  const activeCategory =
    categories.find((category) => category.slug === activeCategorySlug) ?? null;
  const productStatus = dataStatus?.products ?? "ok";

  return (
    <section className="mx-auto flex w-full max-w-[1320px] flex-1 px-4 py-6 sm:px-6 sm:py-8 lg:px-8 lg:py-10">
      <div className="flex w-full flex-col gap-8">
        <div className="relative overflow-hidden rounded-[1rem] border border-[rgba(61,105,52,0.12)] bg-[linear-gradient(135deg,#f6ffe9_0%,#ffffff_42%,#fff1d2_100%)] px-6 py-8 shadow-[0_34px_120px_rgba(38,76,34,0.12)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--color-brand)]">
            {resourceCopy.heroEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.title}
          </h1>
          <p className="mt-4 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.description}
          </p>
        </div>

        {productStatus !== "ok" ? (
          <StatusBanner
            tone="warning"
            title={copy.unavailableTitle}
            message={copy.unavailableMessage}
          />
        ) : null}

        <form
          action={localizeHref("/catalog", culture)}
          className="rounded-[1rem] border border-[var(--color-border-soft)] bg-white/92 p-5 shadow-[0_22px_60px_rgba(38,76,34,0.08)]"
        >
          <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_220px_180px_170px_auto_auto] lg:items-end">
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.search}
              <input
                type="search"
                name="search"
                defaultValue={searchQuery ?? ""}
                placeholder={copy.searchPlaceholder}
                maxLength={80}
                autoComplete="off"
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
              />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.category}
              <select
                name="category"
                defaultValue={activeCategorySlug ?? ""}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
              >
                <option value="">{copy.allCategories}</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.slug}>
                    {category.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.sort}
              <select
                name="visibleSort"
                defaultValue={visibleSort}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
              >
                <option value="featured">{copy.featured}</option>
                <option value="name-asc">{copy.nameAsc}</option>
                <option value="price-asc">{copy.priceAsc}</option>
                <option value="price-desc">{copy.priceDesc}</option>
                <option value="offers-first">{copy.offersFirst}</option>
              </select>
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.availability}
              <select
                name="visibleState"
                defaultValue={visibleState}
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
              >
                <option value="all">{copy.allProducts}</option>
                <option value="offers">{copy.offersOnly}</option>
              </select>
            </label>
            <button
              type="submit"
              className="inline-flex justify-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.apply}
            </button>
            <Link
              href={localizeHref("/catalog", culture)}
              className="inline-flex justify-center rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.clear}
            </Link>
          </div>
          {activeCategory ? (
            <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              <span className="font-semibold text-[var(--color-text-primary)]">
                {activeCategory.name}
              </span>{" "}
              {activeCategory.description ?? ""}
            </p>
          ) : null}
        </form>

        {products.length === 0 ? (
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-12 text-center shadow-[var(--shadow-panel)]">
            <h2 className="font-[family-name:var(--font-display)] text-3xl text-[var(--color-text-primary)]">
              {copy.emptyTitle}
            </h2>
            <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)]">
              {copy.emptyMessage}
            </p>
            <Link
              href={localizeHref("/catalog", culture)}
              className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
            >
              {copy.clear}
            </Link>
          </div>
        ) : (
          <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
            {products.map((product) => {
              const imageUrl = toWebApiUrl(product.primaryImageUrl ?? "");
              const savingsPercent = getCatalogSavingsPercent(product);

              return (
                <article
                  key={product.id}
                  className="flex h-full flex-col overflow-hidden rounded-[1rem] border border-[rgba(53,92,38,0.1)] bg-white/92 p-5 shadow-[0_22px_60px_rgba(38,76,34,0.08)] transition duration-200 hover:-translate-y-1"
                >
                  <div className="flex min-h-44 items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(246,255,233,0.95),rgba(255,255,255,1),rgba(255,244,214,0.9))] p-5">
                    {imageUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img
                        src={imageUrl}
                        alt={product.name}
                        className="max-h-32 w-auto object-contain"
                      />
                    ) : (
                      <span className="text-sm font-semibold uppercase tracking-[0.2em] text-[var(--color-text-muted)]">
                        {copy.noMedia}
                      </span>
                    )}
                  </div>
                  <div className="mt-5 flex flex-1 flex-col">
                    {savingsPercent > 0 ? (
                      <span className="w-fit rounded-full bg-[var(--color-brand)] px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--color-brand-contrast)]">
                        {copy.save} {savingsPercent}%
                      </span>
                    ) : null}
                    <h2 className="mt-3 text-xl font-semibold text-[var(--color-text-primary)]">
                      <Link
                        href={localizeHref(buildCatalogProductPath(product.slug), culture)}
                        className="transition hover:text-[var(--color-brand)]"
                      >
                        {product.name}
                      </Link>
                    </h2>
                    <p className="mt-3 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {product.shortDescription ?? copy.description}
                    </p>
                    <div className="mt-5 flex items-end justify-between gap-4">
                      <div>
                        <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(product.priceMinor, product.currency, culture)}
                        </p>
                        {product.compareAtPriceMinor ? (
                          <p className="mt-1 text-sm text-[var(--color-text-muted)] line-through">
                            {formatMoney(product.compareAtPriceMinor, product.currency, culture)}
                          </p>
                        ) : null}
                      </div>
                      <Link
                        href={localizeHref(buildCatalogProductPath(product.slug), culture)}
                        className="rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[var(--color-brand-strong)]"
                      >
                        {copy.view}
                      </Link>
                    </div>
                  </div>
                </article>
              );
            })}
          </div>
        )}

        {totalPages > 1 ? (
          <div className="flex flex-wrap items-center justify-center gap-3 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <Link
              aria-disabled={currentPage <= 1}
              href={localizeHref(
                buildCatalogHref(
                  activeCategorySlug,
                  Math.max(1, currentPage - 1),
                  searchQuery,
                  visibleState,
                  visibleSort,
                  mediaState,
                  savingsBand,
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
                  searchQuery,
                  visibleState,
                  visibleSort,
                  mediaState,
                  savingsBand,
                ),
                culture,
              )}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.next}
            </Link>
          </div>
        ) : null}
      </div>
    </section>
  );
}
