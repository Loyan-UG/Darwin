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

test("getHomePageParts keeps home promotion lanes on a real merchandising card board", async () => {
  const parts = await getHomePageParts("en-US", null, createHomeDiscoveryContext());

  const promotionLanes = parts.find((part) => part.id === "home-promotion-lanes");
  assert.ok(promotionLanes);
  assert.equal(promotionLanes.kind, "card-grid");
  assert.ok("cards" in promotionLanes);
  assert.equal(promotionLanes.cards.length, 4);
  assert.match(promotionLanes.cards[0].href, /\/catalog/);

  const routeMap = parts.find((part) => part.id === "home-route-map");
  assert.ok(routeMap);
  assert.equal(routeMap.kind, "route-map");
  assert.ok("items" in routeMap);
  assert.equal(routeMap.items.length, 3);

  const readiness = parts.find((part) => part.id === "home-browse-readiness");
  assert.ok(readiness);
  assert.equal(readiness.kind, "status-list");
  assert.ok("items" in readiness);
  assert.equal(readiness.items.length, 2);
});
