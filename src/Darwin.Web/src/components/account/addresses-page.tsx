import { StatusBanner } from "@/components/feedback/status-banner";
import {
  createMemberAddressAction,
  deleteMemberAddressAction,
  setMemberAddressDefaultAction,
  updateMemberAddressAction,
} from "@/features/member-portal/actions";
import type { MemberAddress } from "@/features/member-portal/types";

type AddressesPageProps = {
  addresses: MemberAddress[];
  status: string;
  addressesStatus?: string;
  addressesError?: string;
};

function getAddressesStatusMessage(status?: string) {
  switch (status) {
    case "created":
      return "Address created.";
    case "updated":
      return "Address updated.";
    case "deleted":
      return "Address deleted.";
    case "default-updated":
      return "Default address updated.";
    default:
      return undefined;
  }
}

export function AddressesPage({
  addresses,
  status,
  addressesStatus,
  addressesError,
}: AddressesPageProps) {
  const statusMessage = getAddressesStatusMessage(addressesStatus);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">Addresses</p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Manage reusable addresses
          </h1>
        </div>

        {statusMessage && (
          <StatusBanner title="Address book updated" message={statusMessage} />
        )}

        {(addressesError || status !== "ok") && (
          <StatusBanner
            tone="warning"
            title="Address book loaded with warnings"
            message={addressesError ?? `The member addresses endpoint returned status "${status}".`}
          />
        )}

        <form
          action={createMemberAddressAction}
          className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">Create address</p>
          <div className="mt-6 grid gap-4 sm:grid-cols-2">
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>Full name</span><input name="fullName" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>Company</span><input name="company" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>Street line 1</span><input name="street1" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>Street line 2</span><input name="street2" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>Postal code</span><input name="postalCode" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>City</span><input name="city" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>State</span><input name="state" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>Country code</span><input name="countryCode" defaultValue="DE" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal uppercase outline-none" /></label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>Phone</span><input name="phoneE164" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
            <label className="flex items-center gap-3 text-sm font-medium text-[var(--color-text-primary)]"><input type="checkbox" name="isDefaultBilling" /> Default billing</label>
            <label className="flex items-center gap-3 text-sm font-medium text-[var(--color-text-primary)]"><input type="checkbox" name="isDefaultShipping" /> Default shipping</label>
          </div>
          <button type="submit" className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
            Add address
          </button>
        </form>

        <div className="grid gap-5">
          {addresses.map((address) => (
            <article key={address.id} className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {address.isDefaultBilling ? "Default billing" : address.isDefaultShipping ? "Default shipping" : "Saved address"}
                  </p>
                </div>
                <div className="flex flex-wrap gap-2">
                  <form action={setMemberAddressDefaultAction}>
                    <input type="hidden" name="id" value={address.id} />
                    <input type="hidden" name="asBilling" value="true" />
                    <button type="submit" className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                      Set billing
                    </button>
                  </form>
                  <form action={setMemberAddressDefaultAction}>
                    <input type="hidden" name="id" value={address.id} />
                    <input type="hidden" name="asShipping" value="true" />
                    <button type="submit" className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                      Set shipping
                    </button>
                  </form>
                  <form action={deleteMemberAddressAction}>
                    <input type="hidden" name="id" value={address.id} />
                    <input type="hidden" name="rowVersion" value={address.rowVersion} />
                    <button type="submit" className="rounded-full border border-[rgba(217,111,50,0.2)] px-4 py-2 text-sm font-semibold text-[var(--color-accent)] transition hover:bg-[rgba(217,111,50,0.08)]">
                      Delete
                    </button>
                  </form>
                </div>
              </div>
              <form action={updateMemberAddressAction} className="mt-6">
                <input type="hidden" name="id" value={address.id} />
                <input type="hidden" name="rowVersion" value={address.rowVersion} />
                <div className="grid gap-4 sm:grid-cols-2">
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>Full name</span><input name="fullName" defaultValue={address.fullName} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>Company</span><input name="company" defaultValue={address.company ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>Street line 1</span><input name="street1" defaultValue={address.street1} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>Street line 2</span><input name="street2" defaultValue={address.street2 ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>Postal code</span><input name="postalCode" defaultValue={address.postalCode} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>City</span><input name="city" defaultValue={address.city} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>State</span><input name="state" defaultValue={address.state ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]"><span>Country code</span><input name="countryCode" defaultValue={address.countryCode} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal uppercase outline-none" /></label>
                  <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2"><span>Phone</span><input name="phoneE164" defaultValue={address.phoneE164 ?? ""} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" /></label>
                  <label className="flex items-center gap-3 text-sm font-medium text-[var(--color-text-primary)]"><input type="checkbox" name="isDefaultBilling" defaultChecked={address.isDefaultBilling} /> Default billing</label>
                  <label className="flex items-center gap-3 text-sm font-medium text-[var(--color-text-primary)]"><input type="checkbox" name="isDefaultShipping" defaultChecked={address.isDefaultShipping} /> Default shipping</label>
                </div>
                <button type="submit" className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
                  Save address
                </button>
              </form>
            </article>
          ))}
        </div>

        {addresses.length === 0 && (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
            No saved addresses are currently available for this member.
          </div>
        )}
      </div>
    </section>
  );
}
