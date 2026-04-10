import test from "node:test";
import assert from "node:assert/strict";
import {
  getPublicApiCacheIdentity,
  getPublicApiCachePolicy,
  getPublicApiFetchCacheOptions,
  getPublicApiKeyTag,
  getPublicApiPathTag,
  getPublicApiRequestPlan,
  getPublicApiRevalidate,
  normalizePublicApiCachePath,
} from "@/lib/public-api-cache";

test("public API cache helpers keep revalidate windows and tag identity explicit", () => {
  assert.equal(getPublicApiRevalidate("cms-menu"), 900);
  assert.equal(getPublicApiRevalidate("catalog-products"), 120);
  assert.equal(getPublicApiRevalidate("unknown-feed"), 60);
  assert.equal(getPublicApiKeyTag("catalog-products"), "public:catalog-products");

  assert.equal(
    getPublicApiPathTag(
      "/api/v1/public/catalog/products?pageSize=12&page=1&culture=de-DE",
    ),
    "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
  );
});

test("getPublicApiCacheIdentity keeps normalized paths and canonical tags together", () => {
  assert.deepEqual(
    getPublicApiCacheIdentity(
      "catalog-products",
      "/api/v1/public/catalog/products?pageSize=12&page=1&culture=de-DE",
    ),
    {
      normalizedPath:
        "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
      revalidate: 120,
      keyTag: "public:catalog-products",
      pathTag:
        "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
      tags: [
        "public:catalog-products",
        "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
      ],
    },
  );
});

test("getPublicApiFetchCacheOptions keeps GET cacheable and mutations no-store", () => {
  const identity = getPublicApiCacheIdentity(
    "catalog-products",
    "/api/v1/public/catalog/products?pageSize=12&page=1&culture=de-DE",
  );

  assert.deepEqual(getPublicApiFetchCacheOptions(identity), {
    cache: "force-cache",
    next: {
      revalidate: 120,
      tags: [
        "public:catalog-products",
        "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
      ],
    },
  });

  assert.deepEqual(getPublicApiFetchCacheOptions(identity, "POST"), {
    cache: "no-store",
  });
});

test("getPublicApiRequestPlan keeps request URL, normalized path, and cache mode together", () => {
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?pageSize=12&page=1&culture=de-DE",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
        revalidate: 120,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
        ],
      },
      fetchCacheOptions: {
        cache: "force-cache",
        next: {
          revalidate: 120,
          tags: [
            "public:catalog-products",
            "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
          ],
        },
      },
    },
  );
});

test("getPublicApiCachePolicy returns long-lived cache windows for stable CMS and category feeds", () => {
  assert.deepEqual(
    getPublicApiCachePolicy("cms-menu", "/api/v1/public/cms/menus/main-navigation"),
    {
      revalidate: 900,
      tags: [
        "public:cms-menu",
        "path:/api/v1/public/cms/menus/main-navigation",
      ],
    },
  );

  assert.deepEqual(
    getPublicApiCachePolicy("catalog-categories", "/api/v1/public/catalog/categories?page=1&pageSize=100"),
    {
      revalidate: 900,
      tags: [
        "public:catalog-categories",
        "path:/api/v1/public/catalog/categories?page=1&pageSize=100",
      ],
    },
  );
});

test("getPublicApiCachePolicy keeps catalog browse and unknown feeds on shorter windows", () => {
  assert.equal(
    getPublicApiCachePolicy("catalog-products", "/api/v1/public/catalog/products?page=1").revalidate,
    120,
  );
  assert.equal(
    getPublicApiCachePolicy("unknown-feed", "/api/v1/public/unknown").revalidate,
    60,
  );
});

test("normalizePublicApiCachePath keeps equivalent query strings on one canonical tag", () => {
  assert.equal(
    normalizePublicApiCachePath(
      "/api/v1/public/catalog/products?pageSize=12&page=1&culture=de-DE",
    ),
    "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-products",
      "/api/v1/public/catalog/products?pageSize=12&page=1&culture=de-DE",
    ).tags[1],
    "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
  );
});

test("getPublicApiCachePolicy keeps CMS search and catalog review windows on shorter path-aware cache windows", () => {
  assert.equal(
    getPublicApiCachePolicy(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=12&search=story",
    ).revalidate,
    120,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
    ).revalidate,
    120,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleState=offers&savingsBand=hero",
    ).revalidate,
    90,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&search=coffee",
    ).revalidate,
    90,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-products",
      "/api/v1/public/catalog/products?categorySlug=coffee&page=1&pageSize=12&culture=de-DE",
    ).revalidate,
    90,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
    ).revalidate,
    90,
  );
});

test("getPublicApiCachePolicy keeps CMS and catalog detail routes on fresher path-aware cache windows", () => {
  assert.equal(
    getPublicApiCachePolicy(
      "cms-page",
      "/api/v1/public/cms/pages/about?culture=de-DE",
    ).revalidate,
    180,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-product-detail",
      "/api/v1/public/catalog/products/apples?culture=de-DE",
    ).revalidate,
    120,
  );
});
