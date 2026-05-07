import Link from "next/link";
import { AddToCartForm } from "@/components/cart/add-to-cart-form";
import { CatalogCampaignWindow } from "@/components/catalog/catalog-campaign-window";
import { CatalogContinuationRail } from "@/components/catalog/catalog-continuation-rail";
import { ProductContentCompositionWindow } from "@/components/catalog/product-content-composition-window";
import { ProductStorefrontSupportWindow } from "@/components/catalog/product-storefront-support-window";
import { StatusBanner } from "@/components/feedback/status-banner";
import { SurfaceSectionNav } from "@/components/layout/surface-section-nav";
import { getProductSavingsPercent } from "@/features/catalog/merchandising";
import {
  buildCatalogReviewTargetHref,
} from "@/features/review/review-window";
import {
  buildPreferredCatalogReviewWindowHref,
  getPendingCatalogReviewQueueState,
} from "@/features/review/review-workflow";
import type {
  PublicCategorySummary,
  PublicProductDetail,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  buildAppQueryPath,
  buildLocalizedQueryHref,
  localizeHref,
} from "@/lib/locale-routing";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { sanitizeHtmlFragment } from "@/lib/html-fragment";
import { toWebApiUrl } from "@/lib/webapi-url";
import {
  formatResource,
  getCatalogResource,
  resolveApiStatusLabel,
} from "@/localization";
import { formatMoney } from "@/lib/formatting";

type ProductDetailPageProps = {
  culture: string;
  product: PublicProductDetail | null;
  categories: PublicCategorySummary[];
  primaryCategory: PublicCategorySummary | null;
  reviewWindow?: {
    category?: string;
    visibleQuery?: string;
    visibleState?: "all" | "offers" | "base";
    visibleSort?: "featured" | "name-asc" | "price-asc" | "price-desc" | "savings-desc" | "offers-first" | "base-first";
    mediaState?: "all" | "with-image" | "missing-image";
    savingsBand?: "all" | "value" | "hero";
  };
  relatedProducts: PublicProductSummary[];
  reviewProducts: PublicProductSummary[];
  cmsPages: PublicPageSummary[];
  cartSummary: {
    status: string;
    itemCount: number;
    currency: string;
    grandTotalGrossMinor: number;
  } | null;
  status: string;
  relatedProductsStatus?: string;
  reviewProductsStatus?: string;
  cmsPagesStatus?: string;
};

export function ProductDetailPage({
  culture,
  product,
  categories,
  primaryCategory,
  reviewWindow,
  relatedProducts,
  reviewProducts,
  cmsPages,
  cartSummary,
  status,
  relatedProductsStatus,
  reviewProductsStatus,
  cmsPagesStatus,
}: ProductDetailPageProps) {
  const copy = getCatalogResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy) ?? status;
  const relatedProductsStatusLabel =
    resolveApiStatusLabel(reviewProductsStatus ?? relatedProductsStatus ?? "ok", copy) ??
    reviewProductsStatus ??
    relatedProductsStatus ??
    "ok";
  const cmsPagesStatusLabel =
    resolveApiStatusLabel(cmsPagesStatus ?? "ok", copy) ?? cmsPagesStatus ?? "ok";
  const productRouteSummaryMessage = formatResource(copy.productRouteSummaryMessage, {
    status: statusLabel,
    relatedProductsStatus: relatedProductsStatusLabel,
    cmsPagesStatus: cmsPagesStatusLabel,
    relatedCount: reviewProducts.length,
  });

  if (!product) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.productUnavailableTitle}
            message={formatResource(copy.productUnavailableMessage, { status: statusLabel })}
          />
          <div className="mt-6 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.productRouteSummaryTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {productRouteSummaryMessage}
            </p>
          </div>
          <div className="mt-8">
            <CatalogContinuationRail
              culture={culture}
              description={productRouteSummaryMessage}
              items={[
                {
                  id: "product-unavailable-catalog",
                  label: copy.productBreadcrumbCatalog,
                  title: copy.backToCatalog,
                  description: formatResource(copy.productUnavailableMessage, { status: statusLabel }),
                  href: "/catalog",
                  ctaLabel: copy.backToCatalog,
                },
              ]}
            />
          </div>
        </div>
      </section>
    );
  }

  const gallery = product.media.length > 0 ? product.media : [];
  const resolvedGallery = gallery
    .map((media) => ({
      ...media,
      url: toWebApiUrl(media.url),
    }))
    .filter((media) => Boolean(media.url));
  const primaryProductImageUrl =
    resolvedGallery[0]?.url ?? toWebApiUrl(product.primaryImageUrl ?? "") ?? null;
  const categoryHref = primaryCategory
    ? buildLocalizedQueryHref("/catalog", { category: primaryCategory.slug }, culture)
    : null;
  const categoryCatalogPath = primaryCategory
    ? buildAppQueryPath("/catalog", { category: primaryCategory.slug })
    : "/catalog";
  const primaryVariant = product.variants[0] ?? null;
  const addToCartFormId = primaryVariant ? `add-to-cart-${primaryVariant.id}` : undefined;
  const applicableAddOns = product.applicableAddOns ?? [];
  const addOnOptionCount = applicableAddOns.reduce(
    (count, group) => count + group.options.length,
    0,
  );
  const addOnValueCount = applicableAddOns.reduce(
    (count, group) =>
      count +
      group.options.reduce(
        (optionCount, option) => optionCount + option.values.length,
        0,
      ),
    0,
  );
  const priceMinor = primaryVariant?.basePriceNetMinor ?? product.priceMinor;
  const hasOffer =
    typeof product.compareAtPriceMinor === "number" &&
    product.compareAtPriceMinor > priceMinor;
  const savingsPercent = hasOffer
    ? Math.round(
        ((product.compareAtPriceMinor! - priceMinor) /
          product.compareAtPriceMinor!) *
          100,
      )
    : null;
  const relatedOfferProducts = relatedProducts.filter(
    (relatedProduct) =>
      typeof relatedProduct.compareAtPriceMinor === "number" &&
      relatedProduct.compareAtPriceMinor > relatedProduct.priceMinor,
  );
  const strongestRelatedOffer =
    relatedOfferProducts
      .map((relatedProduct) => ({
        product: relatedProduct,
        savingsPercent: Math.round(
          ((relatedProduct.compareAtPriceMinor! - relatedProduct.priceMinor) /
            relatedProduct.compareAtPriceMinor!) *
            100,
        ),
      }))
      .sort((left, right) => right.savingsPercent - left.savingsPercent)[0] ?? null;
  const digitalVariantCount = product.variants.filter(
    (variant) => variant.isDigital,
  ).length;
  const backorderVariantCount = product.variants.filter(
    (variant) => variant.backorderAllowed,
  ).length;
  const sanitizedDescriptionHtml = sanitizeHtmlFragment(
    product.fullDescriptionHtml ?? "",
  );
  const hasMetaTitle = Boolean(product.metaTitle?.trim());
  const hasMetaDescription = Boolean(product.metaDescription?.trim());
  const hasPrimaryCategory = Boolean(primaryCategory);
  const hasMediaCoverage = product.media.length > 0;
  const hasRelatedCoverage = relatedProducts.length > 0;
  const readinessSignals = [
    hasMetaTitle,
    hasMetaDescription,
    hasPrimaryCategory,
    hasMediaCoverage,
    hasRelatedCoverage,
  ].filter(Boolean).length;
  const readinessState =
    readinessSignals >= 5
      ? copy.productReadinessStateReady
      : readinessSignals >= 3
        ? copy.productReadinessStatePartial
        : copy.productReadinessStateAttention;
  const reviewCatalogPath = buildPreferredCatalogReviewWindowHref(
    hasOffer ? "offers" : "base",
    {
      category: reviewWindow?.category ?? primaryCategory?.slug,
      visibleQuery: reviewWindow?.visibleQuery,
      visibleState: reviewWindow?.visibleState,
      visibleSort: reviewWindow?.visibleSort,
      mediaState: reviewWindow?.mediaState,
      savingsBand: reviewWindow?.savingsBand,
    },
  );
  const reviewQueueState = getPendingCatalogReviewQueueState(reviewProducts, {
    currentSlug: product.slug,
  });
  const reviewQueue = reviewQueueState.queue;
  const nextReviewProduct = reviewQueueState.nextTarget;
  const reviewQueuePreview = reviewQueueState.previewTargets;
  const currentReviewIndex = reviewProducts.findIndex(
    (reviewProduct) => reviewProduct.slug === product.slug,
  );
  const currentReviewPosition = currentReviewIndex >= 0 ? currentReviewIndex + 1 : null;
  const previousReviewProduct =
    currentReviewIndex > 0 ? reviewProducts[currentReviewIndex - 1] : null;
  const nextReviewAdjacentProduct =
    currentReviewIndex >= 0 && currentReviewIndex < reviewProducts.length - 1
      ? reviewProducts[currentReviewIndex + 1]
      : null;
  const sectionLinks = [
    { href: "#product-overview", label: copy.productRouteSummaryTitle },
    { href: "#product-readiness", label: copy.productReadinessTitle },
    { href: "#product-add-ons", label: copy.productAddOnsTitle },
    { href: "#product-review", label: copy.productReviewQueueTitle },
    { href: "#product-composition", label: copy.productCrossSurfaceGridTitle },
    { href: "#product-related", label: copy.relatedProductsTitle },
  ];
  return (
    <section className="mx-auto flex w-full max-w-[1320px] flex-1 px-4 py-6 sm:px-6 sm:py-8 lg:px-8 lg:py-10">
      <div className="flex w-full flex-col gap-8">
      <nav
        aria-label={copy.productBreadcrumbLabel}
        className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
      >
        <Link
          href={localizeHref("/", culture)}
          className="rounded-full bg-white/80 px-3 py-1.5 transition hover:text-[var(--color-brand)]"
        >
          {copy.productBreadcrumbHome}
        </Link>
        <span>/</span>
        <Link
          href={localizeHref("/catalog", culture)}
          className="rounded-full bg-white/80 px-3 py-1.5 transition hover:text-[var(--color-brand)]"
        >
          {copy.productBreadcrumbCatalog}
        </Link>
        {primaryCategory ? (
          <>
            <span>/</span>
            <Link
              href={categoryHref!}
              className="rounded-full bg-white/80 px-3 py-1.5 transition hover:text-[var(--color-brand)]"
            >
              {primaryCategory.name}
            </Link>
          </>
        ) : null}
        <span>/</span>
        <span className="font-medium text-[var(--color-text-primary)]">
          {product.name}
        </span>
      </nav>

      <SurfaceSectionNav items={sectionLinks} />

      <div
        id="product-overview"
        className="scroll-mt-28 rounded-[1rem] border border-[rgba(53,92,38,0.12)] bg-[linear-gradient(135deg,#f6ffe9_0%,#ffffff_38%,#fff1d2_100%)] px-6 py-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)]"
      >
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.productRouteSummaryTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {productRouteSummaryMessage}
        </p>
      </div>

      <div className="grid w-full gap-8 lg:grid-cols-[1.05fr_0.95fr]">
        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-white/92 p-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)] sm:p-8">
          <div className="grid gap-4 sm:grid-cols-2">
            {resolvedGallery.length > 0 ? (
              resolvedGallery.map((media) => (
                <div
                  key={media.id}
                  className="flex min-h-52 items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(246,255,233,0.95),rgba(255,255,255,1),rgba(255,244,214,0.9))] p-5"
                >
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={media.url}
                    alt={media.alt || product.name}
                    className="max-h-40 w-auto object-contain"
                  />
                </div>
              ))
            ) : (
              <div className="flex min-h-72 items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(246,255,233,0.95),rgba(255,255,255,1),rgba(255,244,214,0.9))] p-5 sm:col-span-2">
                <span className="text-sm font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
                  {copy.noMedia}
                </span>
              </div>
            )}
          </div>
        </div>

        <div className="rounded-[1rem] border border-[rgba(53,92,38,0.12)] bg-white/92 px-6 py-8 shadow-[0_24px_80px_rgba(38,76,34,0.08)] sm:px-8">
          {status !== "ok" && (
            <div className="mb-6">
              <StatusBanner
                tone="warning"
                title={copy.productDataWarningsTitle}
                message={formatResource(copy.productDataWarningsMessage, { status: statusLabel })}
              />
            </div>
          )}
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            {copy.productEyebrow}
          </p>
          <div className="mt-4 flex flex-wrap gap-3">
            {primaryCategory ? (
              <Link
                href={categoryHref!}
                className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {primaryCategory.name}
              </Link>
            ) : null}
            {hasOffer && savingsPercent ? (
              <span className="rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-brand-contrast)]">
                {copy.savePrefix} {savingsPercent}%
              </span>
            ) : null}
            {digitalVariantCount > 0 ? (
              <span className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]">
                {copy.digitalReady}
              </span>
            ) : null}
            {backorderVariantCount > 0 ? (
              <span className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]">
                {copy.backorderAvailable}
              </span>
            ) : null}
          </div>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {product.name}
          </h1>
          <p className="mt-5 text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {product.shortDescription ?? copy.productDescriptionFallback}
          </p>
          <div className="mt-6 flex flex-wrap items-end gap-4">
            <p className="text-3xl font-semibold text-[var(--color-text-primary)]">
              {formatMoney(priceMinor, product.currency, culture)}
            </p>
            {product.compareAtPriceMinor ? (
              <div>
                <p className="text-lg text-[var(--color-text-muted)] line-through">
                  {formatMoney(product.compareAtPriceMinor, product.currency, culture)}
                </p>
                <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[var(--color-accent)]">
                  {copy.offerActive}
                </p>
              </div>
            ) : null}
          </div>

          <div
            id="product-readiness"
            className="mt-6 scroll-mt-28 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]"
          >
            <p className="font-semibold text-[var(--color-text-primary)]">
              {copy.merchandisingSnapshotTitle}
            </p>
            <p>
              {primaryCategory
                ? `${copy.activeCategoryEyebrow}: ${primaryCategory.name}.`
                : copy.primaryCategoryUnknown}
            </p>
            <p>
              {product.media.length > 0
                ? formatResource(copy.mediaCountMessage, {
                  count: product.media.length,
                })
                : copy.noMediaGalleryMessage}
            </p>
            <p>
              {strongestRelatedOffer
                ? formatResource(copy.relatedOfferSnapshotMessage, {
                    name: strongestRelatedOffer.product.name,
                    savingsPercent: strongestRelatedOffer.savingsPercent,
                  })
                : copy.relatedOfferSnapshotFallback}
            </p>
          </div>

          <div className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            <p className="font-semibold text-[var(--color-text-primary)]">
              {copy.productReadinessTitle}
            </p>
            <p className="mt-2">
              {formatResource(copy.productReadinessMessage, {
                status: readinessState,
              })}
            </p>
            <div className="mt-4 grid gap-3 md:grid-cols-2">
              <div className="rounded-2xl bg-[var(--color-surface-panel)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.productReadinessDiscoveryLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {readinessState}
                </p>
              </div>
              <div className="rounded-2xl bg-[var(--color-surface-panel)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.productReadinessMetadataLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.productReadinessMetadataValue, {
                    current: Number(hasMetaTitle) + Number(hasMetaDescription),
                    total: 2,
                  })}
                </p>
              </div>
              <div className="rounded-2xl bg-[var(--color-surface-panel)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.productReadinessMerchandisingLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.productReadinessMerchandisingValue, {
                    mediaCount: product.media.length,
                    variantCount: product.variants.length,
                  })}
                </p>
              </div>
              <div className="rounded-2xl bg-[var(--color-surface-panel)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.productReadinessFollowUpLabel}
                </p>
                <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.productReadinessFollowUpValue, {
                    primaryCategory: hasPrimaryCategory ? copy.yes : copy.no,
                    relatedCount: relatedProducts.length,
                  })}
                </p>
              </div>
            </div>
          </div>

          <div className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            <p className="font-semibold text-[var(--color-text-primary)]">
              {copy.productReviewWindowTitle}
            </p>
            <p className="mt-2">
              {formatResource(copy.productReviewWindowMessage, {
                status: readinessState,
              })}
            </p>
            <div className="mt-4 flex flex-wrap gap-3">
              <Link
                href={localizeHref(reviewCatalogPath, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {hasOffer
                  ? copy.productReviewWindowOffersCta
                  : copy.productReviewWindowBaseCta}
              </Link>
              <Link
                href={localizeHref(
                  buildAppQueryPath("/catalog", {
                    category: primaryCategory?.slug,
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
                    mediaState:
                      reviewWindow?.mediaState &&
                      reviewWindow.mediaState !== "all"
                        ? reviewWindow.mediaState
                        : undefined,
                    savingsBand:
                      reviewWindow?.savingsBand &&
                      reviewWindow.savingsBand !== "all"
                        ? reviewWindow.savingsBand
                        : undefined,
                  }),
                  culture,
                )}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.productReviewWindowBrowseAllCta}
              </Link>
            </div>
          </div>

          <div
            id="product-review"
            className="mt-6 scroll-mt-28 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]"
          >
            <p className="font-semibold text-[var(--color-text-primary)]">
              {copy.productNextReviewTargetTitle}
            </p>
            <p className="mt-2">
              {nextReviewProduct
                ? nextReviewProduct.missingImage
                  ? copy.productNextReviewTargetImageMessage
                  : nextReviewProduct.savingsAmount > 0
                    ? formatResource(copy.productNextReviewTargetOfferMessage, {
                        savingsPercent:
                          getProductSavingsPercent(nextReviewProduct.product) ?? 0,
                      })
                    : copy.productNextReviewTargetBaseMessage
                : copy.productNextReviewTargetFallback}
            </p>
            {nextReviewProduct ? (
              <div className="mt-4 rounded-2xl bg-[var(--color-surface-panel)] px-4 py-4">
                <p className="font-semibold text-[var(--color-text-primary)]">
                  {nextReviewProduct.product.name}
                </p>
                <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {nextReviewProduct.product.shortDescription ?? copy.productDescriptionFallback}
                </p>
                <div className="mt-4">
                  <Link
                      href={localizeHref(
                        buildCatalogReviewTargetHref(nextReviewProduct.product.slug, reviewWindow),
                        culture,
                      )}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.productNextReviewTargetCta}
                  </Link>
                </div>
              </div>
            ) : null}
          </div>

          <div className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            <p className="font-semibold text-[var(--color-text-primary)]">
              {copy.productReviewQueueTitle}
            </p>
            <p className="mt-2">
              {formatResource(copy.productReviewQueueMessage, {
                remainingCount: reviewQueue.length,
              })}
            </p>
            <div className="mt-4 grid gap-3">
              {reviewQueuePreview.length > 0 ? (
                reviewQueuePreview.map((target) => (
                  <Link
                    key={target.product.id}
                    href={localizeHref(
                      buildCatalogReviewTargetHref(target.product.slug, reviewWindow),
                      culture,
                    )}
                    className="rounded-2xl bg-[var(--color-surface-panel)] px-4 py-4 transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    <p className="font-semibold text-[var(--color-text-primary)]">
                      {target.product.name}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {target.missingImage
                        ? copy.productReviewQueueImageMessage
                        : target.savingsAmount > 0
                          ? formatResource(copy.productReviewQueueOfferMessage, {
                              savingsPercent:
                                getProductSavingsPercent(target.product) ?? 0,
                            })
                          : copy.productReviewQueueBaseMessage}
                    </p>
                  </Link>
                ))
              ) : (
                <div className="rounded-2xl border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.productReviewQueueFallback}
                </div>
              )}
            </div>
          </div>

          <div className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            <p className="font-semibold text-[var(--color-text-primary)]">
              {copy.productReviewNavigationTitle}
            </p>
            <p className="mt-2">
              {currentReviewPosition
                ? formatResource(copy.productReviewNavigationMessage, {
                    current: currentReviewPosition,
                    total: reviewProducts.length,
                  })
                : copy.productReviewNavigationFallback}
            </p>
            <div className="mt-4 grid gap-3 md:grid-cols-2">
              {previousReviewProduct ? (
                <Link
                  href={localizeHref(
                    buildCatalogReviewTargetHref(
                      previousReviewProduct.slug,
                      reviewWindow,
                    ),
                    culture,
                  )}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-4 transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.previous}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {previousReviewProduct.name}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {previousReviewProduct.shortDescription ??
                      copy.productReviewNavigationAdjacentFallback}
                  </p>
                </Link>
              ) : (
                <div className="rounded-2xl border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.productReviewNavigationPreviousEmpty}
                </div>
              )}

              {nextReviewAdjacentProduct ? (
                <Link
                  href={localizeHref(
                    buildCatalogReviewTargetHref(
                      nextReviewAdjacentProduct.slug,
                      reviewWindow,
                    ),
                    culture,
                  )}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-4 transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {copy.next}
                  </p>
                  <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                    {nextReviewAdjacentProduct.name}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {nextReviewAdjacentProduct.shortDescription ??
                      copy.productReviewNavigationAdjacentFallback}
                  </p>
                </Link>
              ) : (
                <div className="rounded-2xl border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.productReviewNavigationNextEmpty}
                </div>
              )}
            </div>
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-3">
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.productReferenceSlugLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {product.slug}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.productReferenceMediaLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.productReferenceMediaValue, {
                  count: product.media.length,
                })}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.productReferenceVariantsLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.productReferenceVariantsValue, {
                  count: product.variants.length,
                })}
              </p>
            </div>
          </div>

          <div className="mt-8 grid gap-4 rounded-[1rem] bg-[var(--color-surface-panel-strong)] p-5">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.variantSnapshotTitle}
            </p>
            {product.variants.length > 0 ? (
              product.variants.map((variant) => (
                <div
                  key={variant.id}
                  className="rounded-2xl bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]"
                >
                  <p className="font-semibold text-[var(--color-text-primary)]">
                    {copy.variantSkuPrefix} {variant.sku}
                  </p>
                  <p>{copy.basePriceLabel} {formatMoney(variant.basePriceNetMinor, variant.currency, culture)}</p>
                  <p>{copy.backorderAllowedLabel} {variant.backorderAllowed ? copy.yes : copy.no}</p>
                  <p>{copy.digitalLabel} {variant.isDigital ? copy.yes : copy.no}</p>
                </div>
              ))
            ) : (
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.noVariantsMessage}
              </p>
            )}
          </div>

          <div
            id="product-add-ons"
            className="mt-8 scroll-mt-28 rounded-[1rem] bg-[var(--color-surface-panel-strong)] p-5"
          >
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                  {copy.productAddOnsTitle}
                </p>
                <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {applicableAddOns.length > 0
                    ? formatResource(copy.productAddOnsMessage, {
                        groupCount: applicableAddOns.length,
                        optionCount: addOnOptionCount,
                        valueCount: addOnValueCount,
                      })
                    : copy.productAddOnsEmptyMessage}
                </p>
              </div>
            </div>

            {applicableAddOns.length > 0 ? (
              <div className="mt-5 grid gap-4">
                {applicableAddOns.map((group) => {
                  const selectionLabel =
                    group.selectionMode.toLowerCase() === "multiple"
                      ? copy.productAddOnsMultipleSelection
                      : copy.productAddOnsSingleSelection;
                  const selectionInputType =
                    group.selectionMode.toLowerCase() === "multiple"
                      ? "checkbox"
                      : "radio";
                  const selectionInputName =
                    selectionInputType === "radio"
                      ? `selectedAddOnValueIds:${group.id}`
                      : "selectedAddOnValueIds";

                  return (
                    <article
                      key={group.id}
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-4"
                    >
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <h2 className="text-base font-semibold text-[var(--color-text-primary)]">
                            {group.name}
                          </h2>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {formatResource(copy.productAddOnsRuleMessage, {
                              selectionMode: selectionLabel,
                              minSelections: group.minSelections,
                              maxSelections:
                                group.maxSelections ?? copy.productAddOnsNoMaximum,
                            })}
                          </p>
                        </div>
                      </div>

                      <div className="mt-4 grid gap-3">
                        {selectionInputType === "radio" && group.minSelections <= 0 ? (
                          <label className="flex cursor-pointer items-start gap-3 rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                            <input
                              form={addToCartFormId}
                              type="radio"
                              name={selectionInputName}
                              value=""
                              defaultChecked
                              className="mt-1 h-4 w-4 accent-[var(--color-brand)]"
                            />
                            <span>{copy.productAddOnsNoSelection}</span>
                          </label>
                        ) : null}
                        {group.options.map((option) => (
                          <div
                            key={option.id}
                            className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4"
                          >
                            <p className="font-semibold text-[var(--color-text-primary)]">
                              {option.label}
                            </p>
                            {option.values.length > 0 ? (
                              <div className="mt-3 grid gap-2">
                                {option.values.map((value) => (
                                  <div
                                    key={value.id}
                                    className="flex flex-wrap items-start justify-between gap-3 rounded-xl bg-white/80 px-3 py-3 text-sm leading-6 text-[var(--color-text-secondary)]"
                                  >
                                    <label className="flex cursor-pointer items-start gap-3">
                                      <input
                                        form={addToCartFormId}
                                        type={selectionInputType}
                                        name={selectionInputName}
                                        value={value.id}
                                        className="mt-1 h-4 w-4 accent-[var(--color-brand)]"
                                      />
                                      <div>
                                      <p className="font-semibold text-[var(--color-text-primary)]">
                                        {value.label}
                                      </p>
                                      {value.hint ? (
                                        <p className="mt-1">{value.hint}</p>
                                      ) : null}
                                      </div>
                                    </label>
                                    <p className="font-semibold text-[var(--color-text-primary)]">
                                      {value.priceDeltaMinor === 0
                                        ? copy.productAddOnsIncludedPrice
                                        : `+${formatMoney(
                                            value.priceDeltaMinor,
                                            group.currency,
                                            culture,
                                          )}`}
                                    </p>
                                  </div>
                                ))}
                              </div>
                            ) : (
                              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                                {copy.productAddOnsNoValuesMessage}
                              </p>
                            )}
                          </div>
                        ))}
                      </div>
                    </article>
                  );
                })}
              </div>
            ) : null}
          </div>

          <div className="mt-8 flex flex-wrap gap-3">
              {primaryVariant ? (
                <AddToCartForm
                  culture={culture}
                  variantId={primaryVariant.id}
                  productName={product.name}
                  productHref={localizeHref(buildCatalogProductPath(product.slug), culture)}
                  productImageUrl={primaryProductImageUrl}
                  productImageAlt={gallery[0]?.alt ?? product.name}
                  productSku={primaryVariant.sku}
                  returnPath={localizeHref(buildCatalogProductPath(product.slug), culture)}
                  formId={addToCartFormId}
                />
            ) : (
              <StatusBanner
                tone="warning"
                title={copy.cannotAddToCartTitle}
                message={copy.cannotAddToCartMessage}
              />
            )}
            <Link
              href={localizeHref("/cart", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.openCart}
            </Link>
          </div>

          <div className="mt-8">
            <CatalogContinuationRail
              culture={culture}
              title={copy.productCrossSurfaceGridTitle}
              description={copy.productCrossSurfaceMessage}
              catalogHref={categoryCatalogPath}
              catalogCtaLabel={
                primaryCategory
                  ? `${copy.moreFromPrefix} ${primaryCategory.name}`
                  : copy.backToCatalog
              }
            />
          </div>

          {sanitizedDescriptionHtml ? (
            <div
              className="cms-content mt-8 max-w-none"
              dangerouslySetInnerHTML={{ __html: sanitizedDescriptionHtml }}
            />
          ) : null}

          <div className="mt-8">
            <div className="flex flex-wrap gap-3">
              <Link
                href={localizeHref("/catalog", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.backToCatalog}
              </Link>
              {primaryCategory ? (
                <Link
                  href={categoryHref!}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.moreFromPrefix} {primaryCategory.name}
                </Link>
              ) : null}
            </div>
          </div>
        </div>
      </div>

      <div id="product-composition" className="scroll-mt-28">
        <CatalogCampaignWindow
          culture={culture}
          categories={categories}
          products={[product, ...relatedProducts]}
        />
      </div>

      <div className="scroll-mt-28">
        <ProductContentCompositionWindow
          culture={culture}
          product={product}
          primaryCategory={primaryCategory}
          reviewProducts={reviewProducts}
          relatedProducts={relatedProducts}
          cartSummary={cartSummary}
          reviewCatalogPath={reviewCatalogPath}
          reviewPrimaryLabel={
            hasOffer ? copy.productReviewWindowOffersCta : copy.productReviewWindowBaseCta
          }
          nextReviewProduct={nextReviewProduct?.product ?? null}
        />
      </div>

      <ProductStorefrontSupportWindow
        culture={culture}
        cmsPages={cmsPages}
        cmsPagesStatus={cmsPagesStatus ?? "unknown"}
        categories={categories}
        categoriesStatus={status === "ok" ? "ok" : status}
        products={[product, ...relatedProducts]}
        productsStatus={relatedProductsStatus ?? reviewProductsStatus ?? status}
        cartSummary={cartSummary}
      />

      {primaryCategory ? (
        <div
          id="product-related"
          className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-white/92 px-6 py-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)] sm:px-8"
        >
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.relatedProductsTitle}
              </p>
              <p className="mt-2 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.relatedProductsDescription}
              </p>
            </div>
            <Link
              href={categoryHref!}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.moreFromPrefix} {primaryCategory.name}
            </Link>
          </div>

          {relatedProductsStatus && relatedProductsStatus !== "ok" ? (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title={copy.relatedProductsDegradedTitle}
                message={formatResource(copy.relatedProductsDegradedMessage, {
                  status: resolveApiStatusLabel(relatedProductsStatus, copy) ?? relatedProductsStatus,
                })}
              />
            </div>
          ) : null}

          {relatedProducts.length > 0 ? (
            <div className="mt-6 grid gap-5 md:grid-cols-2 xl:grid-cols-4">
              {relatedProducts.map((relatedProduct) => {
                const relatedProductImageUrl = toWebApiUrl(
                  relatedProduct.primaryImageUrl ?? "",
                );
                return (
                <article
                  key={relatedProduct.id}
                  className="flex h-full flex-col rounded-[1rem] border border-[rgba(53,92,38,0.1)] bg-[linear-gradient(145deg,rgba(246,255,233,0.95),rgba(255,255,255,1),rgba(255,244,214,0.88))] p-4 shadow-[0_18px_40px_rgba(38,76,34,0.06)]"
                >
                  <div className="flex min-h-40 items-center justify-center rounded-[1rem] bg-white/80 p-4">
                    {relatedProductImageUrl ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img
                        src={relatedProductImageUrl}
                        alt={relatedProduct.name}
                        className="max-h-28 w-auto object-contain"
                      />
                    ) : (
                      <span className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-text-muted)]">
                        {copy.noMedia}
                      </span>
                    )}
                  </div>
                  <div className="mt-4 flex flex-1 flex-col">
                    <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                      <Link
                        href={localizeHref(
                          buildCatalogReviewTargetHref(relatedProduct.slug, reviewWindow),
                          culture,
                        )}
                        className="transition hover:text-[var(--color-brand)]"
                      >
                        {relatedProduct.name}
                      </Link>
                    </h2>
                    <p className="mt-2 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {relatedProduct.shortDescription ??
                        copy.productDescriptionFallback}
                    </p>
                    <div className="mt-4 flex items-end justify-between gap-3">
                      <p className="text-base font-semibold text-[var(--color-text-primary)]">
                        {formatMoney(
                          relatedProduct.priceMinor,
                          relatedProduct.currency,
                          culture,
                        )}
                      </p>
                      <Link
                        href={localizeHref(
                          buildCatalogReviewTargetHref(relatedProduct.slug, reviewWindow),
                          culture,
                        )}
                        className="inline-flex rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[var(--color-brand-strong)]"
                      >
                        {copy.openProductCta}
                      </Link>
                    </div>
                  </div>
                </article>
                );
              })}
            </div>
          ) : (
            <div className="mt-6 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] px-5 py-8 text-center">
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.relatedProductsEmptyMessage}
              </p>
              <div className="mt-6 text-left">
                <CatalogContinuationRail
                  culture={culture}
                  title={copy.relatedProductsTitle}
                  description={copy.productCrossSurfaceMessage}
                />
              </div>
            </div>
          )}
        </div>
      ) : null}

      <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
        <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.productOfferWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {hasOffer && savingsPercent
              ? formatResource(copy.productOfferWindowMessage, {
                  savingsPercent,
                })
              : copy.productOfferWindowFallback}
          </p>
          <div className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.productOfferWindowPriceLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatMoney(priceMinor, product.currency, culture)}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.productOfferWindowCompareAtLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {product.compareAtPriceMinor
                  ? formatMoney(product.compareAtPriceMinor, product.currency, culture)
                  : copy.productOfferWindowCompareAtFallback}
              </p>
            </div>
          </div>
        </section>

        <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.productBuyingGuideTitle}
          </p>
          <div className="mt-4 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.productBuyingGuideCategoryLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {primaryCategory?.name ?? copy.productBuyingGuideCategoryFallback}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.productBuyingGuideRelatedOfferLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.productBuyingGuideRelatedOfferValue, {
                  count: relatedOfferProducts.length,
                })}
              </p>
            </div>
            <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.productBuyingGuideVariantLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.productBuyingGuideVariantValue, {
                  count: product.variants.length,
                })}
              </p>
            </div>
          </div>
        </section>
      </div>

      <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] sm:px-8">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.productCartWindowTitle}
        </p>
        <p className="mt-2 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
          {cartSummary
            ? formatResource(copy.productCartWindowMessage, {
                itemCount: cartSummary.itemCount,
                total: formatMoney(
                  cartSummary.grandTotalGrossMinor,
                  cartSummary.currency,
                  culture,
                ),
              })
            : copy.productCartWindowFallback}
        </p>
        <div className="mt-5 flex flex-wrap gap-3">
          <Link
            href={localizeHref("/cart", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCartWindowCartCta}
          </Link>
          <Link
            href={localizeHref("/checkout", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCartWindowCheckoutCta}
          </Link>
        </div>
      </div>

      <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] sm:px-8">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.productCmsWindowTitle}
            </p>
            <p className="mt-2 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.productCmsWindowMessage, {
                cmsPagesStatus: cmsPagesStatusLabel,
                pageCount: cmsPages.length,
              })}
            </p>
          </div>
          <Link
            href={localizeHref("/cms", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.productCmsWindowCta}
          </Link>
        </div>

        {cmsPages.length > 0 ? (
          <div className="mt-6 grid gap-5 md:grid-cols-2 xl:grid-cols-3">
            {cmsPages.map((page) => (
              <Link
                key={page.id}
                href={localizeHref(buildCmsPagePath(page.slug), culture)}
                className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-4 transition hover:bg-[var(--color-surface-panel)]"
              >
                <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                  {page.title}
                </p>
                <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {page.metaDescription ?? copy.productCmsWindowFallbackDescription}
                </p>
              </Link>
            ))}
          </div>
        ) : (
          <p className="mt-6 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.productCmsWindowEmptyMessage, {
              status: cmsPagesStatusLabel,
            })}
          </p>
        )}
      </div>
      </div>
    </section>
  );
}
