import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { registerMemberAction } from "@/features/account/actions";

type RegisterPageProps = {
  email?: string;
  registerStatus?: string;
  registerError?: string;
};

export function RegisterPage({
  email,
  registerStatus,
  registerError,
}: RegisterPageProps) {
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <form
          action={registerMemberAction}
          className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
        >
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Member registration
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Create a consumer account
          </h1>
          <p className="mt-5 max-w-2xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            This screen registers a member directly through the public member auth endpoint and then relies on the existing backend confirmation-email lifecycle.
          </p>

          {registerStatus === "registered" && (
            <div className="mt-6">
              <StatusBanner
                title="Registration submitted"
                message="The account was created. Check the email confirmation flow before expecting sign-in to succeed."
              />
            </div>
          )}

          {registerError && (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title="Registration failed"
                message={registerError}
              />
            </div>
          )}

          <div className="mt-8 grid gap-4 sm:grid-cols-2">
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              First name
              <input name="firstName" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              Last name
              <input name="lastName" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
              Email
              <input name="email" type="email" defaultValue={email} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
              Password
              <input name="password" type="password" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
          </div>

          <div className="mt-8 flex flex-wrap gap-3">
            <button
              type="submit"
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              Create account
            </button>
            <Link
              href="/account/activation"
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              Activation flow
            </Link>
          </div>
        </form>

        <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            Current boundary
          </p>
          <ul className="mt-5 space-y-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            <li className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
              Registration is live, but browser sign-in persistence is still a separate decision.
            </li>
            <li className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
              Confirmation email behavior stays on the backend Communication Core path.
            </li>
          </ul>
        </aside>
      </div>
    </section>
  );
}
