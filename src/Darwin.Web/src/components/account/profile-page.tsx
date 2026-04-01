import { StatusBanner } from "@/components/feedback/status-banner";
import {
  confirmMemberPhoneVerificationAction,
  requestMemberPhoneVerificationAction,
  updateMemberProfileAction,
} from "@/features/member-portal/actions";
import type { MemberCustomerProfile } from "@/features/member-portal/types";

type ProfilePageProps = {
  profile: MemberCustomerProfile | null;
  supportedCultures: string[];
  status: string;
  profileStatus?: string;
  profileError?: string;
  phoneStatus?: string;
  phoneError?: string;
};

export function ProfilePage({
  profile,
  supportedCultures,
  status,
  profileStatus,
  profileError,
  phoneStatus,
  phoneError,
}: ProfilePageProps) {
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <form
          action={updateMemberProfileAction}
          className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Profile
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Edit member profile
          </h1>

          {profileStatus === "saved" && (
            <div className="mt-6">
              <StatusBanner title="Profile updated" message="The member profile was saved through the canonical profile endpoint." />
            </div>
          )}

          {(profileError || status !== "ok") && (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title="Profile update needs attention"
                message={profileError ?? `The member profile endpoint returned status "${status}".`}
              />
            </div>
          )}

          {profile ? (
            <>
              <input type="hidden" name="id" value={profile.id} />
              <input type="hidden" name="rowVersion" value={profile.rowVersion ?? ""} />
              <input type="hidden" name="email" value={profile.email ?? ""} />
              <div className="mt-8 grid gap-4 sm:grid-cols-2">
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  First name
                  <input name="firstName" defaultValue={profile.firstName ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  Last name
                  <input name="lastName" defaultValue={profile.lastName ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                  Phone
                  <input name="phoneE164" defaultValue={profile.phoneE164 ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  Locale
                  <select name="locale" defaultValue={profile.locale ?? supportedCultures[0] ?? "de-DE"} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none">
                    {supportedCultures.map((culture) => (
                      <option key={culture} value={culture}>
                        {culture}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  Timezone
                  <input name="timezone" defaultValue={profile.timezone ?? "Europe/Berlin"} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
                </label>
                <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                  Currency
                  <input name="currency" defaultValue={profile.currency ?? "EUR"} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
                </label>
              </div>
              <button type="submit" className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                Save profile
              </button>
            </>
          ) : (
            <p className="mt-6 text-sm leading-7 text-[var(--color-text-secondary)]">
              No profile snapshot is currently available for editing.
            </p>
          )}
        </form>

        <div className="flex flex-col gap-6">
          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              Boundary
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              Email remains display-only here. This page only edits the fields exposed by the canonical member profile contract.
            </p>
          </aside>

          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              Phone verification
            </p>
            <h2 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
              Keep phone status aligned with the canonical profile contract
            </h2>

            {profile?.phoneNumberConfirmed && (
              <div className="mt-6">
                <StatusBanner
                  title="Phone already verified"
                  message="This member profile is already marked as phone-number confirmed."
                />
              </div>
            )}

            {phoneStatus === "requested" && (
              <div className="mt-6">
                <StatusBanner
                  title="Verification code requested"
                  message="The backend accepted the phone verification request. The member should receive a code through the selected channel."
                />
              </div>
            )}

            {phoneStatus === "confirmed" && (
              <div className="mt-6">
                <StatusBanner
                  title="Phone verified"
                  message="The verification code was accepted and the member profile should now reflect a confirmed phone number."
                />
              </div>
            )}

            {phoneError && (
              <div className="mt-6">
                <StatusBanner
                  tone="warning"
                  title="Phone verification needs attention"
                  message={phoneError}
                />
              </div>
            )}

            {profile ? (
              <div className="mt-6 flex flex-col gap-6">
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p>
                    Current phone:{" "}
                    <span className="font-semibold text-[var(--color-text-primary)]">
                      {profile.phoneE164 ?? "Unavailable"}
                    </span>
                  </p>
                  <p>
                    Confirmed:{" "}
                    <span className="font-semibold text-[var(--color-text-primary)]">
                      {profile.phoneNumberConfirmed ? "Yes" : "No"}
                    </span>
                  </p>
                </div>

                {!profile.phoneNumberConfirmed && (
                  <>
                    <form action={requestMemberPhoneVerificationAction} className="flex flex-col gap-4">
                      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                        Delivery channel
                        <select
                          name="channel"
                          defaultValue="Sms"
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
                        >
                          <option value="Sms">SMS</option>
                          <option value="WhatsApp">WhatsApp</option>
                        </select>
                      </label>
                      <button
                        type="submit"
                        className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                      >
                        Request verification code
                      </button>
                    </form>

                    <form action={confirmMemberPhoneVerificationAction} className="flex flex-col gap-4">
                      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                        Verification code
                        <input
                          name="code"
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
                        />
                      </label>
                      <button
                        type="submit"
                        className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                      >
                        Confirm phone
                      </button>
                    </form>
                  </>
                )}
              </div>
            ) : (
              <p className="mt-6 text-sm leading-7 text-[var(--color-text-secondary)]">
                Phone verification becomes available once the member profile can be loaded.
              </p>
            )}
          </aside>
        </div>
      </div>
    </section>
  );
}
