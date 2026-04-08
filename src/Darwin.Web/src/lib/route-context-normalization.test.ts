import assert from "node:assert/strict";
import test from "node:test";
import {
  normalizeConfirmationResultArgs,
  normalizeConfirmationRouteArgs,
  normalizeCultureArg,
  normalizeEntityRouteArgs,
  normalizePagingArgs,
  normalizePagedRouteArgs,
  normalizePublicAuthRouteArgs,
} from "@/lib/route-context-normalization";

test("normalizeCultureArg trims culture keys for route-context caching", () => {
  assert.deepEqual(normalizeCultureArg(" de-DE "), ["de-DE"]);
});

test("normalizePublicAuthRouteArgs trims both culture and route", () => {
  assert.deepEqual(
    normalizePublicAuthRouteArgs(" en-US ", " /account/sign-in "),
    ["en-US", "/account/sign-in"],
  );
});

test("normalizePagedRouteArgs bounds invalid pages to canonical positive integers", () => {
  assert.deepEqual(normalizePagedRouteArgs(" de-DE ", 0, -10), ["de-DE", 1, 1]);
  assert.deepEqual(normalizePagedRouteArgs(" de-DE ", 2.9, 24.1), ["de-DE", 2, 24]);
});

test("normalizePagingArgs bounds page-only tuples to canonical positive integers", () => {
  assert.deepEqual(normalizePagingArgs(0, -10), [1, 1]);
  assert.deepEqual(normalizePagingArgs(2.9, 24.1), [2, 24]);
});

test("normalizeEntityRouteArgs trims route identifiers", () => {
  assert.deepEqual(normalizeEntityRouteArgs(" de-DE ", " order-1 "), ["de-DE", "order-1"]);
});

test("normalizeConfirmation args trim order references and drop empty numbers", () => {
  assert.deepEqual(
    normalizeConfirmationResultArgs(" order-1 ", "   "),
    ["order-1", undefined],
  );
  assert.deepEqual(
    normalizeConfirmationRouteArgs(" de-DE ", " order-1 ", "  10001  "),
    ["de-DE", "order-1", "10001"],
  );
});
