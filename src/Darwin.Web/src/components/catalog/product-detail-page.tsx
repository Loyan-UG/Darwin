import Link from "next/link";
import { AddToCartForm } from "@/components/cart/add-to-cart-form";
import { CatalogContinuationRail } from "@/components/catalog/catalog-continuation-rail";
import { StatusBanner } from "@/components/feedback/status-banner";
import type {
  PublicCategorySummary,
  PublicProductDetail,
  PublicProductSummary,
} from "@/features/catalog/types";
import {
  buildAppQueryPath,
  buildLocalizedQueryHref,
  localizeHref,
} from "@/lib/locale-routing";
import { sanitizeHtmlFragment } from "@/lib/html-fragment";
import { toWebApiUrl } from "@/lib/webapi-url";
import { formatResource, getCatalogResource } from "@/localization";
import { formatMoney } from "@/lib/formatting";

type ProductDetailPageProps = {
  culture: string;
  product: PublicProductDetail | null;
  primaryCategory: PublicCategorySummary | null;
  relatedProducts: PublicProductSummary[];
  status: string;
  relatedProductsStatus?: string;
};

export function ProductDetailPage({
  culture,
  product,
  primaryCategory,
  relatedProducts,
  status,
  relatedProductsStatus,
}: ProductDetailPageProps) {
  const copy = getCatalogResource(culture);

  if (!product) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.productUnavailableTitle}
            message={formatResource(copy.productUnavailableMessage, { status })}
          />
          <div className="mt-8">
            <CatalogContinuationRail culture={culture} />
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
  const digitalVariantCount = product.variants.filter(
    (variant) => variant.isDigital,
  ).length;
  const backorderVariantCount = product.variants.filter(
    (variant) => variant.backorderAllowed,
  ).length;
  const sanitizedDescriptionHtml = sanitizeHtmlFragment(
    product.fullDescriptionHtml ?? "",
  );
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
      <nav
        aria-label={copy.productBreadcrumbLabel}
        className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
      >
        <Link
          href={localizeHref("/", culture)}
          className="transition hover:text-[var(--color-brand)]"
        >
          {copy.productBreadcrumbHome}
        </Link>
        <span>/</span>
        <Link
          href={localizeHref("/catalog", culture)}
          className="transition hover:text-[var(--color-brand)]"
        >
          {copy.productBreadcrumbCatalog}
        </Link>
        {primaryCategory ? (
          <>
            <span>/</span>
            <Link
              href={categoryHref!}
              className="transition hover:text-[var(--color-brand)]"
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

      <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.productRouteSummaryTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {formatResource(copy.productRouteSummaryMessage, {
            status,
            relatedProductsStatus: relatedProductsStatus ?? "ok",
            relatedCount: relatedProducts.length,
          })}
        </p>
      </div>

      <div className="grid w-full gap-8 lg:grid-cols-[1.05fr_0.95fr]">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)] sm:p-8">
          <div className="grid gap-4 sm:grid-cols-2">
            {resolvedGallery.length > 0 ? (
              resolvedGallery.map((media) => (
                <div
                  key={media.id}
                  className="flex min-h-52 items-center justify-center rounded-[1.5rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-5"
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
              <div className="flex min-h-72 items-center justify-center rounded-[1.5rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-5 sm:col-span-2">
                <span className="text-sm font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
                  {copy.noMedia}
                </span>
              </div>
            )}
          </div>
        </div>

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          {status !== "ok" && (
            <div className="mb-6">
              <StatusBanner
                tone="warning"
                title={copy.productDataWarningsTitle}
                message={formatResource(copy.productDataWarningsMessage, { status })}
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

          <div className="mt-6 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
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
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-3">
            <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.productReferenceSlugLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {product.slug}
              </p>
            </div>
            <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {copy.productReferenceMediaLabel}
              </p>
              <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                {formatResource(copy.productReferenceMediaValue, {
                  count: product.media.length,
                })}
              </p>
            </div>
            <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
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

          <div className="mt-8 grid gap-4 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] p-5">
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

          <div className="mt-8 flex flex-wrap gap-3">
              {primaryVariant ? (
                <AddToCartForm
                  culture={culture}
                  variantId={primaryVariant.id}
                  productName={product.name}
                  productHref={localizeHref(`/catalog/${product.slug}`, culture)}
                  productImageUrl={primaryProductImageUrl}
                  productImageAlt={gallery[0]?.alt ?? product.name}
                  productSku={primaryVariant.sku}
                  returnPath={localizeHref(`/catalog/${product.slug}`, culture)}
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

      {primaryCategory ? (
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)] sm:px-8">
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
                  status: relatedProductsStatus,
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
                  className="flex h-full flex-col rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] p-4"
                >
                  <div className="flex min-h-40 items-center justify-center rounded-[1.25rem] bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-4">
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
                        href={localizeHref(`/catalog/${relatedProduct.slug}`, culture)}
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
                        href={localizeHref(`/catalog/${relatedProduct.slug}`, culture)}
                        className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
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
            <div className="mt-6 rounded-[1.5rem] border border-dashed border-[var(--color-border-strong)] px-5 py-8 text-center">
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
      </div>
    </section>
  );
}
