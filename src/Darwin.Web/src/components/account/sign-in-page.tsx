import { StatusBanner } from "@/components/feedback/status-banner";
import { signInMemberAction } from "@/features/member-session/actions";

type SignInPageProps = {
  email?: string;
  signInError?: string;
  returnPath?: string;
};

export function SignInPage({
  email,
  signInError,
  returnPath,
}: SignInPageProps) {
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <form
          action={signInMemberAction}
          className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
        >
          <input type="hidden" name="returnPath" value={returnPath || "/account"} />
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Member sign-in
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Open the authenticated member area
          </h1>
          <p className="mt-5 max-w-2xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            This first browser-auth slice uses web-owned cookies to hold the member session while keeping all protected data behind the canonical member endpoints.
          </p>

          {signInError && (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title="Sign-in failed"
                message={signInError}
              />
            </div>
          )}

          <div className="mt-8 grid gap-4">
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              Email
              <input name="email" type="email" defaultValue={email} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              Password
              <input name="password" type="password" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
          </div>

          <button
            type="submit"
            className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
          >
            Sign in
          </button>
        </form>

        <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            Session note
          </p>
          <ul className="mt-5 space-y-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            <li className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
              This is a provisional browser-session layer owned by `Darwin.Web`, not a permanent auth architecture decision.
            </li>
            <li className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
              Orders, invoices, loyalty, and profile data still come only from `Darwin.WebApi`.
            </li>
          </ul>
        </aside>
      </div>
    </section>
  );
}
