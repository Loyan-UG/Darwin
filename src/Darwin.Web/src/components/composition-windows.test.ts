import assert from "node:assert/strict";
import test from "node:test";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { AccountContentCompositionWindow } from "@/components/account/account-content-composition-window";
import { AccountHubCompositionWindow } from "@/components/account/account-hub-composition-window";
import { AccountHubPage } from "@/components/account/account-hub-page";
import { MemberDashboardPage } from "@/components/account/member-dashboard-page";
import { ProfilePage } from "@/components/account/profile-page";
import { PreferencesPage } from "@/components/account/preferences-page";
import { AddressesPage } from "@/components/account/addresses-page";
import { SecurityPage } from "@/components/account/security-page";
import { AccountStorefrontWindow } from "@/components/account/account-storefront-window";
import { PublicAuthContinuation } from "@/components/account/public-auth-continuation";
import { PublicAuthCompositionWindow } from "@/components/account/public-auth-composition-window";
import { CartContentCompositionWindow } from "@/components/cart/cart-content-composition-window";
import { CartPage } from "@/components/cart/cart-page";
import { CatalogCampaignWindow } from "@/components/catalog/catalog-campaign-window";
import { CatalogContinuationRail } from "@/components/catalog/catalog-continuation-rail";
import { CatalogPage } from "@/components/catalog/catalog-page";
import { CatalogContentCompositionWindow } from "@/components/catalog/catalog-content-composition-window";
import { ProductContentCompositionWindow } from "@/components/catalog/product-content-composition-window";
import { ProductDetailPage } from "@/components/catalog/product-detail-page";
import { CommerceAuthHandoff } from "@/components/checkout/commerce-auth-handoff";
import { CheckoutContentCompositionWindow } from "@/components/checkout/checkout-content-composition-window";
import { CheckoutPage } from "@/components/checkout/checkout-page";
import { MockCheckoutPage } from "@/components/checkout/mock-checkout-page";
import { ConfirmationContentCompositionWindow } from "@/components/checkout/confirmation-content-composition-window";
import { CommerceStorefrontWindow } from "@/components/checkout/commerce-storefront-window";
import { OrderConfirmationPage } from "@/components/checkout/order-confirmation-page";
import { CmsContentCompositionWindow } from "@/components/cms/cms-content-composition-window";
import { CmsIndexCompositionWindow } from "@/components/cms/cms-index-composition-window";
import { CmsPagesIndex } from "@/components/cms/cms-pages-index";
import { CmsPageDetail } from "@/components/cms/cms-page-detail";
import { CmsCommerceCampaignWindow } from "@/components/cms/cms-commerce-campaign-window";
import { CmsContinuationRail } from "@/components/cms/cms-continuation-rail";
import { CmsStorefrontSupportWindow } from "@/components/cms/cms-storefront-support-window";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { InvoiceDetailPage } from "@/components/member/invoice-detail-page";
import { OrdersPage } from "@/components/member/orders-page";
import { InvoicesPage } from "@/components/member/invoices-page";
import { OrderDetailPage } from "@/components/member/order-detail-page";
import { MemberStorefrontWindow } from "@/components/member/member-storefront-window";
import { buildPromotionLaneRouteMapItem } from "@/components/composition-window-promotion-lane";
import { buildMemberPromotionLaneCards } from "@/components/member/member-promotion-lanes";
import type {
  PublicCategorySummary,
  PublicProductDetail,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageDetail, PublicPageSummary } from "@/features/cms/types";

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
test("OrderConfirmationPage renders guest promotion lanes after purchase", () => {
  const html = renderToStaticMarkup(
    React.createElement(OrderConfirmationPage, {
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
        billingAddressJson: JSON.stringify({
          fullName: "Guest Shopper",
          company: null,
          street1: "Main Street 1",
          street2: null,
          postalCode: "10115",
          city: "Berlin",
          state: null,
          countryCode: "DE",
          phoneE164: "+49123456789",
        }),
        shippingAddressJson: JSON.stringify({
          fullName: "Guest Shopper",
          company: null,
          street1: "Main Street 1",
          street2: null,
          postalCode: "10115",
          city: "Berlin",
          state: null,
          countryCode: "DE",
          phoneE164: "+49123456789",
        }),
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
      memberOrders: [],
      memberOrdersStatus: "unauthenticated",
      memberInvoices: [],
      memberInvoicesStatus: "unauthenticated",
      memberLoyaltyOverview: null,
      memberLoyaltyStatus: "unauthenticated",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct, { ...heroProduct, id: "product-2", slug: "pears", name: "Pears" }],
      productsStatus: "ok",
    }),
  );

  assert.match(html, /Next-buy offer board/);
  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
});


test("AccountContentCompositionWindow renders a promotion-lane route map for the strongest product", () => {
  const html = renderToStaticMarkup(
    React.createElement(AccountContentCompositionWindow, {
      culture: "en-US",
      routeCard: {
        label: "Current route",
        title: "Dashboard",
        description: "Current member route",
        href: "/account",
        ctaLabel: "Open dashboard",
      },
      nextCard: {
        label: "Next step",
        title: "Orders",
        description: "Open order follow-up",
        href: "/orders",
        ctaLabel: "Open orders",
      },
      routeMapItems: [],
      cmsPages: [pageSummary],
      categories: [category],
      products: [heroProduct],
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(
    html,
    /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/,
  );
  assert.match(html, /Hero offers/);
});

test("ProductContentCompositionWindow renders a promotion-lane route map entry", () => {
  const html = renderToStaticMarkup(
    React.createElement(ProductContentCompositionWindow, {
      culture: "en-US",
      product: productDetail,
      primaryCategory: category,
      reviewProducts: [heroProduct],
      relatedProducts: [heroProduct],
      cartSummary: null,
      reviewCatalogPath: "/catalog?visibleState=offers",
      reviewPrimaryLabel: "Review visible offers",
      nextReviewProduct: heroProduct,
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(
    html,
    /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/,
  );
  assert.match(html, /Open promotion lane/);
});

test("CmsContentCompositionWindow renders a promotion-lane route map entry from the strongest visible product", () => {
  const html = renderToStaticMarkup(
    React.createElement(CmsContentCompositionWindow, {
      culture: "en-US",
      page: pageDetail,
      pagePath: "/cms/about",
      headings: [{ id: "about", text: "About" }],
      readingMinutes: 2,
      relatedPages: [pageSummary],
      categories: [category],
      products: [heroProduct],
      cartSummary: null,
      reviewPrimaryHref: "/cms?visibleState=needs-attention",
      reviewPrimaryLabel: "Review pages",
      reviewNextPage: pageSummary,
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(
    html,
    /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/,
  );
  assert.match(html, /Open promotion lane/);
});


test("CartContentCompositionWindow renders a promotion-lane route map entry", () => {
  const html = renderToStaticMarkup(
    React.createElement(CartContentCompositionWindow, {
      culture: "en-US",
      hasMemberSession: false,
      itemCount: 2,
      grandTotalMinor: 1400,
      currency: "EUR",
      checkoutHref: "/checkout",
      cmsPages: [pageSummary],
      categories: [category],
      products: [heroProduct],
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(
    html,
    /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/,
  );
  assert.match(html, /Open promotion lane/);
});

test("CheckoutContentCompositionWindow renders a promotion-lane route map entry", () => {
  const html = renderToStaticMarkup(
    React.createElement(CheckoutContentCompositionWindow, {
      culture: "en-US",
      hasMemberSession: false,
      canPlaceOrder: true,
      addressComplete: true,
      hasSelectedShipping: true,
      cartHref: "/cart",
      accountHref: "/account",
      cmsPages: [pageSummary],
      categories: [category],
      products: [heroProduct],
      memberInvoices: [],
      projectedCheckoutTotalMinor: 1400,
      currency: "EUR",
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(
    html,
    /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/,
  );
  assert.match(html, /Open promotion lane/);
});

test("ConfirmationContentCompositionWindow renders a promotion-lane route map entry", () => {
  const html = renderToStaticMarkup(
    React.createElement(ConfirmationContentCompositionWindow, {
      culture: "en-US",
      hasMemberSession: false,
      paymentNeedsAttention: false,
      orderNumber: "ORD-1",
      orderGrossMinor: 1400,
      currency: "EUR",
      memberOrdersHref: "/account/orders",
      signInHref: "/account/sign-in",
      accountHref: "/account",
      memberOrders: [],
      memberInvoices: [],
      memberLoyaltyOverview: null,
      cmsPages: [pageSummary],
      categories: [category],
      products: [heroProduct],
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(
    html,
    /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/,
  );
  assert.match(html, /Open promotion lane/);
});



test("CatalogCampaignWindow renders dedicated promotion lanes and campaign cards", () => {
  const html = renderToStaticMarkup(
    React.createElement(CatalogCampaignWindow, {
      culture: "en-US",
      categories: [category],
      products: [heroProduct],
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Open promotion lane/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /Fruit/);
});

test("CatalogStorefrontSupportWindow renders promotion lanes, CMS, product, and cart continuity follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(CatalogStorefrontSupportWindow, {
      culture: "en-US",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      cartSummary: {
        status: "ok",
        itemCount: 2,
        currency: "EUR",
        grandTotalGrossMinor: 1400,
      },
    }),
  );

  assert.match(html, /Published content alongside catalog browsing/);
  assert.match(html, /Live product follow-up from this browse window/);
  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /Storefront cart continuity/);
  assert.match(html, /\/cms/);
  assert.match(html, /\/checkout/);
});

test("CmsCommerceCampaignWindow renders dedicated promotion lanes and campaign cards", () => {
  const html = renderToStaticMarkup(
    React.createElement(CmsCommerceCampaignWindow, {
      culture: "en-US",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Open promotion lane/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /Fruit/);
});

test("CmsStorefrontSupportWindow renders promotion lanes, browse, product, and cart continuity", () => {
  const html = renderToStaticMarkup(
    React.createElement(CmsStorefrontSupportWindow, {
      culture: "en-US",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      cartSummary: {
        status: "ok",
        itemCount: 1,
        currency: "EUR",
        grandTotalGrossMinor: 700,
      },
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
});
test("ProductStorefrontSupportWindow renders CMS, browse, product, and cart continuity follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(ProductStorefrontSupportWindow, {
      culture: "en-US",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      cartSummary: {
        status: "ok",
        itemCount: 1,
        currency: "EUR",
        grandTotalGrossMinor: 700,
      },
    }),
  );

  assert.match(html, /Published content alongside this product/);
  assert.match(html, /Browse lanes next to this product/);
  assert.match(html, /Live product follow-up/);
  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /Purchase continuity/);
  assert.match(html, /\/cms/);
  assert.match(html, /\/cart/);
});
test("MemberStorefrontWindow renders optional cart and checkout continuity", () => {
  const html = renderToStaticMarkup(
    React.createElement(MemberStorefrontWindow, {
      culture: "en-US",
      title: "Member storefront",
      message: "Storefront continuation",
      cmsTitle: "CMS follow-up",
      cmsCtaLabel: "Open CMS",
      cmsCards: [
        {
          id: "cms-card",
          label: "CMS",
          title: "About",
          description: "Published content",
          href: "/cms/about",
          ctaLabel: "Open CMS page",
        },
      ],
      cmsEmptyMessage: "No CMS follow-up",
      catalogTitle: "Catalog follow-up",
      catalogCtaLabel: "Open catalog",
      categoryCards: [
        {
          id: "category-card",
          label: "Catalog",
          title: "Fruit",
          description: "Browse lane",
          href: "/catalog?category=fruit",
          ctaLabel: "Open category",
        },
      ],
      catalogEmptyMessage: "No category follow-up",
      productTitle: "Product follow-up",
      productCtaLabel: "Open products",
      productMessage: "Visible product opportunities",
      productCards: [
        {
          id: "product-card",
          label: "Product",
          title: "Apples",
          description: "Visible offer",
          href: "/catalog/apples",
          ctaLabel: "Open product",
          price: "EUR 7.00",
          meta: null,
        },
      ],
      productEmptyMessage: "No product follow-up",
      promotionLaneSectionTitle: "Promotion lanes",
      promotionLaneSectionMessage: "Direct merchandising lanes",
      promotionLaneCards: [
        {
          id: "promotion-lane-card",
          label: "Promotion lane",
          title: "Hero offers currently lead with Apples.",
          description: "Hero offers stay visible from the member storefront.",
          href: "/catalog?visibleState=offers&visibleSort=offers-first&savingsBand=hero",
          ctaLabel: "Open promotion lane",
          meta: "1 product in lane.",
        },
      ],
      cartSectionTitle: "Cart and checkout continuity",
      cartSectionMessage: "Resume cart review or checkout without leaving the member storefront context.",
      cartSectionCartCtaLabel: "Open cart",
      cartSectionCheckoutCtaLabel: "Open checkout",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers currently lead with Apples\./);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/checkout/);
});

test("AccountStorefrontWindow renders cart and checkout continuity", () => {
  const html = renderToStaticMarkup(
    React.createElement(AccountStorefrontWindow, {
      culture: "en-US",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/checkout/);
});

test("CommerceStorefrontWindow renders cart and checkout continuity", () => {
  const html = renderToStaticMarkup(
    React.createElement(CommerceStorefrontWindow, {
      culture: "en-US",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/checkout/);
});
test("CatalogContentCompositionWindow renders a promotion-lane route map entry", () => {
  const html = renderToStaticMarkup(
    React.createElement(CatalogContentCompositionWindow, {
      culture: "en-US",
      activeCategory: category,
      cmsPages: [pageSummary],
      products: [heroProduct],
      cartSummary: null,
      totalProducts: 12,
      currentPage: 1,
      reviewHref: "/catalog?visibleState=offers",
      reviewLabel: "Review offers",
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
});

test("CmsIndexCompositionWindow renders a promotion-lane route map entry", () => {
  const html = renderToStaticMarkup(
    React.createElement(CmsIndexCompositionWindow, {
      culture: "en-US",
      currentPage: 1,
      totalItems: 8,
      pages: [pageSummary],
      categories: [category],
      products: [heroProduct],
      cartSummary: null,
      reviewHref: "/cms?visibleState=needs-attention",
      reviewLabel: "Review pages",
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
});

test("CheckoutPage renders conversion support and promotion lanes for the checkout surface", () => {
  const html = renderToStaticMarkup(
    React.createElement(CheckoutPage, {
      culture: "en-US",
      model: {
        anonymousId: "anon-1",
        status: "ok",
        message: undefined,
        cart: {
          id: "cart-1",
          currency: "EUR",
          couponCode: null,
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
        },
      },
      draft: {
        fullName: "Ada Lovelace",
        company: "",
        street1: "Main Street 1",
        street2: "",
        postalCode: "10115",
        city: "Berlin",
        state: "",
        countryCode: "DE",
        phoneE164: "+49123456789",
        shippingMethodId: "ship-1",
      },
      intent: {
        currency: "EUR",
        subtotalNetMinor: 1200,
        subtotalGrossMinor: 1400,
        discountTotalMinor: 0,
        shippingOptions: [
          {
            id: "ship-1",
            name: "Standard",
            description: "Standard shipping",
            totalMinor: 100,
          },
        ],
        selectedShippingMethodId: "ship-1",
        selectedShippingTotalMinor: 100,
        shippingCountryCode: "DE",
        shipmentMassGrams: 500,
        grandTotalGrossMinor: 1400,
      },
      intentStatus: "ok",
      memberAddresses: [],
      memberAddressesStatus: "unauthenticated",
      memberProfile: null,
      memberProfileStatus: "unauthenticated",
      memberPreferences: null,
      memberPreferencesStatus: "unauthenticated",
      memberInvoices: [],
      memberInvoicesStatus: "unauthenticated",
      profilePrefillActive: false,
      hasMemberSession: false,
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/account\/sign-in/);
});
test("CartPage renders commerce support and promotion lanes for the basket surface", () => {
  const html = renderToStaticMarkup(
    React.createElement(CartPage, {
      culture: "en-US",
      model: {
        anonymousId: "anon-1",
        status: "ok",
        cart: {
          cartId: "cart-1",
          currency: "EUR",
          subtotalNetMinor: 1200,
          vatTotalMinor: 200,
          grandTotalGrossMinor: 1400,
          couponCode: null,
          items: [
            {
              variantId: "variant-1",
              quantity: 2,
              unitPriceNetMinor: 600,
              addOnPriceDeltaMinor: 0,
              vatRate: 0.19,
              lineNetMinor: 1200,
              lineVatMinor: 200,
              lineGrossMinor: 1400,
              selectedAddOnValueIdsJson: "[]",
              display: {
                variantId: "variant-1",
                name: "Apples",
                href: "/catalog/apples",
                imageUrl: null,
                imageAlt: "Apples",
                sku: "APL-1",
              },
            },
          ],
        },
      },
      memberAddresses: [],
      memberAddressesStatus: "unauthenticated",
      memberProfile: null,
      memberProfileStatus: "unauthenticated",
      memberPreferences: null,
      memberPreferencesStatus: "unauthenticated",
      hasMemberSession: false,
      cartStatus: "updated",
      followUpProducts: [heroProduct],
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/checkout/);
  assert.match(html, /\/account\/sign-in/);
});

test("ProductDetailPage renders composition, support, and cart continuity for drill-in discovery", () => {
  const html = renderToStaticMarkup(
    React.createElement(ProductDetailPage, {
      culture: "en-US",
      product: productDetail,
      categories: [category],
      primaryCategory: category,
      relatedProducts: [heroProduct],
      reviewProducts: [heroProduct],
      cmsPages: [pageSummary],
      cartSummary: {
        status: "ok",
        itemCount: 1,
        currency: "EUR",
        grandTotalGrossMinor: 700,
      },
      status: "ok",
      relatedProductsStatus: "ok",
      reviewProductsStatus: "ok",
      cmsPagesStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Purchase continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cms\/about/);
  assert.match(html, /\/cart/);
});
test("CmsPageDetail renders composition, support, and cart continuity for drill-in content discovery", () => {
  const html = renderToStaticMarkup(
    React.createElement(CmsPageDetail, {
      culture: "en-US",
      page: pageDetail,
      status: "ok",
      relatedPages: [pageSummary],
      relatedStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      cartSummary: {
        status: "ok",
        itemCount: 1,
        currency: "EUR",
        grandTotalGrossMinor: 700,
      },
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Storefront cart continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/cms/);
});
test("CatalogPage renders composition, support, and promotion-lane discovery follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(CatalogPage, {
      culture: "en-US",
      categories: [category],
      products: [heroProduct],
      cmsPages: [pageSummary],
      cartSummary: {
        status: "ok",
        itemCount: 1,
        currency: "EUR",
        grandTotalGrossMinor: 700,
      },
      totalProducts: 1,
      matchingProductsTotal: 1,
      currentPage: 1,
      pageSize: 12,
      visibleState: "all",
      visibleSort: "featured",
      mediaState: "all",
      savingsBand: "all",
      facetSummary: {
        totalCount: 1,
        offerCount: 1,
        baseCount: 0,
        withImageCount: 0,
        missingImageCount: 1,
        valueOfferCount: 1,
        heroOfferCount: 1,
      },
      loadedProductsCount: 1,
      dataStatus: {
        categories: "ok",
        products: "ok",
        cmsPages: "ok",
      },
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Storefront cart continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/cms\/about/);
});
test("CmsPagesIndex renders composition, support, and cart continuity for public content discovery", () => {
  const html = renderToStaticMarkup(
    React.createElement(CmsPagesIndex, {
      culture: "en-US",
      pages: [pageSummary],
      loadedPageCount: 1,
      totalItems: 1,
      matchingItemsTotal: 1,
      pageSize: 12,
      totalPages: 1,
      currentPage: 1,
      status: "ok",
      visibleState: "all",
      visibleSort: "featured",
      metadataFocus: "all",
      metadataSummary: {
        totalCount: 1,
        readyCount: 1,
        attentionCount: 0,
        missingMetaTitleCount: 0,
        missingMetaDescriptionCount: 0,
        missingBothCount: 0,
      },
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      cartSummary: {
        status: "ok",
        itemCount: 1,
        currency: "EUR",
        grandTotalGrossMinor: 700,
      },
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Storefront cart continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/cms\/about/);
});
test("PublicAuthCompositionWindow renders a promotion-lane route map entry", () => {
  const html = renderToStaticMarkup(
    React.createElement(PublicAuthCompositionWindow, {
      culture: "en-US",
      routeCard: {
        label: "Current route",
        title: "Sign in",
        description: "Current auth step",
        href: "/account/sign-in",
        ctaLabel: "Stay in sign in",
      },
      nextCard: {
        label: "Next step",
        title: "Register",
        description: "Create account next",
        href: "/account/register",
        ctaLabel: "Open register",
      },
      routeMapItems: [],
      cmsPages: [pageSummary],
      categories: [category],
      products: [heroProduct],
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(html, /Open promotion lane/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
});

test("InvoicesPage renders storefront support and promotion lanes for protected billing history", () => {
  const html = renderToStaticMarkup(
    React.createElement(InvoicesPage, {
      culture: "en-US",
      invoices: [
        {
          id: "invoice-1",
          orderId: "order-1",
          orderNumber: "ORD-1001",
          status: "Open",
          balanceMinor: 900,
          totalGrossMinor: 1400,
          currency: "EUR",
          issuedAtUtc: "2026-04-10T10:00:00Z",
          dueAtUtc: "2026-04-15T10:00:00Z",
        },
      ],
      status: "ok",
      currentPage: 1,
      totalPages: 1,
      visibleState: "all",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      cartLinkedProductSlugs: [],
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/checkout/);
});
test("OrderDetailPage renders storefront support and promotion lanes for protected order drill-in", () => {
  const html = renderToStaticMarkup(
    React.createElement(OrderDetailPage, {
      culture: "en-US",
      order: {
        id: "order-1",
        orderNumber: "ORD-1001",
        status: "Processing",
        currency: "EUR",
        grandTotalGrossMinor: 1400,
        subtotalNetMinor: 1200,
        shippingTotalMinor: 100,
        taxTotalMinor: 100,
        discountTotalMinor: 0,
        createdAtUtc: "2026-04-10T10:00:00Z",
        billingAddressJson: JSON.stringify({
          fullName: "Ada Lovelace",
          street1: "Main Street 1",
          postalCode: "10115",
          city: "Berlin",
          countryCode: "DE",
        }),
        shippingAddressJson: JSON.stringify({
          fullName: "Ada Lovelace",
          street1: "Main Street 1",
          postalCode: "10115",
          city: "Berlin",
          countryCode: "DE",
        }),
        actions: {
          canRetryPayment: true,
          documentPath: "/documents/order-1.pdf",
        },
        lines: [
          {
            id: "line-1",
            variantId: "variant-1",
            name: "Apples",
            sku: "APL-1",
            quantity: 2,
            unitPriceGrossMinor: 700,
            lineGrossMinor: 1400,
          },
        ],
        payments: [
          {
            id: "payment-1",
            createdAtUtc: "2026-04-10T10:01:00Z",
            provider: "Stripe",
            providerReference: "pi_1",
            amountMinor: 1400,
            currency: "EUR",
            status: "Pending",
            paidAtUtc: null,
          },
        ],
        shipments: [
          {
            id: "shipment-1",
            shipmentNumber: "SHP-1",
            status: "Packed",
            carrier: "DHL",
            trackingNumber: "TRACK-1",
            shippedAtUtc: null,
            deliveredAtUtc: null,
          },
        ],
        invoices: [
          {
            id: "invoice-1",
            orderId: "order-1",
            orderNumber: "ORD-1001",
            status: "Open",
            balanceMinor: 900,
            totalGrossMinor: 1400,
            currency: "EUR",
            issuedAtUtc: "2026-04-10T10:00:00Z",
            dueDateUtc: "2026-04-15T10:00:00Z",
            paidAtUtc: null,
          },
        ],
      },
      status: "ok",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      cartLinkedProductSlugs: [],
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/checkout/);
});
test("InvoiceDetailPage renders storefront support and promotion lanes for protected invoice drill-in", () => {
  const html = renderToStaticMarkup(
    React.createElement(InvoiceDetailPage, {
      culture: "en-US",
      invoice: {
        id: "invoice-1",
        orderNumber: "INV-1001",
        status: "Open",
        currency: "EUR",
        subtotalNetMinor: 1200,
        taxTotalMinor: 200,
        grandTotalGrossMinor: 1400,
        paidAtUtc: null,
        balanceMinor: 1400,
        createdAtUtc: "2026-04-10T10:00:00Z",
        dueAtUtc: "2026-04-17T10:00:00Z",
        actions: {
          canRetryPayment: true,
          documentPath: "/documents/invoice-1.pdf",
        },
        lines: [
          {
            id: "line-1",
            description: "Apples",
            quantity: 2,
            unitPriceNetMinor: 600,
            lineNetMinor: 1200,
            taxRate: 0.19,
          },
        ],
      },
      status: "ok",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      cartLinkedProductSlugs: [],
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/checkout/);
});

test("OrdersPage renders storefront support and promotion lanes for protected history discovery", () => {
  const html = renderToStaticMarkup(
    React.createElement(OrdersPage, {
      culture: "en-US",
      orders: [
        {
          id: "order-1",
          orderNumber: "ORD-1001",
          status: "Processing",
          grandTotalGrossMinor: 1400,
          currency: "EUR",
          createdAtUtc: "2026-04-10T10:00:00Z",
        },
      ],
      status: "ok",
      currentPage: 1,
      totalPages: 1,
      visibleState: "all",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      cartLinkedProductSlugs: [],
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/checkout/);
});
test("MemberDashboardPage renders storefront support and promotion lanes for protected member entry", () => {
  const html = renderToStaticMarkup(
    React.createElement(MemberDashboardPage, {
      culture: "en-US",
      session: {
        isAuthenticated: true,
        accessTokenExpiresAtUtc: "2026-04-11T10:00:00Z",
      },
      profile: {
        id: "profile-1",
        email: "member@example.com",
        firstName: "Ada",
        lastName: "Lovelace",
        phoneE164: "+49123456789",
        phoneNumberConfirmed: true,
        locale: "en-US",
        currency: "EUR",
        timezone: "Europe/Berlin",
        rowVersion: "1",
      },
      profileStatus: "ok",
      preferences: {
        marketingConsent: true,
        allowEmailMarketing: true,
        allowSmsMarketing: true,
        allowWhatsAppMarketing: false,
        allowPromotionalPushNotifications: false,
        allowOptionalAnalyticsTracking: true,
        rowVersion: "1",
      },
      preferencesStatus: "ok",
      customerContext: {
        customerId: "customer-1",
        email: "member@example.com",
        fullName: "Ada Lovelace",
      },
      customerContextStatus: "ok",
      addresses: [],
      addressesStatus: "ok",
      recentOrders: [],
      recentOrdersStatus: "ok",
      recentInvoices: [],
      recentInvoicesStatus: "ok",
      loyaltyOverview: null,
      loyaltyOverviewStatus: "not-found",
      loyaltyBusinesses: [],
      loyaltyBusinessesStatus: "ok",
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      cartLinkedProductSlugs: [],
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/checkout/);
});
test("ProfilePage renders readiness, composition, and storefront follow-up for self-service", () => {
  const html = renderToStaticMarkup(
    React.createElement(ProfilePage, {
      culture: "en-US",
      profile: {
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
      },
      supportedCultures: ["en-US", "de-DE"],
      status: "ok",
      profileStatus: "saved",
      phoneStatus: "confirmed",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
    }),
  );

  assert.match(html, /Profile readiness/);
  assert.match(html, /Keep profile readiness visible/);
  assert.match(html, /Promotion lanes/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/checkout/);
  assert.match(html, /\/account\/preferences/);
});

test("PreferencesPage renders readiness, composition, and storefront follow-up for preference self-service", () => {
  const html = renderToStaticMarkup(
    React.createElement(PreferencesPage, {
      culture: "en-US",
      preferences: {
        rowVersion: "rv-1",
        marketingConsent: true,
        allowEmailMarketing: true,
        allowSmsMarketing: true,
        allowWhatsAppMarketing: false,
        allowPromotionalPushNotifications: false,
        allowOptionalAnalyticsTracking: true,
      },
      status: "ok",
      profile: {
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
      },
      profileStatus: "ok",
      preferencesStatus: "saved",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
    }),
  );

  assert.match(html, /Preferences/);
  assert.match(html, /Move into communication preferences/);
  assert.match(html, /Promotion lanes/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/account\/profile/);
  assert.match(html, /\/account\/security/);
});

test("AddressesPage renders readiness, composition, and storefront follow-up for address self-service", () => {
  const html = renderToStaticMarkup(
    React.createElement(AddressesPage, {
      culture: "en-US",
      addresses: [
        {
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
        },
      ],
      status: "ok",
      addressesStatus: "updated",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
    }),
  );

  assert.match(html, /Address readiness/);
  assert.match(html, /Move into address readiness/);
  assert.match(html, /Promotion lanes/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/checkout/);
  assert.match(html, /\/account\/profile/);
});

test("SecurityPage renders state, composition, and storefront follow-up for security self-service", () => {
  const html = renderToStaticMarkup(
    React.createElement(SecurityPage, {
      culture: "en-US",
      session: {
        isAuthenticated: true,
        accessTokenExpiresAtUtc: "2026-04-11T10:00:00Z",
      },
      profile: {
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
      },
      profileStatus: "ok",
      securityStatus: "saved",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
    }),
  );

  assert.match(html, /Security/);
  assert.match(html, /Keep security state visible/);
  assert.match(html, /Promotion lanes/);
  assert.match(html, /Cart and checkout continuity/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/account\/profile/);
  assert.match(html, /\/account\/preferences/);
});

test("AccountHubPage renders action-center and promotion-lane storefront follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(AccountHubPage, {
      culture: "en-US",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
      returnPath: "/checkout",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/cms\/about/);
  assert.match(html, /\/catalog\?category=fruit/);
  assert.match(html, /\/account\/sign-in/);
});
test("AccountHubCompositionWindow renders a promotion-lane route map entry", () => {
  const html = renderToStaticMarkup(
    React.createElement(AccountHubCompositionWindow, {
      culture: "en-US",
      returnPath: "/account",
      storefrontCart: null,
      cmsPages: [pageSummary],
      categories: [category],
      products: [heroProduct],
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(html, /Open promotion lane/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
});
test("SignInPage renders auth journey and promotion-lane storefront follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(SignInPage, {
      culture: "en-US",
      email: "ada@example.com",
      returnPath: "/checkout",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/account\/register/);
  assert.match(html, /\/account\/activation/);
  assert.match(html, /\/account\/password/);
  assert.match(html, /\/cart/);
});

test("RegisterPage renders auth journey and promotion-lane storefront follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(RegisterPage, {
      culture: "en-US",
      email: "ada@example.com",
      returnPath: "/checkout",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/account\/activation/);
  assert.match(html, /\/account\/sign-in/);
  assert.match(html, /\/cart/);
});

test("ActivationPage renders auth journey and promotion-lane storefront follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(ActivationPage, {
      culture: "en-US",
      email: "ada@example.com",
      token: "CODE-123",
      returnPath: "/checkout",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/account\/sign-in/);
  assert.match(html, /\/account\/password/);
  assert.match(html, /\/cart/);
});

test("PasswordPage renders auth journey and promotion-lane storefront follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(PasswordPage, {
      culture: "en-US",
      email: "ada@example.com",
      token: "RESET-123",
      returnPath: "/checkout",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/account\/sign-in/);
  assert.match(html, /\/account\/activation/);
  assert.match(html, /\/cart/);
});
test("PublicAuthContinuation renders cart continuity plus promotion lanes for auth entry follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(PublicAuthContinuation, {
      culture: "en-US",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      storefrontCart: {
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
      },
      storefrontCartStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lanes/);
  assert.match(html, /Hero offers/);
  assert.match(html, /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/);
  assert.match(html, /Cart continuity/);
  assert.match(html, /\/cart/);
  assert.match(html, /\/cms\/about/);
  assert.match(html, /\/catalog\?category=fruit/);
});
test("MemberAuthRequired renders a promotion-lane board for protected entry fallback", () => {
  const html = renderToStaticMarkup(
    React.createElement(MemberAuthRequired, {
      culture: "en-US",
      title: "Sign in required",
      message: "Protected member route requires sign in.",
      returnPath: "/orders",
      cmsPages: [pageSummary],
      cmsPagesStatus: "ok",
      categories: [category],
      categoriesStatus: "ok",
      products: [heroProduct],
      productsStatus: "ok",
      storefrontCart: null,
      storefrontCartStatus: "not-found",
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(html, /Open promotion lane/);
  assert.match(
    html,
    /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/,
  );
});

test("CommerceAuthHandoff renders a promotion-lane board for guest cart and checkout interruption", () => {
  const html = renderToStaticMarkup(
    React.createElement(CommerceAuthHandoff, {
      culture: "en-US",
      cart: {
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
      },
      returnPath: "/checkout",
      routeKey: "checkout",
      products: [heroProduct],
      productsStatus: "ok",
    }),
  );

  assert.match(html, /Promotion lane/);
  assert.match(html, /Open promotion lane/);
  assert.match(
    html,
    /\/catalog\?visibleState=offers&amp;visibleSort=offers-first&amp;savingsBand=hero/,
  );
});





























test("buildMemberPromotionLaneCards keeps member merchandising lanes explicit", () => {
  assert.deepEqual(buildMemberPromotionLaneCards([heroProduct], "en-US"), [
    {
      id: "member-promotion-lane-hero-offers",
      label: "Promotion lane",
      title: "Hero offers currently lead with Apples.",
      description: "Hero offers stay visible from the member storefront. 1 item starts at \u20ac7.00.",
      href: "/catalog?visibleState=offers&visibleSort=offers-first&savingsBand=hero",
      ctaLabel: "Open promotion lane",
      meta: "1 item",
    },
    {
      id: "member-promotion-lane-value-offers",
      label: "Promotion lane",
      title: "Value offers remain available from the member storefront.",
      description: "Value offers stay visible from the member storefront.",
      href: "/catalog?visibleState=offers&visibleSort=offers-first&savingsBand=value",
      ctaLabel: "Open promotion lane",
      meta: "0 items",
    },
    {
      id: "member-promotion-lane-live-offers",
      label: "Promotion lane",
      title: "Live offers remain available from the member storefront.",
      description: "Live offers stay visible from the member storefront.",
      href: "/catalog?visibleState=offers&visibleSort=savings-desc",
      ctaLabel: "Open promotion lane",
      meta: "0 items",
    },
    {
      id: "member-promotion-lane-base-assortment",
      label: "Promotion lane",
      title: "Base assortment remains available from the member storefront.",
      description: "Base assortment stays visible from the member storefront.",
      href: "/catalog?visibleState=base&visibleSort=base-first",
      ctaLabel: "Open promotion lane",
      meta: "0 items",
    },
  ]);
});
test("MockCheckoutPage renders explicit success, cancellation, and failure handoff actions", () => {
  const html = renderToStaticMarkup(
    React.createElement(MockCheckoutPage, {
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
    }),
  );

  assert.match(html, /Mock hosted checkout/);
  assert.match(html, /Mark payment as succeeded/);
  assert.match(html, /Mark payment as cancelled/);
  assert.match(html, /Mark payment as failed/);
  assert.match(html, /confirmation\/finalize\?providerReference=session-1&amp;outcome=Succeeded/);
  assert.match(
    html,
    /confirmation\/finalize\?providerReference=session-1&amp;outcome=Cancelled&amp;cancelled=true/,
  );
  assert.match(html, /confirmation\/finalize\?providerReference=session-1&amp;outcome=Failed/);
});

test("CatalogContinuationRail renders feature-level wrapper links for home, catalog, CMS, and account follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(CatalogContinuationRail, {
      culture: "en-US",
      title: "Catalog continuation",
      description: "Stay inside the storefront journey.",
      catalogHref: "/catalog?visibleState=offers",
      catalogCtaLabel: "Resume catalog",
    }),
  );

  assert.match(html, /Catalog continuation/);
  assert.match(html, /Stay inside the storefront journey\./);
  assert.match(html, /href="\/"/);
  assert.match(html, /href="\/catalog\?visibleState=offers"/);
  assert.match(html, /href="\/cms"/);
  assert.match(html, /href="\/account"/);
  assert.match(html, /Resume catalog/);
});

test("CmsContinuationRail renders feature-level wrapper links for home, catalog, and account follow-up", () => {
  const html = renderToStaticMarkup(
    React.createElement(CmsContinuationRail, {
      culture: "en-US",
      title: "CMS continuation",
      description: "Keep content recovery and storefront follow-up explicit.",
    }),
  );

  assert.match(html, /CMS continuation/);
  assert.match(html, /Keep content recovery and storefront follow-up explicit\./);
  assert.match(html, /href="\/"/);
  assert.match(html, /href="\/catalog"/);
  assert.match(html, /href="\/account"/);
});



test("buildPromotionLaneRouteMapItem keeps the strongest merchandising lane explicit", () => {
  assert.deepEqual(
    buildPromotionLaneRouteMapItem({
      id: "shared-promotion-lane",
      products: [heroProduct],
      culture: "en-US",
      copy: {
        cardLabel: "Promotion lane",
        heroLabel: "Hero offers",
        valueLabel: "Value offers",
        liveOffersLabel: "Live offers",
        baseLabel: "Base assortment",
        title: "Open {lane} around {product}",
        fallbackTitle: "Open {lane}",
        description: "{count} items in {lane} from {price}",
        fallbackDescription: "Browse {lane}",
        cta: "Open promotion lane",
        meta: "{count} offers visible",
      },
    }),
    {
      id: "shared-promotion-lane",
      label: "Promotion lane",
      title: "Open Hero offers around Apples",
      description: "1 items in Hero offers from €7.00",
      href: "/catalog?visibleState=offers&visibleSort=offers-first&savingsBand=hero",
      ctaLabel: "Open promotion lane",
      meta: "1 offers visible",
    },
  );
});

