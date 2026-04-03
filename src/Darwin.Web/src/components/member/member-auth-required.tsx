import Link from "next/link";
import { PublicAuthContinuation } from "@/components/account/public-auth-continuation";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCategorySummary, PublicProductSummary } from "@/features/catalog/types";
import type { PublicCartSummary } from "@/features/cart/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
import { formatMoney } from "@/lib/formatting";
import { buildLocalizedAuthHref, sanitizeAppPath } from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";

type MemberAuthRequiredProps = {
  culture: string;
  title: string;
  message: string;
  returnPath: string;
  cmsPages?: PublicPageSummary[];
  cmsPagesStatus?: string;
  categories?: PublicCategorySummary[];
  categoriesStatus?: string;
  products?: PublicProductSummary[];
  productsStatus?: string;
  storefrontCart?: PublicCartSummary | null;
  storefrontCartStatus?: string;
};

export function MemberAuthRequired({
  culture,
  title,
  message,
  returnPath,
  cmsPages = [],
  cmsPagesStatus = "idle",
  categories = [],
  categoriesStatus = "idle",
  products = [],
  productsStatus = "idle",
  storefrontCart = null,
  storefrontCartStatus = "idle",
}: MemberAuthRequiredProps) {
  const copy = getMemberResource(culture);
  const safeReturnPath = sanitizeAppPath(returnPath, "/account");
  const signInHref = buildLocalizedAuthHref("/account/sign-in", safeReturnPath, culture);
  const registerHref = buildLocalizedAuthHref("/account/register", safeReturnPath, culture);
  const offerBoard = sortProductsByOpportunity(products).slice(0, 3);
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
        <div className="mt-8">
          <MemberCrossSurfaceRail
            culture={culture}
            includeOrders={false}
            includeInvoices={false}
            includeLoyalty={false}
          />
        </div>
        <div className="mt-8 rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-accent)]">
            {copy.memberAuthRequiredOfferBoardTitle}
          </p>
          <p className="mt-3 text-sm leading-6 text-[var(--color-text-secondary)]">
            {copy.memberAuthRequiredOfferBoardMessage}
          </p>
          {offerBoard.length > 0 ? (
            <div className="mt-4 grid gap-3 lg:grid-cols-3">
              {offerBoard.map((product) => {
                const savingsPercent = getProductSavingsPercent(product);

                return (
                  <Link
                    key={product.id}
                    href={`/catalog/${product.slug}`}
                    className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    <p className="font-semibold text-[var(--color-text-primary)]">
                      {product.name}
                    </p>
                    <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {savingsPercent !== null
                        ? formatResource(copy.memberAuthRequiredOfferBoardOfferDescription, {
                            savingsPercent,
                            price: formatMoney(
                              product.priceMinor,
                              product.currency,
                              culture,
                            ),
                          })
                        : product.shortDescription ??
                          copy.memberAuthRequiredOfferBoardFallbackDescription}
                    </p>
                  </Link>
                );
              })}
            </div>
          ) : (
            <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.memberAuthRequiredOfferBoardEmptyMessage}
            </p>
          )}
        </div>
        <div className="mt-8">
          <PublicAuthContinuation
            culture={culture}
            cmsPages={cmsPages}
            cmsPagesStatus={cmsPagesStatus}
            categories={categories}
            categoriesStatus={categoriesStatus}
            products={products}
            productsStatus={productsStatus}
            storefrontCart={storefrontCart}
            storefrontCartStatus={storefrontCartStatus}
          />
        </div>
      </div>
    </section>
  );
}
