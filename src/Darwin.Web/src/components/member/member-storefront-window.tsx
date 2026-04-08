import Link from "next/link";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import { StorefrontSpotlightBoard } from "@/components/storefront/storefront-spotlight-board";
import type {
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
}: MemberStorefrontWindowProps) {
  return (
    <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
        {title}
      </p>
      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        {message}
      </p>
      <div className="mt-5 grid gap-4 xl:grid-cols-3">
        <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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

        <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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

        <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
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
    </div>
  );
}
