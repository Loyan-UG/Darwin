import Link from "next/link";
import { AccountContentCompositionWindow } from "@/components/account/account-content-composition-window";
import { AccountStorefrontWindow } from "@/components/account/account-storefront-window";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { SurfaceSectionNav } from "@/components/layout/surface-section-nav";
import {
  confirmMemberPhoneVerificationAction,
  requestMemberPhoneVerificationAction,
  updateMemberProfileAction,
} from "@/features/member-portal/actions";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import type { MemberCustomerProfile } from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveApiStatusLabel,
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
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
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
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
}: ProfilePageProps) {
  const copy = getMemberResource(culture);
  const resolvedProfileError = resolveLocalizedQueryMessage(profileError, copy);
  const resolvedPhoneError = resolveLocalizedQueryMessage(phoneError, copy);
  const localizedStatus = resolveApiStatusLabel(status, copy) ?? status;
  const phoneBanner = getPhoneStatusBanner(
    copy,
    phoneStatus,
    Boolean(profile?.phoneNumberConfirmed),
  );
  const hasPhoneNumber = Boolean(profile?.phoneE164?.trim());
  const hasName = Boolean(profile?.firstName?.trim() || profile?.lastName?.trim());
  const hasLocale = Boolean(profile?.locale?.trim());
  const hasCurrency = Boolean(profile?.currency?.trim());
  const hasTimezone = Boolean(profile?.timezone?.trim());
  const profileReadinessState = hasName
    ? copy.profileReadinessReady
    : copy.profileReadinessNeedsAttention;
  const phoneReadinessState = profile?.phoneNumberConfirmed
    ? copy.profileReadinessReady
    : hasPhoneNumber
      ? copy.profileReadinessPending
      : copy.profileReadinessNeedsAttention;
  const localeReadinessState =
    hasLocale && hasCurrency && hasTimezone
      ? copy.profileReadinessReady
      : copy.profileReadinessNeedsAttention;
  const sectionLinks = [
    { href: "#profile-form", label: copy.profileEditTitle },
    { href: "#profile-readiness", label: copy.profileReadinessTitle },
    { href: "#profile-composition", label: copy.accountCompositionJourneyProfileTitle },
    { href: "#profile-verification", label: copy.phoneVerificationTitle },
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
                <span className="text-[var(--color-text-primary)]">{copy.profileRouteLabel}</span>
              </nav>
              <p className="mt-4 text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                {copy.profileEditEyebrow}
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {copy.profileEditTitle}
              </h1>
              <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {profile
                  ? formatResource(copy.profileRouteSummaryMessage, { status: localizedStatus })
                  : formatResource(copy.profileRouteSummaryUnavailableMessage, { status: localizedStatus })}
              </p>
              <div className="mt-6 flex flex-wrap gap-3">
                <Link
                  href={localizeHref("/account/preferences", culture)}
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.accountCompositionJourneyPreferencesCta}
                </Link>
                <Link
                  href={localizeHref("/account/security", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] bg-white/85 px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-white"
                >
                  {copy.profileReadinessSecurityCta}
                </Link>
              </div>
            </div>
            <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-1">
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.profileReadinessIdentityLabel}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {profileReadinessState}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.profileReadinessPhoneLabel}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {phoneReadinessState}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-[linear-gradient(135deg,rgba(57,116,47,0.94),rgba(255,145,77,0.92))] px-5 py-4 text-white shadow-[0_20px_40px_-28px_rgba(58,92,35,0.55)]">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/78">
                  {copy.profileReadinessLocaleLabel}
                </p>
                <p className="mt-2 text-base font-semibold text-white">
                  {localeReadinessState}
                </p>
              </article>
            </div>
          </div>
        </div>

        <SurfaceSectionNav items={sectionLinks} />
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
        <form
          action={updateMemberProfileAction}
          id="profile-form"
          className="scroll-mt-28 rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#f7fbef_100%)] px-6 py-8 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            {copy.profileRouteLabel}
          </p>
          <h2 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)] sm:text-3xl">
            {copy.profileEditTitle}
          </h2>

          {matchesLocalizedQueryMessageKey(
            profileStatus,
            "profileUpdatedMessage",
            "saved",
          ) && (
            <div className="mt-6">
              <StatusBanner
                title={copy.profileUpdatedTitle}
                message={copy.profileUpdatedMessage}
              />
            </div>
          )}

          {(resolvedProfileError || status !== "ok") && (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title={copy.profileNeedsAttentionTitle}
                message={
                  resolvedProfileError ??
                  formatResource(copy.profileNeedsAttentionMessage, { status: localizedStatus })
                }
              />
            </div>
          )}

          {profile ? (
            <>
              <input type="hidden" name="id" value={profile.id} />
              <input type="hidden" name="email" value={profile.email ?? ""} />
              <input type="hidden" name="rowVersion" value={profile.rowVersion ?? ""} />

              <div className="mt-8 grid gap-4 sm:grid-cols-2">
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
            <p className="mt-6 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.noProfileEditMessage}
            </p>
          )}
        </form>

        <div className="flex flex-col gap-6">
          <MemberPortalNav culture={culture} activePath="/account/profile" />

          <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.boundaryTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.boundaryProfileMessage}
            </p>
          </aside>

          <aside id="profile-readiness" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.profileReadinessTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.profileReadinessMessage, { status: localizedStatus })}
            </p>
            <div className="mt-6 grid gap-3">
              <article className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.profileReadinessIdentityLabel}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {profileReadinessState}
                </p>
              </article>
              <article className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.profileReadinessPhoneLabel}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {phoneReadinessState}
                </p>
              </article>
              <article className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.profileReadinessLocaleLabel}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {localeReadinessState}
                </p>
              </article>
            </div>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link href={localizeHref("/checkout", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.profileReadinessCheckoutCta}
              </Link>
              <Link href={localizeHref("/account/preferences", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.profileReadinessPreferencesCta}
              </Link>
              <Link href={localizeHref("/account/security", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.profileReadinessSecurityCta}
              </Link>
            </div>
          </aside>

          <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.memberRouteSummaryTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {profile
                ? formatResource(copy.profileRouteSummaryMessage, { status: localizedStatus })
                : formatResource(copy.profileRouteSummaryUnavailableMessage, { status: localizedStatus })}
            </p>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link href={localizeHref("/account/preferences", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberRouteSummaryPreferencesCta}
              </Link>
              <Link href={localizeHref("/account/security", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberRouteSummarySecurityCta}
              </Link>
              <Link href={localizeHref("/account/addresses", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.preferencesRouteSummaryAddressesCta}
              </Link>
            </div>
          </aside>

          <MemberCrossSurfaceRail
            culture={culture}
            includeAccount={false}
            includeOrders
            includeLoyalty={false}
          />

          <div id="profile-composition" className="scroll-mt-28">
            <AccountContentCompositionWindow
              culture={culture}
              routeCard={{
                label: copy.accountCompositionJourneyCurrentLabel,
                title: copy.accountCompositionJourneyProfileTitle,
                description: formatResource(copy.accountCompositionJourneyProfileDescription, {
                  status: localizedStatus,
                }),
                href: "/account/profile",
                ctaLabel: copy.accountCompositionJourneyCurrentCta,
              }}
              nextCard={{
                label: copy.accountCompositionJourneyNextLabel,
                title: copy.accountCompositionJourneyPreferencesTitle,
                description: copy.accountCompositionJourneyPreferencesDescription,
                href: "/account/preferences",
                ctaLabel: copy.accountCompositionJourneyPreferencesCta,
              }}
              routeMapItems={[
                {
                  label: copy.accountCompositionRouteMapProfileLabel,
                  title: copy.accountCompositionRouteMapProfileTitle,
                  description: formatResource(copy.accountCompositionRouteMapProfileDescription, {
                    status: localizedStatus,
                  }),
                  href: "/account/profile",
                  ctaLabel: copy.accountCompositionRouteMapProfileCta,
                },
                {
                  label: copy.accountCompositionRouteMapNextLabel,
                  title: copy.accountCompositionRouteMapPreferencesTitle,
                  description: copy.accountCompositionRouteMapPreferencesDescription,
                  href: "/account/preferences",
                  ctaLabel: copy.accountCompositionRouteMapPreferencesCta,
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

          <aside id="profile-verification" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.phoneVerificationEyebrow}
            </p>
            <h2 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
              {copy.phoneVerificationTitle}
            </h2>

            {phoneBanner && (
              <div className="mt-6">
                <StatusBanner title={phoneBanner.title} message={phoneBanner.message} />
              </div>
            )}

            {resolvedPhoneError && (
              <div className="mt-6">
                <StatusBanner
                  tone="warning"
                  title={copy.phoneNeedsAttentionTitle}
                  message={resolvedPhoneError}
                />
              </div>
            )}

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
              <div className="mt-6 space-y-4">
                <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.phoneVerificationUnavailable}
                </p>
                <div className="flex flex-wrap gap-3">
                  <Link href={localizeHref("/account/preferences", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                    {copy.memberRouteSummaryPreferencesCta}
                  </Link>
                  <Link href={localizeHref("/account/addresses", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                    {copy.preferencesRouteSummaryAddressesCta}
                  </Link>
                </div>
              </div>
            )}
          </aside>
        </div>
      </div>
      </div>
    </section>
  );
}
