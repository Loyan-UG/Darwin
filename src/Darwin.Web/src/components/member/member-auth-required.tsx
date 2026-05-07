import Link from "next/link";
import { PublicAuthContinuation } from "@/components/account/public-auth-continuation";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import { buildMemberPromotionLaneCards } from "@/components/member/member-promotion-lanes";
import { StatusBanner } from "@/components/feedback/status-banner";
import { StorefrontCampaignBoard } from "@/components/storefront/storefront-campaign-board";
import { StorefrontOfferBoard } from "@/components/storefront/storefront-offer-board";
import type { PublicCategorySummary, PublicProductSummary } from "@/features/catalog/types";
import type { PublicCartSummary } from "@/features/cart/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { buildStorefrontOfferCards } from "@/features/storefront/storefront-campaigns";
import { buildStorefrontSpotlightSelections } from "@/features/storefront/storefront-spotlight";
import { formatMoney } from "@/lib/formatting";
import {
  buildLocalizedAuthHref,
  sanitizeAppPath,
} from "@/lib/locale-routing";
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
  const { offerBoardProducts } = buildStorefrontSpotlightSelections({
    cmsPages,
    categories,
    products,
    offerBoardCount: 3,
  });
  const offerBoard = buildStorefrontOfferCards(offerBoardProducts, {
    labels: {
      heroOffer: copy.offerCampaignHeroLabel,
      valueOffer: copy.offerCampaignValueLabel,
      priceDrop: copy.offerCampaignPriceDropLabel,
      steadyPick: copy.offerCampaignSteadyLabel,
    },
    formatPrice: (product) =>
      formatMoney(product.priceMinor, product.currency, culture),
    describeWithSavings: (_, input) =>
      formatResource(copy.memberAuthRequiredOfferBoardOfferDescription, {
        campaignLabel: input.campaignLabel,
        savingsPercent: input.savingsPercent,
        price: input.price,
      }),
    describeWithoutSavings: (product) =>
      product.shortDescription ?? copy.memberAuthRequiredOfferBoardFallbackDescription,
    fallbackDescription: copy.memberAuthRequiredOfferBoardFallbackDescription,
  });
  const promotionLaneCards = buildMemberPromotionLaneCards(
    offerBoardProducts,
    culture,
  );
  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="w-full rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-10 shadow-[var(--shadow-panel)] sm:px-8">
        <StatusBanner title={title} message={message} />
        <div className="mt-6 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-4">
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
        <div className="mt-8 rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-accent)]">
            {copy.memberAuthRequiredOfferBoardTitle}
          </p>
          <p className="mt-3 text-sm leading-6 text-[var(--color-text-secondary)]">
            {copy.memberAuthRequiredOfferBoardMessage}
          </p>
          <StorefrontOfferBoard
            culture={culture}
            cards={offerBoard}
            emptyMessage={copy.memberAuthRequiredOfferBoardEmptyMessage}
          />
          <div className="mt-6">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-[var(--color-accent)]">
              {copy.memberStorefrontPromotionLaneSectionTitle}
            </p>
            <p className="mt-3 text-sm leading-6 text-[var(--color-text-secondary)]">
              {copy.memberStorefrontPromotionLaneSectionMessage}
            </p>
            <StorefrontCampaignBoard
              culture={culture}
              cards={promotionLaneCards}
              emptyMessage={copy.memberStorefrontPromotionLaneSectionMessage}
            />
          </div>
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
