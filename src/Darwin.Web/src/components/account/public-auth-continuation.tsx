import {
  PublicContinuationRail,
  type PublicContinuationItem,
} from "@/components/shell/public-continuation-rail";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicCartSummary } from "@/features/cart/types";
import type { PublicPageSummary } from "@/features/cms/types";
import {
  getProductSavingsPercent,
  sortProductsByOpportunity,
} from "@/features/catalog/merchandising";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath } from "@/lib/locale-routing";
import { formatResource, getMemberResource } from "@/localization";

type PublicAuthContinuationProps = {
  culture: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  storefrontCart: PublicCartSummary | null;
  storefrontCartStatus: string;
};

export function PublicAuthContinuation({
  culture,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  storefrontCart,
  storefrontCartStatus,
}: PublicAuthContinuationProps) {
  const copy = getMemberResource(culture);
  const cartLineCount =
    storefrontCart?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0;
  const productOpportunities = sortProductsByOpportunity(products);

  const items: PublicContinuationItem[] = [
    ...(storefrontCart && cartLineCount > 0
      ? [
          {
            id: "auth-cart",
            label: copy.publicAuthCartLabel,
            title: copy.publicAuthCartTitle,
            description: formatResource(copy.publicAuthCartDescription, {
              itemCount: cartLineCount,
              total: formatMoney(
                storefrontCart.grandTotalGrossMinor,
                storefrontCart.currency,
                culture,
              ),
              status: storefrontCartStatus,
            }),
            href: "/cart",
            ctaLabel: copy.publicAuthCartCta,
          },
        ]
      : [
          {
            id: "auth-cart",
            label: copy.publicAuthCartLabel,
            title: copy.publicAuthCartTitle,
            description:
              storefrontCartStatus === "ok"
                ? copy.publicAuthCartFallbackDescription
                : formatResource(copy.publicAuthCartEmptyMessage, {
                    status: storefrontCartStatus,
                  }),
            href: "/cart",
            ctaLabel: copy.publicAuthCartCta,
          },
        ]),
    {
      id: "auth-home",
      label: copy.memberCrossSurfaceTitle,
      title: copy.accountHubHomeTitle,
      description: copy.accountHubHomeDescription,
      href: "/",
      ctaLabel: copy.memberCrossSurfaceHomeCta,
    },
    ...(cmsPages.length > 0
      ? cmsPages.map((page) => ({
          id: `auth-cms-${page.id}`,
          label: copy.accountHubCmsLabel,
          title: page.title,
          description:
            page.metaDescription ?? copy.publicAuthCmsFallbackDescription,
          href: `/cms/${page.slug}`,
          ctaLabel: copy.accountHubCmsCta,
        }))
      : [
          {
            id: "auth-cms",
            label: copy.accountHubCmsLabel,
            title: copy.accountHubCmsTitle,
            description:
              cmsPagesStatus === "ok"
                ? copy.publicAuthCmsFallbackDescription
                : formatResource(copy.publicAuthCmsEmptyMessage, {
                    status: cmsPagesStatus,
                  }),
            href: "/cms",
            ctaLabel: copy.accountHubCmsCta,
          },
        ]),
    ...(categories.length > 0
      ? categories.map((category) => ({
          id: `auth-catalog-${category.id}`,
          label: copy.accountHubCatalogLabel,
          title: category.name,
          description:
            category.description ?? copy.publicAuthCatalogFallbackDescription,
          href: buildAppQueryPath("/catalog", { category: category.slug }),
          ctaLabel: copy.memberCrossSurfaceCatalogCta,
        }))
      : [
          {
            id: "auth-catalog",
            label: copy.accountHubCatalogLabel,
            title: copy.accountHubCatalogTitle,
            description:
              categoriesStatus === "ok"
                ? copy.publicAuthCatalogFallbackDescription
                : formatResource(copy.publicAuthCatalogEmptyMessage, {
                    status: categoriesStatus,
                  }),
            href: "/catalog",
            ctaLabel: copy.memberCrossSurfaceCatalogCta,
          },
        ]),
    ...(productOpportunities.length > 0
      ? productOpportunities.map((product) => {
          const savingsPercent = getProductSavingsPercent(product);

          return {
            id: `auth-product-${product.id}`,
            label: copy.publicAuthProductLabel,
            title: product.name,
            description:
              savingsPercent !== null
                ? formatResource(copy.publicAuthProductOfferDescription, {
                    savingsPercent,
                    price: formatMoney(
                      product.priceMinor,
                      product.currency,
                      culture,
                    ),
                  })
                : product.shortDescription ??
                  copy.publicAuthProductFallbackDescription,
            href: `/catalog/${product.slug}`,
            ctaLabel: copy.publicAuthProductCta,
          };
        })
      : [
          {
            id: "auth-product",
            label: copy.publicAuthProductLabel,
            title: copy.publicAuthProductTitle,
            description:
              productsStatus === "ok"
                ? copy.publicAuthProductFallbackDescription
                : formatResource(copy.publicAuthProductEmptyMessage, {
                    status: productsStatus,
                  }),
            href: "/catalog",
            ctaLabel: copy.publicAuthProductCta,
          },
        ]),
  ];

  return (
    <PublicContinuationRail
      culture={culture}
      eyebrow={copy.memberCrossSurfaceTitle}
      title={copy.accountHubRouteMapTitle}
      description={formatResource(copy.publicAuthStorefrontWindowMessage, {
        cartStatus: storefrontCartStatus,
        cartLineCount,
        cmsStatus: cmsPagesStatus,
        categoriesStatus,
        productsStatus,
        pageCount: cmsPages.length,
        categoryCount: categories.length,
        productCount: products.length,
      })}
      items={items}
    />
  );
}
