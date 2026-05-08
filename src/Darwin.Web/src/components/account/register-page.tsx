import Link from "next/link";
import { ActivationRecoveryPanel } from "@/components/account/activation-recovery-panel";
import { PublicAuthReturnSummary } from "@/components/account/public-auth-return-summary";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCartSummary } from "@/features/cart/types";
import { registerMemberAction } from "@/features/account/actions";
import {
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { buildLocalizedAuthHref } from "@/lib/locale-routing";

type RegisterPageProps = {
  culture: string;
  email?: string;
  registerStatus?: string;
  registerError?: string;
  returnPath?: string;
  storefrontCart: PublicCartSummary | null;
};

export function RegisterPage({
  culture,
  email,
  registerStatus,
  registerError,
  returnPath,
  storefrontCart,
}: RegisterPageProps) {
  const copy = getMemberResource(culture);
  const resolvedRegisterError = resolveLocalizedQueryMessage(registerError, copy);
  const activationHref = buildLocalizedAuthHref("/account/activation", returnPath, culture);
  const signInHref = buildLocalizedAuthHref("/account/sign-in", returnPath, culture);

  return (
    <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-8 lg:grid-cols-[minmax(0,1fr)_340px]">
        <form
          action={registerMemberAction}
          className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
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

          {matchesLocalizedQueryMessageKey(
            registerStatus,
            "registrationSubmittedMessage",
            "registered",
          ) ? (
            <div className="mt-6">
              <StatusBanner
                title={copy.registrationSubmittedTitle}
                message={copy.registrationSubmittedMessage}
              />
            </div>
          ) : null}

          {resolvedRegisterError ? (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title={copy.registrationFailedTitle}
                message={resolvedRegisterError}
              />
            </div>
          ) : null}

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
            <button type="submit" className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
              {copy.createAccountCta}
            </button>
            <Link href={activationHref} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
              {copy.activationFlowCta}
            </Link>
            <Link href={signInHref} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
              {copy.signIn}
            </Link>
          </div>
        </form>

        <aside className="flex flex-col gap-6">
          <PublicAuthReturnSummary
            culture={culture}
            returnPath={returnPath}
            storefrontCart={storefrontCart}
          />
          {(matchesLocalizedQueryMessageKey(
            registerStatus,
            "registrationSubmittedMessage",
            "registered",
          ) || Boolean(email)) ? (
            <ActivationRecoveryPanel
              culture={culture}
              email={email}
              returnPath={returnPath}
              compact
            />
          ) : null}
        </aside>
      </div>
    </section>
  );
}
