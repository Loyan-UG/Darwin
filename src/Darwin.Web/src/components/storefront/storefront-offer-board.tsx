import Link from "next/link";
import type { StorefrontOfferCard } from "@/features/storefront/storefront-campaigns";
import { localizeHref } from "@/lib/locale-routing";

type StorefrontOfferBoardProps = {
  culture: string;
  cards: StorefrontOfferCard[];
  emptyMessage: string;
  columnsClassName?: string;
};

export function StorefrontOfferBoard({
  culture,
  cards,
  emptyMessage,
  columnsClassName = "lg:grid-cols-3",
}: StorefrontOfferBoardProps) {
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
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
        >
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                {card.label}
              </p>
              <p className="font-semibold text-[var(--color-text-primary)]">
                {card.title}
              </p>
              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                {card.description}
              </p>
              {card.meta ? (
                <p className="mt-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {card.meta}
                </p>
              ) : null}
            </div>
            {card.price || card.ctaLabel ? (
              <div className="shrink-0 text-right">
                {card.price ? (
                  <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                    {card.price}
                  </p>
                ) : null}
                {card.ctaLabel ? (
                  <p className="mt-2 text-sm font-semibold text-[var(--color-brand)]">
                    {card.ctaLabel}
                  </p>
                ) : null}
              </div>
            ) : null}
          </div>
        </Link>
      ))}
    </div>
  );
}
