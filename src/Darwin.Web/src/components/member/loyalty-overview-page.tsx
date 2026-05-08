import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { LoyaltyDiscoverySection } from "@/components/member/loyalty-discovery-section";
import type { BusinessCategoryKind, BusinessSummary } from "@/features/businesses/types";
import type {
  MyLoyaltyBusinessSummary,
  MyLoyaltyOverview,
} from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  resolveApiStatusLabel,
} from "@/localization";
import { buildLoyaltyBusinessPath } from "@/lib/entity-paths";
import { formatDateTime } from "@/lib/formatting";
import { buildLocalizedAuthHref, localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";

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

function localizeLoyaltyAccountStatus(
  status: string | null | undefined,
  copy: ReturnType<typeof getMemberResource>,
) {
  switch (status?.toLowerCase()) {
    case "active":
      return copy.loyaltyAccountStatusActiveLabel;
    case "suspended":
      return copy.loyaltyAccountStatusSuspendedLabel;
    case "closed":
      return copy.loyaltyAccountStatusClosedLabel;
    default:
      return status ?? copy.activityFallback;
  }
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
  const copy = getMemberResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy);
  const businessesStatusLabel = resolveApiStatusLabel(businessesStatus, copy);
  const discoveryStatusLabel = resolveApiStatusLabel(discoveryStatus, copy);
  const rewardFocus = [...(overview?.accounts ?? [])]
    .sort((left, right) => {
      const leftPoints = left.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
      const rightPoints = right.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
      return leftPoints - rightPoints;
    })
    .slice(0, 3);

  return (
    <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <nav
          aria-label={copy.memberBreadcrumbLabel}
          className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
        >
          <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.memberBreadcrumbHome}
          </Link>
          {hasMemberSession ? (
            <>
              <span>/</span>
              <Link href={localizeHref("/account", culture)} className="transition hover:text-[var(--color-brand)]">
                {copy.memberBreadcrumbAccount}
              </Link>
            </>
          ) : null}
          <span>/</span>
          <span className="font-medium text-[var(--color-text-primary)]">
            {copy.memberBreadcrumbLoyalty}
          </span>
        </nav>

        <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.memberLoyaltyEyebrow}
          </p>
          <div className="mt-4 flex flex-wrap items-end justify-between gap-6">
            <div>
              <h1 className="font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {hasMemberSession
                  ? copy.loyaltyOverviewTitleSignedIn
                  : copy.loyaltyOverviewTitleSignedOut}
              </h1>
              <p className="mt-4 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)]">
                {hasMemberSession
                  ? copy.loyaltyOverviewDescriptionSignedIn
                  : copy.loyaltyOverviewDescriptionSignedOut}
              </p>
            </div>
            {!hasMemberSession ? (
              <div className="flex flex-wrap gap-3">
                <Link
                  href={buildLocalizedAuthHref("/account/register", "/loyalty", culture)}
                  className="rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.loyaltyCreateAccountCta}
                </Link>
                <Link
                  href={buildLocalizedAuthHref("/account/sign-in", "/loyalty", culture)}
                  className="rounded-full border border-[var(--color-border-soft)] bg-white/70 px-5 py-3 text-sm font-semibold transition hover:bg-white"
                >
                  {copy.signIn}
                </Link>
              </div>
            ) : null}
          </div>
        </header>

        {hasMemberSession && (status !== "ok" || businessesStatus !== "ok") ? (
          <StatusBanner
            tone="warning"
            title={copy.loyaltyOverviewWarningsTitle}
            message={formatResource(copy.loyaltyOverviewWarningsMessage, {
              status: statusLabel ?? status,
              businessesStatus: businessesStatusLabel ?? businessesStatus,
            })}
          />
        ) : null}

        {hasMemberSession ? (
          <MemberPortalNav culture={culture} activePath="/loyalty" />
        ) : null}

        {hasMemberSession && overview ? (
          <>
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
              {[
                { label: copy.totalAccountsLabel, value: String(overview.totalAccounts) },
                { label: copy.activeAccountsLabel, value: String(overview.activeAccounts) },
                { label: copy.pointsBalanceLabel, value: String(overview.totalPointsBalance) },
                { label: copy.lifetimePointsLabel, value: String(overview.totalLifetimePoints) },
              ].map((item) => (
                <article
                  key={item.label}
                  className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
                >
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {item.label}
                  </p>
                  <p className="mt-4 text-3xl font-semibold text-[var(--color-text-primary)]">
                    {item.value}
                  </p>
                </article>
              ))}
            </div>

            <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                    {copy.myLoyaltyPlacesTitle}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.myLoyaltyPlacesDescription}
                  </p>
                </div>
              </div>

              {businesses.length > 0 ? (
                <div className="mt-5 grid gap-5 md:grid-cols-2">
                  {businesses.map((business) => {
                    const primaryImageUrl = toWebApiUrl(business.primaryImageUrl ?? "");

                    return (
                      <article
                        key={business.businessId}
                        className="overflow-hidden rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)]"
                      >
                        <div className="flex min-h-36 items-center justify-center bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-5">
                          {primaryImageUrl ? (
                            // eslint-disable-next-line @next/next/no-img-element
                            <img
                              src={primaryImageUrl}
                              alt={business.businessName}
                              className="max-h-28 w-auto object-contain"
                            />
                          ) : (
                            <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-text-muted)]">
                              {business.category}
                            </p>
                          )}
                        </div>
                        <div className="p-5">
                          <div className="flex flex-wrap items-start justify-between gap-4">
                            <div>
                              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                                {localizeLoyaltyAccountStatus(business.status, copy)}
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
                            <p>{copy.pointsBalanceLabel}: {business.pointsBalance}</p>
                            <p>{copy.lifetimePointsLabel}: {business.lifetimePoints}</p>
                            {business.lastAccrualAtUtc ? (
                              <p>
                                {formatResource(copy.lastAccrualLabel, {
                                  value: formatDateTime(business.lastAccrualAtUtc, culture),
                                })}
                              </p>
                            ) : null}
                          </div>
                          <Link
                            href={localizeHref(buildLoyaltyBusinessPath(business.businessId), culture)}
                            className="mt-5 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold transition hover:bg-[var(--color-surface-panel)]"
                          >
                            {copy.openPlaceDetailsCta}
                          </Link>
                        </div>
                      </article>
                    );
                  })}
                </div>
              ) : (
                <div className="mt-5 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] px-5 py-8 text-center">
                  <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.noJoinedLoyaltyPlacesMessage}
                  </p>
                </div>
              )}
            </section>

            {rewardFocus.length > 0 ? (
              <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                  {copy.dashboardRewardFocusTitle}
                </p>
                <div className="mt-5 grid gap-4 md:grid-cols-3">
                  {rewardFocus.map((account) => (
                    <Link
                      key={account.loyaltyAccountId}
                      href={localizeHref(buildLoyaltyBusinessPath(account.businessId), culture)}
                      className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 transition hover:bg-[var(--color-surface-panel)]"
                    >
                      <p className="font-semibold text-[var(--color-text-primary)]">
                        {account.businessName}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {formatResource(copy.nextRewardLabel, {
                          value: account.nextRewardTitle ?? copy.noNextRewardPublished,
                        })}
                      </p>
                      <p className="mt-2 text-sm font-semibold text-[var(--color-text-primary)]">
                        {formatResource(copy.pointsToNextRewardLabel, {
                          value: account.pointsToNextReward?.toString() ?? copy.unavailable,
                        })}
                      </p>
                    </Link>
                  ))}
                </div>
              </section>
            ) : null}

            {totalPages > 1 ? (
              <div className="flex flex-wrap items-center gap-3">
                <Link
                  aria-disabled={currentPage <= 1}
                  href={localizeHref(buildLoyaltyHref(Math.max(1, currentPage - 1)), culture)}
                  className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
                >
                  {copy.previous}
                </Link>
                <p className="text-sm text-[var(--color-text-secondary)]">
                  {formatResource(copy.pageLabel, { currentPage, totalPages })}
                </p>
                <Link
                  aria-disabled={currentPage >= totalPages}
                  href={localizeHref(buildLoyaltyHref(Math.min(totalPages, currentPage + 1)), culture)}
                  className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
                >
                  {copy.next}
                </Link>
              </div>
            ) : null}
          </>
        ) : hasMemberSession ? (
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.noLoyaltyOverviewMessage}
            </p>
          </div>
        ) : null}

        <LoyaltyDiscoverySection
          culture={culture}
          businesses={discoveryBusinesses}
          status={discoveryStatusLabel ?? discoveryStatus}
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
              ? copy.discoveryTitleSignedIn
              : copy.discoveryTitleSignedOut
          }
          description={
            hasMemberSession
              ? copy.discoveryDescriptionSignedIn
              : copy.discoveryDescriptionSignedOut
          }
        />

        {!hasMemberSession ? (
          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                  {copy.memberOnlySurfacesTitle}
                </p>
                <p className="mt-2 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.memberOnlySurfacesDescription}
                </p>
              </div>
              <div className="flex flex-wrap gap-3">
                <Link
                  href={buildLocalizedAuthHref("/account/register", "/loyalty", culture)}
                  className="rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.loyaltyCreateAccountCta}
                </Link>
                <Link
                  href={buildLocalizedAuthHref("/account/sign-in", "/loyalty", culture)}
                  className="rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.signIn}
                </Link>
              </div>
            </div>
          </section>
        ) : null}
      </div>
    </section>
  );
}
