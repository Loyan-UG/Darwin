import Link from "next/link";
import { AddToCartForm } from "@/components/cart/add-to-cart-form";
import { StatusBanner } from "@/components/feedback/status-banner";
import type {
  PublicCategorySummary,
  PublicProductDetail,
  PublicProductSummary,
} from "@/features/catalog/types";
import { buildCatalogProductPath } from "@/lib/entity-paths";
import { sanitizeHtmlFragment } from "@/lib/html-fragment";
import {
  buildLocalizedQueryHref,
  localizeHref,
} from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";
import { formatMoney } from "@/lib/formatting";

type ProductDetailPageProps = {
  culture: string;
  product: PublicProductDetail | null;
  primaryCategory: PublicCategorySummary | null;
  relatedProducts: PublicProductSummary[];
  status: string;
};

function getCopy(culture: string) {
  const english = culture.toLowerCase().startsWith("en");

  return {
    home: english ? "Home" : "Start",
    catalog: english ? "Shop" : "Shop",
    product: english ? "Product" : "Produkt",
    unavailableTitle: english ? "Product is temporarily unavailable." : "Produkt ist voruebergehend nicht verfuegbar.",
    unavailableMessage: english ? "Please return to the catalog and try again later." : "Bitte kehren Sie zum Katalog zurueck und versuchen Sie es spaeter erneut.",
    backToCatalog: english ? "Back to catalog" : "Zurueck zum Katalog",
    save: english ? "Save" : "Sparen",
    imageComingSoon: english ? "Image coming soon" : "Bild folgt",
    quantityAndCart: english ? "Add to cart" : "In den Warenkorb",
    openCart: english ? "Open cart" : "Warenkorb oeffnen",
    deliveryNote: english ? "Delivery options and shipping costs are confirmed during checkout." : "Lieferoptionen und Versandkosten werden im Checkout bestaetigt.",
    details: english ? "Product details" : "Produktdetails",
    related: english ? "Related products" : "Aehnliche Produkte",
    relatedDescription: english ? "A few products from the same shopping context." : "Eine kurze Auswahl aus dem gleichen Einkaufsumfeld.",
    viewProduct: english ? "View product" : "Produkt ansehen",
    included: english ? "Included" : "Inklusive",
    addOns: english ? "Options" : "Optionen",
    variants: english ? "Variant" : "Variante",
    noVariant: english ? "This product cannot be added to cart yet." : "Dieses Produkt kann aktuell noch nicht in den Warenkorb gelegt werden.",
  };
}

export function ProductDetailPage({
  culture,
  product,
  primaryCategory,
  relatedProducts,
  status,
}: ProductDetailPageProps) {
  const copy = getCopy(culture);

  if (!product) {
    return (
      <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
        <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
          <StatusBanner
            tone="warning"
            title={copy.unavailableTitle}
            message={copy.unavailableMessage}
          />
          <Link
            href={localizeHref("/catalog", culture)}
            className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
          >
            {copy.backToCatalog}
          </Link>
        </div>
      </section>
    );
  }

  const gallery = product.media
    .map((media) => ({ ...media, url: toWebApiUrl(media.url) }))
    .filter((media) => Boolean(media.url));
  const primaryImageUrl =
    gallery[0]?.url ?? toWebApiUrl(product.primaryImageUrl ?? "") ?? null;
  const primaryVariant = product.variants[0] ?? null;
  const addToCartFormId = primaryVariant ? `add-to-cart-${primaryVariant.id}` : undefined;
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
  const categoryHref = primaryCategory
    ? buildLocalizedQueryHref("/catalog", { category: primaryCategory.slug }, culture)
    : localizeHref("/catalog", culture);
  const sanitizedDescriptionHtml = sanitizeHtmlFragment(
    product.fullDescriptionHtml ?? "",
  );
  const related = relatedProducts.slice(0, 4);

  return (
    <section className="mx-auto flex w-full max-w-[1320px] flex-1 px-4 py-6 sm:px-6 sm:py-8 lg:px-8 lg:py-10">
      <div className="flex w-full flex-col gap-8">
        <nav
          aria-label="Breadcrumb"
          className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
        >
          <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.home}
          </Link>
          <span>/</span>
          <Link href={localizeHref("/catalog", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.catalog}
          </Link>
          {primaryCategory ? (
            <>
              <span>/</span>
              <Link href={categoryHref} className="transition hover:text-[var(--color-brand)]">
                {primaryCategory.name}
              </Link>
            </>
          ) : null}
          <span>/</span>
          <span className="font-medium text-[var(--color-text-primary)]">
            {product.name}
          </span>
        </nav>

        {status !== "ok" ? (
          <StatusBanner
            tone="warning"
            title={copy.unavailableTitle}
            message={copy.unavailableMessage}
          />
        ) : null}

        <div className="grid gap-8 lg:grid-cols-[1.05fr_0.95fr]">
          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-white/92 p-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)] sm:p-8">
            <div className="grid gap-4 sm:grid-cols-2">
              {gallery.length > 0 ? (
                gallery.slice(0, 4).map((media) => (
                  <div
                    key={media.id}
                    className="flex min-h-56 items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(246,255,233,0.95),rgba(255,255,255,1),rgba(255,244,214,0.9))] p-5"
                  >
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={media.url}
                      alt={media.alt || product.name}
                      className="max-h-44 w-auto object-contain"
                    />
                  </div>
                ))
              ) : (
                <div className="flex min-h-80 items-center justify-center rounded-[1rem] bg-[linear-gradient(145deg,rgba(246,255,233,0.95),rgba(255,255,255,1),rgba(255,244,214,0.9))] p-5 sm:col-span-2">
                  <span className="text-sm font-semibold uppercase tracking-[0.22em] text-[var(--color-text-muted)]">
                    {copy.imageComingSoon}
                  </span>
                </div>
              )}
            </div>
          </div>

          <div className="rounded-[1rem] border border-[rgba(53,92,38,0.12)] bg-white/92 px-6 py-8 shadow-[0_24px_80px_rgba(38,76,34,0.08)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.product}
            </p>
            <div className="mt-4 flex flex-wrap gap-3">
              {primaryCategory ? (
                <Link
                  href={categoryHref}
                  className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {primaryCategory.name}
                </Link>
              ) : null}
              {savingsPercent ? (
                <span className="rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-brand-contrast)]">
                  {copy.save} {savingsPercent}%
                </span>
              ) : null}
            </div>

            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {product.name}
            </h1>
            <p className="mt-5 text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
              {product.shortDescription ?? copy.deliveryNote}
            </p>
            <div className="mt-6 flex flex-wrap items-end gap-4">
              <p className="text-3xl font-semibold text-[var(--color-text-primary)]">
                {formatMoney(priceMinor, product.currency, culture)}
              </p>
              {product.compareAtPriceMinor ? (
                <p className="text-lg text-[var(--color-text-muted)] line-through">
                  {formatMoney(product.compareAtPriceMinor, product.currency, culture)}
                </p>
              ) : null}
            </div>

            {primaryVariant ? (
              <div className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                <p className="font-semibold text-[var(--color-text-primary)]">
                  {copy.variants}
                </p>
                <p>{primaryVariant.sku}</p>
              </div>
            ) : null}

            {(product.applicableAddOns ?? []).length > 0 ? (
              <div className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4">
                <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                  {copy.addOns}
                </p>
                <div className="mt-4 grid gap-4">
                  {(product.applicableAddOns ?? []).map((group) => (
                    <div key={group.id} className="rounded-2xl bg-white/80 px-4 py-4">
                      <p className="font-semibold text-[var(--color-text-primary)]">
                        {group.name}
                      </p>
                      <div className="mt-3 grid gap-2">
                        {group.options.flatMap((option) =>
                          option.values.map((value) => {
                            const selectionInputType =
                              group.selectionMode === "Single" ? "radio" : "checkbox";
                            const selectionInputName = `addOnOption:${option.id}`;

                            return (
                              <label
                                key={value.id}
                                className="flex cursor-pointer items-start justify-between gap-3 rounded-xl bg-[var(--color-surface-panel)] px-3 py-3 text-sm text-[var(--color-text-secondary)]"
                              >
                                <span className="flex items-start gap-3">
                                  <input
                                    form={addToCartFormId}
                                    type={selectionInputType}
                                    name={selectionInputName}
                                    value={value.id}
                                    className="mt-1 h-4 w-4 accent-[var(--color-brand)]"
                                  />
                                  <span>
                                    <span className="block font-semibold text-[var(--color-text-primary)]">
                                      {value.label}
                                    </span>
                                    {value.hint ? <span className="mt-1 block">{value.hint}</span> : null}
                                  </span>
                                </span>
                                <span className="font-semibold text-[var(--color-text-primary)]">
                                  {value.priceDeltaMinor === 0
                                    ? copy.included
                                    : `+${formatMoney(value.priceDeltaMinor, group.currency, culture)}`}
                                </span>
                              </label>
                            );
                          }),
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            ) : null}

            <div className="mt-8 flex flex-wrap gap-3">
              {primaryVariant ? (
                <AddToCartForm
                  culture={culture}
                  variantId={primaryVariant.id}
                  productName={product.name}
                  productHref={localizeHref(buildCatalogProductPath(product.slug), culture)}
                  productImageUrl={primaryImageUrl}
                  productImageAlt={gallery[0]?.alt ?? product.name}
                  productSku={primaryVariant.sku}
                  returnPath={localizeHref(buildCatalogProductPath(product.slug), culture)}
                  formId={addToCartFormId}
                />
              ) : (
                <StatusBanner
                  tone="warning"
                  title={copy.noVariant}
                  message={copy.unavailableMessage}
                />
              )}
              <Link
                href={localizeHref("/cart", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.openCart}
              </Link>
            </div>

            <p className="mt-6 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.deliveryNote}
            </p>
          </div>
        </div>

        {sanitizedDescriptionHtml ? (
          <article className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.details}
            </p>
            <div
                className="content-body mt-6 max-w-none"
              dangerouslySetInnerHTML={{ __html: sanitizedDescriptionHtml }}
            />
          </article>
        ) : null}

        {related.length > 0 ? (
          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-white/92 px-6 py-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)] sm:px-8">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                  {copy.related}
                </p>
                <p className="mt-2 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.relatedDescription}
                </p>
              </div>
              <Link
                href={primaryCategory ? categoryHref : localizeHref("/catalog", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.backToCatalog}
              </Link>
            </div>
            <div className="mt-6 grid gap-5 md:grid-cols-2 xl:grid-cols-4">
              {related.map((relatedProduct) => {
                const relatedImageUrl = toWebApiUrl(relatedProduct.primaryImageUrl ?? "");
                return (
                  <article
                    key={relatedProduct.id}
                    className="flex h-full flex-col rounded-[1rem] border border-[rgba(53,92,38,0.1)] bg-[linear-gradient(145deg,rgba(246,255,233,0.95),rgba(255,255,255,1),rgba(255,244,214,0.88))] p-4"
                  >
                    <div className="flex min-h-36 items-center justify-center rounded-[1rem] bg-white/80 p-4">
                      {relatedImageUrl ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img
                          src={relatedImageUrl}
                          alt={relatedProduct.name}
                          className="max-h-24 w-auto object-contain"
                        />
                      ) : (
                        <span className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-text-muted)]">
                          {copy.imageComingSoon}
                        </span>
                      )}
                    </div>
                    <div className="mt-4 flex flex-1 flex-col">
                      <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
                        <Link
                          href={localizeHref(buildCatalogProductPath(relatedProduct.slug), culture)}
                          className="transition hover:text-[var(--color-brand)]"
                        >
                          {relatedProduct.name}
                        </Link>
                      </h2>
                      <p className="mt-2 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {relatedProduct.shortDescription ?? copy.relatedDescription}
                      </p>
                      <div className="mt-4 flex items-end justify-between gap-3">
                        <p className="text-base font-semibold text-[var(--color-text-primary)]">
                          {formatMoney(relatedProduct.priceMinor, relatedProduct.currency, culture)}
                        </p>
                        <Link
                          href={localizeHref(buildCatalogProductPath(relatedProduct.slug), culture)}
                          className="inline-flex rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[var(--color-brand-strong)]"
                        >
                          {copy.viewProduct}
                        </Link>
                      </div>
                    </div>
                  </article>
                );
              })}
            </div>
          </section>
        ) : null}
      </div>
    </section>
  );
}
