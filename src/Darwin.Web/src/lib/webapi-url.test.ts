import test from "node:test";
import assert from "node:assert/strict";
import { toSafeHttpUrl, toWebApiUrl } from "@/lib/webapi-url";

test("toSafeHttpUrl keeps valid http and https URLs only", () => {
  assert.equal(
    toSafeHttpUrl("https://example.com/path?x=1"),
    "https://example.com/path?x=1",
  );
  assert.equal(toSafeHttpUrl("http://example.com/path"), "http://example.com/path");
  assert.equal(toSafeHttpUrl("ftp://example.com/file"), "");
  assert.equal(toSafeHttpUrl("javascript:alert(1)"), "");
});

test("toWebApiUrl resolves safe relative paths against the configured API host", () => {
  const previousBaseUrl = process.env.DARWIN_WEBAPI_BASE_URL;
  process.env.DARWIN_WEBAPI_BASE_URL = "https://api.darwin.local";

  try {
    assert.equal(
      toWebApiUrl("/media/product.png"),
      "https://api.darwin.local/media/product.png",
    );
    assert.equal(toWebApiUrl("media/product.png"), "");
    assert.equal(toWebApiUrl("//evil.example.com/x"), "");
    assert.equal(toWebApiUrl("javascript:alert(1)"), "");
    assert.equal(
      toWebApiUrl("https://cdn.example.com/file.pdf"),
      "https://cdn.example.com/file.pdf",
    );
  } finally {
    if (previousBaseUrl === undefined) {
      delete process.env.DARWIN_WEBAPI_BASE_URL;
    } else {
      process.env.DARWIN_WEBAPI_BASE_URL = previousBaseUrl;
    }
  }
});
