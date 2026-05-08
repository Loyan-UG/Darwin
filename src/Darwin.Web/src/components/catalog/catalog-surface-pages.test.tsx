import assert from "node:assert/strict";
import test from "node:test";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { CatalogPage } from "@/components/catalog/catalog-page";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";

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

test("CatalogPage renders the upgraded grocery browse surface", () => {
  const html = renderToStaticMarkup(
    React.createElement(CatalogPage, {
      culture: "en-US",
      categories: [category],
      products: [product],
      activeCategorySlug: "fruit",
      totalProducts: 1,
      currentPage: 1,
      pageSize: 24,
      searchQuery: "apples",
      visibleState: "offers",
      visibleSort: "offers-first",
      mediaState: "all",
      savingsBand: "hero",
      dataStatus: {
        categories: "ok",
        products: "ok",
      },
    }),
  );

  assert.match(html, /linear-gradient\(135deg,#f6ffe9_0%,#ffffff_38%,#fff1d2_100%\)/);
  assert.match(html, /Fruit/);
  assert.match(html, /Apples/);
  assert.ok(html.includes('href="/en-US/catalog/apples"'));
  assert.ok(html.includes('href="/en-US/catalog?category=fruit"'));
  assert.doesNotMatch(html, /Herb guide/);
});
