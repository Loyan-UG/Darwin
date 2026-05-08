import test from "node:test";
import assert from "node:assert/strict";
import {
  getPublicApiCacheIdentity,
  getPublicApiCachePolicy,
  getPublicApiFetchCacheOptions,
  getPublicApiKeyTag,
  getPublicApiCacheProfile,
  getPublicApiPathCharacteristics,
  getPublicApiPathTag,
  getPublicApiProfileRevalidate,
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

test("getPublicApiPathCharacteristics keeps query-driven cache signals explicit", () => {
  assert.deepEqual(
    getPublicApiPathCharacteristics(
      "/api/v1/public/catalog/products?culture=de-DE&page=3&pageSize=12&search=coffee&categorySlug=coffee&visibleState=offers&visibleSort=offers-first&mediaState=with-image&savingsBand=hero",
    ),
    {
      hasSearch: true,
      hasCategorySlug: true,
      hasVisibleState: true,
      hasVisibleSort: true,
      hasMediaState: true,
      hasSavingsBand: true,
      page: 3,
      pageSize: 12,
    },
  );

  assert.deepEqual(
    getPublicApiPathCharacteristics(
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
    ),
    {
      hasSearch: false,
      hasCategorySlug: false,
      hasVisibleState: false,
      hasVisibleSort: false,
      hasMediaState: false,
      hasSavingsBand: false,
      page: 1,
      pageSize: 48,
    },
  );
});

test("getPublicApiCacheProfile keeps stable, detail, filtered, and paged discovery buckets explicit", () => {
  assert.equal(
    getPublicApiCacheProfile(
      "cms-menu",
      "/api/v1/public/cms/menus/main-navigation",
    ),
    "stable",
  );

  assert.equal(
    getPublicApiCacheProfile(
      "cms-page",
      "/api/v1/public/cms/pages/faq?culture=de-DE",
    ),
    "detail",
  );

  assert.equal(
    getPublicApiCacheProfile(
      "catalog-product-detail",
      "/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
    ),
    "detail",
  );

  assert.equal(
    getPublicApiCacheProfile(
      "catalog-categories",
      "/api/v1/public/catalog/categories?culture=de-DE&page=1&pageSize=100",
    ),
    "category-heavy",
  );

  assert.equal(
    getPublicApiCacheProfile(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48&search=story",
    ),
    "discovery-filtered",
  );

  assert.equal(
    getPublicApiCacheProfile(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleSort=offers-first",
    ),
    "discovery-filtered",
  );

  assert.equal(
    getPublicApiCacheProfile(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=3&pageSize=12",
    ),
    "discovery-paged",
  );

  assert.equal(
    getPublicApiCacheProfile(
      "unknown-feed",
      "/api/v1/public/unknown",
    ),
    "default",
  );
});

test("getPublicApiProfileRevalidate keeps profile windows explicit per feed family", () => {
  assert.equal(getPublicApiProfileRevalidate("cms-menu", "stable"), 900);
  assert.equal(getPublicApiProfileRevalidate("cms-page", "detail"), 180);
  assert.equal(
    getPublicApiProfileRevalidate("catalog-product-detail", "detail"),
    120,
  );
  assert.equal(
    getPublicApiProfileRevalidate("catalog-categories", "category-heavy"),
    300,
  );
  assert.equal(
    getPublicApiProfileRevalidate("cms-pages", "discovery-filtered"),
    120,
  );
  assert.equal(
    getPublicApiProfileRevalidate("catalog-products", "discovery-filtered"),
    90,
  );
  assert.equal(
    getPublicApiProfileRevalidate("cms-pages", "discovery-paged"),
    120,
  );
  assert.equal(
    getPublicApiProfileRevalidate("catalog-products", "discovery-paged"),
    90,
  );
  assert.equal(
    getPublicApiProfileRevalidate("unknown-feed", "default"),
    60,
  );
});

test("getPublicApiFetchCacheOptions canonicalizes HTTP method casing", () => {
  const identity = getPublicApiCacheIdentity(
    "catalog-products",
    "/api/v1/public/catalog/products?pageSize=12&page=1&culture=de-DE",
  );

  assert.deepEqual(getPublicApiFetchCacheOptions(identity, "get"), {
    cache: "force-cache",
    next: {
      revalidate: 120,
      tags: [
        "public:catalog-products",
        "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12",
      ],
    },
  });

  assert.deepEqual(getPublicApiFetchCacheOptions(identity, "post"), {
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
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "cms-menu",
      "/api/v1/public/cms/menus/main-navigation",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/cms/menus/main-navigation",
      cacheIdentity: {
        normalizedPath: "/api/v1/public/cms/menus/main-navigation",
        revalidate: 900,
        keyTag: "public:cms-menu",
        pathTag: "path:/api/v1/public/cms/menus/main-navigation",
        tags: [
          "public:cms-menu",
          "path:/api/v1/public/cms/menus/main-navigation",
        ],
      },
      fetchCacheOptions: {
        cache: "force-cache",
        next: {
          revalidate: 900,
          tags: [
            "public:cms-menu",
            "path:/api/v1/public/cms/menus/main-navigation",
          ],
        },
      },
    },
  );

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "cms-menu",
      "/api/v1/public/cms/menus/main-navigation",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/cms/menus/main-navigation",
      cacheIdentity: {
        normalizedPath: "/api/v1/public/cms/menus/main-navigation",
        revalidate: 900,
        keyTag: "public:cms-menu",
        pathTag: "path:/api/v1/public/cms/menus/main-navigation",
        tags: [
          "public:cms-menu",
          "path:/api/v1/public/cms/menus/main-navigation",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-categories",
      "/api/v1/public/catalog/categories?page=1&pageSize=100",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/categories?page=1&pageSize=100",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/categories?page=1&pageSize=100",
        revalidate: 300,
        keyTag: "public:catalog-categories",
        pathTag:
          "path:/api/v1/public/catalog/categories?page=1&pageSize=100",
        tags: [
          "public:catalog-categories",
          "path:/api/v1/public/catalog/categories?page=1&pageSize=100",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
  assert.deepEqual(
    getPublicApiCachePolicy("catalog-categories", "/api/v1/public/catalog/categories?page=1&pageSize=100"),
    {
      revalidate: 300,
      tags: [
        "public:catalog-categories",
        "path:/api/v1/public/catalog/categories?page=1&pageSize=100",
      ],
    },
  );

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-categories",
      "/api/v1/public/catalog/categories?page=1&pageSize=100",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/categories?page=1&pageSize=100",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/categories?page=1&pageSize=100",
        revalidate: 300,
        keyTag: "public:catalog-categories",
        pathTag:
          "path:/api/v1/public/catalog/categories?page=1&pageSize=100",
        tags: [
          "public:catalog-categories",
          "path:/api/v1/public/catalog/categories?page=1&pageSize=100",
        ],
      },
      fetchCacheOptions: {
        cache: "force-cache",
        next: {
          revalidate: 300,
          tags: [
            "public:catalog-categories",
            "path:/api/v1/public/catalog/categories?page=1&pageSize=100",
          ],
        },
      },
    },
  );
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-categories",
      "/api/v1/public/catalog/categories?culture=de-DE&page=1&pageSize=100",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/categories?culture=de-DE&page=1&pageSize=100",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/categories?culture=de-DE&page=1&pageSize=100",
        revalidate: 300,
        keyTag: "public:catalog-categories",
        pathTag:
          "path:/api/v1/public/catalog/categories?culture=de-DE&page=1&pageSize=100",
        tags: [
          "public:catalog-categories",
          "path:/api/v1/public/catalog/categories?culture=de-DE&page=1&pageSize=100",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
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

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "unknown-feed",
      "/api/v1/public/unknown",
    ),
    {
      requestUrl: "http://localhost:5134/api/v1/public/unknown",
      cacheIdentity: {
        normalizedPath: "/api/v1/public/unknown",
        revalidate: 60,
        keyTag: "public:unknown-feed",
        pathTag: "path:/api/v1/public/unknown",
        tags: [
          "public:unknown-feed",
          "path:/api/v1/public/unknown",
        ],
      },
      fetchCacheOptions: {
        cache: "force-cache",
        next: {
          revalidate: 60,
          tags: [
            "public:unknown-feed",
            "path:/api/v1/public/unknown",
          ],
        },
      },
    },
  );

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "unknown-feed",
      "/api/v1/public/unknown",
      "POST",
    ),
    {
      requestUrl: "http://localhost:5134/api/v1/public/unknown",
      cacheIdentity: {
        normalizedPath: "/api/v1/public/unknown",
        revalidate: 60,
        keyTag: "public:unknown-feed",
        pathTag: "path:/api/v1/public/unknown",
        tags: [
          "public:unknown-feed",
          "path:/api/v1/public/unknown",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
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

test("getPublicApiCachePolicy keeps CMS search and catalog browse windows on shorter path-aware cache windows", () => {
  assert.equal(
    getPublicApiCachePolicy(
      "catalog-categories",
      "/api/v1/public/catalog/categories?culture=de-DE&page=1&pageSize=100",
    ).revalidate,
    300,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-categories",
      "/api/v1/public/catalog/categories?culture=de-DE&page=3&pageSize=12",
    ).revalidate,
    300,
  );
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
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleSort=offers-first",
    ).revalidate,
    90,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&mediaState=with-image",
    ).revalidate,
    90,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=3&pageSize=12&visibleSort=offers-first&mediaState=with-image",
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

  assert.equal(
    getPublicApiCachePolicy(
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=3&pageSize=12",
    ).revalidate,
    120,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=3&pageSize=12",
    ).revalidate,
    90,
  );
});
test("getPublicApiCachePolicy keeps CMS and catalog detail routes on fresher path-aware windows", () => {
  assert.equal(
    getPublicApiCachePolicy(
      "cms-page",
      "/api/v1/public/cms/pages/faq?culture=de-DE",
    ).revalidate,
    180,
  );

  assert.equal(
    getPublicApiCachePolicy(
      "catalog-product-detail",
      "/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
    ).revalidate,
    120,
  );

  assert.deepEqual(
    getPublicApiCacheIdentity(
      "cms-page",
      "/api/v1/public/cms/pages/faq?culture=de-DE",
    ),
    {
      normalizedPath: "/api/v1/public/cms/pages/faq?culture=de-DE",
      revalidate: 180,
      keyTag: "public:cms-page",
      pathTag: "path:/api/v1/public/cms/pages/faq?culture=de-DE",
      tags: [
        "public:cms-page",
        "path:/api/v1/public/cms/pages/faq?culture=de-DE",
      ],
    },
  );

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "cms-page",
      "/api/v1/public/cms/pages/faq?culture=de-DE",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/cms/pages/faq?culture=de-DE",
      cacheIdentity: {
        normalizedPath: "/api/v1/public/cms/pages/faq?culture=de-DE",
        revalidate: 180,
        keyTag: "public:cms-page",
        pathTag: "path:/api/v1/public/cms/pages/faq?culture=de-DE",
        tags: [
          "public:cms-page",
          "path:/api/v1/public/cms/pages/faq?culture=de-DE",
        ],
      },
      fetchCacheOptions: {
        cache: "force-cache",
        next: {
          revalidate: 180,
          tags: [
            "public:cms-page",
            "path:/api/v1/public/cms/pages/faq?culture=de-DE",
          ],
        },
      },
    },
  );

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "cms-page",
      "/api/v1/public/cms/pages/faq?culture=de-DE",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/cms/pages/faq?culture=de-DE",
      cacheIdentity: {
        normalizedPath: "/api/v1/public/cms/pages/faq?culture=de-DE",
        revalidate: 180,
        keyTag: "public:cms-page",
        pathTag: "path:/api/v1/public/cms/pages/faq?culture=de-DE",
        tags: [
          "public:cms-page",
          "path:/api/v1/public/cms/pages/faq?culture=de-DE",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-product-detail",
      "/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
        revalidate: 120,
        keyTag: "public:catalog-product-detail",
        pathTag:
          "path:/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
        tags: [
          "public:catalog-product-detail",
          "path:/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
        ],
      },
      fetchCacheOptions: {
        cache: "force-cache",
        next: {
          revalidate: 120,
          tags: [
            "public:catalog-product-detail",
            "path:/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
          ],
        },
      },
    },
  );
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-product-detail",
      "/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
        revalidate: 120,
        keyTag: "public:catalog-product-detail",
        pathTag:
          "path:/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
        tags: [
          "public:catalog-product-detail",
          "path:/api/v1/public/catalog/products/coffee-machine?culture=de-DE",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
});





test("getPublicApiCacheIdentity keeps category-heavy and merchandising browse cache plans canonical", () => {
  assert.deepEqual(
    getPublicApiCacheIdentity(
      "catalog-categories",
      "/api/v1/public/catalog/categories?pageSize=12&page=3&culture=de-DE",
    ),
    {
      normalizedPath:
        "/api/v1/public/catalog/categories?culture=de-DE&page=3&pageSize=12",
      revalidate: 300,
      keyTag: "public:catalog-categories",
      pathTag:
        "path:/api/v1/public/catalog/categories?culture=de-DE&page=3&pageSize=12",
      tags: [
        "public:catalog-categories",
        "path:/api/v1/public/catalog/categories?culture=de-DE&page=3&pageSize=12",
      ],
    },
  );

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-categories",
      "/api/v1/public/catalog/categories?pageSize=12&page=3&culture=de-DE",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/categories?culture=de-DE&page=3&pageSize=12",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/categories?culture=de-DE&page=3&pageSize=12",
        revalidate: 300,
        keyTag: "public:catalog-categories",
        pathTag:
          "path:/api/v1/public/catalog/categories?culture=de-DE&page=3&pageSize=12",
        tags: [
          "public:catalog-categories",
          "path:/api/v1/public/catalog/categories?culture=de-DE&page=3&pageSize=12",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?mediaState=with-image&visibleSort=offers-first&pageSize=12&page=3&culture=de-DE",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=3&pageSize=12&visibleSort=offers-first",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=3&pageSize=12&visibleSort=offers-first",
        revalidate: 90,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=3&pageSize=12&visibleSort=offers-first",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=3&pageSize=12&visibleSort=offers-first",
        ],
      },
      fetchCacheOptions: {
        cache: "force-cache",
        next: {
          revalidate: 90,
          tags: [
            "public:catalog-products",
            "path:/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=3&pageSize=12&visibleSort=offers-first",
          ],
        },
      },
    },
  );
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?mediaState=with-image&visibleSort=offers-first&pageSize=12&page=3&culture=de-DE",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=3&pageSize=12&visibleSort=offers-first",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=3&pageSize=12&visibleSort=offers-first",
        revalidate: 90,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=3&pageSize=12&visibleSort=offers-first",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=3&pageSize=12&visibleSort=offers-first",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
});

test("getPublicApiRequestPlan keeps CMS pagination windows uncached for mutations", () => {
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=3&pageSize=12",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/cms/pages?culture=de-DE&page=3&pageSize=12",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=3&pageSize=12",
        revalidate: 120,
        keyTag: "public:cms-pages",
        pathTag:
          "path:/api/v1/public/cms/pages?culture=de-DE&page=3&pageSize=12",
        tags: [
          "public:cms-pages",
          "path:/api/v1/public/cms/pages?culture=de-DE&page=3&pageSize=12",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
});


test("getPublicApiRequestPlan keeps CMS search windows canonical and short-lived", () => {
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "cms-pages",
      "/api/v1/public/cms/pages?pageSize=48&search=story&page=1&culture=de-DE",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48&search=story",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48&search=story",
        revalidate: 120,
        keyTag: "public:cms-pages",
        pathTag:
          "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48&search=story",
        tags: [
          "public:cms-pages",
          "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48&search=story",
        ],
      },
      fetchCacheOptions: {
        cache: "force-cache",
        next: {
          revalidate: 120,
          tags: [
            "public:cms-pages",
            "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48&search=story",
          ],
        },
      },
    },
  );
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "cms-pages",
      "/api/v1/public/cms/pages?pageSize=48&search=story&page=1&culture=de-DE",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48&search=story",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48&search=story",
        revalidate: 120,
        keyTag: "public:cms-pages",
        pathTag:
          "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48&search=story",
        tags: [
          "public:cms-pages",
          "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48&search=story",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
});

test("getPublicApiRequestPlan keeps mutations uncached while preserving canonical cache identity", () => {
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?visibleState=offers&pageSize=12&page=1&culture=de-DE",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleState=offers",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleState=offers",
        revalidate: 90,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleState=offers",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleState=offers",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );

  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "cms-pages",
      "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        revalidate: 120,
        keyTag: "public:cms-pages",
        pathTag:
          "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        tags: [
          "public:cms-pages",
          "path:/api/v1/public/cms/pages?culture=de-DE&page=1&pageSize=48",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&search=coffee",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&search=coffee",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&search=coffee",
        revalidate: 90,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&search=coffee",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&search=coffee",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?categorySlug=coffee&page=1&pageSize=12&culture=de-DE",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?categorySlug=coffee&culture=de-DE&page=1&pageSize=12",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?categorySlug=coffee&culture=de-DE&page=1&pageSize=12",
        revalidate: 90,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?categorySlug=coffee&culture=de-DE&page=1&pageSize=12",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?categorySlug=coffee&culture=de-DE&page=1&pageSize=12",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleSort=offers-first",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleSort=offers-first",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleSort=offers-first",
        revalidate: 90,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleSort=offers-first",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleSort=offers-first",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&mediaState=with-image",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=1&pageSize=12",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=1&pageSize=12",
        revalidate: 90,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=1&pageSize=12",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?culture=de-DE&mediaState=with-image&page=1&pageSize=12",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&visibleState=offers&savingsBand=hero",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&savingsBand=hero&visibleState=offers",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&savingsBand=hero&visibleState=offers",
        revalidate: 90,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&savingsBand=hero&visibleState=offers",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=12&savingsBand=hero&visibleState=offers",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
});



test("getPublicApiRequestPlan keeps paged catalog browse windows uncached for mutations", () => {
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=3&pageSize=12",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?culture=de-DE&page=3&pageSize=12",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&page=3&pageSize=12",
        revalidate: 90,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?culture=de-DE&page=3&pageSize=12",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?culture=de-DE&page=3&pageSize=12",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
});

test("getPublicApiRequestPlan keeps category-heavy catalog browse windows uncached for mutations", () => {
  assert.deepEqual(
    getPublicApiRequestPlan(
      "http://localhost:5134",
      "catalog-products",
      "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      "POST",
    ),
    {
      requestUrl:
        "http://localhost:5134/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
      cacheIdentity: {
        normalizedPath:
          "/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
        revalidate: 90,
        keyTag: "public:catalog-products",
        pathTag:
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
        tags: [
          "public:catalog-products",
          "path:/api/v1/public/catalog/products?culture=de-DE&page=1&pageSize=100",
        ],
      },
      fetchCacheOptions: {
        cache: "no-store",
      },
    },
  );
});

