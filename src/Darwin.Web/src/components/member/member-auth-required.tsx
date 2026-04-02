import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { localizeHref } from "@/lib/locale-routing";
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

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="w-full rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
        <StatusBanner title={title} message={message} />
        <div className="mt-8 flex flex-wrap gap-3">
          <Link
            href={localizeHref(
              `/account/sign-in?returnPath=${encodeURIComponent(returnPath)}`,
              culture,
            )}
            className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
          >
            {copy.signIn}
          </Link>
          <Link
            href={localizeHref(
              `/account/register?returnPath=${encodeURIComponent(returnPath)}`,
              culture,
            )}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.createAccount}
          </Link>
        </div>
      </div>
    </section>
  );
}
