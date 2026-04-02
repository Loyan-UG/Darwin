import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { updateMemberPreferencesAction } from "@/features/member-portal/actions";
import type { MemberPreferences } from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { localizeHref } from "@/lib/locale-routing";

type PreferencesPageProps = {
  culture: string;
  preferences: MemberPreferences | null;
  status: string;
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
  preferencesStatus,
  preferencesError,
}: PreferencesPageProps) {
  const copy = getMemberResource(culture);
  const resolvedPreferencesError = resolveLocalizedQueryMessage(
    preferencesError,
    copy,
  );

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <form
          action={updateMemberPreferencesAction}
          className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
        >
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
            <span className="text-[var(--color-text-primary)]">{copy.preferencesRouteLabel}</span>
          </nav>
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.preferencesEditEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.preferencesEditTitle}
          </h1>

          {preferencesStatus === "saved" && (
            <div className="mt-6">
              <StatusBanner
                title={copy.preferencesUpdatedTitle}
                message={copy.preferencesUpdatedMessage}
              />
            </div>
          )}

          {(resolvedPreferencesError || status !== "ok") && (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title={copy.preferencesNeedsAttentionTitle}
                message={
                  resolvedPreferencesError ??
                  formatResource(copy.preferencesNeedsAttentionMessage, { status })
                }
              />
            </div>
          )}

          {preferences ? (
            <>
              <input type="hidden" name="rowVersion" value={preferences.rowVersion} />
              <div className="mt-8 grid gap-4">
                <ToggleField name="marketingConsent" label={copy.toggleMarketingConsent} defaultChecked={preferences.marketingConsent} />
                <ToggleField name="allowEmailMarketing" label={copy.toggleEmailMarketing} defaultChecked={preferences.allowEmailMarketing} />
                <ToggleField name="allowSmsMarketing" label={copy.toggleSmsMarketing} defaultChecked={preferences.allowSmsMarketing} />
                <ToggleField name="allowWhatsAppMarketing" label={copy.toggleWhatsAppMarketing} defaultChecked={preferences.allowWhatsAppMarketing} />
                <ToggleField name="allowPromotionalPushNotifications" label={copy.togglePushMarketing} defaultChecked={preferences.allowPromotionalPushNotifications} />
                <ToggleField name="allowOptionalAnalyticsTracking" label={copy.toggleAnalytics} defaultChecked={preferences.allowOptionalAnalyticsTracking} />
              </div>
              <button type="submit" className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                {copy.savePreferencesCta}
              </button>
            </>
          ) : (
            <p className="mt-6 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.noPreferencesEditMessage}
            </p>
          )}
        </form>

        <div className="flex flex-col gap-6">
          <MemberPortalNav culture={culture} activePath="/account/preferences" />

          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.preferencesRouteSummaryTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {preferences
                ? copy.preferencesRouteSummaryMessage
                : copy.preferencesRouteSummaryUnavailableMessage}
            </p>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link href={localizeHref("/account/profile", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberRouteSummaryProfileCta}
              </Link>
              <Link href={localizeHref("/account/addresses", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.preferencesRouteSummaryAddressesCta}
              </Link>
            </div>
          </aside>

          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.currentContractTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.currentContractPreferencesMessage}
            </p>
          </aside>

          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.memberCrossSurfaceTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.memberCrossSurfaceMessage}
            </p>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link href={localizeHref("/", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberCrossSurfaceHomeCta}
              </Link>
              <Link href={localizeHref("/catalog", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberCrossSurfaceCatalogCta}
              </Link>
              <Link href={localizeHref("/invoices", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberCrossSurfaceInvoicesCta}
              </Link>
            </div>
          </aside>
        </div>
      </div>
    </section>
  );
}
