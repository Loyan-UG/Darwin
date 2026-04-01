import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { LoyaltyDiscoverySection } from "@/components/member/loyalty-discovery-section";
import type { BusinessCategoryKind, BusinessSummary } from "@/features/businesses/types";
import type {
  MyLoyaltyBusinessSummary,
  MyLoyaltyOverview,
} from "@/features/member-portal/types";
import { formatDateTime } from "@/lib/formatting";

type LoyaltyOverviewPageProps = {
  culture: string;
  overview: MyLoyaltyOverview | null;
  status: string;
  businesses: MyLoyaltyBusinessSummary[];
  businessesStatus: string;
  currentPage: number;
  totalPages: number;
  discoveryBusinesses: BusinessSummary[];
  discoveryStatus: string;
  discoveryCurrentPage: number;
  discoveryTotalPages: number;
  discoveryQuery?: string;
  discoveryCity?: string;
  discoveryCountryCode?: string;
  discoveryCategory?: string;
  discoveryLatitude?: number;
  discoveryLongitude?: number;
  discoveryRadiusKm?: number;
  discoveryCategories: BusinessCategoryKind[];
  hasMemberSession: boolean;
};

function buildLoyaltyHref(page = 1) {
  return page > 1 ? `/loyalty?joinedPage=${page}` : "/loyalty";
}

export function LoyaltyOverviewPage({
  culture,
  overview,
  status,
  businesses,
  businessesStatus,
  currentPage,
  totalPages,
  discoveryBusinesses,
  discoveryStatus,
  discoveryCurrentPage,
  discoveryTotalPages,
  discoveryQuery,
  discoveryCity,
  discoveryCountryCode,
  discoveryCategory,
  discoveryLatitude,
  discoveryLongitude,
  discoveryRadiusKm,
  discoveryCategories,
  hasMemberSession,
}: LoyaltyOverviewPageProps) {
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">Member loyalty</p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {hasMemberSession
              ? "Loyalty overview now spans joined businesses and public discovery"
              : "Loyalty discovery is available before a member joins"}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {hasMemberSession
              ? "Joined-business dashboards still come from the authenticated member contracts, while discovery and pre-join business context now come from the public business contracts."
              : "Anonymous visitors can browse loyalty-ready businesses first, then sign in only when they want to join a program or access member-only balance and history screens."}
          </p>
        </div>

        {(status !== "ok" || businessesStatus !== "ok") && hasMemberSession && (
          <StatusBanner tone="warning" title="Loyalty overview loaded with warnings." message={`Overview: ${status}. Businesses: ${businessesStatus}.`} />
        )}

        {hasMemberSession && overview ? (
          <>
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
              {[
                { label: "Total accounts", value: String(overview.totalAccounts) },
                { label: "Active accounts", value: String(overview.activeAccounts) },
                { label: "Points balance", value: String(overview.totalPointsBalance) },
                { label: "Lifetime points", value: String(overview.totalLifetimePoints) },
              ].map((item) => (
                <article key={item.label} className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{item.label}</p>
                  <p className="mt-4 text-3xl font-semibold text-[var(--color-text-primary)]">{item.value}</p>
                </article>
              ))}
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                    My loyalty places
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    Business-aware cards now come from the joined-loyalty places contract instead of only relying on aggregate account stats.
                  </p>
                </div>
                <p className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]">
                  {businesses.length} visible on this page
                </p>
              </div>

              {businesses.length > 0 ? (
                <div className="mt-5 grid gap-5 md:grid-cols-2">
                  {businesses.map((business) => (
                    <article
                      key={business.businessId}
                      className="overflow-hidden rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)]"
                    >
                      <div className="flex min-h-44 items-center justify-center bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-5">
                        {business.primaryImageUrl ? (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img
                            src={business.primaryImageUrl}
                            alt={business.businessName}
                            className="max-h-32 w-auto object-contain"
                          />
                        ) : (
                          <div className="text-center">
                            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-text-muted)]">
                              {business.category}
                            </p>
                            <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
                              No business media
                            </p>
                          </div>
                        )}
                      </div>
                      <div className="p-5">
                        <div className="flex flex-wrap items-start justify-between gap-4">
                          <div>
                            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                              {business.status}
                            </p>
                            <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                              {business.businessName}
                            </h2>
                          </div>
                          <span className="rounded-full bg-[var(--color-surface-panel)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-primary)]">
                            {business.category}
                          </span>
                        </div>
                        <div className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {business.city ? <p>{business.city}</p> : null}
                          <p>Points balance: {business.pointsBalance}</p>
                          <p>Lifetime points: {business.lifetimePoints}</p>
                          {business.lastAccrualAtUtc ? (
                            <p>Last accrual: {formatDateTime(business.lastAccrualAtUtc, culture)}</p>
                          ) : null}
                        </div>
                        <div className="mt-5">
                          <Link
                            href={`/loyalty/${business.businessId}`}
                            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                          >
                            Open place details
                          </Link>
                        </div>
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  No joined loyalty places were returned for this page.
                </p>
              )}
            </div>

            <div className="grid gap-5 md:grid-cols-2">
              {overview.accounts.map((account) => (
                <article key={account.loyaltyAccountId} className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{account.status}</p>
                  <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">{account.businessName}</h2>
                  <div className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                    <p>Points balance: {account.pointsBalance}</p>
                    <p>Lifetime points: {account.lifetimePoints}</p>
                    {account.nextRewardTitle ? <p>Next reward: {account.nextRewardTitle}</p> : null}
                    {typeof account.pointsToNextReward === "number" ? <p>Points to next reward: {account.pointsToNextReward}</p> : null}
                    {account.lastAccrualAtUtc ? <p>Last accrual: {formatDateTime(account.lastAccrualAtUtc, culture)}</p> : null}
                  </div>
                  <div className="mt-5">
                    <Link
                      href={`/loyalty/${account.businessId}`}
                      className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      Open business details
                    </Link>
                  </div>
                </article>
              ))}
            </div>

            {totalPages > 1 && (
              <div className="flex flex-wrap items-center gap-3">
                <Link
                  aria-disabled={currentPage <= 1}
                  href={buildLoyaltyHref(Math.max(1, currentPage - 1))}
                  className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
                >
                  Previous
                </Link>
                <p className="text-sm text-[var(--color-text-secondary)]">
                  Page {currentPage} of {totalPages}
                </p>
                <Link
                  aria-disabled={currentPage >= totalPages}
                  href={buildLoyaltyHref(Math.min(totalPages, currentPage + 1))}
                  className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
                >
                  Next
                </Link>
              </div>
            )}
          </>
        ) : hasMemberSession ? (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
            No loyalty overview could be loaded for the current member session.
          </div>
        ) : null}

        <LoyaltyDiscoverySection
          businesses={discoveryBusinesses}
          status={discoveryStatus}
          currentPage={discoveryCurrentPage}
          totalPages={discoveryTotalPages}
          query={discoveryQuery}
          city={discoveryCity}
          countryCode={discoveryCountryCode}
          category={discoveryCategory}
          latitude={discoveryLatitude}
          longitude={discoveryLongitude}
          radiusKm={discoveryRadiusKm}
          categoryKinds={discoveryCategories}
          title={
            hasMemberSession
              ? "Discover more loyalty businesses"
              : "Browse loyalty-ready businesses"
          }
          description={
            hasMemberSession
              ? "The joined-business list remains member-specific, but discovery now stays visible so the portal can also act as a growth surface for the next place the customer wants to join."
              : "This discovery list is public and contract-driven, so storefront visitors can inspect businesses and reward tiers before authenticating."
          }
        />

        {!hasMemberSession && (
          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                  Member-only surfaces
                </p>
                <p className="mt-2 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
                  Balances, joined-business dashboards, rewards history, promotions, orders, and invoices still require member sign-in.
                </p>
              </div>
              <div className="flex flex-wrap gap-3">
                <Link
                  href="/account/register"
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  Create account
                </Link>
                <Link
                  href="/account/sign-in?returnPath=%2Floyalty"
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  Sign in
                </Link>
              </div>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
