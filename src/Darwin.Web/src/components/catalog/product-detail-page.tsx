import Link from "next/link";
import { AddToCartForm } from "@/components/cart/add-to-cart-form";
import { StatusBanner } from "@/components/feedback/status-banner";
import type {
  PublicCategorySummary,
  PublicProductDetail,
} from "@/features/catalog/types";
import { formatResource, getCatalogResource } from "@/localization";
import { formatMoney } from "@/lib/formatting";

type ProductDetailPageProps = {
  culture: string;
  product: PublicProductDetail | null;
  primaryCategory: PublicCategorySummary | null;
  status: string;
};

export function ProductDetailPage({
  culture,
  product,
  primaryCategory,
  status,
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
            <Link
              href="/catalog"
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.backToCatalog}
            </Link>
          </div>
        </div>
      </section>
    );
  }

  const gallery = product.media.length > 0 ? product.media : [];
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

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="grid w-full gap-8 lg:grid-cols-[1.05fr_0.95fr]">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)] sm:p-8">
          <div className="grid gap-4 sm:grid-cols-2">
            {gallery.length > 0 ? (
              gallery.map((media) => (
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
                href={`/catalog?category=${encodeURIComponent(primaryCategory.slug)}`}
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
                variantId={primaryVariant.id}
                productName={product.name}
                productHref={`/catalog/${product.slug}`}
                productImageUrl={gallery[0]?.url ?? product.primaryImageUrl ?? null}
                productImageAlt={gallery[0]?.alt ?? product.name}
                productSku={primaryVariant.sku}
                returnPath={`/catalog/${product.slug}`}
              />
            ) : (
              <StatusBanner
                tone="warning"
                title={copy.cannotAddToCartTitle}
                message={copy.cannotAddToCartMessage}
              />
            )}
            <Link
              href="/cart"
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.openCart}
            </Link>
          </div>

          {product.fullDescriptionHtml ? (
            <div
              className="cms-content mt-8 max-w-none"
              dangerouslySetInnerHTML={{ __html: product.fullDescriptionHtml }}
            />
          ) : null}

          <div className="mt-8">
            <div className="flex flex-wrap gap-3">
              <Link
                href="/catalog"
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.backToCatalog}
              </Link>
              {primaryCategory ? (
                <Link
                  href={`/catalog?category=${encodeURIComponent(primaryCategory.slug)}`}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.moreFromPrefix} {primaryCategory.name}
                </Link>
              ) : null}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
