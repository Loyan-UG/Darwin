import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCartSummary } from "@/features/cart/types";
import {
  buildLocalizedAuthHref,
  localizeHref,
  sanitizeAppPath,
} from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";

type MemberAuthRequiredProps = {
  culture: string;
  title: string;
  message: string;
  returnPath: string;
  storefrontCart?: PublicCartSummary | null;
};

function getCopy(culture: string) {
  const english = culture.toLowerCase().startsWith("en");

  return {
    continueTitle: english
      ? "Continue after sign in"
      : "Nach der Anmeldung fortfahren",
    continueMessage: english
      ? "Sign in or create an account to open this member page."
      : "Melden Sie sich an oder erstellen Sie ein Konto, um diese Mitgliederseite zu oeffnen.",
    cartMessage: english
      ? "{count} items are waiting in your cart."
      : "{count} Artikel liegen im Warenkorb.",
    cartCta: english ? "Review cart" : "Warenkorb ansehen",
    continueShopping: english ? "Continue shopping" : "Weiter einkaufen",
  };
}

export function MemberAuthRequired({
  culture,
  title,
  message,
  returnPath,
  storefrontCart = null,
}: MemberAuthRequiredProps) {
  const memberCopy = getMemberResource(culture);
  const copy = getCopy(culture);
  const safeReturnPath = sanitizeAppPath(returnPath, "/account");
  const signInHref = buildLocalizedAuthHref(
    "/account/sign-in",
    safeReturnPath,
    culture,
  );
  const registerHref = buildLocalizedAuthHref(
    "/account/register",
    safeReturnPath,
    culture,
  );
  const cartItemCount =
    storefrontCart?.items.reduce((total, item) => total + item.quantity, 0) ??
    0;

  return (
    <section className="mx-auto flex w-full max-w-[760px] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
        <StatusBanner title={title} message={message} />
        <div className="mt-8 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-brand)]">
            {copy.continueTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.continueMessage}
          </p>
          <div className="mt-5 flex flex-wrap gap-3">
            <Link
              href={signInHref}
              className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
            >
              {memberCopy.signIn}
            </Link>
            <Link
              href={registerHref}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {memberCopy.createAccount}
            </Link>
          </div>
        </div>

        {cartItemCount > 0 ? (
          <div className="mt-6 rounded-[1rem] border border-[var(--color-border-soft)] bg-white/75 px-5 py-5">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {formatResource(copy.cartMessage, { count: cartItemCount })}
            </p>
            <Link
              href={localizeHref("/cart", culture)}
              className="mt-4 inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.cartCta}
            </Link>
          </div>
        ) : (
          <Link
            href={localizeHref("/catalog", culture)}
            className="mt-6 inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold transition hover:bg-[var(--color-surface-panel-strong)]"
          >
            {copy.continueShopping}
          </Link>
        )}
      </div>
    </section>
  );
}
