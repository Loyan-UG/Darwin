import assert from "node:assert/strict";
import test from "node:test";
import { createMemberProtectedPageLoaderCore } from "@/features/member-portal/server/create-member-protected-page-loader-core";

test("createMemberProtectedPageLoader skips protected route loading when the member session is missing", async () => {
  let protectedExecutions = 0;
  const loader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    getContext: (culture: string, id: string) => ({ culture, id }),
    getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
    loadEntryContext: async () => ({
      session: null,
      storefrontContext: {
        cmsPagesResult: { status: "ok", data: { items: [] } },
        cmsPagesStatus: "ok",
        cmsPages: [],
        categoriesResult: { status: "ok", data: { items: [] } },
        categoriesStatus: "ok",
        categories: [],
        productsResult: { status: "ok", data: { items: [] } },
        productsStatus: "ok",
        products: [],
        storefrontCart: null,
        storefrontCartStatus: "not-found",
        cartSnapshots: [],
        cartLinkedProductSlugs: [],
      },
    }),
    summarizeAuthorized: (routeContext) => ({
      detailStatus: routeContext.status,
    }),
    loadRouteContext: async () => {
      protectedExecutions += 1;
      return { status: "ok" };
    },
  });

  const result = await loader("de-DE", "order-1");

  assert.equal(result.entryContext.session, null);
  assert.equal(result.routeContext, null);
  assert.equal(protectedExecutions, 0);
});

test("createMemberProtectedPageLoader keeps authorized route loading behind the shared entry gate", async () => {
  let protectedExecutions = 0;
  const loader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    getContext: (culture: string, id: string) => ({ culture, id }),
    getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
    loadEntryContext: async () => ({
      session: {
        customerId: "customer-1",
        emailAddress: "member@example.com",
        isAuthenticated: true,
      },
      storefrontContext: null,
    }),
    summarizeAuthorized: (routeContext) => ({
      detailStatus: routeContext.status,
    }),
    loadRouteContext: async (_culture: string, id: string) => {
      protectedExecutions += 1;
      return { status: "ok", id };
    },
  });

  const result = await loader("de-DE", "order-1");

  assert.equal(result.entryContext.session?.customerId, "customer-1");
  assert.deepEqual(result.routeContext, { status: "ok", id: "order-1" });
  assert.equal(protectedExecutions, 1);
});
