import { StatusBanner } from "@/components/feedback/status-banner";
import { updateMemberPreferencesAction } from "@/features/member-portal/actions";
import type { MemberPreferences } from "@/features/member-portal/types";

type PreferencesPageProps = {
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
  preferences,
  status,
  preferencesStatus,
  preferencesError,
}: PreferencesPageProps) {
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <form
          action={updateMemberPreferencesAction}
          className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">Preferences</p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Update privacy and communication preferences
          </h1>

          {preferencesStatus === "saved" && (
            <div className="mt-6">
              <StatusBanner title="Preferences updated" message="The member preference snapshot was saved through the canonical profile endpoint." />
            </div>
          )}

          {(preferencesError || status !== "ok") && (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title="Preference update needs attention"
                message={preferencesError ?? `The member preferences endpoint returned status "${status}".`}
              />
            </div>
          )}

          {preferences ? (
            <>
              <input type="hidden" name="rowVersion" value={preferences.rowVersion} />
              <div className="mt-8 grid gap-4">
                <ToggleField name="marketingConsent" label="Aggregate marketing consent" defaultChecked={preferences.marketingConsent} />
                <ToggleField name="allowEmailMarketing" label="Allow email marketing" defaultChecked={preferences.allowEmailMarketing} />
                <ToggleField name="allowSmsMarketing" label="Allow SMS marketing" defaultChecked={preferences.allowSmsMarketing} />
                <ToggleField name="allowWhatsAppMarketing" label="Allow WhatsApp marketing" defaultChecked={preferences.allowWhatsAppMarketing} />
                <ToggleField name="allowPromotionalPushNotifications" label="Allow promotional push notifications" defaultChecked={preferences.allowPromotionalPushNotifications} />
                <ToggleField name="allowOptionalAnalyticsTracking" label="Allow optional analytics tracking" defaultChecked={preferences.allowOptionalAnalyticsTracking} />
              </div>
              <button type="submit" className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                Save preferences
              </button>
            </>
          ) : (
            <p className="mt-6 text-sm leading-7 text-[var(--color-text-secondary)]">
              No preference snapshot is currently available for editing.
            </p>
          )}
        </form>

        <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">Current contract</p>
          <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
            These switches map directly to the current member preference contract. More granular communication controls can be layered later when Communication Core grows.
          </p>
        </aside>
      </div>
    </section>
  );
}
