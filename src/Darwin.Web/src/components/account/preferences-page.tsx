import Link from "next/link";
import { AccountContentCompositionWindow } from "@/components/account/account-content-composition-window";
import { AccountStorefrontWindow } from "@/components/account/account-storefront-window";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import { SurfaceSectionNav } from "@/components/layout/surface-section-nav";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import { updateMemberPreferencesAction } from "@/features/member-portal/actions";
import type { PublicPageSummary } from "@/features/cms/types";
import type {
  MemberCustomerProfile,
  MemberPreferences,
} from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveApiStatusLabel,
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
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
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
  profileStatus,
  preferencesStatus,
  preferencesError,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
}: PreferencesPageProps) {
  const copy = getMemberResource(culture);
  const localizedStatus = resolveApiStatusLabel(status, copy) ?? status;
  const localizedProfileStatus = resolveApiStatusLabel(profileStatus, copy) ?? profileStatus;
  const resolvedPreferencesError = resolveLocalizedQueryMessage(
    preferencesError,
    copy,
  );
  const hasPhoneNumber = Boolean(profile?.phoneE164?.trim());
  const smsReady = Boolean(
    preferences?.allowSmsMarketing && hasPhoneNumber && profile?.phoneNumberConfirmed,
  );
  const whatsAppReady = Boolean(
    preferences?.allowWhatsAppMarketing &&
      hasPhoneNumber &&
      profile?.phoneNumberConfirmed,
  );
  const sectionLinks = [
    { href: "#preferences-form", label: copy.preferencesEditTitle },
    { href: "#preferences-summary", label: copy.preferencesRouteSummaryTitle },
    { href: "#preferences-readiness", label: copy.preferencesChannelReadinessTitle },
    { href: "#preferences-composition", label: copy.accountCompositionJourneyPreferencesTitle },
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
                <span className="text-[var(--color-text-primary)]">{copy.preferencesRouteLabel}</span>
              </nav>
              <p className="mt-4 text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                {copy.preferencesEditEyebrow}
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {copy.preferencesEditTitle}
              </h1>
              <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {preferences
                  ? copy.preferencesRouteSummaryMessage
                  : copy.preferencesRouteSummaryUnavailableMessage}
              </p>
              <div className="mt-6 flex flex-wrap gap-3">
                <Link
                  href={localizeHref("/account/profile", culture)}
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.memberRouteSummaryProfileCta}
                </Link>
                <Link
                  href={localizeHref("/account/addresses", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] bg-white/85 px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-white"
                >
                  {copy.preferencesRouteSummaryAddressesCta}
                </Link>
              </div>
            </div>
            <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-1">
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.preferencesEmailChannelTitle}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {profile?.email && preferences?.allowEmailMarketing
                    ? copy.dashboardCommunicationReady
                    : copy.dashboardCommunicationNeedsAttention}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.preferencesSmsChannelTitle}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {smsReady
                    ? copy.dashboardCommunicationReady
                    : copy.dashboardCommunicationNeedsAttention}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-[linear-gradient(135deg,rgba(57,116,47,0.94),rgba(255,145,77,0.92))] px-5 py-4 text-white shadow-[0_20px_40px_-28px_rgba(58,92,35,0.55)]">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/78">
                  {copy.preferencesWhatsAppChannelTitle}
                </p>
                <p className="mt-2 text-base font-semibold text-white">
                  {whatsAppReady
                    ? copy.dashboardCommunicationReady
                    : copy.dashboardCommunicationNeedsAttention}
                </p>
              </article>
            </div>
          </div>
        </div>

        <SurfaceSectionNav items={sectionLinks} />
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <form
          action={updateMemberPreferencesAction}
          id="preferences-form"
          className="scroll-mt-28 rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#f7fbef_100%)] px-6 py-8 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            {copy.preferencesRouteLabel}
          </p>
          <h2 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)] sm:text-3xl">
            {copy.preferencesEditTitle}
          </h2>

          {matchesLocalizedQueryMessageKey(
            preferencesStatus,
            "preferencesUpdatedMessage",
            "saved",
          ) && (
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
                  formatResource(copy.preferencesNeedsAttentionMessage, { status: localizedStatus })
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
            <div className="mt-6 space-y-4">
              <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.noPreferencesEditMessage}
              </p>
              <div className="flex flex-wrap gap-3">
                <Link href={localizeHref("/account/profile", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.memberRouteSummaryProfileCta}
                </Link>
                <Link href={localizeHref("/account/security", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.memberRouteSummarySecurityCta}
                </Link>
                <Link href={localizeHref("/account/addresses", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.preferencesRouteSummaryAddressesCta}
                </Link>
                <Link href={localizeHref("/account", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.memberCrossSurfaceAccountCta}
                </Link>
              </div>
            </div>
          )}
        </form>

        <div className="flex flex-col gap-6">
          <MemberPortalNav culture={culture} activePath="/account/preferences" />

          <aside id="preferences-summary" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.preferencesRouteSummaryTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {preferences
                ? copy.preferencesRouteSummaryMessage
                : copy.preferencesRouteSummaryUnavailableMessage}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.preferencesRouteSummaryProfileMessage, {
                preferencesStatus: localizedStatus,
                profileStatus: localizedProfileStatus,
              })}
            </p>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link href={localizeHref("/account/profile", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberRouteSummaryProfileCta}
              </Link>
              <Link href={localizeHref("/account/security", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberRouteSummarySecurityCta}
              </Link>
              <Link href={localizeHref("/account/addresses", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.preferencesRouteSummaryAddressesCta}
              </Link>
            </div>
          </aside>

          <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.currentContractTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.currentContractPreferencesMessage}
            </p>
          </aside>

          <aside id="preferences-readiness" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.preferencesChannelReadinessTitle}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.preferencesChannelReadinessMessage, {
                profileStatus: localizedProfileStatus,
                preferencesStatus: localizedStatus,
              })}
            </p>
            <div className="mt-6 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="font-semibold text-[var(--color-text-primary)]">
                  {copy.preferencesEmailChannelTitle}
                </p>
                <p className="mt-2">
                  {profile?.email
                    ? formatResource(copy.preferencesEmailChannelReadyMessage, {
                        email: profile.email,
                      })
                    : copy.preferencesEmailChannelUnavailableMessage}
                </p>
              </div>
              <div className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="font-semibold text-[var(--color-text-primary)]">
                  {copy.preferencesSmsChannelTitle}
                </p>
                <p className="mt-2">
                  {smsReady
                    ? formatResource(copy.preferencesSmsChannelReadyMessage, {
                        phone: profile?.phoneE164 ?? copy.unavailable,
                      })
                    : hasPhoneNumber
                      ? copy.preferencesSmsChannelVerificationMessage
                      : copy.preferencesSmsChannelUnavailableMessage}
                </p>
              </div>
              <div className="rounded-2xl bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="font-semibold text-[var(--color-text-primary)]">
                  {copy.preferencesWhatsAppChannelTitle}
                </p>
                <p className="mt-2">
                  {whatsAppReady
                    ? formatResource(copy.preferencesWhatsAppChannelReadyMessage, {
                        phone: profile?.phoneE164 ?? copy.unavailable,
                      })
                    : hasPhoneNumber
                      ? copy.preferencesWhatsAppChannelVerificationMessage
                      : copy.preferencesWhatsAppChannelUnavailableMessage}
                </p>
              </div>
            </div>
            <div className="mt-6 flex flex-wrap gap-3">
              <Link href={localizeHref("/account/profile", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.preferencesChannelReviewProfileCta}
              </Link>
              <Link href={localizeHref("/account/security", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberRouteSummarySecurityCta}
              </Link>
            </div>
          </aside>

          <MemberCrossSurfaceRail
            culture={culture}
            includeAccount={false}
            includeInvoices
          />

          <div id="preferences-composition" className="scroll-mt-28">
            <AccountContentCompositionWindow
              culture={culture}
              routeCard={{
                label: copy.accountCompositionJourneyCurrentLabel,
                title: copy.accountCompositionJourneyPreferencesTitle,
                description: formatResource(copy.accountCompositionJourneyPreferencesRouteDescription, {
                  status: localizedStatus,
                  profileStatus: localizedProfileStatus,
                }),
                href: "/account/preferences",
                ctaLabel: copy.accountCompositionJourneyCurrentCta,
              }}
              nextCard={{
                label: copy.accountCompositionJourneyNextLabel,
                title: copy.accountCompositionJourneyAddressesTitle,
                description: copy.accountCompositionJourneyAddressesDescription,
                href: "/account/addresses",
                ctaLabel: copy.accountCompositionJourneyAddressesCta,
              }}
              routeMapItems={[
                {
                  label: copy.accountCompositionRouteMapPreferencesLabel,
                  title: copy.accountCompositionRouteMapPreferencesTitle,
                  description: formatResource(copy.accountCompositionRouteMapPreferencesRouteDescription, {
                    status: localizedStatus,
                  }),
                  href: "/account/preferences",
                  ctaLabel: copy.accountCompositionRouteMapPreferencesCta,
                },
                {
                  label: copy.accountCompositionRouteMapNextLabel,
                  title: copy.accountCompositionRouteMapAddressesTitle,
                  description: copy.accountCompositionRouteMapAddressesDescription,
                  href: "/account/addresses",
                  ctaLabel: copy.accountCompositionRouteMapAddressesCta,
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
