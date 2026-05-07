import Link from "next/link";
import { CatalogCampaignWindow } from "@/components/catalog/catalog-campaign-window";
import { CatalogContentCompositionWindow } from "@/components/catalog/catalog-content-composition-window";
import { CatalogContinuationRail } from "@/components/catalog/catalog-continuation-rail";
import { StatusBanner } from "@/components/feedback/status-banner";
import type {
  CatalogMediaState,
  CatalogSavingsBand,
  CatalogVisibleState,
  CatalogVisibleSort,
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  getCatalogReviewTargets,
  getCatalogSavingsPercent,
  hasPrimaryImage,
} from "@/features/catalog/discovery";
import { buildCatalogReviewTargetHref } from "@/features/review/review-window";
import {
  buildPreferredCatalogReviewWindowHref,
  getCatalogReviewQueueState,
  getPreferredCatalogReviewState,
} from "@/features/review/review-workflow";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";
import {
  formatResource,
  getCatalogResource,
  resolveApiStatusLabel,
} from "@/localization";

type CatalogPageProps = {
  culture: string;
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
  cmsPages: PublicPageSummary[];
  cartSummary: {
    status: string;
    itemCount: number;
    currency: string;
    grandTotalGrossMinor: number;
  } | null;
  activeCategorySlug?: string;
  totalProducts: number;
  matchingProductsTotal: number;
  currentPage: number;
  pageSize: number;
  searchQuery?: string;
  visibleState?: CatalogVisibleState;
  visibleSort?: CatalogVisibleSort;
  mediaState?: CatalogMediaState;
  savingsBand?: CatalogSavingsBand;
  facetSummary: {
    totalCount: number;
    offerCount: number;
    baseCount: number;
    withImageCount: number;
    missingImageCount: number;
    valueOfferCount: number;
    heroOfferCount: number;
  };
  loadedProductsCount: number;
  dataStatus?: {
    categories: string;
    products: string;
    cmsPages: string;
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

export function CatalogPage({
  culture,
  categories,
  products,
  cmsPages,
  cartSummary,
  activeCategorySlug,
  totalProducts,
  matchingProductsTotal,
  currentPage,
  pageSize,
  searchQuery,
  visibleState = "all",
  visibleSort = "featured",
  mediaState = "all",
  savingsBand = "all",
  facetSummary,
  loadedProductsCount,
  dataStatus,
}: CatalogPageProps) {
  const copy = getCatalogResource(culture);
  const categoriesStatusLabel =
    resolveApiStatusLabel(dataStatus?.categories, copy) ??
    dataStatus?.categories ??
    "unknown";
  const productsStatusLabel =
    resolveApiStatusLabel(dataStatus?.products, copy) ??
    dataStatus?.products ??
    "unknown";
  const cmsPagesStatusLabel =
    resolveApiStatusLabel(dataStatus?.cmsPages, copy) ??
    dataStatus?.cmsPages ??
    "unknown";
  const hasProducts = products.length > 0;
  const totalPages = Math.max(1, Math.ceil(totalProducts / pageSize));
  const pageStart = totalProducts === 0 ? 0 : (currentPage - 1) * pageSize + 1;
  const pageEnd =
    totalProducts === 0
      ? 0
      : Math.min(totalProducts, pageStart + loadedProductsCount - 1);
  const activeCategory =
    categories.find((category) => category.slug === activeCategorySlug) ?? null;
  const visibleOfferProducts = products.filter(
    (product) =>
      typeof product.compareAtPriceMinor === "number" &&
      product.compareAtPriceMinor > product.priceMinor,
  );
  const featuredOfferProduct = visibleOfferProducts
    .map((product) => ({
      product,
      savingsPercent: getCatalogSavingsPercent(product),
    }))
    .sort((left, right) => right.savingsPercent - left.savingsPercent)[0] ?? null;
  const hasSearchQuery = Boolean(searchQuery);
  const hasVisibleLens =
    visibleState !== "all" ||
    visibleSort !== "featured" ||
    mediaState !== "all" ||
    savingsBand !== "all";
  const hasFilteredWindow = hasSearchQuery || hasVisibleLens;
  const hasActiveCategory = Boolean(activeCategory);
  const hasCmsFollowUp = cmsPages.length > 0;
  const hasOfferCoverage = facetSummary.offerCount > 0;
  const hasBaseCoverage = facetSummary.baseCount > 0;
  const missingImageCount = facetSummary.missingImageCount;
  const compareAtMissingCount = facetSummary.baseCount;
  const readinessSignals = [
    hasActiveCategory,
    hasCmsFollowUp,
    hasOfferCoverage,
    hasBaseCoverage,
  ].filter(Boolean).length;
  const assortmentReadiness =
    readinessSignals >= 4
      ? copy.catalogReadinessStateReady
      : readinessSignals >= 2
        ? copy.catalogReadinessStatePartial
        : copy.catalogReadinessStateAttention;
  const reviewTargets = getCatalogReviewQueueState(
    getCatalogReviewTargets(products),
  ).previewTargets.map((target) => ({
    product: target.product,
    reason: target.missingImage
      ? copy.catalogReviewTargetImageMessage
      : target.savingsAmount > 0
        ? formatResource(copy.catalogReviewTargetOfferMessage, {
            savingsPercent: getCatalogSavingsPercent(target.product),
          })
        : copy.catalogReviewTargetBaseMessage,
  }));
  const visibleBaseCount = facetSummary.baseCount;
  const preferredCatalogReviewState = getPreferredCatalogReviewState(
    facetSummary.offerCount,
    visibleBaseCount,
  );
  const catalogReviewPrimaryHref = buildPreferredCatalogReviewWindowHref(
    preferredCatalogReviewState,
    {
      category: activeCategorySlug,
      visibleQuery: searchQuery,
      visibleState,
      visibleSort,
      mediaState,
      savingsBand,
    },
  );
  const catalogReviewPrimaryLabel =
    preferredCatalogReviewState === "offers"
      ? copy.catalogReviewOffersCta
      : copy.catalogReviewBaseCta;
  const catalogRouteSummaryMessage = formatResource(copy.catalogRouteSummaryMessage, {
    categoriesStatus: categoriesStatusLabel,
    productsStatus: productsStatusLabel,
    cmsPagesStatus: cmsPagesStatusLabel,
    visibleCount: products.length,
    totalProducts: matchingProductsTotal,
    currentPage,
    totalPages,
  });
  const catalogNoResultsRailItems = [
    {
      id: "catalog-no-results-reset",
      label: copy.visibleToolsTitle,
      title: copy.resetVisibleToolsCta,
      description: hasFilteredWindow
        ? formatResource(copy.visibleLensActiveMessage, {
            count: products.length,
            loadedCount: totalProducts,
            serverTotal: matchingProductsTotal,
          })
        : copy.noResultsDescription,
      href: buildCatalogHref(
        activeCategorySlug,
        1,
        undefined,
        "all",
        "featured",
        "all",
        "all",
      ),
      ctaLabel: copy.resetVisibleToolsCta,
    },
    {
      id: "catalog-no-results-scope",
      label: hasActiveCategory ? copy.activeCategoryEyebrow : copy.categoriesTitle,
      title: hasActiveCategory ? activeCategory!.name : copy.allProducts,
      description: hasActiveCategory
        ? activeCategory?.description ?? copy.categoryFallbackDescription
        : catalogRouteSummaryMessage,
      href: buildCatalogHref(activeCategorySlug, 1, undefined, "all", "featured", "all", "all"),
      ctaLabel: hasActiveCategory ? copy.backToCatalog : copy.allProducts,
    },
  ];
  const sectionLinks = [
    { id: "catalog-overview", label: copy.resultSummaryTitle },
    { id: "catalog-composition", label: copy.catalogCompositionJourneyTitle },
    { id: "catalog-offer-window", label: copy.offerWindowTitle },
    { id: "catalog-filters", label: copy.categoriesTitle },
    { id: "catalog-results", label: copy.productEyebrow },
    ...(totalPages > 1
      ? [{ id: "catalog-pagination", label: copy.paginationTitle }]
      : []),
  ];

  return (
    <section className="mx-auto flex w-full max-w-[1320px] flex-1 px-4 py-6 sm:px-6 sm:py-8 lg:px-8 lg:py-10">
      <div className="flex w-full flex-col gap-8">
        <div className="relative overflow-hidden rounded-[1rem] border border-[rgba(61,105,52,0.12)] bg-[linear-gradient(135deg,#f6ffe9_0%,#ffffff_38%,#fff1d2_100%)] px-6 py-8 shadow-[0_34px_120px_rgba(38,76,34,0.12)] sm:px-8 sm:py-10">
          <div
            aria-hidden="true"
            className="absolute -right-12 top-0 h-48 w-48 rounded-full bg-[rgba(76,175,80,0.14)] blur-3xl"
          />
          <div
            aria-hidden="true"
            className="absolute bottom-0 left-0 h-40 w-40 rounded-full bg-[rgba(255,152,0,0.14)] blur-3xl"
          />
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-[var(--color-brand)]">
            {copy.heroEyebrow}
          </p>
          <div className="relative mt-4 flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <h1 className="font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {copy.heroTitle}
              </h1>
              <p className="mt-4 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {copy.heroDescription}
              </p>
              {activeCategory ? (
                <div className="mt-5 rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-sm">
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
                  value: String(hasVisibleLens ? totalProducts : matchingProductsTotal),
                  note: hasVisibleLens
                    ? copy.visibleResultsNote
                    : copy.currentResultsNote,
                },
                {
                  label: copy.visibleOffersLabel,
                  value: String(facetSummary.offerCount),
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
                  className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-sm"
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
              categoriesStatus: categoriesStatusLabel,
              productsStatus: productsStatusLabel,
            })}
          />
        )}

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-white/86 px-6 py-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)] backdrop-blur">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.catalogRouteSummaryTitle}
          </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {catalogRouteSummaryMessage}
            </p>
          </div>

        {hasFilteredWindow && (
          <StatusBanner
            title={copy.visibleLensActiveTitle}
            message={formatResource(copy.visibleLensActiveMessage, {
              count: products.length,
              loadedCount: loadedProductsCount,
              serverTotal: totalProducts,
            })}
          />
        )}

        <section className="sticky top-4 z-10 rounded-[1rem] border border-[rgba(53,92,38,0.12)] bg-[rgba(255,255,255,0.86)] px-6 py-5 shadow-[0_20px_50px_rgba(38,76,34,0.1)] backdrop-blur">
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
          id="catalog-overview"
          className="scroll-mt-28 grid gap-5 lg:grid-cols-[minmax(0,1.35fr)_minmax(0,0.65fr)]"
        >
          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.resultSummaryTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {hasFilteredWindow
                ? formatResource(copy.resultSummaryFilteredMessage, {
                    visibleCount: products.length,
                    loadedCount: totalProducts,
                    totalProducts: matchingProductsTotal,
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
          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.resultSetTitle}
            </p>
            <div className="mt-4 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.resultSetLoadedLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.resultSetLoadedValue, {
                    count: loadedProductsCount,
                  })}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.resultSetVisibleLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.resultSetVisibleValue, {
                    count: totalProducts,
                  })}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.resultSetTotalLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.resultSetTotalValue, {
                    count: matchingProductsTotal,
                  })}
                </p>
              </div>
            </div>
          </div>
        </div>

        <div id="catalog-composition" className="scroll-mt-28">
          <CatalogContentCompositionWindow
            culture={culture}
            activeCategory={activeCategory}
            cmsPages={cmsPages}
            products={products}
            cartSummary={cartSummary}
            totalProducts={totalProducts}
            currentPage={currentPage}
            searchQuery={searchQuery}
            reviewHref={catalogReviewPrimaryHref}
            reviewLabel={catalogReviewPrimaryLabel}
          />
        </div>

        <div
          id="catalog-offer-window"
          className="scroll-mt-28 grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]"
        >
          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.offerWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {featuredOfferProduct
                ? formatResource(copy.offerWindowMessage, {
                    name: featuredOfferProduct.product.name,
                    savingsPercent: featuredOfferProduct.savingsPercent,
                  })
                : copy.offerWindowFallback}
            </p>
            {featuredOfferProduct ? (
              <div className="mt-5 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                  {featuredOfferProduct.product.name}
                </p>
                <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {featuredOfferProduct.product.shortDescription ??
                    copy.productDescriptionFallback}
                </p>
                <div className="mt-4 flex flex-wrap items-end gap-3">
                  <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                    {formatMoney(
                      featuredOfferProduct.product.priceMinor,
                      featuredOfferProduct.product.currency,
                      culture,
                    )}
                  </p>
                  {featuredOfferProduct.product.compareAtPriceMinor ? (
                    <p className="text-sm text-[var(--color-text-muted)] line-through">
                      {formatMoney(
                        featuredOfferProduct.product.compareAtPriceMinor,
                        featuredOfferProduct.product.currency,
                        culture,
                      )}
                    </p>
                  ) : null}
                </div>
                <div className="mt-4">
                  <Link
                    href={localizeHref(
                      buildCatalogProductPath(featuredOfferProduct.product.slug),
                      culture,
                    )}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                  >
                    {copy.offerWindowCta}
                  </Link>
                </div>
              </div>
            ) : null}
          </section>

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.buyingGuideTitle}
            </p>
            <div className="mt-4 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.buyingGuideCategoryLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {activeCategory?.name ?? copy.buyingGuideCategoryFallback}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.buyingGuideOfferLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.buyingGuideOfferValue, {
                    count: facetSummary.offerCount,
                  })}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.buyingGuideLensLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {hasVisibleLens
                    ? visibleState === "offers"
                      ? copy.buyingGuideLensOffers
                      : visibleState === "base"
                        ? copy.buyingGuideLensBase
                        : mediaState === "missing-image"
                          ? copy.buyingGuideLensMissingImage
                          : mediaState === "with-image"
                            ? copy.buyingGuideLensWithImage
                            : savingsBand === "hero"
                              ? copy.buyingGuideLensHeroOffers
                              : savingsBand === "value"
                                ? copy.buyingGuideLensValueOffers
                        : copy.buyingGuideLensFiltered
                    : copy.buyingGuideLensDefault}
                </p>
              </div>
            </div>
          </section>
        </div>

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.catalogReadinessTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.catalogReadinessMessage, {
              status: assortmentReadiness,
            })}
          </p>
          <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.catalogReadinessDiscoveryLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {assortmentReadiness}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.catalogReadinessOfferLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.catalogReadinessOfferValue, {
                  count: facetSummary.offerCount,
                })}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.catalogReadinessBaseLabel}
              </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.catalogReadinessBaseValue, {
                    count: visibleBaseCount,
                  })}
                </p>
              </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.catalogReadinessSupportLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.catalogReadinessSupportValue, {
                  categoryCount: hasActiveCategory ? 1 : categories.length,
                  cmsCount: cmsPages.length,
                })}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.catalogFacetImageLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.catalogFacetImageValue, {
                  withImageCount: facetSummary.withImageCount,
                  missingImageCount: facetSummary.missingImageCount,
                })}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.catalogFacetSavingsLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.catalogFacetSavingsValue, {
                  valueOfferCount: facetSummary.valueOfferCount,
                  heroOfferCount: facetSummary.heroOfferCount,
                })}
              </p>
            </div>
            </div>
          </div>

          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.catalogReviewTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.catalogReviewMessage, {
                offerCount: facetSummary.offerCount,
                baseCount: visibleBaseCount,
              })}
            </p>
            <div className="mt-5 flex flex-wrap gap-3">
              <Link
                href={localizeHref(catalogReviewPrimaryHref, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {catalogReviewPrimaryLabel}
              </Link>
              <Link
                href={localizeHref(
                  buildCatalogHref(
                    activeCategorySlug,
                    1,
                    searchQuery,
                    "all",
                    "featured",
                    mediaState,
                    savingsBand,
                  ),
                  culture,
                )}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.catalogReviewAllCta}
              </Link>
              <Link
                href={localizeHref(
                  buildCatalogHref(
                    activeCategorySlug,
                    1,
                    undefined,
                    "all",
                    "featured",
                    "all",
                    "all",
                  ),
                  culture,
                )}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.catalogReviewResetCta}
              </Link>
            </div>
            <div className="mt-4 grid gap-3 md:grid-cols-2">
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.catalogReviewImageCoverageLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.catalogReviewImageCoverageValue, {
                    count: missingImageCount,
                  })}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.catalogReviewOfferCoverageLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.catalogReviewOfferCoverageValue, {
                    count: compareAtMissingCount,
                  })}
                </p>
              </div>
            </div>
            <div className="mt-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.catalogReviewTargetsTitle}
              </p>
              <div className="mt-3 grid gap-3">
                {reviewTargets.length > 0 ? (
                  reviewTargets.map(({ product, reason }) => (
                    <Link
                      key={product.id}
                      href={localizeHref(
                        buildCatalogReviewTargetHref(product.slug, {
                          category: activeCategorySlug,
                          visibleQuery: searchQuery,
                          visibleState,
                          visibleSort,
                          mediaState,
                          savingsBand,
                        }),
                        culture,
                      )}
                      className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 transition hover:bg-[var(--color-surface-panel)]"
                    >
                      <p className="font-semibold text-[var(--color-text-primary)]">
                        {product.name}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {reason}
                      </p>
                    </Link>
                  ))
                ) : (
                  <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.catalogReviewTargetsFallback}
                  </div>
                )}
              </div>
            </div>
          </div>
  
          <CatalogCampaignWindow
            culture={culture}
          categories={categories}
          products={products}
        />

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.catalogCartWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {cartSummary
              ? formatResource(copy.catalogCartWindowMessage, {
                  itemCount: cartSummary.itemCount,
                  total: formatMoney(
                    cartSummary.grandTotalGrossMinor,
                    cartSummary.currency,
                    culture,
                  ),
                })
              : copy.catalogCartWindowFallback}
          </p>
          <div className="mt-5 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/cart", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.catalogCartWindowCartCta}
            </Link>
            <Link
              href={localizeHref("/checkout", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.catalogCartWindowCheckoutCta}
            </Link>
          </div>
        </div>

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.catalogCmsWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.catalogCmsWindowMessage, {
              cmsPagesStatus: cmsPagesStatusLabel,
              pageCount: cmsPages.length,
            })}
          </p>
          {cmsPages.length > 0 ? (
            <div className="mt-5 grid gap-4 lg:grid-cols-3">
              {cmsPages.map((page) => (
                <Link
                  key={page.id}
                  href={localizeHref(buildCmsPagePath(page.slug), culture)}
                  className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5 transition hover:bg-[var(--color-surface-panel)]"
                >
                  <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                    {page.title}
                  </p>
                  <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {page.metaDescription ?? copy.catalogCmsWindowFallbackDescription}
                  </p>
                </Link>
              ))}
            </div>
          ) : (
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.catalogCmsWindowEmptyMessage, {
                status: cmsPagesStatusLabel,
              })}
            </p>
          )}
          <div className="mt-5 flex flex-wrap gap-3">
            <Link
              href={localizeHref("/cms", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.catalogCmsWindowCta}
            </Link>
          </div>
        </div>

        <div className="grid gap-8 lg:grid-cols-[280px_minmax(0,1fr)]">
          <aside
            id="catalog-filters"
            className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-white/92 px-5 py-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)]"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.categoriesTitle}
            </p>

            <form
              action={localizeHref("/catalog", culture)}
              method="get"
              className="mt-5 rounded-[1rem] border border-[rgba(53,92,38,0.08)] bg-[linear-gradient(145deg,rgba(246,255,233,0.95),rgba(255,255,255,0.98))] px-4 py-4"
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
                  name="search"
                  defaultValue={searchQuery}
                  placeholder={copy.visibleSearchPlaceholder}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 text-sm font-normal outline-none"
                />
              </label>
              <label className="mt-4 flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.visibleStateLabel}
                <select
                  name="visibleState"
                  defaultValue={visibleState}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 text-sm font-normal outline-none"
                >
                  <option value="all">{copy.visibleStateAllOption}</option>
                  <option value="offers">{copy.visibleStateOffersOption}</option>
                  <option value="base">{copy.visibleStateBaseOption}</option>
                </select>
              </label>
              <label className="mt-4 flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.mediaStateLabel}
                <select
                  name="mediaState"
                  defaultValue={mediaState}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 text-sm font-normal outline-none"
                >
                  <option value="all">{copy.mediaStateAllOption}</option>
                  <option value="with-image">{copy.mediaStateWithImageOption}</option>
                  <option value="missing-image">{copy.mediaStateMissingImageOption}</option>
                </select>
              </label>
              <label className="mt-4 flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.savingsBandLabel}
                <select
                  name="savingsBand"
                  defaultValue={savingsBand}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 text-sm font-normal outline-none"
                >
                  <option value="all">{copy.savingsBandAllOption}</option>
                  <option value="value">{copy.savingsBandValueOption}</option>
                  <option value="hero">{copy.savingsBandHeroOption}</option>
                </select>
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
                  <option value="offers-first">{copy.sortOffersFirstOption}</option>
                  <option value="base-first">{copy.sortBaseFirstOption}</option>
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
                  href={localizeHref(
                    buildCatalogHref(
                      activeCategorySlug,
                      1,
                      searchQuery,
                      visibleState,
                      visibleSort,
                      mediaState,
                      "value",
                    ),
                    culture,
                  )}
                  className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm transition hover:bg-[var(--color-surface-panel)]"
                >
                  <p className="font-semibold text-[var(--color-text-primary)]">
                    {copy.catalogFacetValueOfferCta}
                  </p>
                  <p className="mt-2 text-[var(--color-text-secondary)]">
                    {formatResource(copy.catalogFacetValueOfferMessage, {
                      count: facetSummary.valueOfferCount,
                    })}
                  </p>
                </Link>
                <Link
                  href={localizeHref(
                    buildCatalogHref(
                      activeCategorySlug,
                      1,
                      undefined,
                      "all",
                      "featured",
                      "all",
                      "all",
                    ),
                    culture,
                  )}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-white/70"
                >
                  {copy.resetVisibleToolsCta}
                </Link>
              </div>
              <div className="mt-4 grid gap-3 sm:grid-cols-2">
                <Link
                  href={localizeHref(
                    buildCatalogHref(
                      activeCategorySlug,
                      1,
                      searchQuery,
                      visibleState,
                      visibleSort,
                      "with-image",
                      savingsBand,
                    ),
                    culture,
                  )}
                  className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm transition hover:bg-[var(--color-surface-panel)]"
                >
                  <p className="font-semibold text-[var(--color-text-primary)]">
                    {copy.catalogFacetWithImageCta}
                  </p>
                  <p className="mt-2 text-[var(--color-text-secondary)]">
                    {formatResource(copy.catalogFacetWithImageMessage, {
                      count: facetSummary.withImageCount,
                    })}
                  </p>
                </Link>
                <Link
                  href={localizeHref(
                    buildCatalogHref(
                      activeCategorySlug,
                      1,
                      searchQuery,
                      visibleState,
                      visibleSort,
                      "missing-image",
                      savingsBand,
                    ),
                    culture,
                  )}
                  className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm transition hover:bg-[var(--color-surface-panel)]"
                >
                  <p className="font-semibold text-[var(--color-text-primary)]">
                    {copy.catalogFacetMissingImageCta}
                  </p>
                  <p className="mt-2 text-[var(--color-text-secondary)]">
                    {formatResource(copy.catalogFacetMissingImageMessage, {
                      count: facetSummary.missingImageCount,
                    })}
                  </p>
                </Link>
                <Link
                  href={localizeHref(
                    buildCatalogHref(
                      activeCategorySlug,
                      1,
                      searchQuery,
                      visibleState,
                      visibleSort,
                      mediaState,
                      "hero",
                    ),
                    culture,
                  )}
                  className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm transition hover:bg-[var(--color-surface-panel)]"
                >
                  <p className="font-semibold text-[var(--color-text-primary)]">
                    {copy.catalogFacetHeroOfferCta}
                  </p>
                  <p className="mt-2 text-[var(--color-text-secondary)]">
                    {formatResource(copy.catalogFacetHeroOfferMessage, {
                      count: facetSummary.heroOfferCount,
                    })}
                  </p>
                </Link>
              </div>
            </form>

            {activeCategory ? (
              <div className="mt-4 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                <p className="font-semibold text-[var(--color-text-primary)]">
                  {copy.browsingCategoryPrefix} {activeCategory.name}
                </p>
                <p>
                  {activeCategory.description ??
                    copy.categoryFallbackDescription}
                </p>
                <Link
                  href={localizeHref(
                    buildCatalogHref(
                      undefined,
                      1,
                      searchQuery,
                      visibleState,
                      visibleSort,
                      mediaState,
                      savingsBand,
                    ),
                    culture,
                  )}
                  className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-white/70"
                >
                  {copy.clearCategory}
                </Link>
              </div>
            ) : null}
            <div className="mt-5 flex flex-col gap-2">
              <Link
                  href={localizeHref(
                    buildCatalogHref(
                      undefined,
                      1,
                      searchQuery,
                      visibleState,
                      visibleSort,
                      mediaState,
                      savingsBand,
                    ),
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
                    buildCatalogHref(
                      category.slug,
                      1,
                      searchQuery,
                      visibleState,
                      visibleSort,
                      mediaState,
                      savingsBand,
                    ),
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

          <div id="catalog-results" className="scroll-mt-28 flex flex-col gap-6">
            {!hasProducts ? (
              <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
                  {copy.noResultsEyebrow}
                </p>
                <h2 className="mt-4 font-[family-name:var(--font-display)] text-3xl text-[var(--color-text-primary)]">
                  {copy.noResultsTitle}
                </h2>
                <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)]">
                  {copy.noResultsDescription}
                </p>
                <div className="mt-6 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5 text-left">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {copy.catalogRouteSummaryTitle}
                  </p>
                  <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {catalogRouteSummaryMessage}
                  </p>
                </div>
                <div className="mt-8 text-left">
                  <CatalogContinuationRail
                    culture={culture}
                    title={copy.catalogNoResultsRailTitle}
                    description={copy.productCrossSurfaceMessage}
                    items={catalogNoResultsRailItems}
                  />
                </div>
              </div>
            ) : (
              <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
                {products.map((product) => (
                  (() => {
                    const productImageUrl = toWebApiUrl(product.primaryImageUrl ?? "");
                    const savingsPercent = getCatalogSavingsPercent(product);
                    return (
                  <article
                    key={product.id}
                    className="flex h-full flex-col overflow-hidden rounded-[1rem] border border-[rgba(53,92,38,0.1)] bg-white/92 p-5 shadow-[0_22px_60px_rgba(38,76,34,0.08)] transition duration-200 hover:-translate-y-1"
                  >
                    <div className="flex min-h-48 items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(246,255,233,0.95),rgba(255,255,255,1),rgba(255,244,214,0.9))] p-6">
                      {productImageUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img
                          src={productImageUrl}
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
                        {savingsPercent > 0 ? (
                          <span className="rounded-full bg-[var(--color-brand)] px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--color-brand-contrast)]">
                            {copy.savePrefix} {savingsPercent}%
                          </span>
                        ) : null}
                      </div>
                      <h2 className="mt-3 text-xl font-semibold text-[var(--color-text-primary)]">
                        <Link
                          href={localizeHref(
                            buildCatalogReviewTargetHref(product.slug, {
                              category: activeCategorySlug,
                              visibleQuery: searchQuery,
                              visibleState,
                              visibleSort,
                              mediaState,
                              savingsBand,
                            }),
                            culture,
                          )}
                          className="transition hover:text-[var(--color-brand)]"
                        >
                          {product.name}
                        </Link>
                      </h2>
                      <div className="mt-3 flex flex-wrap gap-2">
                        <span className="rounded-full bg-[rgba(47,125,50,0.08)] px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--color-text-primary)]">
                          {savingsPercent !== null
                            && savingsPercent > 0
                            ? copy.catalogCardOfferLabel
                            : copy.catalogCardBaseLabel}
                        </span>
                        {!hasPrimaryImage(product) ? (
                          <span className="rounded-full bg-[rgba(217,111,50,0.12)] px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--color-accent)]">
                            {copy.catalogCardMissingImageLabel}
                          </span>
                        ) : null}
                        {savingsPercent > 0 ? (
                          <span className="rounded-full bg-[rgba(89,130,70,0.12)] px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-[var(--color-brand)]">
                            {formatResource(copy.catalogCardSavingsLabel, {
                              savingsPercent,
                            })}
                          </span>
                        ) : null}
                      </div>
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
                          href={localizeHref(
                            buildCatalogReviewTargetHref(product.slug, {
                              category: activeCategorySlug,
                              visibleQuery: searchQuery,
                              visibleState,
                              visibleSort,
                              mediaState,
                              savingsBand,
                            }),
                            culture,
                          )}
                          className="rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[var(--color-brand-strong)]"
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
                    );
                  })()
                ))}
              </div>
            )}

            {totalPages > 1 && (
              <div
                id="catalog-pagination"
                className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
              >
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
                    {copy.firstPageCta}
                  </Link>
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
                  <Link
                    aria-disabled={currentPage >= totalPages}
                    href={localizeHref(
                      buildCatalogHref(
                        activeCategorySlug,
                        totalPages,
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

