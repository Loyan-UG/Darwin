import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { updateMemberPreferencesAction } from "@/features/member-portal/actions";
import type {
  MemberCustomerProfile,
  MemberPreferences,
} from "@/features/member-portal/types";
import {
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { localizeHref } from "@/lib/locale-routing";

type PreferencesPageProps = {
  culture: string;
  preferences: MemberPreferences | null;
  status: string;
  profile: MemberCustomerProfile | null;
  profileStatus: string;
  preferencesStatus?: string;
  preferencesError?: string;
};

function ToggleField({
  name,
  label,
  defaultChecked,
}: {
  name: string;
  label: string;
  defaultChecked: boolean;
}) {
  return (
    <label className="flex items-center justify-between gap-4 rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm font-medium text-[var(--color-text-primary)]">
      <span>{label}</span>
      <input type="checkbox" name={name} defaultChecked={defaultChecked} className="h-4 w-4" />
    </label>
  );
}

export function PreferencesPage({
  culture,
  preferences,
  status,
  profile,
  preferencesStatus,
  preferencesError,
}: PreferencesPageProps) {
  const copy = getMemberResource(culture);
  const resolvedPreferencesError = resolveLocalizedQueryMessage(preferencesError, copy);

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
              {copy.preferencesRouteLabel}
            </span>
          </nav>

          <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {copy.preferencesEditEyebrow}
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {copy.preferencesEditTitle}
            </h1>
            <p className="mt-4 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.memberPreferencesIntroMessage}
            </p>
          </header>

          {matchesLocalizedQueryMessageKey(preferencesStatus, "preferencesUpdatedMessage", "saved") ? (
            <StatusBanner
              title={copy.preferencesUpdatedTitle}
              message={copy.preferencesUpdatedMessage}
            />
          ) : null}

          {(resolvedPreferencesError || status !== "ok") ? (
            <StatusBanner
              tone="warning"
              title={copy.preferencesNeedsAttentionTitle}
              message={resolvedPreferencesError ?? copy.noPreferencesEditMessage}
            />
          ) : null}

          <form
            action={updateMemberPreferencesAction}
            className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)] sm:p-8"
          >
            {preferences ? (
              <>
                <input type="hidden" name="rowVersion" value={preferences.rowVersion} />
                <div className="grid gap-4">
                  <ToggleField name="marketingConsent" label={copy.toggleMarketingConsent} defaultChecked={preferences.marketingConsent} />
                  <ToggleField name="allowEmailMarketing" label={copy.toggleEmailMarketing} defaultChecked={preferences.allowEmailMarketing} />
                  <ToggleField name="allowSmsMarketing" label={copy.toggleSmsMarketing} defaultChecked={preferences.allowSmsMarketing} />
                  <ToggleField name="allowWhatsAppMarketing" label={copy.toggleWhatsAppMarketing} defaultChecked={preferences.allowWhatsAppMarketing} />
                  <ToggleField name="allowPromotionalPushNotifications" label={copy.togglePushMarketing} defaultChecked={preferences.allowPromotionalPushNotifications} />
                  <ToggleField name="allowOptionalAnalyticsTracking" label={copy.toggleAnalytics} defaultChecked={preferences.allowOptionalAnalyticsTracking} />
                </div>
                <button
                  type="submit"
                  className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.savePreferencesCta}
                </button>
              </>
            ) : (
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.noPreferencesEditMessage}
              </p>
            )}
          </form>

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)] sm:p-8">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.memberPreferencesChannelsTitle}
            </p>
            <div className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <p className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.preferencesEmailChannelTitle}
                </span>
                <br />
                {profile?.email
                  ? copy.preferencesEmailChannelReadyMessage.replace("{email}", profile.email)
                  : copy.preferencesEmailChannelUnavailableMessage}
              </p>
              <p className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.preferencesSmsChannelTitle}
                </span>
                <br />
                {profile?.phoneE164
                  ? copy.preferencesSmsChannelVerificationMessage
                  : copy.preferencesSmsChannelUnavailableMessage}
              </p>
              <p className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <span className="font-semibold text-[var(--color-text-primary)]">
                  {copy.preferencesWhatsAppChannelTitle}
                </span>
                <br />
                {profile?.phoneE164
                  ? copy.preferencesWhatsAppChannelVerificationMessage
                  : copy.preferencesWhatsAppChannelUnavailableMessage}
              </p>
            </div>
          </section>
        </div>

        <aside className="flex flex-col gap-5">
          <MemberPortalNav culture={culture} activePath="/account/preferences" />
          <Link
            href={localizeHref("/account/profile", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.memberOpenProfileCta}
          </Link>
          <Link
            href={localizeHref("/account/addresses", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.memberOpenAddressesCta}
          </Link>
        </aside>
      </div>
    </section>
  );
}
