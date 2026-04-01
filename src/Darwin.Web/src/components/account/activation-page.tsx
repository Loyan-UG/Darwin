import { StatusBanner } from "@/components/feedback/status-banner";
import {
  confirmEmailAction,
  requestEmailConfirmationAction,
} from "@/features/account/actions";

type ActivationPageProps = {
  email?: string;
  token?: string;
  activationStatus?: string;
  activationError?: string;
};

function getActivationMessage(status?: string) {
  switch (status) {
    case "requested":
      return "If the account exists, the activation email has been requested through the current backend flow.";
    case "confirmed":
      return "The email confirmation request completed successfully. Browser sign-in is still a separate session decision.";
    default:
      return undefined;
  }
}

export function ActivationPage({
  email,
  token,
  activationStatus,
  activationError,
}: ActivationPageProps) {
  const statusMessage = getActivationMessage(activationStatus);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Activation
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Request or complete email confirmation
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            This route exposes the existing backend confirmation lifecycle to the storefront without inventing a web-only activation system.
          </p>
        </div>

        {statusMessage && (
          <StatusBanner title="Activation flow updated" message={statusMessage} />
        )}

        {activationError && (
          <StatusBanner
            tone="warning"
            title="Activation flow failed"
            message={activationError}
          />
        )}

        <div className="grid gap-6 lg:grid-cols-2">
          <form
            action={requestEmailConfirmationAction}
            className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              Request confirmation
            </p>
            <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
              Resend activation email
            </h2>
            <label className="mt-6 flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              Email
              <input name="email" type="email" defaultValue={email} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <button
              type="submit"
              className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              Request activation email
            </button>
          </form>

          <form
            action={confirmEmailAction}
            className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              Confirm email
            </p>
            <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
              Apply confirmation token
            </h2>
            <div className="mt-6 grid gap-4">
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                Email
                <input name="email" type="email" defaultValue={email} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                Token
                <input name="token" defaultValue={token} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
            </div>
            <button
              type="submit"
              className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              Confirm email
            </button>
          </form>
        </div>
      </div>
    </section>
  );
}
