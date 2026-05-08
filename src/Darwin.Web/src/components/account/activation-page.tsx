import Link from "next/link";
import { PublicAuthReturnSummary } from "@/components/account/public-auth-return-summary";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCartSummary } from "@/features/cart/types";
import {
  confirmEmailAction,
  requestEmailConfirmationAction,
} from "@/features/account/actions";
import {
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { buildLocalizedAuthHref } from "@/lib/locale-routing";

type ActivationPageProps = {
  culture: string;
  email?: string;
  token?: string;
  activationStatus?: string;
  activationError?: string;
  returnPath?: string;
  storefrontCart: PublicCartSummary | null;
};

function getActivationMessage(status: string | undefined, culture: string) {
  const copy = getMemberResource(culture);

  if (matchesLocalizedQueryMessageKey(status, "activationRequestedMessage", "requested")) {
    return copy.activationRequestedMessage;
  }

  if (matchesLocalizedQueryMessageKey(status, "activationConfirmedMessage", "confirmed")) {
    return copy.activationConfirmedMessage;
  }

  return undefined;
}

export function ActivationPage({
  culture,
  email,
  token,
  activationStatus,
  activationError,
  returnPath,
  storefrontCart,
}: ActivationPageProps) {
  const copy = getMemberResource(culture);
  const statusMessage = getActivationMessage(activationStatus, culture);
  const resolvedActivationError = resolveLocalizedQueryMessage(activationError, copy);
  const signInHref = buildLocalizedAuthHref("/account/sign-in", returnPath, culture);
  const passwordHref = buildLocalizedAuthHref("/account/password", returnPath, culture);

  return (
    <section className="mx-auto flex w-full max-w-[1180px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <header className="rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_48%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.activationEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.activationTitle}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.activationDescription}
          </p>
        </header>

        {statusMessage ? (
          <StatusBanner title={copy.activationFlowUpdatedTitle} message={statusMessage} />
        ) : null}

        {resolvedActivationError ? (
          <StatusBanner
            tone="warning"
            title={copy.activationFlowFailedTitle}
            message={resolvedActivationError}
          />
        ) : null}

        <div className="grid gap-6 lg:grid-cols-2">
          <form action={requestEmailConfirmationAction} className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <input type="hidden" name="returnPath" value={returnPath || "/account"} />
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.requestConfirmationEyebrow}
            </p>
            <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
              {copy.resendActivationTitle}
            </h2>
            <label className="mt-6 flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.emailLabel}
              <input name="email" type="email" required autoComplete="email" inputMode="email" defaultValue={email} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <button type="submit" className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
              {copy.requestActivationEmailCta}
            </button>
          </form>

          <form action={confirmEmailAction} className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <input type="hidden" name="returnPath" value={returnPath || "/account"} />
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.confirmEmailEyebrow}
            </p>
            <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
              {copy.applyConfirmationTokenTitle}
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
            </div>
            <button type="submit" className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
              {copy.confirmEmailCta}
            </button>
          </form>
        </div>

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
          <div className="flex flex-wrap gap-3">
            <Link href={signInHref} className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">
              {copy.signIn}
            </Link>
            <Link href={passwordHref} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
              {copy.cardPasswordCta}
            </Link>
          </div>
          <PublicAuthReturnSummary
            culture={culture}
            returnPath={returnPath}
            storefrontCart={storefrontCart}
          />
        </div>
      </div>
    </section>
  );
}
