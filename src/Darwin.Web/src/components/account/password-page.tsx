import Link from "next/link";
import { ActivationRecoveryPanel } from "@/components/account/activation-recovery-panel";
import { PublicAuthReturnSummary } from "@/components/account/public-auth-return-summary";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCartSummary } from "@/features/cart/types";
import {
  requestPasswordResetAction,
  resetPasswordAction,
} from "@/features/account/actions";
import {
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { buildLocalizedAuthHref } from "@/lib/locale-routing";

type PasswordPageProps = {
  culture: string;
  email?: string;
  token?: string;
  passwordStatus?: string;
  passwordError?: string;
  returnPath?: string;
  storefrontCart: PublicCartSummary | null;
};

function getPasswordMessage(status: string | undefined, culture: string) {
  const copy = getMemberResource(culture);
  if (matchesLocalizedQueryMessageKey(status, "passwordRequestedMessage", "requested")) {
    return copy.passwordRequestedMessage;
  }

  if (matchesLocalizedQueryMessageKey(status, "passwordResetMessage", "reset")) {
    return copy.passwordResetMessage;
  }

  return undefined;
}

export function PasswordPage({
  culture,
  email,
  token,
  passwordStatus,
  passwordError,
  returnPath,
  storefrontCart,
}: PasswordPageProps) {
  const copy = getMemberResource(culture);
  const statusMessage = getPasswordMessage(passwordStatus, culture);
  const resolvedPasswordError = resolveLocalizedQueryMessage(passwordError, copy);
  const signInHref = buildLocalizedAuthHref("/account/sign-in", returnPath, culture);
  const activationHref = buildLocalizedAuthHref("/account/activation", returnPath, culture);

  return (
    <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.passwordRecoveryEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.passwordRecoveryTitle}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.passwordRecoveryDescription}
          </p>
        </header>

        {statusMessage ? (
          <StatusBanner title={copy.passwordFlowUpdatedTitle} message={statusMessage} />
        ) : null}

        {resolvedPasswordError ? (
          <StatusBanner
            tone="warning"
            title={copy.passwordFlowFailedTitle}
            message={resolvedPasswordError}
          />
        ) : null}

        <div className="grid gap-6 lg:grid-cols-2">
          <form action={requestPasswordResetAction} className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <input type="hidden" name="returnPath" value={returnPath || "/account"} />
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.requestResetEyebrow}
            </p>
            <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
              {copy.sendResetEmailTitle}
            </h2>
            <label className="mt-6 flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.emailLabel}
              <input name="email" type="email" required autoComplete="email" inputMode="email" defaultValue={email} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <button type="submit" className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
              {copy.requestResetEmailCta}
            </button>
          </form>

          <form action={resetPasswordAction} className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <input type="hidden" name="returnPath" value={returnPath || "/account"} />
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.completeResetEyebrow}
            </p>
            <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
              {copy.applyResetTokenTitle}
            </h2>
            <div className="mt-6 grid gap-4">
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.emailLabel}
                <input name="email" type="email" required autoComplete="email" inputMode="email" defaultValue={email} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.tokenLabel}
                <input name="token" required autoComplete="one-time-code" defaultValue={token} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                {copy.newPasswordLabel}
                <input name="newPassword" type="password" required minLength={8} autoComplete="new-password" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
            </div>
            <button type="submit" className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
              {copy.resetPasswordCta}
            </button>
          </form>
        </div>

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
          <div className="flex flex-wrap gap-3">
            <Link href={signInHref} className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
              {copy.signIn}
            </Link>
            <Link href={activationHref} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
              {copy.activationFlowCta}
            </Link>
          </div>
          <aside className="flex flex-col gap-6">
            <ActivationRecoveryPanel
              culture={culture}
              email={email}
              returnPath={returnPath}
              compact
            />
            <PublicAuthReturnSummary
              culture={culture}
              returnPath={returnPath}
              storefrontCart={storefrontCart}
            />
          </aside>
        </div>
      </div>
    </section>
  );
}
