import Link from "next/link";
import { buildPromotionLaneRouteMapItem } from "@/components/composition-window-promotion-lane";
import { sortProductsByOpportunity } from "@/features/catalog/merchandising";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { formatMoney } from "@/lib/formatting";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";

type CompositionCard = {
  label: string;
  title: string;
  description: string;
  href: string;
  ctaLabel: string;
  meta?: string | null;
};

type PublicAuthCompositionWindowProps = {
  culture: string;
  routeCard: CompositionCard;
  nextCard: CompositionCard;
  routeMapItems: CompositionCard[];
  cmsPages: PublicPageSummary[];
  categories: PublicCategorySummary[];
  products: PublicProductSummary[];
};

export function PublicAuthCompositionWindow({
  culture,
  routeCard,
  nextCard,
  routeMapItems,
  cmsPages,
  categories,
  products,
}: PublicAuthCompositionWindowProps) {
  const copy = getMemberResource(culture);
  const strongestProduct = sortProductsByOpportunity(products)[0] ?? null;
  const strongestCategory = categories[0] ?? null;
  const strongestPage = cmsPages[0] ?? null;
  const storefrontCard: CompositionCard = strongestProduct
    ? {
        label: copy.publicAuthCompositionJourneyStorefrontLabel,
        title: formatResource(copy.publicAuthCompositionJourneyStorefrontTitle, {
          product: strongestProduct.name,
        }),
        description: formatResource(
          copy.publicAuthCompositionJourneyStorefrontDescription,
          {
            price: formatMoney(
              strongestProduct.priceMinor,
              strongestProduct.currency,
              culture,
            ),
          },
        ),
        href: buildCatalogProductPath(strongestProduct.slug),
        ctaLabel: copy.publicAuthCompositionJourneyStorefrontCta,
      }
    : strongestCategory
      ? {
          label: copy.publicAuthCompositionJourneyStorefrontLabel,
          title: formatResource(
            copy.publicAuthCompositionJourneyStorefrontCategoryTitle,
            {
              category: strongestCategory.name,
            },
          ),
          description:
            strongestCategory.description ??
            copy.publicAuthCompositionJourneyStorefrontFallbackDescription,
          href: buildAppQueryPath("/catalog", { category: strongestCategory.slug }),
          ctaLabel: copy.publicAuthCompositionJourneyStorefrontCta,
        }
      : {
          label: copy.publicAuthCompositionJourneyStorefrontLabel,
          title: copy.publicAuthCompositionJourneyStorefrontFallbackTitle,
          description: copy.publicAuthCompositionJourneyStorefrontFallbackDescription,
          href: "/catalog",
          ctaLabel: copy.publicAuthCompositionJourneyStorefrontCta,
        };
  const promotionLaneRouteMapItem = buildPromotionLaneRouteMapItem({
    id: "public-auth-composition-route-promotion-lane",
    products,
    culture,
    copy: {
      cardLabel: copy.publicAuthPromotionLaneCardLabel,
      heroLabel: copy.publicAuthPromotionLaneHeroLabel,
      valueLabel: copy.publicAuthPromotionLaneValueLabel,
      liveOffersLabel: copy.publicAuthPromotionLaneLiveOffersLabel,
      baseLabel: copy.publicAuthPromotionLaneBaseLabel,
      title: copy.publicAuthPromotionLaneTitle,
      fallbackTitle: copy.publicAuthPromotionLaneFallbackTitle,
      description: copy.publicAuthPromotionLaneDescription,
      fallbackDescription: copy.publicAuthPromotionLaneFallbackDescription,
      cta: copy.publicAuthPromotionLaneCta,
      meta: copy.publicAuthPromotionLaneMeta,
    },
  });
  const extendedRouteMapItems = [
    ...routeMapItems,
    {
      label: copy.publicAuthCompositionRouteMapContentLabel,
      title: strongestPage
        ? formatResource(copy.publicAuthCompositionRouteMapContentTitle, {
            title: strongestPage.title,
          })
        : copy.publicAuthCompositionRouteMapContentFallbackTitle,
      description:
        strongestPage?.metaDescription ??
        copy.publicAuthCompositionRouteMapContentFallbackDescription,
      href: strongestPage ? buildCmsPagePath(strongestPage.slug) : "/cms",
      ctaLabel: copy.publicAuthCompositionRouteMapContentCta,
      meta: formatResource(copy.publicAuthCompositionRouteMapContentMeta, {
        count: cmsPages.length,
      }),
    },
    {
      label: copy.publicAuthCompositionRouteMapCatalogLabel,
      title: strongestProduct
        ? formatResource(copy.publicAuthCompositionRouteMapCatalogTitle, {
            product: strongestProduct.name,
          })
        : strongestCategory
          ? formatResource(copy.publicAuthCompositionRouteMapCatalogCategoryTitle, {
              category: strongestCategory.name,
            })
          : copy.publicAuthCompositionRouteMapCatalogFallbackTitle,
      description: strongestProduct
        ? formatResource(copy.publicAuthCompositionRouteMapCatalogDescription, {
            price: formatMoney(
              strongestProduct.priceMinor,
              strongestProduct.currency,
              culture,
            ),
          })
        : strongestCategory?.description ??
          copy.publicAuthCompositionRouteMapCatalogFallbackDescription,
      href: storefrontCard.href,
      ctaLabel: copy.publicAuthCompositionRouteMapCatalogCta,
      meta: formatResource(copy.publicAuthCompositionRouteMapCatalogMeta, {
        count: products.length,
      }),
    },
    promotionLaneRouteMapItem,
  ];

  return (
    <div className="grid gap-5 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,0.95fr)]">
      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
          {copy.publicAuthCompositionJourneyTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.publicAuthCompositionJourneyMessage}
        </p>
        <div className="mt-5 grid gap-3">
          {[routeCard, nextCard, storefrontCard].map((card) => (
            <article
              key={`${card.label}-${card.title}`}
              className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
            >
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {card.label}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {card.title}
              </p>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {card.description}
              </p>
              {card.meta ? (
                <p className="mt-3 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {card.meta}
                </p>
              ) : null}
              <div className="mt-4">
                <Link
                  href={localizeHref(card.href, culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {card.ctaLabel}
                </Link>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
          {copy.publicAuthCompositionRouteMapTitle}
        </p>
        <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
          {copy.publicAuthCompositionRouteMapMessage}
        </p>
        <div className="mt-5 grid gap-3">
          {extendedRouteMapItems.map((item) => (
            <article
              key={`${item.label}-${item.title}`}
              className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
            >
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {item.label}
              </p>
              <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                {item.title}
              </p>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {item.description}
              </p>
              {item.meta ? (
                <p className="mt-3 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {item.meta}
                </p>
              ) : null}
              <div className="mt-4">
                <Link
                  href={localizeHref(item.href, culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {item.ctaLabel}
                </Link>
              </div>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}



