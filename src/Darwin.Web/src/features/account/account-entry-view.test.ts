import test from "node:test";
import assert from "node:assert/strict";
import { buildAccountEntryView } from "@/features/account/account-entry-view";

test("buildAccountEntryView sanitizes the public return path and keeps storefront continuation", () => {
  const view = buildAccountEntryView({
    culture: "de-DE",
    returnPath: "https://example.com/outside",
    session: null,
    publicRouteContext: {
      storefrontContext: {
        cmsPagesResult: { status: "ok", data: { items: [{ slug: "faq", title: "FAQ" }] } },
        cmsPagesStatus: "ok",
        cmsPages: [{ slug: "faq", title: "FAQ" }],
        categoriesResult: {
          status: "ok",
          data: { items: [{ slug: "bakery", name: "Bakery" }] },
        },
        categoriesStatus: "ok",
        categories: [{ slug: "bakery", name: "Bakery" }],
        productsResult: {
          status: "ok",
          data: {
            items: [
              {
                slug: "baguette",
                name: "Baguette",
                summary: null,
                sku: null,
                price: null,
                imageUrl: null,
              },
            ],
          },
        },
        productsStatus: "ok",
        products: [
          {
            slug: "baguette",
            name: "Baguette",
            summary: null,
            sku: null,
            price: null,
            imageUrl: null,
          },
        ],
        storefrontCart: null,
        storefrontCartStatus: "ok",
        cartSnapshots: [],
        cartLinkedProductSlugs: [],
        cartResult: { status: "ok", data: null },
      },
    },
    memberRouteContext: null,
  });

  assert.equal(view.kind, "public");
  assert.equal(view.props.returnPath, "/account");
  assert.deepEqual(Object.keys(view.props).sort(), [
    "culture",
    "returnPath",
    "storefrontCart",
  ]);
});

test("buildAccountEntryView assembles the member dashboard props from the shared route context", () => {
  const view = buildAccountEntryView({
    culture: "en-US",
    session: {
      customerId: "customer-1",
      emailAddress: "member@example.com",
      isAuthenticated: true,
    },
    publicRouteContext: null,
    memberRouteContext: {
      identityContext: {
        profileResult: {
          status: "ok",
          data: {
            customerId: "customer-1",
            emailAddress: "member@example.com",
            givenName: "Ada",
            familyName: "Lovelace",
            phoneNumber: null,
            locale: "en-US",
            currencyCode: "EUR",
            timeZoneId: "Europe/Berlin",
            isPhoneNumberConfirmed: false,
          },
        },
        preferencesResult: {
          status: "ok",
          data: {
            preferredLanguageCode: "en",
            preferredChannelCode: "Email",
            prefersMarketingEmails: true,
            prefersMarketingSms: false,
            prefersMarketingWhatsApp: false,
          },
        },
        customerContextResult: {
          status: "ok",
          data: {
            firstName: "Ada",
            lastName: "Lovelace",
            fullName: "Ada Lovelace",
            initials: "AL",
            hasPhoneNumber: false,
            isPhoneNumberConfirmed: false,
            preferredLanguageCode: "en",
            preferredChannelCode: "Email",
          },
        },
        addressesResult: {
          status: "ok",
          data: [
            {
              id: "address-1",
              title: "Home",
              recipientName: "Ada Lovelace",
              streetAddress: "1 Main St",
              postalCode: "10115",
              city: "Berlin",
              countryCode: "DE",
              isDefaultShipping: true,
              isDefaultBilling: false,
            },
          ],
        },
      },
      commerceSummaryContext: {
        ordersResult: {
          status: "ok",
          data: { items: [{ id: "order-1", orderNumber: "1001" }] },
        },
        invoicesResult: {
          status: "ok",
          data: { items: [{ id: "invoice-1", invoiceNumber: "INV-1" }] },
        },
        loyaltyOverviewResult: {
          status: "ok",
          data: {
            totalBusinessCount: 0,
            activeBusinessCount: 0,
            totalPointsBalance: 0,
            accounts: [],
          },
        },
      },
      loyaltyBusinessesResult: {
        status: "ok",
        data: { items: [{ businessId: "business-1", displayName: "Cafe" }] },
      },
      storefrontContext: {
        cmsPagesResult: { status: "ok", data: { items: [{ slug: "faq", title: "FAQ" }] } },
        cmsPagesStatus: "ok",
        cmsPages: [{ slug: "faq", title: "FAQ" }],
        categoriesResult: {
          status: "ok",
          data: { items: [{ slug: "bakery", name: "Bakery" }] },
        },
        categoriesStatus: "ok",
        categories: [{ slug: "bakery", name: "Bakery" }],
        productsResult: {
          status: "ok",
          data: {
            items: [
              {
                slug: "baguette",
                name: "Baguette",
                summary: null,
                sku: null,
                price: null,
                imageUrl: null,
              },
            ],
          },
        },
        productsStatus: "ok",
        products: [
          {
            slug: "baguette",
            name: "Baguette",
            summary: null,
            sku: null,
            price: null,
            imageUrl: null,
          },
        ],
        storefrontCart: {
          cartId: "cart-1",
          currency: "EUR",
          items: [{ variantId: "variant-1", quantity: 2, unitPriceNetMinor: 500, addOnPriceDeltaMinor: 0, vatRate: 19, lineNetMinor: 1000, lineVatMinor: 99, lineGrossMinor: 1099, selectedAddOnValueIdsJson: "[]" }],
          subtotalNetMinor: 1000,
          vatTotalMinor: 99,
          grandTotalGrossMinor: 1099,
        },
        storefrontCartStatus: "ok",
        cartSnapshots: [],
        cartLinkedProductSlugs: [],
        cartResult: {
          status: "ok",
          data: {
            cartId: "cart-1",
            currency: "EUR",
            items: [{ variantId: "variant-1", quantity: 2, unitPriceNetMinor: 500, addOnPriceDeltaMinor: 0, vatRate: 19, lineNetMinor: 1000, lineVatMinor: 99, lineGrossMinor: 1099, selectedAddOnValueIdsJson: "[]" }],
            subtotalNetMinor: 1000,
            vatTotalMinor: 99,
            grandTotalGrossMinor: 1099,
          },
        },
      },
    },
  });

  assert.equal(view.kind, "member");
  assert.equal(view.props.recentOrders.length, 1);
  assert.equal(view.props.recentInvoices.length, 1);
  assert.equal(view.props.loyaltyBusinesses.length, 1);
  assert.equal(view.props.storefrontCart?.grandTotalGrossMinor, 1099);
  assert.equal("cmsPages" in view.props, false);
  assert.equal("products" in view.props, false);
});
