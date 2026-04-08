import Link from "next/link";
import type { StorefrontCampaignCard } from "@/features/storefront/storefront-campaigns";
import { localizeHref } from "@/lib/locale-routing";

type StorefrontCampaignBoardProps = {
  culture: string;
  cards: StorefrontCampaignCard[];
  emptyMessage: string;
  columnsClassName?: string;
  cardClassName?: string;
};

export function StorefrontCampaignBoard({
  culture,
  cards,
  emptyMessage,
  columnsClassName = "lg:grid-cols-2",
  cardClassName = "bg-[var(--color-surface-panel)]",
}: StorefrontCampaignBoardProps) {
  if (cards.length === 0) {
    return (
      <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
        {emptyMessage}
      </p>
    );
  }

  return (
    <div className={`mt-4 grid gap-3 ${columnsClassName}`}>
      {cards.map((card) => (
        <Link
          key={card.id}
          href={localizeHref(card.href, culture)}
          className={`rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)] ${cardClassName}`}
        >
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
            {card.label}
          </p>
          <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
            {card.title}
          </p>
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {card.description}
          </p>
          <div className="mt-3 flex items-center justify-between gap-3">
            <p className="text-sm font-semibold text-[var(--color-brand)]">
              {card.ctaLabel}
            </p>
            {card.meta ? (
              <p className="text-xs font-medium text-[var(--color-text-muted)]">
                {card.meta}
              </p>
            ) : null}
          </div>
        </Link>
      ))}
    </div>
  );
}
