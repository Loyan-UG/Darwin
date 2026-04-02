import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCartSummary } from "@/features/cart/types";
import type { PublicCategorySummary } from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import { buildCheckoutDraftSearch, toCheckoutDraftFromMemberAddress } from "@/features/checkout/helpers";
import { signOutMemberAction } from "@/features/member-session/actions";
import type { MemberSession } from "@/features/member-session/types";
import type {
  LinkedCustomerContext,
  MemberAddress,
  MemberCustomerProfile,
  MemberInvoiceSummary,
  MemberOrderSummary,
  MemberPreferences,
  MyLoyaltyBusinessSummary,
  MyLoyaltyOverview,
} from "@/features/member-portal/types";
import { formatResource, getMemberResource } from "@/localization";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { parseUtcTimestamp } from "@/lib/time";
import { toWebApiUrl } from "@/lib/webapi-url";

type MemberDashboardPageProps = {
  culture: string;
  session: MemberSession;
  profile: MemberCustomerProfile | null;
  profileStatus: string;
  preferences: MemberPreferences | null;
  preferencesStatus: string;
  customerContext: LinkedCustomerContext | null;
  customerContextStatus: string;
  addresses: MemberAddress[];
  addressesStatus: string;
  recentOrders: MemberOrderSummary[];
  recentOrdersStatus: string;
  recentInvoices: MemberInvoiceSummary[];
  recentInvoicesStatus: string;
  loyaltyOverview: MyLoyaltyOverview | null;
  loyaltyOverviewStatus: string;
  loyaltyBusinesses: MyLoyaltyBusinessSummary[];
  loyaltyBusinessesStatus: string;
  storefrontCart: PublicCartSummary | null;
  storefrontCartStatus: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
};

type DashboardActionItem = {
  id: string;
  label: string;
  title: string;
  description: string;
  href: string;
  cta: string;
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
  addresses,
  addressesStatus,
  recentOrders,
  recentOrdersStatus,
  recentInvoices,
  recentInvoicesStatus,
  loyaltyOverview,
  loyaltyOverviewStatus,
  loyaltyBusinesses,
  loyaltyBusinessesStatus,
  storefrontCart,
  storefrontCartStatus,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
}: MemberDashboardPageProps) {
  const copy = getMemberResource(culture);
  const hasValidSessionExpiry = parseUtcTimestamp(session.accessTokenExpiresAtUtc) !== null;
  const sessionNeedsAttention =
    !hasValidSessionExpiry;
  const securityState =
    !profile?.phoneNumberConfirmed && sessionNeedsAttention
      ? copy.dashboardSecurityStateNeedsAttention
      : !profile?.phoneNumberConfirmed
        ? copy.dashboardSecurityStateVerifyPhone
        : sessionNeedsAttention
          ? copy.dashboardSecurityStateRefreshSoon
          : copy.dashboardSecurityStateHealthy;
  const preferredCheckoutAddress =
    addresses.find((address) => address.isDefaultShipping) ??
    addresses.find((address) => address.isDefaultBilling) ??
    addresses[0] ??
    null;
  const emailChannelReady = Boolean(
    profile?.email && preferences?.allowEmailMarketing,
  );
  const smsChannelReady = Boolean(
    profile?.phoneE164 &&
      profile.phoneNumberConfirmed &&
      preferences?.allowSmsMarketing,
  );
  const whatsAppChannelReady = Boolean(
    profile?.phoneE164 &&
      profile.phoneNumberConfirmed &&
      preferences?.allowWhatsAppMarketing,
  );
  const checkoutHref = preferredCheckoutAddress
    ? localizeHref(
        `/checkout${buildCheckoutDraftSearch(
          toCheckoutDraftFromMemberAddress(preferredCheckoutAddress),
          { memberAddressId: preferredCheckoutAddress.id },
        )}`,
        culture,
      )
    : localizeHref("/checkout", culture);
  const loyaltyFocusAccounts = [...(loyaltyOverview?.accounts ?? [])]
    .sort((left, right) => {
      const leftRank = left.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
      const rightRank = right.pointsToNextReward ?? Number.MAX_SAFE_INTEGER;
      return leftRank - rightRank;
    })
    .slice(0, 3);
  const invoiceAttention = recentInvoices.find((invoice) => invoice.balanceMinor > 0);
  const hasStorefrontCart = Boolean(storefrontCart && storefrontCart.items.length > 0);
  const actionItems: DashboardActionItem[] = [
    hasStorefrontCart
      ? {
          id: "storefront-cart",
          label: copy.dashboardActionCartLabel,
          title: copy.dashboardActionCartTitle,
          description: formatResource(copy.dashboardActionCartDescription, {
            count: storefrontCart?.items.length ?? 0,
            total: storefrontCart
              ? formatMoney(
                  storefrontCart.grandTotalGrossMinor,
                  storefrontCart.currency,
                  culture,
                )
              : copy.unavailable,
          }),
          href: localizeHref(
            addresses.length > 0 ? "/checkout" : "/cart",
            culture,
          ),
          cta: addresses.length > 0
            ? copy.dashboardActionCartCheckoutCta
            : copy.dashboardActionCartReviewCta,
        }
      : null,
    !profile?.phoneNumberConfirmed
      ? {
          id: "phone-verification",
          label: copy.dashboardActionPhoneLabel,
          title: copy.dashboardActionPhoneTitle,
          description: copy.dashboardActionPhoneDescription,
          href: localizeHref("/account/profile", culture),
          cta: copy.dashboardActionPhoneCta,
        }
      : null,
    sessionNeedsAttention
      ? {
          id: "security-session",
          label: copy.dashboardActionSecurityLabel,
          title: copy.dashboardActionSecurityTitle,
          description:
            !hasValidSessionExpiry
              ? copy.dashboardActionSecurityInvalidSessionDescription
              : copy.dashboardActionSecurityDescription,
          href: localizeHref("/account/security", culture),
          cta: copy.dashboardActionSecurityCta,
        }
      : null,
    addresses.length === 0
      ? {
          id: "address-book",
          label: copy.dashboardActionAddressLabel,
          title: copy.dashboardActionAddressTitle,
          description: copy.dashboardActionAddressDescription,
          href: localizeHref("/account/addresses", culture),
          cta: copy.dashboardActionAddressCta,
        }
      : null,
    invoiceAttention
      ? {
          id: "invoice-balance",
          label: copy.dashboardActionInvoiceLabel,
          title: copy.dashboardActionInvoiceTitle,
          description: formatResource(copy.dashboardActionInvoiceDescription, {
            value: formatMoney(
              invoiceAttention.balanceMinor,
              invoiceAttention.currency,
              culture,
            ),
          }),
          href: localizeHref(`/invoices/${invoiceAttention.id}`, culture),
          cta: copy.dashboardActionInvoiceCta,
        }
      : null,
    loyaltyFocusAccounts[0]
      ? {
          id: "loyalty-reward",
          label: copy.dashboardActionLoyaltyLabel,
          title: copy.dashboardActionLoyaltyTitle,
          description: formatResource(copy.dashboardActionLoyaltyDescription, {
            business: loyaltyFocusAccounts[0].businessName,
            points:
              loyaltyFocusAccounts[0].pointsToNextReward?.toString() ??
              copy.unavailable,
          }),
          href: localizeHref(
            `/loyalty/${loyaltyFocusAccounts[0].businessId}`,
            culture,
          ),
          cta: copy.dashboardActionLoyaltyCta,
        }
      : null,
  ].filter((item): item is DashboardActionItem => item !== null);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <nav
          aria-label={copy.memberBreadcrumbLabel}
          className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
        >
          <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.memberBreadcrumbHome}
          </Link>
          <span>/</span>
          <span className="font-medium text-[var(--color-text-primary)]">
            {copy.memberBreadcrumbAccount}
          </span>
        </nav>

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
            <MemberPortalNav culture={culture} activePath="/account" />

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.memberRouteSummaryTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.memberRouteSummaryMessage, {
                  profileStatus,
                  preferencesStatus,
                  customerContextStatus,
                })}
              </p>
              <div className="mt-5 flex flex-wrap gap-3">
                <Link
                  href={localizeHref("/account/profile", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {copy.memberRouteSummaryProfileCta}
                </Link>
                <Link
                  href={localizeHref("/account/preferences", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {copy.memberRouteSummaryPreferencesCta}
                </Link>
                <Link
                  href={localizeHref("/account/security", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {copy.memberRouteSummarySecurityCta}
                </Link>
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

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.dashboardStorefrontWindowTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.dashboardStorefrontWindowMessage, {
                  cmsStatus: cmsPagesStatus,
                  categoriesStatus,
                  pageCount: cmsPages.length,
                  categoryCount: categories.length,
                })}
              </p>
              <div className="mt-5 grid gap-4 lg:grid-cols-2">
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.dashboardStorefrontCmsTitle}
                    </p>
                    <Link
                      href={localizeHref("/cms", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.dashboardStorefrontCmsCta}
                    </Link>
                  </div>
                  {cmsPages.length > 0 ? (
                    <div className="mt-4 flex flex-col gap-3 text-sm text-[var(--color-text-secondary)]">
                      {cmsPages.map((page) => (
                        <Link
                          key={page.id}
                          href={localizeHref(`/cms/${page.slug}`, culture)}
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          <p className="font-semibold text-[var(--color-text-primary)]">
                            {page.title}
                          </p>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {page.metaDescription ?? copy.dashboardStorefrontCmsFallbackDescription}
                          </p>
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.dashboardStorefrontCmsEmptyMessage, {
                        status: cmsPagesStatus,
                      })}
                    </p>
                  )}
                </div>

                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.dashboardStorefrontCatalogTitle}
                    </p>
                    <Link
                      href={localizeHref("/catalog", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.dashboardStorefrontCatalogCta}
                    </Link>
                  </div>
                  {categories.length > 0 ? (
                    <div className="mt-4 flex flex-col gap-3 text-sm text-[var(--color-text-secondary)]">
                      {categories.map((category) => (
                        <Link
                          key={category.id}
                          href={localizeHref(
                            buildAppQueryPath("/catalog", {
                              category: category.slug,
                            }),
                            culture,
                          )}
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          <p className="font-semibold text-[var(--color-text-primary)]">
                            {category.name}
                          </p>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {category.description ?? copy.dashboardStorefrontCatalogFallbackDescription}
                          </p>
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.dashboardStorefrontCatalogEmptyMessage, {
                        status: categoriesStatus,
                      })}
                    </p>
                  )}
                </div>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.dashboardCommunicationWindowTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.dashboardCommunicationWindowMessage, {
                  profileStatus,
                  preferencesStatus,
                })}
              </p>
              <div className="mt-5 grid gap-3 sm:grid-cols-3">
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                    {copy.dashboardCommunicationEmailLabel}
                  </p>
                  <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                    {emailChannelReady
                      ? copy.dashboardCommunicationReady
                      : copy.dashboardCommunicationNeedsAttention}
                  </p>
                </div>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                    {copy.dashboardCommunicationSmsLabel}
                  </p>
                  <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                    {smsChannelReady
                      ? copy.dashboardCommunicationReady
                      : copy.dashboardCommunicationNeedsAttention}
                  </p>
                </div>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                    {copy.dashboardCommunicationWhatsAppLabel}
                  </p>
                  <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                    {whatsAppChannelReady
                      ? copy.dashboardCommunicationReady
                      : copy.dashboardCommunicationNeedsAttention}
                  </p>
                </div>
              </div>
              <div className="mt-5 flex flex-wrap gap-3">
                <Link
                  href={localizeHref("/account/preferences", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.dashboardCommunicationPreferencesCta}
                </Link>
                <Link
                  href={localizeHref("/account/profile", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.dashboardCommunicationProfileCta}
                </Link>
                <Link
                  href={localizeHref("/account/security", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.dashboardCommunicationSecurityCta}
                </Link>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.checkoutLaunchTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.checkoutLaunchMessage, {
                  addressesStatus,
                  count: addresses.length,
                })}
              </p>
              <div className="mt-5 flex flex-wrap gap-3">
                <Link
                  href={checkoutHref}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {preferredCheckoutAddress
                    ? copy.checkoutLaunchUseSavedAddressCta
                    : copy.checkoutLaunchOpenCheckoutCta}
                </Link>
                <Link
                  href={localizeHref("/account/addresses", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.checkoutLaunchManageAddressesCta}
                </Link>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.dashboardSecurityWindowTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.dashboardSecurityWindowMessage, {
                  phoneVerified: profile?.phoneNumberConfirmed ? copy.yes : copy.no,
                  expiresAt: formatDateTime(session.accessTokenExpiresAtUtc, culture),
                  state: securityState,
                })}
              </p>
              <div className="mt-5 grid gap-4 sm:grid-cols-3">
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                    {copy.dashboardSecurityPhoneLabel}
                  </p>
                  <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                    {profile?.phoneNumberConfirmed
                      ? copy.dashboardSecurityPhoneReady
                      : copy.dashboardSecurityPhonePending}
                  </p>
                </div>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                    {copy.dashboardSecuritySessionLabel}
                  </p>
                  <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                    {hasValidSessionExpiry
                      ? formatDateTime(session.accessTokenExpiresAtUtc, culture)
                      : copy.dashboardSecuritySessionUnavailable}
                  </p>
                </div>
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                    {copy.dashboardSecurityStateLabel}
                  </p>
                  <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                    {securityState}
                  </p>
                </div>
              </div>
              <div className="mt-5 flex flex-wrap gap-3">
                <Link
                  href={localizeHref("/account/security", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.dashboardSecurityOpenSecurityCta}
                </Link>
                <Link
                  href={localizeHref(
                    profile?.phoneNumberConfirmed ? "/account/preferences" : "/account/profile",
                    culture,
                  )}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {profile?.phoneNumberConfirmed
                    ? copy.dashboardSecurityOpenPreferencesCta
                    : copy.dashboardSecurityVerifyPhoneCta}
                </Link>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.dashboardStorefrontCartTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.dashboardStorefrontCartMessage, {
                  status: storefrontCartStatus,
                  count: storefrontCart?.items.length ?? 0,
                })}
              </p>
              {hasStorefrontCart && storefrontCart ? (
                <div className="mt-5 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                        {formatResource(copy.dashboardStorefrontCartSnapshotTitle, {
                          count: storefrontCart.items.length,
                        })}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {storefrontCart.couponCode
                          ? formatResource(copy.dashboardStorefrontCartCouponMessage, {
                              coupon: storefrontCart.couponCode,
                            })
                          : copy.dashboardStorefrontCartNoCouponMessage}
                      </p>
                    </div>
                    <div className="text-right">
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                        {copy.dashboardStorefrontCartGrossLabel}
                      </p>
                      <p className="mt-2 text-xl font-semibold text-[var(--color-text-primary)]">
                        {formatMoney(
                          storefrontCart.grandTotalGrossMinor,
                          storefrontCart.currency,
                          culture,
                        )}
                      </p>
                    </div>
                  </div>
                  <div className="mt-5 flex flex-wrap gap-3">
                    <Link
                      href={localizeHref("/cart", culture)}
                      className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                    >
                      {copy.dashboardStorefrontCartOpenCartCta}
                    </Link>
                    <Link
                      href={localizeHref("/checkout", culture)}
                      className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                    >
                      {copy.dashboardStorefrontCartOpenCheckoutCta}
                    </Link>
                  </div>
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.dashboardStorefrontCartEmptyMessage, {
                    status: storefrontCartStatus,
                  })}
                </p>
              )}
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.dashboardActionCenterTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.dashboardActionCenterMessage}
              </p>
              {actionItems.length > 0 ? (
                <div className="mt-5 flex flex-col gap-3">
                  {actionItems.map((item) => (
                    <Link
                      key={item.id}
                      href={item.href}
                      className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4 transition hover:bg-[var(--color-surface-panel)]"
                    >
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                        {item.label}
                      </p>
                      <div className="mt-3 flex items-start justify-between gap-4">
                        <div>
                          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                            {item.title}
                          </p>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {item.description}
                          </p>
                        </div>
                        <span className="shrink-0 text-sm font-semibold text-[var(--color-brand)]">
                          {item.cta}
                        </span>
                      </div>
                    </Link>
                  ))}
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.dashboardActionCenterEmptyMessage}
                </p>
              )}
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.dashboardCommerceWindowTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.dashboardCommerceWindowMessage, {
                  ordersStatus: recentOrdersStatus,
                  invoicesStatus: recentInvoicesStatus,
                  orderCount: recentOrders.length,
                  invoiceCount: recentInvoices.length,
                })}
              </p>

              <div className="mt-5 grid gap-4">
                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.dashboardRecentOrdersTitle}
                    </p>
                    <Link
                      href={localizeHref("/orders", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.dashboardOpenOrdersCta}
                    </Link>
                  </div>
                  {recentOrders.length > 0 ? (
                    <div className="mt-4 flex flex-col gap-3 text-sm text-[var(--color-text-secondary)]">
                      {recentOrders.map((order) => (
                        <Link
                          key={order.id}
                          href={localizeHref(`/orders/${order.id}`, culture)}
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          <div className="flex items-start justify-between gap-3">
                            <div>
                              <p className="font-semibold text-[var(--color-text-primary)]">
                                {order.orderNumber}
                              </p>
                              <p className="mt-1">
                                {formatDateTime(order.createdAtUtc, culture)}
                              </p>
                            </div>
                            <div className="text-right">
                              <p className="font-semibold text-[var(--color-text-primary)]">
                                {formatMoney(
                                  order.grandTotalGrossMinor,
                                  order.currency,
                                  culture,
                                )}
                              </p>
                              <p className="mt-1">{order.status}</p>
                            </div>
                          </div>
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.dashboardRecentOrdersEmptyMessage, {
                        status: recentOrdersStatus,
                      })}
                    </p>
                  )}
                </div>

                <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.dashboardRecentInvoicesTitle}
                    </p>
                    <Link
                      href={localizeHref("/invoices", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.dashboardOpenInvoicesCta}
                    </Link>
                  </div>
                  {recentInvoices.length > 0 ? (
                    <div className="mt-4 flex flex-col gap-3 text-sm text-[var(--color-text-secondary)]">
                      {recentInvoices.map((invoice) => (
                        <Link
                          key={invoice.id}
                          href={localizeHref(`/invoices/${invoice.id}`, culture)}
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          <div className="flex items-start justify-between gap-3">
                            <div>
                              <p className="font-semibold text-[var(--color-text-primary)]">
                                {invoice.orderNumber ?? invoice.id}
                              </p>
                              <p className="mt-1">
                                {formatDateTime(invoice.createdAtUtc, culture)}
                              </p>
                            </div>
                            <div className="text-right">
                              <p className="font-semibold text-[var(--color-text-primary)]">
                                {formatMoney(
                                  invoice.totalGrossMinor,
                                  invoice.currency,
                                  culture,
                                )}
                              </p>
                              <p className="mt-1">{invoice.status}</p>
                            </div>
                          </div>
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.dashboardRecentInvoicesEmptyMessage, {
                        status: recentInvoicesStatus,
                      })}
                    </p>
                  )}
                </div>
              </div>
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.dashboardLoyaltyWindowTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.dashboardLoyaltyWindowMessage, {
                  overviewStatus: loyaltyOverviewStatus,
                  businessesStatus: loyaltyBusinessesStatus,
                  accountCount: loyaltyOverview?.totalAccounts ?? 0,
                  visibleCount: loyaltyBusinesses.length,
                })}
              </p>

              {loyaltyOverview ? (
                <div className="mt-5 grid gap-4 sm:grid-cols-3">
                  <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                      {copy.totalAccountsLabel}
                    </p>
                    <p className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                      {loyaltyOverview.totalAccounts}
                    </p>
                  </div>
                  <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                      {copy.activeAccountsLabel}
                    </p>
                    <p className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                      {loyaltyOverview.activeAccounts}
                    </p>
                  </div>
                  <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                      {copy.pointsBalanceLabel}
                    </p>
                    <p className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                      {loyaltyOverview.totalPointsBalance}
                    </p>
                  </div>
                </div>
              ) : null}

              <div className="mt-5 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <div className="flex items-center justify-between gap-3">
                  <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                    {copy.dashboardJoinedPlacesTitle}
                  </p>
                  <Link
                    href={localizeHref("/loyalty", culture)}
                    className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                  >
                    {copy.dashboardOpenLoyaltyCta}
                  </Link>
                </div>
                {loyaltyBusinesses.length > 0 ? (
                  <div className="mt-4 flex flex-col gap-3">
                    {loyaltyBusinesses.map((business) => {
                      const imageUrl = toWebApiUrl(business.primaryImageUrl ?? "");
                      return (
                        <Link
                          key={business.businessId}
                          href={localizeHref(`/loyalty/${business.businessId}`, culture)}
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          <div className="flex items-start gap-4">
                            <div className="flex h-14 w-14 shrink-0 items-center justify-center overflow-hidden rounded-2xl bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))]">
                              {imageUrl ? (
                                // eslint-disable-next-line @next/next/no-img-element
                                <img
                                  src={imageUrl}
                                  alt={business.businessName}
                                  className="h-full w-full object-contain p-2"
                                />
                              ) : (
                                <span className="px-2 text-center text-[10px] font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                                  {copy.memberBreadcrumbLoyalty}
                                </span>
                              )}
                            </div>
                            <div className="min-w-0 flex-1 text-sm text-[var(--color-text-secondary)]">
                              <div className="flex items-start justify-between gap-3">
                                <div className="min-w-0">
                                  <p className="truncate font-semibold text-[var(--color-text-primary)]">
                                    {business.businessName}
                                  </p>
                                  <p className="mt-1 truncate">
                                    {[business.category, business.city].filter(Boolean).join(" · ")}
                                  </p>
                                </div>
                                <span className="rounded-full bg-[var(--color-surface-panel-strong)] px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-primary)]">
                                  {business.status}
                                </span>
                              </div>
                              <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1">
                                <span>
                                  {copy.pointsBalanceLabel}: {business.pointsBalance}
                                </span>
                                <span>
                                  {copy.lifetimePointsLabel}: {business.lifetimePoints}
                                </span>
                                {business.lastAccrualAtUtc ? (
                                  <span>
                                    {formatResource(copy.lastAccrualLabel, {
                                      value: formatDateTime(business.lastAccrualAtUtc, culture),
                                    })}
                                  </span>
                                ) : null}
                              </div>
                            </div>
                          </div>
                        </Link>
                      );
                    })}
                  </div>
                ) : (
                  <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {formatResource(copy.dashboardJoinedPlacesEmptyMessage, {
                      status: loyaltyBusinessesStatus,
                    })}
                  </p>
                )}
              </div>

              <div className="mt-4 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <div className="flex items-center justify-between gap-3">
                  <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                    {copy.dashboardRewardFocusTitle}
                  </p>
                  <Link
                    href={localizeHref("/loyalty", culture)}
                    className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                  >
                    {copy.dashboardRewardFocusCta}
                  </Link>
                </div>
                {loyaltyFocusAccounts.length > 0 ? (
                  <div className="mt-4 flex flex-col gap-3">
                    {loyaltyFocusAccounts.map((account) => (
                      <Link
                        key={account.loyaltyAccountId}
                        href={localizeHref(`/loyalty/${account.businessId}`, culture)}
                        className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="min-w-0">
                            <p className="truncate font-semibold text-[var(--color-text-primary)]">
                              {account.businessName}
                            </p>
                            <p className="mt-1 text-sm text-[var(--color-text-secondary)]">
                              {formatResource(copy.nextRewardLabel, {
                                value: account.nextRewardTitle ?? copy.noNextRewardPublished,
                              })}
                            </p>
                          </div>
                          <span className="rounded-full bg-[var(--color-surface-panel-strong)] px-3 py-1 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-primary)]">
                            {account.status}
                          </span>
                        </div>
                        <div className="mt-3 flex flex-wrap gap-x-4 gap-y-1 text-sm text-[var(--color-text-secondary)]">
                          <span>
                            {copy.pointsBalanceLabel}: {account.pointsBalance}
                          </span>
                          <span>
                            {formatResource(copy.pointsToNextRewardLabel, {
                              value:
                                account.pointsToNextReward?.toString() ??
                                copy.unavailable,
                            })}
                          </span>
                          {account.lastAccrualAtUtc ? (
                            <span>
                              {formatResource(copy.lastAccrualLabel, {
                                value: formatDateTime(account.lastAccrualAtUtc, culture),
                              })}
                            </span>
                          ) : null}
                        </div>
                      </Link>
                    ))}
                  </div>
                ) : (
                  <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {formatResource(copy.dashboardRewardFocusEmptyMessage, {
                      status: loyaltyOverviewStatus,
                    })}
                  </p>
                )}
              </div>
            </div>

            <MemberCrossSurfaceRail
              culture={culture}
              includeOrders
              includeInvoices
              includeLoyalty
            />
          </div>
        </div>
      </div>
    </section>
  );
}
