import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { MemberOrderSummary } from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  resolveApiStatusLabel,
} from "@/localization";
import { buildOrderPath } from "@/lib/entity-paths";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";

type OrdersPageProps = {
  culture: string;
  orders: MemberOrderSummary[];
  status: string;
  currentPage: number;
  totalPages: number;
  visibleQuery?: string;
  visibleState: "all" | "attention" | "settled";
};

function isAttentionOrder(order: MemberOrderSummary) {
  return /(pending|processing|payment|review|hold|open)/i.test(order.status);
}

function localizeOrderStatus(status: string, culture: string) {
  if (culture.toLowerCase().startsWith("en")) {
    return status;
  }

  const labels: Record<string, string> = {
    Created: "Erstellt",
    Confirmed: "Bestaetigt",
    Paid: "Bezahlt",
    PartiallyShipped: "Teilweise versendet",
    Shipped: "Versendet",
    Delivered: "Zugestellt",
    Cancelled: "Storniert",
    Refunded: "Erstattet",
    PartiallyRefunded: "Teilweise erstattet",
    Completed: "Abgeschlossen",
  };

  return labels[status] ?? status;
}

function buildOrdersHref(
  page = 1,
  options?: {
    visibleQuery?: string;
    visibleState?: "all" | "attention" | "settled";
  },
) {
  return buildAppQueryPath("/orders", {
    page: page > 1 ? page : undefined,
    visibleQuery: options?.visibleQuery,
    visibleState:
      options?.visibleState && options.visibleState !== "all"
        ? options.visibleState
        : undefined,
  });
}

export function OrdersPage({
  culture,
  orders,
  status,
  currentPage,
  totalPages,
  visibleQuery,
  visibleState,
}: OrdersPageProps) {
  const copy = getMemberResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy) ?? status;
  const normalizedVisibleQuery = visibleQuery?.trim().toLowerCase() ?? "";
  const filteredOrders = orders.filter((order) => {
    const matchesQuery =
      normalizedVisibleQuery.length === 0 ||
      [order.orderNumber, order.status, localizeOrderStatus(order.status, culture), order.id]
        .filter(Boolean)
        .some((value) => value.toLowerCase().includes(normalizedVisibleQuery));
    const matchesState =
      visibleState === "all" ||
      (visibleState === "attention" && isAttentionOrder(order)) ||
      (visibleState === "settled" && !isAttentionOrder(order));

    return matchesQuery && matchesState;
  });

  return (
    <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-8 lg:grid-cols-[minmax(0,1fr)_320px]">
        <div className="flex flex-col gap-8">
          <nav
            aria-label={copy.memberBreadcrumbLabel}
            className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
          >
            <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-brand)]">
              {copy.memberBreadcrumbHome}
            </Link>
            <span>/</span>
            <Link href={localizeHref("/account", culture)} className="transition hover:text-[var(--color-brand)]">
              {copy.memberBreadcrumbAccount}
            </Link>
            <span>/</span>
            <span className="font-medium text-[var(--color-text-primary)]">
              {copy.memberBreadcrumbOrders}
            </span>
          </nav>

          <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {copy.ordersEyebrow}
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {copy.ordersTitle}
            </h1>
            <p className="mt-4 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.ordersPortalNote}
            </p>
          </header>

          {status !== "ok" ? (
            <StatusBanner
              tone="warning"
              title={copy.ordersWarningsTitle}
              message={formatResource(copy.ordersWarningsMessage, { status: statusLabel })}
            />
          ) : null}

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.ordersFilterTitle}
              </p>
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.ordersFilterMessage, {
                  count: filteredOrders.length,
                })}
              </p>
            </div>
            <form
              action={localizeHref("/orders", culture)}
              method="get"
              className="mt-5 grid gap-4 lg:grid-cols-[minmax(0,1fr)_220px_auto]"
            >
              <label className="flex flex-col gap-2 text-sm text-[var(--color-text-secondary)]">
                <span>{copy.ordersFilterSearchPlaceholder}</span>
                <input
                  type="search"
                  name="visibleQuery"
                  defaultValue={visibleQuery ?? ""}
                  placeholder={copy.ordersFilterSearchPlaceholder}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-base)] px-4 py-3 text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
                />
              </label>
              <label className="flex flex-col gap-2 text-sm text-[var(--color-text-secondary)]">
                <span>{copy.ordersFilterStateLabel}</span>
                <select
                  name="visibleState"
                  defaultValue={visibleState}
                  className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-base)] px-4 py-3 text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
                >
                  <option value="all">{copy.ordersFilterStateAll}</option>
                  <option value="attention">{copy.ordersFilterStateAttention}</option>
                  <option value="settled">{copy.ordersFilterStateSettled}</option>
                </select>
              </label>
              <div className="flex items-end gap-3">
                <button
                  type="submit"
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-white transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.ordersFilterApplyCta}
                </button>
                {(visibleQuery || visibleState !== "all") ? (
                  <Link
                    href={localizeHref("/orders", culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.clearFilterCta}
                  </Link>
                ) : null}
              </div>
            </form>
          </section>

          <section className="grid gap-5">
            {filteredOrders.map((order) => (
              <article
                key={order.id}
                className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
              >
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                      {localizeOrderStatus(order.status, culture)}
                    </p>
                    <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                      <Link
                        href={localizeHref(buildOrderPath(order.id), culture)}
                        className="transition hover:text-[var(--color-brand)]"
                      >
                        {order.orderNumber}
                      </Link>
                    </h2>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {copy.createdLabel} {formatDateTime(order.createdAtUtc, culture)}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                      {formatMoney(order.grandTotalGrossMinor, order.currency, culture)}
                    </p>
                    <Link
                      href={localizeHref(buildOrderPath(order.id), culture)}
                      className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      {copy.openOrderCta}
                    </Link>
                  </div>
                </div>
              </article>
            ))}
          </section>

          {orders.length === 0 ? (
            <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.noOrdersMessage}
              </p>
              <Link
                href={localizeHref("/catalog", culture)}
                className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
              >
                {copy.memberCrossSurfaceCatalogCta}
              </Link>
            </div>
          ) : null}

          {orders.length > 0 && filteredOrders.length === 0 ? (
            <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.ordersFilterEmptyMessage}
              </p>
            </div>
          ) : null}

          {totalPages > 1 ? (
            <div className="flex flex-wrap items-center gap-3">
              <Link
                aria-disabled={currentPage <= 1}
                href={localizeHref(
                  buildOrdersHref(Math.max(1, currentPage - 1), {
                    visibleQuery,
                    visibleState,
                  }),
                  culture,
                )}
                className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
              >
                {copy.previous}
              </Link>
              <p className="text-sm text-[var(--color-text-secondary)]">
                {formatResource(copy.pageLabel, { currentPage, totalPages })}
              </p>
              <Link
                aria-disabled={currentPage >= totalPages}
                href={localizeHref(
                  buildOrdersHref(Math.min(totalPages, currentPage + 1), {
                    visibleQuery,
                    visibleState,
                  }),
                  culture,
                )}
                className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
              >
                {copy.next}
              </Link>
            </div>
          ) : null}
        </div>

        <aside className="flex flex-col gap-6">
          <MemberPortalNav culture={culture} activePath="/orders" />
          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.ordersRouteLabel}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.ordersPortalNote}
            </p>
          </div>
        </aside>
      </div>
    </section>
  );
}
