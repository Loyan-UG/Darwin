import assert from "node:assert/strict";
import test from "node:test";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { AccountHubPage } from "@/components/account/account-hub-page";
import { ActivationPage } from "@/components/account/activation-page";
import { AddressesPage } from "@/components/account/addresses-page";
import { PasswordPage } from "@/components/account/password-page";
import { PreferencesPage } from "@/components/account/preferences-page";
import { ProfilePage } from "@/components/account/profile-page";
import { RegisterPage } from "@/components/account/register-page";
import { SecurityPage } from "@/components/account/security-page";
import { SignInPage } from "@/components/account/sign-in-page";
import { CartPage } from "@/components/cart/cart-page";
import { CatalogPage } from "@/components/catalog/catalog-page";
import { ProductDetailPage } from "@/components/catalog/product-detail-page";
import { CheckoutPage } from "@/components/checkout/checkout-page";
import { MockCheckoutPage } from "@/components/checkout/mock-checkout-page";
import { OrderConfirmationPage } from "@/components/checkout/order-confirmation-page";
import { HelpPageDetail } from "@/components/cms/help-page-detail";
import { HelpPagesIndex } from "@/components/cms/help-pages-index";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import type {
  PublicCategorySummary,
  PublicProductDetail,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageDetail, PublicPageSummary } from "@/features/cms/types";

const blockedBackOfficePhrases = [
  /Promotion lanes/i,
  /Promotion lane/i,
  /Next-buy offer board/i,
  /route summary/i,
  /readiness/i,
  /review window/i,
  /review target/i,
  /composition window/i,
  /API status/i,
  /loaded count/i,
  /server total/i,
  /metadata focus/i,
  /image coverage/i,
  /offer coverage/i,
  /visible lens/i,
];

const category: PublicCategorySummary = {
  id: "category-1",
  slug: "fruit",
  name: "Fruit",
  description: "Fresh picks",
  productCount: 8,
};

const heroProduct: PublicProductSummary = {
  id: "product-1",
  slug: "apples",
  name: "Apples",
  priceMinor: 700,
  compareAtPriceMinor: 1000,
  currency: "EUR",
  imageUrl: null,
  shortDescription: "Crisp apples",
  categoryName: "Fruit",
};

const pageSummary: PublicPageSummary = {
  id: "page-1",
  slug: "about",
  title: "About",
  metaDescription: "About this storefront",
};

const pageDetail: PublicPageDetail = {
  id: "page-1",
  slug: "about",
  title: "About",
  metaTitle: "About",
  metaDescription: "About this storefront",
  contentHtml: "<h2>About</h2><p>Fresh content</p>",
};

const productDetail: PublicProductDetail = {
  id: "product-1",
  slug: "apples",
  name: "Apples",
  sku: "APL-1",
  currency: "EUR",
  priceMinor: 700,
  compareAtPriceMinor: 1000,
  shortDescription: "Crisp apples",
  fullDescriptionHtml: "<p>Fresh apples</p>",
  metaTitle: "Apples",
  metaDescription: "Fresh apples",
  primaryImageUrl: null,
  media: [],
  variants: [
    {
      id: "variant-1",
      sku: "APL-1",
      basePriceNetMinor: 700,
      currency: "EUR",
      backorderAllowed: false,
      isDigital: false,
    },
  ],
};

const address = {
  id: "address-1",
  rowVersion: "rv-1",
  fullName: "Ada Lovelace",
  company: null,
  street1: "Main Street 1",
  street2: null,
  postalCode: "10115",
  city: "Berlin",
  state: null,
  countryCode: "DE",
  phoneE164: "+49123456789",
  isDefaultBilling: true,
  isDefaultShipping: true,
};

const profile = {
  id: "profile-1",
  email: "ada@example.com",
  firstName: "Ada",
  lastName: "Lovelace",
  phoneE164: "+49123456789",
  phoneNumberConfirmed: true,
  locale: "en-US",
  timezone: "Europe/Berlin",
  currency: "EUR",
  rowVersion: "rv-1",
};

const cart = {
  cartId: "cart-1",
  currency: "EUR",
  subtotalNetMinor: 1200,
  vatTotalMinor: 200,
  grandTotalGrossMinor: 1400,
  couponCode: null,
  items: [
    {
      lineId: "line-1",
      quantity: 2,
      productId: "product-1",
      variantId: "variant-1",
      productName: "Apples",
      sku: "APL-1",
      unitPriceNetMinor: 600,
      unitPriceGrossMinor: 700,
      addOnPriceDeltaMinor: 0,
      vatRate: 0.19,
      lineNetMinor: 1200,
      lineVatMinor: 200,
      lineTotalGrossMinor: 1400,
      lineGrossMinor: 1400,
      selectedAddOnValueIdsJson: "[]",
      display: {
        href: "/catalog/apples",
        imageUrl: null,
        name: "Apples",
        sku: "APL-1",
      },
    },
  ],
};

const cartModel = {
  anonymousId: "anon-1",
  status: "ok",
  cart,
};

const storefrontCart = {
  id: "cart-1",
  currency: "EUR",
  subtotalNetMinor: 1200,
  subtotalGrossMinor: 1400,
  grandTotalGrossMinor: 1400,
  items: [
    {
      lineId: "line-1",
      quantity: 2,
      productId: "product-1",
      variantId: "variant-1",
      productName: "Apples",
      sku: "APL-1",
      unitPriceGrossMinor: 700,
      lineTotalGrossMinor: 1400,
      imageUrl: null,
      display: {
        href: "/catalog/apples",
      },
    },
  ],
};

const checkoutDraft = {
  fullName: "Ada Lovelace",
  company: "",
  street1: "Main Street 1",
  street2: "",
  postalCode: "10115",
  city: "Berlin",
  state: "",
  countryCode: "DE",
  phoneE164: "+49123456789",
  selectedShippingMethodId: "ship-1",
};

const checkoutIntent = {
  cartId: "cart-1",
  currency: "EUR",
  subtotalNetMinor: 1200,
  vatTotalMinor: 200,
  grandTotalGrossMinor: 1400,
  shipmentMass: 1,
  requiresShipping: true,
  shippingCountryCode: "DE",
  selectedShippingMethodId: "ship-1",
  selectedShippingTotalMinor: 100,
  shippingOptions: [
    {
      methodId: "ship-1",
      name: "Standard",
      priceMinor: 100,
      currency: "EUR",
      carrier: "DHL",
      service: "Home",
    },
  ],
};

function render(component: React.ElementType, props: Record<string, unknown>) {
  return renderToStaticMarkup(React.createElement(component, props));
}

function assertNoBackOfficeCustomerUi(html: string) {
  for (const pattern of blockedBackOfficePhrases) {
    assert.doesNotMatch(html, pattern);
  }
}

test("CatalogPage renders a clean product listing", () => {
  const html = render(CatalogPage, {
    culture: "en-US",
    categories: [category],
    products: [heroProduct],
    activeCategorySlug: "fruit",
    totalProducts: 1,
    currentPage: 1,
    pageSize: 12,
    searchQuery: "apple",
    visibleState: "offers",
    visibleSort: "featured",
    mediaState: "all",
    savingsBand: "all",
    dataStatus: { categories: "ok", products: "ok" },
  });

  assert.match(html, /Shop groceries/);
  assert.match(html, /Search/);
  assert.match(html, /Fruit/);
  assert.match(html, /Apples/);
  assertNoBackOfficeCustomerUi(html);
});

test("ProductDetailPage renders product purchase content only", () => {
  const html = render(ProductDetailPage, {
    culture: "en-US",
    product: productDetail,
    primaryCategory: category,
    relatedProducts: [heroProduct],
    status: "ok",
  });

  assert.match(html, /Apples/);
  assert.match(html, /Add to cart/);
  assert.match(html, /Product details/);
  assert.match(html, /Related products/);
  assertNoBackOfficeCustomerUi(html);
});

test("CartPage renders cart review, summary, and checkout action", () => {
  const html = render(CartPage, {
    culture: "en-US",
    model: cartModel,
    memberAddresses: [address],
    memberAddressesStatus: "ok",
    memberProfile: profile,
    memberProfileStatus: "ok",
    memberPreferences: null,
    memberPreferencesStatus: "ok",
    hasMemberSession: true,
    followUpProducts: [heroProduct],
  });

  assert.match(html, /Review your basket before checkout/);
  assert.match(html, /Apples/);
  assert.match(html, /Checkout summary/);
  assert.match(html, /Start checkout/);
  assertNoBackOfficeCustomerUi(html);
});

test("CheckoutPage renders checkout form, shipping, payment, and order summary", () => {
  const html = render(CheckoutPage, {
    culture: "en-US",
    model: cartModel,
    draft: checkoutDraft,
    intent: checkoutIntent,
    intentStatus: "ok",
    memberAddresses: [address],
    profilePrefillActive: true,
    selectedMemberAddressId: "address-1",
    hasMemberSession: true,
  });

  assert.match(html, /Checkout/);
  assert.match(html, /Saved addresses/);
  assert.match(html, /Shipping options/);
  assert.match(html, /Place order/);
  assert.match(html, /Checkout summary/);
  assertNoBackOfficeCustomerUi(html);
});

test("OrderConfirmationPage renders order result and next actions without merchandising boards", () => {
  const html = render(OrderConfirmationPage, {
    culture: "en-US",
    confirmation: {
      orderId: "order-1",
      orderNumber: "ORD-1001",
      currency: "EUR",
      subtotalNetMinor: 1200,
      taxTotalMinor: 200,
      shippingTotalMinor: 100,
      shippingMethodId: "ship-1",
      shippingMethodName: "Standard",
      shippingCarrier: "DHL",
      shippingService: "Home",
      discountTotalMinor: 0,
      grandTotalGrossMinor: 1500,
      status: "Placed",
      billingAddressJson: JSON.stringify(address),
      shippingAddressJson: JSON.stringify(address),
      createdAtUtc: "2026-04-10T10:00:00Z",
      lines: [
        {
          id: "line-1",
          variantId: "variant-1",
          name: "Purchased product",
          sku: "SKU-1",
          quantity: 1,
          unitPriceGrossMinor: 1500,
          lineGrossMinor: 1500,
        },
      ],
      payments: [
        {
          id: "payment-1",
          createdAtUtc: "2026-04-10T10:01:00Z",
          provider: "Stripe",
          providerReference: "pi_1",
          amountMinor: 1500,
          currency: "EUR",
          status: "Paid",
          paidAtUtc: "2026-04-10T10:02:00Z",
        },
      ],
    },
    status: "ok",
    checkoutStatus: "order-placed",
    paymentCompletionStatus: "completed",
    paymentOutcome: "Paid",
    cancelled: false,
    hasMemberSession: false,
  });

  assert.match(html, /Order confirmation/);
  assert.match(html, /ORD-1001/);
  assert.match(html, /What happens next/);
  assert.match(html, /Continue shopping/);
  assertNoBackOfficeCustomerUi(html);
});

test("Help listing and content detail avoid visible CMS review language", () => {
  const indexHtml = render(HelpPagesIndex, {
    culture: "en-US",
    pages: [pageSummary],
    totalPages: 1,
    currentPage: 1,
    status: "ok",
  });
  const detailHtml = render(HelpPageDetail, {
    culture: "en-US",
    page: pageDetail,
    status: "ok",
    relatedPages: [pageSummary],
  });

  assert.match(indexHtml, /Help and information/);
  assert.match(indexHtml, /About/);
  assert.match(detailHtml, /Fresh content/);
  assertNoBackOfficeCustomerUi(indexHtml);
  assertNoBackOfficeCustomerUi(detailHtml);
  assert.doesNotMatch(indexHtml, />CMS</i);
  assert.doesNotMatch(detailHtml, />CMS</i);
});

test("Account entry and auth recovery pages stay focused on account actions", () => {
  const accountHtml = render(AccountHubPage, {
    culture: "en-US",
    storefrontCart,
    returnPath: "/checkout",
  });
  const signInHtml = render(SignInPage, {
    culture: "en-US",
    email: "ada@example.com",
    returnPath: "/checkout",
    storefrontCart,
  });
  const registerHtml = render(RegisterPage, {
    culture: "en-US",
    email: "ada@example.com",
    returnPath: "/checkout",
    storefrontCart,
  });
  const activationHtml = render(ActivationPage, {
    culture: "en-US",
    email: "ada@example.com",
    returnPath: "/checkout",
    storefrontCart,
  });
  const passwordHtml = render(PasswordPage, {
    culture: "en-US",
    email: "ada@example.com",
    returnPath: "/checkout",
    storefrontCart,
  });

  assert.match(accountHtml, /Sign in/);
  assert.match(accountHtml, /Register/);
  assert.match(signInHtml, /Sign in/);
  assert.match(registerHtml, /Create account/);
  assert.match(activationHtml, /Request or complete email confirmation/);
  assert.match(passwordHtml, /Request or complete a password reset/);
  assertNoBackOfficeCustomerUi(accountHtml);
  assertNoBackOfficeCustomerUi(signInHtml);
  assertNoBackOfficeCustomerUi(registerHtml);
  assertNoBackOfficeCustomerUi(activationHtml);
  assertNoBackOfficeCustomerUi(passwordHtml);
});

test("Account self-service pages render member forms without storefront rails", () => {
  const baseProps = {
    culture: "en-US",
  };
  const profileHtml = render(ProfilePage, {
    ...baseProps,
    profile,
    supportedCultures: ["en-US", "de-DE"],
    status: "ok",
    profileStatus: "ok",
  });
  const preferencesHtml = render(PreferencesPage, {
    ...baseProps,
    preferences: {
      emailMarketingOptIn: true,
      smsMarketingOptIn: false,
      whatsappMarketingOptIn: false,
      pushMarketingOptIn: false,
      privacyProfilingOptIn: false,
      rowVersion: "rv-1",
    },
    status: "ok",
    preferencesStatus: "ok",
  });
  const addressesHtml = render(AddressesPage, {
    ...baseProps,
    addresses: [address],
    status: "ok",
    addressesStatus: "ok",
  });
  const securityHtml = render(SecurityPage, {
    ...baseProps,
    session: {
      isAuthenticated: true,
      accessTokenExpiresAtUtc: "2026-04-11T10:00:00Z",
    },
    profile,
    profileStatus: "ok",
    securityStatus: "ok",
  });

  assert.match(profileHtml, /Profile/);
  assert.match(preferencesHtml, /Communication channels/);
  assert.match(addressesHtml, /Addresses/);
  assert.match(securityHtml, /Security/);
  assertNoBackOfficeCustomerUi(profileHtml);
  assertNoBackOfficeCustomerUi(preferencesHtml);
  assertNoBackOfficeCustomerUi(addressesHtml);
  assertNoBackOfficeCustomerUi(securityHtml);
});

test("MemberAuthRequired shows focused sign-in actions", () => {
  const html = render(MemberAuthRequired, {
    culture: "en-US",
    title: "Sign in required",
    message: "Protected member route requires sign in.",
    returnPath: "/orders",
    storefrontCart,
  });

  assert.match(html, /Sign in required/);
  assert.match(html, /Continue after sign in/);
  assert.match(html, /Review cart/);
  assertNoBackOfficeCustomerUi(html);
});

test("MockCheckoutPage keeps explicit hosted-payment handoff actions", () => {
  const html = render(MockCheckoutPage, {
    culture: "en-US",
    orderId: "order-1",
    paymentId: "payment-1",
    provider: "DarwinCheckout",
    sessionToken: "session-1",
    returnUrl: "http://localhost:3000/checkout/orders/order-1/confirmation",
    cancelUrl: "http://localhost:3000/checkout/orders/order-1/confirmation",
    cancelActionUrl:
      "http://localhost:3000/checkout/orders/order-1/confirmation/finalize?providerReference=session-1&outcome=Cancelled&cancelled=true",
    successUrl:
      "http://localhost:3000/checkout/orders/order-1/confirmation/finalize?providerReference=session-1&outcome=Succeeded",
    failureUrl:
      "http://localhost:3000/checkout/orders/order-1/confirmation/finalize?providerReference=session-1&outcome=Failed&failureReason=Mock%20checkout%20marked%20the%20payment%20as%20failed.",
    title: "Local hosted checkout",
    description:
      "This development route simulates the PSP handoff for storefront checkout and routes back into confirmation reconciliation.",
  });

  assert.match(html, /Mock hosted checkout/);
  assert.match(html, /Mark payment as succeeded/);
  assert.match(html, /Mark payment as cancelled/);
  assert.match(html, /Mark payment as failed/);
});
