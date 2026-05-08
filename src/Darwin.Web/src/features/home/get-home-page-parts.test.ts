import assert from "node:assert/strict";
import test from "node:test";
import { getHomePageParts } from "@/features/home/get-home-page-parts";
import type { getHomeDiscoveryContext } from "@/features/home/server/get-home-discovery-context";

function createHomeDiscoveryContext(): Awaited<ReturnType<typeof getHomeDiscoveryContext>> {
  return {
    storefrontContext: {
      cmsPagesResult: {
        status: "ok",
        data: {
          items: [
            {
              id: "page-1",
              slug: "story-one",
              title: "Story One",
              metaTitle: "Story One",
              metaDescription: "Story description",
            },
          ],
        },
      },
      cmsPages: [
        {
          id: "page-1",
          slug: "story-one",
          title: "Story One",
          metaTitle: "Story One",
          metaDescription: "Story description",
        },
      ],
      cmsPagesStatus: "ok",
      categoriesResult: {
        status: "ok",
        data: {
          items: [
            {
              id: "cat-1",
              slug: "coffee",
              name: "Coffee",
              description: "Coffee category",
            },
          ],
        },
      },
      categories: [
        {
          id: "cat-1",
          slug: "coffee",
          name: "Coffee",
          description: "Coffee category",
        },
      ],
      categoriesStatus: "ok",
      productsResult: {
        status: "ok",
        data: {
          items: [
            {
              id: "prod-1",
              slug: "hero-coffee",
              name: "Hero Coffee",
              shortDescription: "Strong offer",
              priceMinor: 1000,
              compareAtPriceMinor: 1600,
              currency: "EUR",
              primaryImageUrl: null,
              variants: [],
            },
            {
              id: "prod-2",
              slug: "value-coffee",
              name: "Value Coffee",
              shortDescription: "Good value",
              priceMinor: 1200,
              compareAtPriceMinor: 1500,
              currency: "EUR",
              primaryImageUrl: null,
              variants: [],
            },
            {
              id: "prod-3",
              slug: "base-coffee",
              name: "Base Coffee",
              shortDescription: "Steady pick",
              priceMinor: 900,
              compareAtPriceMinor: null,
              currency: "EUR",
              primaryImageUrl: null,
              variants: [],
            },
          ],
        },
      },
      products: [
        {
          id: "prod-1",
          slug: "hero-coffee",
          name: "Hero Coffee",
          shortDescription: "Strong offer",
          priceMinor: 1000,
          compareAtPriceMinor: 1600,
          currency: "EUR",
          primaryImageUrl: null,
          variants: [],
        },
        {
          id: "prod-2",
          slug: "value-coffee",
          name: "Value Coffee",
          shortDescription: "Good value",
          priceMinor: 1200,
          compareAtPriceMinor: 1500,
          currency: "EUR",
          primaryImageUrl: null,
          variants: [],
        },
        {
          id: "prod-3",
          slug: "base-coffee",
          name: "Base Coffee",
          shortDescription: "Steady pick",
          priceMinor: 900,
          compareAtPriceMinor: null,
          currency: "EUR",
          primaryImageUrl: null,
          variants: [],
        },
      ],
      productsStatus: "ok",
      storefrontCart: null,
      storefrontCartStatus: "not-found",
      cartSnapshots: [],
      cartLinkedProductSlugs: [],
    },
    pagesResult: {
      status: "ok",
      data: {
        items: [
          {
            id: "page-1",
            slug: "story-one",
            title: "Story One",
            metaTitle: "Story One",
            metaDescription: "Story description",
          },
        ],
      },
    },
    productsResult: {
      status: "ok",
      data: {
        items: [
          {
            id: "prod-1",
            slug: "hero-coffee",
            name: "Hero Coffee",
            shortDescription: "Strong offer",
            priceMinor: 1000,
            compareAtPriceMinor: 1600,
            currency: "EUR",
            primaryImageUrl: null,
            variants: [],
          },
          {
            id: "prod-2",
            slug: "value-coffee",
            name: "Value Coffee",
            shortDescription: "Good value",
            priceMinor: 1200,
            compareAtPriceMinor: 1500,
            currency: "EUR",
            primaryImageUrl: null,
            variants: [],
          },
          {
            id: "prod-3",
            slug: "base-coffee",
            name: "Base Coffee",
            shortDescription: "Steady pick",
            priceMinor: 900,
            compareAtPriceMinor: null,
            currency: "EUR",
            primaryImageUrl: null,
            variants: [],
          },
        ],
      },
    },
    categoriesResult: {
      status: "ok",
      data: {
        items: [
          {
            id: "cat-1",
            slug: "coffee",
            name: "Coffee",
            description: "Coffee category",
          },
        ],
      },
    },
    categorySpotlights: [
      {
        category: {
          id: "cat-1",
          slug: "coffee",
          name: "Coffee",
          description: "Coffee category",
        },
        status: "ok",
        product: {
          id: "prod-1",
          slug: "hero-coffee",
          name: "Hero Coffee",
          shortDescription: "Strong offer",
          priceMinor: 1000,
          compareAtPriceMinor: 1600,
          currency: "EUR",
          primaryImageUrl: null,
          variants: [],
        },
      },
    ],
  } as Awaited<ReturnType<typeof getHomeDiscoveryContext>>;
}

test("getHomePageParts returns a concise customer-facing home page model", async () => {
  const parts = await getHomePageParts("en-US", null, createHomeDiscoveryContext());

  assert.deepEqual(
    parts.map((part) => part.id),
    [
      "home-hero",
      "home-category-spotlight",
      "home-product-spotlight",
      "home-loyalty-teaser",
      "home-cms-spotlight",
      "home-trust-strip",
    ],
  );

  const hero = parts.find((part) => part.id === "home-hero");
  assert.ok(hero);
  assert.equal(hero.kind, "hero");
  assert.ok("actions" in hero);
  assert.deepEqual(
    hero.actions.map((action) => action.href),
    ["/catalog", "/loyalty", "/account"],
  );

  const categories = parts.find((part) => part.id === "home-category-spotlight");
  assert.ok(categories);
  assert.equal(categories.kind, "card-grid");
  assert.ok("cards" in categories);
  assert.equal(categories.cards.length, 1);
  assert.equal(categories.cards[0].href, "/catalog?category=coffee");

  const products = parts.find((part) => part.id === "home-product-spotlight");
  assert.ok(products);
  assert.equal(products.kind, "card-grid");
  assert.ok("cards" in products);
  assert.equal(products.cards.length, 3);
  assert.equal(products.cards[0].title, "Hero Coffee");
  assert.equal(products.cards[0].href, "/catalog/hero-coffee");

  const loyalty = parts.find((part) => part.id === "home-loyalty-teaser");
  assert.ok(loyalty);
  assert.equal(loyalty.kind, "blank-state");
  assert.ok("actions" in loyalty);
  assert.deepEqual(
    loyalty.actions.map((action) => action.href),
    ["/loyalty", "/account"],
  );

  const help = parts.find((part) => part.id === "home-cms-spotlight");
  assert.ok(help);
  assert.equal(help.kind, "card-grid");
  assert.ok("cards" in help);
  assert.equal(help.cards.length, 1);
  assert.equal(help.cards[0].href, "/page/story-one");

  const trust = parts.find((part) => part.id === "home-trust-strip");
  assert.ok(trust);
  assert.equal(trust.kind, "card-grid");
  assert.ok("cards" in trust);
  assert.deepEqual(
    trust.cards.map((card) => card.href),
    ["/page/shipping", "/page/payment", "/page/returns"],
  );

  assert.equal(parts.find((part) => part.id === "home-promotion-lanes"), undefined);
  assert.equal(parts.find((part) => part.id === "home-route-map"), undefined);
  assert.equal(parts.find((part) => part.id === "home-browse-readiness"), undefined);
});

test("getHomePageParts keeps degraded discovery customer-safe with empty states", async () => {
  const degradedContext = createHomeDiscoveryContext();
  degradedContext.pagesResult = { status: "error", message: "cms degraded" };
  degradedContext.productsResult = { status: "error", message: "catalog degraded" };
  degradedContext.categoriesResult = { status: "error", message: "categories degraded" };

  const parts = await getHomePageParts("en-US", null, degradedContext);

  assert.deepEqual(
    parts.map((part) => part.id),
    [
      "home-hero",
      "home-category-spotlight",
      "home-product-spotlight",
      "home-loyalty-teaser",
      "home-cms-spotlight",
      "home-trust-strip",
    ],
  );

  const categories = parts.find((part) => part.id === "home-category-spotlight");
  const products = parts.find((part) => part.id === "home-product-spotlight");
  const help = parts.find((part) => part.id === "home-cms-spotlight");
  assert.ok(categories && "cards" in categories && "emptyMessage" in categories);
  assert.ok(products && "cards" in products && "emptyMessage" in products);
  assert.ok(help && "cards" in help && "emptyMessage" in help);
  assert.equal(categories.cards.length, 0);
  assert.equal(products.cards.length, 0);
  assert.equal(help.cards.length, 0);
  assert.equal(categories.emptyMessage, "Categories are temporarily unavailable.");
  assert.equal(products.emptyMessage, "Products are temporarily unavailable.");
  assert.equal(help.emptyMessage, "Help pages are temporarily unavailable.");

  assert.equal(parts.find((part) => part.id === "home-recovery-rail"), undefined);
  assert.equal(parts.find((part) => part.kind === "route-map"), undefined);
  assert.equal(parts.find((part) => part.kind === "status-list"), undefined);
});

