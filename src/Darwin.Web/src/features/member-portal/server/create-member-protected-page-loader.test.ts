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

test("createMemberProtectedPageLoader keeps a stable entry route and auth gate across guest and member paths", async () => {
  const guestLoader = createMemberProtectedPageLoaderCore({
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
    summarizeAuthorized: () => ({ detailStatus: "ok" }),
    loadRouteContext: async () => ({ status: "ok" }),
  });

  const memberLoader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    getContext: (culture: string, id: string) => ({ culture, id }),
    getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
    loadEntryContext: async () => ({
      session: { customerId: "customer-1" },
      storefrontContext: null,
    }),
    summarizeAuthorized: () => ({ detailStatus: "ok" }),
    loadRouteContext: async () => ({ status: "ok" }),
  });

  const guest = await guestLoader("de-DE", "order-1");
  const member = await memberLoader("de-DE", "order-1");

  assert.equal(guest.routeContext, null);
  assert.equal(member.routeContext?.status, "ok");
});

test("createMemberProtectedPageLoader normalizes equivalent arguments before caching", async () => {
  const loader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    normalizeArgs: (culture: string, id: string) => [culture.trim(), id.trim()] as [string, string],
    getContext: (culture: string, id: string) => ({ culture, id }),
    getEntryRoute: (_culture: string, id: string) => `/orders/${id}`,
    loadEntryContext: async () => ({
      session: { customerId: "customer-1" },
      storefrontContext: null,
    }),
    summarizeAuthorized: (routeContext) => ({ detailStatus: routeContext.status }),
    loadRouteContext: async (_culture: string, id: string) => ({ status: "ok", id }),
  });

  const [first, second] = await Promise.all([
    loader("de-DE", "order-1"),
    loader(" de-DE ", " order-1 "),
  ]);

  assert.equal(first.routeContext?.id, "order-1");
  assert.equal(second.routeContext?.id, "order-1");
});

test("createMemberProtectedPageLoader feeds normalized args into entry route and diagnostics", async () => {
  let contextSnapshot: Record<string, unknown> | undefined;
  let successSnapshot: Record<string, unknown> | undefined;
  let entryRouteSnapshot: string | undefined;

  const loader = createMemberProtectedPageLoaderCore({
    operation: "unit-protected-page",
    normalizeArgs: (culture: string, id: string) =>
      [culture.trim(), id.trim()] as [string, string],
    getContext: (culture: string, id: string) => {
      contextSnapshot = { culture, id };
      return { culture, id };
    },
    getEntryRoute: (_culture: string, id: string) => {
      entryRouteSnapshot = `/orders/${id}`;
      return entryRouteSnapshot;
    },
    loadEntryContext: async () => ({
      session: { customerId: "customer-1" },
      storefrontContext: null,
    }),
    summarizeAuthorized: (routeContext) => {
      successSnapshot = {
        detailStatus: routeContext.status,
        id: routeContext.id,
      };

      return {
        detailStatus: routeContext.status,
        detailId: routeContext.id,
      };
    },
    loadRouteContext: async (_culture: string, id: string) => ({
      status: "ok",
      id,
    }),
  });

  const result = await loader(" de-DE ", " order-1 ");

  assert.equal(result.routeContext?.id, "order-1");
  assert.deepEqual(contextSnapshot, {
    culture: "de-DE",
    id: "order-1",
  });
  assert.equal(entryRouteSnapshot, "/orders/order-1");
  assert.deepEqual(successSnapshot, {
    detailStatus: "ok",
    id: "order-1",
  });
});
