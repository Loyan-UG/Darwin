import Link from "next/link";
import { requestEmailConfirmationAction } from "@/features/account/actions";
import { buildLocalizedAuthHref } from "@/lib/locale-routing";
import { getMemberResource } from "@/localization";

type ActivationRecoveryPanelProps = {
  culture: string;
  email?: string;
  returnPath?: string;
  compact?: boolean;
};

export function ActivationRecoveryPanel({
  culture,
  email,
  returnPath,
  compact = false,
}: ActivationRecoveryPanelProps) {
  const copy = getMemberResource(culture);
  const activationHref = buildLocalizedAuthHref(
    "/account/activation",
    returnPath,
    culture,
  );

  return (
    <aside
      className={
        compact
          ? "rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]"
          : "rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
      }
    >
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
        {copy.activationRecoveryEyebrow}
      </p>
      <h2 className="mt-3 text-2xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
        {copy.activationRecoveryTitle}
      </h2>
      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        {copy.activationRecoveryDescription}
      </p>

      <form action={requestEmailConfirmationAction} className="mt-5">
        <input type="hidden" name="returnPath" value={returnPath || "/account"} />
        <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
          {copy.emailLabel}
          <input
            name="email"
            type="email"
            required
            autoComplete="email"
            inputMode="email"
            defaultValue={email}
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal outline-none"
          />
        </label>
        <div className="mt-4 flex flex-wrap gap-3">
          <button
            type="submit"
            className="inline-flex rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
          >
            {copy.activationRecoveryResendCta}
          </button>
          <Link
            href={activationHref}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
          >
            {copy.activationRecoveryOpenFlowCta}
          </Link>
        </div>
      </form>
    </aside>
  );
}
