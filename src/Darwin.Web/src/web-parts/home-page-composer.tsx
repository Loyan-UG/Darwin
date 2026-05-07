import Link from "next/link";
import { localizeHref } from "@/lib/locale-routing";
import { getSharedResource } from "@/localization";
import { PageComposer } from "@/web-parts/page-composer";
import type {
  CardGridPagePart,
  LinkListPagePart,
  RouteMapPagePart,
  StatusListPagePart,
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
}: {
  part: CardGridPagePart;
  culture: string;
  columns?: string;
  emphasizeMeta?: boolean;
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
              <p className="mt-3 min-h-[5.25rem] text-sm leading-7 text-[var(--color-text-secondary)]">
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

function GroceryLinkList({
  part,
  culture,
  compact = false,
}: {
  part: LinkListPagePart;
  culture: string;
  compact?: boolean;
}) {
  return (
    <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-white/90 p-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)] sm:p-8">
      <p className="text-xs font-semibold uppercase tracking-[0.3em] text-[var(--color-brand)]">
        {part.eyebrow}
      </p>
      <h2 className="mt-3 font-[family-name:var(--font-display)] text-3xl leading-tight text-[var(--color-text-primary)] sm:text-4xl">
        {part.title}
      </h2>
      <p className="mt-3 text-base leading-8 text-[var(--color-text-secondary)]">
        {part.description}
      </p>

      {part.items.length > 0 ? (
        <div className={`mt-8 grid gap-4 ${compact ? "lg:grid-cols-2" : "xl:grid-cols-3"}`}>
          {part.items.map((item, index) => (
            <article
              key={item.id}
              className={`rounded-[1rem] border border-[rgba(53,92,38,0.1)] bg-gradient-to-br ${getCardTone(index)} p-5`}
            >
              {item.meta ? (
                <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {item.meta}
                </p>
              ) : null}
              <h3 className="mt-3 text-xl font-semibold text-[var(--color-text-primary)]">
                {item.title}
              </h3>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {item.description}
              </p>
              <div className="mt-5">
                <Link
                  href={localizeHref(item.href, culture)}
                  className="inline-flex items-center rounded-full border border-[rgba(53,92,38,0.12)] bg-white/85 px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:border-[var(--color-brand)] hover:text-[var(--color-brand)]"
                >
                  {item.ctaLabel}
                </Link>
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

function GroceryStatusBoard({
  part,
  culture,
}: {
  part: StatusListPagePart;
  culture: string;
}) {
  return (
    <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[linear-gradient(180deg,rgba(242,255,233,0.92),rgba(255,255,255,0.96))] p-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)] sm:p-8">
      <p className="text-xs font-semibold uppercase tracking-[0.3em] text-[var(--color-brand)]">
        {part.eyebrow}
      </p>
      <h2 className="mt-3 font-[family-name:var(--font-display)] text-3xl leading-tight text-[var(--color-text-primary)] sm:text-4xl">
        {part.title}
      </h2>
      <p className="mt-3 text-base leading-8 text-[var(--color-text-secondary)]">
        {part.description}
      </p>

      {part.items.length > 0 ? (
        <div className="mt-8 grid gap-4 xl:grid-cols-3">
          {part.items.map((item) => (
            <article
              key={item.id}
              className="rounded-[1rem] border border-[rgba(53,92,38,0.1)] bg-white/90 p-5 shadow-[0_10px_30px_rgba(38,76,34,0.05)]"
            >
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                    {item.label}
                  </p>
                  <h3 className="mt-3 text-xl font-semibold text-[var(--color-text-primary)]">
                    {item.title}
                  </h3>
                </div>
                <span
                  className={
                    item.tone === "warning"
                      ? "rounded-full bg-[rgba(239,108,0,0.12)] px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]"
                      : "rounded-full bg-[rgba(47,125,50,0.12)] px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--color-brand)]"
                  }
                >
                  {item.tone === "warning" ? "Watch" : "Ready"}
                </span>
              </div>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {item.description}
              </p>
              {item.meta ? (
                <p className="mt-4 text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {item.meta}
                </p>
              ) : null}
              <div className="mt-5">
                <Link
                  href={localizeHref(item.href, culture)}
                  className="inline-flex items-center rounded-full bg-[var(--color-text-primary)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[var(--color-brand)]"
                >
                  {item.ctaLabel}
                </Link>
              </div>
            </article>
          ))}
        </div>
      ) : (
        <div className="mt-8 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-white/80 px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
          {part.emptyMessage}
        </div>
      )}
    </section>
  );
}

function GroceryRouteMap({
  part,
  culture,
}: {
  part: RouteMapPagePart;
  culture: string;
}) {
  return (
    <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-white/90 p-6 shadow-[0_24px_80px_rgba(38,76,34,0.08)] sm:p-8">
      <p className="text-xs font-semibold uppercase tracking-[0.3em] text-[var(--color-brand)]">
        {part.eyebrow}
      </p>
      <h2 className="mt-3 font-[family-name:var(--font-display)] text-3xl leading-tight text-[var(--color-text-primary)] sm:text-4xl">
        {part.title}
      </h2>
      <p className="mt-3 text-base leading-8 text-[var(--color-text-secondary)]">
        {part.description}
      </p>

      <div className="mt-8 grid gap-5 xl:grid-cols-3">
        {part.items.map((item, index) => (
          <article
            key={item.id}
            className={`rounded-[1rem] border border-[rgba(53,92,38,0.1)] bg-gradient-to-br ${getCardTone(index)} p-5`}
          >
            <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
              {item.label}
            </p>
            <h3 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
              {item.title}
            </h3>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {item.description}
            </p>
            {item.meta ? (
              <p className="mt-4 text-[11px] font-semibold uppercase tracking-[0.16em] text-[var(--color-text-muted)]">
                {item.meta}
              </p>
            ) : null}
            <div className="mt-5 flex flex-wrap gap-3">
              <Link
                href={localizeHref(item.primaryHref, culture)}
                className="inline-flex items-center rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-white transition hover:bg-[var(--color-brand-strong)]"
              >
                {item.primaryCtaLabel}
              </Link>
              {item.secondaryHref && item.secondaryCtaLabel ? (
                <Link
                  href={localizeHref(item.secondaryHref, culture)}
                  className="inline-flex items-center rounded-full border border-[rgba(53,92,38,0.12)] bg-white/85 px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:border-[var(--color-brand)] hover:text-[var(--color-brand)]"
                >
                  {item.secondaryCtaLabel}
                </Link>
              ) : null}
            </div>
          </article>
        ))}
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

  const metrics = findPart(parts, "home-metrics", "stat-grid");
  const priority = findPart(parts, "home-priority-lane", "link-list");
  const offerBoard = findPart(parts, "home-offer-board", "card-grid");
  const campaignBoard = findPart(parts, "home-campaign-board", "card-grid");
  const promotionLanes = findPart(parts, "home-promotion-lanes", "card-grid");
  const routeMap = findPart(parts, "home-route-map", "route-map");
  const memberResume = findPart(parts, "home-member-resume", "link-list");
  const cartResume = findPart(parts, "home-cart-resume", "link-list");
  const cartWindow = findPart(parts, "home-cart-window", "status-list");
  const categories = findPart(parts, "home-category-spotlight", "card-grid");
  const products = findPart(parts, "home-product-spotlight", "card-grid");
  const cmsSpotlight = findPart(parts, "home-cms-spotlight", "card-grid");
  const shortcuts = findPart(parts, "home-shortcuts", "card-grid");
  const journeys = findPart(parts, "home-journeys", "link-list");
  const recoveryRail = findPart(parts, "home-recovery-rail", "link-list");
  const commerceOpportunity = findPart(parts, "home-commerce-opportunity", "status-list");

  const heroCategories = categories?.cards.slice(0, 6) ?? [];
  const heroPriority = priority?.items.slice(0, 3) ?? [];
  const featureMetrics = metrics?.metrics.slice(0, 4) ?? [];
  const topOffers = offerBoard?.cards.slice(0, 4) ?? products?.cards.slice(0, 4) ?? [];
  const editorialCards = campaignBoard?.cards.slice(0, 3) ?? cmsSpotlight?.cards.slice(0, 3) ?? [];
  const supportPanels = [memberResume, cartResume, recoveryRail].filter(Boolean) as LinkListPagePart[];

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
                    {hero.panelTitle ?? "Storefront board"}
                  </p>
                  <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                    Grocery-first merchandising
                  </h2>
                </div>
                <span className="rounded-full bg-[rgba(239,108,0,0.12)] px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                  Live
                </span>
              </div>

              {heroPriority.length > 0 ? (
                <div className="mt-5 space-y-3">
                  {heroPriority.map((item, index) => (
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
                  {priority?.emptyMessage ?? shared.siteDescription}
                </div>
              )}
            </div>

            {featureMetrics.length > 0 ? (
              <div className="grid gap-4 sm:grid-cols-2">
                {featureMetrics.map((metric, index) => (
                  <article
                    key={metric.id}
                    className={`rounded-[1rem] border border-[rgba(53,92,38,0.1)] bg-gradient-to-br ${getCardTone(index)} p-5`}
                  >
                    <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {metric.label}
                    </p>
                    <p className="mt-3 text-4xl font-semibold text-[var(--color-text-primary)]">
                      {metric.value}
                    </p>
                    <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {metric.note}
                    </p>
                  </article>
                ))}
              </div>
            ) : null}
          </div>
        </div>
      </section>

      {categories ? (
        <GroceryCardGrid part={categories} culture={culture} columns="md:grid-cols-2 xl:grid-cols-4" />
      ) : null}

      {topOffers.length > 0 || promotionLanes ? (
        <section className="grid gap-6 xl:grid-cols-[1.35fr_0.65fr]">
          {topOffers.length > 0 ? (
            <GroceryCardGrid
              part={{
                ...(offerBoard ?? products!),
                cards: topOffers,
              }}
              culture={culture}
              columns="md:grid-cols-2"
              emphasizeMeta
            />
          ) : null}
          {promotionLanes ? (
            <GroceryCardGrid
              part={{
                ...promotionLanes,
                cards: promotionLanes.cards.slice(0, 4),
              }}
              culture={culture}
              columns="grid-cols-1"
            />
          ) : null}
        </section>
      ) : null}

      {editorialCards.length > 0 || cmsSpotlight || routeMap ? (
        <section className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
          {editorialCards.length > 0 ? (
            <GroceryCardGrid
              part={{
                ...(campaignBoard ?? cmsSpotlight!),
                cards: editorialCards,
              }}
              culture={culture}
              columns="md:grid-cols-2 xl:grid-cols-1"
            />
          ) : null}
          <div className="grid gap-6">
            {routeMap ? <GroceryRouteMap part={routeMap} culture={culture} /> : null}
            {cmsSpotlight ? (
              <GroceryCardGrid
                part={{
                  ...cmsSpotlight,
                  cards: cmsSpotlight.cards.slice(0, 3),
                }}
                culture={culture}
                columns="grid-cols-1"
              />
            ) : null}
          </div>
        </section>
      ) : null}

      {commerceOpportunity || cartWindow ? (
        <section className="grid gap-6 xl:grid-cols-2">
          {commerceOpportunity ? <GroceryStatusBoard part={commerceOpportunity} culture={culture} /> : null}
          {cartWindow ? <GroceryStatusBoard part={cartWindow} culture={culture} /> : null}
        </section>
      ) : null}

      {supportPanels.length > 0 ? (
        <section className="grid gap-6 xl:grid-cols-3">
          {supportPanels.map((panel) => (
            <GroceryLinkList key={panel.id} part={panel} culture={culture} compact />
          ))}
        </section>
      ) : null}

      {shortcuts || journeys ? (
        <section className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
          {shortcuts ? (
            <GroceryCardGrid part={shortcuts} culture={culture} columns="md:grid-cols-3 xl:grid-cols-1" />
          ) : null}
          {journeys ? <GroceryLinkList part={journeys} culture={culture} /> : null}
        </section>
      ) : null}
    </div>
  );
}
