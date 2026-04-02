import "server-only";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { formatMoney } from "@/lib/formatting";
import { getSupportedCultures } from "@/lib/request-culture";
import { formatResource, getHomeResource } from "@/localization";
import type { WebPagePart } from "@/web-parts/types";

export async function getHomePageParts(culture: string): Promise<WebPagePart[]> {
  const copy = getHomeResource(culture);
  const supportedCultures = getSupportedCultures();
  const [pagesResult, productsResult, categoriesResult] = await Promise.all([
    getPublishedPages({
      page: 1,
      pageSize: 3,
    }),
    getPublicProducts({
      page: 1,
      pageSize: 3,
      culture,
    }),
    getPublicCategories(culture),
  ]);

  return [
    {
      id: "home-hero",
      kind: "hero",
      eyebrow: copy.heroEyebrow,
      title: copy.heroTitle,
      description: copy.heroDescription,
      actions: [
        { label: copy.browseCatalogCta, href: "/catalog" },
        { label: copy.openCheckoutCta, href: "/checkout", variant: "secondary" },
      ],
      highlights: [
        copy.heroHighlightTheme,
        copy.heroHighlightComposition,
        formatResource(copy.heroHighlightStatus, {
          pagesStatus: pagesResult.status,
          productsStatus: productsResult.status,
        }),
      ],
      panelTitle: copy.heroPanelTitle,
    },
    {
      id: "home-metrics",
      kind: "stat-grid",
      eyebrow: copy.metricsEyebrow,
      title: copy.metricsTitle,
      description: copy.metricsDescription,
      metrics: [
        {
          id: "metric-pages",
          label: copy.metricPagesLabel,
          value: String(pagesResult.data?.total ?? pagesResult.data?.items.length ?? 0),
          note: copy.metricPagesNote,
        },
        {
          id: "metric-products",
          label: copy.metricProductsLabel,
          value: String(
            productsResult.data?.total ?? productsResult.data?.items.length ?? 0,
          ),
          note: copy.metricProductsNote,
        },
        {
          id: "metric-categories",
          label: copy.metricCategoriesLabel,
          value: String(
            categoriesResult.data?.total ??
              categoriesResult.data?.items.length ??
              0,
          ),
          note: copy.metricCategoriesNote,
        },
        {
          id: "metric-cultures",
          label: copy.metricCulturesLabel,
          value: String(supportedCultures.length),
          note: copy.metricCulturesNote,
        },
      ],
    },
    {
      id: "home-shortcuts",
      kind: "card-grid",
      eyebrow: copy.shortcutsEyebrow,
      title: copy.shortcutsTitle,
      description: copy.shortcutsDescription,
      cards: [
        {
          id: "shortcut-cms",
          eyebrow: copy.shortcutCmsEyebrow,
          title: copy.shortcutCmsTitle,
          description: copy.shortcutCmsDescription,
          href: "/cms",
          ctaLabel: copy.openCmsCta,
        },
        {
          id: "shortcut-catalog",
          eyebrow: copy.shortcutCatalogEyebrow,
          title: copy.shortcutCatalogTitle,
          description: copy.shortcutCatalogDescription,
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
        },
        {
          id: "shortcut-account",
          eyebrow: copy.shortcutAccountEyebrow,
          title: copy.shortcutAccountTitle,
          description: copy.shortcutAccountDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
        },
      ],
      emptyMessage: copy.shortcutsEmptyMessage,
    },
    {
      id: "home-cms-spotlight",
      kind: "card-grid",
      eyebrow: copy.cmsSpotlightEyebrow,
      title: copy.cmsSpotlightTitle,
      description: copy.cmsSpotlightDescription,
      cards:
        pagesResult.data?.items.map((page) => ({
          id: page.id,
          eyebrow: copy.cmsPageEyebrow,
          title: page.title,
          description: page.metaDescription ?? copy.cmsPageDescriptionFallback,
          href: `/cms/${page.slug}`,
          ctaLabel: copy.readPageCta,
          meta: page.slug,
        })) ?? [],
      emptyMessage: pagesResult.message ?? copy.cmsSpotlightEmptyMessage,
    },
    {
      id: "home-product-spotlight",
      kind: "card-grid",
      eyebrow: copy.productSpotlightEyebrow,
      title: copy.productSpotlightTitle,
      description: copy.productSpotlightDescription,
      cards:
        productsResult.data?.items.map((product) => ({
          id: product.id,
          eyebrow: copy.productEyebrow,
          title: product.name,
          description: product.shortDescription ?? copy.productDescriptionFallback,
          href: `/catalog/${product.slug}`,
          ctaLabel: copy.viewProductCta,
          meta: formatMoney(product.priceMinor, product.currency, culture),
        })) ?? [],
      emptyMessage:
        productsResult.message ?? copy.productSpotlightEmptyMessage,
    },
  ];
}
