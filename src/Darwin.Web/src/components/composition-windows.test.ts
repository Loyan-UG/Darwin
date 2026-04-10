import assert from "node:assert/strict";
import test from "node:test";
import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { AccountContentCompositionWindow } from "@/components/account/account-content-composition-window";
import { AccountHubCompositionWindow } from "@/components/account/account-hub-composition-window";
import { AccountStorefrontWindow } from "@/components/account/account-storefront-window";
import { PublicAuthCompositionWindow } from "@/components/account/public-auth-composition-window";
import { CartContentCompositionWindow } from "@/components/cart/cart-content-composition-window";
import { CatalogContentCompositionWindow } from "@/components/catalog/catalog-content-composition-window";
import { ProductContentCompositionWindow } from "@/components/catalog/product-content-composition-window";
import { CommerceAuthHandoff } from "@/components/checkout/commerce-auth-handoff";
import { CheckoutContentCompositionWindow } from "@/components/checkout/checkout-content-composition-window";
import { ConfirmationContentCompositionWindow } from "@/components/checkout/confirmation-content-composition-window";
import { CommerceStorefrontWindow } from "@/components/checkout/commerce-storefront-window";
import { CmsContentCompositionWindow } from "@/components/cms/cms-content-composition-window";
import { CmsIndexCompositionWindow } from "@/components/cms/cms-index-composition-window";
import { MemberAuthRequired } from "@/components/member/member-auth-required";
import { MemberStorefrontWindow } from "@/components/member/member-storefront-window";
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



test("CatalogStorefrontSupportWindow renders CMS, product, and cart continuity follow-up", () => {
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
  assert.match(html, /Storefront cart continuity/);
  assert.match(html, /\/cms/);
  assert.match(html, /\/checkout/);
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
      cartSectionTitle: "Cart and checkout continuity",
      cartSectionMessage: "Resume cart review or checkout without leaving the member storefront context.",
      cartSectionCartCtaLabel: "Open cart",
      cartSectionCheckoutCtaLabel: "Open checkout",
    }),
  );

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




