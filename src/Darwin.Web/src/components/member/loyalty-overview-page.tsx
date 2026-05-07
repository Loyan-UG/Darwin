import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCategorySummary } from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { LoyaltyDiscoverySection } from "@/components/member/loyalty-discovery-section";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import type { BusinessCategoryKind, BusinessSummary } from "@/features/businesses/types";
import { buildCmsPagePath, buildLoyaltyBusinessPath } from "@/lib/entity-paths";
import type {
  MyLoyaltyBusinessSummary,
  MyLoyaltyOverview,
} from "@/features/member-portal/types";
import { buildAppQueryPath, buildLocalizedAuthHref, localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";
import {
  formatResource,
  getMemberResource,
  resolveApiStatusLabel,
} from "@/localization";
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
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
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
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
}: LoyaltyOverviewPageProps) {
  const copy = getMemberResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy);
  const businessesStatusLabel = resolveApiStatusLabel(businessesStatus, copy);
  const discoveryStatusLabel = resolveApiStatusLabel(discoveryStatus, copy);
  const cmsPagesStatusLabel = resolveApiStatusLabel(cmsPagesStatus, copy);
  const categoriesStatusLabel = resolveApiStatusLabel(categoriesStatus, copy);
  const rewardReadyAccounts = (overview?.accounts ?? []).filter(
    (account) => typeof account.pointsToNextReward === "number",
  );
  const primaryRewardAccount = [...rewardReadyAccounts].sort((left, right) => {
    const leftPoints = left.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
    const rightPoints = right.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
    return leftPoints - rightPoints;
  })[0];
  const activeBusinessCount = businesses.filter((business) =>
    /active/i.test(business.status),
  ).length;

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
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

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
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
              status: statusLabel ?? status,
              businessesStatus: businessesStatusLabel ?? businessesStatus,
            })}
          />
        )}

        {hasMemberSession && (
          <MemberPortalNav culture={culture} activePath="/loyalty" />
        )}

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.loyaltyRouteSummaryTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.loyaltyRouteSummaryMessage, {
              status: statusLabel ?? status,
              businessesStatus: businessesStatusLabel ?? businessesStatus,
              visibleCount: businesses.length,
              currentPage,
              totalPages,
              discoveryStatus: discoveryStatusLabel ?? discoveryStatus,
            })}
          </p>
        </div>

        {hasMemberSession && overview ? (
          <>
            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.loyaltyReadinessTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.loyaltyReadinessMessage, {
                  activeCount: activeBusinessCount,
                  rewardCount: rewardReadyAccounts.length,
                })}
              </p>
              <div className="mt-5 grid gap-3 sm:grid-cols-3">
                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                    {copy.loyaltyReadinessJoinedLabel}
                  </p>
                  <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                    {businesses.length}
                  </p>
                </div>
                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                    {copy.loyaltyReadinessActiveLabel}
                  </p>
                  <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                    {activeBusinessCount}
                  </p>
                </div>
                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                    {copy.loyaltyReadinessRewardLabel}
                  </p>
                  <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                    {rewardReadyAccounts.length}
                  </p>
                </div>
              </div>
              <div className="mt-5 flex flex-wrap gap-3">
                {primaryRewardAccount ? (
                  <Link
            href={localizeHref(buildLoyaltyBusinessPath(primaryRewardAccount.businessId), culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.loyaltyReadinessPrimaryCta}
                  </Link>
                ) : null}
                <Link
                  href={localizeHref("/account", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.securityBackToDashboardCta}
                </Link>
              </div>
            </div>

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

            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.loyaltyStorefrontWindowTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.loyaltyStorefrontWindowMessage, {
                  cmsStatus: cmsPagesStatusLabel ?? cmsPagesStatus,
                  categoriesStatus: categoriesStatusLabel ?? categoriesStatus,
                  pageCount: cmsPages.length,
                  categoryCount: categories.length,
                })}
              </p>
              <div className="mt-5 grid gap-4 lg:grid-cols-2">
                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.loyaltyStorefrontCmsTitle}
                    </p>
                    <Link
                      href={localizeHref("/cms", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.loyaltyStorefrontCmsCta}
                    </Link>
                  </div>
                  {cmsPages.length > 0 ? (
                    <div className="mt-4 flex flex-col gap-3">
                      {cmsPages.map((page) => (
                        <Link
                          key={page.id}
                    href={localizeHref(buildCmsPagePath(page.slug), culture)}
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          <p className="font-semibold text-[var(--color-text-primary)]">
                            {page.title}
                          </p>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {page.metaDescription ?? copy.loyaltyStorefrontCmsFallbackDescription}
                          </p>
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.loyaltyStorefrontCmsEmptyMessage, {
                        status: cmsPagesStatusLabel ?? cmsPagesStatus,
                      })}
                    </p>
                  )}
                </div>

                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.loyaltyStorefrontCatalogTitle}
                    </p>
                    <Link
                      href={localizeHref("/catalog", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.loyaltyStorefrontCatalogCta}
                    </Link>
                  </div>
                  {categories.length > 0 ? (
                    <div className="mt-4 flex flex-col gap-3">
                      {categories.map((categoryItem) => (
                        <Link
                          key={categoryItem.id}
                          href={localizeHref(
                            buildAppQueryPath("/catalog", {
                              category: categoryItem.slug,
                            }),
                            culture,
                          )}
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          <p className="font-semibold text-[var(--color-text-primary)]">
                            {categoryItem.name}
                          </p>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {categoryItem.description ?? copy.loyaltyStorefrontCatalogFallbackDescription}
                          </p>
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.loyaltyStorefrontCatalogEmptyMessage, {
                        status: categoriesStatusLabel ?? categoriesStatus,
                      })}
                    </p>
                  )}
                </div>
              </div>
            </div>

            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
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
                    (() => {
                      const primaryImageUrl = toWebApiUrl(business.primaryImageUrl ?? "");
                      return (
                    <article
                      key={business.businessId}
                      className="overflow-hidden rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)]"
                    >
                      <div className="flex min-h-44 items-center justify-center bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-5">
                        {primaryImageUrl ? (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img
                            src={primaryImageUrl}
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
              href={localizeHref(buildLoyaltyBusinessPath(business.businessId), culture)}
                            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                          >
                            {copy.openPlaceDetailsCta}
                          </Link>
                        </div>
                      </div>
                    </article>
                      );
                    })()
                  ))}
                </div>
              ) : (
                <div className="mt-5 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] px-5 py-8 text-center">
                  <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.noJoinedLoyaltyPlacesMessage}
                  </p>
                  <div className="mt-5 flex flex-wrap justify-center gap-3">
                    <Link
                      href={localizeHref("/catalog", culture)}
                      className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                    >
                      {copy.memberCrossSurfaceCatalogCta}
                    </Link>
                    <Link
                      href={localizeHref("/account", culture)}
                      className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                    >
                      {copy.memberCrossSurfaceAccountCta}
                    </Link>
                  </div>
                </div>
              )}
            </div>

            <div className="grid gap-5 md:grid-cols-2">
              {overview.accounts.map((account) => (
                <article
                  key={account.loyaltyAccountId}
                  className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
                >
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {localizeLoyaltyAccountStatus(account.status, copy)}
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
                    href={localizeHref(buildLoyaltyBusinessPath(account.businessId), culture)}
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
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.noLoyaltyOverviewMessage}
            </p>
            <div className="mt-5 flex flex-wrap justify-center gap-3">
              <Link
                href={localizeHref("/account", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.memberCrossSurfaceAccountCta}
              </Link>
              <Link
                href={localizeHref("/catalog", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.memberCrossSurfaceCatalogCta}
              </Link>
            </div>
          </div>
        ) : null}

        {!hasMemberSession && (
          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
              {copy.loyaltyPublicOverviewStorefrontWindowTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.loyaltyPublicOverviewStorefrontWindowMessage, {
                cmsStatus: cmsPagesStatusLabel ?? cmsPagesStatus,
                categoriesStatus: categoriesStatusLabel ?? categoriesStatus,
                pageCount: cmsPages.length,
                categoryCount: categories.length,
              })}
            </p>
            <div className="mt-5 grid gap-4 lg:grid-cols-2">
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <div className="flex items-center justify-between gap-3">
                  <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                    {copy.loyaltyPublicOverviewStorefrontCmsTitle}
                  </p>
                  <Link
                    href={localizeHref("/cms", culture)}
                    className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                  >
                    {copy.loyaltyPublicOverviewStorefrontCmsCta}
                  </Link>
                </div>
                {cmsPages.length > 0 ? (
                  <div className="mt-4 flex flex-col gap-3">
                    {cmsPages.map((page) => (
                      <Link
                        key={page.id}
                href={localizeHref(buildCmsPagePath(page.slug), culture)}
                        className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                      >
                        <p className="font-semibold text-[var(--color-text-primary)]">
                          {page.title}
                        </p>
                        <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {page.metaDescription ?? copy.loyaltyPublicOverviewStorefrontCmsFallbackDescription}
                        </p>
                      </Link>
                    ))}
                  </div>
                ) : (
                  <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {formatResource(copy.loyaltyPublicOverviewStorefrontCmsEmptyMessage, {
                      status: cmsPagesStatusLabel ?? cmsPagesStatus,
                    })}
                  </p>
                )}
              </div>

              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <div className="flex items-center justify-between gap-3">
                  <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                    {copy.loyaltyPublicOverviewStorefrontCatalogTitle}
                  </p>
                  <Link
                    href={localizeHref("/catalog", culture)}
                    className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                  >
                    {copy.loyaltyPublicOverviewStorefrontCatalogCta}
                  </Link>
                </div>
                {categories.length > 0 ? (
                  <div className="mt-4 flex flex-col gap-3">
                    {categories.map((categoryItem) => (
                      <Link
                        key={categoryItem.id}
                        href={localizeHref(
                          buildAppQueryPath("/catalog", {
                            category: categoryItem.slug,
                          }),
                          culture,
                        )}
                        className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                      >
                        <p className="font-semibold text-[var(--color-text-primary)]">
                          {categoryItem.name}
                        </p>
                        <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {categoryItem.description ?? copy.loyaltyPublicOverviewStorefrontCatalogFallbackDescription}
                        </p>
                      </Link>
                    ))}
                  </div>
                ) : (
                  <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {formatResource(copy.loyaltyPublicOverviewStorefrontCatalogEmptyMessage, {
                      status: categoriesStatusLabel ?? categoriesStatus,
                    })}
                  </p>
                )}
              </div>
            </div>
          </div>
        )}

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

        {!hasMemberSession && (
          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
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
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.loyaltyCreateAccountCta}
                </Link>
                <Link
                  href={buildLocalizedAuthHref("/account/sign-in", "/loyalty", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.signIn}
                </Link>
              </div>
            </div>
          </div>
        )}

        <MemberCrossSurfaceRail
          culture={culture}
          includeAccount
          includeOrders={hasMemberSession}
          includeInvoices={hasMemberSession}
          includeLoyalty={false}
        />
      </div>
    </section>
  );
}
