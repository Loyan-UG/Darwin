import type { PublicCartSummary } from "@/features/cart/types";
import { formatMoney } from "@/lib/formatting";
import { localizeHref, sanitizeAppPath } from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";

type PublicAuthReturnSummaryProps = {
  culture: string;
  returnPath?: string;
  storefrontCart: PublicCartSummary | null;
};

function resolveReturnContext(
  returnPath: string | undefined,
  copy: ReturnType<typeof getMemberResource>,
) {
  if (!returnPath || returnPath === "/account") {
    return {
      title: copy.publicAuthReturnAccountTitle,
      description: copy.publicAuthReturnAccountDescription,
    };
  }

  if (returnPath.startsWith("/checkout")) {
    return {
      title: copy.publicAuthReturnCheckoutTitle,
      description: copy.publicAuthReturnCheckoutDescription,
    };
  }

  if (returnPath.startsWith("/cart")) {
    return {
      title: copy.publicAuthReturnCartTitle,
      description: copy.publicAuthReturnCartDescription,
    };
  }

  if (returnPath.startsWith("/orders")) {
    return {
      title: copy.publicAuthReturnOrdersTitle,
      description: copy.publicAuthReturnOrdersDescription,
    };
  }

  if (returnPath.startsWith("/invoices")) {
    return {
      title: copy.publicAuthReturnInvoicesTitle,
      description: copy.publicAuthReturnInvoicesDescription,
    };
  }

  if (returnPath.startsWith("/loyalty")) {
    return {
      title: copy.publicAuthReturnLoyaltyTitle,
      description: copy.publicAuthReturnLoyaltyDescription,
    };
  }

  return {
    title: copy.publicAuthReturnGenericTitle,
    description: formatResource(copy.publicAuthReturnGenericDescription, {
      returnPath,
    }),
  };
}

export function PublicAuthReturnSummary({
  culture,
  returnPath,
  storefrontCart,
}: PublicAuthReturnSummaryProps) {
  const copy = getMemberResource(culture);
  const safeReturnPath = sanitizeAppPath(returnPath, "/account");
  const context = resolveReturnContext(safeReturnPath, copy);
  const cartLineCount =
    storefrontCart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;
  const localizedReturnHref = localizeHref(safeReturnPath, culture);
  const localizedCartHref = localizeHref("/cart", culture);

  return (
    <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
      <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
        {copy.publicAuthReturnEyebrow}
      </p>
      <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
        {context.title}
      </h2>
      <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
        {context.description}
      </p>
      <dl className="mt-5 grid gap-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        <div className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
          <dt className="font-semibold text-[var(--color-text-primary)]">
            {copy.publicAuthReturnPathLabel}
          </dt>
          <dd>{safeReturnPath}</dd>
        </div>
        <div className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
          <dt className="font-semibold text-[var(--color-text-primary)]">
            {copy.publicAuthReturnCartStateLabel}
          </dt>
          <dd>
            {storefrontCart
              ? formatResource(copy.publicAuthReturnCartStateValue, {
                  itemCount: cartLineCount,
                  total: formatMoney(
                    storefrontCart.grandTotalGrossMinor,
                    storefrontCart.currency,
                    culture,
                  ),
                })
              : copy.publicAuthReturnCartStateEmpty}
          </dd>
        </div>
      </dl>
      <div className="mt-6 flex flex-wrap gap-3">
        <a
          href={localizedReturnHref}
          className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
        >
          {copy.publicAuthReturnPrimaryCta}
        </a>
        {storefrontCart && cartLineCount > 0 && safeReturnPath !== "/cart" && (
          <a
            href={localizedCartHref}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
          >
            {copy.publicAuthReturnCartCta}
          </a>
        )}
      </div>
    </aside>
  );
}
