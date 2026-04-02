import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { localizeHref, sanitizeAppPath } from "@/lib/locale-routing";
import { getMemberResource } from "@/localization";

type MemberAuthRequiredProps = {
  culture: string;
  title: string;
  message: string;
  returnPath: string;
};

export function MemberAuthRequired({
  culture,
  title,
  message,
  returnPath,
}: MemberAuthRequiredProps) {
  const copy = getMemberResource(culture);
  const safeReturnPath = sanitizeAppPath(returnPath, "/account");
  const signInHref = localizeHref(
    `/account/sign-in?returnPath=${encodeURIComponent(safeReturnPath)}`,
    culture,
  );
  const registerHref = localizeHref(
    `/account/register?returnPath=${encodeURIComponent(safeReturnPath)}`,
    culture,
  );
  const homeHref = localizeHref("/", culture);
  const catalogHref = localizeHref("/catalog", culture);
  const accountHref = localizeHref("/account", culture);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
        <StatusBanner title={title} message={message} />
        <div className="mt-6 rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-4">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-muted)]">
            {copy.memberAuthRequiredRouteSummaryTitle}
          </p>
          <p className="mt-3 text-sm leading-6 text-[var(--color-text-secondary)]">
            {copy.memberAuthRequiredRouteSummaryMessage}
          </p>
          <p className="mt-3 text-sm font-medium text-[var(--color-text-primary)]">
            <span className="text-[var(--color-text-secondary)]">
              {copy.memberAuthRequiredReturnPathLabel}{" "}
            </span>
            <span className="break-all">{safeReturnPath}</span>
          </p>
        </div>
        <div className="mt-8 flex flex-wrap gap-3">
          <Link
            href={signInHref}
            className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
          >
            {copy.signIn}
          </Link>
          <Link
            href={registerHref}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.createAccount}
          </Link>
        </div>
        <div className="mt-8 rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-base)] px-5 py-5">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-text-muted)]">
            {copy.memberCrossSurfaceTitle}
          </p>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-[var(--color-text-secondary)]">
            {copy.memberAuthRequiredFollowUpMessage}
          </p>
          <div className="mt-4 flex flex-wrap gap-3">
            <Link
              href={homeHref}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.memberCrossSurfaceHomeCta}
            </Link>
            <Link
              href={catalogHref}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.memberCrossSurfaceCatalogCta}
            </Link>
            <Link
              href={accountHref}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.memberCrossSurfaceAccountCta}
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
