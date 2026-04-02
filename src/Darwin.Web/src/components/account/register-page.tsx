import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { registerMemberAction } from "@/features/account/actions";
import { localizeHref } from "@/lib/locale-routing";
import { getMemberResource, resolveLocalizedQueryMessage } from "@/localization";

type RegisterPageProps = {
  culture: string;
  email?: string;
  registerStatus?: string;
  registerError?: string;
  returnPath?: string;
};

export function RegisterPage({
  culture,
  email,
  registerStatus,
  registerError,
  returnPath,
}: RegisterPageProps) {
  const copy = getMemberResource(culture);
  const resolvedRegisterError = resolveLocalizedQueryMessage(registerError, copy);
  const activationHref = `/account/activation?returnPath=${encodeURIComponent(
    returnPath || "/account",
  )}`;
  const signInHref = `/account/sign-in?returnPath=${encodeURIComponent(
    returnPath || "/account",
  )}`;

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <form
          action={registerMemberAction}
          className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
        >
          <input type="hidden" name="returnPath" value={returnPath || "/account"} />
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.registerEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.registerTitle}
          </h1>
          <p className="mt-5 max-w-2xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.registerDescription}
          </p>

          {registerStatus === "registered" && (
            <div className="mt-6">
              <StatusBanner
                title={copy.registrationSubmittedTitle}
                message={copy.registrationSubmittedMessage}
              />
            </div>
          )}

          {resolvedRegisterError && (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title={copy.registrationFailedTitle}
                message={resolvedRegisterError}
              />
            </div>
          )}

          <div className="mt-8 grid gap-4 sm:grid-cols-2">
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.firstNameLabel}
              <input name="firstName" required autoComplete="given-name" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.lastNameLabel}
              <input name="lastName" required autoComplete="family-name" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
              {copy.emailLabel}
              <input name="email" type="email" required autoComplete="email" inputMode="email" defaultValue={email} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)] sm:col-span-2">
              {copy.passwordLabel}
              <input name="password" type="password" required minLength={8} autoComplete="new-password" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
          </div>

          <div className="mt-8 flex flex-wrap gap-3">
            <button
              type="submit"
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.createAccountCta}
            </button>
            <Link
              href={localizeHref(activationHref, culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.activationFlowCta}
            </Link>
            <Link
              href={localizeHref(signInHref, culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.signIn}
            </Link>
          </div>
        </form>

        <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            {copy.currentBoundaryTitle}
          </p>
          <ul className="mt-5 space-y-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            <li className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
              {copy.currentBoundaryRegistration}
            </li>
            <li className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
              {copy.currentBoundaryCommunication}
            </li>
          </ul>
        </aside>
      </div>
    </section>
  );
}
