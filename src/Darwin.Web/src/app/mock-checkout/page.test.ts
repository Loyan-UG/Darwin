import assert from "node:assert/strict";
import test from "node:test";
import {
  buildOutcomeUrl,
  readSearchValue,
  toFinalizeUrl,
  tryParseAbsoluteUrl,
} from "@/app/mock-checkout/page";

test("mock checkout helpers normalize bounded search values", () => {
  assert.equal(readSearchValue("  order-1  ", 16), "order-1");
  assert.equal(readSearchValue(["  payment-1  ", "ignored"], 16), "payment-1");
  assert.equal(readSearchValue(undefined, 16), "");
  assert.equal(readSearchValue("x".repeat(40), 8), "xxxxxxxx");
});

test("mock checkout helpers only accept absolute URLs and append finalize once", () => {
  assert.equal(tryParseAbsoluteUrl("/checkout/orders/1/confirmation"), null);
  assert.equal(
    toFinalizeUrl("http://localhost:3000/checkout/orders/order-1/confirmation")?.toString(),
    "http://localhost:3000/checkout/orders/order-1/confirmation/finalize",
  );
  assert.equal(
    toFinalizeUrl("http://localhost:3000/checkout/orders/order-1/confirmation/")?.toString(),
    "http://localhost:3000/checkout/orders/order-1/confirmation/finalize",
  );
});

test("mock checkout builds explicit success, cancellation, and failure reconciliation URLs", () => {
  const returnUrl = "http://localhost:3000/checkout/orders/order-1/confirmation?orderNumber=ORD-1001";
  const cancelUrl = "http://localhost:3000/checkout/orders/order-1/confirmation?orderNumber=ORD-1001";

  assert.equal(
    buildOutcomeUrl(returnUrl, "session-1", "Succeeded"),
    "http://localhost:3000/checkout/orders/order-1/confirmation/finalize?orderNumber=ORD-1001&providerReference=session-1&outcome=Succeeded",
  );
  assert.equal(
    buildOutcomeUrl(cancelUrl, "session-1", "Cancelled"),
    "http://localhost:3000/checkout/orders/order-1/confirmation/finalize?orderNumber=ORD-1001&providerReference=session-1&outcome=Cancelled&cancelled=true",
  );
  assert.equal(
    buildOutcomeUrl(
      returnUrl,
      "session-1",
      "Failed",
      "Mock checkout marked the payment as failed.",
    ),
    "http://localhost:3000/checkout/orders/order-1/confirmation/finalize?orderNumber=ORD-1001&providerReference=session-1&outcome=Failed&failureReason=Mock+checkout+marked+the+payment+as+failed.",
  );
  assert.equal(buildOutcomeUrl("/relative", "session-1", "Succeeded"), null);
});
