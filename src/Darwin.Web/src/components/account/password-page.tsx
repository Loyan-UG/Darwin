import { StatusBanner } from "@/components/feedback/status-banner";
import {
  requestPasswordResetAction,
  resetPasswordAction,
} from "@/features/account/actions";

type PasswordPageProps = {
  email?: string;
  token?: string;
  passwordStatus?: string;
  passwordError?: string;
};

function getPasswordMessage(status?: string) {
  switch (status) {
    case "requested":
      return "If the account exists, the password-reset email has been requested through the current backend flow.";
    case "reset":
      return "The password reset completed successfully. Browser sign-in remains a separate session decision.";
    default:
      return undefined;
  }
}

export function PasswordPage({
  email,
  token,
  passwordStatus,
  passwordError,
}: PasswordPageProps) {
  const statusMessage = getPasswordMessage(passwordStatus);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Password recovery
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Request or complete a password reset
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            This route exposes the existing backend reset-token lifecycle and keeps the web client out of its own private reset implementation.
          </p>
        </div>

        {statusMessage && (
          <StatusBanner title="Password flow updated" message={statusMessage} />
        )}

        {passwordError && (
          <StatusBanner
            tone="warning"
            title="Password flow failed"
            message={passwordError}
          />
        )}

        <div className="grid gap-6 lg:grid-cols-2">
          <form
            action={requestPasswordResetAction}
            className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              Request reset
            </p>
            <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
              Send reset email
            </h2>
            <label className="mt-6 flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              Email
              <input name="email" type="email" defaultValue={email} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <button
              type="submit"
              className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              Request reset email
            </button>
          </form>

          <form
            action={resetPasswordAction}
            className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
          >
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              Complete reset
            </p>
            <h2 className="mt-3 text-3xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
              Apply reset token
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
              <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
                New password
                <input name="newPassword" type="password" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
              </label>
            </div>
            <button
              type="submit"
              className="mt-6 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              Reset password
            </button>
          </form>
        </div>
      </div>
    </section>
  );
}
