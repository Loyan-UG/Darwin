import assert from "node:assert/strict";
import test from "node:test";
import {
  buildStorefrontCategoryCampaignCards,
  buildStorefrontCategorySpotlightLinkCards,
  buildStorefrontCategorySpotlightCards,
  buildStorefrontOfferCards,
  buildStorefrontPageSpotlightCards,
  buildStorefrontProductCampaignCards,
} from "@/features/storefront/storefront-campaigns";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";

const category: PublicCategorySummary = {
  id: "category-1",
  slug: "fruit",
  name: "Fruit",
  description: "Fresh picks",
  productCount: 10,
};

const page: PublicPageSummary = {
  id: "page-1",
  slug: "about",
  title: "About",
  metaDescription: "About this storefront",
};

const heroProduct: PublicProductSummary = {
  id: "product-1",
  slug: "apples",
  name: "Apples",
  priceMinor: 700,
  compareAtPriceMinor: 1000,
  currency: "EUR",
  imageUrl: null,
  shortDescription: "Crisp apples",
  categoryName: "Fruit",
};

const steadyProduct: PublicProductSummary = {
  id: "product-2",
  slug: "bananas",
  name: "Bananas",
  priceMinor: 500,
  compareAtPriceMinor: null,
  currency: "EUR",
  imageUrl: null,
  shortDescription: null,
  categoryName: "Fruit",
};

const labels = {
  heroOffer: "Hero",
  valueOffer: "Value",
  priceDrop: "Drop",
  steadyPick: "Steady",
};

test("buildStorefrontCategoryCampaignCards and buildStorefrontProductCampaignCards keep canonical hrefs", () => {
  const categoryCards = buildStorefrontCategoryCampaignCards([category], {
    prefix: "account",
    label: "Category",
    fallbackDescription: "Fallback category",
    ctaLabel: "Open category",
  });
  const productCards = buildStorefrontProductCampaignCards([heroProduct], {
    prefix: "account",
    labels,
    formatPrice: (product) => `${product.priceMinor}`,
    describeWithSavings: (_, input) =>
      `${input.campaignLabel} ${input.savingsPercent}% ${input.price}`,
    describeWithoutSavings: (_, input) => `${input.campaignLabel} ${input.price}`,
    ctaLabel: "Open product",
  });

  assert.deepEqual(categoryCards[0], {
    id: "account-category-category-1",
    label: "Category",
    title: "Fruit",
    description: "Fresh picks",
    href: "/catalog?category=fruit",
    ctaLabel: "Open category",
  });
  assert.deepEqual(productCards[0], {
    id: "account-product-product-1",
    label: "Hero",
    title: "Apples",
    description: "Hero 30% 700",
    href: "/catalog/apples",
    ctaLabel: "Open product",
  });
});

test("buildStorefrontPageSpotlightCards and buildStorefrontCategorySpotlightLinkCards keep canonical browse links", () => {
  const pageCards = buildStorefrontPageSpotlightCards([page], {
    prefix: "member",
    fallbackDescription: "Fallback page",
  });
  const categoryCards = buildStorefrontCategorySpotlightLinkCards([category], {
    prefix: "member",
    fallbackDescription: "Fallback category",
  });

  assert.deepEqual(pageCards[0], {
    id: "member-page-page-1",
    title: "About",
    description: "About this storefront",
    href: "/page/about",
  });
  assert.deepEqual(categoryCards[0], {
    id: "member-category-category-1",
    title: "Fruit",
    description: "Fresh picks",
    href: "/catalog?category=fruit",
  });
});

test("buildStorefrontOfferCards keeps price, meta, and fallback descriptions aligned", () => {
  const offerCards = buildStorefrontOfferCards([heroProduct, steadyProduct], {
    labels,
    formatPrice: (product) => `${product.priceMinor}`,
    describeWithSavings: (_, input) =>
      `${input.campaignLabel} ${input.savingsPercent}% ${input.price}`,
    describeWithoutSavings: (_, input) => `${input.campaignLabel} ${input.price}`,
    fallbackDescription: "Fallback offer",
    formatMeta: (product) =>
      typeof product.compareAtPriceMinor === "number"
        ? `Compare ${product.compareAtPriceMinor}`
        : null,
    ctaLabel: "View",
  });

  assert.deepEqual(offerCards[0], {
    id: "offer-product-1",
    label: "Hero",
    title: "Apples",
    description: "Hero 30% 700",
    href: "/catalog/apples",
    ctaLabel: "View",
    meta: "Compare 1000",
    price: "700",
  });
  assert.deepEqual(offerCards[1], {
    id: "offer-product-2",
    label: "Steady",
    title: "Bananas",
    description: "Steady 500",
    href: "/catalog/bananas",
    ctaLabel: "View",
    meta: null,
    price: "500",
  });
});

test("buildStorefrontCategorySpotlightCards keeps healthy and fallback category storytelling aligned", () => {
  const cards = buildStorefrontCategorySpotlightCards(
    [
      {
        category,
        product: heroProduct,
        status: "ok",
      },
      {
        category: {
          ...category,
          id: "category-2",
          slug: "bakery",
          name: "Bakery",
        },
        product: null,
        status: "degraded",
      },
    ],
    {
      eyebrow: "Category",
      ctaLabel: "Open",
      formatPrice: (product) => `${product.priceMinor}`,
      formatProductDescription: (entry, input) =>
        `${entry.product?.name} ${input.savingsPercent} ${input.price}`,
      formatFallbackDescription: (entry) => `Fallback ${entry.status}`,
      formatMeta: (entry) => `Meta ${entry.status}`,
    },
  );

  assert.deepEqual(cards[0], {
    id: "category-1",
    eyebrow: "Category",
    title: "Fruit",
    description: "Apples 30 700",
    href: "/catalog?category=fruit",
    ctaLabel: "Open",
    meta: "Meta ok",
  });
  assert.deepEqual(cards[1], {
    id: "category-2",
    eyebrow: "Category",
    title: "Bakery",
    description: "Fallback degraded",
    href: "/catalog?category=bakery",
    ctaLabel: "Open",
    meta: "Meta degraded",
  });
});
