import Link from "next/link";
import { AddToCartForm } from "@/components/cart/add-to-cart-form";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicProductDetail } from "@/features/catalog/types";
import { formatMoney } from "@/lib/formatting";

type ProductDetailPageProps = {
  product: PublicProductDetail | null;
  status: string;
};

export function ProductDetailPage({ product, status }: ProductDetailPageProps) {
  if (!product) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title="Product detail is unavailable."
            message={`The public product endpoint returned status "${status}". This is now visible in the storefront instead of failing silently.`}
          />
          <div className="mt-8">
            <Link
              href="/catalog"
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              Back to catalog
            </Link>
          </div>
        </div>
      </section>
    );
  }

  const gallery = product.media.length > 0 ? product.media : [];
  const primaryVariant = product.variants[0] ?? null;
  const priceMinor = primaryVariant?.basePriceNetMinor ?? product.priceMinor;

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
                  No gallery media
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
                title="Product data loaded with warnings."
                message={`The primary detail fetch reported status "${status}". The page still renders what it could resolve from the current API response.`}
              />
            </div>
          )}
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            Storefront product
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {product.name}
          </h1>
          <p className="mt-5 text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {product.shortDescription ?? "Published product detail delivered through the public catalog surface."}
          </p>
          <div className="mt-6 flex flex-wrap items-end gap-4">
            <p className="text-3xl font-semibold text-[var(--color-text-primary)]">
              {formatMoney(priceMinor, product.currency)}
            </p>
            {product.compareAtPriceMinor ? (
              <p className="text-lg text-[var(--color-text-muted)] line-through">
                {formatMoney(product.compareAtPriceMinor, product.currency)}
              </p>
            ) : null}
          </div>

          <div className="mt-8 grid gap-4 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] p-5">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              Variant snapshot
            </p>
            {product.variants.length > 0 ? (
              product.variants.map((variant) => (
                <div
                  key={variant.id}
                  className="rounded-2xl bg-[var(--color-surface-panel)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]"
                >
                  <p className="font-semibold text-[var(--color-text-primary)]">
                    SKU: {variant.sku}
                  </p>
                  <p>Base price: {formatMoney(variant.basePriceNetMinor, variant.currency)}</p>
                  <p>Backorder allowed: {variant.backorderAllowed ? "Yes" : "No"}</p>
                  <p>Digital: {variant.isDigital ? "Yes" : "No"}</p>
                </div>
              ))
            ) : (
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                No published variant snapshots were returned for this product.
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
                title="This product cannot be added to cart yet."
                message="No published variant snapshot is currently available for the storefront add-to-cart flow."
              />
            )}
            <Link
              href="/cart"
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              Open cart
            </Link>
          </div>

          {product.fullDescriptionHtml ? (
            <div
              className="cms-content mt-8 max-w-none"
              dangerouslySetInnerHTML={{ __html: product.fullDescriptionHtml }}
            />
          ) : null}

          <div className="mt-8">
            <Link
              href="/catalog"
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              Back to catalog
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
