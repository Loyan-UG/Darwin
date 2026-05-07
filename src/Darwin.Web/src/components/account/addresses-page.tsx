import Link from "next/link";
import { AccountContentCompositionWindow } from "@/components/account/account-content-composition-window";
import { AccountStorefrontWindow } from "@/components/account/account-storefront-window";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import { SurfaceSectionNav } from "@/components/layout/surface-section-nav";
import { buildCheckoutDraftSearch, toCheckoutDraftFromMemberAddress } from "@/features/checkout/helpers";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import {
  createMemberAddressAction,
  deleteMemberAddressAction,
  setMemberAddressDefaultAction,
  updateMemberAddressAction,
} from "@/features/member-portal/actions";
import type { PublicPageSummary } from "@/features/cms/types";
import type { MemberAddress } from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveApiStatusLabel,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { localizeHref } from "@/lib/locale-routing";

type AddressesPageProps = {
  culture: string;
  addresses: MemberAddress[];
  status: string;
  addressesStatus?: string;
  addressesError?: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
};

function getAddressesStatusMessage(
  status: string | undefined,
  copy: ReturnType<typeof getMemberResource>,
) {
  if (matchesLocalizedQueryMessageKey(status, "addressCreatedMessage", "created")) {
      return copy.addressCreatedMessage;
  }
  if (matchesLocalizedQueryMessageKey(status, "addressUpdatedMessage", "updated")) {
      return copy.addressUpdatedMessage;
  }
  if (matchesLocalizedQueryMessageKey(status, "addressDeletedMessage", "deleted")) {
      return copy.addressDeletedMessage;
  }
  if (
    matchesLocalizedQueryMessageKey(
      status,
      "addressDefaultUpdatedMessage",
      "default-updated",
    )
  ) {
      return copy.addressDefaultUpdatedMessage;
  }

  return undefined;
}

export function AddressesPage({
  culture,
  addresses,
  status,
  addressesStatus,
  addressesError,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
}: AddressesPageProps) {
  const copy = getMemberResource(culture);
  const resolvedAddressesError = resolveLocalizedQueryMessage(addressesError, copy);
  const statusMessage = getAddressesStatusMessage(addressesStatus, copy);
  const localizedStatus = resolveApiStatusLabel(status, copy) ?? status;
  const defaultShippingAddress =
    addresses.find((address) => address.isDefaultShipping) ?? null;
  const defaultBillingAddress =
    addresses.find((address) => address.isDefaultBilling) ?? null;
  const preferredCheckoutAddress =
    defaultShippingAddress ??
    defaultBillingAddress ??
    addresses[0] ??
    null;
  const checkoutHref = preferredCheckoutAddress
    ? localizeHref(
        `/checkout${buildCheckoutDraftSearch(
          toCheckoutDraftFromMemberAddress(preferredCheckoutAddress),
          { memberAddressId: preferredCheckoutAddress.id },
        )}`,
        culture,
      )
    : localizeHref("/checkout", culture);
  const sectionLinks = [
    { href: "#addresses-create", label: copy.createAddressEyebrow },
    { href: "#addresses-readiness", label: copy.addressesReadinessTitle },
    { href: "#addresses-composition", label: copy.accountCompositionJourneyAddressesTitle },
    { href: "#addresses-saved", label: copy.savedAddressLabel },
  ];

  return (
    <section className="mx-auto flex w-full max-w-[1320px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
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
                <span className="text-[var(--color-text-primary)]">{copy.addressesRouteLabel}</span>
              </nav>
              <p className="mt-4 text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                {copy.addressesEyebrow}
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {copy.addressesTitle}
              </h1>
              <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {formatResource(copy.addressesReadinessMessage, {
                  count: addresses.length,
                  shipping: defaultShippingAddress ? copy.yes : copy.no,
                  billing: defaultBillingAddress ? copy.yes : copy.no,
                })}
              </p>
              <div className="mt-6 flex flex-wrap gap-3">
                <Link
                  href={checkoutHref}
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {preferredCheckoutAddress
                    ? copy.addressesCheckoutUseSavedCta
                    : copy.addressesCheckoutOpenCta}
                </Link>
                <Link
                  href={localizeHref("/account/profile", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] bg-white/85 px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-white"
                >
                  {copy.memberRouteSummaryProfileCta}
                </Link>
              </div>
            </div>
            <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-1">
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.addressesReadinessCountLabel}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {addresses.length}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-white/80 px-5 py-4 shadow-[0_20px_40px_-28px_rgba(58,92,35,0.45)] backdrop-blur">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.addressesReadinessShippingLabel}
                </p>
                <p className="mt-2 text-base font-semibold text-[var(--color-text-primary)]">
                  {defaultShippingAddress
                    ? copy.addressesReadinessReady
                    : copy.addressesReadinessMissing}
                </p>
              </article>
              <article className="rounded-[1rem] border border-white/70 bg-[linear-gradient(135deg,rgba(57,116,47,0.94),rgba(255,145,77,0.92))] px-5 py-4 text-white shadow-[0_20px_40px_-28px_rgba(58,92,35,0.55)]">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-white/78">
                  {copy.addressesReadinessBillingLabel}
                </p>
                <p className="mt-2 text-base font-semibold text-white">
                  {defaultBillingAddress
                    ? copy.addressesReadinessReady
                    : copy.addressesReadinessMissing}
                </p>
              </article>
            </div>
          </div>
        </div>

        <SurfaceSectionNav items={sectionLinks} />

        {statusMessage && (
          <StatusBanner title={copy.addressBookUpdatedTitle} message={statusMessage} />
        )}

        {(resolvedAddressesError || status !== "ok") && (
          <StatusBanner
            tone="warning"
            title={copy.addressBookWarningsTitle}
            message={
              resolvedAddressesError ??
              formatResource(copy.addressBookWarningsMessage, { status: localizedStatus })
            }
          />
        )}

        <form
          action={createMemberAddressAction}
          id="addresses-create"
          className="scroll-mt-28 rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#f7fbef_100%)] px-6 py-8 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.createAddressEyebrow}
          </p>
          <div className="mt-6 grid gap-4 sm:grid-cols-2">
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.fullNameLabelBare}</span><input name="fullName" required autoComplete="name" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.companyLabelBare}</span><input name="company" autoComplete="organization" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>{copy.street1LabelBare}</span><input name="street1" required autoComplete="address-line1" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>{copy.street2LabelBare}</span><input name="street2" autoComplete="address-line2" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.postalCodeLabelBare}</span><input name="postalCode" required autoComplete="postal-code" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.cityLabelBare}</span><input name="city" required autoComplete="address-level2" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.stateLabelBare}</span><input name="state" autoComplete="address-level1" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.countryCodeLabelBare}</span><input name="countryCode" defaultValue="DE" required maxLength={2} autoComplete="country" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal uppercase outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>{copy.phoneShortLabel}</span><input name="phoneE164" autoComplete="tel" inputMode="tel" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex items-center gap-3 text-sm font-medium text-[var(--color-text-primary)]"><input type="checkbox" name="isDefaultBilling" /> {copy.defaultBillingLabel}</label>
            <label className="flex items-center gap-3 text-sm font-medium text-[var(--color-text-primary)]"><input type="checkbox" name="isDefaultShipping" /> {copy.defaultShippingLabel}</label>
          </div>
          <button type="submit" className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
            {copy.addAddressCta}
          </button>
        </form>

        <div className="grid gap-5">
          <MemberPortalNav culture={culture} activePath="/account/addresses" />

          <aside id="addresses-readiness" className="scroll-mt-28 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.addressesRouteSummaryTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.addressesRouteSummaryMessage, {
                status: localizedStatus,
                count: addresses.length,
              })}
            </p>
            <div className="mt-5 flex flex-wrap gap-3">
              <Link href={localizeHref("/account/profile", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberRouteSummaryProfileCta}
              </Link>
              <Link href={localizeHref("/account/preferences", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.memberRouteSummaryPreferencesCta}
              </Link>
              <Link href={checkoutHref} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {preferredCheckoutAddress
                  ? copy.addressesCheckoutUseSavedCta
                  : copy.addressesCheckoutOpenCta}
              </Link>
            </div>
          </aside>

          <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.addressesReadinessTitle}
            </p>
            <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.addressesReadinessMessage, {
                count: addresses.length,
                shipping: defaultShippingAddress ? copy.yes : copy.no,
                billing: defaultBillingAddress ? copy.yes : copy.no,
              })}
            </p>
            <div className="mt-5 grid gap-3 sm:grid-cols-3">
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                  {copy.addressesReadinessCountLabel}
                </p>
                <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                  {addresses.length}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                  {copy.addressesReadinessShippingLabel}
                </p>
                <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                  {defaultShippingAddress
                    ? copy.addressesReadinessReady
                    : copy.addressesReadinessMissing}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                  {copy.addressesReadinessBillingLabel}
                </p>
                <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                  {defaultBillingAddress
                    ? copy.addressesReadinessReady
                    : copy.addressesReadinessMissing}
                </p>
              </div>
            </div>
            <div className="mt-5 flex flex-wrap gap-3">
              <Link href={checkoutHref} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {preferredCheckoutAddress
                  ? copy.addressesCheckoutUseSavedCta
                  : copy.addressesCheckoutOpenCta}
              </Link>
              <Link href={localizeHref("/account", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                {copy.securityBackToDashboardCta}
              </Link>
            </div>
          </aside>

          <MemberCrossSurfaceRail
            culture={culture}
            includeAccount={false}
            includeOrders
            includeLoyalty={false}
          />

          <div id="addresses-composition" className="scroll-mt-28">
            <AccountContentCompositionWindow
              culture={culture}
              routeCard={{
                label: copy.accountCompositionJourneyCurrentLabel,
                title: copy.accountCompositionJourneyAddressesTitle,
                description: formatResource(copy.accountCompositionJourneyAddressesRouteDescription, {
                  count: addresses.length,
                }),
                href: "/account/addresses",
                ctaLabel: copy.accountCompositionJourneyCurrentCta,
              }}
              nextCard={{
                label: copy.accountCompositionJourneyNextLabel,
                title: preferredCheckoutAddress
                  ? copy.accountCompositionJourneyCheckoutReadyTitle
                  : copy.accountCompositionJourneyCheckoutSetupTitle,
                description: preferredCheckoutAddress
                  ? copy.accountCompositionJourneyCheckoutReadyDescription
                  : copy.accountCompositionJourneyCheckoutSetupDescription,
                href: preferredCheckoutAddress
                  ? `/checkout${buildCheckoutDraftSearch(
                      toCheckoutDraftFromMemberAddress(preferredCheckoutAddress),
                      { memberAddressId: preferredCheckoutAddress.id },
                    )}`
                  : "/checkout",
                ctaLabel: copy.accountCompositionJourneyCheckoutCta,
              }}
              routeMapItems={[
                {
                  label: copy.accountCompositionRouteMapAddressesLabel,
                  title: copy.accountCompositionRouteMapAddressesTitle,
                  description: formatResource(copy.accountCompositionRouteMapAddressesRouteDescription, {
                    count: addresses.length,
                  }),
                  href: "/account/addresses",
                  ctaLabel: copy.accountCompositionRouteMapAddressesCta,
                },
                {
                  label: copy.accountCompositionRouteMapNextLabel,
                  title: preferredCheckoutAddress
                    ? copy.accountCompositionRouteMapCheckoutReadyTitle
                    : copy.accountCompositionRouteMapCheckoutSetupTitle,
                  description: preferredCheckoutAddress
                    ? copy.accountCompositionRouteMapCheckoutReadyDescription
                    : copy.accountCompositionRouteMapCheckoutSetupDescription,
                  href: preferredCheckoutAddress
                    ? `/checkout${buildCheckoutDraftSearch(
                        toCheckoutDraftFromMemberAddress(preferredCheckoutAddress),
                        { memberAddressId: preferredCheckoutAddress.id },
                      )}`
                    : "/checkout",
                  ctaLabel: copy.accountCompositionRouteMapCheckoutCta,
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

          <div id="addresses-saved" className="scroll-mt-28 grid gap-5">
          {addresses.map((address) => (
            <article key={address.id} className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {address.isDefaultBilling ? copy.defaultBillingLabel : address.isDefaultShipping ? copy.defaultShippingLabel : copy.savedAddressLabel}
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <Link
                    href={localizeHref(
                      `/checkout${buildCheckoutDraftSearch(
                        toCheckoutDraftFromMemberAddress(address),
                        { memberAddressId: address.id },
                      )}`,
                      culture,
                    )}
                    className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.addressesUseForCheckoutCta}
                  </Link>
                  <form action={setMemberAddressDefaultAction}>
                    <input type="hidden" name="id" value={address.id} />
                    <input type="hidden" name="asBilling" value="true" />
                    <button type="submit" className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                      {copy.setBillingCta}
                    </button>
                  </form>
                  <form action={setMemberAddressDefaultAction}>
                    <input type="hidden" name="id" value={address.id} />
                    <input type="hidden" name="asShipping" value="true" />
                    <button type="submit" className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                      {copy.setShippingCta}
                    </button>
                  </form>
                  <form action={deleteMemberAddressAction}>
                    <input type="hidden" name="id" value={address.id} />
                    <input type="hidden" name="rowVersion" value={address.rowVersion} />
                    <button type="submit" className="rounded-full border border-[rgba(217,111,50,0.2)] px-4 py-2 text-sm font-semibold text-[var(--color-accent)] transition hover:bg-[rgba(217,111,50,0.08)]">
                      {copy.deleteCta}
                    </button>
                  </form>
                </div>
              </div>
              <form action={updateMemberAddressAction} className="mt-6">
                <input type="hidden" name="id" value={address.id} />
                <input type="hidden" name="rowVersion" value={address.rowVersion} />
                <div className="grid gap-4 sm:grid-cols-2">
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.fullNameLabelBare}</span><input name="fullName" required autoComplete="name" defaultValue={address.fullName} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.companyLabelBare}</span><input name="company" autoComplete="organization" defaultValue={address.company ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>{copy.street1LabelBare}</span><input name="street1" required autoComplete="address-line1" defaultValue={address.street1} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>{copy.street2LabelBare}</span><input name="street2" autoComplete="address-line2" defaultValue={address.street2 ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.postalCodeLabelBare}</span><input name="postalCode" required autoComplete="postal-code" defaultValue={address.postalCode} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.cityLabelBare}</span><input name="city" required autoComplete="address-level2" defaultValue={address.city} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.stateLabelBare}</span><input name="state" autoComplete="address-level1" defaultValue={address.state ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>{copy.countryCodeLabelBare}</span><input name="countryCode" required maxLength={2} autoComplete="country" defaultValue={address.countryCode} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal uppercase outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>{copy.phoneShortLabel}</span><input name="phoneE164" autoComplete="tel" inputMode="tel" defaultValue={address.phoneE164 ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex items-center gap-3 text-sm font-medium text-[var(--color-text-primary)]"><input type="checkbox" name="isDefaultBilling" defaultChecked={address.isDefaultBilling} /> {copy.defaultBillingLabel}</label>
                  <label className="flex items-center gap-3 text-sm font-medium text-[var(--color-text-primary)]"><input type="checkbox" name="isDefaultShipping" defaultChecked={address.isDefaultShipping} /> {copy.defaultShippingLabel}</label>
                </div>
                <button type="submit" className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                  {copy.saveAddressCta}
                </button>
              </form>
            </article>
          ))}
          </div>
        </div>

        {addresses.length === 0 && (
          <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.noSavedAddressesMessage}
            </p>
            <div className="mt-8 text-left">
              <MemberCrossSurfaceRail
                culture={culture}
                includeAccount={false}
                includeOrders
                includeLoyalty={false}
              />
            </div>
          </div>
        )}
      </div>
    </section>
  );
}
