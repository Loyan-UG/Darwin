import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import {
  createMemberAddressAction,
  deleteMemberAddressAction,
  setMemberAddressDefaultAction,
  updateMemberAddressAction,
} from "@/features/member-portal/actions";
import type { MemberAddress } from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  resolveLocalizedQueryMessage,
} from "@/localization";

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
  switch (status) {
    case "created":
      return copy.addressCreatedMessage;
    case "updated":
      return copy.addressUpdatedMessage;
    case "deleted":
      return copy.addressDeletedMessage;
    case "default-updated":
      return copy.addressDefaultUpdatedMessage;
    default:
      return undefined;
  }
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

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.addressesEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.addressesTitle}
          </h1>
        </div>

        {statusMessage && (
          <StatusBanner title={copy.addressBookUpdatedTitle} message={statusMessage} />
        )}

        {(resolvedAddressesError || status !== "ok") && (
          <StatusBanner
            tone="warning"
            title={copy.addressBookWarningsTitle}
            message={
              resolvedAddressesError ??
              formatResource(copy.addressBookWarningsMessage, { status })
            }
          />
        )}

        <form
          action={createMemberAddressAction}
          className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
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

          {addresses.map((address) => (
            <article key={address.id} className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {address.isDefaultBilling ? copy.defaultBillingLabel : address.isDefaultShipping ? copy.defaultShippingLabel : copy.savedAddressLabel}
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
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

        {addresses.length === 0 && (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.noSavedAddressesMessage}
          </div>
        )}
      </div>
    </section>
  );
}
