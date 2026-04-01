import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { MemberOrderSummary } from "@/features/member-portal/types";
import { formatDateTime, formatMoney } from "@/lib/formatting";

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
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Member orders
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Order history now reads from the authenticated member portal surface
          </h1>
        </div>

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title="Orders loaded with warnings."
            message={`The member orders endpoint returned status "${status}".`}
          />
        )}

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
                    <Link href={`/orders/${order.id}`} className="transition hover:text-[var(--color-brand)]">
                      {order.orderNumber}
                    </Link>
                  </h2>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    Created {formatDateTime(order.createdAtUtc, culture)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                    {formatMoney(order.grandTotalGrossMinor, order.currency, culture)}
                  </p>
                  <Link
                    href={`/orders/${order.id}`}
                    className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    Open order
                  </Link>
                </div>
              </div>
            </article>
          ))}
        </div>

        {orders.length === 0 && (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
            No member orders were returned for this history page.
          </div>
        )}

        {totalPages > 1 && (
          <div className="flex flex-wrap items-center gap-3">
            <Link
              aria-disabled={currentPage <= 1}
              href={buildOrdersHref(Math.max(1, currentPage - 1))}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              Previous
            </Link>
            <p className="text-sm text-[var(--color-text-secondary)]">
              Page {currentPage} of {totalPages}
            </p>
            <Link
              aria-disabled={currentPage >= totalPages}
              href={buildOrdersHref(Math.min(totalPages, currentPage + 1))}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              Next
            </Link>
          </div>
        )}
      </div>
    </section>
  );
}
