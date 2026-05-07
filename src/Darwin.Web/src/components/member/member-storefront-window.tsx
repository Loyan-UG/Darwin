import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import Link from "next/link";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import { StorefrontSpotlightBoard } from "@/components/storefront/storefront-spotlight-board";
import type {
  StorefrontCampaignCard,
  StorefrontOfferCard,
  StorefrontSpotlightCard,
} from "@/features/storefront/storefront-campaigns";
import { localizeHref } from "@/lib/locale-routing";

type MemberStorefrontWindowProps = {
  culture: string;
  title: string;
  message: string;
  cmsTitle: string;
  cmsCtaLabel: string;
  cmsCards: StorefrontSpotlightCard[];
  cmsEmptyMessage: string;
  catalogTitle: string;
  catalogCtaLabel: string;
  categoryCards: StorefrontSpotlightCard[];
  catalogEmptyMessage: string;
  productTitle: string;
  productCtaLabel: string;
  productMessage: string;
  productCards: StorefrontOfferCard[];
  productEmptyMessage: string;
  promotionLaneSectionTitle?: string;
  promotionLaneSectionMessage?: string;
  promotionLaneCards?: StorefrontCampaignCard[];
  cartSectionTitle?: string;
  cartSectionMessage?: string;
  cartSectionCartCtaLabel?: string;
  cartSectionCheckoutCtaLabel?: string;
};

export function MemberStorefrontWindow({
  culture,
  title,
  message,
  cmsTitle,
  cmsCtaLabel,
  cmsCards,
  cmsEmptyMessage,
  catalogTitle,
  catalogCtaLabel,
  categoryCards,
  catalogEmptyMessage,
  productTitle,
  productCtaLabel,
  productMessage,
  productCards,
  productEmptyMessage,
  promotionLaneSectionTitle,
  promotionLaneSectionMessage,
  promotionLaneCards = [],
  cartSectionTitle,
  cartSectionMessage,
  cartSectionCartCtaLabel,
  cartSectionCheckoutCtaLabel,
}: MemberStorefrontWindowProps) {
  return (
    <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
        {title}
      </p>
      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        {message}
      </p>
      <div className="mt-5 grid gap-4 xl:grid-cols-3">
        <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <div className="flex items-center justify-between gap-3">
            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
              {cmsTitle}
            </p>
            <Link
              href={localizeHref("/cms", culture)}
              className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
            >
              {cmsCtaLabel}
            </Link>
          </div>
          <StorefrontSpotlightBoard
            culture={culture}
            cards={cmsCards}
            emptyMessage={cmsEmptyMessage}
          />
        </div>

        <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <div className="flex items-center justify-between gap-3">
            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
              {catalogTitle}
            </p>
            <Link
              href={localizeHref("/catalog", culture)}
              className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
            >
              {catalogCtaLabel}
            </Link>
          </div>
          <StorefrontSpotlightBoard
            culture={culture}
            cards={categoryCards}
            emptyMessage={catalogEmptyMessage}
          />
        </div>

        <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <div className="flex items-center justify-between gap-3">
            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
              {productTitle}
            </p>
            <Link
              href={localizeHref("/catalog", culture)}
              className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
            >
              {productCtaLabel}
            </Link>
          </div>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {productMessage}
          </p>
          <StorefrontOfferBoard
            culture={culture}
            cards={productCards}
            emptyMessage={productEmptyMessage}
            columnsClassName="grid-cols-1"
          />
        </div>
      </div>
      {promotionLaneCards.length > 0 &&
      promotionLaneSectionTitle &&
      promotionLaneSectionMessage ? (
        <div className="mt-5 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
            {promotionLaneSectionTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {promotionLaneSectionMessage}
          </p>
          <StorefrontCampaignBoard
            culture={culture}
            cards={promotionLaneCards}
            emptyMessage={promotionLaneSectionMessage}
          />
        </div>
      ) : null}
      {cartSectionTitle && cartSectionMessage ? (
        <div className="mt-5 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
            {cartSectionTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {cartSectionMessage}
          </p>
          {cartSectionCartCtaLabel || cartSectionCheckoutCtaLabel ? (
            <div className="mt-4 flex flex-wrap gap-3">
              {cartSectionCartCtaLabel ? (
                <Link
                  href={localizeHref("/cart", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {cartSectionCartCtaLabel}
                </Link>
              ) : null}
              {cartSectionCheckoutCtaLabel ? (
                <Link
                  href={localizeHref("/checkout", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {cartSectionCheckoutCtaLabel}
                </Link>
              ) : null}
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}


