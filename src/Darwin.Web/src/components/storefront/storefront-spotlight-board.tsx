import Link from "next/link";
import type { StorefrontSpotlightCard } from "@/features/storefront/storefront-campaigns";
import { localizeHref } from "@/lib/locale-routing";

type StorefrontSpotlightBoardProps = {
  culture: string;
  cards: StorefrontSpotlightCard[];
  emptyMessage: string;
  columnsClassName?: string;
};

export function StorefrontSpotlightBoard({
  culture,
  cards,
  emptyMessage,
  columnsClassName = "",
}: StorefrontSpotlightBoardProps) {
  if (cards.length === 0) {
    return (
      <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
        {emptyMessage}
      </p>
    );
  }

  return (
    <div className={`mt-4 grid gap-3 ${columnsClassName}`.trim()}>
      {cards.map((card) => (
        <Link
          key={card.id}
          href={localizeHref(card.href, culture)}
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
        >
          <p className="font-semibold text-[var(--color-text-primary)]">
            {card.title}
          </p>
          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
            {card.description}
          </p>
        </Link>
      ))}
    </div>
  );
}
