import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  getProductOpportunityCampaign,
  getProductOpportunityCampaignLabel,
  getProductSavingsPercent,
} from "@/features/catalog/merchandising";
import { buildAppQueryPath } from "@/lib/locale-routing";

type CampaignLabels = {
  heroOffer: string;
  valueOffer: string;
  priceDrop: string;
  steadyPick: string;
};

export type StorefrontCampaignCard = {
  id: string;
  label: string;
  title: string;
  description: string;
  href: string;
  ctaLabel: string;
  meta?: string | null;
};

type StorefrontCategoryCampaignCard = StorefrontCampaignCard;

type StorefrontProductCampaignCard = StorefrontCampaignCard;

export type StorefrontSpotlightCard = {
  id: string;
  title: string;
  description: string;
  href: string;
};

type StorefrontPageSpotlightCard = StorefrontSpotlightCard;

type StorefrontCategorySpotlightCard = StorefrontSpotlightCard;

export type StorefrontOfferCard = {
  id: string;
  label: string;
  title: string;
  description: string;
  href: string;
  ctaLabel?: string;
  meta?: string | null;
  price: string;
};

type StorefrontCategorySpotlightEntry = {
  category: PublicCategorySummary;
  product: PublicProductSummary | null;
  status: string;
};

type CategoryCampaignFormatter = {
  prefix: string;
  label: string;
  fallbackDescription: string;
  ctaLabel: string;
};

type PageSpotlightFormatter = {
  prefix: string;
  fallbackDescription: string;
};

type CategorySpotlightCardFormatter = {
  prefix: string;
  fallbackDescription: string;
};

type ProductCampaignFormatter = {
  prefix: string;
  labels: CampaignLabels;
  formatPrice: (product: PublicProductSummary) => string;
  describeWithSavings: (
    product: PublicProductSummary,
    input: {
      campaignLabel: string;
      savingsPercent: number;
      price: string;
    },
  ) => string;
  describeWithoutSavings: (
    product: PublicProductSummary,
    input: {
      campaignLabel: string;
      price: string;
    },
  ) => string;
  ctaLabel: string;
};

type OfferFormatter = {
  labels: CampaignLabels;
  formatPrice: (product: PublicProductSummary) => string;
  describeWithSavings: (
    product: PublicProductSummary,
    input: {
      campaignLabel: string;
      savingsPercent: number;
      price: string;
    },
  ) => string;
  describeWithoutSavings: (
    product: PublicProductSummary,
    input: {
      campaignLabel: string;
      price: string;
    },
  ) => string;
  fallbackDescription: string;
  formatMeta?: (product: PublicProductSummary) => string | null;
  ctaLabel?: string;
};

type CategorySpotlightFormatter = {
  eyebrow: string;
  ctaLabel: string;
  formatProductDescription: (
    entry: StorefrontCategorySpotlightEntry,
    input: {
      savingsPercent: number | null;
      price: string;
    },
  ) => string;
  formatFallbackDescription: (entry: StorefrontCategorySpotlightEntry) => string;
  formatMeta: (entry: StorefrontCategorySpotlightEntry) => string;
  formatPrice: (product: PublicProductSummary) => string;
};

export function buildStorefrontCategoryCampaignCards(
  categories: PublicCategorySummary[],
  formatter: CategoryCampaignFormatter,
): StorefrontCategoryCampaignCard[] {
  return categories.map((category) => ({
    id: `${formatter.prefix}-category-${category.id}`,
    label: formatter.label,
    title: category.name,
    description: category.description ?? formatter.fallbackDescription,
    href: buildAppQueryPath("/catalog", { category: category.slug }),
    ctaLabel: formatter.ctaLabel,
  }));
}

export function buildStorefrontPageSpotlightCards(
  pages: PublicPageSummary[],
  formatter: PageSpotlightFormatter,
): StorefrontPageSpotlightCard[] {
  return pages.map((page) => ({
    id: `${formatter.prefix}-page-${page.id}`,
    title: page.title,
    description: page.metaDescription ?? formatter.fallbackDescription,
    href: `/cms/${page.slug}`,
  }));
}

export function buildStorefrontCategorySpotlightLinkCards(
  categories: PublicCategorySummary[],
  formatter: CategorySpotlightCardFormatter,
): StorefrontCategorySpotlightCard[] {
  return categories.map((category) => ({
    id: `${formatter.prefix}-category-${category.id}`,
    title: category.name,
    description: category.description ?? formatter.fallbackDescription,
    href: buildAppQueryPath("/catalog", { category: category.slug }),
  }));
}

export function buildStorefrontProductCampaignCards(
  products: PublicProductSummary[],
  formatter: ProductCampaignFormatter,
): StorefrontProductCampaignCard[] {
  return products.map((product) => {
    const price = formatter.formatPrice(product);
    const savingsPercent = getProductSavingsPercent(product);
    const campaignLabel = getProductOpportunityCampaignLabel(
      getProductOpportunityCampaign(product),
      formatter.labels,
    );

    return {
      id: `${formatter.prefix}-product-${product.id}`,
      label: campaignLabel,
      title: product.name,
      description:
        savingsPercent !== null
          ? formatter.describeWithSavings(product, {
              campaignLabel,
              savingsPercent,
              price,
            })
          : formatter.describeWithoutSavings(product, {
              campaignLabel,
              price,
            }),
      href: `/catalog/${product.slug}`,
      ctaLabel: formatter.ctaLabel,
    };
  });
}

export function buildStorefrontOfferCards(
  products: PublicProductSummary[],
  formatter: OfferFormatter,
): StorefrontOfferCard[] {
  return products.map((product) => {
    const price = formatter.formatPrice(product);
    const savingsPercent = getProductSavingsPercent(product);
    const campaignLabel = getProductOpportunityCampaignLabel(
      getProductOpportunityCampaign(product),
      formatter.labels,
    );

    return {
      id: `offer-${product.id}`,
      label: campaignLabel,
      title: product.name,
      description:
        savingsPercent !== null
          ? formatter.describeWithSavings(product, {
              campaignLabel,
              savingsPercent,
              price,
            })
          : formatter.describeWithoutSavings(product, {
              campaignLabel,
              price,
            }) || product.shortDescription ||
            formatter.fallbackDescription,
      href: `/catalog/${product.slug}`,
      ctaLabel: formatter.ctaLabel,
      meta: formatter.formatMeta?.(product) ?? null,
      price,
    };
  });
}

export function buildStorefrontCategorySpotlightCards(
  entries: StorefrontCategorySpotlightEntry[],
  formatter: CategorySpotlightFormatter,
) {
  return entries.map((entry) => {
    const savingsPercent = entry.product
      ? getProductSavingsPercent(entry.product)
      : null;

    return {
      id: entry.category.id,
      eyebrow: formatter.eyebrow,
      title: entry.category.name,
      description:
        entry.product && entry.status === "ok"
          ? formatter.formatProductDescription(entry, {
              savingsPercent,
              price: formatter.formatPrice(entry.product),
            })
          : formatter.formatFallbackDescription(entry),
      href: buildAppQueryPath("/catalog", {
        category: entry.category.slug,
      }),
      ctaLabel: formatter.ctaLabel,
      meta: formatter.formatMeta(entry),
    };
  });
}
