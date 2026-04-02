import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { LoyaltyDiscoverySection } from "@/components/member/loyalty-discovery-section";
import type { BusinessCategoryKind, BusinessSummary } from "@/features/businesses/types";
import type {
  MyLoyaltyBusinessSummary,
  MyLoyaltyOverview,
} from "@/features/member-portal/types";
import { localizeHref } from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";
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
  const copy = getMemberResource(culture);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.memberLoyaltyEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {hasMemberSession
              ? copy.loyaltyOverviewTitleSignedIn
              : copy.loyaltyOverviewTitleSignedOut}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {hasMemberSession
              ? copy.loyaltyOverviewDescriptionSignedIn
              : copy.loyaltyOverviewDescriptionSignedOut}
          </p>
        </div>

        {(status !== "ok" || businessesStatus !== "ok") && hasMemberSession && (
          <StatusBanner
            tone="warning"
            title={copy.loyaltyOverviewWarningsTitle}
            message={formatResource(copy.loyaltyOverviewWarningsMessage, {
              status,
              businessesStatus,
            })}
          />
        )}

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
                  className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
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

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                    {copy.myLoyaltyPlacesTitle}
                  </p>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.myLoyaltyPlacesDescription}
                  </p>
                </div>
                <p className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]">
                  {formatResource(copy.visibleOnPageLabel, { count: businesses.length })}
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
                              {copy.noBusinessMedia}
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
                          <p>
                            {copy.pointsBalanceLabel}: {business.pointsBalance}
                          </p>
                          <p>
                            {copy.lifetimePointsLabel}: {business.lifetimePoints}
                          </p>
                          {business.lastAccrualAtUtc ? (
                            <p>
                              {formatResource(copy.lastAccrualLabel, {
                                value: formatDateTime(business.lastAccrualAtUtc, culture),
                              })}
                            </p>
                          ) : null}
                        </div>
                        <div className="mt-5">
                          <Link
                            href={localizeHref(`/loyalty/${business.businessId}`, culture)}
                            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                          >
                            {copy.openPlaceDetailsCta}
                          </Link>
                        </div>
                      </div>
                    </article>
                  ))}
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.noJoinedLoyaltyPlacesMessage}
                </p>
              )}
            </div>

            <div className="grid gap-5 md:grid-cols-2">
              {overview.accounts.map((account) => (
                <article
                  key={account.loyaltyAccountId}
                  className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
                >
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {account.status}
                  </p>
                  <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                    {account.businessName}
                  </h2>
                  <div className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                    <p>
                      {copy.pointsBalanceLabel}: {account.pointsBalance}
                    </p>
                    <p>
                      {copy.lifetimePointsLabel}: {account.lifetimePoints}
                    </p>
                    {account.nextRewardTitle ? (
                      <p>
                        {formatResource(copy.nextRewardLabel, {
                          value: account.nextRewardTitle,
                        })}
                      </p>
                    ) : null}
                    {typeof account.pointsToNextReward === "number" ? (
                      <p>
                        {formatResource(copy.pointsToNextRewardLabel, {
                          value: account.pointsToNextReward,
                        })}
                      </p>
                    ) : null}
                    {account.lastAccrualAtUtc ? (
                      <p>
                        {formatResource(copy.lastAccrualLabel, {
                          value: formatDateTime(account.lastAccrualAtUtc, culture),
                        })}
                      </p>
                    ) : null}
                  </div>
                  <div className="mt-5">
                    <Link
                      href={localizeHref(`/loyalty/${account.businessId}`, culture)}
                      className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      {copy.openBusinessDetailsCta}
                    </Link>
                  </div>
                </article>
              ))}
            </div>

            {totalPages > 1 && (
              <div className="flex flex-wrap items-center gap-3">
                <Link
                  aria-disabled={currentPage <= 1}
                  href={localizeHref(buildLoyaltyHref(Math.max(1, currentPage - 1)), culture)}
                  className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
                >
                  {copy.previous}
                </Link>
                <p className="text-sm text-[var(--color-text-secondary)]">
                  {formatResource(copy.pageLabel, { currentPage, totalPages })}
                </p>
                <Link
                  aria-disabled={currentPage >= totalPages}
                  href={localizeHref(buildLoyaltyHref(Math.min(totalPages, currentPage + 1)), culture)}
                  className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
                >
                  {copy.next}
                </Link>
              </div>
            )}
          </>
        ) : hasMemberSession ? (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.noLoyaltyOverviewMessage}
          </div>
        ) : null}

        <LoyaltyDiscoverySection
          culture={culture}
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
              ? copy.discoveryTitleSignedIn
              : copy.discoveryTitleSignedOut
          }
          description={
            hasMemberSession
              ? copy.discoveryDescriptionSignedIn
              : copy.discoveryDescriptionSignedOut
          }
        />

        {!hasMemberSession && (
          <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
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
                  href={localizeHref(
                    `/account/register?returnPath=${encodeURIComponent(localizeHref("/loyalty", culture))}`,
                    culture,
                  )}
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.loyaltyCreateAccountCta}
                </Link>
                <Link
                  href={localizeHref("/account/sign-in?returnPath=%2Floyalty", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.signIn}
                </Link>
              </div>
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
