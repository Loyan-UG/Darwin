import Link from "next/link";
import { localizeHref } from "@/lib/locale-routing";
import { getSharedResource } from "@/localization";
import { PageComposer } from "@/web-parts/page-composer";
import type {
  BlankStatePagePart,
  CardGridPagePart,
  WebPagePart,
} from "@/web-parts/types";

type HomePageComposerProps = {
  parts: WebPagePart[];
  culture: string;
};

function findPart<K extends WebPagePart["kind"]>(
  parts: WebPagePart[],
  id: string,
  kind: K,
): Extract<WebPagePart, { kind: K }> | undefined {
  const part = parts.find((candidate) => candidate.id === id && candidate.kind === kind);
  return part as Extract<WebPagePart, { kind: K }> | undefined;
}

function getCardTone(index: number) {
  const tones = [
    "from-[#e8f6cc] via-white to-[#fff4d6]",
    "from-[#fff0cc] via-white to-[#ffe2cb]",
    "from-[#dcf5df] via-white to-[#f3f8d6]",
    "from-[#eef8d9] via-white to-[#fff2cc]",
  ];

  return tones[index % tones.length]!;
}

function getCardAccent(index: number) {
  const accents = [
    "bg-[#2f7d32]",
    "bg-[#ef6c00]",
    "bg-[#558b2f]",
    "bg-[#ff8f00]",
  ];

  return accents[index % accents.length]!;
}

function GroceryCardGrid({
  part,
  culture,
  columns = "lg:grid-cols-3",
  emphasizeMeta = false,
  compact = false,
}: {
  part: CardGridPagePart;
  culture: string;
  columns?: string;
  emphasizeMeta?: boolean;
  compact?: boolean;
}) {
  const shared = getSharedResource(culture);

  return (
    <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-white/90 p-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)] sm:p-8">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div className="max-w-3xl">
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-[var(--color-brand)]">
            {part.eyebrow}
          </p>
          <h2 className="mt-3 font-[family-name:var(--font-display)] text-3xl leading-tight text-[var(--color-text-primary)] sm:text-4xl">
            {part.title}
          </h2>
          <p className="mt-3 text-base leading-8 text-[var(--color-text-secondary)]">
            {part.description}
          </p>
        </div>
      </div>

      {part.cards.length > 0 ? (
        <div className={`mt-8 grid gap-5 ${columns}`}>
          {part.cards.map((card, index) => (
            <article
              key={card.id}
              className={`group relative overflow-hidden rounded-[1rem] border border-[rgba(53,92,38,0.1)] bg-gradient-to-br ${getCardTone(index)} p-5 shadow-[0_16px_40px_rgba(50,88,35,0.08)] transition duration-200 hover:-translate-y-1`}
            >
              <div
                aria-hidden="true"
                className={`absolute right-4 top-4 h-12 w-12 rounded-full ${getCardAccent(index)} opacity-10 blur-2xl`}
              />
              {card.eyebrow ? (
                <div className="inline-flex rounded-full bg-white/85 px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)] shadow-sm">
                  {card.eyebrow}
                </div>
              ) : null}
              <h3 className="mt-4 text-xl font-semibold text-[var(--color-text-primary)]">
                {card.title}
              </h3>
              <p className={`${compact ? "mt-3" : "mt-3 min-h-[5.25rem]"} text-sm leading-7 text-[var(--color-text-secondary)]`}>
                {card.description}
              </p>
              <div className="mt-5 flex items-center justify-between gap-4">
                <Link
                  href={localizeHref(card.href, culture)}
                  className="inline-flex items-center rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[var(--color-brand-strong)]"
                >
                  {card.ctaLabel ?? shared.openLinkCta}
                </Link>
                {card.meta ? (
                  <span
                    className={
                      emphasizeMeta
                        ? "rounded-full bg-white px-3 py-2 text-sm font-semibold text-[var(--color-accent)] shadow-sm"
                        : "text-right text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-muted)]"
                    }
                  >
                    {card.meta}
                  </span>
                ) : null}
              </div>
            </article>
          ))}
        </div>
      ) : (
        <div className="mt-8 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
          {part.emptyMessage}
        </div>
      )}
    </section>
  );
}

function GroceryActionPanel({
  part,
  culture,
}: {
  part: BlankStatePagePart;
  culture: string;
}) {
  return (
    <section className="relative overflow-hidden rounded-[1rem] border border-[rgba(61,105,52,0.12)] bg-[linear-gradient(135deg,#2f7d32_0%,#558b2f_48%,#f6ffe9_100%)] p-6 text-white shadow-[0_24px_80px_rgba(38,76,34,0.16)] sm:p-8">
      <div
        aria-hidden="true"
        className="absolute -right-20 -top-20 h-64 w-64 rounded-full bg-white/20 blur-3xl"
      />
      <div className="relative max-w-3xl">
        <p className="text-xs font-semibold uppercase tracking-[0.3em] text-white/80">
          {part.eyebrow}
        </p>
        <h2 className="mt-3 font-[family-name:var(--font-display)] text-3xl leading-tight sm:text-4xl">
          {part.title}
        </h2>
        <p className="mt-3 text-base leading-8 text-white/82">
          {part.description}
        </p>
        <div className="mt-6 flex flex-wrap gap-3">
          {part.actions.map((action) => (
            <Link
              key={`${part.id}-${action.href}`}
              href={localizeHref(action.href, culture)}
              className={
                action.variant === "secondary"
                  ? "inline-flex items-center rounded-full border border-white/45 bg-white/10 px-5 py-3 text-sm font-semibold text-white transition hover:bg-white/18"
                  : "inline-flex items-center rounded-full bg-white px-5 py-3 text-sm font-semibold text-[var(--color-brand)] shadow-sm transition hover:bg-[var(--color-surface-panel-strong)]"
              }
            >
              {action.label}
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}

export function HomePageComposer({ parts, culture }: HomePageComposerProps) {
  const shared = getSharedResource(culture);
  const hero = findPart(parts, "home-hero", "hero");

  if (!hero) {
    return <PageComposer parts={parts} culture={culture} />;
  }

  const categories = findPart(parts, "home-category-spotlight", "card-grid");
  const products = findPart(parts, "home-product-spotlight", "card-grid");
  const loyalty = findPart(parts, "home-loyalty-teaser", "blank-state");
  const cmsSpotlight = findPart(parts, "home-cms-spotlight", "card-grid");
  const trustStrip = findPart(parts, "home-trust-strip", "card-grid");

  const heroCategories = categories?.cards.slice(0, 6) ?? [];
  const heroProducts = products?.cards.slice(0, 3) ?? [];
  const topOffers = products?.cards.slice(0, 8) ?? [];
  const helpCards = cmsSpotlight?.cards.slice(0, 3) ?? [];

  return (
    <div className="mx-auto flex w-full max-w-[1320px] flex-1 flex-col gap-8 px-4 py-6 sm:px-6 sm:py-8 lg:px-8 lg:py-10">
      <section className="relative overflow-hidden rounded-[1rem] border border-[rgba(61,105,52,0.12)] bg-[linear-gradient(135deg,#f6ffe9_0%,#ffffff_40%,#fff4d8_100%)] px-6 py-8 shadow-[0_34px_120px_rgba(38,76,34,0.12)] sm:px-8 sm:py-10 lg:px-10 lg:py-12">
        <div
          aria-hidden="true"
          className="absolute -right-20 -top-10 h-72 w-72 rounded-full bg-[rgba(76,175,80,0.12)] blur-3xl"
        />
        <div
          aria-hidden="true"
          className="absolute bottom-0 left-0 h-56 w-56 rounded-full bg-[rgba(255,152,0,0.14)] blur-3xl"
        />
        <div className="relative grid gap-8 lg:grid-cols-[1.2fr_0.8fr]">
          <div>
            <div className="inline-flex items-center gap-2 rounded-full bg-white/85 px-4 py-2 text-xs font-semibold uppercase tracking-[0.28em] text-[var(--color-brand)] shadow-sm">
              <span className="inline-flex h-2.5 w-2.5 rounded-full bg-[var(--color-accent)]" />
              {hero.eyebrow}
            </div>
            <h1 className="mt-5 max-w-4xl font-[family-name:var(--font-display)] text-4xl leading-[1.02] text-[var(--color-text-primary)] sm:text-5xl lg:text-[4.25rem]">
              {hero.title}
            </h1>
            <p className="mt-5 max-w-2xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
              {hero.description}
            </p>

            <div className="mt-7 flex flex-wrap gap-3">
              {hero.actions.map((action) => (
                <Link
                  key={action.href}
                  href={localizeHref(action.href, culture)}
                  className={
                    action.variant === "secondary"
                      ? "inline-flex items-center rounded-full border border-[rgba(53,92,38,0.12)] bg-white/85 px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] shadow-sm transition hover:border-[var(--color-brand)] hover:text-[var(--color-brand)]"
                      : "inline-flex items-center rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-white shadow-[0_14px_30px_rgba(47,125,50,0.24)] transition hover:bg-[var(--color-brand-strong)]"
                  }
                >
                  {action.label}
                </Link>
              ))}
            </div>

            <div className="mt-7 flex flex-wrap gap-2">
              {hero.highlights.map((highlight) => (
                <span
                  key={highlight}
                  className="rounded-full border border-[rgba(53,92,38,0.1)] bg-white/80 px-4 py-2 text-sm text-[var(--color-text-secondary)] shadow-sm"
                >
                  {highlight}
                </span>
              ))}
            </div>

            {heroCategories.length > 0 ? (
              <div className="mt-8 rounded-[1rem] border border-white/70 bg-white/78 p-5 shadow-sm backdrop-blur">
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-brand)]">
                      Fresh aisles
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {categories?.description ?? shared.siteDescription}
                    </p>
                  </div>
                  <Link
                    href={localizeHref("/catalog", culture)}
                    className="hidden rounded-full border border-[rgba(53,92,38,0.12)] bg-white px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:border-[var(--color-brand)] hover:text-[var(--color-brand)] sm:inline-flex"
                  >
                    {shared.catalogBrowseAllCta}
                  </Link>
                </div>
                <div className="mt-4 flex flex-wrap gap-3">
                  {heroCategories.map((card, index) => (
                    <Link
                      key={card.id}
                      href={localizeHref(card.href, culture)}
                      className={`inline-flex items-center gap-3 rounded-full bg-gradient-to-r ${getCardTone(index)} px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] shadow-sm transition hover:-translate-y-0.5`}
                    >
                      <span className={`inline-flex h-3 w-3 rounded-full ${getCardAccent(index)}`} />
                      {card.title}
                    </Link>
                  ))}
                </div>
              </div>
            ) : null}
          </div>

          <div className="grid gap-5">
            <div className="rounded-[1rem] border border-[rgba(53,92,38,0.12)] bg-white/86 p-6 shadow-sm backdrop-blur">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-brand)]">
                    {products?.eyebrow ?? hero.panelTitle ?? "Featured products"}
                  </p>
                  <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                    {products?.title ?? shared.siteTitle}
                  </h2>
                </div>
                <span className="rounded-full bg-[rgba(239,108,0,0.12)] px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                  Shop
                </span>
              </div>

              {heroProducts.length > 0 ? (
                <div className="mt-5 space-y-3">
                  {heroProducts.map((item, index) => (
                    <div
                      key={item.id}
                      className="rounded-[1rem] border border-[rgba(53,92,38,0.08)] bg-[linear-gradient(135deg,rgba(246,255,233,0.96),rgba(255,255,255,0.96))] p-4"
                    >
                      <div className="flex items-start gap-3">
                        <span className={`mt-1 inline-flex h-8 w-8 flex-none items-center justify-center rounded-full ${getCardAccent(index)} text-xs font-semibold text-white`}>
                          {index + 1}
                        </span>
                        <div>
                          <h3 className="text-base font-semibold text-[var(--color-text-primary)]">
                            {item.title}
                          </h3>
                          <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {item.description}
                          </p>
                          <div className="mt-3 flex flex-wrap items-center gap-3">
                            <Link
                              href={localizeHref(item.href, culture)}
                              className="inline-flex items-center rounded-full bg-[var(--color-text-primary)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[var(--color-brand)]"
                            >
                              {item.ctaLabel}
                            </Link>
                            {item.meta ? (
                              <span className="text-[11px] font-semibold uppercase tracking-[0.16em] text-[var(--color-text-muted)]">
                                {item.meta}
                              </span>
                            ) : null}
                          </div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="mt-5 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel-strong)] px-5 py-8 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {products?.emptyMessage ?? shared.siteDescription}
                </div>
              )}
            </div>
          </div>
        </div>
      </section>

      {categories ? (
        <GroceryCardGrid part={categories} culture={culture} columns="md:grid-cols-2 xl:grid-cols-4" />
      ) : null}

      {topOffers.length > 0 && products ? (
        <GroceryCardGrid
          part={{
            ...products,
            cards: topOffers,
          }}
          culture={culture}
          columns="md:grid-cols-2 xl:grid-cols-4"
          emphasizeMeta
        />
      ) : null}

      {loyalty ? <GroceryActionPanel part={loyalty} culture={culture} /> : null}

      {cmsSpotlight ? (
        <GroceryCardGrid
          part={{
            ...cmsSpotlight,
            cards: helpCards,
          }}
          culture={culture}
          columns="md:grid-cols-3"
          compact
        />
      ) : null}

      {trustStrip ? (
        <GroceryCardGrid
          part={trustStrip}
          culture={culture}
          columns="md:grid-cols-3"
          compact
        />
      ) : null}
    </div>
  );
}
