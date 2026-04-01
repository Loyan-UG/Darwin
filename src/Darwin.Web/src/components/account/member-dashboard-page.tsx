import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { signOutMemberAction } from "@/features/member-session/actions";
import type { MemberSession } from "@/features/member-session/types";
import type {
  LinkedCustomerContext,
  MemberCustomerProfile,
  MemberPreferences,
} from "@/features/member-portal/types";
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
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <div className="flex flex-wrap items-start justify-between gap-5">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                Member dashboard
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                Signed in as {session.email}
              </h1>
              <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                This dashboard reads the authenticated member profile surfaces through the new web-owned browser session layer.
              </p>
            </div>
            <form action={signOutMemberAction}>
              <button
                type="submit"
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                Sign out
              </button>
            </form>
          </div>
        </div>

        {(profileStatus !== "ok" || preferencesStatus !== "ok" || customerContextStatus !== "ok") && (
          <StatusBanner
            tone="warning"
            title="Some member data loaded with warnings."
            message={`Profile: ${profileStatus}. Preferences: ${preferencesStatus}. CRM context: ${customerContextStatus}.`}
          />
        )}

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
          <div className="grid gap-6">
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                Profile snapshot
              </p>
              {profile ? (
                <div className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)] sm:grid-cols-2">
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Email:</span> {profile.email ?? session.email}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Name:</span> {[profile.firstName, profile.lastName].filter(Boolean).join(" ") || "Unavailable"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Phone:</span> {profile.phoneE164 ?? "Unavailable"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Phone verified:</span> {profile.phoneNumberConfirmed ? "Yes" : "No"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Locale:</span> {profile.locale ?? "Unavailable"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Timezone:</span> {profile.timezone ?? "Unavailable"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Currency:</span> {profile.currency ?? "Unavailable"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Access token expiry:</span> {formatDateTime(session.accessTokenExpiresAtUtc, culture)}</p>
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  Profile data is not currently available for this member session.
                </p>
              )}
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                Preference snapshot
              </p>
              {preferences ? (
                <div className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)] sm:grid-cols-2">
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Marketing consent:</span> {preferences.marketingConsent ? "Granted" : "Not granted"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Email marketing:</span> {preferences.allowEmailMarketing ? "Allowed" : "Blocked"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">SMS marketing:</span> {preferences.allowSmsMarketing ? "Allowed" : "Blocked"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">WhatsApp marketing:</span> {preferences.allowWhatsAppMarketing ? "Allowed" : "Blocked"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Promotional push:</span> {preferences.allowPromotionalPushNotifications ? "Allowed" : "Blocked"}</p>
                  <p><span className="font-semibold text-[var(--color-text-primary)]">Optional analytics:</span> {preferences.allowOptionalAnalyticsTracking ? "Allowed" : "Blocked"}</p>
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  Preference data is not currently available for this member session.
                </p>
              )}
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                Portal routes
              </p>
              <div className="mt-5 flex flex-col gap-2">
                <Link href="/account/profile" className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">Profile</Link>
                <Link href="/account/preferences" className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">Preferences</Link>
                <Link href="/account/addresses" className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">Addresses</Link>
                <Link href="/orders" className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">Orders</Link>
                <Link href="/invoices" className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">Invoices</Link>
                <Link href="/loyalty" className="rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">Loyalty</Link>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                CRM context
              </p>
              {customerContext ? (
                <div className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p className="font-semibold text-[var(--color-text-primary)]">{customerContext.displayName}</p>
                  <p>{customerContext.email}</p>
                  {customerContext.companyName ? <p>{customerContext.companyName}</p> : null}
                  <p>Interactions: {customerContext.interactionCount}</p>
                  {customerContext.lastInteractionAtUtc ? (
                    <p>Last interaction: {formatDateTime(customerContext.lastInteractionAtUtc, culture)}</p>
                  ) : null}
                  {customerContext.segments.length > 0 ? (
                    <p>Segments: {customerContext.segments.map((segment) => segment.name).join(", ")}</p>
                  ) : null}
                </div>
              ) : (
                <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  No linked CRM customer context is currently available.
                </p>
              )}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
