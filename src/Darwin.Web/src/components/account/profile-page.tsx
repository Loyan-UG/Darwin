import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import {
  confirmMemberPhoneVerificationAction,
  requestMemberPhoneVerificationAction,
  updateMemberProfileAction,
} from "@/features/member-portal/actions";
import type { MemberCustomerProfile } from "@/features/member-portal/types";
import {
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { getCultureDisplayName } from "@/lib/culture";
import { localizeHref } from "@/lib/locale-routing";

type ProfilePageProps = {
  culture: string;
  profile: MemberCustomerProfile | null;
  supportedCultures: string[];
  status: string;
  profileStatus?: string;
  profileError?: string;
  phoneStatus?: string;
  phoneError?: string;
};

function getPhoneStatusBanner(
  copy: ReturnType<typeof getMemberResource>,
  phoneStatus: string | undefined,
  phoneConfirmed: boolean,
) {
  if (matchesLocalizedQueryMessageKey(phoneStatus, "phoneCodeRequestedMessage", "requested")) {
    return {
      title: copy.phoneCodeRequestedTitle,
      message: copy.phoneCodeRequestedMessage,
    };
  }

  if (matchesLocalizedQueryMessageKey(phoneStatus, "phoneVerifiedMessage", "confirmed")) {
    return {
      title: copy.phoneVerifiedTitle,
      message: copy.phoneVerifiedMessage,
    };
  }

  if (phoneConfirmed) {
    return {
      title: copy.phoneAlreadyVerifiedTitle,
      message: copy.phoneAlreadyVerifiedMessage,
    };
  }

  return null;
}

export function ProfilePage({
  culture,
  profile,
  supportedCultures,
  status,
  profileStatus,
  profileError,
  phoneStatus,
  phoneError,
}: ProfilePageProps) {
  const copy = getMemberResource(culture);
  const resolvedProfileError = resolveLocalizedQueryMessage(profileError, copy);
  const resolvedPhoneError = resolveLocalizedQueryMessage(phoneError, copy);
  const phoneBanner = getPhoneStatusBanner(
    copy,
    phoneStatus,
    Boolean(profile?.phoneNumberConfirmed),
  );
  const hasPhoneNumber = Boolean(profile?.phoneE164?.trim());

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
              {copy.profileRouteLabel}
            </span>
          </nav>

          <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {copy.profileEditEyebrow}
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {copy.profileEditTitle}
            </h1>
            <p className="mt-4 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.boundaryProfileMessage}
            </p>
          </header>

          {matchesLocalizedQueryMessageKey(profileStatus, "profileUpdatedMessage", "saved") ? (
            <StatusBanner
              title={copy.profileUpdatedTitle}
              message={copy.profileUpdatedMessage}
            />
          ) : null}

          {(resolvedProfileError || status !== "ok") ? (
            <StatusBanner
              tone="warning"
              title={copy.profileNeedsAttentionTitle}
              message={resolvedProfileError ?? copy.noProfileEditMessage}
            />
          ) : null}

          <form
            action={updateMemberProfileAction}
            className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)] sm:p-8"
          >
            {profile ? (
              <>
                <input type="hidden" name="id" value={profile.id} />
                <input type="hidden" name="email" value={profile.email ?? ""} />
                <input type="hidden" name="rowVersion" value={profile.rowVersion ?? ""} />

                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 sm:col-span-2">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                      {copy.emailLabel}
                    </p>
                    <p className="mt-2 text-sm font-semibold text-[var(--color-text-primary)]">
                      {profile.email ?? copy.unavailable}
                    </p>
                  </div>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                    {copy.firstNameLabel}
                    <input
                      name="firstName"
                      defaultValue={profile.firstName ?? ""}
                      autoComplete="given-name"
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
                    />
                  </label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                    {copy.lastNameLabel}
                    <input
                      name="lastName"
                      defaultValue={profile.lastName ?? ""}
                      autoComplete="family-name"
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
                    />
                  </label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                    {copy.phoneLabelBare}
                    <input
                      name="phoneE164"
                      defaultValue={profile.phoneE164 ?? ""}
                      autoComplete="tel"
                      inputMode="tel"
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
                    />
                  </label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                    {copy.localeLabel}
                    <select
                      name="locale"
                      defaultValue={profile.locale ?? culture}
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
                    >
                      {supportedCultures.map((supportedCulture) => (
                        <option key={supportedCulture} value={supportedCulture}>
                          {getCultureDisplayName(supportedCulture)}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                    {copy.currencyLabel}
                    <input
                      name="currency"
                      defaultValue={profile.currency ?? "EUR"}
                      maxLength={3}
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal uppercase outline-none"
                    />
                  </label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
                    {copy.timezoneLabel}
                    <input
                      name="timezone"
                      defaultValue={profile.timezone ?? "Europe/Berlin"}
                      autoComplete="off"
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
                    />
                  </label>
                </div>

                <button
                  type="submit"
                  className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.saveProfileCta}
                </button>
              </>
            ) : (
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.noProfileEditMessage}
              </p>
            )}
          </form>

          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)] sm:p-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.phoneVerificationEyebrow}
            </p>
            <h2 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
              {copy.phoneVerificationTitle}
            </h2>

            {phoneBanner ? (
              <div className="mt-6">
                <StatusBanner title={phoneBanner.title} message={phoneBanner.message} />
              </div>
            ) : null}

            {resolvedPhoneError ? (
              <div className="mt-6">
                <StatusBanner
                  tone="warning"
                  title={copy.phoneNeedsAttentionTitle}
                  message={resolvedPhoneError}
                />
              </div>
            ) : null}

            {profile ? (
              <div className="mt-6 space-y-6">
                <div className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p>
                    <span className="font-semibold text-[var(--color-text-primary)]">
                      {copy.currentPhoneLabel}
                    </span>{" "}
                    {profile.phoneE164 ?? copy.unavailable}
                  </p>
                  <p>
                    <span className="font-semibold text-[var(--color-text-primary)]">
                      {copy.confirmedLabel}
                    </span>{" "}
                    {profile.phoneNumberConfirmed ? copy.yes : copy.no}
                  </p>
                </div>

                <form action={requestMemberPhoneVerificationAction} className="space-y-4">
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                    {copy.deliveryChannelLabel}
                    <select
                      name="channel"
                      defaultValue="Sms"
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
                    >
                      <option value="Sms">{copy.smsChannelLabel}</option>
                      <option value="WhatsApp">{copy.whatsAppChannelLabel}</option>
                    </select>
                  </label>
                  <button
                    type="submit"
                    disabled={!hasPhoneNumber}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    {copy.requestVerificationCodeCta}
                  </button>
                  {!hasPhoneNumber ? (
                    <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                      {copy.phoneMissingForVerificationMessage}
                    </p>
                  ) : null}
                </form>

                <form action={confirmMemberPhoneVerificationAction} className="space-y-4">
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                    {copy.verificationCodeLabel}
                    <input
                      name="code"
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
                    />
                  </label>
                  <button
                    type="submit"
                    className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                  >
                    {copy.confirmPhoneCta}
                  </button>
                </form>
              </div>
            ) : (
              <p className="mt-6 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.phoneVerificationUnavailable}
              </p>
            )}
          </section>
        </div>

        <aside className="flex flex-col gap-5">
          <MemberPortalNav culture={culture} activePath="/account/profile" />
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
