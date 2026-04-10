import type { PublicProductSummary } from "@/features/catalog/types";
import { summarizeCatalogPromotionLanes } from "@/features/catalog/promotion-lanes";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { formatResource } from "@/localization";

type PromotionLaneCopy = {
  cardLabel: string;
  heroLabel: string;
  valueLabel: string;
  liveOffersLabel: string;
  baseLabel: string;
  title: string;
  fallbackTitle: string;
  description: string;
  fallbackDescription: string;
  cta: string;
  meta: string;
};

type PromotionLaneRouteMapItem = {
  id: string;
  label: string;
  title: string;
  description: string;
  href: string;
  ctaLabel: string;
  meta: string;
};

function getPromotionLaneHref(lane: "hero-offers" | "value-offers" | "live-offers" | "base-assortment") {
  return lane === "hero-offers"
    ? buildAppQueryPath("/catalog", {
        visibleState: "offers",
        visibleSort: "offers-first",
        savingsBand: "hero",
      })
    : lane === "value-offers"
      ? buildAppQueryPath("/catalog", {
          visibleState: "offers",
          visibleSort: "offers-first",
          savingsBand: "value",
        })
      : lane === "live-offers"
        ? buildAppQueryPath("/catalog", {
            visibleState: "offers",
            visibleSort: "savings-desc",
          })
        : buildAppQueryPath("/catalog", {
            visibleState: "base",
            visibleSort: "base-first",
          });
}

export function buildPromotionLaneRouteMapItem({
  id,
  products,
  culture,
  copy,
}: {
  id: string;
  products: PublicProductSummary[];
  culture: string;
  copy: PromotionLaneCopy;
}): PromotionLaneRouteMapItem {
  const lanes = summarizeCatalogPromotionLanes(products);
  const selectedLane = lanes.find((entry) => entry.count > 0) ?? lanes[0];

  const laneLabel =
    selectedLane.lane === "hero-offers"
      ? copy.heroLabel
      : selectedLane.lane === "value-offers"
        ? copy.valueLabel
        : selectedLane.lane === "live-offers"
          ? copy.liveOffersLabel
          : copy.baseLabel;

  return {
    id,
    label: copy.cardLabel,
    title: selectedLane.anchorProduct
      ? formatResource(copy.title, {
          lane: laneLabel,
          product: selectedLane.anchorProduct.name,
        })
      : formatResource(copy.fallbackTitle, {
          lane: laneLabel,
        }),
    description: selectedLane.anchorProduct
      ? formatResource(copy.description, {
          lane: laneLabel,
          count: selectedLane.count,
          price: formatMoney(
            selectedLane.anchorProduct.priceMinor,
            selectedLane.anchorProduct.currency,
            culture,
          ),
        })
      : formatResource(copy.fallbackDescription, {
          lane: laneLabel,
        }),
    href: getPromotionLaneHref(selectedLane.lane),
    ctaLabel: copy.cta,
    meta: formatResource(copy.meta, {
      count: selectedLane.count,
    }),
  };
}
