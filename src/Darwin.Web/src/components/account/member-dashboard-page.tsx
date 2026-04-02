import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { signOutMemberAction } from "@/features/member-session/actions";
import type { MemberSession } from "@/features/member-session/types";
import type {
  LinkedCustomerContext,
  MemberCustomerProfile,
  MemberPreferences,
} from "@/features/member-portal/types";
import { localizeHref } from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";
import { formatDateTime } from "@/lib/formatting";

type MemberDashboardPageProps = {
  culture: string;
  session: MemberSession;
  profile: MemberCustomerProfile | null;
  profileStatus: string;
  preferences: MemberPreferences | null;
  preferencesStatus: string;
  customerContext: LinkedCustomerContext | null;
  customerContextStatus: string;
};

export function MemberDashboardPage({
  culture,
  session,
  profile,
  profileStatus,
  preferences,
  preferencesStatus,
  customerContext,
  customerContextStatus,
}: MemberDashboardPageProps) {
  const copy = getMemberResource(culture);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <div className="flex flex-wrap items-start justify-between gap-5">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                {copy.memberDashboardEyebrow}
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {formatResource(copy.memberDashboardTitle, { email: session.email })}
              </h1>
              <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {copy.memberDashboardDescription}
              </p>
            </div>
            <form action={signOutMemberAction}>
              <button
                type="submit"
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.signOut}
              </button>
            </form>
          </div>
        </div>

        {(profileStatus !== "ok" || preferencesStatus !== "ok" || customerContextStatus !== "ok") && (
          <StatusBanner
            tone="warning"
            title={copy.memberDataWarningsTitle}
            message={formatResource(copy.memberDataWarningsMessage, {
              profileStatus,
              preferencesStatus,
              customerContextStatus,
            })}
          />
        )}

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
          <div className="grid gap-6">
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.profileSnapshotTitle}
              </p>
              {profile ? (
                <div className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)] sm:grid-cols-2">
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelEmail}</span> {profile.email ?? session.email}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelName}</span> {[profile.firstName, profile.lastName].filter(Boolean).join(" ") || copy.unavailable}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelPhone}</span> {profile.phoneE164 ?? copy.unavailable}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelPhoneVerified}</span> {profile.phoneNumberConfirmed ? copy.yes : copy.no}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelLocale}</span> {profile.locale ?? copy.unavailable}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelTimezone}</span> {profile.timezone ?? copy.unavailable}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelCurrency}</span> {profile.currency ?? copy.unavailable}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelAccessTokenExpiry}</span> {formatDateTime(session.accessTokenExpiresAtUtc, culture)}</p>
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.profileUnavailable}
                </p>
              )}
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.preferencesSnapshotTitle}
              </p>
              {preferences ? (
                <div className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)] sm:grid-cols-2">
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelMarketingConsent}</span> {preferences.marketingConsent ? copy.granted : copy.notGranted}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelEmailMarketing}</span> {preferences.allowEmailMarketing ? copy.allowed : copy.blocked}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelSmsMarketing}</span> {preferences.allowSmsMarketing ? copy.allowed : copy.blocked}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelWhatsAppMarketing}</span> {preferences.allowWhatsAppMarketing ? copy.allowed : copy.blocked}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelPromotionalPush}</span> {preferences.allowPromotionalPushNotifications ? copy.allowed : copy.blocked}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">{copy.labelOptionalAnalytics}</span> {preferences.allowOptionalAnalyticsTracking ? copy.allowed : copy.blocked}</p>
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.preferencesUnavailable}
                </p>
              )}
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.portalRoutesTitle}
              </p>
              <div className="mt-5 flex flex-col gap-2">
                <Link href={localizeHref("/account/profile", culture)} className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">{copy.profileRouteLabel}</Link>
                <Link href={localizeHref("/account/preferences", culture)} className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">{copy.preferencesRouteLabel}</Link>
                <Link href={localizeHref("/account/addresses", culture)} className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">{copy.addressesRouteLabel}</Link>
                <Link href={localizeHref("/orders", culture)} className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">{copy.ordersRouteLabel}</Link>
                <Link href={localizeHref("/invoices", culture)} className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">{copy.invoicesRouteLabel}</Link>
                <Link href={localizeHref("/loyalty", culture)} className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">{copy.loyaltyRouteLabel}</Link>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.crmContextTitle}
              </p>
              {customerContext ? (
                <div className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p className="font-semibold text-[var(--color-text-primary)]">{customerContext.displayName}</p>
                  <p>{customerContext.email}</p>
                  {customerContext.companyName ? <p>{customerContext.companyName}</p> : null}
                  <p>{formatResource(copy.crmInteractionsLabel, { count: customerContext.interactionCount })}</p>
                  {customerContext.lastInteractionAtUtc ? (
                    <p>{formatResource(copy.crmLastInteractionLabel, { value: formatDateTime(customerContext.lastInteractionAtUtc, culture) })}</p>
                  ) : null}
                  {customerContext.segments.length > 0 ? (
                    <p>{formatResource(copy.crmSegmentsLabel, { segments: customerContext.segments.map((segment) => segment.name).join(", ") })}</p>
                  ) : null}
                </div>
              ) : (
                <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.crmContextUnavailable}
                </p>
              )}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
