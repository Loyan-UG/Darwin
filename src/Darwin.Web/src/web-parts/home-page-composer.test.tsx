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
    description: "Shop produce, pantry essentials, and quick follow-up journeys from one storefront.",
    actions: [
      { label: "Browse catalog", href: "/catalog" },
      { label: "Open checkout", href: "/checkout", variant: "secondary" },
    ],
    highlights: [
      "Daily produce refresh",
      "Hero and value offers stay visible",
      "Checkout and member follow-up stay in view",
    ],
    panelTitle: "Storefront board",
  },
  {
    id: "home-metrics",
    kind: "stat-grid",
    eyebrow: "Store metrics",
    title: "Live discovery signals",
    description: "Counts for the current grocery storefront.",
    metrics: [
      { id: "metric-1", label: "Pages", value: "12", note: "CMS stays healthy" },
      { id: "metric-2", label: "Products", value: "38", note: "Catalog stays healthy" },
      { id: "metric-3", label: "Categories", value: "9", note: "Category browse stays healthy" },
      { id: "metric-4", label: "Cultures", value: "2", note: "Localized routes stay ready" },
    ],
  },
  {
    id: "home-priority-lane",
    kind: "link-list",
    eyebrow: "Priority board",
    title: "Best next actions",
    description: "The storefront should keep the strongest next steps explicit.",
    items: [
      {
        id: "priority-checkout",
        title: "Resume checkout for 3 items",
        description: "Cart continuity remains visible on the home page.",
        href: "/checkout",
        ctaLabel: "Open checkout",
        meta: "cart:ok",
      },
      {
        id: "priority-order",
        title: "Track latest order",
        description: "Member follow-up stays visible.",
        href: "/orders/ord-1",
        ctaLabel: "Open order",
        meta: "orders:ok",
      },
      {
        id: "priority-product",
        title: "Review strongest offer",
        description: "Hero merchandising should stay visible.",
        href: "/catalog/apples",
        ctaLabel: "Open product",
        meta: "products:ok",
      },
    ],
    emptyMessage: "No priority actions",
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
    id: "home-offer-board",
    kind: "card-grid",
    eyebrow: "Offer board",
    title: "Top weekly offers",
    description: "Promotional products should read like a grocery carousel even without a slider.",
    cards: [
      {
        id: "offer-apples",
        eyebrow: "Hero offer",
        title: "Organic apples",
        description: "Crunchy apples discounted for this week.",
        href: "/catalog/apples",
        ctaLabel: "Open product",
        meta: "€7.00",
      },
      {
        id: "offer-bananas",
        eyebrow: "Value offer",
        title: "Bananas bundle",
        description: "Breakfast fruit for the whole week.",
        href: "/catalog/bananas",
        ctaLabel: "Open product",
        meta: "€4.50",
      },
    ],
    emptyMessage: "No offers",
  },
  {
    id: "home-promotion-lanes",
    kind: "card-grid",
    eyebrow: "Promotion lanes",
    title: "Browse offer lanes directly",
    description: "Explicit hero/value/live/base merchandising paths stay visible.",
    cards: [
      {
        id: "lane-hero",
        eyebrow: "Hero offers",
        title: "Hero offers around apples",
        description: "1 item starts at €7.00.",
        href: "/catalog?visibleState=offers&visibleSort=offers-first&savingsBand=hero",
        ctaLabel: "Open promotion lane",
        meta: "1 item",
      },
      {
        id: "lane-value",
        eyebrow: "Value offers",
        title: "Value offers for pantry baskets",
        description: "Everyday savings remain available.",
        href: "/catalog?visibleState=offers&visibleSort=offers-first&savingsBand=value",
        ctaLabel: "Open promotion lane",
        meta: "3 items",
      },
    ],
    emptyMessage: "No lanes",
  },
  {
    id: "home-campaign-board",
    kind: "card-grid",
    eyebrow: "Campaign board",
    title: "Seasonal stories",
    description: "Editorial category campaigns should stay visible next to offers.",
    cards: [
      {
        id: "campaign-spring",
        eyebrow: "Featured aisle",
        title: "Spring salad week",
        description: "Leafy greens and dressings bundled together.",
        href: "/catalog?category=greens",
        ctaLabel: "Open campaign",
        meta: "greens",
      },
    ],
    emptyMessage: "No campaigns",
  },
  {
    id: "home-cms-spotlight",
    kind: "card-grid",
    eyebrow: "Store guide",
    title: "Editorial picks",
    description: "CMS guidance should stay discoverable.",
    cards: [
      {
        id: "cms-1",
        eyebrow: "Guide",
        title: "How to store fresh herbs",
        description: "Simple freshness tips for home kitchens.",
        href: "/cms/herb-guide",
        ctaLabel: "Read guide",
        meta: "herb-guide",
      },
    ],
    emptyMessage: "No guides",
  },
  {
    id: "home-route-map",
    kind: "route-map",
    eyebrow: "Route map",
    title: "Move between storefront surfaces",
    description: "Primary and secondary journeys should stay explicit on home.",
    items: [
      {
        id: "map-catalog",
        label: "Catalog",
        title: "Open the grocery catalog",
        description: "Jump from home into browse with one action.",
        primaryHref: "/catalog",
        primaryCtaLabel: "Browse catalog",
        secondaryHref: "/catalog/apples",
        secondaryCtaLabel: "Open apples",
        meta: "catalog:ok",
      },
      {
        id: "map-cms",
        label: "CMS",
        title: "Read current grocery guides",
        description: "Editorial pages stay near commerce.",
        primaryHref: "/cms",
        primaryCtaLabel: "Open CMS",
        secondaryHref: "/cms/herb-guide",
        secondaryCtaLabel: "Read guide",
        meta: "cms:ok",
      },
    ],
    emptyMessage: "No routes",
  },
  {
    id: "home-commerce-opportunity",
    kind: "status-list",
    eyebrow: "Commerce window",
    title: "Close the purchase loop",
    description: "Checkout and live cart opportunities should stay visible.",
    items: [
      {
        id: "commerce-cart",
        label: "Cart",
        title: "3 items ready for checkout",
        description: "Continue from cart into checkout.",
        href: "/checkout",
        ctaLabel: "Open checkout",
        tone: "ok",
        meta: "€23.00",
      },
      {
        id: "commerce-account",
        label: "Account",
        title: "Update delivery details",
        description: "Keep member self-service accessible.",
        href: "/account/addresses",
        ctaLabel: "Open addresses",
        tone: "warning",
        meta: "profile:pending",
      },
    ],
    emptyMessage: "No opportunities",
  },
  {
    id: "home-cart-window",
    kind: "status-list",
    eyebrow: "Cart window",
    title: "Cart continuity",
    description: "Cart and checkout state should stay explicit from home.",
    items: [
      {
        id: "cart-open",
        label: "Cart",
        title: "Review basket",
        description: "Return to cart with your saved items.",
        href: "/cart",
        ctaLabel: "Open cart",
        tone: "ok",
        meta: "basket:live",
      },
    ],
    emptyMessage: "No cart",
  },
  {
    id: "home-member-resume",
    kind: "link-list",
    eyebrow: "Member resume",
    title: "ada@example.com",
    description: "Member journeys should stay visible from home.",
    items: [
      {
        id: "resume-order",
        title: "Latest order",
        description: "Track the current shipment.",
        href: "/orders/ord-1",
        ctaLabel: "Open order",
        meta: "orders:ok",
      },
      {
        id: "resume-loyalty",
        title: "Loyalty wallet",
        description: "Check next reward progress.",
        href: "/loyalty/store-1",
        ctaLabel: "Open loyalty",
        meta: "loyalty:ok",
      },
    ],
    emptyMessage: "No member data",
  },
  {
    id: "home-cart-resume",
    kind: "link-list",
    eyebrow: "Cart resume",
    title: "Resume saved basket",
    description: "Saved cart items should stay accessible from home.",
    items: [
      {
        id: "resume-cart",
        title: "Organic apples",
        description: "SKU APL-1",
        href: "/catalog/apples",
        ctaLabel: "Open product",
        meta: "APL-1",
      },
      {
        id: "resume-checkout",
        title: "Go to checkout",
        description: "Finish purchase without losing context.",
        href: "/checkout",
        ctaLabel: "Open checkout",
        meta: "cart:live",
      },
    ],
    emptyMessage: "No cart data",
  },
  {
    id: "home-recovery-rail",
    kind: "link-list",
    eyebrow: "Recovery rail",
    title: "Fallback routes stay visible",
    description: "Home should still expose the main surfaces when one family degrades.",
    items: [
      {
        id: "recovery-cms",
        title: "Open CMS",
        description: "Recover editorial browsing.",
        href: "/cms",
        ctaLabel: "Open CMS",
        meta: "cms:ok",
      },
      {
        id: "recovery-account",
        title: "Open account",
        description: "Recover member workflows.",
        href: "/account",
        ctaLabel: "Open account",
        meta: "account:ready",
      },
    ],
    emptyMessage: "No recovery routes",
  },
  {
    id: "home-shortcuts",
    kind: "card-grid",
    eyebrow: "Quick shortcuts",
    title: "Start from the main routes",
    description: "Shortcut tiles should stay visible at the bottom of the landing page.",
    cards: [
      {
        id: "shortcut-catalog",
        eyebrow: "Catalog",
        title: "Browse products",
        description: "Open the grocery assortment.",
        href: "/catalog",
        ctaLabel: "Browse catalog",
      },
      {
        id: "shortcut-account",
        eyebrow: "Account",
        title: "Manage account",
        description: "Update self-service details.",
        href: "/account",
        ctaLabel: "Open account",
      },
    ],
    emptyMessage: "No shortcuts",
  },
  {
    id: "home-journeys",
    kind: "link-list",
    eyebrow: "Journeys",
    title: "Core entry points",
    description: "Key home journeys should stay explicit and clickable.",
    items: [
      {
        id: "journey-catalog",
        title: "Catalog journey",
        description: "Browse into product detail.",
        href: "/catalog",
        ctaLabel: "Open catalog",
        meta: "products:ok",
      },
      {
        id: "journey-account",
        title: "Account journey",
        description: "Resume profile and orders.",
        href: "/account",
        ctaLabel: "Open account",
        meta: "member:ready",
      },
    ],
    emptyMessage: "No journeys",
  },
];

test("HomePageComposer renders the dedicated grocery landing layout from home parts", () => {
  const html = renderToStaticMarkup(
    React.createElement(HomePageComposer, {
      parts: homeParts,
      culture: "en-US",
    }),
  );

  assert.match(html, /Weekly grocery deals for the whole household/);
  assert.match(html, /Grocery-first merchandising/);
  assert.match(html, /Fresh aisles/);
  assert.match(html, /Seasonal fruit/);
  assert.match(html, /Organic apples/);
  assert.match(html, /Hero offers around apples/);
  assert.match(html, /Move between storefront surfaces/);
  assert.match(html, /ada@example\.com/);
  assert.match(html, /Resume saved basket/);
  assert.match(html, /Fallback routes stay visible/);
  assert.match(html, /Start from the main routes/);
  assert.ok(
    html.includes('href="/en-US/catalog?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero"'),
  );
  assert.ok(html.includes('href="/checkout"'));
  assert.ok(html.includes('href="/orders/ord-1"'));
  assert.ok(html.includes('href="/en-US/cms/herb-guide"'));
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
