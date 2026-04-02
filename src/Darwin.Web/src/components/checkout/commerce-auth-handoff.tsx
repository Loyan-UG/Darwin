import Link from "next/link";
import type { PublicCartSummary } from "@/features/cart/types";
import { formatMoney } from "@/lib/formatting";
import { buildLocalizedAuthHref } from "@/lib/locale-routing";
import { formatResource, getCommerceResource } from "@/localization";

type CommerceAuthHandoffProps = {
  culture: string;
  cart: PublicCartSummary;
  returnPath: string;
  routeKey: "cart" | "checkout";
};

export function CommerceAuthHandoff({
  culture,
  cart,
  returnPath,
  routeKey,
}: CommerceAuthHandoffProps) {
  const copy = getCommerceResource(culture);
  const cartLineCount = cart.items.reduce((sum, item) => sum + item.quantity, 0);
  const routeTitle =
    routeKey === "checkout"
      ? copy.commerceAuthCheckoutTitle
      : copy.commerceAuthCartTitle;
  const routeDescription =
    routeKey === "checkout"
      ? copy.commerceAuthCheckoutDescription
      : copy.commerceAuthCartDescription;

  return (
    <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
        {copy.commerceAuthEyebrow}
      </p>
      <h2 className="mt-3 text-2xl font-[family-name:var(--font-display)] text-[var(--color-text-primary)]">
        {routeTitle}
      </h2>
      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        {routeDescription}
      </p>
      <dl className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)] sm:grid-cols-2">
        <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <dt className="font-semibold text-[var(--color-text-primary)]">
            {copy.commerceAuthCartSnapshotLabel}
          </dt>
          <dd>
            {formatResource(copy.commerceAuthCartSnapshotValue, {
              itemCount: cartLineCount,
              total: formatMoney(cart.grandTotalGrossMinor, cart.currency, culture),
            })}
          </dd>
        </div>
        <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
          <dt className="font-semibold text-[var(--color-text-primary)]">
            {copy.commerceAuthReturnPathLabel}
          </dt>
          <dd>{returnPath}</dd>
        </div>
      </dl>
      <div className="mt-6 flex flex-wrap gap-3">
        <Link
          href={buildLocalizedAuthHref("/account/sign-in", returnPath, culture)}
          className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
        >
          {copy.commerceAuthSignInCta}
        </Link>
        <Link
          href={buildLocalizedAuthHref("/account/register", returnPath, culture)}
          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
        >
          {copy.commerceAuthRegisterCta}
        </Link>
      </div>
      <div className="mt-4 flex flex-wrap gap-3 text-sm font-semibold">
        <Link
          href={buildLocalizedAuthHref("/account/activation", returnPath, culture)}
          className="text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
        >
          {copy.commerceAuthActivationCta}
        </Link>
        <Link
          href={buildLocalizedAuthHref("/account/password", returnPath, culture)}
          className="text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
        >
          {copy.commerceAuthPasswordCta}
        </Link>
      </div>
    </aside>
  );
}
