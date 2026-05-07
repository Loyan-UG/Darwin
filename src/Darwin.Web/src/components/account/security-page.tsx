import Link from "next/link";
import { AccountContentCompositionWindow } from "@/components/account/account-content-composition-window";
import { AccountStorefrontWindow } from "@/components/account/account-storefront-window";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import { SurfaceSectionNav } from "@/components/layout/surface-section-nav";
import { changeMemberPasswordAction } from "@/features/member-portal/actions";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import type { MemberCustomerProfile } from "@/features/member-portal/types";
import type { MemberSession } from "@/features/member-session/types";
import {
  formatResource,
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveApiStatusLabel,
  resolveStatusMappedMessage,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { formatDateTime } from "@/lib/formatting";
import { localizeHref } from "@/lib/locale-routing";
import { parseUtcTimestamp } from "@/lib/time";

type SecurityPageProps = {
  culture: string;
  session: MemberSession;
  profile: MemberCustomerProfile | null;
  profileStatus: string;
  securityStatus?: string;
  securityError?: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
};

export function SecurityPage({
  culture,
  session,
  profile,
  profileStatus,
  securityStatus,
  securityError,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
}: SecurityPageProps) {
  const copy = getMemberResource(culture);
  const localizedProfileStatus = resolveApiStatusLabel(profileStatus, copy) ?? profileStatus;
  const resolvedSecurityError = resolveLocalizedQueryMessage(securityError, copy);
  const profileWarningMessage = resolveStatusMappedMessage(profileStatus, copy, {
    "not-found": "memberResourceNotFoundMessage",
    "network-error": "memberApiNetworkErrorMessage",
    "http-error": "memberApiHttpErrorMessage",
    "invalid-payload": "memberApiInvalidPayloadMessage",
    unauthorized: "memberSessionUnauthorizedMessage",
    unauthenticated: "memberSessionRequiredMessage",
  });
  const hasValidSessionExpiry = parseUtcTimestamp(session.accessTokenExpiresAtUtc) !== null;
  const securityState =
    !profile?.phoneNumberConfirmed && !hasValidSessionExpiry
      ? copy.dashboardSecurityStateNeedsAttention
      : !profile?.phoneNumberConfirmed
        ? copy.dashboardSecurityStateVerifyPhone
        : !hasValidSessionExpiry
          ? copy.dashboardSecurityStateRefreshSoon
          : copy.dashboardSecurityStateHealthy;
  const sectionLinks = [
    { href: "#security-form", label: copy.securityEditTitle },
    { href: "#security-state", label: copy.securityCurrentStateTitle },
    { href: "#security-summary", label: copy.memberRouteSummaryTitle },
    { href: "#security-composition", label: copy.accountCompositionJourneySecurityTitle },
  ];

  return (
    <section className="mx-auto flex w-full max-w-[1320px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-6">
        <div className="overflow-hidden rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_42%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8 sm:py-10">
          <div className="grid gap-8 lg:grid-cols-[minmax(0,1.2fr)_340px] lg:items-end">
            <div>
              <nav
                aria-label={copy.memberBreadcrumbLabel}
                className="flex flex-wrap items-center gap-2 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-muted)]"
              >
                <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-text-primary)]">
                  {copy.memberBreadcrumbHome}
                </Link>
                <span>/</span>
                <Link href={localizeHref("/account", culture)} className="transition hover:text-[var(--color-text-primary)]">
                  {copy.memberBreadcrumbAccount}
                </Link>
                <span>/</span>
                <span className="text-[var(--color-text-primary)]">{copy.securityRouteLabel}</span>
              </nav>
              <p className="mt-4 text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                {copy.securityEditEyebrow}
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {copy.securityEditTitle}
              </h1>
              <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {copy.securityEditDescription}
              </p>
              <div className="mt-6 flex flex-wrap gap-3">
                <Link
                  href={localizeHref("/account/profile", culture)}
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {profile?.phoneNumberConfirmed
                    ? copy.securityProfileReviewCta
                    : copy.dashboardSecurityVerifyPhoneCta}
                </Link>
                <Link
                  href={localizeHref("/account", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] bg-white/85 px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-white"
                >
                  {copy.securityBackToDashboardCta}
                </Link>
              </div>
            </div>
            <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-1">
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.labelPhoneVerified}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {profile?.phoneNumberConfirmed ? copy.yes : copy.no}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.dashboardSecurityStateLabel}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {securityState}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-[linear-gradient(135deg,rgba(57,116,47,0.94),rgba(255,145,77,0.92))] px-5 py-4 text-white shadow-[0_20px_40px_-28px_rgba(58,92,35,0.55)]">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/78">
                  {copy.labelAccessTokenExpiry}
                </p>
                <p className="mt-2 text-sm font-semibold text-white">
                  {hasValidSessionExpiry
                    ? formatDateTime(session.accessTokenExpiresAtUtc, culture)
                    : copy.dashboardSecuritySessionUnavailable}
                </p>
              </article>
            </div>
          </div>
        </div>

        <SurfaceSectionNav items={sectionLinks} />
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <form
          action={changeMemberPasswordAction}
          id="security-form"
          className="scroll-mt-28 rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#f7fbef_100%)] px-6 py-8 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            {copy.securityRouteLabel}
          </p>
          <h2 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)] sm:text-3xl">
            {copy.securityEditTitle}
          </h2>

          {profileStatus !== "ok" && (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title={copy.securityProfileWarningTitle}
                message={
                  profileWarningMessage ??
                  formatDateTime(session.accessTokenExpiresAtUtc, culture)
                }
              />
            </div>
          )}

          {matchesLocalizedQueryMessageKey(
            securityStatus,
            "securityUpdatedMessage",
            "saved",
          ) && (
            <div className="mt-6">
              <StatusBanner
                title={copy.securityUpdatedTitle}
                message={copy.securityUpdatedMessage}
              />
            </div>
          )}

          {resolvedSecurityError && (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title={copy.securityNeedsAttentionTitle}
                message={resolvedSecurityError}
              />
            </div>
          )}

          <div className="mt-8 grid gap-4">
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.securityCurrentPasswordLabel}
              <input
                name="currentPassword"
                type="password"
                required
                minLength={8}
                autoComplete="current-password"
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
              />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.securityNewPasswordLabel}
              <input
                name="newPassword"
                type="password"
                required
                minLength={8}
                autoComplete="new-password"
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
              />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.securityConfirmPasswordLabel}
              <input
                name="confirmPassword"
                type="password"
                required
                minLength={8}
                autoComplete="new-password"
                className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
              />
            </label>
          </div>

          <button
            type="submit"
            className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
          >
            {copy.securitySaveCta}
          </button>

          <div id="security-state" className="mt-8 scroll-mt-28 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-5 py-5">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
              {copy.securityCurrentStateTitle}
            </p>
            <div className="mt-4 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)] sm:grid-cols-2">
              <p>
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.labelPhoneVerified}
                </span>{" "}
                {profile?.phoneNumberConfirmed ? copy.yes : copy.no}
              </p>
              <p>
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.dashboardSecurityStateLabel}
                </span>{" "}
                {securityState}
              </p>
              <p>
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.labelAccessTokenExpiry}
                </span>{" "}
                {hasValidSessionExpiry
                  ? formatDateTime(session.accessTokenExpiresAtUtc, culture)
                  : copy.dashboardSecuritySessionUnavailable}
              </p>
              <p>
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.labelPhone}
                </span>{" "}
                {profile?.phoneE164 ?? copy.unavailable}
              </p>
            </div>
            <div className="mt-5 flex flex-wrap gap-3">
              <Link
                href={localizeHref("/account/profile", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {profile?.phoneNumberConfirmed
                  ? copy.securityProfileReviewCta
                  : copy.dashboardSecurityVerifyPhoneCta}
              </Link>
              <Link
                href={localizeHref("/account", culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
              >
                {copy.securityBackToDashboardCta}
              </Link>
            </div>
          </div>
        </form>

        <div className="flex flex-col gap-6">
          <MemberPortalNav culture={culture} activePath="/account/security" />

          <aside id="security-summary" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.memberRouteSummaryTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.securityRouteSummaryMessage}
            </p>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link href={localizeHref("/account/profile", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberRouteSummaryProfileCta}
              </Link>
              <Link href={localizeHref("/account/preferences", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberRouteSummaryPreferencesCta}
              </Link>
            </div>
          </aside>

          <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.boundaryTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.securityBoundaryMessage}
            </p>
          </aside>

          <MemberCrossSurfaceRail
            culture={culture}
            includeAccount={false}
            includeOrders
            includeLoyalty={false}
          />

          <div id="security-composition" className="scroll-mt-28">
            <AccountContentCompositionWindow
              culture={culture}
              routeCard={{
                label: copy.accountCompositionJourneyCurrentLabel,
                title: copy.accountCompositionJourneySecurityTitle,
                description: formatResource(copy.accountCompositionJourneySecurityDescription, {
                  profileStatus: localizedProfileStatus,
                }),
                href: "/account/security",
                ctaLabel: copy.accountCompositionJourneyCurrentCta,
              }}
              nextCard={{
                label: copy.accountCompositionJourneyNextLabel,
                title: copy.accountCompositionJourneyProfileTitle,
                description: copy.accountCompositionJourneySecurityNextDescription,
                href: "/account/profile",
                ctaLabel: copy.accountCompositionJourneySecurityNextCta,
              }}
              routeMapItems={[
                {
                  label: copy.accountCompositionRouteMapSecurityLabel,
                  title: copy.accountCompositionRouteMapSecurityTitle,
                  description: copy.accountCompositionRouteMapSecurityDescription,
                  href: "/account/security",
                  ctaLabel: copy.accountCompositionRouteMapSecurityCta,
                },
                {
                  label: copy.accountCompositionRouteMapNextLabel,
                  title: copy.accountCompositionRouteMapProfileTitle,
                  description: formatResource(copy.accountCompositionRouteMapSecurityNextDescription, {
                    profileStatus: localizedProfileStatus,
                  }),
                  href: "/account/profile",
                  ctaLabel: copy.accountCompositionRouteMapProfileCta,
                },
              ]}
              cmsPages={cmsPages}
              categories={categories}
              products={products}
            />
          </div>

          <AccountStorefrontWindow
            culture={culture}
            cmsPages={cmsPages}
            cmsPagesStatus={cmsPagesStatus}
            categories={categories}
            categoriesStatus={categoriesStatus}
            products={products}
            productsStatus={productsStatus}
          />
        </div>
      </div>
      </div>
    </section>
  );
}
