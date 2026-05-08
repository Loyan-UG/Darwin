import Link from "next/link";
import type { PublicCartSummary } from "@/features/cart/types";
import { formatMoney } from "@/lib/formatting";
import {
  buildLocalizedAuthHref,
  localizeHref,
} from "@/lib/locale-routing";

type AccountHubPageProps = {
  culture: string;
  storefrontCart: PublicCartSummary | null;
  returnPath?: string;
};

function getCopy(culture: string) {
  const english = culture.toLowerCase().startsWith("en");

  return {
    title: english ? "Your account" : "Ihr Konto",
    description: english
      ? "Sign in, create an account, or recover access to continue shopping and manage orders."
      : "Melden Sie sich an, erstellen Sie ein Konto oder stellen Sie den Zugriff wieder her, um Einkauf und Bestellungen zu verwalten.",
    signInTitle: english ? "Sign in" : "Anmelden",
    signInDescription: english ? "Continue to your account, orders, invoices, and saved details." : "Weiter zu Konto, Bestellungen, Rechnungen und gespeicherten Daten.",
    registerTitle: english ? "Register" : "Registrieren",
    registerDescription: english ? "Create a member account for faster checkout and loyalty features." : "Erstellen Sie ein Konto fuer schnelleren Checkout und Treuefunktionen.",
    passwordTitle: english ? "Recover access" : "Zugriff wiederherstellen",
    passwordDescription: english ? "Reset your password if you cannot sign in." : "Setzen Sie Ihr Passwort zurueck, wenn Sie sich nicht anmelden koennen.",
    activationTitle: english ? "Resend activation" : "Aktivierung erneut senden",
    activationDescription: english ? "Request a new activation or confirmation link." : "Fordern Sie einen neuen Aktivierungs- oder Bestaetigungslink an.",
    open: english ? "Open" : "Oeffnen",
    cartTitle: english ? "Cart continuation" : "Warenkorb fortsetzen",
    cartEmpty: english ? "Your current cart is empty." : "Ihr aktueller Warenkorb ist leer.",
    cartMessage: english ? "{count} items are waiting in your cart. Current total: {total}." : "{count} Artikel liegen im Warenkorb. Aktuelle Summe: {total}.",
    cartCta: english ? "Review cart" : "Warenkorb ansehen",
    checkoutCta: english ? "Continue to checkout" : "Zur Kasse",
    backHome: english ? "Continue shopping" : "Weiter einkaufen",
  };
}

export function AccountHubPage({
  culture,
  storefrontCart,
  returnPath,
}: AccountHubPageProps) {
  const copy = getCopy(culture);
  const cartLineCount =
    storefrontCart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;
  const preferredReturnPath = returnPath || (cartLineCount > 0 ? "/checkout" : "/account");
  const accountCards = [
    {
      id: "sign-in",
      title: copy.signInTitle,
      description: copy.signInDescription,
      href: buildLocalizedAuthHref("/account/sign-in", preferredReturnPath, culture),
    },
    {
      id: "register",
      title: copy.registerTitle,
      description: copy.registerDescription,
      href: buildLocalizedAuthHref("/account/register", preferredReturnPath, culture),
    },
    {
      id: "password",
      title: copy.passwordTitle,
      description: copy.passwordDescription,
      href: buildLocalizedAuthHref("/account/password", preferredReturnPath, culture),
    },
    {
      id: "activation",
      title: copy.activationTitle,
      description: copy.activationDescription,
      href: buildLocalizedAuthHref("/account/activation", preferredReturnPath, culture),
    },
  ];

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="overflow-hidden rounded-[1rem] border border-[#dbe7c7] bg-[linear-gradient(135deg,#f5ffe8_0%,#ffffff_42%,#fff1d0_100%)] px-6 py-8 shadow-[0_28px_70px_-34px_rgba(58,92,35,0.38)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.title}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.title}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.description}
          </p>
        </div>

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
          {accountCards.map((card) => (
            <article
              key={card.id}
              className="flex h-full flex-col rounded-[1rem] border border-[#dce6cf] bg-white p-6 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.26)]"
            >
              <h2 className="text-2xl font-semibold text-[var(--color-text-primary)]">
                {card.title}
              </h2>
              <p className="mt-4 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                {card.description}
              </p>
              <div className="mt-6">
                <Link
                  href={localizeHref(card.href, culture)}
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-4 py-2 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.open}
                </Link>
              </div>
            </article>
          ))}
        </div>

        <aside className="rounded-[1rem] border border-[#dce6cf] bg-[linear-gradient(160deg,#ffffff_0%,#f7fbef_100%)] px-6 py-8 shadow-[0_24px_54px_-34px_rgba(58,92,35,0.25)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
            {copy.cartTitle}
          </p>
          <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
            {storefrontCart
              ? copy.cartMessage
                  .replace("{count}", String(cartLineCount))
                  .replace(
                    "{total}",
                    formatMoney(storefrontCart.grandTotalGrossMinor, storefrontCart.currency, culture),
                  )
              : copy.cartEmpty}
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            {cartLineCount > 0 ? (
              <>
                <Link
                  href={localizeHref("/cart", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                >
                  {copy.cartCta}
                </Link>
                <Link
                  href={localizeHref("/checkout", culture)}
                  className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                >
                  {copy.checkoutCta}
                </Link>
              </>
            ) : null}
            <Link
              href={localizeHref("/catalog", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.backHome}
            </Link>
          </div>
        </aside>
      </div>
    </section>
  );
}
