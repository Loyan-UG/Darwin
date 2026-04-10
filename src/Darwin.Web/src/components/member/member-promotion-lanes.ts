import type { PublicProductSummary } from "@/features/catalog/types";
import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
import type { StorefrontCampaignCard } from "@/features/storefront/storefront-campaigns";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";

export function buildMemberPromotionLaneCards(
  products: PublicProductSummary[],
  culture: string,
): StorefrontCampaignCard[] {
  const copy = getMemberResource(culture);

  return summarizeCatalogPromotionLanes(products).map((entry) => {
    const laneLabel =
      entry.lane === "hero-offers"
        ? copy.memberStorefrontPromotionLaneHeroLabel
        : entry.lane === "value-offers"
          ? copy.memberStorefrontPromotionLaneValueLabel
          : entry.lane === "live-offers"
            ? copy.memberStorefrontPromotionLaneLiveOffersLabel
            : copy.memberStorefrontPromotionLaneBaseLabel;

    const href =
      entry.lane === "hero-offers"
        ? buildAppQueryPath("/catalog", {
            visibleState: "offers",
            visibleSort: "offers-first",
            savingsBand: "hero",
          })
        : entry.lane === "value-offers"
          ? buildAppQueryPath("/catalog", {
              visibleState: "offers",
              visibleSort: "offers-first",
              savingsBand: "value",
            })
          : entry.lane === "live-offers"
            ? buildAppQueryPath("/catalog", {
                visibleState: "offers",
                visibleSort: "savings-desc",
              })
            : buildAppQueryPath("/catalog", {
                visibleState: "base",
                visibleSort: "base-first",
              });

    return {
      id: `member-promotion-lane-${entry.lane}`,
      label: copy.memberStorefrontPromotionLaneCardLabel,
      title: entry.anchorProduct
        ? formatResource(copy.memberStorefrontPromotionLaneTitle, {
            lane: laneLabel,
            product: entry.anchorProduct.name,
          })
        : formatResource(copy.memberStorefrontPromotionLaneFallbackTitle, {
            lane: laneLabel,
          }),
      description:
        entry.anchorProduct !== null
          ? formatResource(copy.memberStorefrontPromotionLaneDescription, {
              lane: laneLabel,
              count: entry.count,
              price: formatMoney(
                entry.anchorProduct.priceMinor,
                entry.anchorProduct.currency,
                culture,
              ),
            })
          : formatResource(copy.memberStorefrontPromotionLaneFallbackDescription, {
              lane: laneLabel,
            }),
      href,
      ctaLabel: copy.memberStorefrontPromotionLaneCta,
      meta: formatResource(copy.memberStorefrontPromotionLaneMeta, {
        count: entry.count,
      }),
    };
  });
}
