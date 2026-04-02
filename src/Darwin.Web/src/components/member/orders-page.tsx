import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { MemberOrderSummary } from "@/features/member-portal/types";
import { formatResource, getMemberResource } from "@/localization";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { localizeHref } from "@/lib/locale-routing";

type OrdersPageProps = {
  culture: string;
  orders: MemberOrderSummary[];
  status: string;
  currentPage: number;
  totalPages: number;
};

function buildOrdersHref(page = 1) {
  return page > 1 ? `/orders?page=${page}` : "/orders";
}

export function OrdersPage({
  culture,
  orders,
  status,
  currentPage,
  totalPages,
}: OrdersPageProps) {
  const copy = getMemberResource(culture);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
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

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.ordersEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.ordersTitle}
          </h1>
        </div>

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title={copy.ordersWarningsTitle}
            message={formatResource(copy.ordersWarningsMessage, { status })}
          />
        )}

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.ordersWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.ordersWindowMessage, {
              count: orders.length,
              currentPage,
              totalPages,
              status,
            })}
          </p>
        </div>

        <div className="grid gap-5">
          {orders.map((order) => (
            <article
              key={order.id}
              className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
            >
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {order.status}
                  </p>
                  <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                    <Link href={localizeHref(`/orders/${order.id}`, culture)} className="transition hover:text-[var(--color-brand)]">
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
                    href={localizeHref(`/orders/${order.id}`, culture)}
                    className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.openOrderCta}
                  </Link>
                </div>
              </div>
            </article>
          ))}
        </div>

        {orders.length === 0 && (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.noOrdersMessage}
            </p>
            <div className="mt-5 flex flex-wrap justify-center gap-3">
              <Link
                href={localizeHref("/catalog", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.memberCrossSurfaceCatalogCta}
              </Link>
              <Link
                href={localizeHref("/account", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.memberCrossSurfaceAccountCta}
              </Link>
            </div>
          </div>
        )}

        {totalPages > 1 && (
          <div className="flex flex-wrap items-center gap-3">
            <Link
              aria-disabled={currentPage <= 1}
              href={localizeHref(buildOrdersHref(Math.max(1, currentPage - 1)), culture)}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.previous}
            </Link>
            <p className="text-sm text-[var(--color-text-secondary)]">
              {formatResource(copy.pageLabel, { currentPage, totalPages })}
            </p>
            <Link
              aria-disabled={currentPage >= totalPages}
              href={localizeHref(buildOrdersHref(Math.min(totalPages, currentPage + 1)), culture)}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.next}
            </Link>
          </div>
        )}

        </div>

        <div className="flex flex-col gap-6">
          <MemberPortalNav culture={culture} activePath="/orders" />

          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.ordersRouteLabel}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.ordersPortalNote}
            </p>
          </aside>

          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.memberCrossSurfaceTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.memberCrossSurfaceMessage}
            </p>
            <div className="mt-5 flex flex-wrap gap-3">
              <Link href={localizeHref("/", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]">
                {copy.memberCrossSurfaceHomeCta}
              </Link>
              <Link href={localizeHref("/catalog", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]">
                {copy.memberCrossSurfaceCatalogCta}
              </Link>
              <Link href={localizeHref("/loyalty", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]">
                {copy.memberCrossSurfaceLoyaltyCta}
              </Link>
            </div>
          </aside>
        </div>
      </div>
    </section>
  );
}
