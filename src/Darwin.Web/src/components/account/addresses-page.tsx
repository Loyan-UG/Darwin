import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { buildCheckoutDraftSearch, toCheckoutDraftFromMemberAddress } from "@/features/checkout/helpers";
import {
  createMemberAddressAction,
  deleteMemberAddressAction,
  setMemberAddressDefaultAction,
  updateMemberAddressAction,
} from "@/features/member-portal/actions";
import type { MemberAddress } from "@/features/member-portal/types";
import {
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { localizeHref } from "@/lib/locale-routing";

type AddressesPageProps = {
  culture: string;
  addresses: MemberAddress[];
  status: string;
  addressesStatus?: string;
  addressesError?: string;
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

function buildCheckoutHref(address: MemberAddress, culture: string) {
  return localizeHref(
    `/checkout${buildCheckoutDraftSearch(
      toCheckoutDraftFromMemberAddress(address),
      { memberAddressId: address.id },
    )}`,
    culture,
  );
}

function AddressFields({
  copy,
  address,
}: {
  copy: ReturnType<typeof getMemberResource>;
  address?: MemberAddress;
}) {
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
        <span>{copy.fullNameLabelBare}</span>
        <input
          name="fullName"
          required
          autoComplete="name"
          defaultValue={address?.fullName ?? ""}
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
        />
      </label>
      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
        <span>{copy.companyLabelBare}</span>
        <input
          name="company"
          autoComplete="organization"
          defaultValue={address?.company ?? ""}
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
        />
      </label>
      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
        <span>{copy.street1LabelBare}</span>
        <input
          name="street1"
          required
          autoComplete="address-line1"
          defaultValue={address?.street1 ?? ""}
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
        />
      </label>
      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
        <span>{copy.street2LabelBare}</span>
        <input
          name="street2"
          autoComplete="address-line2"
          defaultValue={address?.street2 ?? ""}
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
        />
      </label>
      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
        <span>{copy.postalCodeLabelBare}</span>
        <input
          name="postalCode"
          required
          autoComplete="postal-code"
          defaultValue={address?.postalCode ?? ""}
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
        />
      </label>
      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
        <span>{copy.cityLabelBare}</span>
        <input
          name="city"
          required
          autoComplete="address-level2"
          defaultValue={address?.city ?? ""}
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
        />
      </label>
      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
        <span>{copy.stateLabelBare}</span>
        <input
          name="state"
          autoComplete="address-level1"
          defaultValue={address?.state ?? ""}
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
        />
      </label>
      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
        <span>{copy.countryCodeLabelBare}</span>
        <input
          name="countryCode"
          defaultValue={address?.countryCode ?? "DE"}
          required
          maxLength={2}
          autoComplete="country"
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal uppercase outline-none"
        />
      </label>
      <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
        <span>{copy.phoneShortLabel}</span>
        <input
          name="phoneE164"
          autoComplete="tel"
          inputMode="tel"
          defaultValue={address?.phoneE164 ?? ""}
          className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none"
        />
      </label>
      <label className="flex items-center gap-3 text-sm font-medium text-[var(--color-text-primary)]">
        <input type="checkbox" name="isDefaultBilling" defaultChecked={address?.isDefaultBilling ?? false} />{" "}
        {copy.defaultBillingLabel}
      </label>
      <label className="flex items-center gap-3 text-sm font-medium text-[var(--color-text-primary)]">
        <input type="checkbox" name="isDefaultShipping" defaultChecked={address?.isDefaultShipping ?? false} />{" "}
        {copy.defaultShippingLabel}
      </label>
    </div>
  );
}

export function AddressesPage({
  culture,
  addresses,
  status,
  addressesStatus,
  addressesError,
}: AddressesPageProps) {
  const copy = getMemberResource(culture);
  const resolvedAddressesError = resolveLocalizedQueryMessage(addressesError, copy);
  const statusMessage = getAddressesStatusMessage(addressesStatus, copy);
  const defaultShippingAddress =
    addresses.find((address) => address.isDefaultShipping) ?? null;
  const defaultBillingAddress =
    addresses.find((address) => address.isDefaultBilling) ?? null;
  const preferredCheckoutAddress =
    defaultShippingAddress ?? defaultBillingAddress ?? addresses[0] ?? null;
  const checkoutHref = preferredCheckoutAddress
    ? buildCheckoutHref(preferredCheckoutAddress, culture)
    : localizeHref("/checkout", culture);

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
              {copy.addressesRouteLabel}
            </span>
          </nav>

          <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
              {copy.addressesEyebrow}
            </p>
            <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
              {copy.addressesTitle}
            </h1>
            <p className="mt-4 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.memberAddressesIntroMessage}
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
                {copy.memberOpenProfileCta}
              </Link>
            </div>
          </header>

          {statusMessage ? (
            <StatusBanner title={copy.addressBookUpdatedTitle} message={statusMessage} />
          ) : null}

          {(resolvedAddressesError || status !== "ok") ? (
            <StatusBanner
              tone="warning"
              title={copy.addressBookWarningsTitle}
              message={resolvedAddressesError ?? copy.noSavedAddressesMessage}
            />
          ) : null}

          <form
            action={createMemberAddressAction}
            className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)] sm:p-8"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.createAddressEyebrow}
            </p>
            <div className="mt-6">
              <AddressFields copy={copy} />
            </div>
            <button
              type="submit"
              className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.addAddressCta}
            </button>
          </form>

          <div className="grid gap-5">
            {addresses.length === 0 ? (
              <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
                <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.noSavedAddressesMessage}
                </p>
              </div>
            ) : null}

            {addresses.map((address) => (
              <article
                key={address.id}
                className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
              >
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                      {address.isDefaultBilling
                        ? copy.defaultBillingLabel
                        : address.isDefaultShipping
                          ? copy.defaultShippingLabel
                          : copy.savedAddressLabel}
                    </p>
                    <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {address.fullName}
                      <br />
                      {address.street1}
                      <br />
                      {address.postalCode} {address.city}
                    </p>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Link
                      href={buildCheckoutHref(address, culture)}
                      className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      {copy.addressesUseForCheckoutCta}
                    </Link>
                    <form action={setMemberAddressDefaultAction}>
                      <input type="hidden" name="id" value={address.id} />
                      <input type="hidden" name="asBilling" value="true" />
                      <button
                        type="submit"
                        className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                      >
                        {copy.setBillingCta}
                      </button>
                    </form>
                    <form action={setMemberAddressDefaultAction}>
                      <input type="hidden" name="id" value={address.id} />
                      <input type="hidden" name="asShipping" value="true" />
                      <button
                        type="submit"
                        className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                      >
                        {copy.setShippingCta}
                      </button>
                    </form>
                    <form action={deleteMemberAddressAction}>
                      <input type="hidden" name="id" value={address.id} />
                      <input type="hidden" name="rowVersion" value={address.rowVersion} />
                      <button
                        type="submit"
                        className="rounded-full border border-[rgba(217,111,50,0.2)] px-4 py-2 text-sm font-semibold text-[var(--color-accent)] transition hover:bg-[rgba(217,111,50,0.08)]"
                      >
                        {copy.deleteCta}
                      </button>
                    </form>
                  </div>
                </div>
                <form action={updateMemberAddressAction} className="mt-6">
                  <input type="hidden" name="id" value={address.id} />
                  <input type="hidden" name="rowVersion" value={address.rowVersion} />
                  <AddressFields copy={copy} address={address} />
                  <button
                    type="submit"
                    className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                  >
                    {copy.saveAddressCta}
                  </button>
                </form>
              </article>
            ))}
          </div>
        </div>

        <aside className="flex flex-col gap-5">
          <MemberPortalNav culture={culture} activePath="/account/addresses" />
          <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.savedAddressLabel}
            </p>
            <div className="mt-4 space-y-3 text-sm text-[var(--color-text-secondary)]">
              <div className="flex items-center justify-between gap-4">
                <span>{copy.memberAddressesCountLabel}</span>
                <span>{addresses.length}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.memberAddressesShippingLabel}</span>
                <span>{defaultShippingAddress ? copy.yes : copy.no}</span>
              </div>
              <div className="flex items-center justify-between gap-4">
                <span>{copy.memberAddressesBillingLabel}</span>
                <span>{defaultBillingAddress ? copy.yes : copy.no}</span>
              </div>
            </div>
          </section>
        </aside>
      </div>
    </section>
  );
}
