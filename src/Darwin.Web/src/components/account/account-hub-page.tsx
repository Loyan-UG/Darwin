import Link from "next/link";
import { getMemberResource } from "@/localization";

type AccountHubPageProps = {
  culture: string;
};

export function AccountHubPage({ culture }: AccountHubPageProps) {
  const copy = getMemberResource(culture);
  const accountCards = [
    {
      id: "sign-in",
      eyebrow: copy.cardSignInEyebrow,
      title: copy.cardSignInTitle,
      description: copy.cardSignInDescription,
      href: "/account/sign-in",
      ctaLabel: copy.cardSignInCta,
    },
    {
      id: "register",
      eyebrow: copy.cardRegisterEyebrow,
      title: copy.cardRegisterTitle,
      description: copy.cardRegisterDescription,
      href: "/account/register",
      ctaLabel: copy.cardRegisterCta,
    },
    {
      id: "activation",
      eyebrow: copy.cardActivationEyebrow,
      title: copy.cardActivationTitle,
      description: copy.cardActivationDescription,
      href: "/account/activation",
      ctaLabel: copy.cardActivationCta,
    },
    {
      id: "password",
      eyebrow: copy.cardPasswordEyebrow,
      title: copy.cardPasswordTitle,
      description: copy.cardPasswordDescription,
      href: "/account/password",
      ctaLabel: copy.cardPasswordCta,
    },
  ];

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.accountHubEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.accountHubTitle}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.accountHubDescription}
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
            {copy.sessionStrategyNoteTitle}
          </p>
          <p className="mt-4 text-base leading-8 text-[var(--color-text-secondary)]">
            {copy.sessionStrategyNoteDescription}
          </p>
        </aside>
      </div>
    </section>
  );
}
