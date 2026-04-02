import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import {
  confirmEmailAction,
  requestEmailConfirmationAction,
} from "@/features/account/actions";
import { localizeHref } from "@/lib/locale-routing";
import { getMemberResource, resolveLocalizedQueryMessage } from "@/localization";

type ActivationPageProps = {
  culture: string;
  email?: string;
  token?: string;
  activationStatus?: string;
  activationError?: string;
  returnPath?: string;
};

function getActivationMessage(status: string | undefined, culture: string) {
  const copy = getMemberResource(culture);

  switch (status) {
    case "requested":
      return copy.activationRequestedMessage;
    case "confirmed":
      return copy.activationConfirmedMessage;
    default:
      return undefined;
  }
}

export function ActivationPage({
  culture,
  email,
  token,
  activationStatus,
  activationError,
  returnPath,
}: ActivationPageProps) {
  const copy = getMemberResource(culture);
  const statusMessage = getActivationMessage(activationStatus, culture);
  const resolvedActivationError = resolveLocalizedQueryMessage(
    activationError,
    copy,
  );
  const signInHref = `/account/sign-in?returnPath=${encodeURIComponent(
    returnPath || "/account",
  )}`;
  const passwordHref = `/account/password?returnPath=${encodeURIComponent(
    returnPath || "/account",
  )}`;

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.activationEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.activationTitle}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.activationDescription}
          </p>
        </div>

        {statusMessage && (
          <StatusBanner title={copy.activationFlowUpdatedTitle} message={statusMessage} />
        )}

        {resolvedActivationError && (
          <StatusBanner
            tone="warning"
            title={copy.activationFlowFailedTitle}
            message={resolvedActivationError}
          />
        )}

        <div className="grid gap-6 lg:grid-cols-2">
          <form
            action={requestEmailConfirmationAction}
            className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
          >
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
            <button
              type="submit"
              className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.requestActivationEmailCta}
            </button>
          </form>

          <form
            action={confirmEmailAction}
            className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
          >
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
            <button
              type="submit"
              className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {copy.confirmEmailCta}
            </button>
          </form>
        </div>

        <div className="flex flex-wrap gap-3">
          <Link
            href={localizeHref(signInHref, culture)}
            className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
          >
            {copy.signIn}
          </Link>
          <Link
            href={localizeHref(passwordHref, culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.cardPasswordCta}
          </Link>
        </div>
      </div>
    </section>
  );
}
