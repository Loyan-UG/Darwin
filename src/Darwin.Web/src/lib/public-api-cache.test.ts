import test from "node:test";
import assert from "node:assert/strict";
import { getPublicApiCachePolicy } from "@/lib/public-api-cache";

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
