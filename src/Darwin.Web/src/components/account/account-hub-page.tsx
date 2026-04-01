import Link from "next/link";

const accountCards = [
  {
    id: "sign-in",
    eyebrow: "Member session",
    title: "Sign in to the protected portal",
    description:
      "Use the provisional browser session layer to open profile, orders, invoices, and loyalty pages in the web portal.",
    href: "/account/sign-in",
    ctaLabel: "Sign in",
  },
  {
    id: "register",
    eyebrow: "Registration",
    title: "Create a member account",
    description:
      "Consumer self-service registration is now available directly against the member auth contract.",
    href: "/account/register",
    ctaLabel: "Register",
  },
  {
    id: "activation",
    eyebrow: "Activation",
    title: "Request or confirm email activation",
    description:
      "The current backend activation lifecycle is now exposed in the storefront instead of staying implicit.",
    href: "/account/activation",
    ctaLabel: "Open activation",
  },
  {
    id: "password",
    eyebrow: "Password recovery",
    title: "Request or complete a password reset",
    description:
      "Password reset now stays on the public web route structure while still using the existing backend email/token flow.",
    href: "/account/password",
    ctaLabel: "Open reset flow",
  },
];

export function AccountHubPage() {
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Account self-service
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            Public self-service flows are now split from the future member session.
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            Registration, activation, and password recovery can move forward now. Full profile, addresses, and authenticated member navigation remain intentionally blocked on the final browser auth/session decision.
          </p>
        </div>

        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {accountCards.map((card) => (
            <article
              key={card.id}
              className="flex h-full flex-col rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
            >
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {card.eyebrow}
              </p>
              <h2 className="mt-4 text-2xl font-semibold text-[var(--color-text-primary)]">
                {card.title}
              </h2>
              <p className="mt-4 flex-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                {card.description}
              </p>
              <div className="mt-6">
                <Link
                  href={card.href}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {card.ctaLabel}
                </Link>
              </div>
            </article>
          ))}
        </div>

        <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            Session strategy note
          </p>
          <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)]">
            Login, profile editing, address book, orders, invoices, and loyalty remain behind the browser-session decision. This keeps `Darwin.Web` from hard-coding a token transport that the wider platform has not yet finalized.
          </p>
        </aside>
      </div>
    </section>
  );
}
