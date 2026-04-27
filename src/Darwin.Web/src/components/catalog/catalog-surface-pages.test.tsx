import assert from "node:assert/strict";
import test from "node:test";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { CatalogPage } from "@/components/catalog/catalog-page";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";

const category: PublicCategorySummary = {
  id: "category-1",
  slug: "fruit",
  name: "Fruit",
  description: "Fresh produce aisle",
  productCount: 8,
};

const product: PublicProductSummary = {
  id: "product-1",
  slug: "apples",
  name: "Apples",
  priceMinor: 700,
  compareAtPriceMinor: 1000,
  currency: "EUR",
  imageUrl: null,
  primaryImageUrl: null,
  shortDescription: "Crisp apples",
  categoryName: "Fruit",
};

const cmsPage: PublicPageSummary = {
  id: "cms-1",
  slug: "herb-guide",
  title: "Herb guide",
  metaDescription: "Storage tips for herbs",
};

test("CatalogPage renders the upgraded grocery browse surface", () => {
  const html = renderToStaticMarkup(
    React.createElement(CatalogPage, {
      culture: "en-US",
      categories: [category],
      products: [product],
      cmsPages: [cmsPage],
      cartSummary: {
        status: "ok",
        itemCount: 2,
        currency: "EUR",
        grandTotalGrossMinor: 1400,
      },
      activeCategorySlug: "fruit",
      totalProducts: 1,
      matchingProductsTotal: 1,
      currentPage: 1,
      pageSize: 24,
      searchQuery: "apples",
      visibleState: "offers",
      visibleSort: "offers-first",
      mediaState: "all",
      savingsBand: "hero",
      facetSummary: {
        totalCount: 1,
        offerCount: 1,
        baseCount: 0,
        withImageCount: 0,
        missingImageCount: 1,
        valueOfferCount: 0,
        heroOfferCount: 1,
      },
      loadedProductsCount: 1,
      dataStatus: {
        categories: "ok",
        products: "ok",
        cmsPages: "ok",
      },
    }),
  );

  assert.match(html, /linear-gradient\(135deg,#f6ffe9_0%,#ffffff_38%,#fff1d2_100%\)/);
  assert.match(html, /Fruit/);
  assert.match(html, /Apples/);
  assert.ok(html.includes('href="/en-US/catalog/apples"'));
  assert.ok(html.includes('href="/en-US/catalog?category=fruit"'));
  assert.ok(html.includes('href="/en-US/cms/herb-guide"'));
});
