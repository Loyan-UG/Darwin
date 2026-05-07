import { getCommerceResource } from "@/localization";
import { formatDateTime } from "@/lib/formatting";

type MockCheckoutPageProps = {
  culture: string;
  orderId: string;
  paymentId: string;
  provider: string;
  sessionToken: string;
  returnUrl: string | null;
  cancelUrl: string | null;
  cancelActionUrl: string | null;
  successUrl: string | null;
  failureUrl: string | null;
  title: string;
  description: string;
};

function renderLinkCard(
  label: string,
  title: string,
  description: string,
  href: string | null,
  unavailableLabel: string,
  tone: "brand" | "warning" | "neutral" = "neutral",
) {
  const toneClass =
    tone === "brand"
      ? "bg-[var(--color-brand)] text-[var(--color-brand-contrast)] hover:bg-[var(--color-brand-strong)]"
      : tone === "warning"
        ? "bg-[var(--color-accent)] text-[var(--color-text-primary)] hover:opacity-90"
        : "border border-[var(--color-border-soft)] text-[var(--color-text-primary)] hover:bg-[var(--color-surface-panel-strong)]";

  return (
    <article className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-5 py-5 shadow-[var(--shadow-panel)]">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
        {label}
      </p>
      <h2 className="mt-3 text-lg font-semibold text-[var(--color-text-primary)]">
        {title}
      </h2>
      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
        {description}
      </p>
      {href ? (
        <a
          href={href}
          className={`mt-5 inline-flex rounded-full px-4 py-2 text-sm font-semibold transition ${toneClass}`}
        >
          {title}
        </a>
      ) : (
        <p className="mt-5 text-sm font-semibold text-[var(--color-accent)]">
          {unavailableLabel}
        </p>
      )}
    </article>
  );
}

export function MockCheckoutPage({
  culture,
  orderId,
  paymentId,
  provider,
  sessionToken,
  returnUrl,
  cancelUrl,
  cancelActionUrl,
  successUrl,
  failureUrl,
  title,
  description,
}: MockCheckoutPageProps) {
  const copy = getCommerceResource(culture);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-10 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.mockCheckoutEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {title}
          </h1>
          <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {description}
          </p>
        </div>

        <section className="grid gap-5 lg:grid-cols-[minmax(0,1.15fr)_minmax(0,0.85fr)]">
          <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
            <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
              {copy.mockCheckoutSummaryTitle}
            </p>
            <div className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
              <div className="flex items-center justify-between rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-3">
                <span>{copy.mockCheckoutOrderIdLabel}</span>
                <span className="font-mono text-xs text-[var(--color-text-primary)]">{orderId}</span>
              </div>
              <div className="flex items-center justify-between rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-3">
                <span>{copy.mockCheckoutPaymentIdLabel}</span>
                <span className="font-mono text-xs text-[var(--color-text-primary)]">{paymentId}</span>
              </div>
              <div className="flex items-center justify-between rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-3">
                <span>{copy.mockCheckoutProviderLabel}</span>
                <span className="text-[var(--color-text-primary)]">{provider}</span>
              </div>
              <div className="flex items-center justify-between rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-3">
                <span>{copy.mockCheckoutSessionTokenLabel}</span>
                <span className="font-mono text-xs text-[var(--color-text-primary)]">{sessionToken}</span>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.mockCheckoutReturnLabel}
                </p>
                <p className="mt-2 break-all font-mono text-xs text-[var(--color-text-primary)]">
                  {returnUrl ?? copy.mockCheckoutUrlUnavailable}
                </p>
              </div>
              <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-muted)]">
                  {copy.mockCheckoutCancelLabel}
                </p>
                <p className="mt-2 break-all font-mono text-xs text-[var(--color-text-primary)]">
                  {cancelUrl ?? copy.mockCheckoutUrlUnavailable}
                </p>
              </div>
            </div>
          </div>

          <div className="space-y-5">
            {renderLinkCard(
              copy.mockCheckoutSuccessLabel,
              copy.mockCheckoutSuccessCta,
              copy.mockCheckoutSuccessMessage,
              successUrl,
              copy.mockCheckoutUrlUnavailable,
              "brand",
            )}
            {renderLinkCard(
              copy.mockCheckoutCancelActionLabel,
              copy.mockCheckoutCancelCta,
              copy.mockCheckoutCancelMessage,
              cancelActionUrl,
              copy.mockCheckoutUrlUnavailable,
              "neutral",
            )}
            {renderLinkCard(
              copy.mockCheckoutFailureLabel,
              copy.mockCheckoutFailureCta,
              copy.mockCheckoutFailureMessage,
              failureUrl,
              copy.mockCheckoutUrlUnavailable,
              "warning",
            )}
          </div>
        </section>

        <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.mockCheckoutNotesTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.mockCheckoutNotesMessage}
          </p>
          <p className="mt-3 text-xs text-[var(--color-text-muted)]">
            {formatDateTime(new Date().toISOString(), culture)}
          </p>
        </div>
      </div>
    </section>
  );
}
