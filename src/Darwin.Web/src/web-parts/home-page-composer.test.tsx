import assert from "node:assert/strict";
import test from "node:test";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { HomePageComposer } from "@/web-parts/home-page-composer";
import type { WebPagePart } from "@/web-parts/types";

const homeParts: WebPagePart[] = [
  {
    id: "home-hero",
    kind: "hero",
    eyebrow: "Fresh delivery",
    title: "Weekly grocery deals for the whole household",
    description: "Shop produce, pantry essentials, and helpful customer information from one storefront.",
    actions: [
      { label: "Browse catalog", href: "/catalog" },
      { label: "View loyalty", href: "/loyalty", variant: "secondary" },
      { label: "My account", href: "/account", variant: "secondary" },
    ],
    highlights: [
      "Daily produce refresh",
      "Secure checkout",
      "Clear support",
    ],
    panelTitle: "Customer promise",
  },
  {
    id: "home-category-spotlight",
    kind: "card-grid",
    eyebrow: "Fresh aisles",
    title: "Shop by aisle",
    description: "Browse popular grocery categories directly from the landing page.",
    cards: [
      {
        id: "category-fruit",
        eyebrow: "Fruit",
        title: "Seasonal fruit",
        description: "Citrus, berries, and crisp apples.",
        href: "/catalog?category=fruit",
        ctaLabel: "Browse fruit",
        meta: "12 items",
      },
      {
        id: "category-dairy",
        eyebrow: "Dairy",
        title: "Milk and yogurt",
        description: "Breakfast and everyday staples.",
        href: "/catalog?category=dairy",
        ctaLabel: "Browse dairy",
        meta: "8 items",
      },
    ],
    emptyMessage: "No categories",
  },
  {
    id: "home-product-spotlight",
    kind: "card-grid",
    eyebrow: "Current picks",
    title: "Top weekly offers",
    description: "A short selection from the current catalog.",
    cards: [
      {
        id: "offer-apples",
        eyebrow: "Hero offer",
        title: "Organic apples",
        description: "Crunchy apples discounted for this week.",
        href: "/catalog/apples",
        ctaLabel: "Open product",
        meta: "EUR 7.00",
      },
      {
        id: "offer-bananas",
        eyebrow: "Value offer",
        title: "Bananas bundle",
        description: "Breakfast fruit for the whole week.",
        href: "/catalog/bananas",
        ctaLabel: "Open product",
        meta: "EUR 4.50",
      },
    ],
    emptyMessage: "No offers",
  },
  {
    id: "home-loyalty-teaser",
    kind: "blank-state",
    eyebrow: "Loyalty",
    title: "Earn points with participating businesses.",
    description: "Sign in, discover partners, and track reward progress from your member account.",
    actions: [
      { label: "Open loyalty", href: "/loyalty" },
      { label: "Sign in or register", href: "/account", variant: "secondary" },
    ],
  },
  {
    id: "home-cms-spotlight",
    kind: "card-grid",
    eyebrow: "Store guide",
    title: "Helpful guides",
    description: "Customer information stays easy to find.",
    cards: [
      {
        id: "cms-1",
        eyebrow: "Guide",
        title: "How to store fresh herbs",
        description: "Simple freshness tips for home kitchens.",
        href: "/page/herb-guide",
        ctaLabel: "Read guide",
        meta: "herb-guide",
      },
    ],
    emptyMessage: "No guides",
  },
  {
    id: "home-trust-strip",
    kind: "card-grid",
    eyebrow: "Service",
    title: "Easy shopping, secure payment, clear support.",
    description: "The core customer promises for shopping with Darwin.",
    cards: [
      {
        id: "trust-shipping",
        title: "Shipping",
        description: "Delivery options are shown during checkout.",
        href: "/page/shipping",
        ctaLabel: "Learn more",
      },
      {
        id: "trust-returns",
        title: "Returns",
        description: "Return and cancellation information stays easy to find.",
        href: "/page/returns",
        ctaLabel: "Learn more",
      },
    ],
    emptyMessage: "No service content",
  },
];

test("HomePageComposer renders the dedicated customer landing layout from home parts", () => {
  const html = renderToStaticMarkup(
    React.createElement(HomePageComposer, {
      parts: homeParts,
      culture: "en-US",
    }),
  );

  assert.match(html, /Weekly grocery deals for the whole household/);
  assert.match(html, /Fresh aisles/);
  assert.match(html, /Seasonal fruit/);
  assert.match(html, /Top weekly offers/);
  assert.match(html, /Organic apples/);
  assert.match(html, /Earn points with participating businesses/);
  assert.match(html, /Helpful guides/);
  assert.match(html, /How to store fresh herbs/);
  assert.match(html, /Easy shopping, secure payment, clear support/);
  assert.doesNotMatch(html, /Commerce window/);
  assert.doesNotMatch(html, /Cart window/);
  assert.doesNotMatch(html, /Member resume/);
  assert.doesNotMatch(html, /Store metrics/);
  assert.ok(html.includes('href="/en-US/catalog?category=fruit"'));
  assert.ok(html.includes('href="/en-US/loyalty"'));
  assert.ok(html.includes('href="/en-US/page/herb-guide"'));
});

test("HomePageComposer falls back to the generic page composer when the home hero is missing", () => {
  const html = renderToStaticMarkup(
    React.createElement(HomePageComposer, {
      parts: [
        {
          id: "fallback-shortcuts",
          kind: "card-grid",
          eyebrow: "Shortcuts",
          title: "Fallback content",
          description: "Use generic rendering when the dedicated home hero is unavailable.",
          cards: [
            {
              id: "fallback-card",
              title: "Catalog",
              description: "Generic page composer should still render content.",
              href: "/catalog",
              ctaLabel: "Open catalog",
            },
          ],
          emptyMessage: "No content",
        },
      ],
      culture: "en-US",
    }),
  );

  assert.match(html, /Fallback content/);
  assert.match(html, /href=\"#fallback-shortcuts\"/);
  assert.match(html, /Open catalog/);
});
