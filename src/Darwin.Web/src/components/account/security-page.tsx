import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { changeMemberPasswordAction } from "@/features/member-portal/actions";
import type { MemberCustomerProfile } from "@/features/member-portal/types";
import type { MemberSession } from "@/features/member-session/types";
import {
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveLocalizedQueryMessage,
  resolveStatusMappedMessage,
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
};

export function SecurityPage({
  culture,
  session,
  profile,
  profileStatus,
  securityStatus,
  securityError,
}: SecurityPageProps) {
  const copy = getMemberResource(culture);
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
              {copy.securityRouteLabel}
            </span>
          </nav>

          <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {copy.securityEditEyebrow}
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {copy.securityEditTitle}
            </h1>
            <p className="mt-4 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.memberSecurityIntroMessage}
            </p>
          </header>

          {profileStatus !== "ok" ? (
            <StatusBanner
              tone="warning"
              title={copy.securityProfileWarningTitle}
              message={profileWarningMessage ?? copy.memberSessionRequiredMessage}
            />
          ) : null}

          {matchesLocalizedQueryMessageKey(securityStatus, "securityUpdatedMessage", "saved") ? (
            <StatusBanner
              title={copy.securityUpdatedTitle}
              message={copy.securityUpdatedMessage}
            />
          ) : null}

          {resolvedSecurityError ? (
            <StatusBanner
              tone="warning"
              title={copy.securityNeedsAttentionTitle}
              message={resolvedSecurityError}
            />
          ) : null}

          <form
            action={changeMemberPasswordAction}
            className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)] sm:p-8"
          >
            <div className="grid gap-4">
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
          </form>

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)] sm:p-8">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.memberSecurityStatusTitle}
            </p>
            <div className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)] sm:grid-cols-2">
              <p className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.labelPhoneVerified}
                </span>{" "}
                {profile?.phoneNumberConfirmed ? copy.yes : copy.no}
              </p>
              <p className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.dashboardSecurityStateLabel}
                </span>{" "}
                {securityState}
              </p>
              <p className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.labelAccessTokenExpiry}
                </span>{" "}
                {hasValidSessionExpiry
                  ? formatDateTime(session.accessTokenExpiresAtUtc, culture)
                  : copy.dashboardSecuritySessionUnavailable}
              </p>
              <p className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.labelPhone}
                </span>{" "}
                {profile?.phoneE164 ?? copy.unavailable}
              </p>
            </div>
          </section>
        </div>

        <aside className="flex flex-col gap-5">
          <MemberPortalNav culture={culture} activePath="/account/security" />
          <Link
            href={localizeHref("/account/profile", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {profile?.phoneNumberConfirmed
              ? copy.memberOpenProfileCta
              : copy.dashboardSecurityVerifyPhoneCta}
          </Link>
          <Link
            href={localizeHref("/account", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.securityBackToDashboardCta}
          </Link>
        </aside>
      </div>
    </section>
  );
}
